Shader "Custom/URP/SkinSSS"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.4
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.25

        [Header(Subsurface Scattering)]
        _ThicknessMap ("Thickness Map", 2D) = "white" {}
        _SSSColor ("SSS Color", Color) = (1.0, 0.25, 0.15, 1)
        _SSSStrength ("SSS Strength", Range(0, 5)) = 1
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.3
        _SSSPower ("SSS Falloff", Range(0.5, 16)) = 4
        _SSSAmbient ("SSS Ambient", Range(0, 1)) = 0.1

        [Header(Shadow)]
        _SSSShadowInfluence ("SSS Shadow Influence", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            // Main light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;

                float2 uv         : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_ThicknessMap);
            SAMPLER(sampler_ThicknessMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _Smoothness;
                float _SpecularStrength;

                float4 _SSSColor;
                float _SSSStrength;
                float _SSSDistortion;
                float _SSSPower;
                float _SSSAmbient;

                float _SSSShadowInfluence;

            CBUFFER_END


            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }


            // -------------------------------------------------------
            // Fake SSS
            // -------------------------------------------------------
            float3 CalculateSSS(
                float3 normalWS,
                float3 viewDirWS,
                float3 lightDirWS,
                float3 lightColor,
                float thickness,
                float shadowAttenuation)
            {
                // ---------------------------------------------------
                // 1. Basic back lighting
                //
                // Light phía sau surface:
                //
                // N ---->
                //
                // <---- Light
                //
                // dot(-N, L) -> lớn
                // ---------------------------------------------------

                float backLight =
                    saturate(dot(-normalWS, lightDirWS));


                // ---------------------------------------------------
                // 2. Distorted normal
                //
                // Giúp scattering không chỉ xuất hiện đúng
                // silhouette mà lan mềm hơn.
                // ---------------------------------------------------

                float3 distortedLightDir =
                    normalize(
                        lightDirWS +
                        normalWS * _SSSDistortion
                    );


                // ---------------------------------------------------
                // 3. View-dependent transmission
                //
                // Tạo cảm giác ánh sáng xuyên qua material
                // hướng về camera.
                // ---------------------------------------------------

                float viewScatter =
                    saturate(
                        dot(
                            viewDirWS,
                            -distortedLightDir
                        )
                    );

                viewScatter =
                    pow(
                        viewScatter,
                        _SSSPower
                    );


                // Kết hợp 2 dạng scatter.
                float scatter =
                    max(backLight, viewScatter);


                // ---------------------------------------------------
                // Shadow
                //
                // Không nhất thiết phải để SSS bị shadow triệt tiêu
                // hoàn toàn.
                // ---------------------------------------------------

                float sssShadow =
                    lerp(
                        1.0,
                        shadowAttenuation,
                        _SSSShadowInfluence
                    );


                // ---------------------------------------------------
                // Final
                // ---------------------------------------------------

                float sssAmount =
                    scatter *
                    thickness *
                    _SSSStrength;


                // Một lượng ambient scattering nhỏ giúp
                // vùng shadow không chết hoàn toàn.
                sssAmount +=
                    thickness *
                    _SSSAmbient;


                return
                    _SSSColor.rgb *
                    lightColor *
                    sssAmount *
                    sssShadow;
            }


            half4 Frag(Varyings input) : SV_Target
            {
                // ---------------------------------------------------
                // Surface data
                // ---------------------------------------------------

                float3 N =
                    normalize(input.normalWS);

                float3 V =
                    SafeNormalize(
                        GetCameraPositionWS() -
                        input.positionWS
                    );


                float4 baseTex =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );

                float3 albedo =
                    baseTex.rgb *
                    _BaseColor.rgb;


                // ---------------------------------------------------
                // Thickness
                //
                // White = ánh sáng xuyên mạnh
                // Black = gần như không có SSS
                // ---------------------------------------------------

                float thickness =
                    SAMPLE_TEXTURE2D(
                        _ThicknessMap,
                        sampler_ThicknessMap,
                        input.uv
                    ).r;


                // ---------------------------------------------------
                // Main Light + Shadow
                // ---------------------------------------------------

                float4 shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );

                Light mainLight =
                    GetMainLight(shadowCoord);

                float3 L =
                    normalize(mainLight.direction);

                float3 lightColor =
                    mainLight.color *
                    mainLight.distanceAttenuation;


                // ---------------------------------------------------
                // Diffuse
                // ---------------------------------------------------

                float NdotL =
                    saturate(dot(N, L));

                float3 diffuse =
                    albedo *
                    lightColor *
                    NdotL *
                    mainLight.shadowAttenuation;


                // ---------------------------------------------------
                // Ambient / indirect
                // ---------------------------------------------------

                float3 ambient =
                    SampleSH(N) *
                    albedo;


                // ---------------------------------------------------
                // Basic Skin Specular
                // ---------------------------------------------------

                float3 H =
                    SafeNormalize(L + V);

                float NdotH =
                    saturate(dot(N, H));


                // Rough approximation converting smoothness
                // into specular exponent.
                float specPower =
                    lerp(
                        8.0,
                        256.0,
                        _Smoothness
                    );

                float spec =
                    pow(
                        NdotH,
                        specPower
                    );

                float3 specular =
                    spec *
                    lightColor *
                    _SpecularStrength *
                    mainLight.shadowAttenuation;


                // ---------------------------------------------------
                // SSS
                // ---------------------------------------------------

                float3 sss =
                    CalculateSSS(
                        N,
                        V,
                        L,
                        lightColor,
                        thickness,
                        mainLight.shadowAttenuation
                    );


                // Cho scattering mang màu albedo một phần.
                // Da đỏ vẫn giữ được texture da.
                sss *= lerp(
                    1.0.xxx,
                    albedo,
                    0.35
                );


                // ---------------------------------------------------
                // Final
                // ---------------------------------------------------

                float3 finalColor =
                    ambient +
                    diffuse +
                    specular +
                    sss;


                return half4(
                    finalColor,
                    baseTex.a * _BaseColor.a
                );
            }

            ENDHLSL
        }

        // Cho object vẫn cast shadow như URP Lit.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}