using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ReviewCommentUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI nameTmp;
    [SerializeField] private TextMeshProUGUI dateCommentTmp;
    [SerializeField] private TextMeshProUGUI ratingTmp;
    [SerializeField] private TextMeshProUGUI commentTmp;

    [Header("Avatar")]
    [SerializeField] private Image avatarImg;

    [Header("Review Images")]
    [SerializeField] private Transform imageParent;
    [SerializeField] private Image imagePrefab;

    private readonly List<Image> spawnedImages = new();
    private Coroutine avatarCoroutine;

    public void SetComment(
        string name,
        string date,
        string rating,
        string comment,
        string avatarUrl,
        List<string> imageUrls,
        Sprite fallbackAvatar = null)
    {
        if (nameTmp != null) nameTmp.text = string.IsNullOrEmpty(name) ? "Ẩn danh" : name;
        if (dateCommentTmp != null) dateCommentTmp.text = date;
        if (ratingTmp != null) ratingTmp.text = rating;
        if (commentTmp != null) commentTmp.text = comment;

        if (avatarImg != null)
            avatarImg.sprite = fallbackAvatar;

        if (avatarCoroutine != null)
            StopCoroutine(avatarCoroutine);

        if (!string.IsNullOrEmpty(avatarUrl) && avatarImg != null)
            avatarCoroutine = StartCoroutine(LoadAvatar(avatarImg, avatarUrl, fallbackAvatar));

        BuildImages(imageUrls);
    }

    private void BuildImages(List<string> imageUrls)
    {
        ClearImages();

        if (imageParent == null || imagePrefab == null)
            return;

        if (imageUrls == null || imageUrls.Count == 0)
        {
            imageParent.gameObject.SetActive(false);
            return;
        }

        bool hasValidImage = false;

        for (int i = 0; i < imageUrls.Count; i++)
        {
            if (string.IsNullOrEmpty(imageUrls[i]))
                continue;

            hasValidImage = true;

            var img = Instantiate(imagePrefab, imageParent);
            img.gameObject.SetActive(true);
            spawnedImages.Add(img);

            StartCoroutine(LoadImage(img, imageUrls[i]));
        }

        imageParent.gameObject.SetActive(hasValidImage);
    }

    private IEnumerator LoadAvatar(Image target, string url, Sprite fallbackAvatar)
    {
        Debug.Log("[ReviewCommentUI] Start LoadAvatar: " + url);

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError(
                    $"[ReviewCommentUI] Load avatar FAIL\n" +
                    $"url={url}\n" +
                    $"error={req.error}\n" +
                    $"code={req.responseCode}\n" +
                    $"contentType={req.GetResponseHeader("Content-Type")}\n" +
                    $"finalUrl={req.url}"
                );

                if (target != null && fallbackAvatar != null)
                    target.sprite = fallbackAvatar;

                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);

            Debug.Log(
                $"[ReviewCommentUI] Load avatar OK\n" +
                $"url={url}\n" +
                $"tex={(tex != null ? tex.width + "x" + tex.height : "NULL")}"
            );

            if (tex == null || target == null)
            {
                if (target != null && fallbackAvatar != null)
                    target.sprite = fallbackAvatar;
                yield break;
            }

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            target.sprite = sprite;
            target.preserveAspect = true;
            target.SetNativeSize();

            Debug.Log("[ReviewCommentUI] Avatar sprite assigned");
        }
    }

    private IEnumerator LoadImage(Image target, string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[ReviewCommentUI] Load image fail: {url}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);

            if (tex == null || target == null)
                yield break;

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            target.sprite = sprite;
        }
    }

    private void ClearImages()
    {
        for (int i = 0; i < spawnedImages.Count; i++)
        {
            if (spawnedImages[i] != null)
                Destroy(spawnedImages[i].gameObject);
        }

        spawnedImages.Clear();
    }

    public Image GetAvatarImage()
    {
        return avatarImg;
    }
}