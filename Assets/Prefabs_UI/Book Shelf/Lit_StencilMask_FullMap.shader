Shader "URP/BasicStencilLit"
{
  Properties
    {
        [Header(Base Maps)]
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Base Color (RGB) Alpha (A)", 2D) = "white" {}

        _MetallicMult ("Metallic Multiplier", Range(0,1)) = 1.0

        _RoughnessMult ("Roughness Multiplier", Range(0,2)) = 1.0
        _SmoothnessMult ("Smoothness Multiplier (legacy)", Range(0,2)) = 1.0  // giữ lại để tiện adjust

        _NormalScale ("Normal Strength", Float) = 1.0

        _AOStrength ("AO Strength", Range(0,3)) = 1.0

        // Tùy chọn Height/Parallax (bật khi cần)
        // [NoScaleOffset] _HeightMap ("Height (G)", 2D) = "black" {}
        // _HeightScale ("Parallax Height Scale", Range(0,0.1)) = 0.02
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

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS                   : SV_POSITION;
                float2 uv                           : TEXCOORD0;
                float3 positionWS                   : TEXCOORD1;
                float3 normalWS                     : TEXCOORD2;
                float4 tangentWS                    : TEXCOORD3;
                float3 viewDirWS                    : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(input.lightmapUV, vertexSH, 5);
                float4 fogAndVL                     : TEXCOORD6; // x: fog, yzw: vertex lighting
                float4 shadowCoord                  : TEXCOORD7;
            };

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicMap);    SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AOMap);          SAMPLER(sampler_AOMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _NormalScale;
                float _MetallicMult;
                float _RoughnessMult;
                float _SmoothnessMult;
                float _AOStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                output.normalWS   = norm.normalWS;
                output.tangentWS  = float4(norm.tangentWS.xyz, input.tangentOS.w * unity_WorldTransformParams.w);
                output.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                half3 vertexLight = VertexLighting(pos.positionWS, norm.normalWS);
                half fogFactor = ComputeFogFactor(pos.positionCS.z);
                output.fogAndVL = half4(fogFactor, vertexLight);

                output.shadowCoord = GetShadowCoord(pos);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // --- Base Color ---
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = baseTex.rgb * _BaseColor.rgb;
                half alpha   = baseTex.a * _BaseColor.a;

                // --- Normal ---
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
                half3x3 TBN = half3x3(input.tangentWS.xyz, cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w, input.normalWS);
                half3 normalWS = TransformTangentToWorld(normalTS, TBN);
                normalWS = NormalizeNormalPerPixel(normalWS);

                // --- Metallic & Roughness (Autodesk Interactive standard) ---
                half metallic = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r * _MetallicMult;
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r * _RoughnessMult;
                half smoothness = (1.0 - roughness) * _SmoothnessMult;

                // --- AO ---
                half ao = LerpWhiteTo(SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, input.uv).g, _AOStrength);

                // --- URP PBR ---
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.metallic   = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS   = normalTS;
                surfaceData.occlusion  = ao;
                surfaceData.alpha      = alpha;
                surfaceData.emission   = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS      = input.positionWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);
                inputData.shadowCoord     = input.shadowCoord;
                inputData.fogCoord        = input.fogAndVL.x;
                inputData.vertexLighting  = input.fogAndVL.yzw;
                inputData.bakedGI         = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask      = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }

        // Để nhận bóng đổ đúng trên các object khác
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}