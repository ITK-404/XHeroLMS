Shader "Custom/StencilMask"
{
     SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        ColorMask 0
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass { }
    }
}
