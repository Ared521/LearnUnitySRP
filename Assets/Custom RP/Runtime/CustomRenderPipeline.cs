using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline 
{
	bool useDynamicBatching, useGPUInstancing;

	ShadowSettings shadowSettings;

	public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
	{
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

		this.shadowSettings = shadowSettings;

		// 在Lighting.cs的SetupDirectionalLight()中计算灯光的最终颜色，最终颜色已经提供了灯光的强度，
		// 但是Unity不会把它转换到线性空间里。我们必须设置 GraphicsSettings.lightsUseLinearIntensity 为真。
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

	CameraRenderer renderer = new CameraRenderer();
	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		int cameraNum = 0;
		foreach(Camera camera in cameras) 
		{
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
			//Debug.Log("第" + cameraNum + "个：" + camera + " 总个数：" + cameras.Length);
			cameraNum++;
		}
	}
}
