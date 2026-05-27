using System;
using UnityEditor;
using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    public static class VoluSmokePresetAssetUtility
    {
        public static void SavePreviewTexture(VoluSmokePreset preset, Texture2D source)
        {
            if (preset == null || source == null) return;

            string presetPath = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(presetPath))
            {
                Debug.LogError("Preset is not an asset on disk.");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(presetPath);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Texture2D existing)
                    {
                        if (existing == preset.previewImage || existing.name.StartsWith("Preview", StringComparison.Ordinal))
                        {
                            AssetDatabase.RemoveObjectFromAsset(existing);
                            UnityEngine.Object.DestroyImmediate(existing, true);
                        }
                    }
                }

                Texture2D texCopy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
                {
                    name = "Preview"
                };
                texCopy.SetPixels(source.GetPixels());
                texCopy.Apply();

                AssetDatabase.AddObjectToAsset(texCopy, presetPath);
                preset.previewImage = texCopy;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(presetPath, ImportAssetOptions.ForceUpdate);
        }
    }

}
