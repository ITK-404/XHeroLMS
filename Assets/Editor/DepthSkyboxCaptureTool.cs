#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public static class DepthSkyboxCaptureTool
{
    private const string CaptureCameraName = "CaptureCam";
    private const string OutputRootFolder = "Assets/Depth_Skybox";
    private const string RuntimeShaderPath = OutputRootFolder + "/DepthSkybox_URP_Parallax.shader";
    private const string RuntimeShaderName = "Custom/DepthSkybox/URP_Parallax";

    private const int CubemapSize = 2048;     // 1024 / 2048 / 4096
    private const int PanoramaWidth = 4096;   // 2048 / 4096 / 8192
    private static int PanoramaHeight => PanoramaWidth / 2;

    // Depth được normalize 0..1 theo khoảng này.
    // Ví dụ DepthMaxMeters = 300:
    // pixel depth 0.5 = khoảng 150m.
    private const float DepthMaxMeters = 300f;

    private static Material _cubeToEquirectMat;
    private static Shader _depthCaptureShader;

    [MenuItem("Tools/Depth Skybox/Capture CaptureCam -> 5 Outputs")]
    public static void CaptureFromCaptureCam()
    {
        Camera cam = FindCaptureCam();
        if (cam == null)
        {
            Debug.LogError($"[DepthSkybox] Cannot find camera named '{CaptureCameraName}'. Please create/rename a Camera to '{CaptureCameraName}'.");
            return;
        }

        EnsureFolder(OutputRootFolder);

        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string sessionFolder = $"{OutputRootFolder}/DepthSkybox_{stamp}";
        EnsureFolder(sessionFolder);

        string colorCubePath = $"{sessionFolder}/Color_Cubemap.asset";
        string colorPanoPath = $"{sessionFolder}/Color_Panorama.png";
        string depthCubePath = $"{sessionFolder}/Depth_Cubemap.asset";
        string depthPanoPath = $"{sessionFolder}/Depth_Panorama.exr";
        string materialPath = $"{sessionFolder}/Depth_Skybox.mat";

        // 1) Capture Color Cubemap
        Cubemap colorCube = CaptureCubemap6Faces(cam, false);
        if (colorCube == null)
        {
            Debug.LogError("[DepthSkybox] Failed to capture Color Cubemap.");
            return;
        }

        AssetDatabase.CreateAsset(colorCube, colorCubePath);

        // 2) Capture Depth Cubemap
        Cubemap depthCube = CaptureCubemap6Faces(cam, true);
        if (depthCube == null)
        {
            Debug.LogError("[DepthSkybox] Failed to capture Depth Cubemap.");
            return;
        }

        AssetDatabase.CreateAsset(depthCube, depthCubePath);
        AssetDatabase.SaveAssets();

        // 3) Convert Color Cubemap -> Color Panorama PNG
        bool colorPanoOk = ConvertCubemapToEquirectPNG(
            colorCube,
            PanoramaWidth,
            PanoramaHeight,
            colorPanoPath
        );

        // 4) Convert Depth Cubemap -> Depth Panorama EXR
        bool depthPanoOk = ConvertCubemapToEquirectEXR(
            depthCube,
            PanoramaWidth,
            PanoramaHeight,
            depthPanoPath
        );

        AssetDatabase.Refresh();

        // EXR nên để Linear, đặc biệt depth bắt buộc không được sRGB.
        SetColorTextureImportSettings(colorPanoPath);
        SetDepthTextureImportSettings(depthPanoPath);

        AssetDatabase.Refresh();

        bool materialOk = CreateDepthSkyboxMaterial(
            materialPath,
            colorCubePath,
            colorPanoPath,
            depthCubePath,
            depthPanoPath,
            cam.transform.position);

        Debug.Log(
            "[DepthSkybox] Done!\n" +
            $"Folder: {sessionFolder}\n\n" +
            $"1. Color Cubemap:   {colorCubePath}\n" +
            $"2. Color Panorama:  {colorPanoPath} | OK: {colorPanoOk}\n" +
            $"3. Depth Cubemap:   {depthCubePath}\n" +
            $"4. Depth Panorama:  {depthPanoPath} | OK: {depthPanoOk}\n" +
            $"5. Material:         {materialPath} | OK: {materialOk}\n\n" +
            $"CaptureOrigin: {cam.transform.position}\n" +
            $"DepthMaxMeters: {DepthMaxMeters}"
        );

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(sessionFolder);
    }

    private static Camera FindCaptureCam()
    {
        GameObject go = GameObject.Find(CaptureCameraName);
        if (go == null) return null;

        Camera cam = go.GetComponent<Camera>();
        return cam;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        if (!folder.StartsWith("Assets"))
        {
            Debug.LogError($"[DepthSkybox] Folder must start with Assets: {folder}");
            return;
        }

        string current = "Assets";
        string relative = folder.Substring("Assets".Length).Trim('/');

        if (string.IsNullOrEmpty(relative))
            return;

        string[] parts = relative.Split('/');

        foreach (string part in parts)
        {
            string next = $"{current}/{part}";

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, part);

            current = next;
        }
    }

    private static bool ConvertCubemapToEquirectPNG(Cubemap cube, int w, int h, string pngAssetPath)
    {
        RenderTexture rt = null;
        Texture2D tex = null;
        RenderTexture prevActive = RenderTexture.active;

        try
        {
            Material mat = GetOrCreateCubeToEquirectMaterial();
            if (mat == null)
            {
                Debug.LogError("[DepthSkybox] Failed to create cubemap to equirect material.");
                return false;
            }

            rt = new RenderTexture(
                w,
                h,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            )
            {
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1
            };

            rt.Create();

            mat.SetTexture("_Cube", cube);
            mat.SetFloat("_OutputDisplayColor", 1f);

            Graphics.Blit(null, rt, mat, 0);

            RenderTexture.active = rt;

            tex = new Texture2D(
                w,
                h,
                TextureFormat.RGBA32,
                false,
                false
            );

            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false, false);

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(pngAssetPath, png);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DepthSkybox] ConvertCubemapToEquirectPNG failed: {e}");
            return false;
        }
        finally
        {
            RenderTexture.active = prevActive;

            if (tex != null)
                Object.DestroyImmediate(tex);

            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }

    /// <summary>
    /// Capture cubemap thủ công 6 mặt để Color và Depth dùng chung orientation.
    /// depthMode = false: render màu bình thường.
    /// depthMode = true : render linear depth normalized 0..1.
    /// </summary>
    private static Cubemap CaptureCubemap6Faces(Camera sourceCam, bool depthMode)
    {
        Camera tempCam = null;

        try
        {
            if (depthMode)
            {
                _depthCaptureShader = GetOrCreateDepthCaptureShader();
                if (_depthCaptureShader == null)
                {
                    Debug.LogError("[DepthSkybox] Cannot create depth capture shader.");
                    return null;
                }
            }

            Cubemap cube = new Cubemap(CubemapSize, TextureFormat.RGBAHalf, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            GameObject tempGo = new GameObject(depthMode ? "Temp_Depth_CaptureCam" : "Temp_Color_CaptureCam");
            tempCam = tempGo.AddComponent<Camera>();
            tempCam.CopyFrom(sourceCam);
            CopyUniversalAdditionalCameraData(sourceCam, tempGo);

            tempCam.enabled = false;
            tempCam.transform.position = sourceCam.transform.position;
            tempCam.transform.rotation = Quaternion.identity;

            tempCam.fieldOfView = 90f;
            tempCam.aspect = 1f;
            tempCam.nearClipPlane = sourceCam.nearClipPlane;
            tempCam.farClipPlane = depthMode ? DepthMaxMeters : sourceCam.farClipPlane;
            tempCam.targetTexture = null;

            if (depthMode)
            {
                // Depth không cần skybox. Không có vật thể thì trả về depth = 1.
                tempCam.clearFlags = CameraClearFlags.SolidColor;
                tempCam.backgroundColor = Color.white;
                Shader.SetGlobalFloat("_DepthMax", DepthMaxMeters);
                Shader.SetGlobalVector("_DepthCaptureOrigin", sourceCam.transform.position);
                tempCam.SetReplacementShader(_depthCaptureShader, "");
            }

            // Up vector theo cubemap convention thường dùng.
            // Nếu sau này thấy face bị xoay/lật, chỉ cần chỉnh mảng này.
            bool rendered = tempCam.RenderToCubemap(cube);

            if (depthMode)
                tempCam.ResetReplacementShader();

            if (!rendered)
            {
                Debug.LogError($"[DepthSkybox] RenderToCubemap failed. depthMode={depthMode}");
                return null;
            }

            cube.Apply(false, false);
            cube.SmoothEdges(32);
            cube.Apply(false, false);
            return cube;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DepthSkybox] CaptureCubemap6Faces failed. depthMode={depthMode}\n{e}");
            return null;
        }
        finally
        {
            if (tempCam != null)
            {
                if (depthMode)
                    tempCam.ResetReplacementShader();

                Object.DestroyImmediate(tempCam.gameObject);
            }
        }
    }

    private static void CopyUniversalAdditionalCameraData(Camera sourceCam, GameObject targetGo)
    {
        Component sourceData = null;
        Component[] sourceComponents = sourceCam.GetComponents<Component>();

        foreach (Component component in sourceComponents)
        {
            if (component != null && component.GetType().Name == "UniversalAdditionalCameraData")
            {
                sourceData = component;
                break;
            }
        }

        if (sourceData == null)
            return;

        System.Type dataType = sourceData.GetType();
        Component targetData = targetGo.GetComponent(dataType);
        if (targetData == null)
            targetData = targetGo.AddComponent(dataType);

        EditorUtility.CopySerialized(sourceData, targetData);
    }

    /// <summary>
    /// Convert Cubemap -> Equirect EXR.
    /// Dùng chung cho Color Cubemap và Depth Cubemap.
    /// Với depth, RGB đều chứa cùng giá trị depth normalized 0..1.
    /// </summary>
    private static bool ConvertCubemapToEquirectEXR(Cubemap cube, int w, int h, string exrAssetPath)
    {
        RenderTexture rt = null;
        Texture2D tex = null;
        RenderTexture prevActive = RenderTexture.active;

        try
        {
            Material mat = GetOrCreateCubeToEquirectMaterial();
            if (mat == null)
            {
                Debug.LogError("[DepthSkybox] Failed to create cubemap to equirect material.");
                return false;
            }

            rt = new RenderTexture(
                w,
                h,
                0,
                RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear
            )
            {
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1
            };

            rt.Create();

            mat.SetTexture("_Cube", cube);
            mat.SetFloat("_OutputDisplayColor", 0f);

            Graphics.Blit(null, rt, mat, 0);

            RenderTexture.active = rt;

            tex = new Texture2D(
                w,
                h,
                TextureFormat.RGBAHalf,
                false,
                true
            );

            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false, false);

            byte[] exr = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            File.WriteAllBytes(exrAssetPath, exr);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DepthSkybox] ConvertCubemapToEquirectEXR failed: {e}");
            return false;
        }
        finally
        {
            RenderTexture.active = prevActive;

            if (tex != null)
                Object.DestroyImmediate(tex);

            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }

    private static Material GetOrCreateCubeToEquirectMaterial()
    {
        if (_cubeToEquirectMat != null)
            return _cubeToEquirectMat;

        const string shaderSrc = @"
Shader ""Hidden/DepthSkybox/CubemapToEquirect""
{
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" ""Queue""=""Overlay"" }

        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            samplerCUBE _Cube;
            float _OutputDisplayColor;

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

            float3 EquirectUVToDir(float2 uv)
            {
                float lon = (uv.x * 2.0 - 1.0) * UNITY_PI;
                float lat = (0.5 - uv.y) * UNITY_PI;

                float cosLat = cos(lat);

                float3 dir;
                dir.x = cosLat * sin(lon);
                dir.y = sin(lat);
                dir.z = cosLat * cos(lon);

                return normalize(dir);
            }

            float3 LinearToSRGB(float3 c)
            {
                c = saturate(c);
                float3 low = c * 12.92;
                float3 high = 1.055 * pow(max(c, 0.0), 1.0 / 2.4) - 0.055;
                float3 useLow = 1.0 - step(float3(0.0031308, 0.0031308, 0.0031308), c);
                return lerp(high, low, useLow);
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = EquirectUVToDir(i.uv);
                float4 c = texCUBE(_Cube, dir);
                if (_OutputDisplayColor > 0.5)
                {
                    c.rgb = LinearToSRGB(c.rgb);
                    c.a = 1.0;
                }
                return c;
            }
            ENDHLSL
        }
    }

    Fallback Off
}";

        Shader shader = ShaderUtil.CreateShaderAsset(shaderSrc);
        if (shader == null)
            return null;

        _cubeToEquirectMat = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return _cubeToEquirectMat;
    }

    private static Shader GetOrCreateDepthCaptureShader()
    {
        if (_depthCaptureShader != null)
            return _depthCaptureShader;

        const string shaderSrc = @"
Shader ""Hidden/DepthSkybox/CaptureLinearDepth""
{
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }

        Pass
        {
            ZTest LEqual
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            float _DepthMax;
            float4 _DepthCaptureOrigin;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float d = saturate(distance(i.worldPos, _DepthCaptureOrigin.xyz) / max(_DepthMax, 0.0001));
                return float4(d, d, d, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}";

        _depthCaptureShader = ShaderUtil.CreateShaderAsset(shaderSrc);
        return _depthCaptureShader;
    }

    private static bool CreateDepthSkyboxMaterial(
        string materialPath,
        string colorCubePath,
        string colorPanoPath,
        string depthCubePath,
        string depthPanoPath,
        Vector3 captureOrigin)
    {
        Shader shader = Shader.Find(RuntimeShaderName);
        if (shader == null)
            shader = AssetDatabase.LoadAssetAtPath<Shader>(RuntimeShaderPath);

        if (shader == null)
        {
            Debug.LogWarning("[DepthSkybox] Cannot find runtime shader: " + RuntimeShaderName);
            return false;
        }

        Material material = new Material(shader)
        {
            name = Path.GetFileNameWithoutExtension(materialPath)
        };

        material.SetTexture("_ColorCube", AssetDatabase.LoadAssetAtPath<Cubemap>(colorCubePath));
        material.SetTexture("_DepthCube", AssetDatabase.LoadAssetAtPath<Cubemap>(depthCubePath));
        material.SetTexture("_ColorPanorama", AssetDatabase.LoadAssetAtPath<Texture2D>(colorPanoPath));
        material.SetTexture("_DepthPanorama", AssetDatabase.LoadAssetAtPath<Texture2D>(depthPanoPath));
        material.SetVector("_CaptureOrigin", new Vector4(captureOrigin.x, captureOrigin.y, captureOrigin.z, 0f));
        material.SetFloat("_DepthMaxMeters", DepthMaxMeters);
        material.SetFloat("_UseCubemap", 0f);
        material.SetFloat("_UseScreenRay", 1f);
        material.SetFloat("_PanoramaYawOffset", 0f);
        material.SetFloat("_ParallaxStrength", 1f);
        material.SetFloat("_MotionParallaxScale", 1.5f);
        material.SetFloat("_MaxParallaxOffset", 25f);
        material.SetFloat("_DepthMipBias", 0f);
        material.SetFloat("_DepthSmoothness", 0.2f);
        material.SetFloat("_DepthEdgeFadeStart", 0.025f);
        material.SetFloat("_DepthEdgeFadeEnd", 0.12f);
        material.SetFloat("_SkyDepthFadeStart", 0.985f);
        material.SetFloat("_Exposure", 1f);
        material.SetFloat("_ToneMap", 0f);
        material.SetFloat("_Contrast", 1f);
        material.SetFloat("_Saturation", 1f);
        material.SetFloat("_FlipScreenY", 0f);
        material.SetFloat("_FlipPanoramaX", 0f);
        material.SetFloat("_FlipPanoramaY", 0f);
        material.SetColor("_Tint", Color.white);

        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void SetColorTextureImportSettings(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Mathf.Max(PanoramaWidth, PanoramaHeight);
        importer.SaveAndReimport();
    }

    private static void SetDepthTextureImportSettings(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.sRGBTexture = false;
        importer.mipmapEnabled = true;
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
#endif
