Shader "FX/GoldenRingAdditive_URP"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,0.75,0.2,1)
        _Intensity ("Intensity", Range(0,20)) = 6.0

        _Radius ("Ring Radius", Range(0,1)) = 0.28
        _Thickness ("Base Thickness", Range(0.001,0.3)) = 0.05

        // TÁCH RA:
        _Softness ("Edge Softness (Ring Edge)", Range(0.0001,0.2)) = 0.018
        _GlowSoftness ("Glow Softness (Halo Width)", Range(0.0001,0.25)) = 0.036
        _GlowIntensity ("Glow Intensity", Range(0,3)) = 0.6

        // ===== Taichi Comet =====
        _AngularSpeed ("Angular Speed (rad/sec)", Range(-10,10)) = 2.2
        _TailLength ("Tail Length (radians)", Range(0.1, 6.283)) = 2.2
        _TailSoft ("Tail Softness", Range(0.001, 1.0)) = 0.22

        // UPDATED: _HeadSize là chiều dài theo vòng (ngang), thêm _HeadHeight là chiều dọc (radial)
        _HeadSize   ("Head Length (along arc)", Range(0.01, 1.0)) = 0.11
        _HeadHeight ("Head Height (radial)",    Range(0.1, 3.0))  = 1.15

        _HeadBoost ("Head Boost", Range(0,8)) = 2.5
        _HeadRound ("Head Roundness", Range(0.2, 4.0)) = 1.6
        _HeadSoft  ("Head Softness",  Range(0.001, 0.6)) = 0.10
        _HeadCap   ("Head Cap Boost", Range(0.0, 3.0)) = 1.0

        _TailThicknessScale ("Tail Thickness Scale", Range(0.02, 1.0)) = 0.25
        _TailTaper ("Tail Taper (Comet Shape)", Range(0.1, 3.0)) = 1.6
        _TailPower ("Tail Fade Power", Range(0.5, 8.0)) = 2.4

        // ===== Base Noise (ring wobble) =====
        _NoiseScale ("Noise Scale", Range(0.1,50)) = 10
        _NoiseAmount ("Noise Amount", Range(0,0.25)) = 0.06
        _NoiseSpeed ("Noise Speed", Range(0,10)) = 1.2

        // ===== Spark Particles (DOTS) =====
        _SparkIntensity ("Spark Intensity", Range(0,8)) = 2.0
        _SparkSize ("Spark Size", Range(0.001, 0.08)) = 0.018
        _SparkSizeRand ("Spark Size Random", Range(0,1)) = 0.6
        _SparkRadialSpread ("Spark Radial Spread", Range(0.001, 0.35)) = 0.10

        _SparkAngCells ("Spark Angular Cells", Range(8, 256)) = 90
        _SparkRadCells ("Spark Radial Cells", Range(2, 64)) = 14
        _SparkDensity ("Spark Density", Range(0,1)) = 0.55

        _SparkFlicker ("Spark Flicker Speed", Range(0,40)) = 12
        _SparkScroll ("Spark Scroll Speed", Range(0,6)) = 1.4
        _SparkTailBias ("Spark Tail Bias", Range(0,2)) = 1.2

        _GlintIntensity ("Glint Intensity", Range(0,6)) = 1.0
        _GlintChance ("Glint Chance", Range(0,1)) = 0.12
        _GlintSharpness ("Glint Sharpness", Range(2,40)) = 14

        _SparkleFreq ("Sparkle Freq", Range(0,50)) = 18
        _SparkleAmount ("Sparkle Amount", Range(0,1)) = 0.18
        _SparkleSpeed ("Sparkle Speed", Range(0,10)) = 2.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

CBUFFER_START(UnityPerMaterial)
    float4 _MainColor;
    float  _Intensity;

    float _Radius, _Thickness;

    float _Softness;       // ring edge softness
    float _GlowSoftness;   // halo width
    float _GlowIntensity;  // halo strength

    float _AngularSpeed, _TailLength, _TailSoft;

    float _HeadSize, _HeadHeight, _HeadBoost;
    float _HeadRound, _HeadSoft, _HeadCap;

    // GỘP 1 LẦN DUY NHẤT
    float _TailThicknessScale, _TailTaper, _TailPower;

    float _NoiseScale, _NoiseAmount, _NoiseSpeed;

    float _SparkIntensity, _SparkSize, _SparkSizeRand, _SparkRadialSpread;
    float _SparkAngCells, _SparkRadCells, _SparkDensity;
    float _SparkFlicker, _SparkScroll, _SparkTailBias;

    float _GlintIntensity, _GlintChance, _GlintSharpness;

    float _SparkleFreq, _SparkleAmount, _SparkleSpeed;
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
                return atan2(sin(d), cos(d)); // [-pi..pi]
            }

            float behindDist(float ang01, float head01, float dirSign)
            {
                float twoPi = 2.0 * PI;

                float d = (head01 - ang01); // CCW behind
                if (d < 0) d += twoPi;

                if (dirSign < 0)
                {
                    d = (ang01 - head01); // CW behind
                    if (d < 0) d += twoPi;
                }
                return d;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float glintCross(float2 d, float sharpness)
            {
                float ax = pow(saturate(1.0 - abs(d.x)), sharpness);
                float ay = pow(saturate(1.0 - abs(d.y)), sharpness);
                return ax + ay;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                float t = _Time.y;

                float r = length(uv);
                float ang = atan2(uv.y, uv.x);
                float ang01 = wrap01_2pi(ang);

                float head0 = wrap01_2pi(t * _AngularSpeed);
                float head1 = wrap01_2pi(head0 + PI);
                float dirSign = (_AngularSpeed >= 0) ? 1.0 : -1.0;

                float d0 = behindDist(ang01, head0, dirSign);
                float d1 = behindDist(ang01, head1, dirSign);

                float tail0 = smoothstep(_TailLength, _TailLength - _TailSoft, d0);
                float tail1 = smoothstep(_TailLength, _TailLength - _TailSoft, d1);
                float tailMask = max(tail0, tail1);

                float diff0 = angDiff(ang01, head0);
                float diff1 = angDiff(ang01, head1);

                // Head window theo góc (giữ để giới hạn vùng đầu theo chiều ngang/arc)
                float headWin0 = smoothstep(_HeadSize, _HeadSize * (1.0 - _HeadSoft), abs(diff0));
                float headWin1 = smoothstep(_HeadSize, _HeadSize * (1.0 - _HeadSoft), abs(diff1));
                float headWin = max(headWin0, headWin1);

                // chọn head gần nhất để tính arc
                float use0 = step(abs(diff0), abs(diff1));
                float diff = lerp(diff1, diff0, use0);

                float dr = (r - _Radius);
                float arc = abs(diff) * _Radius;

                // Tail fade trước (để tính localThickness trước khi tính headBlob)
                float dNear = min(d0, d1);
float tail01 = saturate(1.0 - dNear / max(1e-4, _TailLength));
float tailFade = pow(tail01, _TailPower);

// ===== NEW: taper đuôi theo kiểu sao chổi =====
// tail01 = 0 ở cuối đuôi, =1 ở đầu
float tailTaper = pow(tail01, _TailTaper);

// độ dày:
// - đuôi: mỏng hơn nhiều
// - đầu: dày tối đa
float localThickness = _Thickness * lerp(_TailThicknessScale, 1.0, tailTaper);


                // ========= UPDATED HEAD: _HeadHeight điều khiển theo chiều dọc (radial) =========
                float headArc = max(1e-4, _HeadSize * _Radius);                      // ngang (along arc)
                float headRad = max(1e-4, localThickness * 0.5 * _HeadHeight);       // dọc (radial)

                float2 q = float2(arc / headArc, dr / headRad);
                float dBall = length(q);

                float headCore = smoothstep(1.0, 0.0, dBall);
                headCore = pow(saturate(headCore), _HeadRound);
                float headBlob = headCore * headWin;

                // phình thân thêm nhẹ ở vùng đầu (giờ dựa vào headBlob đã tính đúng trục dọc)
                localThickness = lerp(localThickness, _Thickness * 1.18, headBlob);

                float inner = _Radius - localThickness * 0.5;
                float outer = _Radius + localThickness * 0.5;

                // ======= RING EDGE dùng _Softness (mềm mép) =======
                float edgeSoft = max(1e-5, _Softness);
                float ring = smoothstep(inner - edgeSoft, inner + edgeSoft, r) *
                             (1.0 - smoothstep(outer - edgeSoft, outer + edgeSoft, r));

                // noise
                float2 nUV = uv * _NoiseScale + t * _NoiseSpeed;
                float n = noise2(nUV);
                ring *= saturate(1.0 + (n - 0.5) * (_NoiseAmount / max(1e-4, _Thickness)));

                // ======= GLOW/HALO dùng _GlowSoftness (tách riêng) =======
                float glowSoft = max(1e-5, _GlowSoftness);
                float glow = exp(-abs(r - _Radius) / glowSoft) * _GlowIntensity;

                float baseMask = (tailMask * tailFade + headBlob);
                float alphaBase = (ring + glow) * baseMask;

                // ===================== Sparks (giữ nguyên) =====================
                float radialBand = smoothstep(_SparkRadialSpread, 0.0, abs(dr));

                float uTail = saturate(dNear / max(1e-4, _TailLength));
                float sparkBias = pow(saturate(1.0 - uTail), _SparkTailBias);
                float uScroll = uTail + t * _SparkScroll * 0.15;

                float angCells = max(8.0, _SparkAngCells);
                float radCells = max(2.0, _SparkRadCells);

                float2 domain;
                domain.x = frac(uScroll);
                domain.y = saturate((dr / max(1e-4, _SparkRadialSpread)) * 0.5 + 0.5);

                float2 cellF = float2(domain.x * angCells, domain.y * radCells);
                float2 cellI = floor(cellF);
                float2 cellUV = frac(cellF);

                float cellId = cellI.x + cellI.y * 257.0;
                float2 rnd2 = float2(hash11(cellId + 1.23), hash11(cellId + 9.87));

                float2 p = rnd2;
                float2 dd = cellUV - p;

                float sRnd = hash11(cellId + 33.3);
                float dotSize = lerp(_SparkSize, _SparkSize * (1.0 + _SparkSizeRand * 2.0), sRnd);

                float dist = length(dd);
                float dot = smoothstep(dotSize, dotSize * 0.35, dist);

                float tick = floor(t * _SparkFlicker);
                float flick = hash11(cellId + tick * 17.0);
                float active = step(1.0 - _SparkDensity, flick);

                float sparks = dot * active * radialBand * tailMask * sparkBias;

                float glintOn = step(1.0 - _GlintChance, hash11(cellId + 88.8));
                float glint = glintCross(dd / max(1e-4, dotSize), _GlintSharpness) * glintOn;

                float sparkleWave = sin(ang * _SparkleFreq + t * _SparkleSpeed);
                float sparkle = smoothstep(0.72, 1.0, sparkleWave) * _SparkleAmount;

                float3 col = _MainColor.rgb * _Intensity;
                col *= (1.0 + sparkle * ring * 1.3);

                // head highlight (tròn theo chiều dọc)
                col *= (1.0 + headBlob * (_HeadBoost + _HeadCap));

                float alpha = alphaBase;
                alpha += sparks * (ring + glow) * 0.9 * _SparkIntensity;
                alpha += glint  * sparks * (ring + glow) * 0.75 * _GlintIntensity;
                alpha = saturate(alpha);

                float3 outCol = col * alpha;
                outCol += col * (sparks * 0.9 * _SparkIntensity);
                outCol += col * (glint  * sparks * 0.6 * _GlintIntensity);

                return half4(outCol, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
