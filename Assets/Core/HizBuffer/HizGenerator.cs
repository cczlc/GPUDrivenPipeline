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

        RenderTexture currentRenderTexture = null;//��ǰmipmapLevel��Ӧ��mipmap
        RenderTexture preRenderTexture = null;//��һ���mipmap����mipmapLevel-1��Ӧ��mipmap

        while (w > 8)
        {
            currentRenderTexture = RenderTexture.GetTemporary(w, w, 0, RenderTextureFormat.RHalf);
            currentRenderTexture.filterMode = FilterMode.Point;
            if (preRenderTexture == null)
            {
                // Mipmap[0]��copyԭʼ�����ͼ
                // buffer.CopyTexture(depthTextureID, currentRenderTexture);
                buffer.Blit(depthTextureID, currentRenderTexture);
            }
            else
            {
                // ��Mipmap[i] Blit��Mipmap[i+1]��
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
