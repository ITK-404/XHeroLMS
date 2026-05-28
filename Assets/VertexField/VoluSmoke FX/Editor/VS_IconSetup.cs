using UnityEngine;
using UnityEditor;
using System.IO;

namespace VertexField.VoluSmokeFX
{
    [InitializeOnLoad]
    public static class VoluSmokeIconSetup
    {
        private static readonly string iconFolder = "Assets/VertexField/VoluSmoke FX/Editor/Icons";
        private static Texture2D s_CachedGizmoIcon;
        private static Texture2D s_TransparentIcon;

        static VoluSmokeIconSetup()
        {

            EditorApplication.delayCall += () =>
            {
                SetScriptIcon();
                LoadIcons();

                RefreshAllVoluSmokeIcons();


                Selection.selectionChanged += RefreshAllVoluSmokeIcons;
                EditorApplication.hierarchyChanged += RefreshAllVoluSmokeIcons;
                EditorApplication.playModeStateChanged += _ => RefreshAllVoluSmokeIcons();
            };
        }

        private static void LoadIcons()
        {
            if (s_CachedGizmoIcon == null)
            {
                string iconPath = Path.Combine(iconFolder, "VoluSmokeGizmoIcon.png");
                s_CachedGizmoIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            }

            if (s_TransparentIcon == null)
            {

                s_TransparentIcon = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "VF_TransparentIcon",
                    hideFlags = HideFlags.HideAndDontSave
                };
                s_TransparentIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
                s_TransparentIcon.Apply(false, true);
            }
        }

        private static void RefreshAllVoluSmokeIcons()
        {

            if (s_CachedGizmoIcon == null || s_TransparentIcon == null)
                LoadIcons();


            var voluSmokes = Object.FindObjectsByType<VoluSmokeMeshGenerator>(FindObjectsSortMode.None);
            if (voluSmokes == null || voluSmokes.Length == 0) return;

            var selected = Selection.gameObjects;

            foreach (var voluSmoke in voluSmokes)
            {
                if (voluSmoke == null) continue;
                var go = voluSmoke.gameObject;
                if (go == null) continue;

                bool isSelected = System.Array.Exists(selected, s => s == go);



                var targetIcon = isSelected ? s_TransparentIcon : s_CachedGizmoIcon;



                EditorGUIUtility.SetIconForObject(go, targetIcon);
            }
        }

        private static void SetScriptIcon()
        {
            string iconPath = Path.Combine(iconFolder, "VoluSmokeScriptIcon.png");
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

            if (icon == null)
            {
                Debug.LogWarning($"VoluSmokeScriptIcon.png not found at {iconPath}");
                return;
            }


            string[] guids = AssetDatabase.FindAssets("t:MonoScript VoluSmokeMeshGenerator");
            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

                if (script != null)
                {

                    EditorGUIUtility.SetIconForObject(script, icon);
                    EditorUtility.SetDirty(script);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }


    public class VoluSmokeIconImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.Contains("VoluSmoke FX/Editor/Icons"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.GUI;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 64;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
