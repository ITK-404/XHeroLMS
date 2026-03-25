using UnityEngine;

public static class TextureExtension
{
    public static Texture2D Resize(this Texture2D source, int targetSize)
    {
        return source.ResizeKeepAspect(targetSize);
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
    
    public static Texture2D ResizeKeepAspect(this Texture2D source, int targetSize)
    {
        float ratio = (float)source.width / source.height;

        int newWidth, newHeight;

        if (source.width > source.height)
        {
            newWidth = targetSize;
            newHeight = Mathf.RoundToInt(targetSize / ratio);
        }
        else
        {
            newHeight = targetSize;
            newWidth = Mathf.RoundToInt(targetSize * ratio);
        }

        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}