Shader "UI/TutorialFocusURP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Overlay Color", Color) = (0, 0, 0, 0.75)

        // x = minX
        // y = minY
        // z = maxX
        // w = maxY
        _FocusRect ("Focus Rect", Vector) = (0.4, 0.4, 0.6, 0.6)

        _Feather ("Feather", Range(0, 0.1)) = 0.01

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Tutorial Focus Overlay"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPosition : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _FocusRect;
                float _Feather;
                float4 _ClipRect;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.screenPosition = ComputeScreenPos(positionCS);

                output.worldPosition = input.positionOS;
                output.uv = input.uv;
                output.color = input.color * _Color;

                return output;
            }

            float GetUIClipFactor(float2 position, float4 clipRect)
            {
                float2 insideMin = step(clipRect.xy, position);
                float2 insideMax = step(position, clipRect.zw);

                return insideMin.x
                     * insideMin.y
                     * insideMax.x
                     * insideMax.y;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV =
                    input.screenPosition.xy /
                    input.screenPosition.w;

                float2 rectMin = _FocusRect.xy;
                float2 rectMax = _FocusRect.zw;

                float2 rectCenter =
                    (rectMin + rectMax) * 0.5;

                float2 rectHalfSize =
                    (rectMax - rectMin) * 0.5;

                // Khoảng cách từ pixel hiện tại đến hình chữ nhật.
                float2 distanceToEdge =
                    abs(screenUV - rectCenter) -
                    rectHalfSize;

                float signedDistance =
                    max(distanceToEdge.x, distanceToEdge.y);

                // Trong vùng focus = 1
                // Ngoài vùng focus = 0
                float focusMask =
                    1.0 - smoothstep(
                        -_Feather,
                        _Feather,
                        signedDistance
                    );

                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                half4 finalColor =
                    textureColor * input.color;

                // Khoét vùng focus thành trong suốt.
                finalColor.a *= 1.0 - focusMask;

                #ifdef UNITY_UI_CLIP_RECT
                    finalColor.a *= GetUIClipFactor(
                        input.worldPosition.xy,
                        _ClipRect
                    );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }

            ENDHLSL
        }
    }
}