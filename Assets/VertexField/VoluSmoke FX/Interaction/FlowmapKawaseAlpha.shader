Shader "Hidden/Flowmap_AllInOne"

{

    Properties 

    { 

        _MainTex("Src", 2D) = "white" {}

    }

    

    SubShader

    {

        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }

        LOD 100

        ZWrite Off 

        ZTest Always 

        Cull Off 

        

        // PASS 0 — PAINT

        Pass

        {

            Name "PAINT"

            Blend Off

            ColorMask RGBA

            

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma target 3.0

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            

            sampler2D _MainTex;

            float4 _MainTex_TexelSize;

            float4 _BrushUV;

            float4 _FlowDir;

            float _Hardness;

            float _AlphaScale;

            float _Stretch;

            float _AlphaBrushRadius;

            

            struct appdata

            {

                float4 vertex : POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            struct v2f

            {

                float4 pos : SV_POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            v2f vert(appdata v)

            {

                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;

            }

            

            float2 SafeNormalize(float2 v)

            {

                float len = length(v);

                return (len > 0.00001) ? (v / len) : float2(1, 0);

            }

            

            float BrushMask(float2 uv, float2 center, float radius, float2 dir, float hardness, float stretch)

            {

                float2 safeDir = SafeNormalize(dir + float2(0.00001, 0));

                float2 tangent = safeDir;

                float2 normal = float2(-tangent.y, tangent.x);

                

                float2 delta = uv - center;

                float distTangent = dot(delta, tangent);

                float distNormal = dot(delta, normal);

                

                float stretchedRadius = radius * max(stretch, 1.0);

                float normalRadius = radius / max(stretch, 1.0);

                

                float lt = distTangent / max(stretchedRadius, 0.00001);

                float ln = distNormal / max(normalRadius, 0.00001);

                

                float dist = sqrt(lt * lt + ln * ln);

                float falloff = saturate(1.0 - dist);

                float power = lerp(2.0, 6.0, saturate(hardness));

                

                return pow(max(falloff, 0.0), power);

            }

            

            fixed4 frag(v2f i) : SV_Target

            {

                UNITY_SETUP_INSTANCE_ID(i);

                

                float4 current = tex2D(_MainTex, i.uv);

                

                float2 brushCenter = _BrushUV.xy;

                float brushRadius = max(_BrushUV.z, 0.0001);

                float brushStrength = _BrushUV.w;

                

                // Early exit if no painting

                if (brushStrength <= 0.0)

                    return current;

                

                float2 flowDirection = SafeNormalize(_FlowDir.xy);

                float alphaRadius = (_AlphaBrushRadius > 0.0) ? _AlphaBrushRadius : brushRadius;

                alphaRadius = max(alphaRadius, 0.0001);

               

                float flowMask = BrushMask(i.uv, brushCenter, brushRadius, flowDirection, _Hardness, _Stretch);

                float alphaMask = BrushMask(i.uv, brushCenter, alphaRadius, flowDirection, _Hardness, _Stretch);

                float combinedMask = max(flowMask, alphaMask);

                

                // Early exit if outside both brushes

                if (combinedMask <= 0.00001)

                    return current;

                

                float strengthWrite = saturate(flowMask * brushStrength);

                float alphaWrite = saturate(alphaMask * brushStrength * _AlphaScale);



                float ttlPrev = current.a;

                float2 neutral = float2(0.5, 0.5);

                float2 prevDiff = (ttlPrev > 1e-4) ? ((current.rg - neutral) / ttlPrev) : float2(0, 0);

                float2 targetDiff = flowDirection * 0.5;



                float ttl = saturate(lerp(ttlPrev, 1.0, strengthWrite));

                float dirBlendFactor = saturate(flowMask);

                float2 blendedDiff = lerp(prevDiff, targetDiff, dirBlendFactor);



                current.a = ttl;

                current.rg = saturate(blendedDiff * ttl + neutral);



                current.b = saturate(current.b + alphaWrite * (1.0 - current.b));



                return current;

            }

            ENDCG

        }

        

        // PASS 1 — DECAY

        Pass

        {

            Name "DECAY"

            Blend Off

            ColorMask RGBA

            

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma target 3.0

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            

            sampler2D _MainTex;

            float4 _MainTex_TexelSize;

            float _StepRG;

            float _StepA;

            float _SnapAlphaThresh;

            

            struct appdata

            {

                float4 vertex : POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            struct v2f

            {

                float4 pos : SV_POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            v2f vert(appdata v)

            {

                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;

            }

            

            fixed4 frag(v2f i) : SV_Target

            {

                UNITY_SETUP_INSTANCE_ID(i);

                

                float4 color = tex2D(_MainTex, i.uv);

                

                float2 neutral = float2(0.5, 0.5);

                float ttlPrev = color.a;

                float2 baseDir = (ttlPrev > 1e-4) ? ((color.rg - neutral) / ttlPrev) : float2(0, 0);



                float ttlStep = min(ttlPrev, _StepRG);

                float ttl = saturate(ttlPrev - ttlStep);

                color.a = ttl;

                color.rg = saturate(baseDir * ttl + neutral);



                float alphaStep = min(color.b, _StepA);

                color.b = saturate(color.b - alphaStep);



                if (color.b <= _SnapAlphaThresh && ttl <= _SnapAlphaThresh)

                {

                    color.rg = float2(0.5, 0.5);

                    color.b = 0.0;

                    color.a = 0.0;

                }



                return color;

            }

            ENDCG

        }

        

        // PASS 2 — BLUR (Kawase)

        Pass

        {

            Name "BLUR"

            Blend Off

            ColorMask RGBA

            

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma target 3.0

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            

            sampler2D _MainTex;

            float4 _MainTex_TexelSize;

            float _OffsetPx;

            float _BlurA;

            float _BlurRG;

            float _PreserveNeutralRG;

            float _Oriented;

            float _FlipSign;

            

            struct appdata

            {

                float4 vertex : POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            struct v2f

            {

                float4 pos : SV_POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            

            v2f vert(appdata v)

            {

                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;

            }

            

            float2 SafeNormalize(float2 v)

            {

                float len = length(v);

                return (len > 0.00001) ? (v / len) : float2(1, 0);

            }

            

            fixed4 frag(v2f i) : SV_Target

            {

                UNITY_SETUP_INSTANCE_ID(i);

                

                float2 texelSize = _MainTex_TexelSize.xy;

                float flipMult = (_FlipSign < 0.0) ? -1.0 : 1.0;

                float2 offset = texelSize * (_OffsetPx * flipMult);

                

                float4 centerSample = tex2D(_MainTex, i.uv);

                

                // Calculate oriented offsets

                float2 offsetX = float2(offset.x, 0);

                float2 offsetY = float2(0, offset.y);

                

                if (_Oriented > 0.5)

                {

                    float2 flow = centerSample.rg * 2.0 - 1.0;

                    float flowLen = length(flow);

                    

                    if (flowLen > 0.001)

                    {

                        float2 tangent = SafeNormalize(flow);

                        float2 normal = float2(-tangent.y, tangent.x);

                        offsetX = tangent * offset.x;

                        offsetY = normal * offset.y;

                    }

                }

                

                // Kawase blur - 9 samples

                float4 s0 = tex2D(_MainTex, i.uv + offsetX);

                float4 s1 = tex2D(_MainTex, i.uv - offsetX);

                float4 s2 = tex2D(_MainTex, i.uv + offsetY);

                float4 s3 = tex2D(_MainTex, i.uv - offsetY);

                float4 s4 = tex2D(_MainTex, i.uv + offsetX + offsetY);

                float4 s5 = tex2D(_MainTex, i.uv - offsetX + offsetY);

                float4 s6 = tex2D(_MainTex, i.uv + offsetX - offsetY);

                float4 s7 = tex2D(_MainTex, i.uv - offsetX - offsetY);

                

                float weight = 1.0 / 9.0;

                

                // Transparency blur

                float transparencyResult = centerSample.b;

                if (_BlurA > 0.5)

                {

                    float transparencySum = centerSample.b + s0.b + s1.b + s2.b + s3.b + s4.b + s5.b + s6.b + s7.b;

                    transparencyResult = saturate(transparencySum * weight);

                }



                // RG blur (flow)

                float2 rgResult = centerSample.rg;

                if (_BlurRG > 0.5)

                {

                    if (_PreserveNeutralRG > 0.5)

                    {

                        // Blur in signed space to preserve neutral

                        float2 c0 = centerSample.rg - float2(0.5, 0.5);

                        float2 c1 = s0.rg - float2(0.5, 0.5);

                        float2 c2 = s1.rg - float2(0.5, 0.5);

                        float2 c3 = s2.rg - float2(0.5, 0.5);

                        float2 c4 = s3.rg - float2(0.5, 0.5);

                        float2 c5 = s4.rg - float2(0.5, 0.5);

                        float2 c6 = s5.rg - float2(0.5, 0.5);

                        float2 c7 = s6.rg - float2(0.5, 0.5);

                        float2 c8 = s7.rg - float2(0.5, 0.5);

                        

                        float2 signedSum = c0 + c1 + c2 + c3 + c4 + c5 + c6 + c7 + c8;

                        rgResult = saturate((signedSum * weight) + float2(0.5, 0.5));

                    }

                    else

                    {

                        // Standard blur

                        float2 rgSum = centerSample.rg + s0.rg + s1.rg + s2.rg + s3.rg + s4.rg + s5.rg + s6.rg + s7.rg;

                        rgResult = saturate(rgSum * weight);

                    }

                }

                

                return float4(rgResult, transparencyResult, centerSample.a);

            }

            ENDCG

        }

    }

    

    Fallback Off

}

