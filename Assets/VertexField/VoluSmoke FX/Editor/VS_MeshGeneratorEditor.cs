using UnityEngine;
using UnityEditor;
using System.IO;

namespace VertexField.VoluSmokeFX
{
    [CustomEditor(typeof(VoluSmokeMeshGenerator)), CanEditMultipleObjects]
    public class VoluSmokeMeshGeneratorEditor : Editor
    {
        private VoluSmokeMeshGenerator voluSmoke;


        private bool showMeshStructure = true;
        private bool showLayerScaling = false;
        private bool showOpacity = false;
        private bool showAppearance = false;
        private bool showDebug = false;

        private string presetName = "NewPreset";


        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tinyMutedLabel;
        private GUIStyle tagPill;
        private bool pendingManualCropApply = false;
        private bool suppressCurvePresetAutoReset = false;


        private static class Theme
        {
            public static readonly Color Teal     = new Color32(78, 170, 160, 255);
            public static readonly Color Orange   = new Color32(249, 124, 61, 255);


            public static readonly Color Leaf     = new Color32(104, 170, 132, 255);
            public static readonly Color Indigo   = new Color32(102, 123, 196, 255);
            public static readonly Color DebugRed = new Color32(180, 120, 120, 255);


            public static Color NeutralBG  => EditorGUIUtility.isProSkin ? new Color(0.23f, 0.23f, 0.23f) : new Color(0.92f, 0.92f, 0.92f);
            public static Color NeutralBG2 => EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.96f, 0.96f, 0.96f);
            public static Color Divider    => EditorGUIUtility.isProSkin ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.8f, 0.8f, 0.8f);

            public static Color AccentFor(string section)
            {
                switch (section)
                {
                    case "Preset":               return Teal;
                    case "Create Prefab":        return Teal;
                    case "Mesh Structure":       return Teal;
                    case "Layer Scaling":        return Leaf;
                    case "Opacity & Alpha":      return Indigo;
                    case "Appearance":           return Indigo;
                    case "Debug Visualization":  return DebugRed;
                    default:                     return Teal;
                }
            }
        }

        private struct ScaleCurvePresetDefinition
        {
            public readonly VoluSmokeMeshGenerator.ScaleCurvePreset preset;
            public readonly string label;
            public readonly string shortLabel;
            public readonly string tooltip;

            public ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset preset, string label, string shortLabel, string tooltip)
            {
                this.preset = preset;
                this.label = label;
                this.shortLabel = shortLabel;
                this.tooltip = tooltip;
            }
        }

        private static readonly ScaleCurvePresetDefinition[] s_ScaleCurvePresets =
        {
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.TaperedTop,     "Tapered Top",    "TopTaper",  "Broad base that tightens toward the top."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.TaperedBottom,  "Tapered Bottom", "BaseTaper", "Narrow base that opens toward the top."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.Uniform,        "Uniform Stack",  "Uniform",   "Keeps layers at a consistent relative width."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.BulgedMid,      "Bulged Middle",  "Bulged",    "Adds mass through the mid section with lighter ends."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.Hourglass,      "Hourglass",      "Hourglass", "Full top and bottom with a pronounced pinch in the centre."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.Dome,           "Dome",           "Dome",      "Strong dome profile with a gentle falloff toward the top."),
            new ScaleCurvePresetDefinition(VoluSmokeMeshGenerator.ScaleCurvePreset.Anvil,          "Anvil",          "Anvil",     "Dense upper volume with a supporting flare through the middle."),
        };


        private const string HeaderImagePath  = "Assets/VertexField/VoluSmoke FX/Editor/Icons/VS_Header.png";
        private const string DiscordIconPath = "Assets/VertexField/VoluSmoke FX/Editor/Icons/Discord.png";
        private const string WebsiteIconPath = "Assets/VertexField/VoluSmoke FX/Editor/Icons/website.png";
        private const string StoreIconPath   = "Assets/VertexField/VoluSmoke FX/Editor/Icons/storepage.png";
        private static bool s_HeaderImportEnsuredThisSession = false;
        private static GUIContent s_DiscordIconContent;
        private static GUIContent s_WebsiteIconContent;
        private static GUIContent s_StoreIconContent;
        private static Texture2D s_HeaderTexture;


        SerializedProperty sp_gridResolution, sp_planeSize, sp_stackLayers, sp_layerSpacing;
        SerializedProperty sp_enableCrop, sp_hideCropHandles, sp_cropLeft, sp_cropRight, sp_cropForward, sp_cropBack;
        SerializedProperty sp_enableLayerScaling, sp_scalingMode, sp_scaleReduction, sp_minScale, sp_maxScale, sp_scaleCurve, sp_scaleCurvePreset;
        SerializedProperty sp_sphereRadius, sp_falloffCurve, sp_centerOpacity, sp_edgeOpacity;
        SerializedProperty sp_sphereCenter, sp_noiseAmount, sp_noiseScale, sp_enableLayerColorGradient, sp_layerColorGradient, sp_smoothNormals, sp_planarNormalWeight, sp_inwardBubbleStrength, sp_bubbleCenterOffset;
        SerializedProperty sp_showGizmos, sp_debugShowPolygons, sp_debugShowWire, sp_debugShowNormals, sp_debugShowTriIndices, sp_debugShowVertIndices;
        SerializedProperty sp_debugMaxTriangles, sp_debugNormalScale, sp_debugWireColor, sp_debugNormalColor, sp_debugTriIndexColor, sp_debugVertIndexColor;

        void OnEnable()
        {
            voluSmoke = (VoluSmokeMeshGenerator)target;

            if (!s_HeaderImportEnsuredThisSession)
            {
                EnsureHeaderImportSettings(HeaderImagePath, 4096);
                s_HeaderImportEnsuredThisSession = true;
            }


            sp_gridResolution  = serializedObject.FindProperty("gridResolution");
            sp_planeSize       = serializedObject.FindProperty("planeSize");
            sp_stackLayers     = serializedObject.FindProperty("stackLayers");
            sp_layerSpacing    = serializedObject.FindProperty("layerSpacing");
            sp_enableCrop      = serializedObject.FindProperty("enableCrop");
            sp_hideCropHandles = serializedObject.FindProperty("hideCropHandles");
            sp_cropLeft        = serializedObject.FindProperty("cropLeft");
            sp_cropRight       = serializedObject.FindProperty("cropRight");
            sp_cropForward     = serializedObject.FindProperty("cropForward");
            sp_cropBack        = serializedObject.FindProperty("cropBack");

            sp_enableLayerScaling = serializedObject.FindProperty("enableLayerScaling");
            sp_scalingMode        = serializedObject.FindProperty("scalingMode");
            sp_scaleReduction     = serializedObject.FindProperty("scaleReduction");
            sp_minScale           = serializedObject.FindProperty("minScale");
            sp_maxScale           = serializedObject.FindProperty("maxScale");
            sp_scaleCurve         = serializedObject.FindProperty("scaleCurve");
            sp_scaleCurvePreset   = serializedObject.FindProperty("scaleCurvePreset");

            sp_sphereRadius   = serializedObject.FindProperty("sphereRadius");
            sp_falloffCurve   = serializedObject.FindProperty("falloffCurve");
            sp_centerOpacity  = serializedObject.FindProperty("centerOpacity");
            sp_edgeOpacity    = serializedObject.FindProperty("edgeOpacity");

            sp_sphereCenter             = serializedObject.FindProperty("sphereCenter");
            sp_noiseAmount              = serializedObject.FindProperty("noiseAmount");
            sp_noiseScale               = serializedObject.FindProperty("noiseScale");
            sp_enableLayerColorGradient = serializedObject.FindProperty("enableLayerColorGradient");
            sp_layerColorGradient       = serializedObject.FindProperty("layerColorGradient");
            sp_smoothNormals            = serializedObject.FindProperty("smoothNormals");
            sp_planarNormalWeight   = serializedObject.FindProperty("planarNormalWeight");
            sp_inwardBubbleStrength = serializedObject.FindProperty("inwardBubbleStrength");
            sp_bubbleCenterOffset   = serializedObject.FindProperty("bubbleCenterOffset");

            sp_showGizmos          = serializedObject.FindProperty("showGizmos");
            sp_debugShowPolygons   = serializedObject.FindProperty("debugShowPolygons");
            sp_debugShowWire       = serializedObject.FindProperty("debugShowWire");
            sp_debugShowNormals    = serializedObject.FindProperty("debugShowNormals");
            sp_debugShowTriIndices = serializedObject.FindProperty("debugShowTriIndices");
            sp_debugShowVertIndices= serializedObject.FindProperty("debugShowVertIndices");
            sp_debugMaxTriangles   = serializedObject.FindProperty("debugMaxTriangles");
            sp_debugNormalScale    = serializedObject.FindProperty("debugNormalScale");
            sp_debugWireColor      = serializedObject.FindProperty("debugWireColor");
            sp_debugNormalColor    = serializedObject.FindProperty("debugNormalColor");
            sp_debugTriIndexColor  = serializedObject.FindProperty("debugTriIndexColor");
            sp_debugVertIndexColor = serializedObject.FindProperty("debugVertIndexColor");
        }

        void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    normal = { textColor = Theme.Teal },
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 28
                };
            }

            if (tinyMutedLabel == null)
            {
                tinyMutedLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.35f, 0.35f, 0.35f) },
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (tagPill == null)
            {
                tagPill = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = 9,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 16,
                    padding = new RectOffset(6, 6, 0, 0)
                };
            }
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            serializedObject.Update();

            DrawVoluSmokeHeader();
            EditorGUILayout.Space(5);

            DrawQuickActionsRow();

            DrawPresetManager();
            EditorGUILayout.Space(10);

            DrawExportSection();
            EditorGUILayout.Space(10);

            DrawFoldoutSections();
            DrawStatusBar();

            serializedObject.ApplyModifiedProperties();

            if (pendingManualCropApply && voluSmoke != null)
            {
                pendingManualCropApply = false;
                voluSmoke.ClampCropValues();
                voluSmoke.GenerateMesh();
                EditorUtility.SetDirty(voluSmoke);
                EditorUtility.SetDirty(voluSmoke.transform);
                SceneView.RepaintAll();
            }
        }



        private static void EnsureHeaderImportSettings(string assetPath, int desiredMaxSize)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.maxTextureSize < desiredMaxSize) { importer.maxTextureSize = desiredMaxSize; changed = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; changed = true; }

            if (changed)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }


        private struct ColorScope : System.IDisposable
        {
            private readonly Color prevBG, prevContent;
            private readonly GUIStyle style;
            public ColorScope(Color bg, GUIStyle s, Color textColor)
            {
                prevBG = GUI.backgroundColor;
                prevContent = GUI.contentColor;
                style = s;

                GUI.backgroundColor = bg;
                GUI.contentColor = textColor;

                if (style != null)
                {
                    style.normal.textColor  = textColor;
                    style.active.textColor  = textColor;
                    style.focused.textColor = textColor;
                    style.hover.textColor   = textColor;
                }
            }
            public void Dispose()
            {
                GUI.backgroundColor = prevBG;
                GUI.contentColor = prevContent;
            }
        }

        bool ThemedButton(string label, Color bg, GUIStyle style, params GUILayoutOption[] options)
        {
            using (new ColorScope(bg, style, Color.white))
                return GUILayout.Button(label, style, options);
        }

        private bool IconPill(string text, Color bg, params GUILayoutOption[] options)
        {
            using (new ColorScope(bg, tagPill, Color.white))
                return GUILayout.Button(text, tagPill, options);
        }

        private GUIStyle linkIconButtonStyle;
        private const string WebsiteUrl = "https://vertexfield.wixsite.com/home";
        private const string UnityStoreUrl = "https://assetstore.unity.com/publishers/25153";
        private const string DiscordUrl = "https://discord.gg/X3DSWG7zvp";

        void DrawVoluSmokeHeader()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            Texture2D headerImage = GetHeaderTexture();

            if (headerImage != null)
            {
                float aspectRatio = (float)headerImage.width / Mathf.Max(1, headerImage.height);
                const float padding = 12f;
                float reservedHeight = headerImage.height + padding * 2f;
                Rect containerRect = GUILayoutUtility.GetRect(headerImage.width, reservedHeight, GUILayout.ExpandWidth(true));

                float availableWidth = Mathf.Max(1f, containerRect.width - padding * 2f);
                float targetWidth = Mathf.Min(headerImage.width, availableWidth);

                float targetHeight = targetWidth / aspectRatio;
                Rect headerRect = new Rect(
                    containerRect.x + (containerRect.width - targetWidth) * 0.5f,
                    containerRect.y + padding,
                    targetWidth,
                    targetHeight);

                GUI.DrawTexture(headerRect, headerImage, ScaleMode.ScaleToFit, true);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("VoluSmoke Mesh Generator", headerStyle, GUILayout.ExpandWidth(true));
                    DrawHeaderLinkButtonsInline();
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        void DrawHeaderLinkButtonsInline()
        {
            const float iconSize = 32f;
            const float spacing = 4f;
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(iconSize * 3 + spacing * 2)))
            {
                GUILayout.FlexibleSpace();
                DrawLinkIconRow(iconSize, spacing);
            }
        }

        void DrawLinkIconRow(float iconSize, float spacing)
        {
            DrawLinkIcon("Website", Theme.AccentFor("Links"), WebsiteUrl, GetWebsiteIconContent(), iconSize);
            GUILayout.Space(spacing);
            DrawLinkIcon("Unity Asset Store", Theme.AccentFor("Links"), UnityStoreUrl, GetStoreIconContent(), iconSize);
            GUILayout.Space(spacing);
            DrawLinkIcon("Discord", Theme.AccentFor("Links"), DiscordUrl, GetDiscordIconContent(), iconSize);
        }

        void DrawLinkIcon(string tooltip, Color color, string url, GUIContent iconContent, float size)
        {
            if (iconContent == null) return;

            if (linkIconButtonStyle == null)
            {
                linkIconButtonStyle = new GUIStyle(GUIStyle.none)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 4, 0, 0),
                    imagePosition = ImagePosition.ImageOnly
                };
                linkIconButtonStyle.border = new RectOffset(0, 0, 0, 0);
                linkIconButtonStyle.normal.background = null;
                linkIconButtonStyle.hover.background = null;
                linkIconButtonStyle.active.background = null;
                linkIconButtonStyle.focused.background = null;
                linkIconButtonStyle.onNormal.background = null;
                linkIconButtonStyle.onHover.background = null;
                linkIconButtonStyle.onActive.background = null;
                linkIconButtonStyle.onFocused.background = null;
            }

            linkIconButtonStyle.fixedWidth = linkIconButtonStyle.fixedHeight = size;

            Texture iconTexture = iconContent.image;
            Rect iconRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            if (iconTexture != null)
            {
                GUI.DrawTexture(iconRect, iconTexture, ScaleMode.ScaleToFit, true);

                EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);
                if (GUI.Button(iconRect, new GUIContent(string.Empty, tooltip), GUIStyle.none))
                {
                    Application.OpenURL(url);
                }
                return;
            }

            GUIContent content;
            if (!string.IsNullOrEmpty(iconContent.text))
            {
                content = new GUIContent(iconContent.text, tooltip);
            }
            else
            {
                content = new GUIContent("?", tooltip);
            }

            Color prevContent = GUI.contentColor;
            GUI.contentColor = Color.white;

            if (GUI.Button(iconRect, content, linkIconButtonStyle))
            {
                Application.OpenURL(url);
            }

            GUI.contentColor = prevContent;
        }

        GUIContent GetIconContent(string iconName, string fallback)
        {
            GUIContent icon = EditorGUIUtility.IconContent(iconName);
            if (icon == null || (icon.image == null && string.IsNullOrEmpty(icon.text)))
            {
                return new GUIContent(fallback);
            }
            return icon;
        }

        GUIContent GetWebsiteIconContent()
        {
            return GetImageIconContent(ref s_WebsiteIconContent, WebsiteIconPath, "Website", "BuildSettings.Web.Small", "Web");
        }

        GUIContent GetStoreIconContent()
        {
            return GetImageIconContent(ref s_StoreIconContent, StoreIconPath, "Unity Asset Store", "Asset Store", "Asset");
        }

        GUIContent GetDiscordIconContent()
        {
            return GetImageIconContent(ref s_DiscordIconContent, DiscordIconPath, "Discord", "d_UnityEditor.ConsoleWindow", "Chat");
        }

        GUIContent GetImageIconContent(ref GUIContent cache, string path, string tooltip, string fallbackIconName, string fallbackText)
        {
            if (cache != null)
                return cache;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                cache = new GUIContent(texture, tooltip);
            }
            else
            {
                cache = GetIconContent(fallbackIconName, fallbackText);
            }

            return cache;
        }

        Texture2D GetHeaderTexture()
        {
            if (s_HeaderTexture == null)
            {
                s_HeaderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HeaderImagePath);
            }
            return s_HeaderTexture;
        }


        void DrawQuickActionsRow()
        {
            const float iconSize = 32f;
            const float spacing = 4f;

            EditorGUILayout.BeginHorizontal();
            DrawLinkIconRow(iconSize, spacing);
            GUILayout.FlexibleSpace();

            if (IconPill("Expand all", new Color32(90, 120, 90, 255), GUILayout.Height(iconSize)))
                showMeshStructure = showLayerScaling = showOpacity = showAppearance = showDebug = true;

            if (IconPill("Collapse all", new Color32(120, 90, 90, 255), GUILayout.Height(iconSize)))
                showMeshStructure = showLayerScaling = showOpacity = showAppearance = showDebug = false;

            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }



        bool _dummyFold = true;

        void DrawPresetManager()
        {
            DrawSection("Preset", ref _dummyFold, Theme.AccentFor("Preset"), () =>
            {
                EditorGUILayout.BeginVertical(sectionStyle);


                presetName = EditorGUILayout.TextField(new GUIContent("Name", "Name for the saved preset asset"), presetName);

                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();


                if (ThemedButton("Open Preset Browser", Theme.Teal, buttonStyle, GUILayout.Height(30)))
                    VoluSmokePresetBrowser.Open();

                if (ThemedButton("Save Preset", Theme.Orange, buttonStyle, GUILayout.Height(30)))
                    SavePreset();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }, headerAsFoldout: false);
        }

        void DrawExportSection()
        {
            DrawSection("Create Prefab", ref _dummyFold, Theme.AccentFor("Create Prefab"), () =>
            {
                if (ThemedButton("Make Prefab", Theme.Orange, buttonStyle, GUILayout.Height(32)))
                {
                    var editorInstance = this;
                    EditorApplication.delayCall += () =>
                    {
                        if (editorInstance != null)
                            editorInstance.MakePrefab();
                    };
                    GUIUtility.ExitGUI();
                }
            }, headerAsFoldout: false);
        }



        void DrawFoldoutSections()
        {
            DrawSection("Mesh Structure", ref showMeshStructure, Theme.AccentFor("Mesh Structure"), () =>
            {
                EditorGUILayout.PropertyField(sp_gridResolution, new GUIContent("Grid Resolution", "XY resolution of each layer"));
                EditorGUILayout.PropertyField(sp_planeSize,      new GUIContent("Plane Size", "Width/Height world size per layer"));
                EditorGUILayout.PropertyField(sp_stackLayers,    new GUIContent("Stack Layers", "Number of layers to stack vertically"));
                EditorGUILayout.PropertyField(sp_layerSpacing,   new GUIContent("Layer Spacing", "Distance between stacked layers"));
                DrawCropControls();
            });

            DrawSection("Layer Scaling", ref showLayerScaling, Theme.AccentFor("Layer Scaling"), () =>
            {
                EditorGUILayout.PropertyField(sp_enableLayerScaling, new GUIContent("Enable Layer Scaling", "Scale layers progressively"));

                using (new EditorGUI.DisabledScope(!sp_enableLayerScaling.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sp_scalingMode,    new GUIContent("Mode", "Strategy for scaling across layers"));
                    EditorGUILayout.PropertyField(sp_scaleReduction, new GUIContent("Scale Reduction", "Amount reduced per step"));
                    EditorGUILayout.PropertyField(sp_minScale,       new GUIContent("Min Scale"));
                    EditorGUILayout.PropertyField(sp_maxScale,       new GUIContent("Max Scale"));

                    DrawScaleCurvePresetControls();

                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope((VoluSmokeMeshGenerator.ScalingMode)sp_scalingMode.enumValueIndex != VoluSmokeMeshGenerator.ScalingMode.Curve))
                    {
                        EditorGUILayout.PropertyField(sp_scaleCurve, new GUIContent("Scale Curve", "Fine control over scaling profile"));
                    }
                    if (EditorGUI.EndChangeCheck() && !suppressCurvePresetAutoReset)
                    {
                        ApplyScaleCurvePresetToTargets(VoluSmokeMeshGenerator.ScaleCurvePreset.Custom, false);
                    }
                    EditorGUI.indentLevel--;
                }
            });

            DrawSection("Opacity & Alpha", ref showOpacity, Theme.AccentFor("Opacity & Alpha"), () =>
            {
                EditorGUILayout.PropertyField(sp_sphereRadius,  new GUIContent("Sphere Radius", "Controls overall falloff distance"));
                EditorGUILayout.PropertyField(sp_falloffCurve,  new GUIContent("Falloff Curve"));
                EditorGUILayout.PropertyField(sp_centerOpacity, new GUIContent("Center Opacity"));
                EditorGUILayout.PropertyField(sp_edgeOpacity,   new GUIContent("Edge Opacity"));
            });

            DrawSection("Appearance", ref showAppearance, Theme.AccentFor("Appearance"), () =>
            {
                EditorGUILayout.PropertyField(sp_sphereCenter,  new GUIContent("Sphere Center"));
                EditorGUILayout.PropertyField(sp_noiseAmount,   new GUIContent("Noise Amount"));
                EditorGUILayout.PropertyField(sp_noiseScale,    new GUIContent("Noise Scale"));
                EditorGUILayout.PropertyField(
                    sp_enableLayerColorGradient,
                    new GUIContent(
                        "Colorize Layers",
                        "Apply a vertical gradient across the stacked layers using vertex colors."));
                if (sp_enableLayerColorGradient.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(
                            sp_layerColorGradient,
                            new GUIContent(
                                "Layer Gradient",
                                "Gradient sampled from bottom (0) to top (1) of the stack."));
                    }
                }
                EditorGUILayout.PropertyField(sp_smoothNormals, new GUIContent("Smooth Normals"));

                using (new EditorGUI.IndentLevelScope())
                {
                    if (sp_smoothNormals.boolValue)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Normal Flatness", "Blend between a sphere-projected normal (0) and a flat plane normal (1)."), GUILayout.Width(EditorGUIUtility.labelWidth));
                        EditorGUILayout.LabelField("Sphere", EditorStyles.miniLabel, GUILayout.Width(50));
                        EditorGUILayout.Slider(sp_planarNormalWeight, 0f, 1f, GUIContent.none, GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField("Flat", EditorStyles.miniLabel, GUILayout.Width(35));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Slider(
                            sp_inwardBubbleStrength,
                            0f,
                            1f,
                            new GUIContent(
                                "Wrap Amount",
                                "Amount of spherical wrapping applied to the normals."));

                        EditorGUILayout.PropertyField(
                            sp_bubbleCenterOffset,
                            new GUIContent(
                                "Wrap Offset",
                                "Offsets the spherical wrap pivot to steer highlight placement."));
                    }
                }
            });

            DrawSection("Debug Visualization", ref showDebug, Theme.AccentFor("Debug Visualization"), () =>
            {
                EditorGUILayout.PropertyField(sp_showGizmos,          new GUIContent("Show Gizmos"));
                EditorGUILayout.PropertyField(sp_debugShowPolygons,   new GUIContent("Show Polygons"));
                EditorGUILayout.PropertyField(sp_debugShowWire,       new GUIContent("Show Wireframe"));
                EditorGUILayout.PropertyField(sp_debugShowNormals,    new GUIContent("Show Normals"));
                EditorGUILayout.PropertyField(sp_debugShowTriIndices, new GUIContent("Show Triangle Indices"));
                EditorGUILayout.PropertyField(sp_debugShowVertIndices,new GUIContent("Show Vertex Indices"));
                EditorGUILayout.PropertyField(sp_debugMaxTriangles,   new GUIContent("Max Triangles"));
                EditorGUILayout.PropertyField(sp_debugNormalScale,    new GUIContent("Normal Scale"));
                EditorGUILayout.PropertyField(sp_debugWireColor,      new GUIContent("Wire Color"));
                EditorGUILayout.PropertyField(sp_debugNormalColor,    new GUIContent("Normal Color"));
                EditorGUILayout.PropertyField(sp_debugTriIndexColor,  new GUIContent("Tri Index Color"));
                EditorGUILayout.PropertyField(sp_debugVertIndexColor, new GUIContent("Vert Index Color"));
            });
        }

        void DrawScaleCurvePresetControls()
        {
            if ((VoluSmokeMeshGenerator.ScalingMode)sp_scalingMode.enumValueIndex != VoluSmokeMeshGenerator.ScalingMode.Curve)
                return;

            EditorGUI.indentLevel++;

            EditorGUI.showMixedValue = sp_scaleCurvePreset.hasMultipleDifferentValues;
            VoluSmokeMeshGenerator.ScaleCurvePreset currentPreset = sp_scaleCurvePreset.hasMultipleDifferentValues
                ? VoluSmokeMeshGenerator.ScaleCurvePreset.Custom
                : (VoluSmokeMeshGenerator.ScaleCurvePreset)sp_scaleCurvePreset.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            var newPreset = (VoluSmokeMeshGenerator.ScaleCurvePreset)EditorGUILayout.EnumPopup(
                new GUIContent("Scale Preset", "Apply curated profiles to the scale curve."),
                currentPreset);
            if (EditorGUI.EndChangeCheck())
            {
                bool updateCurve = newPreset != VoluSmokeMeshGenerator.ScaleCurvePreset.Custom;
                ApplyScaleCurvePresetToTargets(newPreset, updateCurve);
            }
            EditorGUI.showMixedValue = false;

            EditorGUILayout.Space(2f);
            DrawScaleCurvePresetButtons();
            EditorGUI.indentLevel--;
        }

        void DrawScaleCurvePresetButtons()
        {
            const float buttonWidth = 90f;
            const float spacing = 4f;
            float indentPadding = EditorGUI.indentLevel * 15f;
            float viewWidth = EditorGUIUtility.currentViewWidth;
            float usableWidth = Mathf.Max(buttonWidth, viewWidth - indentPadding - 40f);
            int buttonsPerRow = Mathf.Clamp(Mathf.FloorToInt((usableWidth + spacing) / (buttonWidth + spacing)), 1, s_ScaleCurvePresets.Length);

            int index = 0;
            while (index < s_ScaleCurvePresets.Length)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentPadding);

                for (int column = 0; column < buttonsPerRow && index < s_ScaleCurvePresets.Length; column++, index++)
                {
                    var preset = s_ScaleCurvePresets[index];
                    bool isActive = !sp_scaleCurvePreset.hasMultipleDifferentValues && sp_scaleCurvePreset.enumValueIndex == (int)preset.preset;
                    Color previous = GUI.backgroundColor;
                    if (isActive) GUI.backgroundColor = Theme.AccentFor("Layer Scaling");

                    GUIContent content = new GUIContent(preset.shortLabel, $"{preset.label}\n{preset.tooltip}");
                    if (GUILayout.Button(content, GUILayout.Height(20f), GUILayout.Width(buttonWidth)))
                    {
                        ApplyScaleCurvePresetToTargets(preset.preset, true);
                    }

                    GUI.backgroundColor = previous;
                    if (column < buttonsPerRow - 1) GUILayout.Space(spacing);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
        }

        void ApplyScaleCurvePresetToTargets(VoluSmokeMeshGenerator.ScaleCurvePreset preset, bool updateCurve)
        {
            serializedObject.ApplyModifiedProperties();
            suppressCurvePresetAutoReset = true;
            try
            {
                Undo.IncrementCurrentGroup();
                bool repaintScene = false;

                foreach (var obj in targets)
                {
                    if (obj is VoluSmokeMeshGenerator generator)
                    {
                        Undo.RegisterCompleteObjectUndo(generator, "Apply Scale Curve Preset");
                        if (updateCurve && preset != VoluSmokeMeshGenerator.ScaleCurvePreset.Custom)
                        {
                            generator.ApplyScaleCurvePreset(preset);
                        }
                        else
                        {
                            generator.scaleCurvePreset = preset;
                        }

                        EditorUtility.SetDirty(generator);

                        if (!Application.isPlaying && generator.autoUpdate)
                        {
                            generator.GenerateMesh();
                            repaintScene = true;
                        }
                    }
                }

                if (repaintScene) SceneView.RepaintAll();
            }
            finally
            {
                suppressCurvePresetAutoReset = false;
            }

            serializedObject.Update();
        }


        void DrawSection(string title, ref bool foldout, Color accent, System.Action content, bool headerAsFoldout = true)
        {

            Rect headerRect = GUILayoutUtility.GetRect(1, 24f);
            headerRect.x += 4;
            headerRect.width -= 8;


            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + 1, headerRect.width, headerRect.height - 2), Theme.NeutralBG2);

            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + 1, 3, headerRect.height - 2), accent);

            if (headerAsFoldout)
            {
                foldout = EditorGUI.Foldout(headerRect, foldout, title, true, EditorStyles.foldoutHeader);
            }
            else
            {
                var labelRect = new Rect(headerRect.x + 6, headerRect.y + 2, headerRect.width - 6, headerRect.height);
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.15f, 0.15f, 0.15f);
                GUI.Label(labelRect, title, style);
                foldout = true;
            }

            if (foldout)
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.Space(4);
                content?.Invoke();
                EditorGUILayout.Space(1);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(5);
        }

        void DrawCropControls()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Crop", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool enable = EditorGUILayout.Toggle(new GUIContent("Enable Crop", "Limit mesh extents and expose scene handles for cropping."), sp_enableCrop.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Toggle Crop");
                sp_enableCrop.boolValue = enable;
                if (!voluSmoke.autoUpdate) pendingManualCropApply = true;
            }

            if (!sp_enableCrop.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            bool hide = EditorGUILayout.Toggle(new GUIContent("Hide Arrows", "Hide the crop arrows and outline in the scene view."), sp_hideCropHandles.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(voluSmoke, "Toggle Crop Handles Visibility");
                sp_hideCropHandles.boolValue = hide;
            }
            EditorGUILayout.Space(2);
            DrawCropSlider(sp_cropLeft, "Left", "Crop amount from the negative X edge (normalized 0-0.49).");
            DrawCropSlider(sp_cropRight, "Right", "Crop amount from the positive X edge (normalized 0-0.49).");
            DrawCropSlider(sp_cropBack, "Back", "Crop amount from the negative Z edge (normalized 0-0.49).");
            DrawCropSlider(sp_cropForward, "Forward", "Crop amount from the positive Z edge (normalized 0-0.49).");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Crop", GUILayout.Height(22)))
                {
                    Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Reset Crop");
                    sp_cropLeft.floatValue = 0f;
                    sp_cropRight.floatValue = 0f;
                    sp_cropForward.floatValue = 0f;
                    sp_cropBack.floatValue = 0f;
                    pendingManualCropApply = true;
                }

                if (GUILayout.Button("Apply Crop (Manual)", GUILayout.Height(22)))
                {
                    Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Apply Crop");
                    pendingManualCropApply = true;
                }
            }
        }

        void DrawCropSlider(SerializedProperty prop, string label, string tooltip)
        {
            EditorGUI.BeginChangeCheck();
            float next = EditorGUILayout.Slider(new GUIContent(label, tooltip), prop.floatValue, 0f, 0.49f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, $"Adjust Crop {label}");
                prop.floatValue = next;
                if (!voluSmoke.autoUpdate) pendingManualCropApply = true;
            }
        }

        void OnSceneGUI()
        {
            if (voluSmoke == null) return;
            if (!voluSmoke.enableCrop) return;
            if (voluSmoke.hideCropHandles) return;
            if (voluSmoke.planeSize <= Mathf.Epsilon) return;

            VoluSmokeMeshGenerator.CropData crop = voluSmoke.CalculateCropData();

            Vector3[] localCorners =
            {
                new Vector3(crop.localMinX, 0f, crop.localMinZ),
                new Vector3(crop.localMaxX, 0f, crop.localMinZ),
                new Vector3(crop.localMaxX, 0f, crop.localMaxZ),
                new Vector3(crop.localMinX, 0f, crop.localMaxZ)
            };

            Vector3[] worldCorners = new Vector3[5];
            for (int i = 0; i < 4; i++) worldCorners[i] = voluSmoke.transform.TransformPoint(localCorners[i]);
            worldCorners[4] = worldCorners[0];

            Handles.color = new Color(0f, 0.7f, 1f, 0.6f);
            Handles.DrawAAPolyLine(3f, worldCorners);

            float handleSize = HandleUtility.GetHandleSize(voluSmoke.transform.position) * 0.35f;
            float baseHalf = voluSmoke.planeSize * 0.5f;
            Vector3 pivotOffset = crop.pivotOffset;


            Vector3 leftWorld = voluSmoke.transform.TransformPoint(new Vector3(crop.localMinX, 0f, 0f));
            Handles.color = new Color(0.1f, 0.8f, 1f, 0.9f);
            EditorGUI.BeginChangeCheck();
            Vector3 leftResult = Handles.Slider(leftWorld, voluSmoke.transform.right, handleSize, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Adjust Crop Left");
                float newLocal = voluSmoke.transform.InverseTransformPoint(leftResult).x;
                float newBaseMin = newLocal + pivotOffset.x;
                voluSmoke.cropLeft = Mathf.Clamp01((newBaseMin + baseHalf) / voluSmoke.planeSize);
                voluSmoke.ClampCropValues();
                voluSmoke.GenerateMesh();
                EditorUtility.SetDirty(voluSmoke);
                EditorUtility.SetDirty(voluSmoke.transform);
                SceneView.RepaintAll();
                return;
            }


            Vector3 rightWorld = voluSmoke.transform.TransformPoint(new Vector3(crop.localMaxX, 0f, 0f));
            Handles.color = new Color(0.1f, 0.8f, 1f, 0.9f);
            EditorGUI.BeginChangeCheck();
            Vector3 rightResult = Handles.Slider(rightWorld, -voluSmoke.transform.right, handleSize, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Adjust Crop Right");
                float newLocal = voluSmoke.transform.InverseTransformPoint(rightResult).x;
                float newBaseMax = newLocal + pivotOffset.x;
                voluSmoke.cropRight = Mathf.Clamp01((baseHalf - newBaseMax) / voluSmoke.planeSize);
                voluSmoke.ClampCropValues();
                voluSmoke.GenerateMesh();
                EditorUtility.SetDirty(voluSmoke);
                EditorUtility.SetDirty(voluSmoke.transform);
                SceneView.RepaintAll();
                return;
            }


            Vector3 backWorld = voluSmoke.transform.TransformPoint(new Vector3(0f, 0f, crop.localMinZ));
            Handles.color = new Color(0.1f, 0.8f, 1f, 0.9f);
            EditorGUI.BeginChangeCheck();
            Vector3 backResult = Handles.Slider(backWorld, voluSmoke.transform.forward, handleSize, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Adjust Crop Back");
                float newLocal = voluSmoke.transform.InverseTransformPoint(backResult).z;
                float newBaseMinZ = newLocal + pivotOffset.z;
                voluSmoke.cropBack = Mathf.Clamp01((newBaseMinZ + baseHalf) / voluSmoke.planeSize);
                voluSmoke.ClampCropValues();
                voluSmoke.GenerateMesh();
                EditorUtility.SetDirty(voluSmoke);
                EditorUtility.SetDirty(voluSmoke.transform);
                SceneView.RepaintAll();
                return;
            }


            Vector3 forwardWorld = voluSmoke.transform.TransformPoint(new Vector3(0f, 0f, crop.localMaxZ));
            Handles.color = new Color(0.1f, 0.8f, 1f, 0.9f);

            EditorGUI.BeginChangeCheck();
            Vector3 forwardResult = Handles.Slider(forwardWorld, -voluSmoke.transform.forward, handleSize, Handles.ArrowHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { voluSmoke, voluSmoke.transform }, "Adjust Crop Forward");
                float newLocal = voluSmoke.transform.InverseTransformPoint(forwardResult).z;
                float newBaseMaxZ = newLocal + pivotOffset.z;
                voluSmoke.cropForward = Mathf.Clamp01((baseHalf - newBaseMaxZ) / voluSmoke.planeSize);
                voluSmoke.ClampCropValues();
                voluSmoke.GenerateMesh();
                EditorUtility.SetDirty(voluSmoke);
                EditorUtility.SetDirty(voluSmoke.transform);
                SceneView.RepaintAll();
            }
        }



        void DrawStatusBar()
        {
            GUILayout.Space(4);
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20));
            EditorGUI.DrawRect(r, Theme.NeutralBG2);

            EditorGUI.LabelField(new Rect(r.x + 8, r.y + 2, r.width - 16, r.height - 4),
                new GUIContent(BuildStatusText(), "Quick summary of the current object"),
                tinyMutedLabel);
        }

        string BuildStatusText()
        {
            var mf = voluSmoke.GetComponent<MeshFilter>();
            var mr = voluSmoke.GetComponent<MeshRenderer>();
            string mesh = (mf && mf.sharedMesh) ? mf.sharedMesh.name : "No mesh";
            string mat  = (mr && mr.sharedMaterial) ? mr.sharedMaterial.name : "No material";
            return $"Mesh: {mesh}   •   Material: {mat}";
        }



        void SavePreset()
        {

            string path = "Assets/VoluSmokePresets";
            EnsureFolder(path);


            VoluSmokePreset preset = ScriptableObject.CreateInstance<VoluSmokePreset>();
            preset.SaveFromGenerator(voluSmoke, presetName);


            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{presetName}.asset");
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();


            EditorUtility.DisplayProgressBar("Saving Preset", "Capturing preview image...", 0.5f);
            var captured = VoluSmokePreviewCapture.CapturePreview(voluSmoke, 256);
            EditorUtility.ClearProgressBar();

            VoluSmokePresetAssetUtility.SavePreviewTexture(preset, captured);
            Object.DestroyImmediate(captured);


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = $"Preset '{presetName}' saved.\nPath: {assetPath}";
            EditorUtility.DisplayDialog("Preset Saved", message, "OK");

            voluSmoke.EditorSetLastAppliedPreset(preset);


            EditorGUIUtility.PingObject(preset);
        }


        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Replace("\\", "/").Split('/');
            if (parts.Length == 0) return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        void MakePrefab()
        {
            MeshFilter meshFilter = voluSmoke.GetComponent<MeshFilter>();
            MeshRenderer originalRenderer = voluSmoke.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "No mesh to export. Generate a mesh first!", "OK");
                return;
            }

            if (originalRenderer == null || originalRenderer.sharedMaterial == null)
            {
                EditorUtility.DisplayDialog("Error", "No material assigned. Assign a material first!", "OK");
                return;
            }

            string prefabPath = EditorUtility.SaveFilePanelInProject(
                "Create VoluSmoke Prefab",
                voluSmoke.gameObject.name,
                "prefab",
                "Choose where to save the prefab"
            );

            if (string.IsNullOrEmpty(prefabPath))
                return;

            string prefabFolder = Path.GetDirectoryName(prefabPath);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string meshPath = $"{prefabFolder}/{prefabName}_Mesh.asset";
            string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolder}/{prefabName}_Material.mat");

            Mesh meshCopy = Object.Instantiate(meshFilter.sharedMesh);
            meshCopy.name = prefabName + "_Mesh";
            AssetDatabase.CreateAsset(meshCopy, meshPath);

            Material materialCopy = new Material(originalRenderer.sharedMaterial);
            materialCopy.name = prefabName + "_Material";
            AssetDatabase.CreateAsset(materialCopy, materialPath);
            AssetDatabase.SaveAssets();

            GameObject prefabObject = Object.Instantiate(voluSmoke.gameObject);
            prefabObject.name = prefabName;

            VoluSmokeMeshGenerator generatorComponent = prefabObject.GetComponent<VoluSmokeMeshGenerator>();
            if (generatorComponent != null)
                Object.DestroyImmediate(generatorComponent);

            var materialWatchers = prefabObject.GetComponentsInChildren<MaterialChangeWatcher>(true);
            for (int i = 0; i < materialWatchers.Length; i++)
            {
                Object.DestroyImmediate(materialWatchers[i]);
            }

            MeshFilter prefabMeshFilter = prefabObject.GetComponent<MeshFilter>();
            MeshRenderer prefabRenderer = prefabObject.GetComponent<MeshRenderer>();
            prefabMeshFilter.sharedMesh = meshCopy;
            prefabRenderer.sharedMaterial = materialCopy;
            prefabRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            prefabRenderer.receiveShadows = false;

            VoluSmokeShaderSetup shaderSetup = prefabObject.GetComponent<VoluSmokeShaderSetup>();
            if (shaderSetup != null)
            {
                shaderSetup.voluSmokeMaterial = materialCopy;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
            Object.DestroyImmediate(prefabObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Prefab Created",
                $"Prefab: {prefabPath}\nMesh: {meshPath}\nMaterial: {materialPath}",
                "OK");

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }
}
