using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public partial class CameraRenderer
{

	partial void DrawUnsupportedShaders();
	partial void DrawGizmos();
	partial void PrepareForSceneWindow();
	partial void PrepareBuffer();

// Editorģʽ�£���Releaseģʽ�Ĳ�ͬ��: �����ʴ���Editorģʽ�»���ʾ�۷ۣ�Releaseģʽ�²�����ʾ����Ĳ���(�۷�)��
#if UNITY_EDITOR || DEVELOPMENT_BUILD

	string SampleName { get; set; }

	static Material errorMaterial;

	// ��������Unity��legacy shaders
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	partial void DrawUnsupportedShaders()
	{
		if (errorMaterial == null)
		{
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };
		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}
		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	partial void DrawGizmos()
	{
		// Handles.ShouldRenderGizmos(): check whether gizmos should be drawn
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	partial void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	partial void PrepareBuffer()
	{
		buffer.name = SampleName = camera.name;
	}
#else
	const string SampleName = bufferName;
#endif
}
