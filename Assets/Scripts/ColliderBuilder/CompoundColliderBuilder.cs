using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CompoundColliderBuilder : MonoBehaviour
{
    public enum BuildMode
    {
        MeshColliderPerMesh, // chính xác, nặng hơn
        BoxPerRenderer       // nhẹ, nhanh, gần đúng
    }

    [Header("General")]
    public BuildMode mode = BuildMode.MeshColliderPerMesh;
    public bool includeInactive = true;
    public bool removeExistingFirst = true;

    [Header("MeshCollider Options")]
    // Bật nếu object có Rigidbody và di chuyển va chạm động
    public bool convex = false;
    // Giữ mesh nguyên bản khi nấu collider (nếu bản Unity hỗ trợ cooking options)
    public bool useTightCooking = true;

    [Header("Box Mode Options")]
    // Bỏ qua các renderer quá nhỏ (m), tránh spam collider vụn
    public float skipIfBoundsDiagonalUnder = 0.02f;

    [Header("Skinned Mesh")]
    // SkinnedMeshRenderer: dùng bounds thay vì bake mesh (nhanh, gần đúng)
    public bool skinnedUseBounds = true;

    [Header("Debug")]
    public bool logSummary = true;
    
    public void Build()
    {
        if (removeExistingFirst) RemoveAllCollidersInChildren();

        int created = 0;
        int skipped = 0;

        var meshFilters = GetComponentsInChildren<MeshFilter>(includeInactive);
        var smrList     = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);

        if (mode == BuildMode.MeshColliderPerMesh)
        {
            // MeshFilter -> MeshCollider
            foreach (var mf in meshFilters)
            {
                if (!mf || !mf.sharedMesh) { skipped++; continue; }
                var mr = mf.GetComponent<Renderer>();
                if (!mr) { skipped++; continue; }
                if (ShouldSkipBySize(mr)) { skipped++; continue; }

                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convex;

#if UNITY_2020_2_OR_NEWER
                if (useTightCooking)
                {
                    // Gợi ý: các flag sau thường có mặt từ 2020.2+
                    mc.cookingOptions =
                          MeshColliderCookingOptions.EnableMeshCleaning
                        | MeshColliderCookingOptions.WeldColocatedVertices
                        | MeshColliderCookingOptions.UseFastMidphase
                        | MeshColliderCookingOptions.CookForFasterSimulation;
                }
#endif
                created++;
            }

            // SkinnedMeshRenderer
            foreach (var smr in smrList)
            {
                if (!smr) { skipped++; continue; }

                if (skinnedUseBounds)
                {
                    // Nhanh: Box theo localBounds
                    var diag = smr.localBounds.size.magnitude;
                    if (diag < skipIfBoundsDiagonalUnder) { skipped++; continue; }
                    var bc = smr.gameObject.AddComponent<BoxCollider>();
                    ApplyLocalBoundsToBox(bc, smr.localBounds);
                    created++;
                }
                else
                {
                    // Chính xác: bake ra mesh rồi gán MeshCollider
                    var baked = new Mesh();
                    smr.BakeMesh(baked, true);
                    if (baked.vertexCount < 3)
                    {
                        // Không đủ đỉnh để làm collider
                        DestroySafe(baked);
                        skipped++;
                        continue;
                    }

                    var mc = smr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = baked; // Giữ lại baked để MeshCollider dùng
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
                    created++;
                }
            }
        }
        else // BoxPerRenderer
        {
            var renderers = GetComponentsInChildren<Renderer>(includeInactive);
            foreach (var r in renderers)
            {
                if (!r) { skipped++; continue; }
                if (ShouldSkipBySize(r)) { skipped++; continue; }

                var bc = r.gameObject.AddComponent<BoxCollider>();
                ApplyLocalBoundsToBox(bc, r.localBounds);
                created++;
            }
        }

        if (logSummary)
            Debug.Log($"[CompoundColliderBuilder] Mode={mode}  Created={created}, Skipped={skipped}, RemovedOld={(removeExistingFirst ? "Yes" : "No")}", this);
    }

    public void ClearGenerated()
    {
        RemoveAllCollidersInChildren();
        Debug.Log("[CompoundColliderBuilder] Cleared all colliders under this object.", this);
    }

    // ===== INTERNAL =====
    bool ShouldSkipBySize(Renderer r)
    {
        var diag = r.localBounds.size.magnitude;
        return diag < skipIfBoundsDiagonalUnder;
    }

    static void ApplyLocalBoundsToBox(BoxCollider bc, Bounds localBounds)
    {
        bc.center = localBounds.center; // local space
        bc.size   = localBounds.size;
    }

    static void DestroySafe(Object obj)
    {
        if (!obj) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(obj);
        else Object.Destroy(obj);
#else
        Object.Destroy(obj);
#endif
    }

    void RemoveAllCollidersInChildren()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(c);
#else
                DestroyImmediate(c);
#endif
            }
            else
            {
                Destroy(c);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CompoundColliderBuilder))]
public class CompoundColliderBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var t = (CompoundColliderBuilder)target;

        GUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Build Colliders", GUILayout.Height(32)))
            {
                Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Build Colliders");
                t.Build();
                EditorUtility.SetDirty(t.gameObject);
            }
            if (GUILayout.Button("Clear Colliders", GUILayout.Height(32)))
            {
                Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear Colliders");
                t.ClearGenerated();
                EditorUtility.SetDirty(t.gameObject);
            }
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "• MeshColliderPerMesh: chính xác nhất (bật Convex nếu có Rigidbody).\n" +
            "• BoxPerRenderer: nhẹ, dùng localBounds của renderer.\n" +
            "• Đặt Rigidbody ở root để tạo Compound Collider động.\n" +
            "• Skinned: dùng bounds (nhanh) hoặc bake mesh (chính xác).",
            MessageType.Info);
    }
}
#endif
