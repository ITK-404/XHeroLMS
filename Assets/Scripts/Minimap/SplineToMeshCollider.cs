using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class SplineToMeshCollider : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int resolution = 30; // Số điểm lấy từ spline
    [SerializeField] private Material material; // Material để hiển thị
    
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Start()
    {
        return;
        CreateCollider();
    }

    public void CreateCollider()
    {
        // Lấy points từ spline
        List<Vector3> points = GetSplinePoints();
        
        // Tạo mesh từ points
        Mesh mesh = CreateFlatMesh(points);
        
        // Setup MeshFilter
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        // Setup MeshRenderer
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (material != null)
                meshRenderer.material = material;
        }
        
        // Gắn collider
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        
        meshCollider.sharedMesh = mesh;
    }

    private List<Vector3> GetSplinePoints()
    {
        List<Vector3> points = new List<Vector3>();
        
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 pos = splineContainer.EvaluatePosition(t);
            points.Add(pos);
        }
        
        return points;
    }

    private Mesh CreateFlatMesh(List<Vector3> points)
    {
        Mesh mesh = new Mesh();
        
        int count = points.Count;
        Vector3[] vertices = new Vector3[count];
        int[] triangles = new int[(count - 2) * 3];
        
        // Copy vertices
        for (int i = 0; i < count; i++)
        {
            vertices[i] = points[i];
        }
        
        // Tạo triangles (fan triangulation từ điểm đầu)
        int triIndex = 0;
        for (int i = 1; i < count - 1; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i;
            triangles[triIndex++] = i + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    // Gọi hàm này nếu spline thay đổi
    public void UpdateCollider()
    {
        CreateCollider();
    }
}