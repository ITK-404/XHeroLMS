using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoginPopupUI : PopupBaseUI
{
    [Header("Texts")]
    public TextMeshProUGUI textHeader;
    public TextMeshProUGUI textDescription;

    [Header("Buttons")]
    public Button returnBtn;

    public enum PopupIconType { None, Info, Warning, Error, Success }

    [SerializeField] private TMP_SpriteAsset headerSpriteAsset;

    [SerializeField] private Sprite iconInfo;
    [SerializeField] private Sprite iconWarning;
    [SerializeField] private Sprite iconError;
    [SerializeField] private Sprite iconSuccess;

    private int iconSizePercent = 90;

    private float iconVOffsetPx = 10f;
    private float spaceEm = 0.5f;

    // ========================= PUBLIC API =========================

    public void SetHeader(string header)
    {
        if (textHeader == null) return;
        EnsureSpriteAssetBound();
        textHeader.text = header ?? "";
    }

    public void SetHeader(string header, PopupIconType iconType)
    {
        if (textHeader == null) return;
        EnsureSpriteAssetBound();

        header ??= "";

        string iconTag = BuildInlineIconTag(iconType);

        textHeader.text = string.IsNullOrEmpty(iconTag)
            ? header
            : $"{header}{iconTag}";
    }

    public void SetTextDescription(string description)
    {
        if (textDescription == null) return;
        textDescription.text = description ?? "";
    }

    public void Init(string header, string description, UnityAction onReturn = null)
    {
        SetHeader(header);
        SetTextDescription(description);
        BindReturn(onReturn);
    }

    public void Init(string header, string description, PopupIconType headerIcon, UnityAction onReturn = null)
    {
        SetHeader(header, headerIcon);
        SetTextDescription(description);
        BindReturn(onReturn);
    }

    // ========================= INTERNAL =========================

    private void EnsureSpriteAssetBound()
    {
        if (textHeader == null) return;

        if (headerSpriteAsset != null && textHeader.spriteAsset != headerSpriteAsset)
            textHeader.spriteAsset = headerSpriteAsset;

        textHeader.richText = true;
    }

    private void BindReturn(UnityAction onReturn)
    {
        if (returnBtn == null) return;

        returnBtn.onClick.RemoveAllListeners();

        if (onReturn != null)
            returnBtn.onClick.AddListener(onReturn);

        returnBtn.onClick.AddListener(() => Destroy(gameObject));
    }

    private string BuildInlineIconTag(PopupIconType iconType)
    {
        if (iconType == PopupIconType.None) return "";
        if (headerSpriteAsset == null) return "";

        Sprite sp = GetIconSprite(iconType);
        if (sp == null) return "";

        int spriteIndex = FindSpriteIndexInAsset(sp, headerSpriteAsset);
        if (spriteIndex < 0)
        {
            Debug.LogWarning($"[LoginPopupUI] Sprite '{sp.name}' không tìm thấy trong SpriteAsset '{headerSpriteAsset.name}'.");
            return "";
        }

        float em = Mathf.Max(0f, spaceEm);

        // icon luôn là ký tự cuối chuỗi
        string tag = $"<space={em}em><size={iconSizePercent}%><sprite={spriteIndex}></size>";
        if (!Mathf.Approximately(iconVOffsetPx, 0f))
            tag = $"<space={em}em><voffset={iconVOffsetPx}px><size={iconSizePercent}%><sprite={spriteIndex}></size></voffset>";

        return tag;
    }

    private Sprite GetIconSprite(PopupIconType iconType)
    {
        return iconType switch
        {
            PopupIconType.Info => iconInfo,
            PopupIconType.Warning => iconWarning,
            PopupIconType.Error => iconError,
            PopupIconType.Success => iconSuccess,
            _ => null
        };
    }

    private static int FindSpriteIndexInAsset(Sprite sprite, TMP_SpriteAsset asset)
    {
        if (sprite == null || asset == null) return -1;

        var table = asset.spriteCharacterTable;
        if (table == null) return -1;

        for (int i = 0; i < table.Count; i++)
        {
            var ch = table[i];
            if (ch == null) continue;

            if (string.Equals(ch.name, sprite.name, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}
