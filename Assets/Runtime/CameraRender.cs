using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    const string bufferName = "Render Camera";
    ScriptableRenderContext context;
    CullingResults cullingResults;
    Camera camera;
    Texture2D missingTexture;

    // ʵ������
    InstanceDrawer instanceDrawer;
    InstanceSetting instanceSetting;

    // hizBuffer����
    HizGenerator hizGenerator;
    RenderTexture hizDepthTexture = null;
    int hizDepthTextureSize
    {
        get
        {
            return Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
        }
    }

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    // ���Ĭ������
    CameraSettings defaultCameraSettings;
    Material cameraRenderMat;
    static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);
    static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
    static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
    static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    static int sourceTextureId = Shader.PropertyToID("_SourceTexture");
    static int srcBlendId = Shader.PropertyToID("_CameraSrcBlend");
    static int dstBlendId = Shader.PropertyToID("_CameraDstBlend");

    public CameraRender(InstanceSetting instanceSetting, Material hizGeneratorMat, Shader cameraRenderShader)
    {
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };

        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);

        instanceDrawer = new InstanceDrawer(instanceSetting.instanceCull);
        this.instanceSetting = instanceSetting;
        for (int i = 0; i < instanceSetting.instanceDatas.Length; ++i)
        {
            instanceSetting.instanceDatas[i].Init();
        }

        hizGenerator = new HizGenerator(hizGeneratorMat);
        hizDepthTexture = new RenderTexture(hizDepthTextureSize, hizDepthTextureSize, 0, RenderTextureFormat.RHalf);
        hizDepthTexture.autoGenerateMips = false;
        hizDepthTexture.useMipMap = true;
        hizDepthTexture.filterMode = FilterMode.Point;
        hizDepthTexture.Create();

        defaultCameraSettings = new CameraSettings();

        this.cameraRenderMat = CoreUtils.CreateEngineMaterial(cameraRenderShader);
    }

    public void Dispose()
    {
        for (int i = 0; i < instanceSetting.instanceDatas.Length; ++i)
        {
            instanceSetting.instanceDatas[i].Release();
        }
    }

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();

        if (!Cull())
        {
            return;
        }

        Setup();

        DrawInstance();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

        if (!Handles.ShouldRenderGizmos())
        {
            GenerateHizBuffer();
        }

        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();

        // Todo��ʹ�õ���Ĭ�ϵ��������
        DrawFinal(defaultCameraSettings.finalBlendMode);

        DrawGizmosAfterFX();

        Cleanup();
        Submit();
    }

    bool Cull()
    {
        // Unityͨ��GameObject���Ƿ����Renderer������жϸ������Ƿ���Ա���Ⱦ
        // ��ȡ����������޳��Ĳ���
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Setup()
    {
        // �ѵ�ǰ���������Ϣ���������ģ�����shader�оͿ��Ի�ȡ����ǰ֡�����������Ϣ������VP�����
        // ͬʱҲ�����õ�ǰ��Render Target������ClearRenderTarget����ֱ�����Render Target�е����ݣ�������ͨ������һ��ȫ����quad���ﵽͬ��Ч�����ȽϷѣ�
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear);
        buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);

        buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        // �����ǰ�����Render Target�е�����,������Ⱥ���ɫ��ClearRenderTarget�ڲ���Begin/EndSample(buffer.name)
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);

        // �ύCommandBuffer�������������Setup������һ��������Ӧ����ȷ���ں�����CommandBuffer���ָ��֮ǰ���������ǿյġ�
        ExecuteBuffer();
    }

    void DrawInstance()
    {
        for (int i = 0; i < instanceSetting.instanceDatas.Length; ++i)
        {
            instanceDrawer.Draw(instanceSetting.instanceDatas[i], Camera.main, ref hizDepthTexture, ref buffer);
        }

        ExecuteBuffer();
    }

    // Todo������������
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("CustomDefault");   // ʹ�� LightMode Ϊ gbuffer �� shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        // ����һ�㼸����
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        CopyAttachments();
    }

    void GenerateHizBuffer()
    {
        hizGenerator.HizGenerate(depthTextureId, ref hizDepthTexture, ref buffer);
    }

    void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    {
        buffer.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
        buffer.SetGlobalTexture(sourceTextureId, colorAttachmentId);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ?
            RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, cameraRenderMat, 0, MeshTopology.Triangles, 3);
        buffer.SetGlobalFloat(srcBlendId, 1f);
        buffer.SetGlobalFloat(dstBlendId, 0f);
    }

    void Cleanup()
    {
        buffer.ReleaseTemporaryRT(colorTextureId);
        buffer.ReleaseTemporaryRT(depthAttachmentId);
        buffer.ReleaseTemporaryRT(colorTextureId);
        buffer.ReleaseTemporaryRT(depthTextureId);
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

    void CopyAttachments()
    {
        buffer.GetTemporaryRT(colorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear);
        buffer.GetTemporaryRT(depthTextureId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
        if (copyTextureSupported)
        {
            buffer.CopyTexture(colorAttachmentId, colorTextureId);
            buffer.CopyTexture(depthAttachmentId, depthTextureId);
        }
        else
        {
            Draw(colorAttachmentId, colorTextureId);
            Draw(depthAttachmentId, depthTextureId, true);
        }

        if (!copyTextureSupported)
        {
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }
        ExecuteBuffer();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(
            to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, cameraRenderMat, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }
}
