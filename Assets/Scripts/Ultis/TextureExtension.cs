using UnityEngine;

public static class TextureExtension
{
    public static Texture2D Resize(this Texture2D source, int targetSize)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetSize, targetSize);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}