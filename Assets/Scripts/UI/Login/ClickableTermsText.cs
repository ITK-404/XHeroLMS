using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ClickableTermsText : MonoBehaviour, IPointerClickHandler
{
    [Header("Text hiển thị")]
    public TMP_Text targetText;

    [Header("Link")]
    public string privacyPolicyUrl;   // link Chính sách bảo mật
    public string termsOfUseUrl;      // link Điều khoản sử dụng

    [Header("Màu chữ link")]
    public Color linkColor = new Color(1f, 0.5f, 0f); // cam cam

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        ApplyFormattedText();
    }

    public void ApplyFormattedText()
    {
        if (targetText == null)
        {
            Debug.LogWarning("[ClickableTermsText] targetText == null");
            return;
        }

        string colorHex = ColorUtility.ToHtmlStringRGB(linkColor);

        string formatted =
            $"Tôi đồng ý với " +
            $"<link=\"privacy\"><color=#{colorHex}><u>Chính sách bảo mật</u></color></link> " +
            $"và " +
            $"<link=\"terms\"><color=#{colorHex}><u>Điều khoản sử dụng</u></color></link> " +
            $"dịch vụ từ PTĐN";

        targetText.richText = true;
        targetText.raycastTarget = true;
        targetText.text = formatted;

        // Cập nhật mesh để có linkInfo
        targetText.ForceMeshUpdate();

        Debug.Log($"[ClickableTermsText] formatted text set. linkCount = {targetText.textInfo.linkCount}");
        for (int i = 0; i < targetText.textInfo.linkCount; i++)
        {
            var li = targetText.textInfo.linkInfo[i];
            Debug.Log($"[ClickableTermsText] link[{i}] id={li.GetLinkID()}, text=\"{li.GetLinkText()}\"");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetText == null) return;

        // Log cho chắc là đã click vào đúng object
        Debug.Log("[ClickableTermsText] OnPointerClick");

        // Đảm bảo textInfo mới nhất (phòng khi layout thay đổi sau Start)
        targetText.ForceMeshUpdate();

        // Lấy Canvas + camera đúng kiểu
        Canvas canvas = targetText.canvas;
        Camera cam = null;

        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cam = null; // overlay phải để null
            }
            else
            {
                cam = canvas.worldCamera;
            }
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            targetText,
            eventData.position,
            cam
        );

        Debug.Log($"[ClickableTermsText] linkIndex = {linkIndex}, linkCount = {targetText.textInfo.linkCount}");

        if (linkIndex == -1)
        {
            Debug.Log("[ClickableTermsText] Không trúng link nào (click ngoài vùng chữ link hoặc linkInfo chưa có).");
            return;
        }

        TMP_LinkInfo linkInfo = targetText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();
        string linkText = linkInfo.GetLinkText();

        Debug.Log($"[ClickableTermsText] Clicked link index={linkIndex}, id=\"{linkId}\", text=\"{linkText}\"");

        if (linkId == "privacy")
        {
            Debug.Log($"[ClickableTermsText] Mở privacyPolicyUrl = {privacyPolicyUrl}");
            if (!string.IsNullOrEmpty(privacyPolicyUrl))
                Application.OpenURL(privacyPolicyUrl);
            else
                Debug.LogWarning("[ClickableTermsText] privacyPolicyUrl đang rỗng!");
        }
        else if (linkId == "terms")
        {
            Debug.Log($"[ClickableTermsText] Mở termsOfUseUrl = {termsOfUseUrl}");
            if (!string.IsNullOrEmpty(termsOfUseUrl))
                Application.OpenURL(termsOfUseUrl);
            else
                Debug.LogWarning("[ClickableTermsText] termsOfUseUrl đang rỗng!");
        }
        else
        {
            Debug.Log($"[ClickableTermsText] linkId không khớp (\"{linkId}\")");
        }
    }
}
