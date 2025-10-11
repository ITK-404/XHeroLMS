using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LODToolEditor : EditorWindow
{
    private List<float> lodPercents = new List<float>();
    private const string LOD_PREFS_KEY = "LODToolEditor_lodPercents";

    [MenuItem("Tools/Auto Add LOD Group")]
    public static void ShowWindow()
    {
        GetWindow<LODToolEditor>("LOD Tool");
    }

    private void OnEnable()
    {
        LoadLODSettings();
    }

    private void OnGUI()
    {
        GUILayout.Label("LOD Percents (Screen Relative Transition Height)", EditorStyles.boldLabel);
        for (int i = 0; i < lodPercents.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            float newValue = EditorGUILayout.FloatField($"LOD_{i}", lodPercents[i]);
            if (newValue != lodPercents[i])
            {
                lodPercents[i] = newValue;
                SaveLODSettings();
            }
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                lodPercents.RemoveAt(i);
                SaveLODSettings();
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add LOD Level"))
        {
            lodPercents.Add(0.01f);
            SaveLODSettings();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Add LOD Group to Selected GameObject"))
        {
            AddLODGroupToSelected();
        }
    }

    private void AddLODGroupToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponent<LODGroup>())
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        Undo.AddComponent<LODGroup>(selected);
        LODGroup lodGroup = selected.GetComponent<LODGroup>();

        // Lọc các child có tên bắt đầu bằng LOD_ và gom theo index
        Dictionary<int, List<Renderer>> lodRenderers = new Dictionary<int, List<Renderer>>();
        Regex lodRegex = new Regex(@"^LOD_(\d+)");
        foreach (Transform child in selected.transform)
        {
            Match match = lodRegex.Match(child.name);
            if (match.Success)
            {
                int lodIndex = int.Parse(match.Groups[1].Value);
                if (!lodRenderers.ContainsKey(lodIndex))
                    lodRenderers[lodIndex] = new List<Renderer>();
                lodRenderers[lodIndex].AddRange(child.GetComponentsInChildren<Renderer>());
            }
        }

        // Gom renderers theo thứ tự LOD index tăng dần
        List<LOD> lods = new List<LOD>();
        int maxLod = -1;
        foreach (var key in lodRenderers.Keys)
            if (key > maxLod) maxLod = key;
        for (int i = 0; i <= maxLod; i++)
        {
            Renderer[] renderers = lodRenderers.ContainsKey(i) ? lodRenderers[i].ToArray() : new Renderer[0];
            float screenRelativeTransitionHeight = i < lodPercents.Count ? lodPercents[i] : 0.01f;
            lods.Add(new LOD(screenRelativeTransitionHeight, renderers));
        }

        // Xử lý nếu có ít nhất 1 LOD
        if (lods.Count > 0 && lodRenderers.Count > 0)
        {
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();
            Debug.Log("LOD Group added and configured.");
        }
        else
        {
            Debug.LogWarning("No LOD_x children found.");
        }
    }

    private void SaveLODSettings()
    {
        string data = string.Join(",", lodPercents);
        EditorPrefs.SetString(LOD_PREFS_KEY, data);
    }

    private void LoadLODSettings()
    {
        lodPercents.Clear();
        string data = EditorPrefs.GetString(LOD_PREFS_KEY, "0.6,0.3,0.1");
        string[] parts = data.Split(',');
        foreach (var part in parts)
        {
            if (float.TryParse(part, out float value))
                lodPercents.Add(value);
        }
    }
}