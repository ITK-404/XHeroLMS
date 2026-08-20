Shader "Custom/URP/FishBend"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _Speed("Swim Speed", Float) = 2.0
        _Frequency("Wave Frequency", Float) = 1.5
        _Amplitude("Bend Amplitude", Float) = 0.1
        
        _RandomPhase("Random Phase", Range(0, 6.28318)) = 0
        _SecondaryStrength("Secondary Wave Strength", Range(0, 1)) = 0.2
        _SecondarySpeed("Secondary Wave Speed", Float) = 0.7
        _SecondaryFrequency("Secondary Frequency", Float) = 0.8
        // Vị trí đầu cá trên trục Z.
        _BendStart("Bend Start Z", Float) = 0.0

        // Khoảng cách từ đầu đến đuôi theo trục Z.
        // Dùng giá trị âm nếu đuôi nằm về hướng Z âm.
        _BendLength("Bend Length Z", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;

            float _RandomPhase;
            float _SecondaryStrength;
            float _SecondarySpeed;
            float _SecondaryFrequency;
        
            float _Speed;
            float _Frequency;
            float _Amplitude;
            float _BendStart;
            float _BendLength;
        CBUFFER_END

        float3 BendFishPositionOS(float3 positionOS)
        {
            // Thân cá chạy dọc trục X.
            float bodyAxis = positionOS.x;

            float safeLength = abs(_BendLength) < 0.0001
                ? 0.0001
                : _BendLength;

            // 0 ở đầu cá, 1 ở đuôi cá.
            float bendMask = saturate(
                (bodyAxis - _BendStart) / safeLength
            );

            // Sóng chính tạo chuyển động bơi.
            float mainPhase =
                _Time.y * _Speed +
                bodyAxis * _Frequency +
                _RandomPhase;

            float mainWave = sin(mainPhase);

            // Sóng phụ khiến chuyển động bớt đều.
            float secondaryPhase =
                _Time.y * _SecondarySpeed +
                bodyAxis * _SecondaryFrequency +
                _RandomPhase * 1.731;

            float secondaryWave =
                sin(secondaryPhase) *
                _SecondaryStrength;

            // Biên độ thay đổi nhẹ theo thời gian.
            float amplitudeVariation =
                1.0 +
                sin(
                    _Time.y * (_SecondarySpeed * 0.45) +
                    _RandomPhase * 2.137
                ) * (_SecondaryStrength * 0.35);

            float offset =
                (mainWave + secondaryWave) *
                _Amplitude *
                amplitudeVariation *
                bendMask;

            // Giữ Position gốc và chỉ uốn theo trục Z.
            positionOS.z += offset;

            return positionOS;
        }

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                half3 normalWS     : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 bentPositionOS =
                    BendFishPositionOS(input.positionOS.xyz);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(bentPositionOS);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseMap = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    input.uv
                );

                half4 albedo = baseMap * _BaseColor;
                half3 normalWS = normalize(input.normalWS);

                float4 shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);

                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(
                    dot(normalWS, mainLight.direction)
                );

                half3 directLighting =
                    mainLight.color *
                    NdotL *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 ambientLighting = SampleSH(normalWS);

                half3 finalColor =
                    albedo.rgb *
                    (directLighting + ambientLighting);

                return half4(finalColor, albedo.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 bentPositionOS =
                    BendFishPositionOS(input.positionOS.xyz);

                float3 positionWS =
                    TransformObjectToWorld(bentPositionOS);

                float3 normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                positionWS = ApplyShadowBias(
                    positionWS,
                    normalWS,
                    _MainLightPosition.xyz
                );

                output.positionHCS =
                    TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                    output.positionHCS.z = min(
                        output.positionHCS.z,
                        UNITY_NEAR_CLIP_VALUE *
                        output.positionHCS.w
                    );
                #else
                    output.positionHCS.z = max(
                        output.positionHCS.z,
                        UNITY_NEAR_CLIP_VALUE *
                        output.positionHCS.w
                    );
                #endif

                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 bentPositionOS =
                    BendFishPositionOS(input.positionOS.xyz);

                output.positionHCS =
                    TransformObjectToHClip(bentPositionOS);

                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}