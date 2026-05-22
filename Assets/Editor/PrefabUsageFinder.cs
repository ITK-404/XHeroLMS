#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PrefabUsageFinder : EditorWindow
{
    private GameObject targetPrefab;
    private Vector2 scroll;

    private readonly List<SearchResult> results = new List<SearchResult>();

    private class SearchResult
    {
        public string type;
        public string title;
        public string path;
        public UnityEngine.Object asset;
        public string detail;
    }

    [MenuItem("Tools/Find Prefab Usage/Find Who Creates Prefab")]
    public static void Open()
    {
        GetWindow<PrefabUsageFinder>("Prefab Usage Finder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Find Who Creates / Uses Prefab", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab",
            targetPrefab,
            typeof(GameObject),
            false
        );

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(targetPrefab == null))
        {
            if (GUILayout.Button("Find Serialized References In Project", GUILayout.Height(30)))
            {
                FindSerializedReferencesInProject();
            }

            if (GUILayout.Button("Find By Name / Path / GUID In Scripts", GUILayout.Height(30)))
            {
                FindTextReferencesInScripts();
            }

            if (GUILayout.Button("Find Existing Instances In Open Scene", GUILayout.Height(30)))
            {
                FindPrefabInstancesInOpenScene();
            }

            if (GUILayout.Button("Run All", GUILayout.Height(34)))
            {
                results.Clear();
                FindSerializedReferencesInProject(false);
                FindTextReferencesInScripts(false);
                FindPrefabInstancesInOpenScene(false);
                Repaint();
            }
        }

        EditorGUILayout.Space(12);

        if (targetPrefab == null)
        {
            EditorGUILayout.HelpBox("Kéo prefab cần tìm vào đây. Ví dụ: UI Setting Canvas.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Target", targetPrefab.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Results", results.Count.ToString());

        EditorGUILayout.Space(6);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (SearchResult result in results)
        {
            DrawResult(result);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResult(SearchResult result)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(result.type, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(result.title);

        if (!string.IsNullOrEmpty(result.path))
        {
            EditorGUILayout.SelectableLabel(result.path, GUILayout.Height(18));
        }

        if (!string.IsNullOrEmpty(result.detail))
        {
            EditorGUILayout.HelpBox(result.detail, MessageType.None);
        }

        EditorGUILayout.BeginHorizontal();

        if (result.asset != null)
        {
            if (GUILayout.Button("Select Asset"))
            {
                Selection.activeObject = result.asset;
                EditorGUIUtility.PingObject(result.asset);
            }
        }

        if (!string.IsNullOrEmpty(result.path))
        {
            if (GUILayout.Button("Open"))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(result.path);

                if (obj != null)
                {
                    AssetDatabase.OpenAsset(obj);
                }
                else if (File.Exists(result.path))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(result.path, 1);
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void FindSerializedReferencesInProject(bool clear = true)
    {
        if (clear)
            results.Clear();

        string targetPath = AssetDatabase.GetAssetPath(targetPrefab);
        string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);

        if (string.IsNullOrEmpty(targetGuid))
        {
            Debug.LogWarning("[PrefabUsageFinder] Target prefab has no GUID.");
            return;
        }

        string[] allPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string path in allPaths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            if (!path.StartsWith("Assets/"))
                continue;

            if (path == targetPath)
                continue;

            string extension = Path.GetExtension(path).ToLowerInvariant();

            bool canContainSerializedReference =
                extension == ".prefab" ||
                extension == ".unity" ||
                extension == ".asset" ||
                extension == ".controller" ||
                extension == ".overridecontroller" ||
                extension == ".playable" ||
                extension == ".mat";

            if (!canContainSerializedReference)
                continue;

            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
                continue;

            string text;

            try
            {
                text = File.ReadAllText(fullPath);
            }
            catch
            {
                continue;
            }

            if (!text.Contains(targetGuid))
                continue;

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            results.Add(new SearchResult
            {
                type = "Serialized Reference",
                title = $"Asset references prefab: {Path.GetFileName(path)}",
                path = path,
                asset = asset,
                detail =
                    "File này có serialized reference tới prefab target. " +
                    "Nếu đây là prefab/scene/scriptableobject config, rất có thể script lấy reference từ đây để Instantiate."
            });
        }

        Debug.Log($"[PrefabUsageFinder] Serialized reference results: {results.Count}");
        Repaint();
    }

    private void FindTextReferencesInScripts(bool clear = true)
    {
        if (clear)
            results.Clear();

        string targetPath = AssetDatabase.GetAssetPath(targetPrefab);
        string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
        string prefabName = targetPrefab.name;
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
        string resourcesPath = TryGetResourcesPath(targetPath);

        string[] scriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string fullPath in scriptPaths)
        {
            string text;

            try
            {
                text = File.ReadAllText(fullPath);
            }
            catch
            {
                continue;
            }

            List<string> matches = new List<string>();

            if (!string.IsNullOrEmpty(prefabName) && text.Contains(prefabName))
                matches.Add($"Prefab name: {prefabName}");

            if (!string.IsNullOrEmpty(fileNameWithoutExt) && fileNameWithoutExt != prefabName && text.Contains(fileNameWithoutExt))
                matches.Add($"File name: {fileNameWithoutExt}");

            if (!string.IsNullOrEmpty(targetPath) && text.Contains(targetPath))
                matches.Add($"Asset path: {targetPath}");

            if (!string.IsNullOrEmpty(targetGuid) && text.Contains(targetGuid))
                matches.Add($"GUID: {targetGuid}");

            if (!string.IsNullOrEmpty(resourcesPath) && text.Contains(resourcesPath))
                matches.Add($"Resources path: {resourcesPath}");

            if (matches.Count == 0)
                continue;

            string assetRelativePath = FullPathToAssetPath(fullPath);
            UnityEngine.Object scriptAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetRelativePath);

            results.Add(new SearchResult
            {
                type = "Script Text Match",
                title = $"Possible script usage: {Path.GetFileName(fullPath)}",
                path = assetRelativePath,
                asset = scriptAsset,
                detail =
                    "Match found:\n- " + string.Join("\n- ", matches) +
                    "\n\nCheck script này xem có Resources.Load, Addressables.LoadAssetAsync, Instantiate, hoặc field prefab nào không."
            });
        }

        Debug.Log($"[PrefabUsageFinder] Script text results: {results.Count}");
        Repaint();
    }

    private void FindPrefabInstancesInOpenScene(bool clear = true)
    {
        if (clear)
            results.Clear();

        int sceneCount = SceneManager.sceneCount;

        for (int s = 0; s < sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);

            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                ScanInstanceRecursive(root);
            }
        }

        Debug.Log($"[PrefabUsageFinder] Scene instance results: {results.Count}");
        Repaint();
    }

    private void ScanInstanceRecursive(GameObject go)
    {
        if (go == null)
            return;

        GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(go);

        if (prefabSource == targetPrefab)
        {
            results.Add(new SearchResult
            {
                type = "Scene Instance",
                title = $"Instance found in scene: {GetHierarchyPath(go.transform)}",
                path = SceneManager.GetActiveScene().path,
                asset = go,
                detail =
                    "Prefab này đang tồn tại trong scene. " +
                    "Nếu nó xuất hiện sau khi bấm Play thì có script đang tạo nó runtime. " +
                    "Dùng kết quả Script Text Match / Serialized Reference để lần ra script tạo."
            });
        }

        Transform t = go.transform;

        for (int i = 0; i < t.childCount; i++)
        {
            ScanInstanceRecursive(t.GetChild(i).gameObject);
        }
    }

    private string TryGetResourcesPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        const string marker = "/Resources/";

        int index = assetPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
            return null;

        string subPath = assetPath.Substring(index + marker.Length);
        string withoutExtension = Path.ChangeExtension(subPath, null);

        return withoutExtension.Replace("\\", "/");
    }

    private string FullPathToAssetPath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (fullPath.StartsWith(dataPath))
        {
            return "Assets" + fullPath.Substring(dataPath.Length);
        }

        return fullPath;
    }

    private string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "";

        string path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
#endif