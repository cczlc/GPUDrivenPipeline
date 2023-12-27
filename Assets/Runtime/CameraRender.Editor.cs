using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRender
{
    partial void PrepareBuffer();
    partial void DrawUnsupportedShaders();
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();


#if UNITY_EDITOR
    // ��ȡUnityĬ�ϵ�shader tag id
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    // Error Material
    static Material errorMaterial;

    string SampleName { get; set; }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        // ��ÿ�������ʹ�ò�ͬ��Sample Name
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

    partial void DrawUnsupportedShaders()
    {
        // ʹ��Ĭ�ϵ�Error����
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        // ���Ʋ�֧�ֵ�Shader Pass������
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            // ���ø�д�Ĳ���
            overrideMaterial = errorMaterial
        };

        // ���ø����ڴ˴�DrawCall��Ҫ��Ⱦ��ShaderPass��Ҳ���ǲ�֧�ֵ�ShaderPass
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void DrawGizmosBeforeFX()
    {
        // Scene�����л���Gizmos
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        // Scene�����л���Gizmos
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

#else
    const string SampleName = bufferName;
#endif
}
