// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_VFX_URP_Puddle_01"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Toggle(_USEWORLDSPACE_ON)] _UseWorldSpace("Use World Space", Float) = 1
		_Color01("Color 01", Color) = (1,1,1)
		_Emission("Emission", Float) = 0
		_Specular("Specular", Float) = 0.001
		_Smoothness("Smoothness", Float) = 0
		_DepthFadeOpacity("Depth Fade Opacity", Float) = 1
		[Space(33)][Header(UV Noise)][Space(13)]_UVNoise("UV Noise", 2D) = "white" {}
		_UVNoiseIntensity("UV Noise Intensity", Float) = 0.1
		_UVNoiseScale("UV Noise Scale", Vector) = (1,1,0,0)
		[Space(33)][Header(World Noise)][Space(13)]_WorldNoise("World Noise", 2D) = "white" {}
		_WorldUVNoiseIntensity("World UV Noise Intensity", Float) = 0.1
		_TriplanarTile("Triplanar Tile", Vector) = (1,1,1,0)
		_TriplanarTileOffsetCustom("Triplanar Tile Offset Custom", Vector) = (0,0,0,0)
		_WaterNormalStrength("Water Normal Strength", Float) = 1
		[Space(33)][Header(Fresnel)][Space(13)]_FresnelBias("Fresnel Bias", Float) = 0
		_FresnelScale("Fresnel Scale", Float) = 1
		_FresnelPower("Fresnel Power", Float) = 5
		[Space(33)][Header(Refraction Fresnel)][Space(13)]_RefractionFresnelBias("Refraction Fresnel Bias", Float) = 0
		_RefractionFresnelScale("Refraction Fresnel Scale", Float) = 1
		_RefractionFresnelPower("Refraction Fresnel Power", Float) = 5
		_RefractionMin("Refraction Min", Float) = 0
		_RefractionMax("Refraction Max", Float) = 1
		[Space(33)][Header(Camera Depth Fade)][Space(13)]_CameraDepthFadeLength("Camera Depth Fade Length", Float) = 1
		_CameraDepthFadeOffset("Camera Depth Fade Offset", Float) = 0
		[Space(33)][Header(Wave Mask)][Space(13)]_WaveMask("Wave Mask", 2D) = "white" {}
		_WavesIntensity("Waves Intensity", Float) = 8
		_WavesTiling("Waves Tiling", Float) = 3
		_WavesSpeed("Waves Speed", Float) = 2
		_ErodeIn("Erode In", Float) = 0
		_ErodeOut("Erode Out", Float) = -0.5
		[Space(33)][Header(AR)][Space(13)]_Cull("Cull", Float) = 0
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2


		//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		//_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
		//_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
		//_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
		//_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
		//_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
		//_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector][ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1
		[HideInInspector][ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1
		[HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Lit" }

		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Offset 0 , 0
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile _ _FORWARD_PLUS

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_FORWARD

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_BITANGENT
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float4 lightmapUVOrVertexSH : TEXCOORD1;
				half4 fogFactorAndVertexLight : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord : TEXCOORD6;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON)
					float2 dynamicLightmapUV : TEXCOORD7;
				#endif	
				#if defined(USE_APV_PROBE_OCCLUSION)
					float4 probeOcclusion : TEXCOORD8;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord9 : TEXCOORD9;
				float4 ase_texcoord10 : TEXCOORD10;
				float4 ase_texcoord11 : TEXCOORD11;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WaveMask;
			sampler2D _UVNoise;
			sampler2D _WorldNoise;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord11.x = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord9 = input.texcoord;
				output.ase_texcoord10 = input.texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord11.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif
				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.tSpace0 = float4( normalInput.normalWS, vertexInput.positionWS.x );
				output.tSpace1 = float4( normalInput.tangentWS, vertexInput.positionWS.y );
				output.tSpace2 = float4( normalInput.bitangentWS, vertexInput.positionWS.z );

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV( input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy );
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				OUTPUT_SH4( vertexInput.positionWS, normalInput.normalWS.xyz, GetWorldSpaceNormalizeViewDir( vertexInput.positionWS ), output.lightmapUVOrVertexSH.xyz, output.probeOcclusion );

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					output.lightmapUVOrVertexSH.zw = input.texcoord.xy;
					output.lightmapUVOrVertexSH.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				half3 vertexLight = VertexLighting( vertexInput.positionWS, normalInput.normalWS );

				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( vertexInput.positionCS.z );
				#else
					half fogFactor = 0;
				#endif

				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.texcoord1 = input.texcoord1;
				output.texcoord2 = input.texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						, bool ase_vface : SV_IsFrontFace ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float2 sampleCoords = (input.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
					float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
				#else
					float3 WorldNormal = normalize( input.tSpace0.xyz );
					float3 WorldTangent = input.tSpace1.xyz;
					float3 WorldBiTangent = input.tSpace2.xyz;
				#endif

				float3 WorldPosition = float3(input.tSpace0.w,input.tSpace1.w,input.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif

				WorldViewDirection = SafeNormalize( WorldViewDirection );

				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 texCoord437 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult433 = (float2(0.005 , 0.0));
				float wavesTiling444 = _WavesTiling;
				float2 temp_cast_1 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g93 = ( ( ( texCoord437 + appendResult433 ) * wavesTiling444 ) - temp_cast_1 );
				float2 temp_output_15_0_g93 = ( temp_output_2_0_g93 * temp_output_2_0_g93 );
				float2 appendResult20_g93 = (float2(frac( ( atan2( (temp_output_2_0_g93).x , (temp_output_2_0_g93).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g93).x + (temp_output_15_0_g93).y ) )));
				float2 temp_output_468_0 = appendResult20_g93;
				float saferPower469 = abs( (temp_output_468_0).y );
				float2 texCoord423 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float randomOffset532 = input.ase_texcoord10.x;
				float4 tex2DNode419 = tex2D( _UVNoise, ( ( texCoord423 * _UVNoiseScale ) + randomOffset532 ) );
				float2 appendResult429 = (float2(( ( tex2DNode419.r + -0.2 ) * 2.0 ) , ( ( tex2DNode419.g + -0.2 ) * 2.0 )));
				float2 UVNoise421 = ( appendResult429 * _UVNoiseIntensity );
				float2 appendResult472 = (float2((temp_output_468_0).x , ( pow( saferPower469 , 2.0 ) + UVNoise421 ).x));
				float wavesSpeed482 = _WavesSpeed;
				float2 appendResult475 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float2 texCoord439 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_5 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g94 = ( ( texCoord439 * wavesTiling444 ) - temp_cast_5 );
				float2 temp_output_15_0_g94 = ( temp_output_2_0_g94 * temp_output_2_0_g94 );
				float2 appendResult20_g94 = (float2(frac( ( atan2( (temp_output_2_0_g94).x , (temp_output_2_0_g94).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g94).x + (temp_output_15_0_g94).y ) )));
				float2 temp_output_498_0 = appendResult20_g94;
				float saferPower486 = abs( (temp_output_498_0).y );
				float2 appendResult489 = (float2((temp_output_498_0).x , ( pow( saferPower486 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult493 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float4 tex2DNode403 = tex2D( _WaveMask, ( ( appendResult489 - appendResult493 ) + worldUVNoise364 ) );
				float3 appendResult410 = (float3(1.0 , 0.0 , ( ( tex2D( _WaveMask, ( ( appendResult472 - appendResult475 ) + worldUVNoise364 ) ).g - tex2DNode403.g ) * _WavesIntensity )));
				float2 appendResult434 = (float2(0.0 , 0.005));
				float2 texCoord436 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_7 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g95 = ( ( ( appendResult434 + texCoord436 ) * wavesTiling444 ) - temp_cast_7 );
				float2 temp_output_15_0_g95 = ( temp_output_2_0_g95 * temp_output_2_0_g95 );
				float2 appendResult20_g95 = (float2(frac( ( atan2( (temp_output_2_0_g95).x , (temp_output_2_0_g95).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g95).x + (temp_output_15_0_g95).y ) )));
				float2 temp_output_513_0 = appendResult20_g95;
				float saferPower501 = abs( (temp_output_513_0).y );
				float2 appendResult504 = (float2((temp_output_513_0).x , ( pow( saferPower501 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult508 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 appendResult411 = (float3(0.0 , 1.0 , ( ( tex2DNode403.g - tex2D( _WaveMask, ( ( appendResult504 - appendResult508 ) + worldUVNoise364 ) ).g ) * _WavesIntensity )));
				float2 texCoord352 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord9.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord9.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float3 lerpResult347 = lerp( float3(0,0,1) , cross( appendResult410 , appendResult411 ) , temp_output_392_0);
				float3 normanReedus398 = lerpResult347;
				float3 appendResult138 = (float3(_WaterNormalStrength , _WaterNormalStrength , 1.0));
				float3 WaterNormal140 = ( normanReedus398 * appendResult138 );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 tanToWorld0 = float3( WorldTangent.x, WorldBiTangent.x, WorldNormal.x );
				float3 tanToWorld1 = float3( WorldTangent.y, WorldBiTangent.y, WorldNormal.y );
				float3 tanToWorld2 = float3( WorldTangent.z, WorldBiTangent.z, WorldNormal.z );
				float3 tanNormal1_g91 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g91 = _RefractionFresnelPower;
				float lerpResult3_g91 = lerp( ( -1.0 * temp_output_4_0_g91 ) , temp_output_4_0_g91 , ase_vface);
				float fresnelNdotV1_g91 = dot( float3(dot(tanToWorld0,tanNormal1_g91), dot(tanToWorld1,tanNormal1_g91), dot(tanToWorld2,tanNormal1_g91)), ase_viewDirWS );
				float fresnelNode1_g91 = ( _RefractionFresnelBias + _RefractionFresnelScale * pow( 1.0 - fresnelNdotV1_g91, lerpResult3_g91 ) );
				float temp_output_198_0 = fresnelNode1_g91;
				float lerpResult205 = lerp( _RefractionMin , _RefractionMax , saturate( temp_output_198_0 ));
				float4 fetchOpaqueVal23 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( (ase_screenPosNorm).xy + ( ( ( (WaterNormal140).xy + -0.5 ) * 2.0 ) * lerpResult205 ) ) ), 1.0 );
				float4 temp_output_39_0 = ( ( float4( _Color01 , 0.0 ) * input.ase_color ) * fetchOpaqueVal23 );
				
				float spec84 = _Specular;
				float3 temp_cast_13 = (spec84).xxx;
				
				float smoothness90 = _Smoothness;
				
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord11.x;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float gangnamStyle397 = temp_output_392_0;
				

				float3 BaseColor = temp_output_39_0.rgb;
				float3 Normal = WaterNormal140;
				float3 Emission = ( temp_output_39_0 * _Emission ).rgb;
				float3 Specular = temp_cast_13;
				float Metallic = 0;
				float Smoothness = smoothness90;
				float Occlusion = 1;
				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _CLEARCOAT
					float CoatMask = 0;
					float CoatSmoothness = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = WorldPosition;
				inputData.positionCS = input.positionCS;
				inputData.viewDirectionWS = WorldViewDirection;

				#ifdef _NORMALMAP
						#if _NORMAL_DROPOFF_TS
							inputData.normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent, WorldBiTangent, WorldNormal));
						#elif _NORMAL_DROPOFF_OS
							inputData.normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							inputData.normalWS = Normal;
						#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = WorldNormal;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					inputData.shadowCoord = ShadowCoords;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = input.fogFactorAndVertexLight.x;
				#endif
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = input.lightmapUVOrVertexSH.xyz;
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
					inputData.bakedGI = SAMPLE_GI( SH, GetAbsolutePositionWS(inputData.positionWS),
						inputData.normalWS,
						inputData.viewDirectionWS,
						input.positionCS.xy,
						input.probeOcclusion,
						inputData.shadowMask );
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#endif

				#ifdef ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif

				inputData.normalizedScreenSpaceUV = NormalizedScreenSpaceUV;

				#if defined(DEBUG_DISPLAY)
					#if defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
						#endif
					#if defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVOrVertexSH.xy;
					#else
						inputData.vertexSH = SH;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				SurfaceData surfaceData;
				surfaceData.albedo              = BaseColor;
				surfaceData.metallic            = saturate(Metallic);
				surfaceData.specular            = Specular;
				surfaceData.smoothness          = saturate(Smoothness),
				surfaceData.occlusion           = Occlusion,
				surfaceData.emission            = Emission,
				surfaceData.alpha               = saturate(Alpha);
				surfaceData.normalTS            = Normal;
				surfaceData.clearCoatMask       = 0;
				surfaceData.clearCoatSmoothness = 1;

				#ifdef _CLEARCOAT
					surfaceData.clearCoatMask       = saturate(CoatMask);
					surfaceData.clearCoatSmoothness = saturate(CoatSmoothness);
				#endif

				#ifdef _DBUFFER
					ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
				#endif

				#ifdef _ASE_LIGHTING_SIMPLE
					half4 color = UniversalFragmentBlinnPhong( inputData, surfaceData);
				#else
					half4 color = UniversalFragmentPBR( inputData, surfaceData);
				#endif

				#ifdef ASE_TRANSMISSION
				{
					float shadow = _TransmissionShadow;

					#define SUM_LIGHT_TRANSMISSION(Light)\
						float3 atten = Light.color * Light.distanceAttenuation;\
						atten = lerp( atten, atten * Light.shadowAttenuation, shadow );\
						half3 transmission = max( 0, -dot( inputData.normalWS, Light.direction ) ) * atten * Transmission;\
						color.rgb += BaseColor * transmission;

					SUM_LIGHT_TRANSMISSION( GetMainLight( inputData.shadowCoord ) );

					#if defined(_ADDITIONAL_LIGHTS)
						uint meshRenderingLayers = GetMeshRenderingLayer();
						uint pixelLightCount = GetAdditionalLightsCount();
						#if USE_FORWARD_PLUS
							[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
							{
								FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

								Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
								#ifdef _LIGHT_LAYERS
								if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
								#endif
								{
									SUM_LIGHT_TRANSMISSION( light );
								}
							}
						#endif
						LIGHT_LOOP_BEGIN( pixelLightCount )
							Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
							#ifdef _LIGHT_LAYERS
							if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
							#endif
							{
								SUM_LIGHT_TRANSMISSION( light );
							}
						LIGHT_LOOP_END
					#endif
				}
				#endif

				#ifdef ASE_TRANSLUCENCY
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;

					#define SUM_LIGHT_TRANSLUCENCY(Light)\
						float3 atten = Light.color * Light.distanceAttenuation;\
						atten = lerp( atten, atten * Light.shadowAttenuation, shadow );\
						half3 lightDir = Light.direction + inputData.normalWS * normal;\
						half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );\
						half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;\
						color.rgb += BaseColor * translucency * strength;

					SUM_LIGHT_TRANSLUCENCY( GetMainLight( inputData.shadowCoord ) );

					#if defined(_ADDITIONAL_LIGHTS)
						uint meshRenderingLayers = GetMeshRenderingLayer();
						uint pixelLightCount = GetAdditionalLightsCount();
						#if USE_FORWARD_PLUS
							[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
							{
								FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

								Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
								#ifdef _LIGHT_LAYERS
								if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
								#endif
								{
									SUM_LIGHT_TRANSLUCENCY( light );
								}
							}
						#endif
						LIGHT_LOOP_BEGIN( pixelLightCount )
							Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
							#ifdef _LIGHT_LAYERS
							if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
							#endif
							{
								SUM_LIGHT_TRANSLUCENCY( light );
							}
						LIGHT_LOOP_END
					#endif
				}
				#endif

				#ifdef ASE_REFRACTION
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, float4( WorldNormal,0 ) ).xyz * ( 1.0 - dot( WorldNormal, WorldViewDirection ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos.xy ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), input.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
					#endif
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				return color;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask R
			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 positionWS : TEXCOORD1;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WorldNoise;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_worldTangent = TransformObjectToWorldDir(input.ase_tangent.xyz);
				output.ase_texcoord3.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.ase_texcoord4.xyz = ase_worldNormal;
				float ase_vertexTangentSign = input.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord5.xyz = ase_worldBitangent;
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord3.w = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord6 = input.ase_texcoord;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord4.w = 0;
				output.ase_texcoord5.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_tangent = input.ase_tangent;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(	PackedVaryings input
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						, bool ase_vface : SV_IsFrontFace ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldTangent = input.ase_texcoord3.xyz;
				float3 ase_worldNormal = input.ase_texcoord4.xyz;
				float3 ase_worldBitangent = input.ase_texcoord5.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord3.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float2 texCoord352 = input.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord6.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord6.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float gangnamStyle397 = temp_output_392_0;
				

				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma shader_feature EDITOR_VISUALIZATION

			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 texcoord0 : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef EDITOR_VISUALIZATION
					float4 VizUV : TEXCOORD2;
					float4 LightCoord : TEXCOORD3;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				float4 ase_texcoord8 : TEXCOORD8;
				float4 ase_texcoord9 : TEXCOORD9;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WaveMask;
			sampler2D _UVNoise;
			sampler2D _WorldNoise;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_clipPos = TransformObjectToHClip((input.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				output.ase_texcoord4 = screenPos;
				float3 ase_worldTangent = TransformObjectToWorldDir(input.ase_tangent.xyz);
				output.ase_texcoord7.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.ase_texcoord8.xyz = ase_worldNormal;
				float ase_vertexTangentSign = input.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord9.xyz = ase_worldBitangent;
				
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord7.w = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord5 = input.texcoord0;
				output.ase_texcoord6 = input.texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord8.w = 0;
				output.ase_texcoord9.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = positionWS;
				#endif

				output.positionCS = MetaVertexPosition( input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );

				#ifdef EDITOR_VISUALIZATION
					float2 VizUV = 0;
					float4 LightCoord = 0;
					UnityEditorVizData(input.positionOS.xyz, input.texcoord0.xy, input.texcoord1.xy, input.texcoord2.xy, VizUV, LightCoord);
					output.VizUV = float4(VizUV, 0, 0);
					output.LightCoord = LightCoord;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = output.positionCS;
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 texcoord0 : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.texcoord0 = input.texcoord0;
				output.texcoord1 = input.texcoord1;
				output.texcoord2 = input.texcoord2;
				output.ase_color = input.ase_color;
				output.ase_tangent = input.ase_tangent;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.texcoord0 = patch[0].texcoord0 * bary.x + patch[1].texcoord0 * bary.y + patch[2].texcoord0 * bary.z;
				output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , bool ase_vface : SV_IsFrontFace ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 screenPos = input.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 texCoord437 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult433 = (float2(0.005 , 0.0));
				float wavesTiling444 = _WavesTiling;
				float2 temp_cast_1 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g93 = ( ( ( texCoord437 + appendResult433 ) * wavesTiling444 ) - temp_cast_1 );
				float2 temp_output_15_0_g93 = ( temp_output_2_0_g93 * temp_output_2_0_g93 );
				float2 appendResult20_g93 = (float2(frac( ( atan2( (temp_output_2_0_g93).x , (temp_output_2_0_g93).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g93).x + (temp_output_15_0_g93).y ) )));
				float2 temp_output_468_0 = appendResult20_g93;
				float saferPower469 = abs( (temp_output_468_0).y );
				float2 texCoord423 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float randomOffset532 = input.ase_texcoord6.x;
				float4 tex2DNode419 = tex2D( _UVNoise, ( ( texCoord423 * _UVNoiseScale ) + randomOffset532 ) );
				float2 appendResult429 = (float2(( ( tex2DNode419.r + -0.2 ) * 2.0 ) , ( ( tex2DNode419.g + -0.2 ) * 2.0 )));
				float2 UVNoise421 = ( appendResult429 * _UVNoiseIntensity );
				float2 appendResult472 = (float2((temp_output_468_0).x , ( pow( saferPower469 , 2.0 ) + UVNoise421 ).x));
				float wavesSpeed482 = _WavesSpeed;
				float2 appendResult475 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float2 texCoord439 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_5 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g94 = ( ( texCoord439 * wavesTiling444 ) - temp_cast_5 );
				float2 temp_output_15_0_g94 = ( temp_output_2_0_g94 * temp_output_2_0_g94 );
				float2 appendResult20_g94 = (float2(frac( ( atan2( (temp_output_2_0_g94).x , (temp_output_2_0_g94).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g94).x + (temp_output_15_0_g94).y ) )));
				float2 temp_output_498_0 = appendResult20_g94;
				float saferPower486 = abs( (temp_output_498_0).y );
				float2 appendResult489 = (float2((temp_output_498_0).x , ( pow( saferPower486 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult493 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float4 tex2DNode403 = tex2D( _WaveMask, ( ( appendResult489 - appendResult493 ) + worldUVNoise364 ) );
				float3 appendResult410 = (float3(1.0 , 0.0 , ( ( tex2D( _WaveMask, ( ( appendResult472 - appendResult475 ) + worldUVNoise364 ) ).g - tex2DNode403.g ) * _WavesIntensity )));
				float2 appendResult434 = (float2(0.0 , 0.005));
				float2 texCoord436 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_7 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g95 = ( ( ( appendResult434 + texCoord436 ) * wavesTiling444 ) - temp_cast_7 );
				float2 temp_output_15_0_g95 = ( temp_output_2_0_g95 * temp_output_2_0_g95 );
				float2 appendResult20_g95 = (float2(frac( ( atan2( (temp_output_2_0_g95).x , (temp_output_2_0_g95).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g95).x + (temp_output_15_0_g95).y ) )));
				float2 temp_output_513_0 = appendResult20_g95;
				float saferPower501 = abs( (temp_output_513_0).y );
				float2 appendResult504 = (float2((temp_output_513_0).x , ( pow( saferPower501 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult508 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 appendResult411 = (float3(0.0 , 1.0 , ( ( tex2DNode403.g - tex2D( _WaveMask, ( ( appendResult504 - appendResult508 ) + worldUVNoise364 ) ).g ) * _WavesIntensity )));
				float2 texCoord352 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord5.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord5.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float3 lerpResult347 = lerp( float3(0,0,1) , cross( appendResult410 , appendResult411 ) , temp_output_392_0);
				float3 normanReedus398 = lerpResult347;
				float3 appendResult138 = (float3(_WaterNormalStrength , _WaterNormalStrength , 1.0));
				float3 WaterNormal140 = ( normanReedus398 * appendResult138 );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldTangent = input.ase_texcoord7.xyz;
				float3 ase_worldNormal = input.ase_texcoord8.xyz;
				float3 ase_worldBitangent = input.ase_texcoord9.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal1_g91 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g91 = _RefractionFresnelPower;
				float lerpResult3_g91 = lerp( ( -1.0 * temp_output_4_0_g91 ) , temp_output_4_0_g91 , ase_vface);
				float fresnelNdotV1_g91 = dot( float3(dot(tanToWorld0,tanNormal1_g91), dot(tanToWorld1,tanNormal1_g91), dot(tanToWorld2,tanNormal1_g91)), ase_viewDirWS );
				float fresnelNode1_g91 = ( _RefractionFresnelBias + _RefractionFresnelScale * pow( 1.0 - fresnelNdotV1_g91, lerpResult3_g91 ) );
				float temp_output_198_0 = fresnelNode1_g91;
				float lerpResult205 = lerp( _RefractionMin , _RefractionMax , saturate( temp_output_198_0 ));
				float4 fetchOpaqueVal23 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( (ase_screenPosNorm).xy + ( ( ( (WaterNormal140).xy + -0.5 ) * 2.0 ) * lerpResult205 ) ) ), 1.0 );
				float4 temp_output_39_0 = ( ( float4( _Color01 , 0.0 ) * input.ase_color ) * fetchOpaqueVal23 );
				
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord7.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float gangnamStyle397 = temp_output_392_0;
				

				float3 BaseColor = temp_output_39_0.rgb;
				float3 Emission = ( temp_output_39_0 * _Emission ).rgb;
				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = BaseColor;
				metaInput.Emission = Emission;
				#ifdef EDITOR_VISUALIZATION
					metaInput.VizUV = input.VizUV.xy;
					metaInput.LightCoord = input.LightCoord;
				#endif

				return UnityMetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WaveMask;
			sampler2D _UVNoise;
			sampler2D _WorldNoise;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_TRANSFER_INSTANCE_ID( input, output );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				float4 ase_clipPos = TransformObjectToHClip((input.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				output.ase_texcoord2 = screenPos;
				float3 ase_worldTangent = TransformObjectToWorldDir(input.ase_tangent.xyz);
				output.ase_texcoord5.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.ase_texcoord6.xyz = ase_worldNormal;
				float ase_vertexTangentSign = input.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord7.xyz = ase_worldBitangent;
				
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord5.w = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord3 = input.ase_texcoord;
				output.ase_texcoord4 = input.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord6.w = 0;
				output.ase_texcoord7.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_tangent : TANGENT;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_tangent = input.ase_tangent;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , bool ase_vface : SV_IsFrontFace ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 screenPos = input.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 texCoord437 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult433 = (float2(0.005 , 0.0));
				float wavesTiling444 = _WavesTiling;
				float2 temp_cast_1 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g93 = ( ( ( texCoord437 + appendResult433 ) * wavesTiling444 ) - temp_cast_1 );
				float2 temp_output_15_0_g93 = ( temp_output_2_0_g93 * temp_output_2_0_g93 );
				float2 appendResult20_g93 = (float2(frac( ( atan2( (temp_output_2_0_g93).x , (temp_output_2_0_g93).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g93).x + (temp_output_15_0_g93).y ) )));
				float2 temp_output_468_0 = appendResult20_g93;
				float saferPower469 = abs( (temp_output_468_0).y );
				float2 texCoord423 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float randomOffset532 = input.ase_texcoord4.x;
				float4 tex2DNode419 = tex2D( _UVNoise, ( ( texCoord423 * _UVNoiseScale ) + randomOffset532 ) );
				float2 appendResult429 = (float2(( ( tex2DNode419.r + -0.2 ) * 2.0 ) , ( ( tex2DNode419.g + -0.2 ) * 2.0 )));
				float2 UVNoise421 = ( appendResult429 * _UVNoiseIntensity );
				float2 appendResult472 = (float2((temp_output_468_0).x , ( pow( saferPower469 , 2.0 ) + UVNoise421 ).x));
				float wavesSpeed482 = _WavesSpeed;
				float2 appendResult475 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float2 texCoord439 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_5 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g94 = ( ( texCoord439 * wavesTiling444 ) - temp_cast_5 );
				float2 temp_output_15_0_g94 = ( temp_output_2_0_g94 * temp_output_2_0_g94 );
				float2 appendResult20_g94 = (float2(frac( ( atan2( (temp_output_2_0_g94).x , (temp_output_2_0_g94).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g94).x + (temp_output_15_0_g94).y ) )));
				float2 temp_output_498_0 = appendResult20_g94;
				float saferPower486 = abs( (temp_output_498_0).y );
				float2 appendResult489 = (float2((temp_output_498_0).x , ( pow( saferPower486 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult493 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float4 tex2DNode403 = tex2D( _WaveMask, ( ( appendResult489 - appendResult493 ) + worldUVNoise364 ) );
				float3 appendResult410 = (float3(1.0 , 0.0 , ( ( tex2D( _WaveMask, ( ( appendResult472 - appendResult475 ) + worldUVNoise364 ) ).g - tex2DNode403.g ) * _WavesIntensity )));
				float2 appendResult434 = (float2(0.0 , 0.005));
				float2 texCoord436 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_7 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g95 = ( ( ( appendResult434 + texCoord436 ) * wavesTiling444 ) - temp_cast_7 );
				float2 temp_output_15_0_g95 = ( temp_output_2_0_g95 * temp_output_2_0_g95 );
				float2 appendResult20_g95 = (float2(frac( ( atan2( (temp_output_2_0_g95).x , (temp_output_2_0_g95).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g95).x + (temp_output_15_0_g95).y ) )));
				float2 temp_output_513_0 = appendResult20_g95;
				float saferPower501 = abs( (temp_output_513_0).y );
				float2 appendResult504 = (float2((temp_output_513_0).x , ( pow( saferPower501 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult508 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 appendResult411 = (float3(0.0 , 1.0 , ( ( tex2DNode403.g - tex2D( _WaveMask, ( ( appendResult504 - appendResult508 ) + worldUVNoise364 ) ).g ) * _WavesIntensity )));
				float2 texCoord352 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord3.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord3.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float3 lerpResult347 = lerp( float3(0,0,1) , cross( appendResult410 , appendResult411 ) , temp_output_392_0);
				float3 normanReedus398 = lerpResult347;
				float3 appendResult138 = (float3(_WaterNormalStrength , _WaterNormalStrength , 1.0));
				float3 WaterNormal140 = ( normanReedus398 * appendResult138 );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldTangent = input.ase_texcoord5.xyz;
				float3 ase_worldNormal = input.ase_texcoord6.xyz;
				float3 ase_worldBitangent = input.ase_texcoord7.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal1_g91 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g91 = _RefractionFresnelPower;
				float lerpResult3_g91 = lerp( ( -1.0 * temp_output_4_0_g91 ) , temp_output_4_0_g91 , ase_vface);
				float fresnelNdotV1_g91 = dot( float3(dot(tanToWorld0,tanNormal1_g91), dot(tanToWorld1,tanNormal1_g91), dot(tanToWorld2,tanNormal1_g91)), ase_viewDirWS );
				float fresnelNode1_g91 = ( _RefractionFresnelBias + _RefractionFresnelScale * pow( 1.0 - fresnelNdotV1_g91, lerpResult3_g91 ) );
				float temp_output_198_0 = fresnelNode1_g91;
				float lerpResult205 = lerp( _RefractionMin , _RefractionMax , saturate( temp_output_198_0 ));
				float4 fetchOpaqueVal23 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( (ase_screenPosNorm).xy + ( ( ( (WaterNormal140).xy + -0.5 ) * 2.0 ) * lerpResult205 ) ) ), 1.0 );
				float4 temp_output_39_0 = ( ( float4( _Color01 , 0.0 ) * input.ase_color ) * fetchOpaqueVal23 );
				
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord5.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float gangnamStyle397 = temp_output_392_0;
				

				float3 BaseColor = temp_output_39_0.rgb;
				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;

				half4 color = half4(BaseColor, Alpha );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				return color;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }

			ZWrite On
			Blend One Zero
			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
			//#define SHADERPASS SHADERPASS_DEPTHNORMALS

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float4 worldTangent : TEXCOORD2;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD3;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD4;
				#endif
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_color : COLOR;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WaveMask;
			sampler2D _UVNoise;
			sampler2D _WorldNoise;


			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				float3 ase_worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
				float ase_vertexTangentSign = input.tangentOS.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord7.xyz = ase_worldBitangent;
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord7.w = customEye170;
				
				output.ase_texcoord5 = input.ase_texcoord;
				output.ase_texcoord6 = input.ase_texcoord1;
				output.ase_color = input.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				float3 normalWS = TransformObjectToWorldNormal( input.normalOS );
				float4 tangentWS = float4( TransformObjectToWorldDir( input.tangentOS.xyz ), input.tangentOS.w );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				output.worldNormal = normalWS;
				output.worldTangent = tangentWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(	PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						, bool ase_vface : SV_IsFrontFace )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float3 WorldNormal = input.worldNormal;
				float4 WorldTangent = input.worldTangent;

				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 texCoord437 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult433 = (float2(0.005 , 0.0));
				float wavesTiling444 = _WavesTiling;
				float2 temp_cast_0 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g93 = ( ( ( texCoord437 + appendResult433 ) * wavesTiling444 ) - temp_cast_0 );
				float2 temp_output_15_0_g93 = ( temp_output_2_0_g93 * temp_output_2_0_g93 );
				float2 appendResult20_g93 = (float2(frac( ( atan2( (temp_output_2_0_g93).x , (temp_output_2_0_g93).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g93).x + (temp_output_15_0_g93).y ) )));
				float2 temp_output_468_0 = appendResult20_g93;
				float saferPower469 = abs( (temp_output_468_0).y );
				float2 texCoord423 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float randomOffset532 = input.ase_texcoord6.x;
				float4 tex2DNode419 = tex2D( _UVNoise, ( ( texCoord423 * _UVNoiseScale ) + randomOffset532 ) );
				float2 appendResult429 = (float2(( ( tex2DNode419.r + -0.2 ) * 2.0 ) , ( ( tex2DNode419.g + -0.2 ) * 2.0 )));
				float2 UVNoise421 = ( appendResult429 * _UVNoiseIntensity );
				float2 appendResult472 = (float2((temp_output_468_0).x , ( pow( saferPower469 , 2.0 ) + UVNoise421 ).x));
				float wavesSpeed482 = _WavesSpeed;
				float2 appendResult475 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float2 texCoord439 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_4 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g94 = ( ( texCoord439 * wavesTiling444 ) - temp_cast_4 );
				float2 temp_output_15_0_g94 = ( temp_output_2_0_g94 * temp_output_2_0_g94 );
				float2 appendResult20_g94 = (float2(frac( ( atan2( (temp_output_2_0_g94).x , (temp_output_2_0_g94).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g94).x + (temp_output_15_0_g94).y ) )));
				float2 temp_output_498_0 = appendResult20_g94;
				float saferPower486 = abs( (temp_output_498_0).y );
				float2 appendResult489 = (float2((temp_output_498_0).x , ( pow( saferPower486 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult493 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float4 tex2DNode403 = tex2D( _WaveMask, ( ( appendResult489 - appendResult493 ) + worldUVNoise364 ) );
				float3 appendResult410 = (float3(1.0 , 0.0 , ( ( tex2D( _WaveMask, ( ( appendResult472 - appendResult475 ) + worldUVNoise364 ) ).g - tex2DNode403.g ) * _WavesIntensity )));
				float2 appendResult434 = (float2(0.0 , 0.005));
				float2 texCoord436 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_6 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g95 = ( ( ( appendResult434 + texCoord436 ) * wavesTiling444 ) - temp_cast_6 );
				float2 temp_output_15_0_g95 = ( temp_output_2_0_g95 * temp_output_2_0_g95 );
				float2 appendResult20_g95 = (float2(frac( ( atan2( (temp_output_2_0_g95).x , (temp_output_2_0_g95).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g95).x + (temp_output_15_0_g95).y ) )));
				float2 temp_output_513_0 = appendResult20_g95;
				float saferPower501 = abs( (temp_output_513_0).y );
				float2 appendResult504 = (float2((temp_output_513_0).x , ( pow( saferPower501 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult508 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 appendResult411 = (float3(0.0 , 1.0 , ( ( tex2DNode403.g - tex2D( _WaveMask, ( ( appendResult504 - appendResult508 ) + worldUVNoise364 ) ).g ) * _WavesIntensity )));
				float2 texCoord352 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord5.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord5.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float3 lerpResult347 = lerp( float3(0,0,1) , cross( appendResult410 , appendResult411 ) , temp_output_392_0);
				float3 normanReedus398 = lerpResult347;
				float3 appendResult138 = (float3(_WaterNormalStrength , _WaterNormalStrength , 1.0));
				float3 WaterNormal140 = ( normanReedus398 * appendResult138 );
				
				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldBitangent = input.ase_texcoord7.xyz;
				float3 tanToWorld0 = float3( WorldTangent.xyz.x, ase_worldBitangent.x, WorldNormal.x );
				float3 tanToWorld1 = float3( WorldTangent.xyz.y, ase_worldBitangent.y, WorldNormal.y );
				float3 tanToWorld2 = float3( WorldTangent.xyz.z, ase_worldBitangent.z, WorldNormal.z );
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord7.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float gangnamStyle397 = temp_output_392_0;
				

				float3 Normal = WaterNormal140;
				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(WorldNormal);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					#if defined(_NORMALMAP)
						#if _NORMAL_DROPOFF_TS
							float crossSign = (WorldTangent.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
							float3 bitangent = crossSign * cross(WorldNormal.xyz, WorldTangent.xyz);
							float3 normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent.xyz, bitangent, WorldNormal.xyz));
						#elif _NORMAL_DROPOFF_OS
							float3 normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							float3 normalWS = Normal;
						#endif
					#else
						float3 normalWS = WorldNormal;
					#endif
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SHADERPASS SHADERPASS_GBUFFER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif
			
			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_BITANGENT
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float4 lightmapUVOrVertexSH : TEXCOORD1;
				half4 fogFactorAndVertexLight : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD6;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON)
				float2 dynamicLightmapUV : TEXCOORD7;
				#endif
				#if defined(USE_APV_PROBE_OCCLUSION)
					float4 probeOcclusion : TEXCOORD8;
				#endif
				float4 ase_color : COLOR;
				float4 ase_texcoord9 : TEXCOORD9;
				float4 ase_texcoord10 : TEXCOORD10;
				float4 ase_texcoord11 : TEXCOORD11;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WaveMask;
			sampler2D _UVNoise;
			sampler2D _WorldNoise;


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord11.x = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord9 = input.texcoord;
				output.ase_texcoord10 = input.texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord11.yzw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.tSpace0 = float4( normalInput.normalWS, vertexInput.positionWS.x);
				output.tSpace1 = float4( normalInput.tangentWS, vertexInput.positionWS.y);
				output.tSpace2 = float4( normalInput.bitangentWS, vertexInput.positionWS.z);

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				OUTPUT_SH4( vertexInput.positionWS, normalInput.normalWS.xyz, GetWorldSpaceNormalizeViewDir( vertexInput.positionWS ), output.lightmapUVOrVertexSH.xyz, output.probeOcclusion );

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					output.lightmapUVOrVertexSH.zw = input.texcoord.xy;
					output.lightmapUVOrVertexSH.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				half3 vertexLight = VertexLighting( vertexInput.positionWS, normalInput.normalWS );

				output.fogFactorAndVertexLight = half4(0, vertexLight);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.texcoord1 = input.texcoord1;
				output.texcoord2 = input.texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			FragmentOutput frag ( PackedVaryings input
								#ifdef ASE_DEPTH_WRITE_ON
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								, bool ase_vface : SV_IsFrontFace )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float2 sampleCoords = (input.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
					float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
				#else
					float3 WorldNormal = normalize( input.tSpace0.xyz );
					float3 WorldTangent = input.tSpace1.xyz;
					float3 WorldBiTangent = input.tSpace2.xyz;
				#endif

				float3 WorldPosition = float3(input.tSpace0.w,input.tSpace1.w,input.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#else
					ShadowCoords = float4(0, 0, 0, 0);
				#endif

				WorldViewDirection = SafeNormalize( WorldViewDirection );

				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 texCoord437 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult433 = (float2(0.005 , 0.0));
				float wavesTiling444 = _WavesTiling;
				float2 temp_cast_1 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g93 = ( ( ( texCoord437 + appendResult433 ) * wavesTiling444 ) - temp_cast_1 );
				float2 temp_output_15_0_g93 = ( temp_output_2_0_g93 * temp_output_2_0_g93 );
				float2 appendResult20_g93 = (float2(frac( ( atan2( (temp_output_2_0_g93).x , (temp_output_2_0_g93).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g93).x + (temp_output_15_0_g93).y ) )));
				float2 temp_output_468_0 = appendResult20_g93;
				float saferPower469 = abs( (temp_output_468_0).y );
				float2 texCoord423 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float randomOffset532 = input.ase_texcoord10.x;
				float4 tex2DNode419 = tex2D( _UVNoise, ( ( texCoord423 * _UVNoiseScale ) + randomOffset532 ) );
				float2 appendResult429 = (float2(( ( tex2DNode419.r + -0.2 ) * 2.0 ) , ( ( tex2DNode419.g + -0.2 ) * 2.0 )));
				float2 UVNoise421 = ( appendResult429 * _UVNoiseIntensity );
				float2 appendResult472 = (float2((temp_output_468_0).x , ( pow( saferPower469 , 2.0 ) + UVNoise421 ).x));
				float wavesSpeed482 = _WavesSpeed;
				float2 appendResult475 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = WorldPosition;
				#else
				float3 staticSwitch525 = ( WorldPosition - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float2 texCoord439 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_5 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g94 = ( ( texCoord439 * wavesTiling444 ) - temp_cast_5 );
				float2 temp_output_15_0_g94 = ( temp_output_2_0_g94 * temp_output_2_0_g94 );
				float2 appendResult20_g94 = (float2(frac( ( atan2( (temp_output_2_0_g94).x , (temp_output_2_0_g94).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g94).x + (temp_output_15_0_g94).y ) )));
				float2 temp_output_498_0 = appendResult20_g94;
				float saferPower486 = abs( (temp_output_498_0).y );
				float2 appendResult489 = (float2((temp_output_498_0).x , ( pow( saferPower486 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult493 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float4 tex2DNode403 = tex2D( _WaveMask, ( ( appendResult489 - appendResult493 ) + worldUVNoise364 ) );
				float3 appendResult410 = (float3(1.0 , 0.0 , ( ( tex2D( _WaveMask, ( ( appendResult472 - appendResult475 ) + worldUVNoise364 ) ).g - tex2DNode403.g ) * _WavesIntensity )));
				float2 appendResult434 = (float2(0.0 , 0.005));
				float2 texCoord436 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_7 = (( wavesTiling444 / 2.0 )).xx;
				float2 temp_output_2_0_g95 = ( ( ( appendResult434 + texCoord436 ) * wavesTiling444 ) - temp_cast_7 );
				float2 temp_output_15_0_g95 = ( temp_output_2_0_g95 * temp_output_2_0_g95 );
				float2 appendResult20_g95 = (float2(frac( ( atan2( (temp_output_2_0_g95).x , (temp_output_2_0_g95).y ) / 6.283 ) ) , sqrt( ( (temp_output_15_0_g95).x + (temp_output_15_0_g95).y ) )));
				float2 temp_output_513_0 = appendResult20_g95;
				float saferPower501 = abs( (temp_output_513_0).y );
				float2 appendResult504 = (float2((temp_output_513_0).x , ( pow( saferPower501 , 2.0 ) + UVNoise421 ).x));
				float2 appendResult508 = (float2(0.0 , frac( ( _TimeParameters.x * wavesSpeed482 ) )));
				float3 appendResult411 = (float3(0.0 , 1.0 , ( ( tex2DNode403.g - tex2D( _WaveMask, ( ( appendResult504 - appendResult508 ) + worldUVNoise364 ) ).g ) * _WavesIntensity )));
				float2 texCoord352 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord9.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord9.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord9.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float3 lerpResult347 = lerp( float3(0,0,1) , cross( appendResult410 , appendResult411 ) , temp_output_392_0);
				float3 normanReedus398 = lerpResult347;
				float3 appendResult138 = (float3(_WaterNormalStrength , _WaterNormalStrength , 1.0));
				float3 WaterNormal140 = ( normanReedus398 * appendResult138 );
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - WorldPosition );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 tanToWorld0 = float3( WorldTangent.x, WorldBiTangent.x, WorldNormal.x );
				float3 tanToWorld1 = float3( WorldTangent.y, WorldBiTangent.y, WorldNormal.y );
				float3 tanToWorld2 = float3( WorldTangent.z, WorldBiTangent.z, WorldNormal.z );
				float3 tanNormal1_g91 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g91 = _RefractionFresnelPower;
				float lerpResult3_g91 = lerp( ( -1.0 * temp_output_4_0_g91 ) , temp_output_4_0_g91 , ase_vface);
				float fresnelNdotV1_g91 = dot( float3(dot(tanToWorld0,tanNormal1_g91), dot(tanToWorld1,tanNormal1_g91), dot(tanToWorld2,tanNormal1_g91)), ase_viewDirWS );
				float fresnelNode1_g91 = ( _RefractionFresnelBias + _RefractionFresnelScale * pow( 1.0 - fresnelNdotV1_g91, lerpResult3_g91 ) );
				float temp_output_198_0 = fresnelNode1_g91;
				float lerpResult205 = lerp( _RefractionMin , _RefractionMax , saturate( temp_output_198_0 ));
				float4 fetchOpaqueVal23 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( (ase_screenPosNorm).xy + ( ( ( (WaterNormal140).xy + -0.5 ) * 2.0 ) * lerpResult205 ) ) ), 1.0 );
				float4 temp_output_39_0 = ( ( float4( _Color01 , 0.0 ) * input.ase_color ) * fetchOpaqueVal23 );
				
				float spec84 = _Specular;
				float3 temp_cast_13 = (spec84).xxx;
				
				float smoothness90 = _Smoothness;
				
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord11.x;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float gangnamStyle397 = temp_output_392_0;
				

				float3 BaseColor = temp_output_39_0.rgb;
				float3 Normal = WaterNormal140;
				float3 Emission = ( temp_output_39_0 * _Emission ).rgb;
				float3 Specular = temp_cast_13;
				float Metallic = 0;
				float Smoothness = smoothness90;
				float Occlusion = 1;
				float Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = WorldPosition;
				inputData.positionCS = input.positionCS;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
						inputData.normalWS = TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal ));
					#elif _NORMAL_DROPOFF_OS
						inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
						inputData.normalWS = Normal;
					#endif
				#else
					inputData.normalWS = WorldNormal;
				#endif

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = SafeNormalize( WorldViewDirection );

				inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = input.lightmapUVOrVertexSH.xyz;
				#endif

				#if defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
					inputData.bakedGI = SAMPLE_GI( SH, GetAbsolutePositionWS(inputData.positionWS),
						inputData.normalWS,
						inputData.viewDirectionWS,
						input.positionCS.xy,
						input.probeOcclusion,
						inputData.shadowMask );
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#endif

				#ifdef ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif

				inputData.normalizedScreenSpaceUV = NormalizedScreenSpaceUV;

				#if defined(DEBUG_DISPLAY)
					#if defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
						#endif
					#if defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVOrVertexSH.xy;
					#else
						inputData.vertexSH = SH;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				#ifdef _DBUFFER
					ApplyDecal(input.positionCS,
						BaseColor,
						Specular,
						inputData.normalWS,
						Metallic,
						Occlusion,
						Smoothness);
				#endif

				BRDFData brdfData;
				InitializeBRDFData
				(BaseColor, Metallic, Specular, Smoothness, Alpha, brdfData);

				Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
				half4 color;
				MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
				color.rgb = GlobalIllumination(brdfData, inputData.bakedGI, Occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
				color.a = Alpha;

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return BRDFDataToGbuffer(brdfData, inputData, Smoothness, Emission + color.rgb, Occlusion);
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

			#define SCENESELECTIONPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WorldNoise;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_clipPos = TransformObjectToHClip((input.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				output.ase_texcoord = screenPos;
				float3 ase_worldPos = TransformObjectToWorld( (input.positionOS).xyz );
				output.ase_texcoord1.xyz = ase_worldPos;
				float3 ase_worldTangent = TransformObjectToWorldDir(input.ase_tangent.xyz);
				output.ase_texcoord2.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.ase_texcoord3.xyz = ase_worldNormal;
				float ase_vertexTangentSign = input.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord4.xyz = ase_worldBitangent;
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord1.w = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord5 = input.ase_texcoord;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.w = 0;
				output.ase_texcoord3.w = 0;
				output.ase_texcoord4.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				output.positionCS = TransformWorldToHClip(positionWS);

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_tangent = input.ase_tangent;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , bool ase_vface : SV_IsFrontFace) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float4 screenPos = input.ase_texcoord;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 ase_worldPos = input.ase_texcoord1.xyz;
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldTangent = input.ase_texcoord2.xyz;
				float3 ase_worldNormal = input.ase_texcoord3.xyz;
				float3 ase_worldBitangent = input.ase_texcoord4.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord1.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float2 texCoord352 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = ase_worldPos;
				#else
				float3 staticSwitch525 = ( ase_worldPos - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord5.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord5.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float gangnamStyle397 = temp_output_392_0;
				

				surfaceDescription.Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;

				#ifdef SCENESELECTIONPASS
					outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				#elif defined(SCENEPICKINGPASS)
					outColor = _SelectionID;
				#endif

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif

		    #define SCENEPICKINGPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEWORLDSPACE_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _WorldNoise;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_clipPos = TransformObjectToHClip((input.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				output.ase_texcoord = screenPos;
				float3 ase_worldPos = TransformObjectToWorld( (input.positionOS).xyz );
				output.ase_texcoord1.xyz = ase_worldPos;
				float3 ase_worldTangent = TransformObjectToWorldDir(input.ase_tangent.xyz);
				output.ase_texcoord2.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.ase_texcoord3.xyz = ase_worldNormal;
				float ase_vertexTangentSign = input.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				output.ase_texcoord4.xyz = ase_worldBitangent;
				float3 customSurfaceDepth170 = input.positionOS.xyz;
				float customEye170 = -TransformWorldToView(TransformObjectToWorld(customSurfaceDepth170)).z;
				output.ase_texcoord1.w = customEye170;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord5 = input.ase_texcoord;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.w = 0;
				output.ase_texcoord3.w = 0;
				output.ase_texcoord4.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				output.positionCS = TransformWorldToHClip(positionWS);

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.vertex = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_tangent = input.ase_tangent;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].vertex, input[1].vertex, input[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].vertex.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , bool ase_vface : SV_IsFrontFace) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float4 screenPos = input.ase_texcoord;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth36 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth36 = saturate( ( screenDepth36 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFadeOpacity ) );
				float DF59 = distanceDepth36;
				float3 ase_worldPos = input.ase_texcoord1.xyz;
				float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_worldPos );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				float3 ase_worldTangent = input.ase_texcoord2.xyz;
				float3 ase_worldNormal = input.ase_texcoord3.xyz;
				float3 ase_worldBitangent = input.ase_texcoord4.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal1_g89 = float4( 0,0,1,0 ).rgb;
				float temp_output_4_0_g89 = _FresnelPower;
				float lerpResult3_g89 = lerp( ( -1.0 * temp_output_4_0_g89 ) , temp_output_4_0_g89 , ase_vface);
				float fresnelNdotV1_g89 = dot( float3(dot(tanToWorld0,tanNormal1_g89), dot(tanToWorld1,tanNormal1_g89), dot(tanToWorld2,tanNormal1_g89)), ase_viewDirWS );
				float fresnelNode1_g89 = ( _FresnelBias + _FresnelScale * pow( 1.0 - fresnelNdotV1_g89, lerpResult3_g89 ) );
				float temp_output_178_0 = fresnelNode1_g89;
				float DSFres181 = saturate( temp_output_178_0 );
				float customEye170 = input.ase_texcoord1.w;
				float cameraDepthFade170 = (( customEye170 -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
				float camDF183 = saturate( cameraDepthFade170 );
				float2 texCoord352 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float3 objToWorld518 = mul( GetObjectToWorldMatrix(), float4( float3( 0,0,0 ), 1 ) ).xyz;
				#ifdef _USEWORLDSPACE_ON
				float3 staticSwitch525 = ase_worldPos;
				#else
				float3 staticSwitch525 = ( ase_worldPos - objToWorld518 );
				#endif
				float3 temp_output_520_0 = ( staticSwitch525 + _TriplanarTileOffsetCustom );
				float4 tex2DNode365 = tex2D( _WorldNoise, ( float3( (temp_output_520_0).xz ,  0.0 ) * _TriplanarTile ).xy );
				float2 appendResult527 = (float2(( ( tex2DNode365.r + -0.2 ) * 2.0 ) , ( ( tex2DNode365.g + -0.2 ) * 2.0 )));
				float2 worldUVNoise364 = ( appendResult527 * _WorldUVNoiseIntensity );
				float clampResult9_g86 = clamp( ( ( length( (float2( -1,-1 ) + (( texCoord352 + worldUVNoise364 ) - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.666 ) * 4.2 ) , 0.0 , 1.0 );
				float2 texCoord356 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g87 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord356 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.5 ) * 2.0 ) , 0.0 , 1.0 );
				float erodeIn534 = input.ase_texcoord5.z;
				float temp_output_539_0 = ( erodeIn534 + _ErodeIn );
				float2 texCoord377 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g88 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord377 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult372 = smoothstep( temp_output_539_0 , ( temp_output_539_0 + 0.5 ) , ( 1.0 - saturate( clampResult9_g88 ) ));
				float erodeOut535 = input.ase_texcoord5.w;
				float temp_output_537_0 = ( erodeOut535 + _ErodeOut );
				float2 texCoord385 = input.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g90 = clamp( ( ( length( (float2( -1,-1 ) + (texCoord385 - float2( 0,0 )) * (float2( 1,1 ) - float2( -1,-1 )) / (float2( 1,1 ) - float2( 0,0 ))) ) + -0.2 ) * 1.0 ) , 0.0 , 1.0 );
				float smoothstepResult375 = smoothstep( temp_output_537_0 , ( temp_output_537_0 + 0.5 ) , saturate( clampResult9_g90 ));
				float temp_output_392_0 = saturate( ( saturate( ( saturate( ( ( 1.0 - saturate( clampResult9_g86 ) ) * ( 1.0 - saturate( clampResult9_g87 ) ) ) ) * smoothstepResult372 ) ) * smoothstepResult375 ) );
				float gangnamStyle397 = temp_output_392_0;
				

				surfaceDescription.Alpha = saturate( ( input.ase_color.a * saturate( ( saturate( ( saturate( ( DF59 * DSFres181 ) ) * camDF183 ) ) * gangnamStyle397 ) ) ) );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
						clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;

				#ifdef SCENESELECTIONPASS
					outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				#elif defined(SCENEPICKINGPASS)
					outColor = _SelectionID;
				#endif

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define _SPECULAR_SETUP 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define _NORMALMAP 1
			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#if defined(_SPECULAR_SETUP) && defined(_ASE_LIGHTING_SIMPLE)
				#define _SPECULAR_COLOR 1
			#endif
	
            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float3 _TriplanarTile;
			float3 _Color01;
			float3 _TriplanarTileOffsetCustom;
			float2 _UVNoiseScale;
			float _Cull;
			float _RefractionFresnelBias;
			float _RefractionFresnelScale;
			float _RefractionFresnelPower;
			float _Emission;
			float _Specular;
			float _Smoothness;
			float _DepthFadeOpacity;
			float _FresnelBias;
			float _FresnelScale;
			float _FresnelPower;
			float _RefractionMax;
			float _RefractionMin;
			float _ErodeOut;
			float _CameraDepthFadeLength;
			float _ErodeIn;
			float _WavesIntensity;
			float _WorldUVNoiseIntensity;
			float _WavesSpeed;
			float _UVNoiseIntensity;
			float _WavesTiling;
			float _ZWrite;
			float _Dst;
			float _Src;
			float _ZTest;
			float _WaterNormalStrength;
			float _CameraDepthFadeOffset;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(APLICATION_SPACE_WARP_MOTION)
					// We do not need jittered position in ASW
					output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));;
					output.positionCS = output.positionCSNoJitter;
				#else
					// Jittered. Match the frame.
					output.positionCS = vertexInput.positionCS;
					output.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, mul( UNITY_MATRIX_M, input.positionOS));
				#endif

				float4 prevPos = ( unity_MotionVectorsParams.x == 1 ) ? float4( input.positionOld, 1 ) : input.positionOS;

				#if _ADD_PRECOMPUTED_VELOCITY
					prevPos = prevPos - float4(input.alembicMotionVector, 0);
				#endif

				output.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, prevPos ) );
				// removed in ObjectMotionVectors.hlsl found in unity 6000.0.23 and higher
				//ApplyMotionVectorZBias( output.positionCS );
				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				

				float Alpha = 1;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(APLICATION_SPACE_WARP_MOTION)
					return float4( CalcAswNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 1 );
				#else
					return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
				#endif
			}		
			ENDHLSL
		}
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphLitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;413;-8754,-4146;Inherit;False;1820.773;539.3306;World UV Noise;9;364;367;366;368;365;363;526;527;528;World UV Noise;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;189;-3250,1742;Inherit;False;1060;674.95;Double Sided Fresnel;7;175;176;177;178;179;184;181;Double Sided Fresnel;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;188;-3250,1230;Inherit;False;676;290.95;Depth Fade;3;38;36;59;Depth Fade;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;190;-3248,2640;Inherit;False;804;674.95;Camera Depth Fade;6;166;167;168;170;172;183;Camera Depth Fade;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;326;-2480,1232;Inherit;False;676;290.95;Depth Fade Color;3;323;325;324;Depth Fade Color;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;243;-3250,4174;Inherit;False;932;162.9502;Smoothness;2;90;86;Smoothness;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;242;-3250,3534;Inherit;False;932;418.95;Specular;2;84;80;Specular;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;15;590,-50;Inherit;False;1252;162.95;Ge Lush was here! <3;5;10;14;11;12;13;Ge Lush was here! <3;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;415;-8752,-3376;Inherit;False;1841;824.95;UV Noise;8;420;421;419;418;417;429;430;431;UV Noise;0,0,0,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;517;-10624,-4096;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformPositionNode;518;-10624,-3840;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;519;-10368,-3968;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;525;-10368,-4096;Inherit;False;Property;_UseWorldSpace;Use World Space;0;0;Create;True;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;515;-9856,-3584;Inherit;False;Property;_TriplanarTileOffsetCustom;Triplanar Tile Offset Custom;13;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;520;-9856,-4096;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;524;-9344,-3840;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector3Node;516;-9088,-3712;Inherit;False;Property;_TriplanarTile;Triplanar Tile;12;0;Create;True;0;0;0;False;0;False;1,1,1;1,1,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;521;-9088,-3840;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;363;-8704,-4096;Inherit;True;Property;_WorldNoise;World Noise;10;0;Create;True;0;0;0;False;3;Space(33);Header(World Noise);Space(13);False;d96e50eec907e1c428a285fb1124a713;d96e50eec907e1c428a285fb1124a713;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;365;-8704,-3840;Inherit;True;Property;_TextureSample0;Texture Sample 0;29;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode;526;-8304,-3840;Inherit;True;ConstantBiasScale;-1;;84;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT;0;False;1;FLOAT;-0.2;False;2;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;528;-8304,-4096;Inherit;True;ConstantBiasScale;-1;;85;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT;0;False;1;FLOAT;-0.2;False;2;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;368;-7424,-3712;Inherit;False;Property;_WorldUVNoiseIntensity;World UV Noise Intensity;11;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;527;-7920,-3968;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;367;-7424,-3840;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;364;-7168,-3840;Inherit;False;worldUVNoise;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;352;-6912,-4096;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;369;-6400,-4224;Inherit;False;364;worldUVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;353;-6272,-3840;Inherit;False;Constant;_Float0;Float 0;28;0;Create;True;0;0;0;False;0;False;0.666;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;362;-6400,-4096;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;354;-6272,-3712;Inherit;False;Constant;_Float1;Float 1;28;0;Create;True;0;0;0;False;0;False;4.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;360;-6272,-3072;Inherit;False;Constant;_Float4;Float 1;28;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;356;-6272,-3456;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;359;-6272,-3200;Inherit;False;Constant;_Float3;Float 0;28;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;529;-7552,-2176;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;345;-5888,-4096;Inherit;False;RadialGradient;-1;;86;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;361;-5888,-3456;Inherit;False;RadialGradient;-1;;87;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;376;-6272,-2304;Inherit;False;Constant;_Float5;Float 1;28;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;377;-6272,-2688;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;380;-6272,-2432;Inherit;False;Constant;_Float6;Float 0;28;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;534;-7296,-2048;Inherit;False;erodeIn;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;349;-5504,-4096;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;357;-5504,-3456;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;381;-5888,-2688;Inherit;False;RadialGradient;-1;;88;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;373;-6272,-2816;Inherit;False;Property;_ErodeIn;Erode In;29;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;538;-6272,-2944;Inherit;False;534;erodeIn;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;175;-3200,2304;Inherit;False;Property;_FresnelPower;Fresnel Power;17;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-3200,2176;Inherit;False;Property;_FresnelScale;Fresnel Scale;16;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;177;-3200,2048;Inherit;False;Property;_FresnelBias;Fresnel Bias;15;0;Create;True;0;0;0;False;3;Space(33);Header(Fresnel);Space(13);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;355;-5248,-4096;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;358;-5248,-3456;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;378;-5504,-2688;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;535;-7296,-1920;Inherit;False;erodeOut;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;539;-6016,-2944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;178;-3200,1792;Inherit;False;DoubleSidedFresnel;-1;;89;64ed426cf297c5b48b5b91bdfa24d4b5;0;4;10;COLOR;0,0,1,0;False;7;FLOAT;0;False;6;FLOAT;1;False;4;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-3200,1408;Inherit;False;Property;_DepthFadeOpacity;Depth Fade Opacity;6;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;370;-4992,-3840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;382;-6016,-2784;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;379;-5248,-2688;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;385;-6272,-1920;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;374;-6272,-2048;Inherit;False;Property;_ErodeOut;Erode Out;30;0;Create;True;0;0;0;False;0;False;-0.5;-0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;536;-6272,-2176;Inherit;False;535;erodeOut;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;166;-3200,3200;Inherit;False;Property;_CameraDepthFadeOffset;Camera Depth Fade Offset;24;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;167;-3200,3072;Inherit;False;Property;_CameraDepthFadeLength;Camera Depth Fade Length;23;0;Create;True;0;0;0;False;3;Space(33);Header(Camera Depth Fade);Space(13);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;168;-3200,2816;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;184;-2688,1792;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;36;-3200,1280;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;389;-5888,-1920;Inherit;False;RadialGradient;-1;;90;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;372;-4992,-2816;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;371;-4736,-3840;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;537;-6016,-2176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;170;-3200,2688;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;181;-2432,1792;Inherit;False;DSFres;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;59;-2816,1280;Inherit;False;DF;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;383;-6016,-2016;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;386;-5504,-1920;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;390;-4608,-3200;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;187;-1792,640;Inherit;False;181;DSFres;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;172;-2944,2688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;375;-4992,-2048;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;391;-4352,-3200;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;180;-2048,768;Inherit;False;59;DF;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;-1792,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;183;-2688,2688;Inherit;False;camDF;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;393;-4352,-2432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;185;-1280,640;Inherit;False;183;camDF;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;171;-1536,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;392;-4096,-2432;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;173;-1280,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;397;-3712,-2432;Inherit;False;gangnamStyle;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;174;-1024,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;396;-768,1024;Inherit;False;397;gangnamStyle;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;394;-768,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;29;-1280,384;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;395;-640,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-896,384;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;179;-2944,1792;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;640,0;Inherit;False;Property;_Cull;Cull;31;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;1664,0;Inherit;False;Property;_ZTest;ZTest;35;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;896,0;Inherit;False;Property;_Src;Src;32;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;1152,0;Inherit;False;Property;_Dst;Dst;33;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;1408,0;Inherit;False;Property;_ZWrite;ZWrite;34;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;-1536,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;21;-2048,-384;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;22;-1792,-384;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScreenColorNode;23;-1280,0;Inherit;False;Global;_GrabScreen0;Grab Screen 0;6;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-640,-384;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;18;-2816,0;Inherit;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-3200,0;Inherit;False;140;WaterNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2048,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;198;-3200,256;Inherit;False;DoubleSidedFresnel;-1;;91;64ed426cf297c5b48b5b91bdfa24d4b5;0;4;10;COLOR;0,0,1,0;False;7;FLOAT;0;False;6;FLOAT;1;False;4;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;200;-2688,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;196;-3200,640;Inherit;False;Property;_RefractionFresnelScale;Refraction Fresnel Scale;19;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;195;-3200,768;Inherit;False;Property;_RefractionFresnelPower;Refraction Fresnel Power;20;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;197;-3200,512;Inherit;False;Property;_RefractionFresnelBias;Refraction Fresnel Bias;18;0;Create;True;0;0;0;False;3;Space(33);Header(Refraction Fresnel);Space(13);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;204;-2688,640;Inherit;False;Property;_RefractionMax;Refraction Max;22;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;203;-2688,512;Inherit;False;Property;_RefractionMin;Refraction Min;21;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;205;-2432,256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-2560,3584;Inherit;False;spec;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;90;-2560,4224;Inherit;False;smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-3200,4224;Inherit;False;Property;_Smoothness;Smoothness;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;186;-640,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;323;-2048,1280;Inherit;False;DFColor;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;324;-2432,1280;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;325;-2432,1408;Inherit;False;Property;_DepthFadeColor;Depth Fade Color;5;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;331;-128,-384;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;332;-128,-256;Inherit;False;Property;_Emission;Emission;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;26;-2688,-1280;Inherit;False;Property;_Color01;Color 01;1;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;False;0;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;333;-640,-1280;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;334;-640,-1664;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;17;-2560,0;Inherit;False;ConstantBiasScale;-1;;92;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;199;-2944,320;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-3200,3584;Inherit;False;Property;_Specular;Specular;3;0;Create;True;0;0;0;False;0;False;0.001;0.001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;347;-4096,-1280;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;348;-4480,-1536;Inherit;False;Constant;_Vector0;Vector 0;28;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;91;-384,256;Inherit;False;90;smoothness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-384,128;Inherit;False;84;spec;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;79;-384,0;Inherit;False;140;WaterNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;398;-3712,-1280;Inherit;False;normanReedus;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;420;-8704,-3328;Inherit;True;Property;_UVNoise;UV Noise;7;0;Create;True;0;0;0;False;3;Space(33);Header(UV Noise);Space(13);False;d96e50eec907e1c428a285fb1124a713;d96e50eec907e1c428a285fb1124a713;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;419;-8704,-3072;Inherit;True;Property;_TextureSample4;Texture Sample 0;29;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;418;-7680,-3072;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;417;-7680,-2944;Inherit;False;Property;_UVNoiseIntensity;UV Noise Intensity;8;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;421;-7296,-3072;Inherit;True;UVNoise;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;400;-7424,-1152;Inherit;True;Property;_TextureSample1;Texture Sample 0;29;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;403;-7424,-768;Inherit;True;Property;_TextureSample2;Texture Sample 0;29;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;404;-7424,-384;Inherit;True;Property;_TextureSample3;Texture Sample 0;29;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleSubtractOpNode;405;-6912,-384;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;406;-6656,-384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;407;-6656,-1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;409;-6912,-768;Inherit;False;Property;_WavesIntensity;Waves Intensity;26;0;Create;True;0;0;0;False;0;False;8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;410;-6272,-1024;Inherit;True;FLOAT3;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;411;-6272,-384;Inherit;True;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CrossProductOpNode;346;-5760,-640;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;402;-6912,-1024;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;437;-11008,-1152;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;438;-10368,-1152;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;446;-10112,-1152;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;447;-9856,-1152;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;448;-10112,-1024;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;449;-10368,-1024;Inherit;False;444;wavesTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;432;-11392,-256;Inherit;False;Constant;_Float7;Float 7;32;0;Create;True;0;0;0;False;0;False;0.005;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;439;-10624,-256;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;440;-10368,-256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;441;-10112,-256;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;445;-10624,-128;Inherit;False;444;wavesTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;436;-11008,640;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;454;-9088,-1152;Inherit;False;True;False;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;455;-9088,-1024;Inherit;False;False;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;469;-8832,-1024;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;471;-8832,-896;Inherit;False;421;UVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;470;-8576,-1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;472;-8448,-1152;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;473;-8192,-1152;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;414;-7936,-1024;Inherit;False;364;worldUVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;474;-7936,-1152;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;475;-8320,-896;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;478;-8832,-768;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;480;-8416,-768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;479;-8576,-768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;483;-8832,-640;Inherit;False;482;wavesSpeed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;442;-10368,-128;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;434;-11008,512;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;433;-11008,-1024;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;435;-10368,512;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;450;-10112,512;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;451;-9856,512;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;452;-10112,640;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;453;-10368,640;Inherit;False;444;wavesTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;468;-9600,-1152;Inherit;False;SH_F_Vefects_VFX_Vector_To_Radial_Value;-1;;93;56cc3109e9c9cfb4eb8311df7a8658f8;0;1;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;484;-9088,-256;Inherit;False;True;False;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;485;-9088,-128;Inherit;False;False;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;486;-8832,-128;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;487;-8832,0;Inherit;False;421;UVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;488;-8576,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;489;-8448,-256;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;490;-8192,-256;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;491;-7936,-128;Inherit;False;364;worldUVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;492;-7936,-256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;493;-8320,0;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;494;-8832,128;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;495;-8416,128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;496;-8576,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;497;-8832,256;Inherit;False;482;wavesSpeed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;498;-9600,-256;Inherit;False;SH_F_Vefects_VFX_Vector_To_Radial_Value;-1;;94;56cc3109e9c9cfb4eb8311df7a8658f8;0;1;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;499;-9088,512;Inherit;False;True;False;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;500;-9088,640;Inherit;False;False;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;501;-8832,640;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;502;-8832,768;Inherit;False;421;UVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;503;-8576,640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;504;-8448,512;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;505;-8192,512;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;506;-7936,640;Inherit;False;364;worldUVNoise;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;507;-7936,512;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;508;-8320,768;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;509;-8832,896;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;510;-8416,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;511;-8576,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;512;-8832,1024;Inherit;False;482;wavesSpeed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;513;-9600,512;Inherit;False;SH_F_Vefects_VFX_Vector_To_Radial_Value;-1;;95;56cc3109e9c9cfb4eb8311df7a8658f8;0;1;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;444;-11776,-256;Inherit;False;wavesTiling;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;481;-12032,-128;Inherit;False;Property;_WavesSpeed;Waves Speed;28;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;482;-11776,-128;Inherit;False;wavesSpeed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;443;-12032,-256;Inherit;False;Property;_WavesTiling;Waves Tiling;27;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;138;-3968,-256;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;139;-4352,-144;Inherit;False;Constant;_Float2;Float 2;29;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;-3968,-512;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-4352,-256;Inherit;False;Property;_WaterNormalStrength;Water Normal Strength;14;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;399;-4224,-512;Inherit;False;398;normanReedus;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;140;-3712,-512;Inherit;False;WaterNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;401;-7424,-1408;Inherit;True;Property;_WaveMask;Wave Mask;25;0;Create;True;0;0;0;False;3;Space(33);Header(Wave Mask);Space(13);False;3c64f3705b3056d418217e7e84d4fb62;3c64f3705b3056d418217e7e84d4fb62;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ComponentMaskNode;522;-9344,-4096;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;523;-9344,-3968;Inherit;False;False;True;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;431;-8320,-2944;Inherit;True;ConstantBiasScale;-1;;96;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT;0;False;1;FLOAT;-0.2;False;2;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;429;-7936,-3072;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;430;-8320,-3200;Inherit;True;ConstantBiasScale;-1;;97;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT;0;False;1;FLOAT;-0.2;False;2;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;366;-7680,-3840;Inherit;True;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;423;-9856,-3072;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;424;-9472,-3072;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;426;-9472,-2944;Inherit;False;Property;_UVNoiseScale;UV Noise Scale;9;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;531;-9088,-3072;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;532;-7296,-2176;Inherit;False;randomOffset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;533;-9088,-2944;Inherit;False;532;randomOffset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;540;-7552,-1920;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;61;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;62;0,0;Float;False;True;-1;2;UnityEditor.ShaderGraphLitGUI;0;12;Vefects/SH_Vefects_VFX_URP_Puddle_01;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;21;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;True;True;0;True;_Cull;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;True;_ZWrite;True;3;True;_ZTest;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;True;True;1;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;1;True;_ZWrite;True;3;True;_ZTest;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;;0;0;Standard;45;Lighting Model;0;0;Workflow;0;638760135066821991;Surface;1;638760136525985827;  Refraction Model;0;0;  Blend;0;0;Two Sided;1;0;Alpha Clipping;0;638760136595984769;  Use Shadow Threshold;0;0;Fragment Normal Space,InvertActionOnDeselection;0;0;Forward Only;0;0;Transmission;0;0;  Transmission Shadow;0.5,False,;0;Translucency;0;0;  Translucency Strength;1,False,;0;  Normal Distortion;0.5,False,;0;  Scattering;2,False,;0;  Direct;0.9,False,;0;  Ambient;0.1,False,;0;  Shadow;0.5,False,;0;Cast Shadows;0;638760151794615394;Receive Shadows;1;0;Receive SSAO;1;0;Motion Vectors;1;0;  Add Precomputed Velocity;0;0;GPU Instancing;1;0;LOD CrossFade;1;0;Built-in Fog;1;0;_FinalColorxAlpha;0;0;Meta Pass;1;0;Override Baked GI;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Write Depth;0;0;  Early Z;0;0;Vertex Position,InvertActionOnDeselection;1;0;Debug Display;0;0;Clear Coat;0;0;0;11;False;True;False;True;True;True;True;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;63;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;64;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;True;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;65;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;66;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;True;1;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;False;False;True;1;True;;True;3;True;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;67;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthNormals;0;6;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormals;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;68;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;GBuffer;0;7;GBuffer;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;True;1;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;True;;True;3;True;;True;True;0;False;;0;False;;True;1;LightMode=UniversalGBuffer;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;69;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;SceneSelectionPass;0;8;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;70;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ScenePickingPass;0;9;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;541;0,100;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;MotionVectors;0;10;MotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Lit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;False;False;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=MotionVectors;False;False;0;;0;0;Standard;0;False;0
WireConnection;519;0;517;0
WireConnection;519;1;518;0
WireConnection;525;1;519;0
WireConnection;525;0;517;0
WireConnection;520;0;525;0
WireConnection;520;1;515;0
WireConnection;524;0;520;0
WireConnection;521;0;524;0
WireConnection;521;1;516;0
WireConnection;365;0;363;0
WireConnection;365;1;521;0
WireConnection;526;3;365;2
WireConnection;528;3;365;1
WireConnection;527;0;528;0
WireConnection;527;1;526;0
WireConnection;367;0;527;0
WireConnection;367;1;368;0
WireConnection;364;0;367;0
WireConnection;362;0;352;0
WireConnection;362;1;369;0
WireConnection;345;1;362;0
WireConnection;345;6;353;0
WireConnection;345;7;354;0
WireConnection;361;1;356;0
WireConnection;361;6;359;0
WireConnection;361;7;360;0
WireConnection;534;0;529;3
WireConnection;349;0;345;0
WireConnection;357;0;361;0
WireConnection;381;1;377;0
WireConnection;381;6;380;0
WireConnection;381;7;376;0
WireConnection;355;0;349;0
WireConnection;358;0;357;0
WireConnection;378;0;381;0
WireConnection;535;0;529;4
WireConnection;539;0;538;0
WireConnection;539;1;373;0
WireConnection;178;7;177;0
WireConnection;178;6;176;0
WireConnection;178;4;175;0
WireConnection;370;0;355;0
WireConnection;370;1;358;0
WireConnection;382;0;539;0
WireConnection;379;0;378;0
WireConnection;184;0;178;0
WireConnection;36;0;38;0
WireConnection;389;1;385;0
WireConnection;389;6;380;0
WireConnection;389;7;376;0
WireConnection;372;0;379;0
WireConnection;372;1;539;0
WireConnection;372;2;382;0
WireConnection;371;0;370;0
WireConnection;537;0;536;0
WireConnection;537;1;374;0
WireConnection;170;2;168;0
WireConnection;170;0;167;0
WireConnection;170;1;166;0
WireConnection;181;0;184;0
WireConnection;59;0;36;0
WireConnection;383;0;537;0
WireConnection;386;0;389;0
WireConnection;390;0;371;0
WireConnection;390;1;372;0
WireConnection;172;0;170;0
WireConnection;375;0;386;0
WireConnection;375;1;537;0
WireConnection;375;2;383;0
WireConnection;391;0;390;0
WireConnection;169;0;180;0
WireConnection;169;1;187;0
WireConnection;183;0;172;0
WireConnection;393;0;391;0
WireConnection;393;1;375;0
WireConnection;171;0;169;0
WireConnection;392;0;393;0
WireConnection;173;0;171;0
WireConnection;173;1;185;0
WireConnection;397;0;392;0
WireConnection;174;0;173;0
WireConnection;394;0;174;0
WireConnection;394;1;396;0
WireConnection;395;0;394;0
WireConnection;37;0;29;4
WireConnection;37;1;395;0
WireConnection;179;0;178;0
WireConnection;20;0;22;0
WireConnection;20;1;19;0
WireConnection;22;0;21;0
WireConnection;23;0;20;0
WireConnection;39;0;333;0
WireConnection;39;1;23;0
WireConnection;18;0;141;0
WireConnection;19;0;17;0
WireConnection;19;1;205;0
WireConnection;198;7;197;0
WireConnection;198;6;196;0
WireConnection;198;4;195;0
WireConnection;200;0;198;0
WireConnection;205;0;203;0
WireConnection;205;1;204;0
WireConnection;205;2;200;0
WireConnection;84;0;80;0
WireConnection;90;0;86;0
WireConnection;186;0;37;0
WireConnection;323;0;324;0
WireConnection;324;0;325;0
WireConnection;331;0;39;0
WireConnection;331;1;332;0
WireConnection;333;0;26;0
WireConnection;333;1;334;0
WireConnection;17;3;18;0
WireConnection;199;0;198;0
WireConnection;347;0;348;0
WireConnection;347;1;346;0
WireConnection;347;2;392;0
WireConnection;398;0;347;0
WireConnection;419;0;420;0
WireConnection;419;1;531;0
WireConnection;418;0;429;0
WireConnection;418;1;417;0
WireConnection;421;0;418;0
WireConnection;400;0;401;0
WireConnection;400;1;474;0
WireConnection;403;0;401;0
WireConnection;403;1;492;0
WireConnection;404;0;401;0
WireConnection;404;1;507;0
WireConnection;405;0;403;2
WireConnection;405;1;404;2
WireConnection;406;0;405;0
WireConnection;406;1;409;0
WireConnection;407;0;402;0
WireConnection;407;1;409;0
WireConnection;410;2;407;0
WireConnection;411;2;406;0
WireConnection;346;0;410;0
WireConnection;346;1;411;0
WireConnection;402;0;400;2
WireConnection;402;1;403;2
WireConnection;438;0;437;0
WireConnection;438;1;433;0
WireConnection;446;0;438;0
WireConnection;446;1;449;0
WireConnection;447;0;446;0
WireConnection;447;1;448;0
WireConnection;448;0;449;0
WireConnection;440;0;439;0
WireConnection;440;1;445;0
WireConnection;441;0;440;0
WireConnection;441;1;442;0
WireConnection;454;0;468;0
WireConnection;455;0;468;0
WireConnection;469;0;455;0
WireConnection;470;0;469;0
WireConnection;470;1;471;0
WireConnection;472;0;454;0
WireConnection;472;1;470;0
WireConnection;473;0;472;0
WireConnection;473;1;475;0
WireConnection;474;0;473;0
WireConnection;474;1;414;0
WireConnection;475;1;480;0
WireConnection;480;0;479;0
WireConnection;479;0;478;0
WireConnection;479;1;483;0
WireConnection;442;0;445;0
WireConnection;434;1;432;0
WireConnection;433;0;432;0
WireConnection;435;0;434;0
WireConnection;435;1;436;0
WireConnection;450;0;435;0
WireConnection;450;1;453;0
WireConnection;451;0;450;0
WireConnection;451;1;452;0
WireConnection;452;0;453;0
WireConnection;468;2;447;0
WireConnection;484;0;498;0
WireConnection;485;0;498;0
WireConnection;486;0;485;0
WireConnection;488;0;486;0
WireConnection;488;1;487;0
WireConnection;489;0;484;0
WireConnection;489;1;488;0
WireConnection;490;0;489;0
WireConnection;490;1;493;0
WireConnection;492;0;490;0
WireConnection;492;1;491;0
WireConnection;493;1;495;0
WireConnection;495;0;496;0
WireConnection;496;0;494;0
WireConnection;496;1;497;0
WireConnection;498;2;441;0
WireConnection;499;0;513;0
WireConnection;500;0;513;0
WireConnection;501;0;500;0
WireConnection;503;0;501;0
WireConnection;503;1;502;0
WireConnection;504;0;499;0
WireConnection;504;1;503;0
WireConnection;505;0;504;0
WireConnection;505;1;508;0
WireConnection;507;0;505;0
WireConnection;507;1;506;0
WireConnection;508;1;510;0
WireConnection;510;0;511;0
WireConnection;511;0;509;0
WireConnection;511;1;512;0
WireConnection;513;2;451;0
WireConnection;444;0;443;0
WireConnection;482;0;481;0
WireConnection;138;0;137;0
WireConnection;138;1;137;0
WireConnection;138;2;139;0
WireConnection;136;0;399;0
WireConnection;136;1;138;0
WireConnection;140;0;136;0
WireConnection;522;0;520;0
WireConnection;523;0;520;0
WireConnection;431;3;419;2
WireConnection;429;0;430;0
WireConnection;429;1;431;0
WireConnection;430;3;419;1
WireConnection;366;0;365;5
WireConnection;424;0;423;0
WireConnection;424;1;426;0
WireConnection;531;0;424;0
WireConnection;531;1;533;0
WireConnection;532;0;540;1
WireConnection;62;0;39;0
WireConnection;62;1;79;0
WireConnection;62;2;331;0
WireConnection;62;9;85;0
WireConnection;62;4;91;0
WireConnection;62;6;186;0
ASEEND*/
//CHKSM=88829181FCB4EF2A9110640259A5EDDCDA6AD855