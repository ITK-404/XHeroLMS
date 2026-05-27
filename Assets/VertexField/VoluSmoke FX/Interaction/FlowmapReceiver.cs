using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace VertexField.VoluSmokeFX
{
    [DefaultExecutionOrder(50)]
    public class FlowmapReceiver : MonoBehaviour
    {
        public enum PlaneSpace { Auto, LocalXZ, LocalXY, LocalYZ }
        public enum TickMode { Update, LateUpdate, FixedUpdate }

        [Header("Receivers")]
        public Transform player;
        [Tooltip("Additional transforms (enemies, projectiles, weapons) that should write into the flowmap.")]
        public List<Transform> additionalReceivers = new List<Transform>();
        public Renderer targetRenderer;
        public string flowTextureProperty = "_FlowTex";
        public int resolution = 1024;

        public PlaneSpace plane = PlaneSpace.Auto;
        public Vector2 padding = Vector2.zero;

        [FormerlySerializedAs("brushRadiusFrac")]
        [Range(0.001f, 0.5f)] public float flowRadiusFrac = 0.08f;
        [Range(0.001f, 0.5f)] public float alphaRadiusFrac = 0.08f;
        [Range(0f, 1f)] public float brushHardness = 0.25f;
        [Range(1f, 6f)] public float stretchAlongMotion = 2f;
        [Range(0f, 1f)] public float flowWriteStrength = 0.7f;
        [Range(0f, 1f)] public float alphaWriteScale = 1f;

        public float maxPaintDistanceToPlane = 1f;
        public float minSpeedToPaint = 0.05f;
        [Range(0f, 1f)] public float velocitySmoothing = 0.2f;

        [Header("Restore timing")]
        [Min(0.05f)] public float restoreSeconds = 1f;
        [Range(0f, 0.05f)] public float snapAlphaThreshold = 0.01f;

        [Header("Stability")]
        [Range(0.25f, 1f)] public float maxPerFrameStepScale = 1f;

        [Header("Blur (Kawase)")]
        public bool blurAlpha = true;
        public bool blurFlow = true;
        [Range(0, 6)] public int blurIterations = 3;
        [Range(0.5f, 3f)] public float blurBaseOffset = 1.5f;
        [Range(1f, 2f)] public float blurOffsetGrowth = 1.35f;
        public bool orientedBlur = false;

        public TickMode tick = TickMode.LateUpdate;
        public bool useUnscaledTime = false;

        Material mat;
        RenderTexture rtA, rtB;
        bool useA = true;

        static readonly int _BrushUV = Shader.PropertyToID("_BrushUV");
        static readonly int _FlowDir = Shader.PropertyToID("_FlowDir");
        static readonly int _Hardness = Shader.PropertyToID("_Hardness");
        static readonly int _AlphaScale = Shader.PropertyToID("_AlphaScale");
        static readonly int _Stretch = Shader.PropertyToID("_Stretch");
        static readonly int _AlphaBrushRadius = Shader.PropertyToID("_AlphaBrushRadius");

        static readonly int _StepRG = Shader.PropertyToID("_StepRG");
        static readonly int _StepA = Shader.PropertyToID("_StepA");
        static readonly int _SnapAlphaThresh = Shader.PropertyToID("_SnapAlphaThresh");

        static readonly int _OffsetPx = Shader.PropertyToID("_OffsetPx");
        static readonly int _BlurA = Shader.PropertyToID("_BlurA");
        static readonly int _BlurRG = Shader.PropertyToID("_BlurRG");
        static readonly int _PreserveNeutralRG = Shader.PropertyToID("_PreserveNeutralRG");
        static readonly int _Oriented = Shader.PropertyToID("_Oriented");
        static readonly int _FlipSign = Shader.PropertyToID("_FlipSign");
        const float kBoundsChangeThreshold = 1e-4f;
        static readonly float kBoundsChangeThresholdSqr = kBoundsChangeThreshold * kBoundsChangeThreshold;
        static readonly Vector3[] kBoundsCornerSigns = new Vector3[]
        {
            new Vector3(-1f, -1f, -1f),
            new Vector3(-1f, -1f, +1f),
            new Vector3(-1f, +1f, -1f),
            new Vector3(-1f, +1f, +1f),
            new Vector3(+1f, -1f, -1f),
            new Vector3(+1f, -1f, +1f),
            new Vector3(+1f, +1f, -1f),
            new Vector3(+1f, +1f, +1f)
        };

        static readonly List<Renderer> s_Renderers = new List<Renderer>(64);
        readonly List<Renderer> cachedRenderers = new List<Renderer>(32);
        readonly Dictionary<Renderer, BoundsSnapshot> rendererBoundsCache = new Dictionary<Renderer, BoundsSnapshot>(32);
        static readonly List<Renderer> s_RenderersToRemove = new List<Renderer>(32);
        float flowUvMinU = 0f, flowUvMaxU = 1f;
        float flowUvMinV = 0f, flowUvMaxV = 1f;
        MaterialPropertyBlock mpb;
        readonly List<Vector3> boundsWorldPoints = new List<Vector3>(128);
        static readonly List<Transform> s_ActiveSources = new List<Transform>(32);
        static readonly List<Transform> s_SourcesToRemove = new List<Transform>(32);
        static readonly List<PaintRequest> s_PaintRequests = new List<PaintRequest>(32);
        readonly Dictionary<Transform, SourceState> sourceStates = new Dictionary<Transform, SourceState>(32);
        bool hasBounds;
        bool boundsDirty = true;
        int blurParity;
        bool isOpenGLLike; // OpenGLCore / OpenGLES3 (WebGL2)

        CommandBuffer cmd;
        RenderTextureFormat activeRtFormat = RenderTextureFormat.ARGB32;
        float restoreQuantizationStep;
        float restoreStepBudgetRG;
        float restoreStepBudgetAlpha;

        void Awake()
        {
            CacheGraphicsBackend();
        }

        void Start()
        {
            CacheGraphicsBackend();

            if (isOpenGLLike)
                Debug.LogWarning("[Flowmap] OpenGL/WebGL path enabled");

            cmd = new CommandBuffer { name = "FlowmapPaint" };
        }

        void OnEnable()
        {
            if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();

            var sh = Shader.Find("Hidden/Flowmap_AllInOne");
            if (!sh)
            {
                enabled = false;
                Debug.LogError("[Flowmap] Missing shader 'Hidden/Flowmap_AllInOne'");
                return;
            }

            mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            CreateRTs();
            ClearRT(rtA);
            ClearRT(rtB);
            RefitBounds();
            boundsDirty = false;

            SeedSourceStates();
            ApplyRT(Cur);
        }

        void OnDisable()
        {
            ReleaseRTs();
            if (mat) DestroyImmediate(mat);

            if (cmd != null)
            {
                cmd.Dispose(); // dispose ONCE
                cmd = null;
            }

            rendererBoundsCache.Clear();
            cachedRenderers.Clear();
            boundsWorldPoints.Clear();
            sourceStates.Clear();
            s_ActiveSources.Clear();
            s_SourcesToRemove.Clear();
            s_PaintRequests.Clear();
            hasBounds = false;
            flowUvMinU = 0f;
            flowUvMaxU = 1f;
            flowUvMinV = 0f;
            flowUvMaxV = 1f;
        }

        // keep empty; no dispose here (prevents double-dispose)
        void OnDestroy() { }

        void Update()
        {
            if (tick == TickMode.Update) Step(Time.deltaTime, Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            if (tick == TickMode.LateUpdate) Step(Time.deltaTime, Time.unscaledDeltaTime);
        }

        void FixedUpdate()
        {
            if (tick == TickMode.FixedUpdate) Step(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        }

        void Step(float dt, float udt)
        {
            if (!rtA || !rtB || mat == null) return;
            DetectBoundsChanges();
            EnsureBounds();
            if (!hasBounds) return;

            float d = useUnscaledTime ? udt : dt;
            if (d <= 0f) return;

            GetPlaneFrame(out Vector3 o, out Vector3 ux, out Vector3 vx, out Vector3 nx, out Vector2 sz);
            UpdateActiveSources();
            BuildPaintRequests(d, o, ux, vx, nx, sz);

            float flowRadius = Mathf.Max(1e-4f, Mathf.Clamp01(flowRadiusFrac) * 0.5f);
            float alphaRadius = Mathf.Max(1e-4f, Mathf.Clamp01(alphaRadiusFrac) * 0.5f);
            float flowStrength = Mathf.Clamp01(flowWriteStrength);

            float sec = Mathf.Max(0.05f, restoreSeconds);
            float stepScale = Mathf.Clamp01(maxPerFrameStepScale);
            float baseStep = (sec > 0f) ? (d / sec) : 0f;
            float requestedRestore = Mathf.Max(0f, baseStep * stepScale);
            float flowRestoreStep = ConsumeRestoreBudget(ref restoreStepBudgetRG, requestedRestore, restoreQuantizationStep);
            float alphaRestoreStep = ConsumeRestoreBudget(ref restoreStepBudgetAlpha, requestedRestore, restoreQuantizationStep);
            bool needsDecay = flowRestoreStep > 0f || alphaRestoreStep > 0f;

            var src = Cur;
            var dst = Alt;

            if (isOpenGLLike && cmd != null)
            {
                cmd.Clear();

                if (needsDecay)
                {
                    mat.SetFloat(_StepRG, flowRestoreStep);
                    mat.SetFloat(_StepA, alphaRestoreStep);
                    mat.SetFloat(_SnapAlphaThresh, snapAlphaThreshold);

                    cmd.SetRenderTarget(dst);
                    cmd.Blit(src, BuiltinRenderTextureType.CurrentActive, mat, 1);
                    Graphics.ExecuteCommandBuffer(cmd);
                    Swap();
                    src = Cur; dst = Alt;
                    cmd.Clear();
                }

                if (s_PaintRequests.Count > 0)
                {
                    for (int i = 0; i < s_PaintRequests.Count; i++)
                    {
                        var req = s_PaintRequests[i];
                        mat.SetVector(_BrushUV, new Vector4(req.brushU, req.brushV, flowRadius, flowStrength));
                        mat.SetFloat(_AlphaBrushRadius, alphaRadius);
                        mat.SetVector(_FlowDir, new Vector4(req.dir.x, req.dir.y, 0f, 0f));
                        mat.SetFloat(_Hardness, brushHardness);
                        mat.SetFloat(_AlphaScale, alphaWriteScale);
                        mat.SetFloat(_Stretch, Mathf.Max(1f, stretchAlongMotion));

                        cmd.SetRenderTarget(dst);
                        cmd.Blit(src, BuiltinRenderTextureType.CurrentActive, mat, 0);
                        Graphics.ExecuteCommandBuffer(cmd);
                        Swap();
                        src = Cur; dst = Alt;
                        cmd.Clear();
                    }
                }

                if (blurIterations > 0 && (blurAlpha || blurFlow))
                {
                    float off = blurBaseOffset;
                    blurParity ^= 1;

                    mat.SetFloat(_FlipSign, blurParity == 1 ? -1f : 1f);
                    mat.SetFloat(_BlurA, blurAlpha ? 1f : 0f);
                    mat.SetFloat(_BlurRG, blurFlow ? 1f : 0f);
                    mat.SetFloat(_PreserveNeutralRG, 1f);
                    mat.SetFloat(_Oriented, orientedBlur ? 1f : 0f);

                    for (int i = 0; i < blurIterations; i++)
                    {
                        mat.SetFloat(_OffsetPx, off);
                        cmd.SetRenderTarget(dst);
                        cmd.Blit(src, BuiltinRenderTextureType.CurrentActive, mat, 2);
                        Graphics.ExecuteCommandBuffer(cmd);
                        Swap();
                        src = Cur; dst = Alt;
                        off *= blurOffsetGrowth;
                        cmd.Clear();
                    }
                }
            }
            else
            {
                // Non-OpenGL path
                if (needsDecay)
                {
                    mat.SetFloat(_StepRG, flowRestoreStep);
                    mat.SetFloat(_StepA, alphaRestoreStep);
                    mat.SetFloat(_SnapAlphaThresh, snapAlphaThreshold);

                    Graphics.Blit(src, dst, mat, 1);
                    Swap();
                    src = Cur; dst = Alt;
                }

                if (s_PaintRequests.Count > 0)
                {
                    for (int i = 0; i < s_PaintRequests.Count; i++)
                    {
                        var req = s_PaintRequests[i];
                        mat.SetVector(_BrushUV, new Vector4(req.brushU, req.brushV, flowRadius, flowStrength));
                        mat.SetFloat(_AlphaBrushRadius, alphaRadius);
                        mat.SetVector(_FlowDir, new Vector4(req.dir.x, req.dir.y, 0f, 0f));
                        mat.SetFloat(_Hardness, brushHardness);
                        mat.SetFloat(_AlphaScale, alphaWriteScale);
                        mat.SetFloat(_Stretch, Mathf.Max(1f, stretchAlongMotion));

                        Graphics.Blit(src, dst, mat, 0);
                        Swap();
                        src = Cur; dst = Alt;
                    }
                }

                if (blurIterations > 0 && (blurAlpha || blurFlow))
                {
                    float off = blurBaseOffset;
                    blurParity ^= 1;

                    mat.SetFloat(_FlipSign, blurParity == 1 ? -1f : 1f);
                    mat.SetFloat(_BlurA, blurAlpha ? 1f : 0f);
                    mat.SetFloat(_BlurRG, blurFlow ? 1f : 0f);
                    mat.SetFloat(_PreserveNeutralRG, 1f);
                    mat.SetFloat(_Oriented, orientedBlur ? 1f : 0f);

                    for (int i = 0; i < blurIterations; i++)
                    {
                        mat.SetFloat(_OffsetPx, off);
                        Graphics.Blit(src, dst, mat, 2);
                        Swap();
                        src = Cur; dst = Alt;
                        off *= blurOffsetGrowth;
                    }
                }
            }

            ApplyRT(Cur);
        }

        void UpdateActiveSources()
        {
            s_ActiveSources.Clear();
            if (player) s_ActiveSources.Add(player);

            if (additionalReceivers != null)
            {
                for (int i = 0; i < additionalReceivers.Count; i++)
                {
                    var t = additionalReceivers[i];
                    if (t && !s_ActiveSources.Contains(t))
                        s_ActiveSources.Add(t);
                }
            }

            s_SourcesToRemove.Clear();
            foreach (var kvp in sourceStates)
            {
                if (!kvp.Key || !s_ActiveSources.Contains(kvp.Key))
                    s_SourcesToRemove.Add(kvp.Key);
            }

            for (int i = 0; i < s_SourcesToRemove.Count; i++)
                sourceStates.Remove(s_SourcesToRemove[i]);

            s_SourcesToRemove.Clear();
        }

        void BuildPaintRequests(float d, Vector3 origin, Vector3 ux, Vector3 vx, Vector3 nx, Vector2 size)
        {
            s_PaintRequests.Clear();
            if (s_ActiveSources.Count == 0) return;

            float smoothing = 1f - Mathf.Pow(1f - velocitySmoothing, d * 60f);
            float invWidth = 1f / Mathf.Max(1e-5f, size.x);
            float invHeight = 1f / Mathf.Max(1e-5f, size.y);

            for (int i = 0; i < s_ActiveSources.Count; i++)
            {
                Transform t = s_ActiveSources[i];
                if (!t) continue;

                Vector3 pos = t.position;
                SourceState state = GetOrCreateSourceState(t, pos);

                Vector3 wv = (pos - state.prevPos) / Mathf.Max(d, 1e-5f);
                Vector2 planarVel = new Vector2(Vector3.Dot(wv, ux), Vector3.Dot(wv, vx));
                state.smoothVel = Vector2.Lerp(state.smoothVel, planarVel, smoothing);
                state.prevPos = pos;
                sourceStates[t] = state;

                Vector3 rel = pos - origin;
                float u = Vector3.Dot(rel, ux) * invWidth + 0.5f;
                float v = Vector3.Dot(rel, vx) * invHeight + 0.5f;
                float dist = Mathf.Abs(Vector3.Dot(rel, nx));
                float speed = state.smoothVel.magnitude;

                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                if (dist > maxPaintDistanceToPlane || speed < minSpeedToPaint) continue;

                Vector2 dir = (state.smoothVel.sqrMagnitude > 1e-8f) ? state.smoothVel.normalized : new Vector2(1f, 0f);
                float brushU = Mathf.Lerp(flowUvMinU, flowUvMaxU, Mathf.Clamp01(u));
                float brushV = Mathf.Lerp(flowUvMinV, flowUvMaxV, Mathf.Clamp01(v));

                s_PaintRequests.Add(new PaintRequest
                {
                    dir = dir,
                    brushU = brushU,
                    brushV = brushV
                });
            }
        }

        SourceState GetOrCreateSourceState(Transform t, Vector3 position)
        {
            if (sourceStates.TryGetValue(t, out SourceState state))
                return state;

            state.prevPos = position;
            state.smoothVel = Vector2.zero;
            sourceStates[t] = state;
            return state;
        }

        void SeedSourceStates()
        {
            UpdateActiveSources();
            for (int i = 0; i < s_ActiveSources.Count; i++)
            {
                Transform t = s_ActiveSources[i];
                if (!t) continue;
                sourceStates[t] = new SourceState { prevPos = t.position, smoothVel = Vector2.zero };
            }
            s_PaintRequests.Clear();
        }

        void EnsureRes()
        {
            resolution = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution, 16, 4096));
        }

        void CreateRTs()
        {
            EnsureRes();
            ReleaseRTs();

            var desiredFormat = PickRenderTextureFormat();
            rtA = CreateRT("A", desiredFormat);
            activeRtFormat = rtA ? rtA.format : desiredFormat;
            rtB = CreateRT("B", activeRtFormat);
            restoreQuantizationStep = DetermineRestoreQuantizationStep(activeRtFormat);
            ResetRestoreBudgets();
            useA = true;
        }

        RenderTexture CreateRT(string suffix, RenderTextureFormat format)
        {
            RenderTextureFormat finalFormat = format;
            if (!SystemInfo.SupportsRenderTextureFormat(finalFormat))
                finalFormat = RenderTextureFormat.Default;

            var desc = new RenderTextureDescriptor(resolution, resolution, finalFormat, 24)
            {
                sRGB = false,              // linear math
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false, // no compute on WebGL
                vrUsage = VRTextureUsage.None,
                msaaSamples = 1            // MSAA RTs not supported on WebGL the same way
            };

            var rt = new RenderTexture(desc)
            {
                name = $"{name}_{suffix}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };

            if (!rt.Create())
            {
                Debug.LogError($"[Flowmap] Failed to create RenderTexture {rt.name}");
                DestroyImmediate(rt);
                return null;
            }

            return rt;
        }

        void ReleaseRTs()
        {
            if (rtA) { rtA.Release(); DestroyImmediate(rtA); }
            if (rtB) { rtB.Release(); DestroyImmediate(rtB); }
            rtA = rtB = null;
            ResetRestoreBudgets();
        }

        void ClearRT(RenderTexture rt)
        {
            if (!rt) return;

            // Clear BOTH color and depth (we allocated depth=24)
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.5f, 0.5f, 0f, 0f));
            RenderTexture.active = prev;
        }

        RenderTexture Cur => useA ? rtA : rtB;
        RenderTexture Alt => useA ? rtB : rtA;
        void Swap() { useA = !useA; }

        void ApplyRT(RenderTexture rt)
        {
            if (!targetRenderer || !rt) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetTexture(flowTextureProperty, rt);
            targetRenderer.SetPropertyBlock(mpb);
        }

        void OnValidate()
        {
            EnsureRes();
            if (Application.isPlaying && rtA && (rtA.width != resolution || rtA.height != resolution))
            {
                CreateRTs();
            }
            if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
            boundsDirty = true;
        }

        void EnsureBounds()
        {
            if (boundsDirty || !hasBounds)
            {
                RefitBounds();
                boundsDirty = false;
            }
        }

        void DetectBoundsChanges()
        {
            if (transform.hasChanged)
            {
                boundsDirty = true;
                transform.hasChanged = false;
            }

            for (int i = cachedRenderers.Count - 1; i >= 0; i--)
            {
                var r = cachedRenderers[i];
                if (!r)
                {
                    if (!ReferenceEquals(r, null))
                        rendererBoundsCache.Remove(r);
                    cachedRenderers.RemoveAt(i);
                    boundsDirty = true;
                    continue;
                }

                var rt = r.transform;
                bool rendererDirty = false;

                if (rt.hasChanged)
                {
                    rendererDirty = true;
                    rt.hasChanged = false;
                }

                BoundsSnapshot snapshot = CaptureBoundsSnapshot(r);
                if (rendererBoundsCache.TryGetValue(r, out BoundsSnapshot previous))
                {
                    if (BoundsSnapshotChanged(previous, snapshot))
                        rendererDirty = true;
                }
                else
                {
                    rendererDirty = true;
                }

                rendererBoundsCache[r] = snapshot;

                if (rendererDirty)
                    boundsDirty = true;
            }
        }

        void RefitBounds()
        {
            hasBounds = TryCollectRendererBoundsPoints();
            if (!hasBounds)
            {
                boundsWorldPoints.Clear();
                boundsWorldPoints.Add(transform.position);
                hasBounds = true;
            }

            cachedRenderers.Clear();
            for (int i = 0; i < s_Renderers.Count; i++)
            {
                var r = s_Renderers[i];
                if (r && !cachedRenderers.Contains(r))
                    cachedRenderers.Add(r);
            }

            PruneRendererBoundsCache();
            UpdateUvRange();
        }

        void PruneRendererBoundsCache()
        {
            s_RenderersToRemove.Clear();
            foreach (var kvp in rendererBoundsCache)
            {
                if (!cachedRenderers.Contains(kvp.Key))
                    s_RenderersToRemove.Add(kvp.Key);
            }

            for (int i = 0; i < s_RenderersToRemove.Count; i++)
                rendererBoundsCache.Remove(s_RenderersToRemove[i]);

            s_RenderersToRemove.Clear();
        }

        void UpdateUvRange()
        {
            float minU = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float minV = float.PositiveInfinity;
            float maxV = float.NegativeInfinity;

            bool found = TryAccumulateRendererUvRange(targetRenderer, ref minU, ref maxU, ref minV, ref maxV);
            if (!found)
            {
                for (int i = 0; i < cachedRenderers.Count; i++)
                {
                    if (TryAccumulateRendererUvRange(cachedRenderers[i], ref minU, ref maxU, ref minV, ref maxV))
                        found = true;
                }
            }

            if (!found || !IsFinite(minU) || !IsFinite(maxU) || !IsFinite(minV) || !IsFinite(maxV))
            {
                flowUvMinU = 0f;
                flowUvMaxU = 1f;
                flowUvMinV = 0f;
                flowUvMaxV = 1f;
            }
            else
            {
                flowUvMinU = Mathf.Min(minU, maxU);
                flowUvMaxU = Mathf.Max(minU, maxU);
                flowUvMinV = Mathf.Min(minV, maxV);
                flowUvMaxV = Mathf.Max(minV, maxV);

                flowUvMinU = Mathf.Clamp01(flowUvMinU);
                flowUvMaxU = Mathf.Clamp01(flowUvMaxU);
                flowUvMinV = Mathf.Clamp01(flowUvMinV);
                flowUvMaxV = Mathf.Clamp01(flowUvMaxV);

                if (Mathf.Abs(flowUvMaxU - flowUvMinU) < 1e-5f)
                {
                    flowUvMinU = 0f;
                    flowUvMaxU = 1f;
                }

                if (Mathf.Abs(flowUvMaxV - flowUvMinV) < 1e-5f)
                {
                    flowUvMinV = 0f;
                    flowUvMaxV = 1f;
                }
            }
        }

        bool TryCollectRendererBoundsPoints()
        {
            boundsWorldPoints.Clear();
            s_Renderers.Clear();
            GetComponentsInChildren(true, s_Renderers);
            if (targetRenderer && !s_Renderers.Contains(targetRenderer))
                s_Renderers.Add(targetRenderer);

            bool any = false;
            for (int i = 0; i < s_Renderers.Count; i++)
            {
                var r = s_Renderers[i];
                if (!r) continue;
                rendererBoundsCache[r] = CaptureBoundsSnapshot(r);
                any |= AddRendererBoundsPoints(r);
            }

            return any && boundsWorldPoints.Count > 0;
        }

        bool TryAccumulateRendererUvRange(Renderer renderer, ref float minU, ref float maxU, ref float minV, ref float maxV)
        {
            if (!renderer) return false;
            if (!TryGetSharedMesh(renderer, out Mesh mesh)) return false;

            var uvs = mesh.uv;
            if (uvs == null || uvs.Length == 0) return false;

            bool any = false;
            for (int i = 0; i < uvs.Length; i++)
            {
                Vector2 uv = uvs[i];
                if (!IsFiniteVector(uv)) continue;

                if (uv.x < minU) minU = uv.x;
                if (uv.x > maxU) maxU = uv.x;
                if (uv.y < minV) minV = uv.y;
                if (uv.y > maxV) maxV = uv.y;
                any = true;
            }

            return any;
        }

        bool AddRendererBoundsPoints(Renderer r)
        {
            bool added = false;

            var lb = r.localBounds;
            Vector3 center = lb.center;
            Vector3 ext = lb.extents;
            bool validLocal = IsFiniteVector(ext) && (ext.x > 0f || ext.y > 0f || ext.z > 0f);

            if (validLocal)
            {
                Matrix4x4 l2w = r.localToWorldMatrix;
                for (int i = 0; i < kBoundsCornerSigns.Length; i++)
                {
                    Vector3 sign = kBoundsCornerSigns[i];
                    Vector3 localCorner = center + new Vector3(ext.x * sign.x, ext.y * sign.y, ext.z * sign.z);
                    Vector3 worldCorner = l2w.MultiplyPoint3x4(localCorner);
                    if (!IsFiniteVector(worldCorner)) continue;
                    boundsWorldPoints.Add(worldCorner);
                    added = true;
                }
            }
            else
            {
                Bounds wb = r.bounds;
                Vector3 c = wb.center;
                Vector3 e = wb.extents;
                if (!IsFiniteVector(e)) return added;

                for (int i = 0; i < kBoundsCornerSigns.Length; i++)
                {
                    Vector3 sign = kBoundsCornerSigns[i];
                    Vector3 worldCorner = c + new Vector3(e.x * sign.x, e.y * sign.y, e.z * sign.z);
                    if (!IsFiniteVector(worldCorner)) continue;
                    boundsWorldPoints.Add(worldCorner);
                    added = true;
                }
            }

            return added;
        }

        void GetPlaneFrame(out Vector3 o, out Vector3 ux, out Vector3 vx, out Vector3 nx, out Vector2 sz)
        {
            if (!hasBounds || boundsWorldPoints.Count == 0)
            {
                Transform fallback = transform;
                if (!BuildOrthonormalBasis(fallback.right, fallback.forward, out ux, out vx, out nx))
                    BuildOrthonormalBasis(Vector3.right, Vector3.forward, out ux, out vx, out nx);
                o = fallback.position;
                sz = new Vector2(1f, 1f);
                return;
            }

            Transform basis = targetRenderer ? targetRenderer.transform : transform;
            Vector3 basisRight = SafeDirection(basis ? basis.right : transform.right);
            Vector3 basisUp = SafeDirection(basis ? basis.up : transform.up);
            Vector3 basisForward = SafeDirection(basis ? basis.forward : transform.forward);

            PlaneFitData fit;
            switch (plane)
            {
                case PlaneSpace.LocalXY:
                    fit = ComputePlaneFit(basisRight, basisUp);
                    break;
                case PlaneSpace.LocalYZ:
                    fit = ComputePlaneFit(basisUp, basisForward);
                    break;
                case PlaneSpace.LocalXZ:
                    fit = ComputePlaneFit(basisRight, basisForward);
                    break;
                default:
                    fit = ChooseBestFit(basisRight, basisUp, basisForward);
                    break;
            }

            if (!fit.valid)
            {
                fit = ComputePlaneFit(transform.right, transform.forward);
                if (!fit.valid)
                {
                    fit = ComputePlaneFit(Vector3.right, Vector3.forward);
                }
            }

            if (!fit.valid)
            {
                Transform fallback = transform;
                if (!BuildOrthonormalBasis(fallback.right, fallback.forward, out ux, out vx, out nx))
                    BuildOrthonormalBasis(Vector3.right, Vector3.forward, out ux, out vx, out nx);
                o = fallback.position;
                sz = new Vector2(1f, 1f);
                return;
            }

            ux = fit.ux;
            vx = fit.vx;
            nx = fit.nx;
            o = fit.center;

            float width = Mathf.Max(1e-4f, fit.width + padding.x);
            float height = Mathf.Max(1e-4f, fit.height + padding.y);
            sz = new Vector2(width, height);
        }

        PlaneFitData ChooseBestFit(Vector3 axisA, Vector3 axisB, Vector3 axisC)
        {
            PlaneFitData best = default;

            PlaneFitData fitXZ = ComputePlaneFit(axisA, axisC);
            if (fitXZ.valid)
                best = fitXZ;

            PlaneFitData fitXY = ComputePlaneFit(axisA, axisB);
            if (fitXY.valid && (!best.valid || fitXY.area > best.area))
                best = fitXY;

            PlaneFitData fitYZ = ComputePlaneFit(axisB, axisC);
            if (fitYZ.valid && (!best.valid || fitYZ.area > best.area))
                best = fitYZ;

            return best;
        }

        PlaneFitData ComputePlaneFit(Vector3 axisU, Vector3 axisV)
        {
            PlaneFitData fit = default;
            if (boundsWorldPoints.Count == 0) return fit;

            if (!BuildOrthonormalBasis(axisU, axisV, out Vector3 u, out Vector3 v, out Vector3 n))
                return fit;

            float minU = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float minV = float.PositiveInfinity;
            float maxV = float.NegativeInfinity;
            float minN = float.PositiveInfinity;
            float maxN = float.NegativeInfinity;

            for (int i = 0; i < boundsWorldPoints.Count; i++)
            {
                Vector3 p = boundsWorldPoints[i];
                if (!IsFiniteVector(p)) continue;

                float du = Vector3.Dot(p, u);
                float dv = Vector3.Dot(p, v);
                float dn = Vector3.Dot(p, n);

                if (du < minU) minU = du;
                if (du > maxU) maxU = du;
                if (dv < minV) minV = dv;
                if (dv > maxV) maxV = dv;
                if (dn < minN) minN = dn;
                if (dn > maxN) maxN = dn;
            }

            if (!IsFinite(minU) || !IsFinite(minV) || !IsFinite(minN))
                return fit;

            fit.valid = true;
            fit.ux = u;
            fit.vx = v;
            fit.nx = n;
            fit.minU = minU;
            fit.maxU = maxU;
            fit.minV = minV;
            fit.maxV = maxV;
            fit.minN = minN;
            fit.maxN = maxN;
            fit.width = Mathf.Max(0f, maxU - minU);
            fit.height = Mathf.Max(0f, maxV - minV);
            fit.area = fit.width * fit.height;

            float centerU = 0.5f * (minU + maxU);
            float centerV = 0.5f * (minV + maxV);
            float centerN = 0.5f * (minN + maxN);
            fit.center = u * centerU + v * centerV + n * centerN;

            return fit;
        }

        static bool BuildOrthonormalBasis(Vector3 axisU, Vector3 axisV, out Vector3 u, out Vector3 v, out Vector3 n)
        {
            u = axisU;
            v = axisV;

            if (!TryNormalize(ref u))
            {
                n = Vector3.zero;
                return false;
            }

            v -= Vector3.Dot(v, u) * u;
            if (!TryNormalize(ref v))
            {
                Vector3 fallback = Vector3.Cross(u, Vector3.up);
                if (!TryNormalize(ref fallback))
                    fallback = Vector3.Cross(u, Vector3.right);
                if (!TryNormalize(ref fallback))
                {
                    n = Vector3.zero;
                    return false;
                }
                v = fallback;
            }

            n = Vector3.Cross(u, v);
            if (!TryNormalize(ref n))
                return false;

            v = Vector3.Cross(n, u);
            return true;
        }

        static Vector3 SafeDirection(Vector3 dir)
        {
            if (TryNormalize(ref dir))
                return dir;
            return Vector3.forward;
        }

        static bool TryGetSharedMesh(Renderer renderer, out Mesh mesh)
        {
            mesh = null;
            if (!renderer) return false;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
                return mesh != null;
            }

            if (renderer.TryGetComponent<MeshFilter>(out var filter))
            {
                mesh = filter.sharedMesh;
                return mesh != null;
            }

            return false;
        }

        static bool TryNormalize(ref Vector3 v)
        {
            float mag = v.magnitude;
            if (IsFinite(mag) && mag > 1e-6f)
            {
                v /= mag;
                return true;
            }

            v = Vector3.zero;
            return false;
        }

        static bool IsFiniteVector(Vector2 v)
        {
            return IsFinite(v.x) && IsFinite(v.y);
        }

        static bool IsFiniteVector(Vector3 v)
        {
            return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
        }

        static BoundsSnapshot CaptureBoundsSnapshot(Renderer r)
        {
            Bounds wb = r.bounds;
            return new BoundsSnapshot
            {
                center = wb.center,
                extents = wb.extents
            };
        }

        static bool BoundsSnapshotChanged(BoundsSnapshot previous, BoundsSnapshot current)
        {
            return (previous.center - current.center).sqrMagnitude > kBoundsChangeThresholdSqr ||
                   (previous.extents - current.extents).sqrMagnitude > kBoundsChangeThresholdSqr;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        RenderTextureFormat PickRenderTextureFormat()
        {
            if (!isOpenGLLike)
            {
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                    return RenderTextureFormat.ARGBHalf;
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                    return RenderTextureFormat.ARGBFloat;
            }

            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                return RenderTextureFormat.ARGB32;

            return RenderTextureFormat.Default;
        }

        float DetermineRestoreQuantizationStep(RenderTextureFormat format)
        {
            switch (format)
            {
                case RenderTextureFormat.ARGB32:
                case RenderTextureFormat.BGRA32:
                case RenderTextureFormat.Default:
                case RenderTextureFormat.DefaultHDR:
                    return 1f / 255f;
                default:
                    return 0f;
            }
        }

        void ResetRestoreBudgets()
        {
            restoreStepBudgetRG = 0f;
            restoreStepBudgetAlpha = 0f;
        }

        float ConsumeRestoreBudget(ref float budget, float delta, float minStep)
        {
            if (delta <= 0f && budget <= 0f)
                return 0f;

            budget = Mathf.Clamp01(budget + Mathf.Max(delta, 0f));
            if (budget <= 0f)
                return 0f;

            float threshold = Mathf.Max(minStep, 0f);
            if (threshold <= 0f)
            {
                float appliedAll = budget;
                budget = 0f;
                return Mathf.Clamp01(appliedAll);
            }

            if (budget < threshold)
                return 0f;

            float steps = Mathf.Floor(budget / threshold);
            float applied = Mathf.Clamp01(steps * threshold);
            budget = Mathf.Clamp01(budget - applied);
            return applied;
        }

        void CacheGraphicsBackend()
        {
            var gdt = SystemInfo.graphicsDeviceType;
            isOpenGLLike = gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3;
        }

        struct SourceState
        {
            public Vector3 prevPos;
            public Vector2 smoothVel;
        }

        struct PaintRequest
        {
            public Vector2 dir;
            public float brushU;
            public float brushV;
        }

        struct BoundsSnapshot
        {
            public Vector3 center;
            public Vector3 extents;
        }

        struct PlaneFitData
        {
            public bool valid;
            public Vector3 ux, vx, nx;
            public float minU, maxU, minV, maxV, minN, maxN;
            public float width, height, area;
            public Vector3 center;
        }

    }
}
