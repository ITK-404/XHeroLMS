Shader "Effects/S_Knife_A01_URP" {
    Properties {
        _TexKnife ("贴图1", 2D) = "white" {}
        _UKnife ("U向位移", Float ) = 0
        _VKnife ("V向位移", Float ) = 0
        _RotatorKnife ("旋转", Range(-1, 1)) = 0
        [Toggle] _CustomUV ("自定义UV", Float ) = 0
        _TexDetails ("细节贴图", 2D) = "black" {}
        [HDR]_Color ("颜色", Color) = (1,1,1,1)
        _Desaturate ("褪色", Float ) = 0
        _Alpha ("Alpha强度", Float ) = 1
        [Toggle] _RorA ("RorA通道", Float ) = 0
        _UDetails ("U向位移", Float ) = 0
        _VDetails ("V向位移", Float ) = 0
        _RotatorDetails ("旋转", Range(-1, 1)) = 0
        _TexNoise ("噪波贴图", 2D) = "black" {}
        _UNoise ("U向位移", Float ) = 0
        _VNoise ("V向位移", Float ) = 0
        _Noise ("噪波强度", Float ) = 0
        _TexMask ("遮罩贴图", 2D) = "white" {}
        _Dissolve ("溶解", Range(0, 1)) = 0
        [Toggle] _DissolveNoise ("噪波溶解", Float ) = 0
        [Toggle] _DissolveReverse ("反向溶解", Float ) = 0
        [Toggle] _CustomDissolve ("自定义溶解", Float ) = 0
        _Edgelight ("溶解边宽度", Range(0, 1)) = 0
        [HDR]_EdgeColor ("溶解边颜色", Color) = (1,1,1,1)
        _EdgeAlpha ("溶解边Alpha强度", Float ) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }

    SubShader {
        Tags {
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="UniversalForward"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UI_CLIP_EFFECT
            #pragma multi_compile _ _CUSTOMUV_ON
            #pragma multi_compile _ _RORA_ON
            #pragma multi_compile _ _DISSOLVENOISE_ON
            #pragma multi_compile _ _DISSOLVEREVERSE_ON
            #pragma multi_compile _ _CUSTOMDISSOLVE_ON
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _ClipRect;
            float4 _ClipSoftness;

            TEXTURE2D(_TexDetails); SAMPLER(sampler_TexDetails); float4 _TexDetails_ST;
            TEXTURE2D(_TexNoise);   SAMPLER(sampler_TexNoise);   float4 _TexNoise_ST;
            TEXTURE2D(_TexKnife);   SAMPLER(sampler_TexKnife);   float4 _TexKnife_ST;
            TEXTURE2D(_TexMask);    SAMPLER(sampler_TexMask);    float4 _TexMask_ST;

            float4 _Color;
            float _UNoise;
            float _VNoise;
            float _Alpha;
            float _UDetails;
            float _VDetails;
            float _RotatorKnife;
            float _RotatorDetails;
            float _Desaturate;
            float _Noise;
            float _Dissolve;
            float _Edgelight;
            float4 _EdgeColor;
            float _UKnife;
            float _VKnife;
            float _EdgeAlpha;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 vertexColor : COLOR;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 vertexColor : COLOR;
                #ifdef UI_CLIP_EFFECT
                    float3 vpos : TEXCOORD3;
                #endif
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.vertexColor = v.vertexColor;

                VertexPositionInputs posInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = posInput.positionCS;

                #ifdef UI_CLIP_EFFECT
                    o.vpos = TransformObjectToWorld(v.vertex.xyz);
                #endif

                return o;
            }

            half4 frag(VertexOutput i, float facing : VFACE) : SV_Target {

                float alpha = 1;
                #ifdef UI_CLIP_EFFECT
                    alpha *= (i.vpos.x >= _ClipRect.x);
                    alpha *= (i.vpos.x <= _ClipRect.z);
                    alpha *= (i.vpos.y >= _ClipRect.y);
                    alpha *= (i.vpos.y <= _ClipRect.w);
                    clip(alpha - 0.001);

                    float2 center = (_ClipRect.xy + _ClipRect.zw) * 0.5;
                    float2 length = (_ClipRect.zw - _ClipRect.xy) * 0.5;
                    float2 softness = length * _ClipSoftness.xy;
                    float2 m = clamp((length - abs(i.vpos.xy - center)) / softness, 0, 1);
                    alpha *= m.x * m.y;
                #endif

                float node_697 = 3.141592654;
                float node_7943_ang = (_RotatorDetails*node_697);
                float node_7943_spd = 1.0;
                float node_7943_cos = cos(node_7943_spd*node_7943_ang);
                float node_7943_sin = sin(node_7943_spd*node_7943_ang);
                float2 node_7943_piv = float2(0.5,0.5);
                float4 node_5777 = _Time;
                float2 node_7943 = (mul((i.uv0+(node_5777.g*float2(_UDetails,_VDetails)))-node_7943_piv,float2x2( node_7943_cos, -node_7943_sin, node_7943_sin, node_7943_cos))+node_7943_piv);

                float2 uvNew = i.uv0.xy + node_5777.g*float2(_UNoise,_VNoise);

                float4 _TexNoise_var = SAMPLE_TEXTURE2D(_TexNoise, sampler_TexNoise, TRANSFORM_TEX(uvNew, _TexNoise));

                float rjqd = _Noise;

                float node_7898 = (_TexNoise_var.r*rjqd);
                float2 node_2665 = (node_7943+node_7898);
                float4 _TexDetails_var = SAMPLE_TEXTURE2D(_TexDetails, sampler_TexDetails, TRANSFORM_TEX(node_2665, _TexDetails));
                float node_7092_ang = (_RotatorKnife*node_697);
                float node_7092_spd = 1.0;
                float node_7092_cos = cos(node_7092_spd*node_7092_ang);
                float node_7092_sin = sin(node_7092_spd*node_7092_ang);
                float2 node_7092_piv = float2(0.5,0.5);
                #ifdef _CUSTOMUV_ON
                    float2 aaaa =((i.uv0*float2(i.uv1.r,i.uv1.g))+float2(i.uv1.b,i.uv1.a));
                #else
                    float2 aaaa =(i.uv0+(node_5777.g*float2(_UKnife,_VKnife)));
                #endif
                float2 node_7092 = (mul( aaaa -node_7092_piv,float2x2( node_7092_cos, -node_7092_sin, node_7092_sin, node_7092_cos))+node_7092_piv);
                float2 node_2667 = (node_7092+node_7898);
                half4 _TexKnife_var = SAMPLE_TEXTURE2D(_TexKnife, sampler_TexKnife, TRANSFORM_TEX(node_2667, _TexKnife));
                half4 _TexMask_var = SAMPLE_TEXTURE2D(_TexMask, sampler_TexMask, TRANSFORM_TEX(i.uv0.xy, _TexMask));
                #ifdef _DISSOLVENOISE_ON
                    float _DissolveNoise_var =_TexMask_var.r*_TexNoise_var.r*_TexDetails_var.a;
                #else
                    float _DissolveNoise_var =_TexDetails_var.r*_TexKnife_var.r;
                #endif
                #ifdef _CUSTOMDISSOLVE_ON
                    float node_3980 = (i.uv2.r)*2;
                #else
                    float node_3980 = (_Dissolve)*2;
                #endif
                float node_9118 = saturate((node_3980-1.0));
                float node_9441 = 0.0;
                #ifdef _DISSOLVEREVERSE_ON
                    float fxrj = (1.0 - _DissolveNoise_var);
                #else
                    float fxrj = _DissolveNoise_var;
                #endif
                float node_9522 = saturate((node_9441 + ( (fxrj - node_9118) * (1.0 - node_9441) ) / (saturate(node_3980) - node_9118)));
                float node_7871 = step((_Edgelight*-0.5+0.5),node_9522);
                clip(node_7871 - 0.5);

                float node_4359 = saturate((_EdgeAlpha*(node_7871-step(0.5,node_9522))));
                half3 emissive = (lerp((_Color.rgb*lerp(_TexDetails_var.rgb,dot(_TexDetails_var.rgb,float3(0.3,0.59,0.11)),_Desaturate)),(_EdgeColor.rgb*node_4359),node_4359)*i.vertexColor.rgb);
                half3 finalColor = emissive;
                #ifdef _RORA_ON
                     half a1 = _TexDetails_var.a;
                #else
                     half a1 = _TexDetails_var.r;
                #endif
                return half4(finalColor,(saturate(((((_Color.a* a1 *_Alpha)*_TexKnife_var.r)+node_4359)*_TexMask_var.r))*i.vertexColor.a)) * alpha;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
