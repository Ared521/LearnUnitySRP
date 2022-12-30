// NOT GPU Instance
/* #ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)
	float4 _BaseColor;
CBUFFER_END

float4 UnlitPassVertex (float3 position : POSITION) : SV_POSITION {
	float3 positionWS = TransformObjectToWorld(position.xyz);
	return TransformWorldToHClip(positionWS);
}

float4 UnlitPassFragment () : SV_TARGET {
	return _BaseColor;
}

#endif
*/


// GPU Instance
#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"


TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	//float4 _BaseColor;
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// appdata
struct Attributes {
	float3 position : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normal : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

// v2f
struct Varyings {
	float4 pos : SV_POSITION;
	float3 pos_World : VAR_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float3 normal : VAR_NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Vertex Shader
Varyings LitPassVertex (Attributes input) { //: SV_POSITION {
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	output.pos_World = TransformObjectToWorld(input.position);
	output.pos = TransformWorldToHClip(output.pos_World);
	output.normal = TransformObjectToWorldNormal(input.normal);

	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	output.baseUV = input.baseUV * baseST.xy + baseST.zw;
	return output;
}

// Fragment Shader
float4 LitPassFragment (Varyings input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 base =  baseMap * baseColor;
	#if defined(_CLIPPING)
		clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#endif
	// base.rgb = normalize(input.normal);

	Surface surface;
	surface.normal = normalize(input.normal);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.pos_World);

	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
	surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);

	// 针对玻璃这种透明材质
	#if defined(_PREMULTIPLY_ALPHA)
		BRDF brdf = GetBRDF(surface, true);
	#else
		BRDF brdf = GetBRDF(surface);
	#endif

	float3 color = GetLighting(surface, brdf);
	return float4(color, surface.alpha);
}

#endif
