using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CourseProductItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button actionButton;

    [Header("Optional")]
    [SerializeField] private Sprite defaultIcon;

    [Header("Payment Config")]
    [Tooltip("Nếu item không có variant riêng thì có thể set cứng ở đây.")]
    [SerializeField] private string fallbackVariantId = "682e88f9e1a878778f2575c9";

    [Tooltip("Số lượng mặc định")]
    [SerializeField] private int defaultQty = 1;

    private string _productId;
    private string _variantId;
    private string _externalUrl;
    private Coroutine _loadImageRoutine;

    /// <summary>
    /// Dùng setup này nếu bạn đã có productId và variantId.
    /// </summary>
    public void Setup(string productId, string variantId, string productName, string imageUrl, string externalUrl = "")
    {
        _productId = productId;
        _variantId = string.IsNullOrWhiteSpace(variantId) ? fallbackVariantId : variantId;
        _externalUrl = externalUrl;

        if (nameText != null)
            nameText.text = productName ?? string.Empty;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickButton);

            // Chỉ cần có productId là cho bấm
            actionButton.interactable = !string.IsNullOrWhiteSpace(_productId);
        }

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (iconImage != null)
            iconImage.sprite = defaultIcon;

        if (!string.IsNullOrWhiteSpace(imageUrl))
            _loadImageRoutine = StartCoroutine(LoadImageFromUrl(imageUrl));
    }

    /// <summary>
    /// Giữ tương thích nếu code cũ của bạn vẫn đang gọi setup kiểu cũ.
    /// </summary>
    public void Setup(string productName, string imageUrl, string externalUrl)
    {
        _productId = null;
        _variantId = fallbackVariantId;
        _externalUrl = externalUrl;

        if (nameText != null)
            nameText.text = productName ?? string.Empty;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickButton);

            // Nếu chưa có productId thì fallback theo externalUrl
            bool canClick = !string.IsNullOrWhiteSpace(_productId) || !string.IsNullOrWhiteSpace(_externalUrl);
            actionButton.interactable = canClick;
        }

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (iconImage != null)
            iconImage.sprite = defaultIcon;

        if (!string.IsNullOrWhiteSpace(imageUrl))
            _loadImageRoutine = StartCoroutine(LoadImageFromUrl(imageUrl));
    }

    private void OnClickButton()
    {
        string token = TokenStore.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            LoadingUI.ShowErrorPopup(
                "Bạn cần đăng nhập để tiếp tục thanh toán.",
                "Thông báo"
            );
            return;
        }

        token = token.Trim();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring("Bearer ".Length).Trim();

        if (!string.IsNullOrWhiteSpace(_productId))
        {
            if (string.IsNullOrWhiteSpace(_variantId))
            {
                Debug.LogWarning("[CourseProductItemUI] Missing variantId.");
                LoadingUI.ShowErrorPopup(
                    "Không xác định được biến thể sản phẩm để thanh toán.",
                    "Thông báo"
                );
                return;
            }

            string url =
                "https://phongthuydainam.vn/vi/thanh-toan" +
                "?productId=" + UnityWebRequest.EscapeURL(_productId) +
                "&variant=" + UnityWebRequest.EscapeURL(_variantId) +
                "&qty=" + defaultQty +
                "&accessToken=" + UnityWebRequest.EscapeURL(token);

            Debug.Log("[CourseProductItemUI] Open payment webview: " + url);
            Debug.LogError("[CourseProductItemUI] null title " + url);
            WebViewTest.LoadWebView(url,nameText.text);
            return;
        }

        // fallback nếu item không có productId mà chỉ có externalUrl
        if (!string.IsNullOrWhiteSpace(_externalUrl))
        {
            // WebViewTest.LoadWebView(_externalUrl,"@@@@@@@");
            WebViewTest.LoadWebView(_externalUrl,nameText.text);
            return;
        }

        Debug.LogWarning("[CourseProductItemUI] productId and externalUrl are both empty.");
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogWarning($"[CourseProductItemUI] Load image failed: {url}\n{request.error}");
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
            yield break;

        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

        if (iconImage != null)
            iconImage.sprite = sprite;
    }
}