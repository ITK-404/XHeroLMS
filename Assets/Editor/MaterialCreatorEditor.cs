using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class MaterialCreatorEditor : EditorWindow
{
    private Texture2D albedoMap;
    private Texture2D normalMap;
    private Texture2D metallicMap;
    private Texture2D roughnessMap;
    private Shader selectedShader;
    private Material templateMaterial;
    private string materialName = "NewMaterial";
    private string savePath = "Assets/";
    private string textureFolder = "Assets/";
    private string saveFolder = "Assets/";
    
    [MenuItem("Tools/Material Creator")]
    public static void ShowWindow()
    {
        GetWindow<MaterialCreatorEditor>("Material Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Maps", EditorStyles.boldLabel);
        albedoMap = (Texture2D)EditorGUILayout.ObjectField("Albedo", albedoMap, typeof(Texture2D), false);
        normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal", normalMap, typeof(Texture2D), false);
        metallicMap = (Texture2D)EditorGUILayout.ObjectField("Metallic", metallicMap, typeof(Texture2D), false);
        roughnessMap = (Texture2D)EditorGUILayout.ObjectField("Roughness", roughnessMap, typeof(Texture2D), false);

        GUILayout.Space(10);
        GUILayout.Label("Material Settings", EditorStyles.boldLabel);
        selectedShader = (Shader)EditorGUILayout.ObjectField("Shader", selectedShader, typeof(Shader), false);
        templateMaterial = (Material)EditorGUILayout.ObjectField("Template Material", templateMaterial, typeof(Material), false);
        materialName = EditorGUILayout.TextField("Material Name", materialName);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        GUILayout.Space(10);
        if (GUILayout.Button("Create Material"))
        {
            CreateMaterial();
        }

        GUILayout.Space(20);
        GUILayout.Label("Batch Material Creation (BaseColor + Normal)", EditorStyles.boldLabel);
        textureFolder = EditorGUILayout.TextField("Texture Folder", textureFolder);
        saveFolder = EditorGUILayout.TextField("Save Material Folder", saveFolder);
        if (GUILayout.Button("Create Materials From Folder"))
        {
            CreateMaterialsFromFolder();
        }
    }

    private void CreateMaterial()
    {
        Material mat = null;
        if (templateMaterial != null)
        {
            mat = new Material(templateMaterial);
        }
        else if (selectedShader != null)
        {
            mat = new Material(selectedShader);
        }
        else
        {
            // M?c ð?nh dùng URP Lit
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        if (albedoMap != null) mat.SetTexture("_BaseMap", albedoMap);
        if (normalMap != null) mat.SetTexture("_BumpMap", normalMap);
        if (metallicMap != null) mat.SetTexture("_MetallicGlossMap", metallicMap);
        if (roughnessMap != null) mat.SetTexture("_SpecGlossMap", roughnessMap);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(savePath, materialName + ".mat"));
        AssetDatabase.CreateAsset(mat, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Material Creator", "Material created at: " + assetPath, "OK");
    }

    private void CreateMaterialsFromFolder()
    {
        if (!AssetDatabase.IsValidFolder(textureFolder))
        {
            EditorUtility.DisplayDialog("Error", "Invalid texture folder!", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            EditorUtility.DisplayDialog("Error", "Invalid save folder!", "OK");
            return;
        }
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder });
        var textures = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToArray();
        var baseColorKeywords = new[] { "basecolor", "albedo", "diffuse" };
        var normalKeywords = new[] { "normal" };
        int created = 0;
        foreach (var texPath in textures)
        {
            string fileName = Path.GetFileNameWithoutExtension(texPath).ToLower();
            if (!baseColorKeywords.Any(k => fileName.Contains(k))) continue;
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            // T?m normal map cùng tên (ýu tiên cùng prefix)
            string normalPath = textures.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p).ToLower().Replace("normal","") == fileName.Replace("basecolor","").Replace("albedo","").Replace("diffuse","") &&
                normalKeywords.Any(k => Path.GetFileNameWithoutExtension(p).ToLower().Contains(k))
            );
            Texture2D normal = null;
            if (!string.IsNullOrEmpty(normalPath))
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            // T?o material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (baseColor != null) mat.SetTexture("_BaseMap", baseColor);
            if (normal != null) mat.SetTexture("_BumpMap", normal);
            string matName = Path.GetFileNameWithoutExtension(texPath).Replace("_basecolor","").Replace("_albedo","").Replace("_diffuse","") + "_Mat.mat";
            string matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(saveFolder, matName));
            AssetDatabase.CreateAsset(mat, matPath);
            created++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Material Creator", $"Created {created} materials.", "OK");
    }
}
