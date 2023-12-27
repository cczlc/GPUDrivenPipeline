using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "GPURendering/GPURenderAssert")]
public partial class CustomRenderPipelineAssert : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true;
    bool useGPUInstancing = true;
    bool useSRPBatcher = true;

    [SerializeField]
    Shader cameraRenderShader;

    [SerializeField]
    Material hizGeneratorMat;

    [SerializeField]
    InstanceSetting instanceSetting;

    protected override RenderPipeline CreatePipeline()
    {
        if (instanceSetting.instanceCull == null && instanceSetting.instanceDatas.Length != 0) Debug.Log("instance Cull shader is null!");
        if (hizGeneratorMat == null) Debug.Log("hizGenerator material is null!");
        if (cameraRenderShader == null) Debug.Log("camRender shader is null!");

        return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, instanceSetting, hizGeneratorMat, cameraRenderShader);
    }
}

