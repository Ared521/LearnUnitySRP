using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline 
{
	bool useDynamicBatching, useGPUInstancing;

	public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
	{
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

		// ��Lighting.cs��SetupDirectionalLight()�м���ƹ��������ɫ��������ɫ�Ѿ��ṩ�˵ƹ��ǿ�ȣ�
		// ����Unity�������ת�������Կռ�����Ǳ������� GraphicsSettings.lightsUseLinearIntensity Ϊ�档
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

	CameraRenderer renderer = new CameraRenderer();
	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		int cameraNum = 0;
		foreach(Camera camera in cameras) 
		{
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
			//Debug.Log("��" + cameraNum + "����" + camera + " �ܸ�����" + cameras.Length);
			cameraNum++;
		}
	}
}
