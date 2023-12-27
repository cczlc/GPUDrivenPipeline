using UnityEngine;
using UnityEngine.Rendering;

public class HizGenerator
{
    int hizDepthTextureSize {
        get {
            return Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
        }
    }

    Material hizGeneratorMat;

    public HizGenerator(Material hizGeneratorMat)
    {
        this.hizGeneratorMat = hizGeneratorMat;
    }

    public void HizGenerate(RenderTargetIdentifier depthTextureID, ref RenderTexture hizDepthTexture, ref CommandBuffer buffer)
    {
        CheckAndInit(ref hizDepthTexture);

        int w = hizDepthTexture.width;
        int mipmapLevel = 0;

        RenderTexture currentRenderTexture = null;//当前mipmapLevel对应的mipmap
        RenderTexture preRenderTexture = null;//上一层的mipmap，即mipmapLevel-1对应的mipmap

        while (w > 8)
        {
            currentRenderTexture = RenderTexture.GetTemporary(w, w, 0, RenderTextureFormat.RHalf);
            currentRenderTexture.filterMode = FilterMode.Point;
            if (preRenderTexture == null)
            {
                // Mipmap[0]即copy原始的深度图
                // buffer.CopyTexture(depthTextureID, currentRenderTexture);
                buffer.Blit(depthTextureID, currentRenderTexture);
            }
            else
            {
                // 将Mipmap[i] Blit到Mipmap[i+1]上
                buffer.Blit(preRenderTexture, currentRenderTexture, hizGeneratorMat);
                RenderTexture.ReleaseTemporary(preRenderTexture);
            }
            buffer.CopyTexture(currentRenderTexture, 0, 0, hizDepthTexture, 0, mipmapLevel);
            preRenderTexture = currentRenderTexture;

            w /= 2;
            mipmapLevel++;
        }
        RenderTexture.ReleaseTemporary(preRenderTexture);
    }

    void CheckAndInit(ref RenderTexture hizDepthTexture)
    {
        if (hizDepthTexture != null) return;

        hizDepthTexture = new RenderTexture(hizDepthTextureSize, hizDepthTextureSize, 0, RenderTextureFormat.RHalf);
        hizDepthTexture.autoGenerateMips = false;
        hizDepthTexture.useMipMap = true;
        hizDepthTexture.filterMode = FilterMode.Point;
        hizDepthTexture.Create();
    }
}
