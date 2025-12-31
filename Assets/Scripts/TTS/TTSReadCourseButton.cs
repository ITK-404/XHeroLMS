using UnityEngine;
using UnityEngine.UI;

public class TTSReadCourseButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button readButton;

    [Header("Vietnamese TTS tone")]
    [Range(0.2f, 2f)] public float rate = 0.95f;
    [Range(0.5f, 2f)] public float pitch = 1.05f;

    [TextArea(5, 10)]
    public string content = 
        "Khoá học Đại Đạo Chí Giản - Phong Thủy Cổ Học I mang đến cho bạn một cơ hội hiếm có để tìm hiểu và thực hành nghệ thuật phong thủy, một lĩnh vực đa chiều và sâu sắc trong nền văn hóa Á Đông. " +
        "Dưới sự hướng dẫn của Phong Thủy Sư, Thạc sĩ Nguyễn Trọng Mạnh, một trong những chuyên gia hàng đầu trong lĩnh vực này, bạn sẽ khám phá ra những bí quyết và kỹ thuật để áp dụng Phong thủy vào cuộc sống hàng ngày của mình.";

    private void Awake()
    {
        if (readButton != null)
            readButton.onClick.AddListener(ReadContent);
    }

    private void OnDestroy()
    {
        if (readButton != null)
            readButton.onClick.RemoveListener(ReadContent);
    }

    private void ReadContent()
    {
        if (TTSManager.I == null)
        {
            Debug.LogWarning("TTSManager chưa được khởi tạo.");
            return;
        }

        // Set tông giọng tiếng Việt
        TTSManager.I.SetRatePitch(rate, pitch);

        // Đọc nội dung (tự ngắt theo dấu . , ? !)
        TTSManager.I.Speak(content);
    }
}
