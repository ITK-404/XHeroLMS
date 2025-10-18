using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RemoveInvalidScripts : EditorWindow
{
    [MenuItem("Tools/Cleanup/Remove All Missing/Zombie Scripts (Scene)")]
    public static void RemoveAllMissingAndZombieScripts()
    {
        int removed = 0;
        int affectedObjects = 0;

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var allObjects = new List<GameObject>();

        foreach (var root in roots)
        {
            allObjects.Add(root);
            allObjects.AddRange(GetAllChildren(root));
        }

        foreach (var go in allObjects)
        {
            var components = go.GetComponents<Component>();
            bool changed = false;

            for (int i = components.Length - 1; i >= 0; i--)
            {
                var comp = components[i];

                if (comp == null)
                {
                    // Missing script
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    removed++;
                    changed = true;
                    continue;
                }

                if (comp is MonoBehaviour mb)
                {
                    var so = new SerializedObject(mb);
                    var prop = so.FindProperty("m_Script");

                    if (prop == null || prop.objectReferenceValue == null)
                    {
                        // “Zombie” component — script mất nhưng chưa null
                        Undo.RegisterCompleteObjectUndo(go, "Remove zombie script");
                        Object.DestroyImmediate(comp, true);
                        removed++;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(go);
                affectedObjects++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"🧹 Đã xóa {removed} component lỗi/zombie trên {affectedObjects} object trong scene '{scene.name}'.");
    }

    static List<GameObject> GetAllChildren(GameObject parent)
    {
        var list = new List<GameObject>();
        foreach (Transform child in parent.transform)
        {
            list.Add(child.gameObject);
            list.AddRange(GetAllChildren(child.gameObject));
        }
        return list;
    }
}
