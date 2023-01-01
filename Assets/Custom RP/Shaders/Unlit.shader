Shader "Custom RP/Unlit" {
	
	Properties {
		_BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BaseMap("Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
	}
	
	SubShader {
		
		Pass {

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5

			// GPU Instance
			#pragma multi_compile_instancing
			
			#pragma shader_feature _CLIPPING

			#pragma shader_feature _PREMULTIPLY_ALPHA
		
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"

			ENDHLSL
		}

		Pass {

			// 光照模式设置为ShadowCaster
			Tags { "LightMode" = "ShadowCaster" }

			// 只需要写深度，禁用颜色功能
			ColorMask 0

			HLSLPROGRAM
			// 使用相同的目标级别
			#pragma target 3.5
			// _CLIPPING着色器功能
			// #pragma shader_feature _CLIPPING
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			// 提供实例化支持
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment

			// 使用特殊的阴影投射器功能，这些功能将在下面新创建的文件中定义
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	// 这告诉Unity编辑器使用CustomShaderGUI类的实例来绘制使用Lit着色器的材质的检查器。CustomShaderGUI.cs脚本在Custom RP / Editor文件夹。
	CustomEditor "CustomShaderGUI"
}