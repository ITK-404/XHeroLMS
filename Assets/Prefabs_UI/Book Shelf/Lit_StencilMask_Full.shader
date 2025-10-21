Shader "URP/BasicStencil"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [Toggle] _Grayscale ("Grayscale", Float) = 0
        _GrayscaleAmount ("Grayscale Amount", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        
        Stencil
        {
            Ref 1
            Comp Equal
            Pass Keep
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _GRAYSCALE_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _GrayscaleAmount;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color *= _BaseColor;
                
                #ifdef _GRAYSCALE_ON
                    // Luminance method (perceptually accurate)
                    half gray = dot(color.rgb, half3(0.299, 0.587, 0.114));
                    color.rgb = lerp(color.rgb, half3(gray, gray, gray), _GrayscaleAmount);
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}