Shader "Custom/DepthSkybox/URP_Parallax"
{
    Properties
    {
        [Header(Depth Skybox Assets)]
        [NoScaleOffset]_ColorCube("Color Cubemap", CUBE) = "" {}
        [NoScaleOffset]_DepthCube("Depth Cubemap", CUBE) = "" {}

        [NoScaleOffset]_ColorPanorama("Color Panorama", 2D) = "black" {}
        [NoScaleOffset]_DepthPanorama("Depth Panorama", 2D) = "white" {}

        [Header(Source Mode)]
        [Toggle]_UseCubemap("Use Cubemap Instead Of Panorama", Float) = 0
        [Toggle]_UseScreenRay("Use Screen Ray Like HDRI Skybox", Float) = 1
        _PanoramaYawOffset("Panorama Yaw Offset Degrees", Range(-180, 180)) = 0

        [Header(Capture Settings)]
        _CaptureOrigin("Capture Origin WS", Vector) = (0, 0, 0, 0)
        _DepthMaxMeters("Depth Max Meters", Float) = 300
        _DepthBias("Depth Bias", Float) = 0

        [Header(Parallax)]
        _ParallaxStrength("Parallax Strength", Range(0, 1)) = 1
        _MotionParallaxScale("Motion Parallax Scale", Range(0, 4)) = 1.5
        _MaxParallaxOffset("Max Parallax Offset", Float) = 25
        _SkyDepthFadeStart("Sky/Far Depth Fade Start", Range(0.5, 1)) = 0.985

        [Header(Depth Filtering)]
        _DepthMipBias("Depth Mip Bias", Range(0, 4)) = 0
        _DepthSmoothness("Depth Smoothness", Range(0, 1)) = 0.2
        _DepthEdgeFadeStart("Depth Edge Fade Start", Range(0, 0.25)) = 0.025
        _DepthEdgeFadeEnd("Depth Edge Fade End", Range(0, 0.5)) = 0.12

        [Header(Display)]
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _Exposure("Exposure", Range(0, 4)) = 1
        [Toggle]_ToneMap("Filmic Tone Map", Float) = 0
        _Contrast("Contrast", Range(0, 2)) = 1
        _Saturation("Saturation", Range(0, 2)) = 1
        [Toggle]_DebugDepth("Debug Depth", Float) = 0
        [Toggle]_InvertDirection("Invert Direction", Float) = 0
        [Toggle]_FlipScreenY("Flip Screen Ray Y", Float) = 0
        [Toggle]_FlipPanoramaX("Flip Panorama X", Float) = 0
        [Toggle]_FlipPanoramaY("Flip Panorama Y", Float) = 0

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DepthSkyboxForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define DEPTH_SKYBOX_PI 3.14159265359

            TEXTURECUBE(_ColorCube);
            SAMPLER(sampler_ColorCube);

            TEXTURECUBE(_DepthCube);
            SAMPLER(sampler_DepthCube);

            TEXTURE2D(_ColorPanorama);
            SAMPLER(sampler_ColorPanorama);

            TEXTURE2D(_DepthPanorama);
            SAMPLER(sampler_DepthPanorama);
            float4 _DepthPanorama_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _CaptureOrigin;
                float4 _Tint;

                float _UseCubemap;
                float _UseScreenRay;
                float _PanoramaYawOffset;
                float _DepthMaxMeters;
                float _DepthBias;
                float _ParallaxStrength;
                float _MotionParallaxScale;
                float _MaxParallaxOffset;
                float _SkyDepthFadeStart;
                float _DepthMipBias;
                float _DepthSmoothness;
                float _DepthEdgeFadeStart;
                float _DepthEdgeFadeEnd;

                float _Exposure;
                float _ToneMap;
                float _Contrast;
                float _Saturation;
                float _DebugDepth;
                float _InvertDirection;
                float _FlipScreenY;
                float _FlipPanoramaX;
                float _FlipPanoramaY;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);

                return OUT;
            }

            float2 DirToEquirectUV(float3 dir)
            {
                dir = normalize(dir);

                float yawRadians = radians(_PanoramaYawOffset);
                float s;
                float c;
                sincos(yawRadians, s, c);
                dir = float3(dir.x * c - dir.z * s, dir.y, dir.x * s + dir.z * c);

                float u = atan2(dir.x, dir.z) / (2.0 * DEPTH_SKYBOX_PI) + 0.5;
                float v = 0.5 - asin(clamp(dir.y, -1.0, 1.0)) / DEPTH_SKYBOX_PI;

                if (_FlipPanoramaX > 0.5)
                    u = 1.0 - u;

                if (_FlipPanoramaY > 0.5)
                    v = 1.0 - v;

                return float2(frac(u), saturate(v));
            }

            float2 WrapPanoramaUV(float2 uv)
            {
                uv.x = frac(uv.x);
                uv.y = clamp(uv.y, 0.001, 0.999);
                return uv;
            }

            float SampleDepthPanorama(float2 uv, float lod)
            {
                uv = WrapPanoramaUV(uv);
                float center = SAMPLE_TEXTURE2D_LOD(_DepthPanorama, sampler_DepthPanorama, uv, lod).r;

                if (_DepthSmoothness <= 0.001)
                    return center;

                float2 texel = _DepthPanorama_TexelSize.xy * lerp(1.0, 3.0, saturate(_DepthSmoothness));
                float d = center;
                d += SAMPLE_TEXTURE2D_LOD(_DepthPanorama, sampler_DepthPanorama, WrapPanoramaUV(uv + float2(texel.x, 0.0)), lod).r;
                d += SAMPLE_TEXTURE2D_LOD(_DepthPanorama, sampler_DepthPanorama, WrapPanoramaUV(uv - float2(texel.x, 0.0)), lod).r;
                d += SAMPLE_TEXTURE2D_LOD(_DepthPanorama, sampler_DepthPanorama, WrapPanoramaUV(uv + float2(0.0, texel.y)), lod).r;
                d += SAMPLE_TEXTURE2D_LOD(_DepthPanorama, sampler_DepthPanorama, WrapPanoramaUV(uv - float2(0.0, texel.y)), lod).r;
                d *= 0.2;

                return lerp(center, d, saturate(_DepthSmoothness));
            }

            float SampleDepth01(float3 dir)
            {
                dir = normalize(dir);
                float lod = max(_DepthMipBias, 0.0);
                float d;

                if (_UseCubemap > 0.5)
                {
                    d = SAMPLE_TEXTURECUBE_LOD(_DepthCube, sampler_DepthCube, dir, lod).r;
                }
                else
                {
                    d = SampleDepthPanorama(DirToEquirectUV(dir), lod);
                }

                return saturate(d);
            }

            float Depth01ToMeters(float depth01)
            {
                return max(depth01 * max(_DepthMaxMeters, 0.01) + _DepthBias, 0.05);
            }

            float4 SampleColor(float3 dir)
            {
                dir = normalize(dir);

                if (_UseCubemap > 0.5)
                    return SAMPLE_TEXTURECUBE_LOD(_ColorCube, sampler_ColorCube, dir, 0);

                return SAMPLE_TEXTURE2D(_ColorPanorama, sampler_ColorPanorama, DirToEquirectUV(dir));
            }

            float3 ApplyColorGrade(float3 color)
            {
                color = max(color * max(_Exposure, 0.0), 0.0);

                if (_ToneMap > 0.5)
                {
                    color = (color * (2.51 * color + 0.03)) / (color * (2.43 * color + 0.59) + 0.14);
                    color = saturate(color);
                }

                color = (color - 0.5) * _Contrast + 0.5;

                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luminance.xxx, color, _Saturation);

                return saturate(color);
            }

            float3 ScreenRayWS(float4 positionCS)
            {
                float2 uv = GetNormalizedScreenSpaceUV(positionCS);

                if (_FlipScreenY > 0.5)
                    uv.y = 1.0 - uv.y;

                #if UNITY_REVERSED_Z
                    real depth = 0.0;
                #else
                    real depth = 1.0;
                #endif

                float3 worldOnFarPlane = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                return normalize(worldOnFarPlane - GetCameraPositionWS());
            }

            float3 ClampCameraForParallax(float3 cameraWS, float3 captureOriginWS)
            {
                float3 offset = cameraWS - captureOriginWS;
                float offsetLength = length(offset);

                if (_MaxParallaxOffset > 0.0001 && offsetLength > _MaxParallaxOffset)
                    cameraWS = captureOriginWS + offset / max(offsetLength, 0.0001) * _MaxParallaxOffset;

                return cameraWS;
            }

            float3 ComputeParallaxDirection(float3 rayWS)
            {
                rayWS = normalize(rayWS);

                float3 captureOriginWS = _CaptureOrigin.xyz;
                float3 rawCameraWS = GetCameraPositionWS();
                rawCameraWS = captureOriginWS + (rawCameraWS - captureOriginWS) * max(_MotionParallaxScale, 0.0);

                float3 cameraWS = ClampCameraForParallax(rawCameraWS, captureOriginWS);
                float3 cameraOffset = cameraWS - captureOriginWS;

                float3 sampleDir = rayWS;
                float depth01 = SampleDepth01(sampleDir);
                float depthMeters = Depth01ToMeters(depth01);
                float t = max(depthMeters - dot(cameraOffset, rayWS), 0.05);

                [unroll]
                for (int iter = 0; iter < 3; iter++)
                {
                    float3 rayPointWS = cameraWS + rayWS * t;
                    sampleDir = normalize(rayPointWS - captureOriginWS);

                    depth01 = SampleDepth01(sampleDir);
                    depthMeters = Depth01ToMeters(depth01);

                    float3 surfacePointWS = captureOriginWS + sampleDir * depthMeters;
                    t = max(dot(surfacePointWS - cameraWS, rayWS), 0.05);
                }

                float farFadeStart = min(saturate(_SkyDepthFadeStart), 0.999);
                float farFade = 1.0 - smoothstep(farFadeStart, 1.0, depth01);
                float depthEdge = length(float2(ddx(depth01), ddy(depth01)));
                float edgeFade = 1.0 - smoothstep(_DepthEdgeFadeStart, max(_DepthEdgeFadeEnd, _DepthEdgeFadeStart + 0.0001), depthEdge);
                float strength = saturate(_ParallaxStrength) * farFade * edgeFade;

                return normalize(lerp(rayWS, sampleDir, strength));
            }

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 cameraWS = GetCameraPositionWS();
                float3 rayWS = (_UseScreenRay > 0.5)
                    ? ScreenRayWS(IN.positionCS)
                    : normalize(IN.positionWS - cameraWS);

                if (_InvertDirection > 0.5)
                    rayWS = -rayWS;

                float3 sampleDir = ComputeParallaxDirection(rayWS);

                if (_DebugDepth > 0.5)
                {
                    float d = SampleDepth01(sampleDir);
                    return float4(d, d, d, 1.0);
                }

                float4 col = SampleColor(sampleDir);
                col.rgb = ApplyColorGrade(col.rgb) * _Tint.rgb;
                col.a = 1.0;

                return col;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
