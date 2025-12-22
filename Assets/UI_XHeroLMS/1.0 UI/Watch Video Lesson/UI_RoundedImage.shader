Shader "UI/RoundedImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Radius ("Corner Radius (px)", Float) = 24
        _Softness ("Edge Softness (px)", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _Radius;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Lấy texture gốc (Image / RawImage)
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Chuẩn hoá UV về -1..1
                float2 uv = i.uv * 2.0 - 1.0;

                // Tính khoảng cách tới mép (signed distance)
                float2 d = abs(uv) - (1.0 - (_Radius * 2.0 / 100.0));
                float dist = length(max(d, 0.0));

                // Smooth alpha
                float alpha = 1.0 - smoothstep(0.0, _Softness / 100.0, dist);

                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
