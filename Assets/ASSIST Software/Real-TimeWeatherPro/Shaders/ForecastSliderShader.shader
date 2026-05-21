Shader "Unlit/ForecastSliderShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Tiling("Tiling", Vector) = (1,1,1,1)
        _Anim("Anim", Vector) = (0,0,0,0)
    }
        SubShader
        {
            Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert alpha
                #pragma fragment frag alpha

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _Tiling;
                float4 _Anim;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.uv.x *= _Tiling.x;
                    o.uv.y *= _Tiling.y;
                    o.uv += _Anim.xy;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // sample the texture
                    fixed4 col = tex2D(_MainTex, i.uv); 
                    return col;
                }
                ENDCG
            }
        }
}
