using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 摄像机渲染器
/// </summary>
public partial class CameraRenderer
{
	ScriptableRenderContext context;
	Camera camera;

	const string bufferName = "Render Camera";

	/// <summary>
	/// 除了特殊的渲染命令(如天空盒)，其他物体的渲染都会先存储在command buffer中。
	/// </summary>
	CommandBuffer buffer = new CommandBuffer { name = bufferName };

	CullingResults cullingResults;

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();

	// 对于每个摄像机，调用此函数进行渲染该摄像机拍摄的场景。
	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;

		// 设置buffer、FrameDubugger中的为场景中摄像机的名字。
		// 设置buffer名字的意义在于可以在FrameDubugger中将不同的摄像机渲染的DrawCall分开，以便我们更清晰方便的查看每个摄像机渲染了哪些。
		PrepareBuffer();

		// Emit UI geometry into the Scene view for rendering.
		PrepareForSceneWindow();

		// 摄像机剔除操作，传递的参数是为了设置视锥体范围内的阴影范围。
		if (!Cull(shadowSettings.maxDistance)) 
		{
			return;
		}

		/* 1、首先需要Profile，Profile是Unity提供的一款性能分析工具。
		我们写了配套的BeginSample、EndSample和ExecuteCommandBuffer是为了分析该command的CPU与GPU的耗时，如果不关心这些，不写这两行配套的代码，场景也不会受到影响。
		Unity官网对于这两个函数的解释: This makes it easy to measure the CPU and GPU time spent by one or more commands in the command buffer.*/
		/*	2、在BeginSample和EndSample中，如果执行buffer.Draw相关的代码，可以在FrameDebugger中根据赋予buffer的name查看相应的绘制信息。*/

		buffer.BeginSample(SampleName);
		ExecuteBuffer();

		// Lighting实例, 在绘制可见几何体之前用它来设置lighting。
		// 写在buffer.BeginSample(SampleName); 和 buffer.EndSample(SampleName);中间的意义是: 将阴影条目嵌套在帧调试器中的摄像机内部。
		lighting.Setup(context, cullingResults, shadowSettings);
		
		buffer.EndSample(SampleName);

		// 1、设置相机VP矩阵，这样在Scene面板中能够旋转视角，以及其他一些属性。
		// 2、渲染每一帧时，需要清除摄像机的RenderTarget，默认为帧缓冲区，也可以指定纹理。清楚的内容为深度和颜色。
		Setup();

		// 绘制可见物体。
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

		// 该方法在CameraRender.Editor脚本中。CameraRender和CameraRender.Editor是同一个类，分开在两个脚本中写，通过partial关键字。
		DrawUnsupportedShaders();
		
		// 在Scene中绘制摄像机的视锥体范围的白色虚线。
		DrawGizmos();
		
		// 目前是释放shadow的RenderTexture和清空shadow的command buffer。
		lighting.Cleanup();

		// 上下文结构中，渲染相关的内容都已经更新完毕，此时就需要Submit提交以供执行。
		Submit();
	}

	bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{	
			// 阴影距离不能超过摄像机视锥体的远平面。
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
		// 要渲染使用这个pass的对象，我们必须把它添加进CameraRenderer中,利用Tags标识符。
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		// 绘制
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		
		// 绘制天空盒
		context.DrawSkybox(camera);

		sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
		drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
		// 这个如果不加的话，渲染队列设置成transparent，物体不会显示，上面opaque同理。
		drawingSettings.SetShaderPassName(1, litShaderTagId);
		filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
		// 绘制
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
    }

	/// <summary>
	/// 1、执行缓冲区，这会从缓冲区复制命令但不会清除它，因此执行完后需要Clear。2、可以理解为每次执行这个函数，就会把传递的command buffer放在context中队列中，等待最终context.Submit()。
	/// </summary>
	void ExecuteBuffer()
	{
		// 这里向上下文发出的命令是缓冲的。
		context.ExecuteCommandBuffer(buffer);
		// 因为之后buffer还要存放别的内容，所以在发送完之后，buffer需要Clear。
		buffer.Clear();
	}
}
