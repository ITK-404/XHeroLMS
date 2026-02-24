#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class HDRICaptureTool
{
    private const string CaptureCameraName = "CaptureCam";
    private const string OutputFolder = "Assets/HDRI_Captures";

    private const int CubemapSize = 2048;     // 1024 / 2048 / 4096
    private const int PanoramaWidth = 4096;   // 2048 / 4096 / 8192 (2:1)
    private static int PanoramaHeight => PanoramaWidth / 2;

    private static Material _equirectMat;

    [MenuItem("Tools/HDRI/Capture CaptureCam -> Cubemap + HDRI (EXR)")]
    public static void CaptureFromCaptureCam()
    {
        Camera cam = FindCaptureCam();
        if (cam == null)
        {
            Debug.LogError($"[HDRI] Cannot find camera named '{CaptureCameraName}'. Please create/rename a Camera to '{CaptureCameraName}'.");
            return;
        }

        EnsureFolder(OutputFolder);

        // 1) Render Cubemap HDR
        var cubemap = new Cubemap(CubemapSize, TextureFormat.RGBAHalf, false);
        bool ok = cam.RenderToCubemap(cubemap);
        if (!ok)
        {
            Object.DestroyImmediate(cubemap);
            Debug.LogError("[HDRI] RenderToCubemap failed.");
            return;
        }

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string cubeAssetPath = $"{OutputFolder}/RoomCubemap_{stamp}.asset";
        AssetDatabase.CreateAsset(cubemap, cubeAssetPath);
        AssetDatabase.SaveAssets();

        // 2) Convert Cubemap -> Equirect (EXR) bằng shader
        string exrPath = $"{OutputFolder}/RoomHDRI_{stamp}.exr";
        bool exrOk = ConvertCubemapToEquirectEXR_Shader(cubemap, PanoramaWidth, PanoramaHeight, exrPath);

        AssetDatabase.Refresh();

        if (exrOk)
        {
            Debug.Log($"[HDRI] Done!\n- Cubemap: {cubeAssetPath}\n- HDRI EXR: {exrPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(cubeAssetPath);
        }
        else
        {
            Debug.LogWarning($"[HDRI] Cubemap saved but EXR conversion failed.\n- Cubemap: {cubeAssetPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(cubeAssetPath);
        }
    }

    private static Camera FindCaptureCam()
    {
        var go = GameObject.Find(CaptureCameraName);
        if (go != null)
        {
            var cam = go.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            if (cam != null && cam.name == CaptureCameraName) return cam;
        }
        return null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = "Assets";
        string[] parts = folder.Substring("Assets/".Length).Split('/');
        foreach (var p in parts)
        {
            string next = $"{parent}/{p}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(parent, p);
            parent = next;
        }
    }

    /// <summary>
    /// Convert cubemap -> equirect panorama bằng shader (không phụ thuộc ConvertToEquirect API).
    /// </summary>
    private static bool ConvertCubemapToEquirectEXR_Shader(Cubemap cube, int w, int h, string exrAssetPath)
    {
        RenderTexture rt = null;
        Texture2D tex = null;

        try
        {
            var mat = GetOrCreateEquirectMaterial();
            if (mat == null)
            {
                Debug.LogError("[HDRI] Failed to create conversion material.");
                return false;
            }

            // RenderTexture HDR
            rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();

            mat.SetTexture("_Cube", cube);

            // Fullscreen blit => equirect
            Graphics.Blit(null, rt, mat, 0);

            // Readback -> EXR float
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false, false);

            byte[] exr = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            File.WriteAllBytes(exrAssetPath, exr);

            RenderTexture.active = prev;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HDRI] ConvertCubemapToEquirectEXR_Shader failed: {e}");
            return false;
        }
        finally
        {
            if (tex != null) Object.DestroyImmediate(tex);
            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }

    /// <summary>
    /// Tạo shader runtime (ẩn) để sample cubemap theo UV equirect.
    /// </summary>
    private static Material GetOrCreateEquirectMaterial()
    {
        if (_equirectMat != null) return _equirectMat;

        // Shader rất ngắn, dùng UNITY_SAMPLE_TEXCUBE
        // Note: đặt Hidden/ để không hiện trong list
        const string shaderSrc = @"
Shader ""Hidden/HDRI/CubemapToEquirect""
{
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" ""Queue""=""Overlay"" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            samplerCUBE _Cube;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // UV (0..1, 0..1) -> direction (unit vector)
            // Equirect: u = lon, v = lat
            float3 EquirectUVToDir(float2 uv)
            {
                // lon: -pi..pi
                float lon = (uv.x * 2.0 - 1.0) * UNITY_PI;
                // lat: -pi/2..pi/2 (v=0 top => lat=+pi/2)
                float lat = (0.5 - uv.y) * UNITY_PI;

                float cosLat = cos(lat);
                float3 dir;
                dir.x = cosLat * sin(lon);
                dir.y = sin(lat);
                dir.z = cosLat * cos(lon);
                return dir;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = EquirectUVToDir(i.uv);
                fixed4 c = texCUBE(_Cube, dir);
                return c;
            }
            ENDHLSL
        }
    }
    Fallback Off
}";
        // Tạo shader asset tạm runtime
        Shader sh = ShaderUtil.CreateShaderAsset(shaderSrc);
        if (sh == null) return null;

        _equirectMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        return _equirectMat;
    }
}
#endif
