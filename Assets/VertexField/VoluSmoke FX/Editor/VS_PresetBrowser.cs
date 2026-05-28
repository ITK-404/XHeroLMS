



using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace VertexField.VoluSmokeFX
{
    public class VoluSmokePresetBrowser : EditorWindow
    {
        private static VoluSmokeMeshGenerator targetGenerator;
        private readonly List<VoluSmokePreset> allPresets = new List<VoluSmokePreset>();
        private string searchFilter = "";
        private Vector2 scroll;

        [Range(120, 320)] public int previewSize = 180;


        const float Margin = 16f;
        const float CardPad = 10f;
        const float CardBottomSpace = 76f;
        const float ColSpacing = 16f;
        const float RowSpacing = 16f;
        const float BorderThickness = 2.6f;


        const float LiftMax = 6f;
        const float ScaleMax = 0.015f;
        const float ShadowAlphaSoft = 0.26f;
        const float ShadowInflate = 16f;
        const float HoverAnimSpeed = 10.0f;

        const float HardShadowOffsetX = 8f;
        const float HardShadowOffsetY = 10f;
        static readonly Color HardShadowColor = new Color(0f, 0f, 0f, 0.55f);

        const float TiltMaxDeg = 2.6f;
        const float TiltLerpSpeed = 9.0f;

        const float ParallaxMaxPx = 3f;

        const int TargetFPS = 30;
        private double _nextRepaintAt = 0;
        private bool _forceRepaint = false;


        const float FixedYOffset = 20f;


        private GUIStyle descStyle;
        private GUIStyle smallStyle;
        private GUIStyle toastTextStyle;
        private GUIStyle overlayTitleStyle;
        private GUIStyle infoStyle;
        private GUIStyle folderLabelStyle;

        private Rect _lastCanvasRect;
        private static readonly GUIContent TempContent = new GUIContent();
        private Vector2 _lastScroll;

        private Texture2D goldTex;
        private Texture2D greenTex;
        private Texture2D shadowTexSoft;
        private Texture2D cardBgTex;
        private Texture2D favStarTex;
        private Texture2D radialLight;
        private Texture2D foilTex;
        private Texture2D foilSpecTex;
        private Texture2D diagWashTex;
        private Texture2D white1px;
        private Texture2D overlayBgTex;
        private Texture2D folderIconTex;
        private Texture2D folderEmptyIconTex;
        private Texture2D folderUpIconTex;
        private static readonly string[] ShowAllIconNamesOn = new[] { "scenevis_visible_hover", "scenevis_visible_on", "SceneViewVisibility", "SceneViewFx" };
        private static readonly string[] ShowAllIconNamesOff = new[] { "scenevis_hidden_hover", "scenevis_hidden_on", "scenevis_visible_off", "VisibilityToggleOff", "scenevis_hidden" };

        private class Anim { public float hover; public float tilt; public float select; }
        private readonly Dictionary<string, Anim> anims = new Dictionary<string, Anim>();

        private double lastTime;

        private const string FavPrefsKey = "VoluSmokePresetBrowser.Favorites";
        private readonly HashSet<string> favoriteGuids = new HashSet<string>();
        private bool showFavoritesOnly = false;


        private const string FolderPrefKey = "VoluSmokePresetBrowser.CurrentFolder";
        private string currentFolderPath = "";
        private bool showAllFlattened = false;
        private string lastFolderBeforeShowAll = "";

        private enum EntryKind { Preset, Folder, Parent }
        private struct BrowserEntry
        {
            public EntryKind kind;
            public VoluSmokePreset preset;
            public string folderPath;
            public string displayName;
            public bool hasContent;
        }

        private struct CardGeom { public BrowserEntry entry; public Rect baseRect; public int col; public int row; }

        private class Toast { public Vector2 contentPos; public double start; public float life = 1.6f; public string text; }
        private readonly List<Toast> toasts = new List<Toast>();

        private class ClickFx { public Vector2 contentPos; public double start; public float life = 0.45f; }
        private readonly List<ClickFx> clickFx = new List<ClickFx>();


        private struct MouseContext
        {
            public Vector2 View;
            public Vector2 Content;
            public Vector2 Screen;
            public bool IsInsideView;
        }

        private struct CardTransform
        {
            public Rect baseRectContent;
            public Rect scaledRectContent;
            public Vector2 pivotContent;
            public Vector2 sizeContent;
            public float scale;
            public float tilt;
            public Matrix4x4 localToContent;
            public Matrix4x4 contentToLocal;
        }

        enum CardMode { Full, Compact, Ultra }

        [MenuItem("Window/VertexField/VoluSmoke Preset Browser")]
        public static void Open()
        {
            var w = GetWindow<VoluSmokePresetBrowser>("VoluSmoke Presets");
            w.minSize = new Vector2(640, 480);
            w.Show();
        }
        public static void Open(VoluSmokeMeshGenerator gen) { targetGenerator = gen; Open(); }

        void OnEnable()
        {
            BuildTextures();
            LoadFavorites();
            string stored = EditorPrefs.GetString(FolderPrefKey, "");
            if (!string.IsNullOrEmpty(stored) && !stored.Contains("/"))
            {
                currentFolderPath = string.Equals(stored, "General", StringComparison.OrdinalIgnoreCase)
                    ? ""
                    : NormalizeAssetPath(Path.Combine(PresetRoot, stored));
            }
            else
            {
                currentFolderPath = NormalizeAssetPath(stored);
            }
            if (!string.IsNullOrEmpty(currentFolderPath) && !currentFolderPath.StartsWith(PresetRoot, StringComparison.OrdinalIgnoreCase))
                currentFolderPath = "";
            lastFolderBeforeShowAll = currentFolderPath;
            EnsurePresetRootExists();
            RefreshList();
            lastTime = EditorApplication.timeSinceStartup;
            wantsMouseMove = true;
            EditorApplication.update += EditorUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }
        void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorPrefs.SetString(FolderPrefKey, currentFolderPath ?? "");
        }

        void OnSelectionChanged()
        {
            var go = Selection.activeGameObject;
            if (go != null) targetGenerator = go.GetComponent<VoluSmokeMeshGenerator>();
        }

        void EditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextRepaintAt) return;

            bool anyAnimating = toasts.Count > 0 || clickFx.Count > 0;
            if (!anyAnimating)
            {
                foreach (var kv in anims)
                    if (kv.Value.hover > 0.0001f || kv.Value.select > 0.0001f) { anyAnimating = true; break; }
            }
            if (anyAnimating || _forceRepaint || focusedWindow == this)
            {
                Repaint();
                _forceRepaint = false;
            }
            _nextRepaintAt = now + (1.0 / TargetFPS);
        }


        void BuildTextures()
        {
            goldTex = MakeSolid(new Color(1f, 0.82f, 0.25f, 1f));
            greenTex = MakeSolid(new Color(0.30f, 0.95f, 0.50f, 1f));
            cardBgTex = MakeSolid(new Color(0.20f, 0.20f, 0.20f, 0.92f));
            shadowTexSoft = MakeRadial(72, new Color(0, 0, 0, 1f), 0.95f, true);
            favStarTex = MakeStar(new Color(1f, 0.85f, 0.2f, 1f));
            radialLight = MakeRadial(256, new Color(1, 1, 1, 1), 1.0f, true);
            foilTex = MakeFoilFewBandsSeamless(512, 512);
            foilSpecTex = MakeFoilSpecularSeamless(512, 512);
            diagWashTex = MakeDiagonalWash(512, 512);
            white1px = MakeSolid(new Color(1, 1, 1, 1));
            overlayBgTex = MakeSolid(new Color(0f, 0f, 0f, 0.45f));
        }

        static void SetRepeat(Texture2D t) { if (t) t.wrapMode = TextureWrapMode.Repeat; }
        static Texture2D MakeSolid(Color c) { var t = new Texture2D(1, 1, TextureFormat.RGBA32, false); t.SetPixel(0, 0, c); t.Apply(); return t; }
        static Texture2D MakeStar(Color col)
        {
            int s = 16;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                int dx = Mathf.Abs(x - s / 2), dy = Mathf.Abs(y - s / 2);
                if (dx + dy <= 3) px[y * s + x] = col; else if (dx + dy == 4) { var c = col; c.a = 0.65f; px[y * s + x] = c; }
            }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Texture2D MakeRadial(int size, Color col, float falloff = 1f, bool smooth = true)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f, cx = r, cy = r;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / r, dy = (y - cy) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    if (smooth) a = a * a * (3f - 2f * a);
                    a *= falloff;
                    px[y * size + x] = new Color(col.r, col.g, col.b, a);
                }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Texture2D MakeFoilFewBandsSeamless(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color bandA = new Color(1f, 1f, 1f, 0.22f);
            Color bandB = new Color(0.92f, 0.96f, 1f, 0.12f);
            Color clear = new Color(0, 0, 0, 0);
            float bandsPerTile = 3.0f, bandFrac = 0.18f, feather = 0.10f;
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float s = Mathf.Repeat((u + v) * bandsPerTile, 1f);
                    Color c = clear;
                    if (s < bandFrac)
                    {
                        float dEdge = Mathf.Min(s, bandFrac - s) / Mathf.Max(0.0001f, feather);
                        float e = Mathf.Clamp01(dEdge);
                        e = e * e * (3f - 2f * e);
                        c = Color.Lerp(bandA, bandB, 1f - e);
                    }
                    px[y * w + x] = c;
                }
            }
            for (int y = 0; y < h; y++) px[y * w + (w - 1)] = px[y * w + 0];
            for (int x = 0; x < w; x++) px[(h - 1) * w + x] = px[0 * w + x];
            t.SetPixels(px); t.Apply(); SetRepeat(t);
            return t;
        }
        static Texture2D MakeFoilSpecularSeamless(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color bright = new Color(1f, 1f, 1f, 0.22f);
            Color clear = new Color(0, 0, 0, 0);
            float bandsPerTile = 3.0f, specFrac = 0.035f, specOffset = 0.18f * 0.27f;
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float s = Mathf.Repeat((u + v) * bandsPerTile, 1f);
                    Color c = clear;
                    if (s >= specOffset && s < specOffset + specFrac)
                    {
                        float vy = Mathf.Abs(v - 0.5f) * 2f;
                        float a = Mathf.Lerp(1f, 0.5f, vy);
                        c = new Color(bright.r, bright.g, bright.b, bright.a * a);
                    }
                    px[y * w + x] = c;
                }
            }
            for (int y = 0; y < h; y++) px[y * w + (w - 1)] = px[y * w + 0];
            for (int x = 0; x < w; x++) px[(h - 1) * w + x] = px[0 * w + x];
            t.SetPixels(px); t.Apply(); SetRepeat(t);
            return t;
        }
        static Texture2D MakeDiagonalWash(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = (x + y) / (float)(w + h);
                    float a = Mathf.Lerp(0.10f, 0.02f, n);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px); t.Apply(); return t;
        }

        void InitStyles()
        {
            if (descStyle == null)
            {
                descStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = Color.white }
                };
            }
            if (smallStyle == null)
            {
                smallStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.white }
                };
            }
            if (toastTextStyle == null) toastTextStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            if (overlayTitleStyle == null)
            {
                overlayTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.LowerLeft,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    fontSize = 12,
                    normal = { textColor = Color.white }
                };
            }
            if (infoStyle == null)
            {
                infoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    fontSize = 9,
                    normal = { textColor = Color.white }
                };
            }

            if (folderLabelStyle == null)
            {
                folderLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (!folderIconTex)
            {
                var icon = EditorGUIUtility.IconContent("Folder Icon");
                folderIconTex = icon != null ? icon.image as Texture2D : null;
            }
            if (!folderEmptyIconTex)
            {
                var iconEmpty = EditorGUIUtility.IconContent("FolderEmpty Icon");
                folderEmptyIconTex = iconEmpty != null ? iconEmpty.image as Texture2D : null;
                if (!folderEmptyIconTex) folderEmptyIconTex = folderIconTex;
            }
            if (!folderUpIconTex)
            {
                var iconUp = EditorGUIUtility.IconContent("FolderOpened Icon");
                folderUpIconTex = iconUp != null ? iconUp.image as Texture2D : null;
                if (!folderUpIconTex) folderUpIconTex = folderIconTex;
            }
        }

        void LoadFavorites()
        {
            favoriteGuids.Clear();
            string data = EditorPrefs.GetString(FavPrefsKey, "");
            if (!string.IsNullOrEmpty(data))
            {
                string[] parts = data.Split('|');
                for (int i = 0; i < parts.Length; i++)
                    if (!string.IsNullOrEmpty(parts[i])) favoriteGuids.Add(parts[i]);
            }
        }
        void SaveFavorites()
        {
            if (favoriteGuids.Count == 0) { EditorPrefs.DeleteKey(FavPrefsKey); return; }
            var list = new List<string>(favoriteGuids);
            string data = string.Join("|", list.ToArray());
            EditorPrefs.SetString(FavPrefsKey, data);
        }
        static string GetGuid(VoluSmokePreset p)
        {
            string path = AssetDatabase.GetAssetPath(p);
            return string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
        }
        bool IsFavorite(VoluSmokePreset p) { string g = GetGuid(p); return !string.IsNullOrEmpty(g) && favoriteGuids.Contains(g); }
        void ToggleFavorite(VoluSmokePreset p)
        {
            string g = GetGuid(p);
            if (string.IsNullOrEmpty(g)) return;
            if (favoriteGuids.Contains(g)) favoriteGuids.Remove(g);
            else favoriteGuids.Add(g);
            SaveFavorites();
            _forceRepaint = true;
        }


        string PresetRoot => VoluSmokePreset.PresetFolder.Replace("\\", "/");

        static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : path.Replace("\\", "/");
        }

        void EnsurePresetRootExists()
        {
            VoluSmokePreset.EnsurePresetFolder();
            EnsureFolder(PresetRoot);
        }

        void EnsureFolder(string path)
        {
            path = NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            if (parts.Length == 0) return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        string GetFolderLabel(string path)
        {
            if (string.IsNullOrEmpty(path)) return "All Presets";
            string normalized = NormalizeAssetPath(path);
            if (string.Equals(normalized, PresetRoot, StringComparison.OrdinalIgnoreCase))
                return "All Presets";
            if (normalized.StartsWith(PresetRoot + "/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(PresetRoot.Length + 1);
            return normalized;
        }

        string GetParentFolder(string folderPath)
        {
            string normalized = NormalizeAssetPath(folderPath).TrimEnd('/');
            if (string.IsNullOrEmpty(normalized)) return "";
            if (string.Equals(normalized, PresetRoot, StringComparison.OrdinalIgnoreCase)) return "";
            int slash = normalized.LastIndexOf('/');
            if (slash <= 0) return "";
            string parent = normalized.Substring(0, slash);
            if (!parent.StartsWith(PresetRoot, StringComparison.OrdinalIgnoreCase))
                return "";
            return parent;
        }

        void CollectSubfolders(string parent, List<string> list)
        {
            string[] subs = AssetDatabase.GetSubFolders(parent);
            Array.Sort(subs, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < subs.Length; i++)
            {
                string sub = NormalizeAssetPath(subs[i]);
                if (!list.Contains(sub))
                    list.Add(sub);
                CollectSubfolders(sub, list);
            }
        }

        List<string> GetAllPresetFolders()
        {
            EnsurePresetRootExists();
            var result = new List<string> { PresetRoot };
            CollectSubfolders(PresetRoot, result);
            return result;
        }

        void SetCurrentFolder(string folderPath, bool refresh)
        {
            showAllFlattened = false;
            currentFolderPath = NormalizeAssetPath(folderPath);
            if (string.Equals(currentFolderPath, PresetRoot, StringComparison.OrdinalIgnoreCase))
                currentFolderPath = "";
            if (!string.IsNullOrEmpty(currentFolderPath) && !currentFolderPath.StartsWith(PresetRoot, StringComparison.OrdinalIgnoreCase))
                currentFolderPath = "";
            lastFolderBeforeShowAll = currentFolderPath;
            EditorPrefs.SetString(FolderPrefKey, currentFolderPath ?? "");
            _forceRepaint = true;
            if (refresh) RefreshList();
        }

        bool IsInFolder(VoluSmokePreset p, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return true;
            if (!p) return false;
            string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(p));
            if (string.IsNullOrEmpty(assetPath)) return false;
            string assetFolder = NormalizeAssetPath(Path.GetDirectoryName(assetPath));
            string normalizedFolder = NormalizeAssetPath(folderPath).TrimEnd('/');
            return string.Equals(assetFolder, normalizedFolder, StringComparison.OrdinalIgnoreCase);
        }

        bool FolderHasVisiblePresets(string folderPath, string filterLower)
        {
            string normalizedFolder = NormalizeAssetPath(folderPath).TrimEnd('/');
            if (string.IsNullOrEmpty(normalizedFolder)) normalizedFolder = PresetRoot;

            for (int i = 0; i < allPresets.Count; i++)
            {
                var preset = allPresets[i];
                if (!preset) continue;
                string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(preset));
                if (string.IsNullOrEmpty(assetPath)) continue;
                string assetFolder = NormalizeAssetPath(Path.GetDirectoryName(assetPath));
                if (!string.Equals(assetFolder, normalizedFolder, StringComparison.OrdinalIgnoreCase)) continue;
                if (showFavoritesOnly && !IsFavorite(preset)) continue;
                if (!string.IsNullOrEmpty(filterLower))
                {
                    string name = preset.presetName ?? "";
                    if (!name.ToLower().Contains(filterLower)) continue;
                }
                return true;
            }

            string[] subs = AssetDatabase.GetSubFolders(normalizedFolder);
            return subs != null && subs.Length > 0;
        }

        Texture2D GetFolderIconTexture(BrowserEntry entry)
        {
            if (entry.kind == EntryKind.Parent)
                return folderUpIconTex ?? folderIconTex ?? folderEmptyIconTex;

            return entry.hasContent
                ? (folderIconTex ?? folderEmptyIconTex)
                : (folderEmptyIconTex ?? folderIconTex);
        }

        List<BrowserEntry> BuildBrowserEntries(List<VoluSmokePreset> presets, string filterLower)
        {
            var entries = new List<BrowserEntry>();
            bool flatten = showAllFlattened;
            string effectiveFolder = flatten ? PresetRoot : (string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath);
            string normalizedEffective = NormalizeAssetPath(effectiveFolder).TrimEnd('/');
            bool inAllMode = flatten || string.IsNullOrEmpty(currentFolderPath);

            if (!flatten && !string.IsNullOrEmpty(currentFolderPath))
            {
                string parent = GetParentFolder(effectiveFolder);
                string parentLabel = string.IsNullOrEmpty(parent) ? "All Presets" : "Up to " + GetFolderLabel(parent);
                entries.Add(new BrowserEntry
                {
                    kind = EntryKind.Parent,
                    folderPath = parent,
                    displayName = parentLabel,
                    hasContent = true
                });
            }

            string[] subfolders = flatten ? Array.Empty<string>() : AssetDatabase.GetSubFolders(effectiveFolder);
            Array.Sort(subfolders, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < subfolders.Length; i++)
            {
                string folder = NormalizeAssetPath(subfolders[i]);
                string leaf = Path.GetFileName(folder);
                bool matchesFilter = string.IsNullOrEmpty(filterLower) || (leaf ?? "").ToLower().Contains(filterLower);
                bool hasVisible = FolderHasVisiblePresets(folder, filterLower);
                if (!hasVisible && !matchesFilter && !string.IsNullOrEmpty(filterLower)) continue;

                entries.Add(new BrowserEntry
                {
                    kind = EntryKind.Folder,
                    folderPath = folder,
                    displayName = string.IsNullOrEmpty(leaf) ? folder : leaf,
                    hasContent = hasVisible
                });
            }

            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (!preset) continue;
                string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(preset));
                if (string.IsNullOrEmpty(assetPath)) continue;
                string presetFolder = NormalizeAssetPath(Path.GetDirectoryName(assetPath)).TrimEnd('/');
                if (!flatten && !string.Equals(presetFolder, normalizedEffective, StringComparison.OrdinalIgnoreCase)) continue;

                entries.Add(new BrowserEntry
                {
                    kind = EntryKind.Preset,
                    preset = preset,
                    folderPath = presetFolder,
                    displayName = preset.presetName ?? "Preset",
                    hasContent = true
                });
            }

            if (!flatten && entries.Count == 0 && inAllMode && string.IsNullOrEmpty(filterLower))
            {
                for (int i = 0; i < subfolders.Length; i++)
                {
                    string folder = NormalizeAssetPath(subfolders[i]);
                    string leaf = Path.GetFileName(folder);
                    entries.Add(new BrowserEntry
                    {
                        kind = EntryKind.Folder,
                        folderPath = folder,
                        displayName = string.IsNullOrEmpty(leaf) ? folder : leaf,
                        hasContent = FolderHasVisiblePresets(folder, filterLower)
                    });
                }
            }

            return entries;
        }

        string GetPresetAnimKey(VoluSmokePreset preset)
        {
            if (!preset) return "preset:null";
            string guid = GetGuid(preset);
            if (!string.IsNullOrEmpty(guid)) return "preset:" + guid;
            return "preset_inst:" + preset.GetInstanceID();
        }

        static string GetFolderAnimKey(string folderPath)
        {
            return "folder:" + NormalizeAssetPath(folderPath);
        }

        static string GetParentAnimKey(string folderPath)
        {
            return "parent:" + NormalizeAssetPath(folderPath);
        }

        string GetEntryAnimKey(BrowserEntry entry)
        {
            switch (entry.kind)
            {
                case EntryKind.Preset: return GetPresetAnimKey(entry.preset);
                case EntryKind.Folder: return GetFolderAnimKey(entry.folderPath);
                case EntryKind.Parent: return GetParentAnimKey(entry.folderPath);
                default: return "entry:unknown";
            }
        }

        void RefreshList()
        {
            allPresets.Clear();
            EnsurePresetRootExists();
            string folder = PresetRoot;

            string[] guids = AssetDatabase.FindAssets("t:VoluSmokePreset", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var pr = AssetDatabase.LoadAssetAtPath<VoluSmokePreset>(path);
                if (pr != null) allPresets.Add(pr);
            }

            VoluSmokePreset starting = VoluSmokePreset.LoadDefaultPreset();
            allPresets.Sort((a, b) => {
                if (a == starting && b != starting) return -1;
                if (b == starting && a != starting) return 1;
                return string.Compare(a.presetName, b.presetName, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < allPresets.Count; i++)
            {
                string key = GetPresetAnimKey(allPresets[i]);
                if (!anims.ContainsKey(key)) anims[key] = new Anim();
            }
            var keep = new HashSet<string>();
            for (int i = 0; i < allPresets.Count; i++) keep.Add(GetPresetAnimKey(allPresets[i]));
            var keys = new List<string>(anims.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if ((key.StartsWith("preset:", StringComparison.Ordinal) || key.StartsWith("preset_inst:", StringComparison.Ordinal)) && !keep.Contains(key))
                    anims.Remove(key);
            }

            _forceRepaint = true;
        }

        void OnGUI()
        {
            InitStyles();

            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp01((float)(now - lastTime));
            lastTime = now;

            DrawTopBar();
            DrawGrid(dt, (float)now);
            DrawClickFx((float)now);
            DrawToasts((float)now);
        }

        void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                float w = position.width;

                GUILayout.Space(4);

                GUIContent favContent;
                float favWidth;
                if (favStarTex != null)
                {
                    if (w < 760)
                    {
                        favContent = new GUIContent(favStarTex, "Show only favorite presets");
                        favWidth = 28f;
                    }
                    else
                    {
                        favContent = new GUIContent(" Favorites", favStarTex, "Show only favorite presets");
                        favWidth = 100f;
                    }
                }
                else
                {
                    string fallbackLabel = w < 760 ? "*" : "* Favorites";
                    favContent = new GUIContent(fallbackLabel, "Show only favorite presets");
                    favWidth = w < 760 ? 32f : 100f;
                }

                bool newFavOnly = GUILayout.Toggle(showFavoritesOnly, favContent,
                                                   EditorStyles.toolbarButton, GUILayout.Width(favWidth));
                if (newFavOnly != showFavoritesOnly) { showFavoritesOnly = newFavOnly; _forceRepaint = true; }

                GUILayout.Space(6);

                GUIContent showAllIcon = GetShowAllIconContent(showAllFlattened,
                                                               "Showing all presets (click to return to folders)",
                                                               "Show all presets without folder grouping");
                bool newShowAll = GUILayout.Toggle(showAllFlattened, showAllIcon, EditorStyles.toolbarButton, GUILayout.Width(26));
                if (newShowAll != showAllFlattened)
                {
                    showAllFlattened = newShowAll;
                    if (showAllFlattened)
                    {
                        lastFolderBeforeShowAll = currentFolderPath;
                        RefreshList();
                    }
                    else
                    {
                        string restore = lastFolderBeforeShowAll;
                        if (!string.Equals(restore ?? "", currentFolderPath ?? "", StringComparison.OrdinalIgnoreCase))
                            SetCurrentFolder(restore ?? "", true);
                        else
                            RefreshList();
                    }
                    _forceRepaint = true;
                }

                GUILayout.Space(6);

                GUIContent folderIcon = EditorGUIUtility.IconContent("Folder Icon");
                folderIcon.tooltip = "Folder options: Select Folder, Add Folder, Open Current Folder";
                if (GUILayout.Button(folderIcon, EditorStyles.toolbarButton, GUILayout.Width(26)))
                    ShowFolderMenu();

                if (w >= 720) GUILayout.Label("Search:", GUILayout.Width(52));
                string newFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.MinWidth(80));
                if (newFilter != searchFilter) { searchFilter = newFilter; _forceRepaint = true; }

                GUILayout.FlexibleSpace();

                if (w >= 860) GUILayout.Label("Preview Size:", GUILayout.Width(86));
                int newSize = (int)GUILayout.HorizontalSlider(previewSize, 130, 280, GUILayout.Width(140));
                if (newSize != previewSize) { previewSize = newSize; _forceRepaint = true; }

                GUILayout.Space(6);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RefreshList();

                GUILayout.Space(10);
                string folderLabel = showAllFlattened ? "All Presets (All Folders)" : GetFolderLabel(currentFolderPath);
                GUIContent folderGC = EditorGUIUtility.IconContent("Folder Icon");
                GUILayout.Label(new GUIContent(" " + folderLabel, folderGC.image), EditorStyles.miniLabel, GUILayout.Height(16), GUILayout.MaxWidth(220));
            }
        }

        static GUIContent GetShowAllIconContent(bool flattened, string tooltipOn, string tooltipOff)
        {
            var names = flattened ? ShowAllIconNamesOn : ShowAllIconNamesOff;
            string tooltip = flattened ? tooltipOn : tooltipOff;
            foreach (var iconName in names)
            {
                var content = EditorGUIUtility.IconContent(iconName);
                if (content != null && content.image != null)
                    return new GUIContent(content.image, tooltip);
            }
            return new GUIContent(flattened ? "All" : "Folders", tooltip);
        }
        void ShowFolderMenu()
        {
            GenericMenu m = new GenericMenu();


            var folders = GetAllPresetFolders();
            if (folders.Count > 0)
            {
                m.AddSeparator("");
                foreach (var f in folders)
                {
                    if (string.Equals(f, PresetRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    string fPath = NormalizeAssetPath(f);
                    string display = GetFolderLabel(fPath);
                    m.AddItem(new GUIContent("Select Folder/" + display), string.Equals(currentFolderPath, fPath, StringComparison.OrdinalIgnoreCase), () =>
                    {
                        SetCurrentFolder(fPath, true);
                    });
                }
            }


            m.AddSeparator("");
            m.AddItem(new GUIContent("Add Folder..."), false, () =>
            {
                TextInputPopup.Show("Add Preset Folder", "Folder name:", "NewPresetFolder", (folderName) =>
                {
                    folderName = SanitizeFolderName(folderName);
                    if (string.IsNullOrEmpty(folderName)) return;

                    string parent = string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath;
                    string newPath = NormalizeAssetPath(Path.Combine(parent, folderName));
                    EnsureFolder(newPath);
                    AssetDatabase.Refresh();
                    SetCurrentFolder(newPath, true);
                });
            });


            m.AddItem(new GUIContent("Open Current Folder"), false, () =>
            {
                string path = string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath;
                EditorUtility.RevealInFinder(path);
            });

            m.ShowAsContext();
        }

        CardMode GetCardMode()
        {
            float pct = Mathf.InverseLerp(130f, 280f, previewSize);
            if (pct < 0.20f) return CardMode.Ultra;
            if (pct < 0.30f) return CardMode.Compact;
            return CardMode.Full;
        }

        void GetCardDims(CardMode mode, out float cardW, out float cardH, out float innerPreviewSize)
        {
            if (mode == CardMode.Ultra)
            {
                float s = Mathf.Max(64f, previewSize * 0.5f);
                innerPreviewSize = s * 1.10f;
                cardW = innerPreviewSize + CardPad * 2f;
                cardH = innerPreviewSize + CardPad * 2f;
                return;
            }
            if (mode == CardMode.Compact)
            {
                innerPreviewSize = previewSize * 1.10f;
                cardW = innerPreviewSize + CardPad * 2f;
                cardH = innerPreviewSize + CardPad * 2f;
                return;
            }
            innerPreviewSize = previewSize * 1.15f;
            cardW = innerPreviewSize + CardPad * 2f;
            cardH = innerPreviewSize + CardBottomSpace;
        }

        static Rect ScaleRectAroundPivot(Rect r, Vector2 pivotContent, float s)
        {
            Vector2 min = r.min, max = r.max;
            min = pivotContent + (min - pivotContent) * s;
            max = pivotContent + (max - pivotContent) * s;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Matrix4x4 BuildCardLocalToContent(Rect rect, Vector2 pivotContent, float tiltDeg)
        {
            float safeWidth = Mathf.Max(1e-4f, rect.width);
            float safeHeight = Mathf.Max(1e-4f, rect.height);

            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(safeWidth, safeHeight, 1f));
            Matrix4x4 translateTopLeft = Matrix4x4.Translate(new Vector3(rect.x, rect.y, 0f));
            Matrix4x4 translateToPivot = Matrix4x4.Translate(new Vector3(pivotContent.x, pivotContent.y, 0f));
            Matrix4x4 translateFromPivot = Matrix4x4.Translate(new Vector3(-pivotContent.x, -pivotContent.y, 0f));
            Matrix4x4 rotate = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, tiltDeg));

            return translateToPivot * rotate * translateFromPivot * translateTopLeft * scale;
        }

        CardTransform GetCardTransform(Rect baseRect, Anim a)
        {
            CardTransform t;
            t.baseRectContent = baseRect;

            float lift = LiftMax * a.hover + 2.0f * a.select;
            Rect lifted = new Rect(baseRect.x, baseRect.y - lift, baseRect.width, baseRect.height);
            Vector2 pivot = new Vector2(lifted.x + lifted.width * 0.5f, lifted.y + lifted.height * 0.5f);
            t.pivotContent = pivot;

            t.scale = 1f + ScaleMax * a.hover + 0.02f * a.select;
            t.scaledRectContent = ScaleRectAroundPivot(lifted, pivot, t.scale);
            t.tilt = a.tilt;
            t.sizeContent = t.scaledRectContent.size;

            t.localToContent = BuildCardLocalToContent(t.scaledRectContent, t.pivotContent, t.tilt);
            t.contentToLocal = t.localToContent.inverse;
            return t;
        }

        bool HitTestExact(MouseContext mouse, CardTransform tr)
        {
            if (!mouse.IsInsideView) return false;
            Vector3 local01 = tr.contentToLocal.MultiplyPoint3x4(new Vector3(mouse.Content.x, mouse.Content.y, 0f));
            return (local01.x >= 0f && local01.x <= 1f && local01.y >= 0f && local01.y <= 1f);
        }

        Vector2 GetMouseInCardLocal(MouseContext mouse, CardTransform tr)
        {
            Vector3 local01 = tr.contentToLocal.MultiplyPoint3x4(new Vector3(mouse.Content.x, mouse.Content.y, 0f));
            local01.x = Mathf.Clamp01(local01.x);
            local01.y = Mathf.Clamp01(local01.y);
            return new Vector2(local01.x * tr.sizeContent.x, local01.y * tr.sizeContent.y);
        }

        static float GetCardVisualScale(Rect cardRect)
        {
            float longest = Mathf.Max(cardRect.width, cardRect.height);
            return Mathf.Clamp01(Mathf.InverseLerp(110f, 280f, longest));
        }

        void DrawGrid(float dt, float timeNow)
        {
            var filtered = new List<VoluSmokePreset>();
            string f = string.IsNullOrEmpty(searchFilter) ? "" : searchFilter.ToLower();
            for (int i = 0; i < allPresets.Count; i++)
            {
                var p = allPresets[i];
                if (!p) continue;
                if (showFavoritesOnly && !IsFavorite(p)) continue;
                if (!showAllFlattened && !string.IsNullOrEmpty(currentFolderPath) && !IsInFolder(p, currentFolderPath)) continue;
                if (!string.IsNullOrEmpty(f))
                {
                    string name = p.presetName ?? "";
                    if (!name.ToLower().Contains(f)) continue;
                }
                filtered.Add(p);
            }

            var entries = BuildBrowserEntries(filtered, f);

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    showFavoritesOnly
                        ? "No favorite presets match your filter."
                        : "No presets found here.\nUse the context menu or folder menu to add/move presets.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!showFavoritesOnly)
                    {
                        if (GUILayout.Button("Ensure Preset Folder", GUILayout.Height(26)))
                        { VoluSmokePreset.EnsurePresetFolder(); AssetDatabase.Refresh(); RefreshList(); }
                        if (GUILayout.Button("Open Current Folder", GUILayout.Height(26)))
                        {
                            string path = string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath;
                            EditorUtility.RevealInFinder(path);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Show All", GUILayout.Height(26)))
                        { showFavoritesOnly = false; _forceRepaint = true; }
                    }
                }
                return;
            }

            CardMode mode = GetCardMode();
            GetCardDims(mode, out float cardW, out float cardH, out float innerPreviewSize);

            float viewW = position.width - Margin * 2f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((viewW + ColSpacing) / (cardW + ColSpacing)));
            int rows = Mathf.CeilToInt((float)entries.Count / cols);
            float contentH = Margin + rows * (cardH + RowSpacing) - RowSpacing + Margin;

            Rect canvasRect = GUILayoutUtility.GetRect(position.width, position.height, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Rect contentRect = new Rect(0, 0, Mathf.Max(viewW, canvasRect.width), contentH);

            Vector2 mouseWindow = Event.current.mousePosition;
            scroll = GUI.BeginScrollView(canvasRect, scroll, contentRect, false, true);
            _lastCanvasRect = canvasRect;
            _lastScroll = scroll;

            Vector2 mouseView = mouseWindow - canvasRect.position;
            mouseView.y -= FixedYOffset;
            Vector2 mouseScreen = GUIUtility.GUIToScreenPoint(mouseWindow);

            MouseContext mouseCtx = new MouseContext
            {
                View = mouseView,
                Content = mouseView + scroll,
                Screen = mouseScreen,
                IsInsideView = new Rect(0, 0, canvasRect.width, canvasRect.height).Contains(mouseView)
            };

            var geoms = new List<CardGeom>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                CardGeom g;
                g.entry = entries[i];
                g.baseRect = new Rect(Margin + col * (cardW + ColSpacing), Margin + row * (cardH + RowSpacing), cardW, cardH);
                g.col = col;
                g.row = row;
                geoms.Add(g);
            }

            float viewMinY = scroll.y - RowSpacing - 100f;
            float viewMaxY = scroll.y + canvasRect.height + RowSpacing + 100f;

            bool needRepaint = false;
            VoluSmokePreset startingPreset = VoluSmokePreset.LoadDefaultPreset();

            string hoveredKey = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < geoms.Count; i++)
            {
                var g = geoms[i];
                BrowserEntry entry = g.entry;
                string key = GetEntryAnimKey(entry);
                if (!anims.TryGetValue(key, out Anim aTmp)) { aTmp = new Anim(); anims[key] = aTmp; }
                CardTransform t = GetCardTransform(g.baseRect, aTmp);
                if (!HitTestExact(mouseCtx, t)) continue;
                float score = aTmp.hover * 10f + (10000f - Vector2.Distance(mouseCtx.Content, t.scaledRectContent.center));
                if (score > bestScore) { bestScore = score; hoveredKey = key; }
            }

            for (int i = 0; i < geoms.Count; i++)
                DrawEntry(geoms[i], hoveredKey, mouseCtx, mode, innerPreviewSize, dt, timeNow, viewMinY, viewMaxY, ref needRepaint, startingPreset);

            GUI.EndScrollView();

            if (needRepaint) _forceRepaint = true;
        }

        void DrawEntry(CardGeom g, string hoveredKey, MouseContext mouse, CardMode mode, float innerPreviewSize, float dt, float timeNow, float viewMinY, float viewMaxY, ref bool needRepaint, VoluSmokePreset startingPreset)
        {
            BrowserEntry entry = g.entry;
            bool isPreset = entry.kind == EntryKind.Preset;
            if (isPreset && !entry.preset) return;

            string animKey = GetEntryAnimKey(entry);
            if (!anims.TryGetValue(animKey, out Anim a)) { a = new Anim(); anims[animKey] = a; }

            Rect baseRect = g.baseRect;

            if (baseRect.yMax < viewMinY || baseRect.y > viewMaxY)
            {
                float beforeHover = a.hover;
                a.hover = Mathf.MoveTowards(a.hover, 0f, dt * HoverAnimSpeed);
                float beforeSelect = a.select;
                a.select = Mathf.MoveTowards(a.select, 0f, dt * 3.5f);
                if (Mathf.Abs(a.hover - beforeHover) > 0.0001f || Mathf.Abs(a.select - beforeSelect) > 0.0001f)
                    needRepaint = true;
                anims[animKey] = a;
                return;
            }

            CardTransform tr = GetCardTransform(baseRect, a);
            bool overCard = HitTestExact(mouse, tr);
            bool isHover = hoveredKey == animKey && mouse.IsInsideView;
            bool highlight = isPreset ? overCard : (overCard || isHover);

            float beforeHoverAnim = a.hover;
            a.hover = Mathf.MoveTowards(a.hover, highlight ? 1f : 0f, dt * HoverAnimSpeed);

            float beforeSelectAnim = a.select;
            a.select = Mathf.MoveTowards(a.select, 0f, dt * 3.5f);

            float targetTilt = 0f;
            if (highlight)
            {
                Vector3 local01 = tr.contentToLocal.MultiplyPoint3x4(new Vector3(mouse.Content.x, mouse.Content.y, 0f));
                float nx = Mathf.Clamp(local01.x, -0.25f, 1.25f);
                targetTilt = (nx - 0.5f) * 2f * TiltMaxDeg;
            }
            a.tilt = Mathf.Lerp(a.tilt, targetTilt, 1f - Mathf.Exp(-TiltLerpSpeed * dt));

            tr = GetCardTransform(baseRect, a);
            anims[animKey] = a;

            if (Mathf.Abs(a.hover - beforeHoverAnim) > 0.0001f || Mathf.Abs(a.select - beforeSelectAnim) > 0.0001f)
                needRepaint = true;

            Rect scaled = tr.scaledRectContent;
            float cardScaleVisual = GetCardVisualScale(scaled);

            if (Event.current.type == EventType.Repaint)
            {
                float hsMul = 1f + 0.7f * a.hover + 0.4f * a.select;
                float hsOffX = HardShadowOffsetX * hsMul + a.tilt * 1.7f;
                float hsOffY = HardShadowOffsetY * hsMul + Mathf.Lerp(0f, 6f, a.hover) + 4f * a.select;
                Color hsCol = HardShadowColor;
                hsCol.a = Mathf.Clamp01(HardShadowColor.a * (1f + 0.35f * a.hover + 0.2f * a.select));
                EditorGUI.DrawRect(new Rect(scaled.x + hsOffX, scaled.y + hsOffY, scaled.width, scaled.height), hsCol);

                float h = Mathf.SmoothStep(0f, 1f, a.hover);
                Color old = GUI.color;
                GUI.color = new Color(0, 0, 0, ShadowAlphaSoft * (0.8f + 0.4f * h + 0.3f * a.select));
                Rect sh = new Rect(
                    scaled.x - ShadowInflate * 0.4f + a.tilt * 0.5f,
                    scaled.y - ShadowInflate * 0.1f,
                    scaled.width + ShadowInflate * 1.1f,
                    scaled.height + ShadowInflate * 1.0f
                );
                GUI.DrawTexture(sh, shadowTexSoft, ScaleMode.StretchToFill, true);
                GUI.color = old;

                Matrix4x4 prev = GUI.matrix;
                if (Mathf.Abs(a.tilt) > 0.001f)
                    GUIUtility.RotateAroundPivot(a.tilt, tr.pivotContent);

                GUI.BeginGroup(scaled);
                {
                    if (isPreset)
                    {
                        float maxBrighten = Mathf.Lerp(0.18f, 0.10f, cardScaleVisual);
                        float brighten = highlight ? Mathf.Lerp(0f, maxBrighten, a.hover) : 0f;
                        GUI.color = new Color(1f + brighten, 1f + brighten, 1f + brighten, 1f);
                        GUI.DrawTexture(new Rect(0, 0, scaled.width, scaled.height), cardBgTex, ScaleMode.StretchToFill);
                        GUI.color = Color.white;

                        GUI.color = new Color(1f, 1f, 1f, 0.07f + 0.05f * a.hover);
                        GUI.DrawTexture(new Rect(0, 0, scaled.width, scaled.height), diagWashTex, ScaleMode.StretchToFill, true);
                        GUI.color = Color.white;

                        GUI.color = new Color(0f, 0f, 0f, 0.12f + 0.10f * a.hover);
                        Rect aoR = new Rect(scaled.width * 0.35f, scaled.height * 0.55f, scaled.width * 0.60f, scaled.height * 0.60f);
                        GUI.DrawTexture(aoR, shadowTexSoft, ScaleMode.StretchToFill, true);
                        GUI.color = Color.white;

                        Rect inner = new Rect(CardPad, CardPad, scaled.width - CardPad * 2f, scaled.height - CardPad * 2f);
                        bool isStart = (startingPreset && entry.preset == startingPreset);
                        Rect previewR = DrawCardContentsLocal(inner, entry.preset, isStart, highlight, a, mode, innerPreviewSize, timeNow, scaled);

                        if (highlight)
                        {
                            float hoverT = Mathf.SmoothStep(0f, 1f, a.hover);
                            float radiusMin = Mathf.Lerp(26f, 54f, cardScaleVisual);
                            float radiusMax = Mathf.Lerp(90f, 140f, cardScaleVisual);
                            float radius = Mathf.Lerp(radiusMin, radiusMax, hoverT);
                            float glowAlpha = Mathf.Lerp(0.34f, 0.18f, cardScaleVisual) * hoverT;

                            Vector2 mouseLocal = GetMouseInCardLocal(mouse, tr);
                            GUI.color = new Color(1, 1, 1, glowAlpha);
                            Rect glowRect = new Rect(mouseLocal.x - radius, mouseLocal.y - radius, radius * 2f, radius * 2f);
                            GUI.DrawTexture(glowRect, radialLight, ScaleMode.StretchToFill, true);
                            GUI.color = Color.white;
                        }

                        bool isFav = IsFavorite(entry.preset);
                        if (isFav)
                        {
                            float star = 16f;
                            Rect starR = new Rect(scaled.width - star - 6f, 6f, star, star);
                            GUI.DrawTexture(starR, favStarTex, ScaleMode.ScaleToFit, true);

                            int id = entry.preset ? entry.preset.GetInstanceID() : 0;
                            float tScroll = timeNow * 0.035f + (id & 7) * 0.09f;
                            Rect uv = new Rect(tScroll % 1f, (tScroll * 0.6f) % 1f, 1f, 1f);

                            GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.18f, 0.26f, a.hover));
                            GUI.DrawTextureWithTexCoords(new Rect(0, 0, scaled.width, scaled.height), foilTex, uv, true);
                            GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.14f, 0.20f, a.hover));
                            GUI.DrawTextureWithTexCoords(new Rect(0, 0, scaled.width, scaled.height), foilSpecTex, uv, true);
                            GUI.color = Color.white;

                            float gap = 2f;
                            DrawFrameLocal(new Rect(gap, gap, scaled.width - gap * 2f, scaled.height - gap * 2f), BorderThickness, goldTex);
                        }

                        if (isStart)
                        {
                            DrawFrameLocal(new Rect(0, 0, scaled.width, scaled.height), BorderThickness, greenTex);
                        }

                        if (mode != CardMode.Ultra)
                        {
                            Rect menuLocalDraw = new Rect(scaled.width - 28f, scaled.height - 24f, 24f, 20f);
                            if (GUI.Button(menuLocalDraw, "\u22EE"))
                                ShowMenu(entry.preset);
                        }

                        if (a.select > 0f)
                        {
                            float t = 1f - a.select;
                            float ringRadius = Mathf.Lerp(30f, Mathf.Max(scaled.width, scaled.height) * 0.9f, t);
                            float alpha = Mathf.SmoothStep(0.25f, 0f, t);
                            GUI.color = new Color(1f, 1f, 1f, alpha);
                            Rect rr = new Rect(scaled.width * 0.5f - ringRadius, scaled.height * 0.5f - ringRadius, ringRadius * 2f, ringRadius * 2f);
                            GUI.DrawTexture(rr, radialLight, ScaleMode.StretchToFill, true);
                            GUI.color = new Color(1, 1, 1, Mathf.SmoothStep(0.35f, 0f, t));
                            DrawFrameLocal(new Rect(0, 0, scaled.width, scaled.height), 2.0f, white1px);
                            GUI.color = Color.white;
                        }
                    }
                    else
                    {
                        DrawFolderContents(new Rect(0, 0, scaled.width, scaled.height), entry, highlight, a, cardScaleVisual, mode);
                    }
                }
                GUI.EndGroup();
                GUI.matrix = prev;
            }

            if (entry.kind == EntryKind.Preset)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && overCard)
                {
                    ShowCardContextMenu(entry.preset);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && overCard)
                {
                    ApplyPreset(entry.preset, mouse.Content);
                    a.select = 1f;
                    anims[animKey] = a;
                    Event.current.Use();
                }
            }
            else
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && overCard)
                {
                    ShowFolderContextMenu(entry);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && overCard)
                {
                    HandleFolderClick(entry, mouse);
                    Event.current.Use();
                }
            }
        }

        void DrawFolderContents(Rect area, BrowserEntry entry, bool highlight, Anim anim, float cardScaleVisual, CardMode mode)
        {
            float labelHeight = Mathf.Clamp(area.height * 0.28f, 18f, 30f);
            float iconMaxHeight = Mathf.Max(32f, area.height - labelHeight - CardPad);
            float iconSize = Mathf.Min(iconMaxHeight, area.width - CardPad * 1.5f);
            iconSize = Mathf.Clamp(iconSize, 28f, area.width - CardPad * 0.5f);

            float iconX = area.x + (area.width - iconSize) * 0.5f;
            float iconYOffset = area.y + Mathf.Max(CardPad * 0.4f, (area.height - labelHeight - iconSize) * 0.5f);
            Rect iconRect = new Rect(iconX, iconYOffset, iconSize, iconSize);

            Texture2D iconTex = GetFolderIconTexture(entry);
            float alpha = entry.hasContent ? 1f : 0.65f;
            float brighten = highlight ? Mathf.Lerp(0f, 0.2f, anim.hover) : 0f;
            GUI.color = new Color(1f + brighten, 1f + brighten, 1f + brighten, alpha);
            if (iconTex)
                GUI.DrawTexture(iconRect, iconTex, ScaleMode.ScaleToFit, true);
            else
                EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f, alpha));
            GUI.color = Color.white;

            if (highlight)
            {
                float hoverT = Mathf.SmoothStep(0f, 1f, anim.hover);
                float radius = Mathf.Lerp(iconSize * 0.5f, iconSize * 0.9f, cardScaleVisual);
                Vector2 center = iconRect.center;
                GUI.color = new Color(1f, 1f, 1f, 0.12f * hoverT);
                GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), radialLight, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }

            string label = entry.displayName ?? "";
            Rect labelRect = new Rect(area.x + CardPad * 0.4f, area.yMax - labelHeight - CardPad * 0.2f, area.width - CardPad * 0.8f, labelHeight);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, highlight ? 0.62f : 0.45f));
            GUIStyle labelStyle = folderLabelStyle ?? new GUIStyle(EditorStyles.boldLabel);
            int originalFont = labelStyle.fontSize;
            int referenceFont = originalFont > 0 ? originalFont : 12;
            if (mode == CardMode.Ultra)
                labelStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(referenceFont * 0.72f));

            string display = FitTextToWidth(labelStyle, label, labelRect.width - 8f);
            Rect labelInner = new Rect(labelRect.x + 4f, labelRect.y + 2f, labelRect.width - 8f, labelRect.height - 4f);
            TempContent.text = display;
            TempContent.tooltip = label;
            GUI.Label(labelInner, TempContent, labelStyle);
            TempContent.tooltip = null;

            if (folderLabelStyle != null)
                folderLabelStyle.fontSize = originalFont;
        }

        void HandleFolderClick(BrowserEntry entry, MouseContext mouse)
        {
            if (entry.kind == EntryKind.Parent)
                SetCurrentFolder(entry.folderPath, true);
            else if (entry.kind == EntryKind.Folder)
                SetCurrentFolder(entry.folderPath, true);

            clickFx.Add(new ClickFx { contentPos = mouse.Content, start = EditorApplication.timeSinceStartup, life = 0.45f });
        }

        void ShowFolderContextMenu(BrowserEntry entry)
        {
            GenericMenu menu = new GenericMenu();
            if (entry.kind == EntryKind.Parent)
            {
                menu.AddItem(new GUIContent("Go Up"), false, () =>
                {
                    SetCurrentFolder(entry.folderPath, true);
                });
            }
            else if (entry.kind == EntryKind.Folder)
            {
                menu.AddItem(new GUIContent("Open"), false, () =>
                {
                    SetCurrentFolder(entry.folderPath, true);
                });
                menu.AddItem(new GUIContent("Reveal in Explorer"), false, () =>
                {
                    string path = string.IsNullOrEmpty(entry.folderPath) ? PresetRoot : entry.folderPath;
                    EditorUtility.RevealInFinder(path);
                });
            }
            menu.ShowAsContext();
        }

        string FitTextToWidth(GUIStyle style, string text, float maxWidth)
        {
            if (style == null) return text ?? string.Empty;
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (maxWidth <= 0f) return string.Empty;

            TempContent.text = text;
            if (style.CalcSize(TempContent).x <= maxWidth) return text;

            const string ellipsis = "...";
            int len = text.Length;
            while (len > 0)
            {
                string candidate = text.Substring(0, len) + ellipsis;
                TempContent.text = candidate;
                if (style.CalcSize(TempContent).x <= maxWidth) return candidate;
                len--;
            }
            return ellipsis;
        }

        int FitFontSizeToWidth(GUIStyle baseStyle, string text, float width, int maxSize, int minSize)
        {
            int size = maxSize;
            Vector2 m;
            while (size > minSize)
            {
                baseStyle.fontSize = size;
                m = baseStyle.CalcSize(new GUIContent(text));
                if (m.x <= width) break;
                size--;
            }
            return size;
        }

        void DrawOverlayTitle(Rect previewRect, string title, bool useSmallerFont)
        {
            float pad = useSmallerFont ? 5f : 6f;
            float height = useSmallerFont ? 19f : 20f;
            float bgExtra = useSmallerFont ? 7f : 6f;
            Rect textRect = new Rect(previewRect.x + pad, previewRect.yMax - height - 4f, previewRect.width - pad * 2f, height);
            GUI.DrawTexture(new Rect(textRect.x - 4f, textRect.y - 2f, textRect.width + 8f, textRect.height + bgExtra), overlayBgTex, ScaleMode.StretchToFill, true);
            float scale = useSmallerFont ? 0.72f : 1f;
            int maxSize = Mathf.Max(1, Mathf.RoundToInt(12f * scale));
            int minSize = Mathf.Max(1, Mathf.RoundToInt(8f * scale));
            if (minSize > maxSize) minSize = maxSize;
            int target = FitFontSizeToWidth(overlayTitleStyle, title, textRect.width, maxSize, minSize);
            overlayTitleStyle.fontSize = target;
            string display = FitTextToWidth(overlayTitleStyle, title, textRect.width - 8f);
            TempContent.text = display;
            TempContent.tooltip = title;
            GUI.Label(textRect, TempContent, overlayTitleStyle);
            TempContent.tooltip = null;
        }

        Rect DrawCardContentsLocal(Rect r, VoluSmokePreset p, bool isStarting, bool isHover, Anim a, CardMode mode, float previewEdge, float timeNow, Rect scaledCard)
        {
            float parallaxX = 0f, parallaxY = 0f;
            if (isHover && a != null)
            {
                parallaxX = -a.tilt * (ParallaxMaxPx / TiltMaxDeg);
                parallaxX = Mathf.Clamp(parallaxX, -ParallaxMaxPx, ParallaxMaxPx);
                parallaxY = -ParallaxMaxPx * 0.3f * a.hover;
                float parallaxScale = Mathf.Lerp(0.45f, 1f, GetCardVisualScale(scaledCard));
                parallaxX *= parallaxScale;
                parallaxY *= parallaxScale;
            }

            Rect previewR;
            if (mode == CardMode.Full)
            {
                float availH = r.height - 34f;
                float imgH = Mathf.Min(previewEdge, availH);
                previewR = new Rect(r.x, r.y, r.width, imgH);
            }
            else
            {
                float side = Mathf.Min(previewEdge, Mathf.Min(r.width, r.height));
                float px = r.x + (r.width - side) * 0.5f;
                float py = r.y + (r.height - side) * 0.5f;
                previewR = new Rect(px, py, side, side);
            }

            previewR.position += new Vector2(parallaxX, parallaxY);

            GUI.color = Color.white;
            if (p.previewImage != null) GUI.DrawTexture(previewR, p.previewImage, ScaleMode.ScaleToFit);
            else { EditorGUI.DrawRect(previewR, new Color(0.12f, 0.12f, 0.12f)); GUI.Label(previewR, "No Preview", EditorStyles.centeredGreyMiniLabel); }

            string title = p.presetName;
            if (isStarting) title = "[Default] " + title;
            else if (IsFavorite(p)) title = "* " + title;

            if (mode == CardMode.Full)
            {
                DrawOverlayTitle(previewR, title, false);

                float y = previewR.yMax + 4f;

                if (!string.IsNullOrEmpty(p.description) && p.description != "No description")
                {
                    int descSize = FitFontSizeToWidth(descStyle, p.description, r.width, 11, 9);
                    descStyle.fontSize = descSize;
                    GUI.Label(new Rect(r.x, y, r.width, 32f), p.description, descStyle);
                    y += 28f;
                }

                string facts = "L: " + p.stackLayers + "  |  R: " + p.gridResolution + "  |  S: " + p.planeSize.ToString("F1") + "m";
                int infoSize = FitFontSizeToWidth(infoStyle, facts, r.width, 10, 8);
                infoStyle.fontSize = infoSize;
                GUI.Label(new Rect(r.x, y, r.width, 14f), facts, infoStyle);
            }
            else if (mode == CardMode.Compact)
            {
                DrawOverlayTitle(previewR, title, false);
            }
            else if (mode != CardMode.Ultra)
            {
                DrawOverlayTitle(previewR, title, true);
            }

            return previewR;
        }

        static void DrawFrameLocal(Rect r, float t, Texture2D tex)
        {
            if (!tex) return;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), tex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), tex);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), tex);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), tex);
        }

        void DrawClickFx(float now)
        {
            if (clickFx.Count == 0) return;

            Rect full = new Rect(0, 0, position.width, position.height);
            GUI.BeginGroup(full);

            for (int i = clickFx.Count - 1; i >= 0; i--)
            {
                var fx = clickFx[i];

                float age = (float)(now - fx.start);
                if (age > fx.life) { clickFx.RemoveAt(i); continue; }


                Vector2 guiPos = _lastCanvasRect.position + (fx.contentPos - _lastScroll);

                float u = Mathf.Clamp01(age / fx.life);
                Color old = GUI.color;

                GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.35f, 0f, u));
                Rect core = new Rect(guiPos.x - 8f, guiPos.y - 8f, 16f, 16f);
                GUI.DrawTexture(core, radialLight, ScaleMode.StretchToFill, true);

                float ringRSmall = Mathf.Lerp(10f, 64f, u);
                GUI.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0.9f, 0f, u) * 0.9f);
                Rect ringSmall = new Rect(guiPos.x - ringRSmall, guiPos.y - ringRSmall, ringRSmall * 2f, ringRSmall * 2f);
                GUI.DrawTexture(ringSmall, radialLight, ScaleMode.StretchToFill, true);

                float ringRBig = Mathf.Lerp(40f, 180f, u);
                float bigAlpha = Mathf.SmoothStep(0.35f, 0f, u);
                GUI.color = new Color(1f, 1f, 1f, bigAlpha * 0.65f);
                Rect ringBig = new Rect(guiPos.x - ringRBig, guiPos.y - ringRBig, ringRBig * 2f, ringRBig * 2f);
                GUI.DrawTexture(ringBig, radialLight, ScaleMode.StretchToFill, true);

                float ringRHuge = Mathf.Lerp(80f, 280f, u);
                float hugeAlpha = Mathf.SmoothStep(0.18f, 0f, u);
                GUI.color = new Color(1f, 1f, 1f, hugeAlpha * 0.35f);
                Rect ringHuge = new Rect(guiPos.x - ringRHuge, guiPos.y - ringRHuge, ringRHuge * 2f, ringRHuge * 2f);
                GUI.DrawTexture(ringHuge, radialLight, ScaleMode.StretchToFill, true);

                GUI.color = old;
            }

            GUI.EndGroup();
            _forceRepaint = true;
        }

        void DrawToasts(float now)
        {
            if (toasts.Count == 0) return;

            if (toastTextStyle == null)
                toastTextStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };

            Rect full = new Rect(0, 0, position.width, position.height);
            GUI.BeginGroup(full);

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];

                float age = (float)(now - t.start);
                if (age > t.life) { toasts.RemoveAt(i); continue; }


                Vector2 guiPos = _lastCanvasRect.position + (t.contentPos - _lastScroll);

                float u = Mathf.Clamp01(age / t.life);
                float yOff = 0f;

                float shock = Mathf.Exp(-8f * u) * Mathf.Sin(24f * u);
                float scale = 1f + 0.12f * shock;

                float alpha = (u > 0.65f) ? Mathf.Lerp(1f, 0f, (u - 0.65f) / 0.35f) : 1f;

                float ringR = Mathf.Lerp(10f, 36f, u);
                float ringA = Mathf.Lerp(0.35f, 0f, u) * alpha;
                Color old = GUI.color;

                GUI.color = new Color(1f, 1f, 1f, ringA);
                Rect ring = new Rect(guiPos.x - ringR, guiPos.y + yOff - ringR, ringR * 2f, ringR * 2f);
                GUI.DrawTexture(ring, radialLight, ScaleMode.StretchToFill, true);

                float bigR = Mathf.Lerp(40f, 180f, u);
                float bigA = Mathf.Lerp(0.25f, 0f, u) * alpha;
                GUI.color = new Color(1f, 1f, 1f, bigA);
                Rect big = new Rect(guiPos.x - bigR, guiPos.y + yOff - bigR, bigR * 2f, bigR * 2f);
                GUI.DrawTexture(big, radialLight, ScaleMode.StretchToFill, true);

                string msg = t.text;
                Vector2 size = toastTextStyle.CalcSize(new GUIContent(msg));
                Rect tr = new Rect(guiPos.x - size.x * 0.5f, guiPos.y + yOff - size.y * 0.5f, size.x, size.y);

                GUI.color = new Color(0, 0, 0, 0.6f * alpha);
                GUI.matrix = Matrix4x4.TRS(new Vector3(1, 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.Label(tr, msg, toastTextStyle);

                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
                GUI.color = new Color(1, 1, 1, 0.95f * alpha);
                GUI.Label(tr, msg, toastTextStyle);

                GUI.matrix = Matrix4x4.identity;
                GUI.color = old;
            }

            GUI.EndGroup();
            _forceRepaint = true;
        }

        void ShowCardContextMenu(VoluSmokePreset p)
        {
            GenericMenu m = new GenericMenu();
            m.AddItem(new GUIContent("Apply to Selected"), false, () => ApplyPreset(p, GetCurrentMouseContent()));


            m.AddSeparator("");
            var folders = GetAllPresetFolders();
            foreach (var f in folders)
            {
                string fPath = NormalizeAssetPath(f);
                string display = string.Equals(fPath, PresetRoot, StringComparison.OrdinalIgnoreCase) ? "All Presets" : GetFolderLabel(fPath);
                m.AddItem(new GUIContent("Move to Folder/" + display), false, () => MovePresetToFolder(p, fPath));
            }
            m.AddItem(new GUIContent("Move to Folder/New Folder..."), false, () =>
            {
                TextInputPopup.Show("New Folder", "Folder name:", "NewPresetFolder", (folderName) =>
                {
                    folderName = SanitizeFolderName(folderName);
                    if (string.IsNullOrEmpty(folderName)) return;
                    string parent = string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath;
                    string newPath = NormalizeAssetPath(Path.Combine(parent, folderName));
                    EnsureFolder(newPath);
                    AssetDatabase.Refresh();
                    MovePresetToFolder(p, newPath);
                });
            });


            bool fav = IsFavorite(p);
            m.AddSeparator("");
            m.AddItem(new GUIContent(fav ? "Unmark Favorite" : "Mark as Favorite"), false, () => {
                ToggleFavorite(p);
                if (showFavoritesOnly && !IsFavorite(p)) _forceRepaint = true;
            });


            m.AddSeparator("");
            m.AddItem(new GUIContent("Ping in Project"), false, () => EditorGUIUtility.PingObject(p));
            m.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(p)));
            m.AddSeparator("");
            m.AddItem(new GUIContent("Duplicate"), false, () => DuplicatePreset(p));
            m.AddItem(new GUIContent("Delete"), false, () => DeletePreset(p));
            m.ShowAsContext();
        }

        void ShowMenu(VoluSmokePreset p) => ShowCardContextMenu(p);


        Vector2 GetCurrentMouseContent()
        {

            Vector2 mouseWindow = (Event.current != null) ? Event.current.mousePosition : Vector2.zero;
            Vector2 mouseView = mouseWindow - _lastCanvasRect.position;
            mouseView.y -= FixedYOffset;
            return mouseView + _lastScroll;
        }


        void ApplyPreset(VoluSmokePreset p, Vector2 clickContentPos)
        {
            var go = Selection.activeGameObject;
            if (go != null) targetGenerator = go.GetComponent<VoluSmokeMeshGenerator>();

            if (targetGenerator == null)
            {
                EditorUtility.DisplayDialog("No Target", "Select a GameObject with VoluSmokeMeshGenerator (the browser picks it automatically).", "OK");
                return;
            }

            Undo.RecordObject(targetGenerator, "Apply VoluSmoke Preset");
            Undo.RecordObject(targetGenerator.transform, "Apply VoluSmoke Preset");
            p.ApplyToGenerator(targetGenerator);
            targetGenerator.GenerateMesh();

            MeshRenderer mr = targetGenerator.GetComponent<MeshRenderer>();
            if (mr != null) { mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; }

            EditorUtility.SetDirty(targetGenerator);


            toasts.Add(new Toast { contentPos = clickContentPos, start = EditorApplication.timeSinceStartup, life = 1.6f, text = "Preset applied!" });
            clickFx.Add(new ClickFx { contentPos = clickContentPos, start = EditorApplication.timeSinceStartup, life = 0.45f });

            _forceRepaint = true;
        }

        void MovePresetToFolder(VoluSmokePreset p, string folderPath)
        {
            if (p == null) return;

            string targetFolder = string.IsNullOrEmpty(folderPath) ? PresetRoot : NormalizeAssetPath(folderPath);
            EnsureFolder(targetFolder);

            string srcPath = AssetDatabase.GetAssetPath(p);
            if (string.IsNullOrEmpty(srcPath)) return;

            string fileName = Path.GetFileName(srcPath);
            string dstPath = Path.Combine(targetFolder, fileName).Replace("\\", "/");
            dstPath = AssetDatabase.GenerateUniqueAssetPath(dstPath);

            string result = AssetDatabase.MoveAsset(srcPath, dstPath);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError("Move failed: " + result);
                return;
            }

            AssetDatabase.Refresh();
            RefreshList();

            string newFolder = NormalizeAssetPath(Path.GetDirectoryName(dstPath));
            if (!string.Equals(currentFolderPath, newFolder, StringComparison.OrdinalIgnoreCase))
                SetCurrentFolder(newFolder, false);
        }

        void DuplicatePreset(VoluSmokePreset p)
        {
            VoluSmokePreset.EnsurePresetFolder();
            string src = AssetDatabase.GetAssetPath(p);
            string file = Path.GetFileName(src);


            string targetFolder = string.IsNullOrEmpty(currentFolderPath) ? PresetRoot : currentFolderPath;
            EnsureFolder(targetFolder);
            string dst = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, file).Replace("\\", "/"));

            AssetDatabase.CopyAsset(src, dst);
            AssetDatabase.Refresh();

            VoluSmokePreset copy = AssetDatabase.LoadAssetAtPath<VoluSmokePreset>(dst);
            if (copy != null)
            {
                copy.presetName = p.presetName + " Copy";
                EditorUtility.SetDirty(copy);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(copy);
            }
            RefreshList();
        }

        void DeletePreset(VoluSmokePreset p)
        {
            if (!EditorUtility.DisplayDialog("Delete Preset", "Delete '" + p.presetName + "'?", "Delete", "Cancel")) return;
            string path = AssetDatabase.GetAssetPath(p);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            RefreshList();
        }

        static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), "");
            name = name.Trim();
            return name;
        }
    }


    public class TextInputPopup : EditorWindow
    {
        private string _title;
        private string _label;
        private string _text;
        private Action<string> _onOk;

        public static void Show(string title, string label, string defaultText, Action<string> onOk)
        {
            var w = CreateInstance<TextInputPopup>();
            w._title = title;
            w._label = label;
            w._text = defaultText ?? "";
            w._onOk = onOk;
            w.titleContent = new GUIContent(title);
            var center = new Rect(0, 0, 360, 110);
            center.x = (Screen.currentResolution.width - center.width) / 2f;
            center.y = (Screen.currentResolution.height - center.height) / 2f;
            w.position = center;
            w.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(_label, EditorStyles.label);
            GUI.SetNextControlName("TextInputPopupField");
            _text = EditorGUILayout.TextField(_text);
            EditorGUI.FocusTextInControl("TextInputPopupField");

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                {
                    Close();
                }
                if (GUILayout.Button("OK", GUILayout.Width(90)))
                {
                    try { _onOk?.Invoke(_text); }
                    finally { Close(); }
                }
            }


            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    try { _onOk?.Invoke(_text); }
                    finally { Close(); }
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                    e.Use();
                }
            }
        }
    }





}
