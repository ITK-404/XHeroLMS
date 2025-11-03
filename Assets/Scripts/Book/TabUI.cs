using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabUI : MonoBehaviour
{
    public CourseLessonTabID tabID;

    [Header("Refs")]
    public TabItemManagerUI manager;
    [Tooltip("Kéo thả CourseListPageAllUI vào đây (nếu dùng tab ALL)")]
    public CourseListPageAllUI listUIAll;
    [Tooltip("Kéo thả CourseListPageMyUI vào đây (nếu dùng tab MY)")]
    public CourseListPageMyUI listUIMy;

    private Button button;

    [Header("Visuals")]
    public Sprite activeSprite;
    public Sprite deActiveSprite;
    public TextMeshProUGUI nameTitle;
    public TMP_ColorGradient gradient;
    public Color deActiveColor = Color.white;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClickTab);
        ActiveState(false);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClickTab);
    }

    private void OnClickTab()
    {
        if (manager != null)
            manager.ActiveTab(tabID);
            
        string savedKey = CourseMenuButtons.GetSavedKey();
        
        object target = null;

        if (savedKey == CourseMenuButtons.KEY_ALL)
        {
            target = listUIAll;
            if (target == null)
            {
                // fallback an toàn: tìm trong scene đúng loại ALL
#if UNITY_2022_2_OR_NEWER
                target = FindFirstObjectByType<CourseListPageAllUI>();
#else
                target = Object.FindObjectOfType<CourseListPageAllUI>();
#endif
            }
        }
        else
        {
            target = listUIMy;
            if (target == null)
            {
                // fallback an toàn: tìm trong scene đúng loại MY
#if UNITY_2022_2_OR_NEWER
                target = FindFirstObjectByType<CourseListPageMyUI>();
#else
                target = Object.FindObjectOfType<CourseListPageMyUI>();
#endif
            }
        }

        // Gọi RefreshForTab cho đúng component
        if (target is CourseListPageAllUI allUI && allUI != null)
        {
            allUI.RefreshForTab(tabID);
        }
        else if (target is CourseListPageMyUI myUI && myUI != null)
        {
            myUI.RefreshForTab(tabID);
        }
        else
        {
            Debug.LogWarning($"[TabUI] Không tìm thấy UI phù hợp để refresh (tabID={tabID}, key={savedKey}).");
        }
    }

    public void ActiveState(bool state)
    {
        if (button == null) return;

        // An toàn khi thiếu Image hoặc Sprite
        var img = button.image;
        if (img != null)
        {
            if (state && activeSprite != null)
                img.sprite = activeSprite;
            else if (!state && deActiveSprite != null)
                img.sprite = deActiveSprite;
        }

        SetGradientActive(state);
    }

    public void SetGradientActive(bool enable)
    {
        if (nameTitle == null) return;

        if (enable)
        {
            nameTitle.enableVertexGradient = true;
            if (gradient != null)
            {
                nameTitle.colorGradientPreset = gradient;
            }
        }
        else
        {
            nameTitle.enableVertexGradient = false;
            nameTitle.color = deActiveColor;
        }
        nameTitle.ForceMeshUpdate();
    }
}
