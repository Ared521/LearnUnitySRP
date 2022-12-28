Shader "Custom RP/Unlit_SRP" {
	
	Properties {
		_BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BaseMap("Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	}
	
	SubShader {
		
		Pass {

		Blend [_SrcBlend] [_DstBlend]
		ZWrite [_ZWrite]

		HLSLPROGRAM
		
		// GPU Instance
		#pragma multi_compile_instancing
		#pragma vertex UnlitPassVertex
		#pragma fragment UnlitPassFragment
		#include "UnlitPass.hlsl"

		ENDHLSL
		}
	}
}