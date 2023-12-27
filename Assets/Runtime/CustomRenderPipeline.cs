using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing;

    CameraRender cameraRender;

    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, InstanceSetting instanceSetting, Material hizGeneratorMat, Shader cameraRenderShader)
    {
        this.useDynamicBatching = useDynamicBatching; 
        this.useGPUInstancing = useGPUInstancing;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;

        cameraRender = new CameraRender(instanceSetting, hizGeneratorMat, cameraRenderShader);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        throw new System.NotImplementedException();
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            cameraRender.Render(context, cameras[i], useDynamicBatching, useGPUInstancing);
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        cameraRender.Dispose();
    }
}
