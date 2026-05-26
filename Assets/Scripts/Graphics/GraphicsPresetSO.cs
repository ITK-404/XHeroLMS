using UnityEngine;

[CreateAssetMenu(fileName = "Graphics_Preset_", menuName = "SO/Graphics Preset")]
public class GraphicsPresetSO : ScriptableObject
{
    public string presetName;
    public GraphicsConfigData config;
}