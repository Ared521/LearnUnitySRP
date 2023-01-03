using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{	
	// 定义以下变量能够在渲染管线Inspector面板显示参数。

	/// <summary>
	/// 批处理batch模式
	/// </summary>
	[SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

	/// <summary>
	/// shadows 参数
	/// </summary>
	[SerializeField]
	ShadowSettings shadows = default;

	/// <summary>
	/// 创建渲染管线实例，传递批处理模式和ShadowSettings
	/// </summary>
	/// <returns></returns>
	protected override RenderPipeline CreatePipeline()
	{
		return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
	}
}
