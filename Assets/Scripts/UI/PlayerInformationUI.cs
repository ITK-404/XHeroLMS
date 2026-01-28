using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;

public class PlayerInformationUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerName;
    public Image playerImage;

    [Header("Identicon (fallback)")]
    public int identiconSize = 256;
    public bool useIdenticonBackground = true;
    public Color identiconBackground = new Color(0.95f, 0.95f, 0.95f, 1f);

    [Header("Network")]
    public int timeoutSeconds = 15;
    public bool verboseLog = true;

    private Coroutine _loadAvatarCo;

    // === Cache gateway tốt ===
    private const string PREF_IPFS_GOOD_GATEWAY = "IPFS_GOOD_GATEWAY";
    private static readonly string DefaultGoodGateway = "https://dweb.link/ipfs/";

    // Fallback list
    private static readonly string[] FallbackGateways =
    {
        "https://dweb.link/ipfs/",
        "https://ipfs.io/ipfs/",
        "https://gateway.pinata.cloud/ipfs/",
        "https://cloudflare-ipfs.com/ipfs/" // cái này thấy đang deny, để cuối
    };

    private void Awake()
    {
        LoginController.OnLoginComplete += FillData;
    }

    private void OnEnable()
    {
        FillData();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= FillData;
        if (_loadAvatarCo != null) StopCoroutine(_loadAvatarCo);
    }

    public void FillData()
    {
        if (playerName != null)
            playerName.text = string.IsNullOrEmpty(TokenStore.FullName) ? "(no name)" : TokenStore.FullName;

        // luôn có ảnh ngay
        ApplyIdenticon();

        string avatarUrl = TokenStore.Avatar;
        avatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.Equals("null", StringComparison.OrdinalIgnoreCase))
            avatarUrl = null;

        if (verboseLog)
            Debug.Log($"[PlayerInformationUI] AvatarUrl='{avatarUrl}' Username='{TokenStore.Username}'");

        if (string.IsNullOrEmpty(avatarUrl))
            return;

        if (_loadAvatarCo != null) StopCoroutine(_loadAvatarCo);

        // Nếu là filebase ipfs -> đổi sang dweb.link ngay để khỏi thử deny
        avatarUrl = NormalizeToFastGateway(avatarUrl);

        _loadAvatarCo = StartCoroutine(CoLoadAvatarToImage_WithCache(avatarUrl));
    }

    private void ApplyIdenticon()
    {
        if (playerImage == null) return;

        string seed = !string.IsNullOrEmpty(TokenStore.Username)
            ? TokenStore.Username
            : (!string.IsNullOrEmpty(TokenStore.UserID) ? TokenStore.UserID : "guest");

        Texture2D tex = useIdenticonBackground
            ? IdenticonGenerator.Generate(seed, identiconSize, 0.08f, identiconBackground)
            : IdenticonGenerator.Generate(seed, identiconSize);

        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        playerImage.sprite = sprite;
        playerImage.preserveAspect = true;
    }
    private static string NormalizeToFastGateway(string url)
    {
        url = url.Trim();

        // Nếu đã là dweb.link rồi thì thôi
        if (url.StartsWith("https://dweb.link/ipfs/", StringComparison.OrdinalIgnoreCase))
            return url;

        // Nếu là filebase -> chuyển thẳng sang dweb.link
        const string filebasePrefix = "https://ipfs.filebase.io/ipfs/";
        if (url.StartsWith(filebasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string cid = url.Substring(filebasePrefix.Length);
            return DefaultGoodGateway + cid;
        }

        // ipfs://<cid>
        const string ipfsScheme = "ipfs://";
        if (url.StartsWith(ipfsScheme, StringComparison.OrdinalIgnoreCase))
        {
            string cid = url.Substring(ipfsScheme.Length);
            return DefaultGoodGateway + cid;
        }

        return url;
    }

    private IEnumerator CoLoadAvatarToImage_WithCache(string url)
    {
        if (playerImage == null) yield break;

        // Nếu URL là ipfs gateway dạng .../ipfs/<cid> thì lấy CID để thử gateway cache
        string cid = ExtractCidIfAny(url);
        List<string> candidates = BuildCandidates(url, cid);

        foreach (var u in candidates)
        {
            using (var req = UnityWebRequestTexture.GetTexture(u, true))
            {
                req.timeout = timeoutSeconds;
                req.SetRequestHeader("User-Agent", "Mozilla/5.0");
                req.SetRequestHeader("Accept", "image/*,*/*;q=0.8");

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool ok = req.result == UnityWebRequest.Result.Success;
#else
                bool ok = !req.isNetworkError && !req.isHttpError;
#endif

                if (!ok)
                {
                    if (verboseLog)
                        Debug.LogWarning($"[PlayerInformationUI] Avatar try FAILED: {req.error} code={req.responseCode}\nURL: {u}");
                    continue;
                }

                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null)
                {
                    if (verboseLog)
                        Debug.LogWarning($"[PlayerInformationUI] NULL texture code={req.responseCode}\nURL: {u}");
                    continue;
                }

                // success -> set sprite
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                playerImage.sprite = sprite;
                playerImage.preserveAspect = true;

                if (verboseLog)
                    Debug.Log($"[PlayerInformationUI] Avatar loaded OK from: {u} ({tex.width}x{tex.height})");

                // cache gateway tốt nếu URL là gateway/ipfs/<cid>
                CacheGoodGateway(u, cid);
                yield break;
            }
        }

        if (verboseLog)
            Debug.LogWarning("[PlayerInformationUI] All avatar URLs failed => keep identicon.");
    }

    private static List<string> BuildCandidates(string originalUrl, string cid)
    {
        var list = new List<string>();

        // Ưu tiên gateway đã cache (nếu có CID)
        if (!string.IsNullOrEmpty(cid))
        {
            string cached = PlayerPrefs.GetString(PREF_IPFS_GOOD_GATEWAY, "");
            if (!string.IsNullOrEmpty(cached))
                list.Add(cached + cid);
        }

        // Thử luôn URL hiện tại (đã normalize sang dweb.link nếu filebase)
        list.Add(originalUrl);

        // Nếu có CID -> thêm fallback gateways
        if (!string.IsNullOrEmpty(cid))
        {
            for (int i = 0; i < FallbackGateways.Length; i++)
                list.Add(FallbackGateways[i] + cid);
        }

        return Dedup(list);
    }

    private static void CacheGoodGateway(string usedUrl, string cid)
    {
        if (string.IsNullOrEmpty(cid)) return;

        // Nếu URL dạng "<gateway>/ipfs/<cid>" -> lấy prefix "<gateway>/ipfs/"
        int idx = usedUrl.IndexOf("/ipfs/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;

        string prefix = usedUrl.Substring(0, idx + "/ipfs/".Length);
        PlayerPrefs.SetString(PREF_IPFS_GOOD_GATEWAY, prefix);
        PlayerPrefs.Save();
    }

    private static string ExtractCidIfAny(string url)
    {
        // tìm "/ipfs/<cid>"
        int idx = url.IndexOf("/ipfs/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        string cid = url.Substring(idx + "/ipfs/".Length);
        // cắt query nếu có
        int q = cid.IndexOf("?");
        if (q >= 0) cid = cid.Substring(0, q);
        return string.IsNullOrWhiteSpace(cid) ? null : cid.Trim();
    }

    private static List<string> Dedup(List<string> input)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<string>(input.Count);
        foreach (var s in input)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (set.Add(s)) output.Add(s);
        }
        return output;
    }
}
