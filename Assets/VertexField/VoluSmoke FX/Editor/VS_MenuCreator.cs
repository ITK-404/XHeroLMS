using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace VertexField.VoluSmokeFX
{
    public static class VoluSmokeSliceMenuCreator
    {
        [MenuItem("GameObject/VertexField/VoluSmokeSliceVFX/VoluSmokeSlice", false, 10)]
        static void CreateVoluSmokeSlice(MenuCommand menuCommand)
        {

            GameObject voluSmokeObj = new GameObject("VoluSmokeSlice");
            GameObject parentContext = menuCommand.context as GameObject;

            GameObjectUtility.SetParentAndAlign(voluSmokeObj, parentContext);


            VoluSmokeMeshGenerator generator = voluSmokeObj.AddComponent<VoluSmokeMeshGenerator>();


            ApplyStartingPresetOrDefaults(generator, includeExtendedDefaults: true);


            PositionVoluSmokeSlice(generator.transform, parentContext);


            MeshRenderer renderer = voluSmokeObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }


            generator.GenerateMesh();


            Undo.RegisterCreatedObjectUndo(voluSmokeObj, "Create VoluSmokeSlice");



            Selection.activeObject = voluSmokeObj;
        }


        [MenuItem("Assets/Create/VertexField/VoluSmokeSlice Prefab", false, 81)]
        static void CreateVoluSmokeSlicePrefab()
        {

            GameObject voluSmokeObj = new GameObject("VoluSmokeSlice");


            VoluSmokeMeshGenerator generator = voluSmokeObj.AddComponent<VoluSmokeMeshGenerator>();


            ApplyStartingPresetOrDefaults(generator, includeExtendedDefaults: false);


            MeshRenderer renderer = voluSmokeObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            generator.GenerateMesh();


            string path = "Assets/VoluSmokeSlice.prefab";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            PrefabUtility.SaveAsPrefabAsset(voluSmokeObj, path);
            Object.DestroyImmediate(voluSmokeObj);


            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        static void ApplyStartingPresetOrDefaults(VoluSmokeMeshGenerator generator, bool includeExtendedDefaults)
        {
            VoluSmokePreset startingPreset = VoluSmokePreset.LoadDefaultPreset();
            if (startingPreset != null)
            {
                startingPreset.ApplyToGenerator(generator);
                return;
            }

    #if UNITY_EDITOR
            generator.EditorSetLastAppliedPreset(null);
    #endif

            generator.gridResolution = 15;
            generator.planeSize = 5f;
            generator.stackLayers = 25;
            generator.layerSpacing = 0.08f;
            generator.sphereRadius = 2.5f;

            if (!includeExtendedDefaults) return;

            generator.centerOpacity = 1f;
            generator.edgeOpacity = 0f;
            generator.smoothNormals = true;
        }

        static void PositionVoluSmokeSlice(Transform target, GameObject parentContext)
        {
            if (target == null) return;


            if (parentContext != null) return;


            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null) return;

            Vector3 spawnPosition = sceneView.pivot;
            if (sceneView.in2DMode) spawnPosition.z = 0f;

            target.position = spawnPosition;
        }
    }

}
