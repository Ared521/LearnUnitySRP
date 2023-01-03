using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{	
	// �������±����ܹ�����Ⱦ����Inspector�����ʾ������

	/// <summary>
	/// ������batchģʽ
	/// </summary>
	[SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

	/// <summary>
	/// shadows ����
	/// </summary>
	[SerializeField]
	ShadowSettings shadows = default;

	/// <summary>
	/// ������Ⱦ����ʵ��������������ģʽ��ShadowSettings
	/// </summary>
	/// <returns></returns>
	protected override RenderPipeline CreatePipeline()
	{
		return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
	}
}
