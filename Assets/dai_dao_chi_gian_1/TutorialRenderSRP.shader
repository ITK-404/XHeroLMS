Shader "Custom/TutorialRenderSRB"
{
    Properties
    {
        _Strength ("Strength",Range(0,1)) = 0.2
        _Center ("Center",Vector) = (0.5,0.5,0,0)
        _Radius ("Radius",Range(0,1)) = 0.1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "TutorialRenderSRP"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            float _Strength;
            float4 _Center;
            float _Radius;
            float4 Frag(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = input.texcoord;
                uv.x *= aspect;
                
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                half2 center = half2(_Center.x * aspect, _Center.y);
                
                float dis = distance(uv,center);
                
                float mask = smoothstep(_Radius, _Radius-0.05, dis);
                
                half4 dimmed = half4(color.rgb * _Strength, color.a);
                return lerp(dimmed, color, mask);
            }
            ENDHLSL
        }
    }
}