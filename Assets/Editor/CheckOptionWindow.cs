#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CheckOptionWindow : EditorWindow
{
    // ==== Kết quả scan ====
    private bool _hasScan;
    private long _meshCount;
    private long _totalTriangles;
    private long _textureCount;
    private long _totalTexturePixels;

    private GameComplexityLevel _complexityLevel;
    private HardwareSuggestion _suggestion;

    private Vector2 _scroll;

    [MenuItem("Window/checkOption")]
    public static void Open()
    {
        GetWindow<CheckOptionWindow>("checkOption");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game Hardware Check", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool này sẽ scan Mesh & Texture trong toàn project (ước lượng độ nặng)\n" +
            "rồi gợi ý cấu hình Minimum / Recommended (CPU/RAM/GPU).",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Scan toàn project (Mesh + Texture)"))
        {
            ScanProject();
        }

        EditorGUILayout.Space();

        if (!_hasScan)
        {
            EditorGUILayout.HelpBox("Chưa có dữ liệu. Bấm nút Scan để bắt đầu.", MessageType.Warning);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawScanResult();
        EditorGUILayout.Space();
        DrawSuggestion();
        EditorGUILayout.Space();
        DrawCurrentDevMachineInfo();

        EditorGUILayout.EndScrollView();
    }

    // ================================
    // SCAN PROJECT
    // ================================
    private void ScanProject()
    {
        _meshCount = 0;
        _totalTriangles = 0;
        _textureCount = 0;
        _totalTexturePixels = 0;

        try
        {
            // Scan Mesh
            string[] meshGuids = AssetDatabase.FindAssets("t:Mesh");
            for (int i = 0; i < meshGuids.Length; i++)
            {
                string guid = meshGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) continue;

                _meshCount++;
                // triangles.Length = số index, mỗi 3 index = 1 tris
                _totalTriangles += mesh.triangles.Length / 3;

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Scanning Meshes",
                        $"Đang xử lý: {path}",
                        (float)i / meshGuids.Length
                    );
                }
            }

            EditorUtility.ClearProgressBar();

            // Scan Texture
            string[] texGuids = AssetDatabase.FindAssets("t:Texture");
            for (int i = 0; i < texGuids.Length; i++)
            {
                string guid = texGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                if (tex == null) continue;

                _textureCount++;
                _totalTexturePixels += (long)tex.width * tex.height;

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Scanning Textures",
                        $"Đang xử lý: {path}",
                        (float)i / texGuids.Length
                    );
                }
            }

            EditorUtility.ClearProgressBar();
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("CheckOption scan error: " + e.Message);
        }

        // Đánh giá độ phức tạp -> đề xuất cấu hình
        _complexityLevel = HardwareSpecEstimator.EstimateComplexity(_totalTriangles, _totalTexturePixels);
        // _suggestion = HardwareSpecEstimator.BuildSuggestion(_complexityLevel);
        _suggestion = HardwareSpecEstimator.BuildSuggestion(
                        _complexityLevel,
                        _totalTriangles,
                        _totalTexturePixels
                    );
        _hasScan = true;
    }

    private void DrawScanResult()
    {
        EditorGUILayout.LabelField("Kết quả Scan Game", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Số Mesh: {_meshCount}");
        EditorGUILayout.LabelField($"Tổng triangles (ước lượng): {_totalTriangles:N0}");

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField($"Số Texture: {_textureCount}");
        EditorGUILayout.LabelField($"Tổng pixels (width*height): {_totalTexturePixels:N0}");

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Độ phức tạp ước lượng:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"-> {_complexityLevel}", EditorStyles.helpBox);
        EditorGUILayout.HelpBox(
            "Đây là ước lượng rất thô dựa trên Mesh + Texture trong project.",
            MessageType.Info
        );
    }

    private void DrawSuggestion()
    {
        if (_suggestion == null) return;

        EditorGUILayout.LabelField("Gợi ý cấu hình (có thể dùng để ghi vào store/website)", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Minimum (ước lượng):", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"CPU: {_suggestion.minCPU}");
        EditorGUILayout.LabelField($"RAM: {_suggestion.minRAM}");
        EditorGUILayout.LabelField($"GPU: {_suggestion.minGPU}");

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Recommended:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"CPU: {_suggestion.recCPU}");
        EditorGUILayout.LabelField($"RAM: {_suggestion.recRAM}");
        EditorGUILayout.LabelField($"GPU: {_suggestion.recGPU}");

        EditorGUILayout.Space(4);
    }

    private void DrawCurrentDevMachineInfo()
    {
        EditorGUILayout.LabelField("Máy Dev hiện tại", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        EditorGUILayout.LabelField($"RAM: {(SystemInfo.systemMemorySize / 1024f):0.0} GB");
        EditorGUILayout.LabelField(
            $"GPU: {SystemInfo.graphicsDeviceName} ({(SystemInfo.graphicsMemorySize / 1024f):0.0} GB VRAM)"
        );
        EditorGUILayout.LabelField($"OS: {SystemInfo.operatingSystem}");

        var devTier = HardwareSpecEstimator.EvaluateGpuTier(SystemInfo.graphicsDeviceName, SystemInfo.graphicsMemorySize);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField($"Đánh giá GPU dev (thô): {devTier}");
    }
}

public enum GameComplexityLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

// Ranking GPU dev hiện tại
public enum GPUTier
{
    IntegratedOrVeryWeak,
    MinimumCapable,
    Recommended,
    HighEnd
}

// Kết quả gợi ý cấu hình
[Serializable]
public class HardwareSuggestion
{
    public string minCPU;
    public string minRAM;
    public string minGPU;

    public string recCPU;
    public string recRAM;
    public string recGPU;
}

public static class HardwareSpecEstimator
{
    private static readonly Dictionary<GameComplexityLevel, HardwareSuggestion> Presets =
        new Dictionary<GameComplexityLevel, HardwareSuggestion>
        {
            {
                GameComplexityLevel.VeryLow,
                new HardwareSuggestion
                {
                    // 2D / UI là chính, 3D rất nhẹ
                    minCPU = "Intel Core i3-4130 (4th gen) hoặc tương đương",
                    minRAM = "4–8 GB",
                    minGPU = "iGPU (Intel HD 4600 / UHD) hoặc GT 730",

                    recCPU = "Intel Core i5-4570 (4th gen) hoặc tương đương",
                    recRAM = "8 GB",
                    recGPU = "NVIDIA GTX 750 (2GB VRAM) hoặc tương đương"
                }
            },
            {
                GameComplexityLevel.Low,
                new HardwareSuggestion
                {
                    // 3D nhẹ, ít effect
                    minCPU = "Intel Core i3-4130 / i3-4170 (4th gen) hoặc tương đương",
                    minRAM = "8 GB",
                    minGPU = "NVIDIA GTX 750 (2GB VRAM) hoặc tương đương",

                    recCPU = "Intel Core i5-6500 (6th gen) hoặc tương đương",
                    recRAM = "8–16 GB",
                    recGPU = "NVIDIA GTX 1050 Ti (4GB VRAM) hoặc tương đương"
                }
            },
            {
                GameComplexityLevel.Medium,
                new HardwareSuggestion
                {
                    // Game 3D tầm trung (kiểu của ông)
                    minCPU = "Intel Core i5-4570 (4th gen) hoặc tương đương",
                    minRAM = "8 GB",
                    minGPU = "NVIDIA GTX 750 (2GB VRAM) hoặc tương đương",

                    recCPU = "Intel Core i5-8400 (8th gen) hoặc tương đương",
                    recRAM = "16 GB",
                    recGPU = "NVIDIA RTX 2060 (6GB VRAM) hoặc tương đương"
                }
            },
            {
                GameComplexityLevel.High,
                new HardwareSuggestion
                {
                    // 3D nặng, nhiều mesh/texture/effect
                    minCPU = "Intel Core i5-4570 / i5-6500 (4th–6th gen) hoặc tương đương",
                    minRAM = "16 GB",
                    minGPU = "NVIDIA GTX 1050 Ti / GTX 1650 (4GB VRAM) hoặc tương đương",

                    recCPU = "Intel Core i5-10400 / i5-10500 (10th gen) hoặc tương đương",
                    recRAM = "16 GB",
                    recGPU = "NVIDIA RTX 2060 / RTX 3060 (6–8GB VRAM)"
                }
            },
            {
                GameComplexityLevel.VeryHigh,
                new HardwareSuggestion
                {
                    // Map lớn, asset dày – nhưng vẫn cho máy yếu vào được
                    minCPU = "Intel Core i5-6500 / i5-7500 (6th–7th gen) hoặc tương đương",
                    minRAM = "16 GB",
                    minGPU = "NVIDIA GTX 1070 / RTX 2060 (6GB VRAM) hoặc tương đương",

                    // Recommended mới nhắc tới đời mới / i7
                    recCPU = "Intel Core i5-12400 / i7-11700 hoặc mới hơn",
                    recRAM = "16–32 GB",
                    recGPU = "NVIDIA RTX 3060 / RTX 3070 (8GB+ VRAM) hoặc tương đương"
                }
            }
        };

    // ====== Phần ước lượng complexity giữ nguyên ======
    public static GameComplexityLevel EstimateComplexity(long totalTriangles, long totalTexturePixels)
    {
        var tris   = Mathf.Max(totalTriangles, 1);
        var pixels = Mathf.Max(totalTexturePixels, 1);

        float triScore = Mathf.Log10(tris);
        float texScore = Mathf.Log10(pixels);

        float combined = triScore * 0.6f + texScore * 0.4f;

        if (combined < 5.2f) return GameComplexityLevel.VeryLow;
        if (combined < 6.0f) return GameComplexityLevel.Low;
        if (combined < 6.8f) return GameComplexityLevel.Medium;
        if (combined < 7.4f) return GameComplexityLevel.High;
        return GameComplexityLevel.VeryHigh;
    }

    // ====== EstimateResources + BuildSuggestion: dùng bản ông đang có ======
    private class ResourceEstimate
    {
        public float estimatedVramGB1080p;
        public float estimatedRamGB;
    }

    private static ResourceEstimate EstimateResources(long totalTriangles, long totalTexturePixels)
    {
        const float ACTIVE_TEXTURE_FRACTION = 0.4f;
        const float BYTES_PER_PIXEL        = 2.0f;
        const float MESH_BYTES_PER_TRI     = 48f;

        double activePixels = totalTexturePixels * ACTIVE_TEXTURE_FRACTION;
        double texBytes     = activePixels * BYTES_PER_PIXEL;
        double meshBytes    = totalTriangles * MESH_BYTES_PER_TRI;
        double vramBytes    = texBytes + meshBytes + 256.0 * 1024.0 * 1024.0;

        float vramGB = (float)(vramBytes / (1024.0 * 1024.0 * 1024.0));
        float ramGB  = 4f + vramGB * 1.5f;
        ramGB = Mathf.Max(ramGB, 4f);

        return new ResourceEstimate
        {
            estimatedVramGB1080p = vramGB,
            estimatedRamGB       = ramGB
        };
    }

    public static HardwareSuggestion BuildSuggestion(
        GameComplexityLevel level,
        long totalTriangles,
        long totalTexturePixels)
    {
        if (!Presets.TryGetValue(level, out var basePreset))
            basePreset = Presets[GameComplexityLevel.Medium];

        var res     = EstimateResources(totalTriangles, totalTexturePixels);
        float ramEst = res.estimatedRamGB;
        float vramEst = res.estimatedVramGB1080p;

        // ---- RAM mapping như bản hiện tại của ông ----
        string minRam, recRam;
        if (ramEst <= 6f)
        {
            minRam = "4–8 GB";
            recRam = "8 GB";
        }
        else if (ramEst <= 10f)
        {
            minRam = "8 GB";
            recRam = "8–16 GB";
        }
        else if (ramEst <= 16f)
        {
            minRam = "8 GB";
            recRam = "16 GB";
        }
        else if (ramEst <= 24f)
        {
            minRam = "16 GB";
            recRam = "16–32 GB";
        }
        else
        {
            minRam = "16–32 GB";
            recRam = "32 GB trở lên";
        }

        // ---- VRAM mapping như bản hiện tại của ông ----
        string minGpu, recGpu;
        if (vramEst <= 2.0f)
        {
            minGpu = "GPU 1–2GB VRAM (GTX 750 / GTX 950 hoặc tương đương)";
            recGpu = "GPU 2GB VRAM trở lên (GTX 750 Ti / GTX 950 hoặc tương đương)";
        }
        else if (vramEst <= 3.5f)
        {
            minGpu = "GPU 2GB VRAM (GTX 750 Ti / GTX 950 hoặc tương đương)";
            recGpu = "GPU 3–4GB VRAM (GTX 1050 Ti / GTX 1650 hoặc tương đương)";
        }
        else if (vramEst <= 5.5f)
        {
            minGpu = "GPU 2–4GB VRAM (GTX 1050 / GTX 1050 Ti / GTX 1650 hoặc tương đương)";
            recGpu = "GPU 4–6GB VRAM (GTX 1060 / GTX 1660 / RTX 2060 hoặc tương đương)";
        }
        else if (vramEst <= 7.5f)
        {
            minGpu = "GPU 2–4GB VRAM (GTX 1050 Ti / GTX 1650 hoặc tương đương)";
            recGpu = "GPU 6GB VRAM (RTX 2060 / RTX 3060 hoặc tương đương)";
        }
        else
        {
            minGpu = "GPU 4GB VRAM (GTX 970 / GTX 1650 Super hoặc tương đương)";
            recGpu = "GPU 8GB VRAM trở lên (RTX 3060 Ti / RTX 3070 hoặc tương đương)";
        }

        return new HardwareSuggestion
        {
            minCPU = basePreset.minCPU,
            minRAM = minRam,
            minGPU = minGpu,

            recCPU = basePreset.recCPU,
            recRAM = recRam,
            recGPU = recGpu
        };
    }

    public static GPUTier EvaluateGpuTier(string gpuName, int vramMB)
    {
        string gpu = gpuName.ToLower();

        bool isIntegrated = gpu.Contains("intel") || gpu.Contains("iris") || gpu.Contains("uhd") ||
                            gpu.Contains("vega") || gpu.Contains("radeon graphics");

        if (isIntegrated || vramMB < 2048)
            return GPUTier.IntegratedOrVeryWeak;

        if (vramMB >= 2048 && vramMB < 4096)
            return GPUTier.MinimumCapable;

        if (vramMB >= 4096 && vramMB < 6144)
            return GPUTier.Recommended;

        return GPUTier.HighEnd;
    }
}

#endif
