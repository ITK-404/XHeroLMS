Shader "Custom/AlwaysOnTop3D_HiddenByUI"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color (Tint)", Color) = (1,1,1,1) // Hỗ trợ HDR để rực rỡ hơn
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "IgnoreProjector"="True" }
        
        LOD 100
        ZWrite Off
        ZTest Always // Luôn hiện trên 3D object
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Tắt sương mù để tránh bị xám khi ở xa
            #pragma multi_compile_fog 

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; 
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // Loại bỏ ảnh hưởng của sương mù bằng cách ép màu về đúng col ban đầu
                // Hoặc đơn giản là không áp dụng UNITY_APPLY_FOG
                return col;
            }
            ENDCG
        }
    }
}