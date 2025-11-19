Shader "URP/Autodesk Interactive Full Maps + stencil"
{
    Properties
    {
        _BaseColor              ("Color Tint", Color) = (1,1,1,1)
        _MainTex                 ("Base Color (RGB) Opacity (A)", 2D) = "white" {}

        [NoScaleOffset] _MetallicMap      ("Metallic (R)", 2D) = "white" {}
        _MetallicMult            ("Metallic Multiplier", Range(0,1)) = 1.0

        [NoScaleOffset] _RoughnessMap     ("Roughness (R)", 2D) = "white" {}
        _RoughnessMult           ("Roughness Multiplier", Range(0,2)) = 1.0
        _SmoothnessMult          ("Smoothness Boost (tùy chọn)", Range(0,2)) = 1.0

        [NoScaleOffset] _NormalMap        ("Normal (OpenGL)", 2D) = "bump" {}
        _NormalScale             ("Normal Strength", Float) = 1.0

        [NoScaleOffset] _AOMap            ("Ambient Occlusion (R hoặc G)", 2D) = "white" {}
        _AOStrength              ("AO Strength", Range(0,3)) = 1.0

        [NoScaleOffset] _HeightMap        ("Height / Displacement (G)", 2D) = "black" {}
        _HeightScale             ("Parallax Height Scale", Range(-0.1,0.1)) = 0.02
        [Toggle] _ParallaxOn     ("Enable Simple Parallax", Float) = 0

        [NoScaleOffset] _DetailAlbedoMap  ("Detail Albedo (RGB)", 2D) = "grey" {}
        [NoScaleOffset] _DetailNormalMap  ("Detail Normal", 2D) = "bump" {}
        _DetailScale             ("Detail Tiling", Float) = 4.0
        _DetailStrength          ("Detail Normal Strength", Range(0,2)) = 1.0

        [NoScaleOffset] _EmissiveMap      ("Emissive (RGB)", 2D) = "black" {}
        [HDR] _EmissiveColor     ("Emissive Color", Color) = (0,0,0,1)
        _EmissiveIntensity       ("Emissive Intensity", Float) = 1.0

        _Cutoff                  ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle] _AlphaClip      ("Enable Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Stencil
        {
            Ref 1
            Comp Equal
            Pass Keep
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Blend One Zero
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
            #pragma shader_feature _PARALLAXON_ON
            #pragma shader_feature _ALPHACLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;
                float2 detailUV         : TEXCOORD8;
                float3 positionWS       : TEXCOORD1;
                float3 normalWS         : TEXCOORD2;
                float4 tangentWS        : TEXCOORD3; // w = sign
                float3 viewDirWS        : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                float4 fogAndVL         : TEXCOORD6;
                float4 shadowCoord      : TEXCOORD7;
            };

            // Textures
            TEXTURE2D(_MainTex);           SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicMap);       SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_RoughnessMap);      SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_NormalMap);         SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AOMap);             SAMPLER(sampler_AOMap);
            TEXTURE2D(_HeightMap);         SAMPLER(sampler_HeightMap);
            TEXTURE2D(_DetailAlbedoMap);   SAMPLER(sampler_DetailAlbedoMap);
            TEXTURE2D(_DetailNormalMap);   SAMPLER(sampler_DetailNormalMap);
            TEXTURE2D(_EmissiveMap);       SAMPLER(sampler_EmissiveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _NormalScale;
                float _MetallicMult;
                float _RoughnessMult;
                float _SmoothnessMult;
                float _AOStrength;
                float _HeightScale;
                float _DetailScale;
                float _DetailStrength;
                float3 _EmissiveColor;
                float _EmissiveIntensity;
                float _Cutoff;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS   = pos.positionCS;
                o.positionWS   = pos.positionWS;
                o.uv           = TRANSFORM_TEX(input.uv, _MainTex);
                o.detailUV     = input.uv * _DetailScale;
                o.normalWS     = norm.normalWS;
                o.tangentWS    = float4(norm.tangentWS.xyz, input.tangentOS.w * unity_WorldTransformParams.w);
                o.viewDirWS    = GetWorldSpaceViewDir(pos.positionWS);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS, o.vertexSH);

                half3 vertexLight = VertexLighting(pos.positionWS, norm.normalWS);
                half fogFactor = ComputeFogFactor(pos.positionCS.z);
                o.fogAndVL = half4(fogFactor, vertexLight);

                o.shadowCoord = GetShadowCoord(pos);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // Simple parallax (optional)
                #ifdef _PARALLAXON_ON
                    float height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.uv).g;
                    float2 offset = ParallaxOffset(height, _HeightScale, i.viewDirWS, i.tangentWS.xyz, cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w, i.normalWS);
                    i.uv += offset;
                    i.detailUV += offset * _DetailScale;
                #endif

                // Base Color
                half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 albedo = base.rgb * _BaseColor.rgb;
                half alpha = base.a * _BaseColor.a;

                #ifdef _ALPHACLIP_ON
                    clip(alpha - _Cutoff);
                #endif

                // Normal
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv), _NormalScale);
                // Detail normal (optional)
                #if defined(_DetailNormalMap)
                    half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, i.detailUV), _DetailStrength);
                    normalTS = BlendNormalRNM(normalTS, detailNormalTS);
                #endif
                half3x3 TBN = half3x3(i.tangentWS.xyz,
                                      cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w,
                                      i.normalWS);
                half3 normalWS = TransformTangentToWorld(normalTS, TBN);
                normalWS = NormalizeNormalPerPixel(normalWS);

                // Metallic / Roughness (Autodesk Interactive)
                half metallic  = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, i.uv).r * _MetallicMult;
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, i.uv).r * _RoughnessMult;
                half smoothness = (1.0 - roughness) * _SmoothnessMult;

                // AO
                half ao = LerpWhiteTo(SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv).g, _AOStrength);

                // Emissive
                half3 emissive = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, i.uv).rgb * _EmissiveColor * _EmissiveIntensity;

                // Detail Albedo (optional overlay)
                #if defined(_DetailAlbedoMap)
                    half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, i.detailUV).rgb;
                    albedo = albedo * lerp(1, detailAlbedo * 2.0, detailAlbedo.r);
                #endif

                // URP PBR
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.metallic   = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS   = normalTS;
                surfaceData.occlusion  = ao;
                surfaceData.emission   = emissive;
                surfaceData.alpha      = alpha;

                InputData inputData = (InputData)0;
                inputData.positionWS      = i.positionWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = SafeNormalize(i.viewDirWS);
                inputData.shadowCoord     = i.shadowCoord;
                inputData.fogCoord        = i.fogAndVL.x;
                inputData.vertexLighting  = i.fogAndVL.yzw;
                inputData.bakedGI         = SAMPLE_GI(i.lightmapUV, i.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask      = SAMPLE_SHADOWMASK(i.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    Fallback "Universal Render Pipeline/Lit"
}