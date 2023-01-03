using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{	
	// 场景支持直接光数量
	const int maxDirLightCount = 4;

	/*static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
	static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");*/

	// shader对struct buffer的支持还不够好，要么只存在于片段程序中，要么性能比常规的数组差 。好消息是数据在CPU和GPU之间传递的细节只在少数地方需要关注。

	// 直接光数量
	static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	
	// 直接光颜色
	static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
	
	// 直接光方向
	static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
	
	// 直接光shadow数据
	static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
	static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];


	const string bufferName = "Lighting";
	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	// 摄像机视锥体剔除结果。
	CullingResults cullingResults;

	// 该直接光阴影实例。
	Shadows shadows = new Shadows();

	/// <summary>
	/// 直接光设置。
	/// </summary>
	/// <param name="context">上下文结构</param>
	/// <param name="cullingResults">视锥体剔除结果</param>
	/// <param name="shadowSettings">阴影设置</param>
	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.cullingResults = cullingResults;
		buffer.BeginSample(bufferName);
		// 阴影设置。
		shadows.Setup(context, cullingResults, shadowSettings);

		// 设置直接光相关的一系列参数。
		SetupLights();

		// 渲染阴影。
		shadows.Render();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	// 使用CommandBuffer.SetGlobalVector将灯光数据发送到GPU。颜色是灯光在线性空间中的颜色，而方向是灯光变换的前向向量求反。
	void SetupLights()
	{
		// 它是类似数组的一种结构，但提供与本地内存缓冲的连接功能。它使得在托管C代码和本机Unity引擎代码之间高效地共享数据成为可能。
		// 使用可见光数据让RP支持多个平行光成为可能，但是我们必须发送这些灯光数据给GPU。
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

		// 遍历Lighting.SetupLights中所有的可见光，并为每个元素调用SetupDirectionalLight。
		// 然后调用缓冲区上的SetGlobalInt和SetGlobalVectorArrray将数据发送给GPU。
		int dirLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			VisibleLight visibleLight = visibleLights[i];
			if (visibleLight.lightType == LightType.Directional)
			{
				// visibleLight 是结构体，这里通过传递引用，来节省内存资源。
				SetupDirectionalLight(dirLightCount++, ref visibleLight);
				if (dirLightCount >= maxDirLightCount)
				{
					break;
				}
			}
		}

		buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
		buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
		buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
	}

	void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		dirLightColors[index] = visibleLight.finalColor;
		// 前向向量可以在VisibleLight.localToWorldMatrix属性中找到。矩阵中的第三列就是，这里还是要求个反向。
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

		// 阴影设置。
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}

	public void Cleanup()
	{
		shadows.Cleanup();
	}
}