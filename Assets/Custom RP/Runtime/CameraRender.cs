using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	ScriptableRenderContext context;
	Camera camera;

	const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer { name = bufferName };

	CullingResults cullingResults;

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();

	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
	{
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();

		if (!Cull()) 
		{
			return;
		}

		Setup();

		// Lightingʵ��, �ڻ��ƿɼ�������֮ǰ����������lighting��
		lighting.Setup(context, cullingResults);

		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		// �÷�����CameraRender.Editor�ű��С�CameraRender��CameraRender.Editor��ͬһ���࣬�ֿ��������ű���д��ͨ��partial�ؼ��֡�
		DrawUnsupportedShaders();
		DrawGizmos();
		Submit();
	}

	bool Cull()
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
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
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		
		context.DrawSkybox(camera);

		sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
		drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
		filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
    }

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}
