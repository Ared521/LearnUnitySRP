using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �������Ⱦ��
/// </summary>
public partial class CameraRenderer
{
	ScriptableRenderContext context;
	Camera camera;

	const string bufferName = "Render Camera";

	/// <summary>
	/// �����������Ⱦ����(����պ�)�������������Ⱦ�����ȴ洢��command buffer�С�
	/// </summary>
	CommandBuffer buffer = new CommandBuffer { name = bufferName };

	CullingResults cullingResults;

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();

	// ����ÿ������������ô˺���������Ⱦ�����������ĳ�����
	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;

		// ����buffer��FrameDubugger�е�Ϊ����������������֡�
		// ����buffer���ֵ��������ڿ�����FrameDubugger�н���ͬ���������Ⱦ��DrawCall�ֿ����Ա����Ǹ���������Ĳ鿴ÿ���������Ⱦ����Щ��
		PrepareBuffer();

		// Emit UI geometry into the Scene view for rendering.
		PrepareForSceneWindow();

		// ������޳����������ݵĲ�����Ϊ��������׶�巶Χ�ڵ���Ӱ��Χ��
		if (!Cull(shadowSettings.maxDistance)) 
		{
			return;
		}

		/* 1��������ҪProfile��Profile��Unity�ṩ��һ�����ܷ������ߡ�
		����д�����׵�BeginSample��EndSample��ExecuteCommandBuffer��Ϊ�˷�����command��CPU��GPU�ĺ�ʱ�������������Щ����д���������׵Ĵ��룬����Ҳ�����ܵ�Ӱ�졣
		Unity�������������������Ľ���: This makes it easy to measure the CPU and GPU time spent by one or more commands in the command buffer.*/
		/*	2����BeginSample��EndSample�У����ִ��buffer.Draw��صĴ��룬������FrameDebugger�и��ݸ���buffer��name�鿴��Ӧ�Ļ�����Ϣ��*/

		buffer.BeginSample(SampleName);
		ExecuteBuffer();

		// Lightingʵ��, �ڻ��ƿɼ�������֮ǰ����������lighting��
		// д��buffer.BeginSample(SampleName); �� buffer.EndSample(SampleName);�м��������: ����Ӱ��ĿǶ����֡�������е�������ڲ���
		lighting.Setup(context, cullingResults, shadowSettings);
		
		buffer.EndSample(SampleName);

		// 1���������VP����������Scene������ܹ���ת�ӽǣ��Լ�����һЩ���ԡ�
		// 2����Ⱦÿһ֡ʱ����Ҫ����������RenderTarget��Ĭ��Ϊ֡��������Ҳ����ָ���������������Ϊ��Ⱥ���ɫ��
		Setup();

		// ���ƿɼ����塣
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

		// �÷�����CameraRender.Editor�ű��С�CameraRender��CameraRender.Editor��ͬһ���࣬�ֿ��������ű���д��ͨ��partial�ؼ��֡�
		DrawUnsupportedShaders();
		
		// ��Scene�л������������׶�巶Χ�İ�ɫ���ߡ�
		DrawGizmos();
		
		// Ŀǰ���ͷ�shadow��RenderTexture�����shadow��command buffer��
		lighting.Cleanup();

		// �����Ľṹ�У���Ⱦ��ص����ݶ��Ѿ�������ϣ���ʱ����ҪSubmit�ύ�Թ�ִ�С�
		Submit();
	}

	bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{	
			// ��Ӱ���벻�ܳ����������׶���Զƽ�档
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void Setup()
	{
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;
        //buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,
                                    flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
		var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		// Ҫ��Ⱦʹ�����pass�Ķ������Ǳ��������ӽ�CameraRenderer��,����Tags��ʶ����
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		// ����
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		
		// ������պ�
		context.DrawSkybox(camera);

		sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
		drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
		// ���������ӵĻ�����Ⱦ�������ó�transparent�����岻����ʾ������opaqueͬ��
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
		// ����
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
    }

	/// <summary>
	/// 1��ִ�л����������ӻ��������������������������ִ�������ҪClear��2���������Ϊÿ��ִ������������ͻ�Ѵ��ݵ�command buffer����context�ж����У��ȴ�����context.Submit()��
	/// </summary>
	void ExecuteBuffer()
	{
		// �����������ķ����������ǻ���ġ�
		context.ExecuteCommandBuffer(buffer);
		// ��Ϊ֮��buffer��Ҫ��ű�����ݣ������ڷ�����֮��buffer��ҪClear��
		buffer.Clear();
	}
}
