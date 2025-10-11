#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapColliderBuilderWindow : EditorWindow
{
    public enum BuildMode
    {
        MeshColliderPerMesh, // chính xác, nặng hơn
        BoxPerRenderer       // nhẹ, nhanh, gần đúng
    }

    // ====== Options (persisted via EditorPrefs) ======
    BuildMode mode = BuildMode.MeshColliderPerMesh;
    bool includeInactive = true;
    bool removeExistingFirst = true;

    bool convex = false;                  // cho MeshCollider (đối tượng động)
    bool useTightCooking = true;          // cookingOptions nếu Unity hỗ trợ

    float skipIfBoundsDiagonalUnder = 0.02f;

    bool skinnedUseBounds = true;         // Skinned: Box từ localBounds (nhanh) hay Bake mesh (chính xác)

    // Lọc theo Layer/Tag (tuỳ chọn)
    bool filterByLayer = false;
    int  layerMask = ~0;                  // mặc định tất cả layer
    bool filterByTag = false;
    string requiredTag = "Untagged";

    // Chỉ áp dụng cho Static (tuỳ chọn)
    bool onlyStatic = false;

    // Gắn Rigidbody ở root (chế độ compound động — tuỳ chọn; an toàn: off mặc định)
    bool addRigidbodyToRoots = false;
    bool rigidbodyIsKinematic = true;
    bool rigidbodyUseGravity = false;

    // Marker để biết collider do tool sinh ra (dễ Clear)
    const string MARKER_COMPONENT_NAME = "GeneratedColliderMarker";

    int created, skipped, removed;
    Vector2 scroll;

    [MenuItem("Tools/Map Collider Builder")]
    static void Open() => GetWindow<MapColliderBuilderWindow>("Map Collider Builder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Map Collider Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        using (var s = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = s.scrollPosition;

            // Build mode
            mode = (BuildMode)EditorGUILayout.EnumPopup(new GUIContent("Build Mode"), mode);

            EditorGUILayout.Space(6);
            includeInactive      = EditorGUILayout.ToggleLeft("Include Inactive Objects", includeInactive);
            removeExistingFirst  = EditorGUILayout.ToggleLeft("Remove Existing Colliders First", removeExistingFirst);
            onlyStatic           = EditorGUILayout.ToggleLeft("Only GameObjects Marked Static", onlyStatic);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Filters (Optional)", EditorStyles.boldLabel);
            filterByLayer = EditorGUILayout.ToggleLeft("Filter by Layer", filterByLayer);
            if (filterByLayer)
                layerMask = EditorGUILayout.MaskField("Layer Mask", layerMask, UnityEditorInternal.InternalEditorUtility.layers);

            filterByTag = EditorGUILayout.ToggleLeft("Filter by Tag", filterByTag);
            if (filterByTag)
                requiredTag = EditorGUILayout.TagField("Required Tag", requiredTag);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("MeshCollider Options", EditorStyles.boldLabel);
            convex          = EditorGUILayout.Toggle("Convex (for dynamic)", convex);
            useTightCooking = EditorGUILayout.Toggle(new GUIContent("Use Tight Cooking", "Dùng cookingOptions để collider bám mesh hơn (nếu bản Unity hỗ trợ)"), useTightCooking);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Box Mode Options", EditorStyles.boldLabel);
            skipIfBoundsDiagonalUnder = EditorGUILayout.Slider(new GUIContent("Skip If Diagonal < (m)", "Bỏ qua renderer quá nhỏ để tránh spam collider vụn"), skipIfBoundsDiagonalUnder, 0f, 0.25f);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Skinned Mesh", EditorStyles.boldLabel);
            skinnedUseBounds = EditorGUILayout.Toggle(new GUIContent("Use Bounds (fast) instead of Bake", "Bật: dùng Box theo localBounds. Tắt: Bake ra MeshCollider (chính xác, nặng)"), skinnedUseBounds);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Rigidbody (Optional)", EditorStyles.boldLabel);
            addRigidbodyToRoots   = EditorGUILayout.ToggleLeft("Add Rigidbody to Scene Roots (for compound dynamics)", addRigidbodyToRoots);
            if (addRigidbodyToRoots)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    rigidbodyIsKinematic = EditorGUILayout.Toggle("Is Kinematic", rigidbodyIsKinematic);
                    rigidbodyUseGravity  = EditorGUILayout.Toggle("Use Gravity", rigidbodyUseGravity);
                }
            }

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Colliders for Entire Scene", GUILayout.Height(32)))
                {
                    RunForScene(build: true);
                }
                if (GUILayout.Button("Clear Generated Colliders (Scene)", GUILayout.Height(32)))
                {
                    RunForScene(build: false);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build for Selection Only", GUILayout.Height(24)))
                {
                    RunForSelection(build: true);
                }
                if (GUILayout.Button("Clear Generated (Selection)", GUILayout.Height(24)))
                {
                    RunForSelection(build: false);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "• MeshColliderPerMesh: chính xác nhất (bật Convex nếu có Rigidbody).\n" +
                "• BoxPerRenderer: nhẹ, dùng localBounds của renderer.\n" +
                "• Dùng bộ lọc Layer/Tag/Static nếu muốn giới hạn khu vực build.\n" +
                "• Bộ đánh dấu (marker) sẽ được thêm để tiện Clear về sau.",
                MessageType.Info);
        }
    }

    // ================= Core =================
    void RunForScene(bool build)
    {
        created = skipped = removed = 0;

        var sceneObjects = GetAllSceneGameObjects(includeInactive);
        if (sceneObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Map Collider Builder", "No scene objects found.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            int count = sceneObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var go = sceneObjects[i];
                if (!go) continue;

                if (EditorUtility.DisplayCancelableProgressBar(build ? "Building Colliders..." : "Clearing Colliders...", go.name, (float)i / count))
                    break;

                if (onlyStatic && !HasAnyStaticFlag(go))
                    continue;

                if (filterByLayer && ((1 << go.layer) & MaskToLayerMask(layerMask)) == 0)
                    continue;

                if (filterByTag && !go.CompareTag(requiredTag))
                    continue;

                if (build)
                    BuildForRoot(go.transform);
                else
                    ClearForRoot(go.transform);

                if (build && addRigidbodyToRoots && go.transform.parent == null)
                    EnsureRigidbody(go);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(build
                ? $"[MapColliderBuilder] Mode={mode}  Created={created}, Skipped={skipped}."
                : $"[MapColliderBuilder] Removed generated colliders: {removed}.");
        }
    }

    void RunForSelection(bool build)
    {
        created = skipped = removed = 0;

        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Map Collider Builder", "No selection. Select one or more objects in the Hierarchy.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            int count = selection.Length;
            for (int i = 0; i < count; i++)
            {
                var go = selection[i];
                if (!go) continue;

                if (EditorUtility.DisplayCancelableProgressBar(build ? "Building Colliders (Selection)..." : "Clearing Colliders (Selection)...", go.name, (float)i / count))
                    break;

                if (build)
                    BuildForRoot(go.transform);
                else
                    ClearForRoot(go.transform);

                if (build && addRigidbodyToRoots && go.transform.parent == null)
                    EnsureRigidbody(go);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(build
                ? $"[MapColliderBuilder] Mode={mode}  Created={created}, Skipped={skipped} (Selection)."
                : $"[MapColliderBuilder] Removed generated colliders: {removed} (Selection).");
        }
    }

    // Build for a hierarchy root
    void BuildForRoot(Transform root)
    {
        if (!root) return;

        if (removeExistingFirst)
            ClearForRoot(root);

        // MeshFilter path
        var meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactive);
        if (mode == BuildMode.MeshColliderPerMesh)
        {
            foreach (var mf in meshFilters)
            {
                if (!mf || !mf.sharedMesh) { skipped++; continue; }
                var mr = mf.GetComponent<Renderer>();
                if (!mr) { skipped++; continue; }
                if (ShouldSkipBySize(mr, skipIfBoundsDiagonalUnder)) { skipped++; continue; }

                if (mf.TryGetComponent<Collider>(out _)) { skipped++; continue; }

                Undo.RegisterCompleteObjectUndo(mf.gameObject, "Add MeshCollider");
                var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convex;

#if UNITY_2020_2_OR_NEWER
                if (useTightCooking)
                {
                    mc.cookingOptions =
                          MeshColliderCookingOptions.EnableMeshCleaning
                        | MeshColliderCookingOptions.WeldColocatedVertices
                        | MeshColliderCookingOptions.UseFastMidphase
                        | MeshColliderCookingOptions.CookForFasterSimulation;
                }
#endif
                AddMarker(mf.gameObject);
                created++;
            }
        }
        else // BoxPerRenderer
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
            foreach (var r in renderers)
            {
                if (!r) { skipped++; continue; }
                // SKIP skinned ở vòng này để xử lý ở khối skinned phía dưới
                if (r is SkinnedMeshRenderer) { continue; }

                if (ShouldSkipBySize(r, skipIfBoundsDiagonalUnder)) { skipped++; continue; }

                // Tránh duplicate: nếu đã có Collider thì bỏ qua
                if (r.TryGetComponent<Collider>(out _)) { skipped++; continue; }

                Undo.RegisterCompleteObjectUndo(r.gameObject, "Add BoxCollider");
                var bc = Undo.AddComponent<BoxCollider>(r.gameObject);
                ApplyLocalBoundsToBox(bc, r.localBounds);
                AddMarker(r.gameObject);
                created++;
            }
        }

        // Skinned Mesh path
        var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
        foreach (var smr in smrs)
        {
            if (!smr) { skipped++; continue; }

            // Nếu đã có Collider sẵn thì bỏ qua để không duplicate
            if (smr.TryGetComponent<Collider>(out _)) { skipped++; continue; }

            if (mode == BuildMode.BoxPerRenderer || skinnedUseBounds)
            {
                var diag = smr.localBounds.size.magnitude;
                if (diag < skipIfBoundsDiagonalUnder) { skipped++; continue; }

                Undo.RegisterCompleteObjectUndo(smr.gameObject, "Add BoxCollider");
                var bc = Undo.AddComponent<BoxCollider>(smr.gameObject);
                ApplyLocalBoundsToBox(bc, smr.localBounds);
                AddMarker(smr.gameObject);
                created++;
            }
            else
            {
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                if (baked.vertexCount < 3)
                {
                    DestroyImmediate(baked);
                    skipped++;
                    continue;
                }

                Undo.RegisterCompleteObjectUndo(smr.gameObject, "Add MeshCollider");
                var mc = Undo.AddComponent<MeshCollider>(smr.gameObject);
                mc.sharedMesh = baked;
                mc.convex = convex;
        #if UNITY_2020_2_OR_NEWER
                if (useTightCooking)
                {
                    mc.cookingOptions =
                        MeshColliderCookingOptions.EnableMeshCleaning
                        | MeshColliderCookingOptions.WeldColocatedVertices
                        | MeshColliderCookingOptions.UseFastMidphase
                        | MeshColliderCookingOptions.CookForFasterSimulation;
                }
        #endif
                AddMarker(smr.gameObject);
                created++;
            }
        }
    }

    void ClearForRoot(Transform root)
    {
        if (!root) return;

        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (!t) continue;

            var marker = t.GetComponent<GeneratedColliderMarker>();
            if (marker == null) continue;

            var colliders = t.GetComponents<Collider>();
            foreach (var c in colliders)
            {
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
            Undo.DestroyObjectImmediate(marker);
        }
    }

    // ================= Utils =================
    static List<GameObject> GetAllSceneGameObjects(bool includeInactive)
    {
        var results = new List<GameObject>();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go) continue;
            if (EditorUtility.IsPersistent(go)) continue; // skip asset/prefab file
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            if (!includeInactive && !go.activeInHierarchy) continue;

            results.Add(go);
        }
        return results;
    }

    static bool ShouldSkipBySize(Renderer r, float threshold)
    {
        var diag = r.localBounds.size.magnitude;
        return diag < threshold;
    }

    static void ApplyLocalBoundsToBox(BoxCollider bc, Bounds localBounds)
    {
        bc.center = localBounds.center; // local space
        bc.size   = localBounds.size;
    }

    void EnsureRigidbody(GameObject root)
    {
        var rb = root.GetComponent<Rigidbody>();
        if (!rb)
        {
            Undo.RegisterCompleteObjectUndo(root, "Add Rigidbody");
            rb = Undo.AddComponent<Rigidbody>(root);
        }
        rb.isKinematic = rigidbodyIsKinematic;
        rb.useGravity  = rigidbodyUseGravity;
    }

    static int MaskToLayerMask(int maskField)
    {
        return maskField;
    }

    // Add a tiny marker component (hidden) to tag generated colliders' GO
    static void AddMarker(GameObject go)
    {
        if (!go.GetComponent(MARKER_COMPONENT_NAME))
        {
            var marker = go.AddComponent<GeneratedColliderMarker>();
            marker.hideFlags = HideFlags.HideInInspector;
        }
    }
    static bool HasAnyStaticFlag(GameObject go)
    {
        var flags = GameObjectUtility.GetStaticEditorFlags(go);

        // Chỉ giữ lại những flag hiện còn hợp lệ (và tương đương với các cũ)
        var mask =
            StaticEditorFlags.ContributeGI |     // thay cho LightmapStatic
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        // NavigationStatic và OffMeshLinkGeneration đã bị loại bỏ trong Unity mới
        return (flags & mask) != 0;
    }
    // Dummy type to mark generated colliders
    [AddComponentMenu("")]
    private class GeneratedColliderMarker : MonoBehaviour { }
}
#endif
