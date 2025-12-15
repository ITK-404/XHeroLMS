Shader "FX/SolarCrescent_URP_Fixed2"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,0.78,0.25,1)
        _Intensity ("Intensity", Range(0,20)) = 6.0

        _Radius ("Radius", Range(0,1)) = 0.36
        _Thickness ("Thickness", Range(0.001,0.3)) = 0.06
        _Softness ("Edge Softness", Range(0.0001,0.2)) = 0.02

        _ArcCenterDeg ("Arc Center (Deg)", Range(0,360)) = 45
        _ArcSpanDeg   ("Arc Span (Deg)", Range(1,360)) = 210
        _ArcEndSoft01 ("Arc End Soft (0..1)", Range(0.0001,0.3)) = 0.06

        _FiberCount ("Fiber Count", Range(4,160)) = 44
        _FiberWidth ("Fiber Width", Range(0.0005,0.06)) = 0.012
        _FiberStrength ("Fiber Strength", Range(0,3)) = 1.2
        _FiberFlow ("Fiber Flow Speed", Range(-10,10)) = 1.5
        _FiberJitter ("Fiber Jitter", Range(0,1)) = 0.45

        _Halo ("Halo", Range(0,1)) = 0.35
        _InnerGlow ("Inner Glow", Range(0,2)) = 0.65

        _SparkIntensity ("Spark Intensity", Range(0,6)) = 1.8
        _SparkDensity ("Spark Density", Range(0,1)) = 0.45
        _SparkSize ("Spark Size", Range(0.001,0.06)) = 0.015
        _SparkFlicker ("Spark Flicker", Range(0,40)) = 12
        _SparkSpread ("Spark Spread", Range(0.001,0.5)) = 0.22

        _NoiseScale ("Noise Scale", Range(0.1,50)) = 10
        _NoiseAmount ("Noise Amount", Range(0,0.3)) = 0.08
        _NoiseSpeed ("Noise Speed", Range(0,10)) = 1.0
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings  { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float  _Intensity;

                float _Radius, _Thickness, _Softness;
                float _ArcCenterDeg, _ArcSpanDeg, _ArcEndSoft01;

                float _FiberCount, _FiberWidth, _FiberStrength, _FiberFlow, _FiberJitter;
                float _Halo, _InnerGlow;

                float _SparkIntensity, _SparkDensity, _SparkSize, _SparkFlicker, _SparkSpread;

                float _NoiseScale, _NoiseAmount, _NoiseSpeed;
            CBUFFER_END

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f*f*(3.0 - 2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float wrap01_2pi(float a)
            {
                float twoPi = 2.0 * PI;
                a = fmod(a, twoPi);
                if (a < 0) a += twoPi;
                return a;
            }

            float angDiff(float a, float b)
            {
                float d = a - b;
                return atan2(sin(d), cos(d));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ===== declare ALL vars first (D3D-safe) =====
                float2 uv;
                float t;

                float r, ang;

                float center, spanHalf, endSoft, dAng, arcMask;

                float inner, outer, band;

                float n, wobble;

                float dr, halo, innerGlow;

                float fibers;
                float twoPi, count, id, id01, cellCenter, aErr, rnd, w, flow, onA, onB, onv, linev, radial;

                float sparks;
                float angCells, radCells;
                float2 domain, cellF, cellI, cellUV, p, d;
                float cellId, sizeRnd, s, distv, dotv, tick, activeRnd, active, radialBand;

                float basev, alpha;
                float3 col, outCol;

                // ===== compute =====
                uv = IN.uv * 2.0 - 1.0;
                t = _Time.y;

                r = length(uv);
                ang = wrap01_2pi(atan2(uv.y, uv.x));

                // Arc mask
                center = _ArcCenterDeg * (PI / 180.0);
                spanHalf = (_ArcSpanDeg * (PI / 180.0)) * 0.5;
                endSoft = max(1e-4, _ArcEndSoft01 * PI);

                dAng = abs(angDiff(ang, center));
                arcMask = smoothstep(spanHalf, spanHalf - endSoft, dAng);

                // Ring band
                inner = _Radius - _Thickness * 0.5;
                outer = _Radius + _Thickness * 0.5;

                band = smoothstep(inner - _Softness, inner + _Softness, r) *
                       (1.0 - smoothstep(outer - _Softness, outer + _Softness, r));

                n = noise2(uv * _NoiseScale + t * _NoiseSpeed);
                wobble = 1.0 + (n - 0.5) * (_NoiseAmount / max(1e-4, _Thickness));
                band *= saturate(wobble);

                // Glow
                dr = abs(r - _Radius);
                halo = exp(-dr / max(1e-4, _Softness * 2.0)) * _Halo;
                innerGlow = exp(-abs(r - (_Radius - _Thickness*0.35)) / max(1e-4, _Softness * 2.0)) * _InnerGlow;

                // Fibers
                fibers = 0.0;
                twoPi = 2.0 * PI;
                count = max(4.0, _FiberCount);

                id = floor(ang / twoPi * count);
                id01 = id / count;

                cellCenter = (id + 0.5) * (twoPi / count);
                aErr = abs(angDiff(ang, cellCenter));

                rnd = hash11(id + 17.3);
                w = lerp(_FiberWidth * 0.6, _FiberWidth * 1.4, rnd);

                flow = frac(id01 * 3.0 + t * _FiberFlow + hash11(id + 91.7) * _FiberJitter);
                onA = smoothstep(0.15, 0.0, abs(flow - 0.25));
                onB = smoothstep(0.15, 0.0, abs(flow - 0.75));
                onv = saturate(onA + onB);

                linev = smoothstep(w, 0.0, aErr);
                radial = smoothstep(_Thickness*0.9, 0.0, dr);

                fibers = linev * radial * onv;
                fibers *= _FiberStrength;

                // Sparks
                sparks = 0.0;
                angCells = 90.0;
                radCells = 12.0;

                domain.x = frac(ang / (2.0*PI) + t * 0.05);
                domain.y = saturate(((r - _Radius) / max(1e-4, _SparkSpread)) * 0.5 + 0.5);

                cellF = float2(domain.x * angCells, domain.y * radCells);
                cellI = floor(cellF);
                cellUV = frac(cellF);

                cellId = cellI.x + cellI.y * 257.0;

                p = float2(hash11(cellId + 1.2), hash11(cellId + 9.8));
                d = cellUV - p;

                sizeRnd = hash11(cellId + 33.3);
                s = lerp(_SparkSize*0.6, _SparkSize*1.6, sizeRnd);

                distv = length(d);
                dotv = smoothstep(s, s * 0.35, distv);

                tick = floor(t * _SparkFlicker);
                activeRnd = hash11(cellId + tick * 19.0);
                active = step(1.0 - _SparkDensity, activeRnd);

                radialBand = smoothstep(_SparkSpread, 0.0, abs(r - _Radius));
                sparks = dotv * active * radialBand * _SparkIntensity;

                // Combine
                basev = (band + halo + innerGlow) * arcMask;
                alpha = saturate(basev + (fibers + sparks) * arcMask);

                col = _MainColor.rgb * _Intensity;

                outCol = col * (basev + fibers * 0.9);
                outCol += col * (sparks * 1.2);

                return half4(outCol, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
