using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UI;

public class TabUI : MonoBehaviour
{
    public CourseLessonTabID tabID;
    public TabItemManagerUI manager;
    private Button button;

    public Sprite activeSprite;
    public Sprite deActiveSprite;

    public TextMeshProUGUI nameTitle;
    public TMP_ColorGradient gradient;

    public Color deActiveColor;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClickTab);
        ActiveState(false);
    }


    private void OnDestroy()
    {
        button.onClick.AddListener(OnClickTab);
    }

    private void OnClickTab()
    {
        manager.ActiveTab(tabID);
    }

    public void ActiveState(bool state)
    {
        if (button == null) return;
        if (state)
        {
            button.image.sprite = activeSprite;
        }
        else
        {
            button.image.sprite = deActiveSprite;
        }

        SetGradientActive(state);
    }

    public void SetGradientActive(bool enable)
    {
        var tmp = nameTitle;
        if (enable)
        {
            tmp.enableVertexGradient = true;
            tmp.colorGradientPreset = gradient;
        }
        else
        {
            tmp.enableVertexGradient = false;
            tmp.color = deActiveColor; // hoặc màu text gốc
        }

        tmp.ForceMeshUpdate(); // refresh lại màu
    }
}