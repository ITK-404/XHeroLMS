Shader "URP/BasicStencilLit"
{
  Properties
    {
        [Header(Base Maps)]
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB) Smoothness(A)", 2D) = "white" {}
        
        [Header(Normal)]
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        
        [Header(Metallic Roughness)]
        _MetallicGlossMap ("Metallic (R) Occlusion (G) Roughness (B)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5   // Inverse of Roughness
        
        [Header(Occlusion)]
        _OcclusionMap ("Occlusion Map (G channel if packed)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1.0
        
        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionMap ("Emission Map", 2D) = "black" {}
        
        [Header(Grayscale)]
        [Toggle(_GRAYSCALE_ON)] _Grayscale ("Grayscale", Float) = 0
        _GrayscaleAmount ("Grayscale Amount", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque" 
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "ShaderModel"="4.5"
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
            #pragma vertex vert
            #pragma fragment frag

            // Shader features
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _GRAYSCALE_ON

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
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;
                float3 positionWS       : TEXCOORD1;
                float3 normalWS         : TEXCOORD2;
                float4 tangentWS        : TEXCOORD3; // xyz: tangent, w: sign
            };

            // Textures & Samplers
            TEXTURE2D(_MainTex);                SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);                SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);       SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);           SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                
                float _BumpScale;
                float _Metallic;
                float _Glossiness;
                float _OcclusionStrength;
                
                half3 _EmissionColor;
                
                float _GrayscaleAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = norm.normalWS;
                output.tangentWS = float4(norm.tangentWS, input.tangentOS.w * unity_WorldTransformParams.w);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- Sample base maps ---
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = albedoAlpha.rgb * _BaseColor.rgb;

                // --- Normal mapping ---
                #ifdef _NORMALMAP
                    half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                    half3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                    float sgn = input.tangentWS.w;
                    float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                    half3x3 TBN = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                    half3 normalWS = TransformTangentToWorld(normalTS, TBN);
                #else
                    half3 normalWS = normalize(input.normalWS);
                #endif

                // --- Metallic & Smoothness ---
                half metallic = _Metallic;
                half smoothness = _Glossiness;
                #ifdef _METALLICGLOSSMAP
                    half4 metallicMap = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                    metallic *= metallicMap.r;
                    smoothness *= metallicMap.b; // Roughness in B → invert to smoothness
                #endif
                half roughness = 1.0 - smoothness;

                // --- Occlusion ---
                half occlusion = 1.0;
                #ifdef _OCCLUSIONMAP
                    occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                #elif _METALLICGLOSSMAP
                    occlusion = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv).g;
                #endif
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);

                // --- Emission ---
                half3 emission = 0;
                #ifdef _EMISSION
                    half3 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                    emission = emissionTex * _EmissionColor;
                #endif

                // --- Grayscale (optional post-process) ---
                #ifdef _GRAYSCALE_ON
                    half gray = dot(albedo, half3(0.299, 0.587, 0.114));
                    albedo = lerp(albedo, gray.xxx, _GrayscaleAmount);
                #endif

                // --- Prepare input data for URP lighting ---
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                inputData.bakedGI = SAMPLE_GI(0, input.positionWS, normalWS); // Simple GI

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.emission = emission;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = albedoAlpha.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;

                // URP Global Illumination + Main Light
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // Apply occlusion again (URP applies it internally, but we reinforce if needed)
                color.rgb *= occlusion;

                // Add emission
                color.rgb += emission;

                return color;
            }
            ENDHLSL
        }

        // Shadow caster pass (nếu cần shadow)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        // Depth only pass
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}