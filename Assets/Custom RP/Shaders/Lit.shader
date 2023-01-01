Shader "Custom RP/Lit" {

	Properties {
		_BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BaseMap("Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows(can cast shadow)", Float) = 1
	}
	
	SubShader {
		
		Pass {
			Tags { "LightMode" = "CustomLit" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5

			#pragma shader_feature _RECEIVE_SHADOWS
		
			// GPU Instance
			#pragma multi_compile_instancing
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			
			#pragma shader_feature _CLIPPING

			// #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma shader_feature _PREMULTIPLY_ALPHA

			// #pragma instance_options assumeuniformscaling
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"

			ENDHLSL
		}

		Pass {

			// ����ģʽ����ΪShadowCaster
			Tags { "LightMode" = "ShadowCaster" }

			// ֻ��Ҫд��ȣ�������ɫ����
			ColorMask 0

			HLSLPROGRAM
			// ʹ����ͬ��Ŀ�꼶��
			#pragma target 3.5
			// _CLIPPING��ɫ������
			// #pragma shader_feature _CLIPPING
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			// �ṩʵ����֧��
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment

			// ʹ���������ӰͶ�������ܣ���Щ���ܽ��������´������ļ��ж���
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	// �����Unity�༭��ʹ��CustomShaderGUI���ʵ��������ʹ��Lit��ɫ���Ĳ��ʵļ������CustomShaderGUI.cs�ű���Custom RP / Editor�ļ��С�
	CustomEditor "CustomShaderGUI"
}