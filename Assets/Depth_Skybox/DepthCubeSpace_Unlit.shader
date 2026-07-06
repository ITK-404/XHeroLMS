
Shader "Custom/DepthSkybox/DepthCubeSpace_Unlit"
{
    Properties
    {
        [NoScaleOffset]_ColorCube("Color Cubemap", CUBE) = "" {}
        _CaptureOrigin("Capture Origin WS", Vector) = (0, 0, 0, 0)
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _Exposure("Exposure", Range(0, 4)) = 1
        _Contrast("Contrast", Range(0, 2)) = 1
        _Saturation("Saturation", Range(0, 2)) = 1
        [Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DepthCubeSpaceUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_ColorCube);
            SAMPLER(sampler_ColorCube);

            CBUFFER_START(UnityPerMaterial)
                float4 _CaptureOrigin;
                float4 _Tint;
                float _Exposure;
                float _Contrast;
                float _Saturation;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float3 ApplyColorGrade(float3 color)
            {
                color = max(color * max(_Exposure, 0.0), 0.0);
                color = (color - 0.5) * _Contrast + 0.5;

                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luminance.xxx, color, _Saturation);

                return saturate(color) * _Tint.rgb;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.positionWS - _CaptureOrigin.xyz);
                float4 col = SAMPLE_TEXTURECUBE(_ColorCube, sampler_ColorCube, dir);
                col.rgb = ApplyColorGrade(col.rgb);
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}