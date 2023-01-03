using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 渲染管线实例
/// </summary>
public class CustomRenderPipeline : RenderPipeline 
{
	bool useDynamicBatching, useGPUInstancing;

	ShadowSettings shadowSettings;
	
	/// <summary>
	/// 摄像机渲染器，核心渲染类
	/// </summary>
	CameraRenderer renderer = new CameraRenderer();

	public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
	{
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		// 若使用SRP Batcher，必须开启下行代码，设置为true
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

		// 在Lighting.cs的SetupDirectionalLight()中计算灯光的最终颜色，最终颜色已经提供了灯光的强度，
		// 但是Unity不会把它转换到线性空间里。我们必须设置 GraphicsSettings.lightsUseLinearIntensity 为真。
		GraphicsSettings.lightsUseLinearIntensity = true;
		
		// 将阴影设置赋值给管线实例
		this.shadowSettings = shadowSettings;
	}

	/// <summary>
	/// 重写管线实例的Render方法为我们自定义的方法。
	/// </summary>
	/// <param name="context">Unity在渲染管线上调用每一帧时，提供的一个与管线相关的上下文结构，将其用于渲染</param>
	/// <param name="cameras">可以理解为场景中的摄像机数组</param>
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
