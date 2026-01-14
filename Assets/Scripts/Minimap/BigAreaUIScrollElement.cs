using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigAreaUIScrollElement : MonoBehaviour
{
    public RectTransform Rect;
    public CanvasGroup CanvasGroup;
    [SerializeField ]TextMeshProUGUI displayNameText;
    [SerializeField ] TextMeshProUGUI percentText;
    public Button btn;
    
    private float timer;
    [HideInInspector] public BigArea bigArea;
    [SerializeField] private Image hiddenIcon;
    [SerializeField] private Image hiddenBackground;
    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();
        
        btn.onClick.AddListener(OnClickButton);

        Highlight(false);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickButton);
    }

    private void OnClickButton()
    {
        if (timer >= 0)
            return;
        timer = 2;
        // for safe handle
        if (bigArea != null)
        {
            AreaDisplayManager.Instance.HighlightSingleArea(bigArea);
        }
    }

    private void LateUpdate()
    {
        if(timer >= 0)
            timer -= Time.deltaTime;
    }

    public void UpdateUI(float percent)
    {
        if (bigArea == null) return;

        displayNameText.text = bigArea.Data.displayName;
        UpdatePercent(percent);
        UpdateHighlight();
    }

    private void UpdatePercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);
        percentText.text = $"{Mathf.RoundToInt(percent)}%";
    }

    public void UpdateHighlight()
    {
        if (bigArea == null) return;
        
        bool isAreSelected = AreaDisplayManager.Instance.SelectArea == bigArea;

        Highlight(isAreSelected);
    }

    private void Highlight(bool isHighlight)
    {
        displayNameText.enableVertexGradient = isHighlight;
        percentText.enableVertexGradient = isHighlight;
        hiddenIcon.gameObject.SetActive(isHighlight);
        hiddenBackground.gameObject.SetActive(isHighlight);
    }
}