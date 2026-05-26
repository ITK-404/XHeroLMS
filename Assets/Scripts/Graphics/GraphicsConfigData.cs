using System;

[System.Serializable]
public class GraphicsConfigData
{
    public int targetFPS;           // 30, 60
    public float renderScale;       // 0.75f, 0.85f, 1.0f
    public bool shadowEnabled;
    public float shadowDistance;    // 30f, 50f
    public int textureMipmapLimit;  // 0 = full, 1 = half
}

public enum GraphicsPreset { Low, Medium, High, Ultra }