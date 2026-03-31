using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoadingTextureTest : MonoBehaviour
{
    [SerializeField] private Image testImg;
    [SerializeField] private string imgUrl;
    private void Awake()
    {
    }

    private Coroutine test;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            test = StartCoroutine(LoadImageTo(testImg, imgUrl));
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (test != null)
            {
                StopCoroutine(test);
            }
        }
    }

    private IEnumerator LoadImageTo(Image target, string url)
    {
        Debug.Log($"[PTS] Start loading image: {url} into {(target?.name ?? "null")}");

        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            var op = req.SendWebRequest();

            while (!op.isDone)
            {
                Debug.Log($"[PTS] Downloading {url} progress: {req.downloadProgress:P1}");
                yield return null;
            }

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[PTS] Load image failed: {url} | {req.error}");
                yield break;
            }

            Debug.Log($"[PTS] Download complete: {url} | downloadedBytes={req.downloadedBytes}");

            var tempText = DownloadHandlerTexture.GetContent(req);
            if (tempText == null)
            {
                Debug.LogWarning($"[PTS] DownloadHandler returned null texture for {url}");
                yield break;
            }

            Debug.Log($"[PTS] Texture downloaded: width={tempText.width} height={tempText.height} format={tempText.format}");

            var tex = tempText.Resize(256);
            Debug.Log($"[PTS] Texture resized: width={tex.width} height={tex.height}");

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            Debug.Log($"[PTS] Sprite created: rect={sprite.rect} pivot={sprite.pivot}");

            target.sprite = sprite;
            Debug.Log($"[PTS] Assigned sprite to {(target?.name ?? "null")}");

            DestroyImmediate(tempText);
            Debug.Log("[PTS] Destroyed temporary texture");
        }
    }
}