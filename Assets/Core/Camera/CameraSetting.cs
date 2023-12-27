using System;
using UnityEngine;
using UnityEngine.Rendering;

// 相机相关配置
[Serializable]
public class CameraSettings
{

    public bool copyColor = true, copyDepth = true;

    public bool maskLights = false;

    public enum RenderScaleMode { Inherit, Multiply, Override }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;

    public bool overridePostFX = false;

    public bool allowFXAA = false;

    public bool keepAlpha = false;

    [Serializable]
    public struct FinalBlendMode
    {

        public BlendMode source, destination;
    }

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.Zero
    };
}