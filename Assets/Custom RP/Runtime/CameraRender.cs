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
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		// 该方法在CameraRender.Editor脚本中。CameraRender和CameraRender.Editor是同一个类，分开在两个脚本中写，通过partial关键字。
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
