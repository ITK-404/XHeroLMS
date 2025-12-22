Shader "UI/LoadingBarPro"
{
    Properties
    {
        _MainTex ("Base Texture (optional)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)

        // Rounded corners
        _RadiusPx ("Corner Radius (px)", Float) = 10
        _SoftnessPx ("Edge Softness (px)", Float) = 1.2
        _RectPx ("Rect Size (px) X=width Y=height", Vector) = (900,18,0,0)

        // Optional fill clip
        _Fill ("Fill 0..1", Range(0,1)) = 1
        _UseFillClip ("Use Fill Clip (0/1)", Range(0,1)) = 0

        // Rails (top/bottom thin golden lines)
        _RailColor ("Rail Color", Color) = (1,0.95,0.65,1)
        _RailIntensity ("Rail Intensity", Range(0,6)) = 1.4
        _RailWidthPx ("Rail Width (px)", Float) = 1.0
        _RailGlowPx ("Rail Glow (px)", Float) = 5.0
        _RailY ("Rail Y Offset (0..0.45)", Range(0,0.45)) = 0.26

        // Silk energy threads (the main effect)
        _EnergyColor ("Energy Color", Color) = (1,0.93,0.55,1)
        _EnergyIntensity ("Energy Intensity", Range(0,12)) = 3.2

        _StrandCount ("Strand Count", Range(2,12)) = 6
        _StrandSpreadPx ("Strand Spread (px)", Float) = 4.0

        _CoreWidthPx ("Core Width (px)", Float) = 0.9
        _GlowWidthPx ("Glow Width (px)", Float) = 7.5

        _FlowSpeed ("Flow Speed", Range(0,8)) = 2.4
        _FlowFreq ("Flow Freq", Range(1,50)) = 18

        _WobbleAmpPx ("Wobble Amp (px)", Float) = 2.5
        _WobbleFreq ("Wobble Freq", Range(0.5,12)) = 2.2
        _WobbleScroll ("Wobble Scroll", Range(0,10)) = 3.0

        // Sparkles / twinkles
        _Sparkle ("Sparkle", Range(0,1)) = 0.22
        _SparkleDensity ("Sparkle Density", Range(10,220)) = 110
        _SparkleSharpness ("Sparkle Sharpness", Range(1,12)) = 6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off

        // PASS 1: Base + rails + energy (alpha blend)
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Tint;

            float _RadiusPx, _SoftnessPx;
            float4 _RectPx;

            float _Fill, _UseFillClip;

            fixed4 _RailColor;
            float _RailIntensity;
            float _RailWidthPx, _RailGlowPx;
            float _RailY;

            fixed4 _EnergyColor;
            float _EnergyIntensity;

            float _StrandCount;
            float _StrandSpreadPx;
            float _CoreWidthPx, _GlowWidthPx;

            float _FlowSpeed, _FlowFreq;

            float _WobbleAmpPx, _WobbleFreq, _WobbleScroll;

            float _Sparkle, _SparkleDensity, _SparkleSharpness;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float sdRoundRectUV(float2 uv01, float radiusUV)
            {
                float2 p = uv01 - 0.5;
                float2 b = float2(0.5, 0.5) - radiusUV;
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) - radiusUV;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float gauss(float x, float w)
            {
                return exp(-(x*x) / max(1e-8, (w*w)));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 rectPx = max(_RectPx.xy, float2(1,1));
                float minDim = min(rectPx.x, rectPx.y);

                float radiusUV   = _RadiusPx   / minDim;
                float softnessUV = _SoftnessPx / minDim;

                float distR = sdRoundRectUV(uv, radiusUV);
                float edgeAlpha = 1.0 - smoothstep(0.0, softnessUV, max(distR, 0.0));

                float fillMask = lerp(1.0, step(uv.x, _Fill), saturate(_UseFillClip));

                fixed4 col = tex2D(_MainTex, uv) * _Tint;
                col.a *= edgeAlpha * fillMask;

                float t = _Time.y;

                // -----------------------
                // Rails (top + bottom)
                // -----------------------
                float railWidthUV = _RailWidthPx / rectPx.y;
                float railGlowUV  = _RailGlowPx  / rectPx.y;

                float yTop = 0.5 + _RailY;
                float yBot = 0.5 - _RailY;

                float rails =
                    (gauss(abs(uv.y - yTop), railWidthUV) + gauss(abs(uv.y - yBot), railWidthUV)) * 1.2 +
                    (gauss(abs(uv.y - yTop), railGlowUV)  + gauss(abs(uv.y - yBot), railGlowUV))  * 0.55;

                rails *= edgeAlpha * fillMask;
                col.rgb += _RailColor.rgb * rails * _RailIntensity;

                // -----------------------
                // Silk energy threads
                // -----------------------
                float coreW = _CoreWidthPx / rectPx.y;
                float glowW = _GlowWidthPx / rectPx.y;

                float wobbleA = _WobbleAmpPx / rectPx.y;

                // centerline wobble (global)
                float center = 0.5 + sin((uv.x * _WobbleFreq + t * _WobbleScroll) * 6.2831853) * wobbleA;

                // multi strands across Y around center
                float count = clamp(floor(_StrandCount + 0.5), 2.0, 12.0);
                float spreadUV = (_StrandSpreadPx / rectPx.y);

                float energy = 0.0;

                // flow noise along X makes it look like "silk flowing"
                float flowPhase = (uv.x * _FlowFreq - t * _FlowSpeed);

                [unroll(12)]
                for (int si = 0; si < 12; si++)
                {
                    if (si >= (int)count) break;

                    float seed = si * 13.7 + 1.3;

                    // strand vertical offset (spread) + slight per-strand wobble
                    float off = ( (si / max(1.0, count-1.0)) - 0.5 ) * 2.0; // -1..1
                    float strandY = center + off * spreadUV;

                    // per-strand micro wiggle
                    strandY += sin((uv.x * (_WobbleFreq*1.7) + t * (_WobbleScroll*1.25) + seed) * 6.2831853) * (wobbleA * 0.35);

                    float dy = abs(uv.y - strandY);

                    // flow intensity modulation (continuous, not beads)
                    float n = sin((flowPhase + seed) * 6.2831853);
                    float m = 0.55 + 0.45 * (n * 0.5 + 0.5); // 0.55..1.0

                    // strand core + glow
                    float sCore = gauss(dy, coreW) * 1.1;
                    float sGlow = gauss(dy, glowW) * 0.6;

                    // extra "filament" variation
                    float thin = gauss(dy, coreW * 0.55) * (0.35 + 0.65 * hash21(float2(seed, floor(flowPhase))));

                    float s = (sCore + sGlow + thin) * m;

                    // fade near ends slightly (like sample: energy stronger in middle)
                    float vfade = smoothstep(0.0, 0.08, uv.y) * (1.0 - smoothstep(0.92, 1.0, uv.y));
                    s *= vfade;

                    energy += s;
                }

                energy *= edgeAlpha * fillMask;

                // add energy color
                col.rgb += _EnergyColor.rgb * energy * _EnergyIntensity;

                // -----------------------
                // Sparkles (tiny twinkles near energy)
                // -----------------------
                if (_Sparkle > 0.001)
                {
                    float2 sp = float2(uv.x * _SparkleDensity, uv.y * (_SparkleDensity * 0.35));
                    float rnd = hash21(sp + t * 1.9);
                    float gate = saturate(energy * 0.22);
                    float tw = pow(saturate(rnd), _SparkleSharpness); // sharper = rarer & brighter
                    float s = step(0.985, tw) * gate * edgeAlpha * fillMask;
                    col.rgb += _EnergyColor.rgb * s * (_Sparkle * 3.0);
                }

                // optional: lift alpha a bit where energy is strong
                col.a = max(col.a, saturate(energy) * 0.18 * edgeAlpha * fillMask);

                return col;
            }
            ENDCG
        }

        // PASS 2: Additive glow pass (makes it "gold silk" like your sample)
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAdd
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Tint;

            float _RadiusPx, _SoftnessPx;
            float4 _RectPx;

            float _Fill, _UseFillClip;

            fixed4 _EnergyColor;
            float _EnergyIntensity;

            float _StrandCount;
            float _StrandSpreadPx;
            float _GlowWidthPx;

            float _FlowSpeed, _FlowFreq;
            float _WobbleAmpPx, _WobbleFreq, _WobbleScroll;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float sdRoundRectUV(float2 uv01, float radiusUV)
            {
                float2 p = uv01 - 0.5;
                float2 b = float2(0.5, 0.5) - radiusUV;
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) - radiusUV;
            }

            float gauss(float x, float w)
            {
                return exp(-(x*x) / max(1e-8, (w*w)));
            }

            fixed4 fragAdd(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 rectPx = max(_RectPx.xy, float2(1,1));
                float minDim = min(rectPx.x, rectPx.y);

                float radiusUV   = _RadiusPx   / minDim;
                float softnessUV = _SoftnessPx / minDim;

                float distR = sdRoundRectUV(uv, radiusUV);
                float edgeAlpha = 1.0 - smoothstep(0.0, softnessUV, max(distR, 0.0));

                float fillMask = lerp(1.0, step(uv.x, _Fill), saturate(_UseFillClip));

                float t = _Time.y;

                float glowW = _GlowWidthPx / rectPx.y;
                float wobbleA = _WobbleAmpPx / rectPx.y;

                float center = 0.5 + sin((uv.x * _WobbleFreq + t * _WobbleScroll) * 6.2831853) * wobbleA;

                float count = clamp(floor(_StrandCount + 0.5), 2.0, 12.0);
                float spreadUV = (_StrandSpreadPx / rectPx.y);

                float flowPhase = (uv.x * _FlowFreq - t * _FlowSpeed);

                float glow = 0.0;

                [unroll(12)]
                for (int si = 0; si < 12; si++)
                {
                    if (si >= (int)count) break;

                    float seed = si * 9.3 + 2.1;
                    float off = ( (si / max(1.0, count-1.0)) - 0.5 ) * 2.0;
                    float strandY = center + off * spreadUV;

                    strandY += sin((uv.x * (_WobbleFreq*1.7) + t * (_WobbleScroll*1.25) + seed) * 6.2831853) * (wobbleA * 0.35);

                    float dy = abs(uv.y - strandY);

                    float n = sin((flowPhase + seed) * 6.2831853);
                    float m = 0.45 + 0.55 * (n * 0.5 + 0.5);

                    glow += gauss(dy, glowW) * m;
                }

                glow *= edgeAlpha * fillMask;

                // Additive glow color (scaled down so it doesn't blow out)
                fixed3 add = _EnergyColor.rgb * glow * (_EnergyIntensity * 0.22);
                return fixed4(add, 1);
            }
            ENDCG
        }
    }
}
