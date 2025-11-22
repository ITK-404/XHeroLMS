using System.Collections.Generic;
using UnityEngine;

public class MatchingElementHandler : MonoBehaviour
{
    public static MatchingElementHandler Instance { get; private set; }

    [Header("Line Renderer Settings")]
    [SerializeField] private GameObject linePrefab; // Prefab có LineRenderer + đã set material, width...
    [SerializeField] private Transform lineParent;   // Optional: để gom tất cả line vào 1 parent

    // Lưu tất cả các cặp đã kết nối để quản lý dễ dàng
    private readonly Dictionary<ExamMatchingElement, ExamMatchingElement> connections = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        { 
            Destroy(gameObject); 
        }
        else
        {
            Instance = this;
        }
    }
    /// <summary>
    /// Thử kết nối 2 phần tử. Nếu một trong hai đã có đôi thì tự động hủy đôi cũ.
    /// </summary>
    public void TryConnect(ExamMatchingElement a, ExamMatchingElement b)
    {
        // Kiểm tra điều kiện hợp lệ
        if (a == null || b == null || a.side == b.side || a == b) return;

        // Hủy các kết nối cũ của cả hai (nếu có)
        if (connections.TryGetValue(a, out var oldB))
        {
            DisconnectPair(a, oldB);
        }
        if (connections.TryGetValue(b, out var oldA))
        {
            DisconnectPair(b, oldA);
        }

        // Tạo LineRenderer mới
        GameObject lineObj = Instantiate(linePrefab, lineParent);
        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        lr.positionCount = 2;

        // Thiết lập kết nối cho cả hai phía
        a.SetConnection(b, lr);
        b.SetConnection(a, lr);

        // Lưu vào dictionary (lưu 2 chiều để dễ tìm)
        connections[a] = b;
        connections[b] = a;

        Debug.Log($"Đã kết nối: {a.name} <-> {b.name}");
    }

    /// <summary>
    /// Ngắt kết nối một cặp cụ thể
    /// </summary>
    public void DisconnectPair(ExamMatchingElement a, ExamMatchingElement b)
    {
        if (a != null) a.ClearConnection();
        if (b != null) b.ClearConnection();

        connections.Remove(a);
        connections.Remove(b);
    }

    /// <summary>
    /// Ngắt kết nối của một phần tử (dùng khi muốn xóa thủ công)
    /// </summary>
    public void Disconnect(ExamMatchingElement element)
    {
        if (connections.TryGetValue(element, out var partner))
        {
            DisconnectPair(element, partner);
        }
    }
}