using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
	int ShadowedDirectionalLightCount;

	const int maxShadowedDirectionalLightCount = 4;
	const int maxCascades = 4;

	static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};

	static string[] cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};

	static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
	static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
	static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	static int cascadeDataId = Shader.PropertyToID("_CascadeData");

	// PCF图集大小
	static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

	// 最远处的阴影加一个平滑过渡
	static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");


	static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

	static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

	static Vector4[] cascadeData = new Vector4[maxCascades];

	struct ShadowedDirectionalLight
	{
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float nearPlaneOffset;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	ScriptableRenderContext context;

	CullingResults cullingResults;

	ShadowSettings settings;


	Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
	{
		// 反向Z buffer，此时1代表近平面，0代表远平面。OpenGL就是这样做的。
		if (SystemInfo.usesReversedZBuffer)
		{
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}

		// clip space is defined inside a cube with with coordinates going from −1 to 1,
		// But, textures coordinates and depth go from zero to one.
		// Also, apply the tile offset and scale.
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;

		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);

		return m;
	}

	public void Render()
	{
		if (ShadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
	}

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;

		ShadowedDirectionalLightCount = 0;
	}

	public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None 
											&& light.shadowStrength > 0f && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
		{
			ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex, slopeScaleBias = light.shadowBias, nearPlaneOffset = light.shadowNearPlane };
			return new Vector3(light.shadowStrength, settings.directional.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias);
		}
		
		return Vector3.zero;
	}

	void RenderDirectionalShadows() 
	{
		int atlasSize = (int)settings.directional.atlasSize;
		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		// 只关心深度缓冲区。
		buffer.ClearRenderTarget(true, false, Color.clear);

		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		/* 后面除以的系数是2，而不是跟maxShadowedDirectionalLightCount = 4，是因为这里相当于将一个正方形切割成四个正方形。
		故每个正方形的size是之前的二分之一，所以后面是2，不是4。
		int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;*/

		// 使用了类似mipmap的级联(level 4)阴影贴图，所以除了4
		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;

		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i, split, tileSize);
		}

		buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
		buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);

		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);

		float f = 1f - settings.directional.cascadeFade;
		buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

		SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
		SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
		buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));

		buffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	void RenderDirectionalShadows(int index, int split, int tileSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

		int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;

		float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

		// 对于级联纹理贴图，因此需要循环多次DrawShadows
		for (int i = 0; i < cascadeCount; i++)
		{
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);


			splitData.shadowCascadeBlendCullingFactor = cullingFactor;

			shadowSettings.splitData = splitData;

			if (index == 0)
			{
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}

			int tileIndex = tileOffset + i;
			dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

			// 深度偏差Depth Bias去除自阴影，另一种是斜率比例偏差slope-scale bias: buffer.SetGlobalDepthBias(0f, 3f);
			// buffer.SetGlobalDepthBias(5000f, 0f);

			buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
			context.DrawShadows(ref shadowSettings);
			buffer.SetGlobalDepthBias(0f, 0f);
		}
	}

	void SetKeywords(string[] keywords, int enabledIndex)
	{
		for (int i = 0; i < keywords.Length; i++)
		{
			if (i == enabledIndex)
			{
				buffer.EnableShaderKeyword(keywords[i]);
			}
			else
			{
				buffer.DisableShaderKeyword(keywords[i]);
			}
		}
	}

	void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
	{
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)settings.directional.filter + 1f);

		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;
		cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}

	Vector2 SetTileViewport(int index, int split, float tileSize)
	{
		Vector2 offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
		return offset;
	}

	public void Cleanup()
	{
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}