using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ��Ⱦ����ʵ��
/// </summary>
public class CustomRenderPipeline : RenderPipeline 
{
	bool useDynamicBatching, useGPUInstancing;

	ShadowSettings shadowSettings;
	
	/// <summary>
	/// �������Ⱦ����������Ⱦ��
	/// </summary>
	CameraRenderer renderer = new CameraRenderer();

	public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
	{
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		// ��ʹ��SRP Batcher�����뿪�����д��룬����Ϊtrue
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

		// ��Lighting.cs��SetupDirectionalLight()�м���ƹ��������ɫ��������ɫ�Ѿ��ṩ�˵ƹ��ǿ�ȣ�
		// ����Unity�������ת�������Կռ�����Ǳ������� GraphicsSettings.lightsUseLinearIntensity Ϊ�档
		GraphicsSettings.lightsUseLinearIntensity = true;
		
		// ����Ӱ���ø�ֵ������ʵ��
		this.shadowSettings = shadowSettings;
	}

	/// <summary>
	/// ��д����ʵ����Render����Ϊ�����Զ���ķ�����
	/// </summary>
	/// <param name="context">Unity����Ⱦ�����ϵ���ÿһ֡ʱ���ṩ��һ���������ص������Ľṹ������������Ⱦ</param>
	/// <param name="cameras">�������Ϊ�����е����������</param>
	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		int cameraNum = 0;
		foreach(Camera camera in cameras) 
		{
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
			//Debug.Log("��" + cameraNum + "����" + camera + " �ܸ�����" + cameras.Length);
			cameraNum++;
		}
	}
}
