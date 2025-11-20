using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class CertificateItemUI : MonoBehaviour
{
    [Header("Quad hiển thị ảnh chứng chỉ")]
    [Tooltip("Renderer của tấm Quad (MeshRenderer trên giaykhen).")]
    public Renderer quadRenderer;

    [Header("Text Info")]
    public TMP_Text nameText;       // tên học viên
    public TMP_Text certNameText;   // tên chứng chỉ
    public TMP_Text dateText;       // ngày cấp

    [Header("Optional")]
    public TMP_Text errorText;      // lỗi load ảnh nếu có

    /// <summary>
    /// Hàm controller gọi để gán dữ liệu.
    /// </summary>
    public void Setup(string fullName, string certName, string createdAt, string certImgUrl)
    {
        // ------- TEXT -------
        if (nameText != null)
            nameText.text = fullName ?? "";

        if (certNameText != null)
            certNameText.text = certName ?? "";

        // ------- DATE -------
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

        // ------- IMAGE -> QUAD -------
        if (!string.IsNullOrEmpty(certImgUrl) && quadRenderer != null)
        {
            StartCoroutine(LoadImageIntoQuad(certImgUrl));
        }
    }

    // ===================== LOAD IMAGE TO QUAD =====================
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
                LogError($"[CertificateItemUI] Load image FAIL: {req.responseCode} {req.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                LogError("[CertificateItemUI] Texture rỗng sau khi download.");
                yield break;
            }

            // Gán texture cho material của Quad
            if (quadRenderer != null)
            {
                // .material tạo instance riêng, ổn nếu số lượng không quá lớn
                var mat = quadRenderer.material;
                mat.mainTexture = tex;
            }
        }
    }

    // ===================== DATE PARSER =====================
    private bool TryParseIsoDate(string isoString, out DateTime dtLocal)
    {
        dtLocal = default;
        if (string.IsNullOrEmpty(isoString)) return false;

        // Ví dụ: "2025-11-17T07:11:02.153Z"
        if (DateTime.TryParse(isoString, null, DateTimeStyles.AdjustToUniversal, out var dt))
        {
            dtLocal = dt.ToLocalTime();
            return true;
        }

        return false;
    }

    // ===================== ERROR HANDLER =====================
    private void LogError(string msg)
    {
        Debug.LogError(msg);
        if (errorText != null)
            errorText.text = msg;
    }
}
