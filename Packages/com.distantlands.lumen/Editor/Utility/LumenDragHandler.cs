// Distant Lands 2025
// Lumen: Stylized Light FX 2
// The contents of this file are protected under the Unity Asset Store EULA
//
// Handles drag-and-drop operations for Lumen effect layers in the Unity Editor.

using UnityEditor;
using UnityEngine;

namespace DistantLands.Lumen.EditorScripts
{
    static class DragDropHandler
    {
        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
        }

        private static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            if (perform)
            {
                if (DragAndDrop.objectReferences[0] is LumenEffectProfile)
                {
                    LumenEffectProfile lumenEffect = (LumenEffectProfile)DragAndDrop.objectReferences[0];

                    GameObject go = new GameObject(lumenEffect.name);
                    LumenEffectPlayer player = go.AddComponent<LumenEffectPlayer>();
                    player.profile = lumenEffect;

                    GameObject parentObject = (GameObject)EditorUtility.InstanceIDToObject(dropTargetInstanceID);
                    if (parentObject)
                    {
                        go.transform.SetParent(parentObject.transform);
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localRotation = Quaternion.identity;
                    }

                    Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
                    Selection.activeObject = go;
                }
                else
                {
                    return DragAndDropVisualMode.None;
                }
            }

            return DragAndDropVisualMode.Move;
        }
    }
}