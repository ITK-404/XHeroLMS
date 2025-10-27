using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabUI : MonoBehaviour
{
    public CourseLessonTabID tabID;

    [Header("Refs")]
    public TabItemManagerUI manager;                   // optional
    [Tooltip("Kéo thả CourseListPageAllUI vào đây")]
    public CourseListPageAllUI listUI;                 // <- GẮN QUA INSPECTOR

    private Button button;

    [Header("Visuals")]
    public Sprite activeSprite;
    public Sprite deActiveSprite;
    public TextMeshProUGUI nameTitle;
    public TMP_ColorGradient gradient;
    public Color deActiveColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClickTab);
        ActiveState(false);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClickTab); // FIX
    }

    private void OnClickTab()
    {
        // 1) Bật/tắt state UI của tab (nếu bạn có manager)
        if (manager != null) manager.ActiveTab(tabID);

        // 2) Gọi CourseListPageAllUI để LỌC VÀ RENDER LẠI THEO GROUP
        //    Ưu tiên dùng reference đã gắn trong Inspector
        var target = listUI;
        if (target == null)
        {
            // fallback an toàn (trường hợp quên gán)
#if UNITY_2022_2_OR_NEWER
            target = FindFirstObjectByType<CourseListPageAllUI>();
#else
            target = Object.FindObjectOfType<CourseListPageAllUI>();
#endif
        }

        if (target != null)
        {
            target.RefreshForTab(tabID);
        }
        else
        {
            Debug.LogWarning($"[TabUI] Không tìm thấy CourseListPageAllUI để refresh (tabID={tabID}).");
        }
    }

    public void ActiveState(bool state)
    {
        if (button == null) return;

        button.image.sprite = state ? activeSprite : deActiveSprite;
        SetGradientActive(state);
    }

    public void SetGradientActive(bool enable)
    {
        if (nameTitle == null) return;

        if (enable)
        {
            nameTitle.enableVertexGradient = true;
            nameTitle.colorGradientPreset = gradient;
        }
        else
        {
            nameTitle.enableVertexGradient = false;
            nameTitle.color = deActiveColor;
        }
        nameTitle.ForceMeshUpdate();
    }
}
