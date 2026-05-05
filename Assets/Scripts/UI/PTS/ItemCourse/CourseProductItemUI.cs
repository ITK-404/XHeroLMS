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

    // Nếu không có externalUrl -> ẩn button
    if (actionButton != null)
    {
        bool hasUrl = !string.IsNullOrWhiteSpace(_externalUrl);

        actionButton.gameObject.SetActive(hasUrl);

        if (hasUrl)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickButton);
        }
    }

    // Load image giữ nguyên
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
            "Bạn cần đăng nhập để tiếp tục.",
            "Thông báo"
        );
        return;
    }

    token = token.Trim();
    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        token = token.Substring("Bearer ".Length).Trim();

    if (!string.IsNullOrWhiteSpace(_externalUrl))
    {
        string url = NormalizeProductUrl(_externalUrl);

        string separator = url.Contains("?") ? "&" : "?";
        url += separator + "accessToken=" + UnityWebRequest.EscapeURL(token);

        Debug.Log("[CourseProductItemUI] Open product detail webview: " + url);
        WebViewTest.LoadWebView(url, nameText != null ? nameText.text : "");
        return;
    }

    Debug.LogWarning("[CourseProductItemUI] externalUrl is empty. Cannot open product detail page.");

    LoadingUI.ShowErrorPopup(
        "Sản phẩm này chưa có đường dẫn chi tiết.",
        "Thông báo"
    );
}

private string NormalizeProductUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url))
        return "";

    url = url.Trim();

    // API đang trả: https://phongthuydainam.vn/products/...
    // Web bạn muốn có thể là: https://phongthuydainam.vn/vi/san-phams/...
    url = url.Replace(
        "https://phongthuydainam.vn/products/",
        "https://phongthuydainam.vn/vi/san-phams/"
    );

    return url;
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