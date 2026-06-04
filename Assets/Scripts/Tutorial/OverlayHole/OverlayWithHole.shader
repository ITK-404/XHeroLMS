Shader "Tutorial/OverlayWithHole"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,0.95)
        _HoleRect ("Hole Rect", Vector) = (0,0,0,0)
        // x = xMin, y = yMin, z = xMax, w = yMax (screen space pixels)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha   // alpha blending bình thường
        ZWrite Off                         // không ghi depth buffer
        Cull Off                           // render cả 2 mặt

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Input từ mesh của UI Image
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Dữ liệu truyền từ vertex sang fragment
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;  // vị trí pixel thực trên màn hình
            };

            float4 _Color;
            float4 _HoleRect;  // (xMin, yMin, xMax, yMax)

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Tính screen position thực (pixel coordinates)
                // ComputeScreenPos trả về 0..1, nhân với _ScreenParams.xy để ra pixels
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Convert về pixel coords
                float2 screenPx = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy;

                // Kiểm tra pixel có nằm trong hole không
                bool insideHole =
                    screenPx.x > _HoleRect.x &&
                    screenPx.y > _HoleRect.y &&
                    screenPx.x < _HoleRect.z &&
                    screenPx.y < _HoleRect.w;

                if (insideHole)
                    discard;  // bỏ pixel này, để lộ UI bên dưới

                return _Color;
            }
            ENDCG
        }
    }
}