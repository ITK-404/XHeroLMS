// Author: Blastom
// 基于Unity原生Unlit/Texture，渲染序列改为2500，加入颜色参数

Shader "Zhanyou/Skybox" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Tint ("Tint Color", Color) = (1, 1, 1, 1)
		_FogRatio("Fog Ratio", Range(0, 1)) = 0
	}

	SubShader {
		Tags { "Queue"="AlphaTest+40" "RenderType"="Background" }
		Pass {
			Cull Back
			Lighting Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "SkyBoxFog.cginc"
			
			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Tint;
			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.texcoord);
				col.rgb *= _Tint.rgb;
				UNITY_OPAQUE_ALPHA(col.a);
				APPLY_SKY_BOX_FOG(i.fogCoord, col)
				return col;
			}
			ENDCG
		}
	}
}