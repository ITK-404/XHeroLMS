using UnityEditor;
using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    public static class VoluSmokeMeshGeneratorContextMenu
    {
        [MenuItem("CONTEXT/VoluSmokeMeshGenerator/Overwrite Current Preset")]
        private static void OverwriteCurrentPreset(MenuCommand command)
        {
            if (!(command.context is VoluSmokeMeshGenerator generator)) return;

            var preset = generator.LastAppliedPreset;
            if (preset == null)
            {
                EditorUtility.DisplayDialog(
                    "Overwrite Current Preset",
                    "No preset is currently recorded for this VoluSmokeMeshGenerator.\n\nApply a preset from the browser or save one first.",
                    "OK");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog(
                    "Overwrite Current Preset",
                    $"The recorded preset '{preset.name}' has not been saved as an asset yet.",
                    "OK");
                return;
            }

            string message = $"Overwrite preset '{preset.presetName}' with the values currently on '{generator.name}'?\n\nAsset Path:\n{assetPath}";
            if (!EditorUtility.DisplayDialog("Overwrite Current Preset", message, "Overwrite", "Cancel"))
                return;

            Undo.RegisterCompleteObjectUndo(preset, "Overwrite VoluSmoke Preset");
            preset.SaveFromGenerator(generator, preset.presetName);

            var preview = VoluSmokePreviewCapture.CapturePreview(generator, 256);
            if (preview != null)
            {
                VoluSmokePresetAssetUtility.SavePreviewTexture(preset, preview);
                Object.DestroyImmediate(preview);
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            generator.EditorSetLastAppliedPreset(preset);

            EditorGUIUtility.PingObject(preset);
            Debug.Log($"VoluSmoke preset '{preset.presetName}' overwritten with settings from '{generator.name}'.", preset);
        }
    }

}
