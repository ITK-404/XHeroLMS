using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class CertificateItemUI : MonoBehaviour
{
    [Header("Quad hiển thị ảnh chứng chỉ")]
    public Renderer quadRenderer;

    [Header("Text Info")]
    public TMP_Text nameText;
    public TMP_Text certNameText;
    public TMP_Text dateText;

    // Texture đã tải về để preview dùng
    [NonSerialized] public Texture2D loadedTexture;

    public void Setup(string fullName, string certName, string createdAt, string certImgUrl)
    {
        if (nameText != null)      nameText.text = fullName ?? "";
        if (certNameText != null)  certNameText.text = certName ?? "";

        if (TryParseIsoDate(createdAt, out var dtLocal))
        {
            if (dateText != null)
                dateText.text = $"{dtLocal.Day} tháng {dtLocal.Month} năm {dtLocal.Year}";
        }
        else
        {
            if (dateText != null)
                dateText.text = createdAt ?? "";
        }

        // material riêng cho từng instance
        if (quadRenderer != null)
        {
            var baseMat = quadRenderer.sharedMaterial != null
                ? quadRenderer.sharedMaterial
                : quadRenderer.material;

            quadRenderer.material = new Material(baseMat);
        }

        if (!string.IsNullOrEmpty(certImgUrl) && quadRenderer != null)
        {
            StartCoroutine(LoadImageIntoQuad(certImgUrl));
        }
    }

    private IEnumerator LoadImageIntoQuad(string imageUrl)
    {
        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            if (hasErr)
            {
                Debug.Log($"[CertificateItemUI] Load image FAIL: {req.responseCode} {req.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                Debug.Log("[CertificateItemUI] Texture rỗng sau khi download.");
                yield break;
            }

            // cache texture cho preview dùng
            loadedTexture = tex;

            if (quadRenderer != null && quadRenderer.material != null)
            {
                quadRenderer.material.mainTexture = tex;
            }
        }
    }

    private bool TryParseIsoDate(string isoString, out DateTime dtLocal)
    {
        dtLocal = default;
        if (string.IsNullOrEmpty(isoString)) return false;

        if (DateTime.TryParse(isoString, null, DateTimeStyles.AdjustToUniversal, out var dt))
        {
            dtLocal = dt.ToLocalTime();
            return true;
        }

        return false;
    }
}
