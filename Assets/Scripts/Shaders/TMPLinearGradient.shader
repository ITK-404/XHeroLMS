Shader "Custom/TMP_LinearGradient_URP"
{
    Properties
    {
        _MainTex("Font Atlas (SDF)", 2D) = "white" {}
        _ColorTop("Top Color", Color) = (1,1,1,1)
        _ColorBottom("Bottom Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

              TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _ColorTop;
            float4 _ColorBottom;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                // Lấy SDF alpha từ font atlas
                float sdf = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;

                // Linear gradient toàn bộ chữ theo UV y
                float4 gradient = lerp(_ColorBottom, _ColorTop, IN.uv.y);

                // Nhân alpha SDF để chữ hiển thị đúng
                gradient.a *= sdf;

                return gradient;
            }
            ENDHLSL
        }
    }
}
