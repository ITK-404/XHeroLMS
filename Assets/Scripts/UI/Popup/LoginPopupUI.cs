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

    public enum PopupIconType
    {
        None,
        Info,
        Warning,
        Error,
        Success
    }

    [Header("Header Icon (Drag & Drop)")]
    [Tooltip("Sprite Asset mà TextHeader sẽ dùng để render icon inline.")]
    [SerializeField] private TMP_SpriteAsset headerSpriteAsset;

    [Tooltip("Kéo icon (Sprite) vào từng field. Sprite phải nằm trong headerSpriteAsset.")]
    [SerializeField] private Sprite iconInfo;
    [SerializeField] private Sprite iconWarning;
    [SerializeField] private Sprite iconError;
    [SerializeField] private Sprite iconSuccess;

    [Tooltip("Scale icon so với font (1.0 = theo size chữ).")]
    [SerializeField] private float headerIconSize = 1.05f;

    [Tooltip("Nâng/hạ icon theo px để canh baseline. (vd: 1~3px)")]
    [SerializeField] private float headerIconVOffsetPx = 1.5f;

    [Tooltip("Thêm khoảng trắng sau icon")]
    [SerializeField] private bool addSpaceAfterIcon = true;

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

        string iconTag = BuildHeaderIconTag(iconType);
        textHeader.text = $"{iconTag}{(header ?? "")}";
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
        // Nếu bạn muốn “kéo sprite asset vào code”, mình cũng tự set vào TMP để khỏi quên.
        if (textHeader != null && headerSpriteAsset != null && textHeader.spriteAsset != headerSpriteAsset)
            textHeader.spriteAsset = headerSpriteAsset;

        // RichText thường bật sẵn, nhưng nếu project bạn tắt thì bật lại
        if (textHeader != null) textHeader.richText = true;
    }

    private void BindReturn(UnityAction onReturn)
    {
        if (returnBtn == null) return;

        returnBtn.onClick.RemoveAllListeners();

        if (onReturn != null)
            returnBtn.onClick.AddListener(onReturn);

        returnBtn.onClick.AddListener(() => Destroy(gameObject));
    }

    private string BuildHeaderIconTag(PopupIconType iconType)
    {
        if (iconType == PopupIconType.None) return "";
        if (headerSpriteAsset == null) return ""; // chưa gán asset thì thôi

        Sprite sp = GetIconSprite(iconType);
        if (sp == null) return "";

        // TMP chỉ render được sprite nằm trong TMP Sprite Asset.
        // Ta sẽ lấy đúng spriteName (trong spriteCharacterTable) dựa theo sprite textureName.
        string spriteNameInAsset = FindSpriteNameInAsset(sp, headerSpriteAsset);
        if (string.IsNullOrEmpty(spriteNameInAsset))
        {
            // Không tìm thấy -> khả năng sprite bạn kéo vào không nằm trong SpriteAsset này
            Debug.LogWarning($"[LoginPopupUI] Sprite '{sp.name}' không nằm trong SpriteAsset '{headerSpriteAsset.name}'. " +
                             $"Hãy đảm bảo icon được pack vào TMP Sprite Asset.");
            return "";
        }

        string tag = $"<sprite name=\"{spriteNameInAsset}\" size={headerIconSize}>";

        if (headerIconVOffsetPx != 0f)
            tag = $"<voffset={headerIconVOffsetPx}px>{tag}</voffset>";

        if (addSpaceAfterIcon)
            tag += " ";

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

    private static string FindSpriteNameInAsset(Sprite sprite, TMP_SpriteAsset asset)
    {
        if (sprite == null || asset == null) return null;

        // TMP store sprite entries as TMP_SpriteCharacter with name=textureName
        // textureName thường trùng sprite.name, nhưng không phải lúc nào cũng vậy (tuỳ lúc tạo asset).
        // Ta match theo texture rect/name tốt nhất là dùng textureName.
        var table = asset.spriteCharacterTable;
        if (table == null) return null;

        // 1) thử match theo sprite.name
        for (int i = 0; i < table.Count; i++)
        {
            var ch = table[i];
            if (ch == null) continue;
            if (string.Equals(ch.name, sprite.name, System.StringComparison.Ordinal))
                return ch.name;
        }

        // 2) fallback: match theo textureName nếu có
        // (Một số version TMP set textureName ở spriteGlyph)
        var glyphTable = asset.spriteGlyphTable;
        if (glyphTable != null)
        {
            // thử match dựa trên rect gần đúng (ít khi cần)
            var r = sprite.rect;
            for (int i = 0; i < glyphTable.Count; i++)
            {
                var g = glyphTable[i];
                if (g == null) continue;
                var gr = g.glyphRect;
                if (Mathf.Approximately(gr.x, r.x) &&
                    Mathf.Approximately(gr.y, r.y) &&
                    Mathf.Approximately(gr.width, r.width) &&
                    Mathf.Approximately(gr.height, r.height))
                {
                    // lấy character tương ứng index
                    // (thường spriteCharacterTable và spriteGlyphTable cùng thứ tự)
                    if (i < table.Count && table[i] != null)
                        return table[i].name;
                }
            }
        }

        return null;
    }
}
