Shader "Custom/VFX/GoldThreadTube"
{
    Properties
    {
        _BaseColor ("Gold Color", Color) = (1.0, 0.58, 0.12, 1)
        _HotColor ("Hot Color", Color) = (1.0, 0.92, 0.45, 1)
        _StarColor ("Star Color", Color) = (1.0, 0.98, 0.76, 1)

        _Intensity ("Intensity", Float) = 7.0
        _Alpha ("Alpha", Range(0, 1)) = 0.75

        _BandCenter ("Band Center", Range(0, 1)) = 0.5
        _BandWidth ("Band Width", Range(0.01, 1)) = 0.55
        _BandSoftness ("Band Softness", Range(0.001, 0.5)) = 0.18

        _HorizontalLineCount ("Horizontal Ring Count", Float) = 7
        _HorizontalSharpness ("Horizontal Sharpness", Range(1, 240)) = 130
        _HorizontalPower ("Horizontal Power", Range(0, 5)) = 0.55

        _MainArcCount ("Main Point Count", Float) = 9
        _MainArcSharpness ("Point U Sharpness", Range(1, 220)) = 95
        _MainArcPower ("Point Power", Range(0, 8)) = 4.2

        _SecondaryArcCount ("Secondary Point Count", Float) = 15
        _SecondaryArcSharpness ("Secondary Point U Sharpness", Range(1, 220)) = 105
        _SecondaryArcPower ("Secondary Point Power", Range(0, 5)) = 1.6

        _ThreadCount ("Fine Vertical Thread Count", Float) = 80
        _ThreadSharpness ("Fine Vertical Thread Sharpness", Range(1, 240)) = 145
        _ThreadPower ("Fine Vertical Thread Power", Range(0, 3)) = 0.08

        _SparkCount ("Spark Count", Float) = 42
        _SparkSharpness ("Spark Sharpness", Range(1, 320)) = 210
        _SparkPower ("Spark Power", Range(0, 8)) = 2.4
        _SparkVerticalSize ("Spark Vertical Size", Range(0.001, 0.2)) = 0.035

        _RotateSpeed ("Main Rotate Speed", Float) = 0.22
        _CounterRotateSpeed ("Counter Rotate Speed", Float) = -0.11
        _RiseSpeed ("Vertical Flow Speed", Float) = 0.08
        _PulseSpeed ("Pulse Speed", Float) = 1.2

        _SoftGlowPower ("Soft Glow Power", Range(0, 5)) = 0.28
        _EdgeFade ("Vertical Edge Fade", Range(0, 0.5)) = 0.12

        _NoiseScale ("Noise Scale", Float) = 22
        _NoiseSpeed ("Noise Speed", Float) = 0.35
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.2

        _CapCull ("Cull Caps", Range(0, 1)) = 0.5

        _PointHorizontalStretch ("Point Horizontal Stretch", Range(0, 5)) = 1.8
        _PointVerticalSharpness ("Point Vertical Sharpness", Range(1, 8)) = 3.2
        _BaseLineDim ("Base Line Dim", Range(0, 1)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "GoldThreadTube_PointGlow"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalOS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _HotColor;
                float4 _StarColor;

                float _Intensity;
                float _Alpha;

                float _BandCenter;
                float _BandWidth;
                float _BandSoftness;

                float _HorizontalLineCount;
                float _HorizontalSharpness;
                float _HorizontalPower;

                float _MainArcCount;
                float _MainArcSharpness;
                float _MainArcPower;

                float _SecondaryArcCount;
                float _SecondaryArcSharpness;
                float _SecondaryArcPower;

                float _ThreadCount;
                float _ThreadSharpness;
                float _ThreadPower;

                float _SparkCount;
                float _SparkSharpness;
                float _SparkPower;
                float _SparkVerticalSize;

                float _RotateSpeed;
                float _CounterRotateSpeed;
                float _RiseSpeed;
                float _PulseSpeed;

                float _SoftGlowPower;
                float _EdgeFade;

                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseStrength;

                float _CapCull;

                float _PointHorizontalStretch;
                float _PointVerticalSharpness;
                float _BaseLineDim;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x)
                    + (c - a) * u.y * (1.0 - u.x)
                    + (d - b) * u.x * u.y;
            }

            float softBand(float v, float center, float width, float softness)
            {
                float halfWidth = width * 0.5;
                float d = abs(v - center);

                float inner = halfWidth;
                float outer = halfWidth + softness;

                return 1.0 - smoothstep(inner, outer, d);
            }

            float thinLine(float phase, float sharpness)
            {
                float w = sin(phase) * 0.5 + 0.5;
                return pow(w, sharpness);
            }

            float softLine(float phase, float sharpness)
            {
                float w = sin(phase) * 0.5 + 0.5;
                return pow(w, sharpness);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalOS = IN.normalOS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // Chỉ dùng mặt hông cylinder/tube, bỏ cap trên/dưới.
                clip(_CapCull - abs(IN.normalOS.y));

                float u = frac(IN.uv.x);
                float v = frac(IN.uv.y);

                float bottomFade = smoothstep(0.0, _EdgeFade, v);
                float topFade = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, v);

                float band = softBand(v, _BandCenter, _BandWidth, _BandSoftness);
                float mask = bottomFade * topFade * band;

                float n = noise(float2(u * _NoiseScale, v * _NoiseScale - time * _NoiseSpeed));
                float livingNoise = lerp(1.0, n, _NoiseStrength);

                float pulse = 0.86 + 0.14 * sin(time * _PulseSpeed * 6.28318);

                // =========================
                // Vòng ngang: chỉ giữ làm nền mờ
                // =========================
                float hPhase = v * _HorizontalLineCount * 6.28318;
                hPhase -= time * _RiseSpeed * 6.28318;
                hPhase += sin(u * 6.28318 * 2.0 + time * 0.7) * 0.18;

                float horizontalCore = thinLine(hPhase, _HorizontalSharpness);
                float horizontalSoft = softLine(hPhase, 18.0);

                float baseHorizontalLine = horizontalCore * _HorizontalPower * _BaseLineDim;
                float baseHorizontalGlow = horizontalSoft * _SoftGlowPower * _BaseLineDim;

                // Dùng bản mềm hơn để tạo vùng giao điểm sáng.
                float pointVerticalMask = pow(saturate(horizontalSoft), _PointVerticalSharpness);

                // =========================
                // Point theo chiều ngang
                // Không cộng arc trực tiếp vào light nữa.
                // Arc chỉ dùng làm mask để tạo chấm sáng tại giao với vòng ngang.
                // =========================
                float arcPhase = u * _MainArcCount * 6.28318;
                arcPhase += time * _RotateSpeed * 6.28318;
                arcPhase += sin(v * 6.28318 * 2.0) * 0.7;
                arcPhase += n * 1.25;

                float mainPointU = thinLine(arcPhase, _MainArcSharpness);

                // kéo point theo ngang nhẹ, để nhìn như lóa ngang chứ không phải cục tròn quá gắt
                float mainPointUWide = thinLine(arcPhase, max(4.0, _MainArcSharpness / max(_PointHorizontalStretch, 0.001)));

                float mainPoints = pointVerticalMask * mainPointU * _MainArcPower;
                float mainPointGlow = pointVerticalMask * mainPointUWide * _MainArcPower * 0.45;

                // =========================
                // Point phụ chạy ngược chiều
                // =========================
                float arcPhase2 = u * _SecondaryArcCount * 6.28318;
                arcPhase2 += time * _CounterRotateSpeed * 6.28318;
                arcPhase2 -= sin(v * 6.28318 * 3.0) * 0.45;
                arcPhase2 += 1.73;

                float secondaryPointU = thinLine(arcPhase2, _SecondaryArcSharpness);
                float secondaryPointUWide = thinLine(arcPhase2, max(4.0, _SecondaryArcSharpness / max(_PointHorizontalStretch, 0.001)));

                float secondaryPoints = pointVerticalMask * secondaryPointU * _SecondaryArcPower;
                float secondaryPointGlow = pointVerticalMask * secondaryPointUWide * _SecondaryArcPower * 0.35;

                // =========================
                // Sợi dọc cực nhẹ, tránh nhìn thành mì
                // =========================
                float threadPhase = u * _ThreadCount * 6.28318;
                threadPhase += v * 0.6 * 6.28318;
                threadPhase += time * _RotateSpeed * 0.55 * 6.28318;

                float verticalThreads = thinLine(threadPhase, _ThreadSharpness) * _ThreadPower;
                verticalThreads *= pointVerticalMask * 0.45;

                // =========================
                // Spark/star chỉ xuất hiện gần point
                // =========================
                float sparkMove = u * _SparkCount + time * _RotateSpeed * 2.4;
                float sparkCell = floor(sparkMove);

                float randV = hash21(float2(sparkCell, 17.31));
                float randPower = hash21(float2(sparkCell, 91.22));

                float sparkVTarget = _BandCenter + lerp(-_BandWidth * 0.28, _BandWidth * 0.28, randV);

                float sparkUWave = sin(sparkMove * 6.28318) * 0.5 + 0.5;
                float sparkU = pow(sparkUWave, _SparkSharpness);

                float sparkV = 1.0 - saturate(abs(v - sparkVTarget) / max(_SparkVerticalSize, 0.0001));
                sparkV = pow(sparkV, 3.0);

                float pointOnlyMask = saturate(mainPoints + secondaryPoints + mainPointGlow + secondaryPointGlow);

                float sparks = sparkU * sparkV * _SparkPower;
                sparks *= lerp(0.35, 1.0, randPower);
                sparks *= saturate(pointOnlyMask * 2.0 + pointVerticalMask * 0.25);

                // Lóa ngang nhỏ tại point.
                float horizontalGlint = pointVerticalMask;
                horizontalGlint *= saturate(mainPointUWide + secondaryPointUWide);
                horizontalGlint *= 0.75;

                // =========================
                // Tổng ánh sáng
                // =========================
                float light = 0.0;

                // Nền line rất mờ.
                light += baseHorizontalLine;
                light += baseHorizontalGlow;

                // Chính: chỉ sáng ở các point.
                light += mainPoints;
                light += mainPointGlow;

                light += secondaryPoints;
                light += secondaryPointGlow;

                // Texture phụ rất nhẹ.
                light += verticalThreads;

                // Lóe tại point.
                light += sparks;
                light += horizontalGlint;

                light *= mask;
                light *= livingNoise;
                light *= pulse;

                float alpha = saturate(light) * _Alpha;

                float starMix = saturate(sparks + horizontalGlint + mainPoints * 0.65);
                float hotMix = saturate(mainPoints + secondaryPoints + mainPointGlow * 0.8);

                float3 col = lerp(_BaseColor.rgb, _HotColor.rgb, hotMix);
                col = lerp(col, _StarColor.rgb, starMix);

                col *= light * _Intensity;

                return half4(col, alpha);
            }

            ENDHLSL
        }
    }
}