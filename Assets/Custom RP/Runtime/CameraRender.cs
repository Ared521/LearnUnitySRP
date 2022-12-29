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

		// Lighting实例, 在绘制可见几何体之前用它来设置lighting。
		lighting.Setup(context, cullingResults);

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

		// 要渲染使用这个pass的对象，我们必须把它添加进CameraRenderer中,利用Tags标识符。
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
