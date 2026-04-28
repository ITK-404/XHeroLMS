Shader "URP/Autodesk Interactive Full Maps + Stencil (No Parallax, No AlphaClip)"
{
    Properties
    {
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Base Color (RGB) Opacity (A)", 2D) = "white" {}

        [NoScaleOffset] _MetallicMap ("Metallic (R) - Optional", 2D) = "white" {}
        _MetallicMult ("Metallic Mult", Range(0,1)) = 1.0

        [NoScaleOffset] _RoughnessMap ("Roughness (R) - Optional", 2D) = "white" {}
        _RoughnessMult ("Roughness Mult", Range(0,2)) = 1.0
        _SmoothnessMult ("Smoothness Boost", Range(0,2)) = 1.0

        [NoScaleOffset] _NormalMap ("Normal (OpenGL) - Optional", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Float) = 1.0

        [NoScaleOffset] _AOMap ("Ambient Occlusion (R/G) - Optional", 2D) = "white" {}
        _AOStrength ("AO Strength", Range(0,3)) = 1.0

        [NoScaleOffset] _EmissiveMap ("Emissive (RGB) - Optional", 2D) = "black" {}
        [HDR] _EmissiveColor ("Emissive HDR Color", Color) = (0,0,0,1)
        _EmissiveIntensity ("Emissive Intensity", Float) = 1.0

        [Toggle(_ENVLIGHTING_ON)] _EnvLighting ("Enable Environment Lighting", Float) = 1

        _Cutoff ("Unused Placeholder", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

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

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _ENVLIGHTING_ON

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
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float3 viewDirWS  : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                float3 vertexLight: TEXCOORD6;
                float4 shadowCoord: TEXCOORD7;
            };

            TEXTURE2D(_MainTex);           SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicMap);       SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_RoughnessMap);      SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_NormalMap);         SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AOMap);             SAMPLER(sampler_AOMap);
            TEXTURE2D(_EmissiveMap);       SAMPLER(sampler_EmissiveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _NormalScale;
                float _MetallicMult;
                float _RoughnessMult;
                float _SmoothnessMult;
                float _AOStrength;
                float3 _EmissiveColor;
                float _EmissiveIntensity;
                float _Cutoff;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionCS   = pos.positionCS;
                o.posWS        = pos.positionWS;
                o.uv           = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS     = norm.normalWS;
                o.tangentWS    = float4(norm.tangentWS.xyz, v.tangentOS.w * unity_WorldTransformParams.w);
                o.viewDirWS    = GetWorldSpaceViewDir(pos.positionWS);

                OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS, o.vertexSH);

                o.vertexLight  = VertexLighting(pos.positionWS, norm.normalWS);
                o.shadowCoord  = GetShadowCoord(pos);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // === Base Color ===
                half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 albedo = base.rgb * _BaseColor.rgb;
                half alpha = base.a * _BaseColor.a;

                // === Normal Map ===
                half3 normalTS = half3(0,0,1);
                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv);
                if (any(normalSample.rgb != half3(0.5, 0.5, 1.0)))
                {
                    normalTS = UnpackNormalScale(normalSample, _NormalScale);
                }

                // TBN
                half3x3 TBN = half3x3(
                    i.tangentWS.xyz,
                    cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w,
                    i.normalWS
                );
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, TBN));

                // Metallic
                half metallic = 0;
                half4 metSample = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, i.uv);
                if (metSample.r < 0.99)
                    metallic = metSample.r * _MetallicMult;

                // Roughness -> Smoothness
                half roughness = 1.0;
                half4 roughSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, i.uv);
                if (roughSample.r < 0.99)
                    roughness = roughSample.r * _RoughnessMult;
                half smoothness = (1.0 - roughness) * _SmoothnessMult;

                // AO
                half ao = 1.0;
                half aoSample = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv).g;
                if (aoSample < 0.99)
                    ao = lerp(1.0, aoSample, _AOStrength);

                // Emissive
                half3 emissive = 0;
                half3 emisSample = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, i.uv).rgb;
                if (any(emisSample > 0.01))
                    emissive = emisSample * _EmissiveColor * _EmissiveIntensity;

                // === Final PBR ===
                SurfaceData surf = (SurfaceData)0;
                surf.albedo     = albedo;
                surf.metallic   = metallic;
                surf.smoothness = smoothness;
                surf.normalTS   = normalTS;
                surf.occlusion  = ao;
                surf.emission   = emissive;
                surf.alpha      = alpha;

                InputData inputData = (InputData)0;
                inputData.positionWS      = i.posWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = SafeNormalize(i.viewDirWS);
                inputData.shadowCoord     = i.shadowCoord;
                inputData.fogCoord        = 0; // Fog disabled
                inputData.vertexLighting  = i.vertexLight;
                
                // Toggle Environment Lighting (GI)
                #if defined(_ENVLIGHTING_ON)
                    inputData.bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, inputData.normalWS);
                #else
                    inputData.bakedGI = half3(0, 0, 0);
                #endif
                
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask      = SAMPLE_SHADOWMASK(i.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surf);
                // No fog mixing
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}