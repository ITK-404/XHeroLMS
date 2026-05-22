Shader "Custom/UI/GradientSlider"
{
    Properties
    {
        // --- Màu gradient ---
        _ColorFrom ("Color From (Left)", Color) = (1, 0, 0, 1)
        _ColorTo   ("Color To (Right)",  Color) = (0, 1, 0, 1)

        // --- Giá trị slider [0, 1] ---
        _Value ("Value", Range(0, 1)) = 0.5

        // --- Unity UI yêu cầu các prop sau để stencil / masking hoạt động ---
        _StencilComp  ("Stencil Comparison", Float) = 8
        _Stencil      ("Stencil ID",         Float) = 0
        _StencilOp    ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask    ("Color Mask", Float) = 15
    }

    SubShader
    {
        // Đây là UI nên phải set đúng Tags
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // Stencil dùng cho Unity UI Masking (ScrollRect, Mask component...)
        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GradientSlider"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP core
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // -------------------------------------------------------
            // Struct
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;       // vertex color từ CanvasRenderer
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            // -------------------------------------------------------
            // Properties → CBUFFER
            // -------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorFrom;
                float4 _ColorTo;
                float  _Value;
            CBUFFER_END

            // -------------------------------------------------------
            // Vertex
            // -------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;   // giữ lại tint màu của Canvas
                return OUT;
            }

            // -------------------------------------------------------
            // Fragment
            // -------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                // uv.x chạy từ 0 (trái) → 1 (phải)
                float uvX = IN.uv.x;

                // --- Phần chưa tới: discard hoàn toàn ---
                // clip() sẽ discard pixel nếu argument < 0
                clip(_Value - uvX);

                // --- Gradient 2 màu theo chiều ngang ---
                // Remap uvX vào [0, _Value] để gradient
                // vẫn trải đều trên phần được hiển thị
                float t = uvX / max(_Value, 0.0001);
                half4 gradColor = lerp(_ColorFrom, _ColorTo, t);

                // Nhân với vertex color để tương thích Canvas tint / alpha fade
                gradColor *= IN.color;

                return gradColor;
            }
            ENDHLSL
        }
    }

    // Fallback cho editor preview
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
