using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class CourseImageRuntimeCache
{
    private sealed class Host : MonoBehaviour { }

    private sealed class Subscriber
    {
        public int id;
        public Action<Sprite, string> onDone;
    }

    private sealed class PendingLoad
    {
        public string url;
        public int imageSize;
        public readonly List<Subscriber> subscribers = new();
    }

    private static readonly Dictionary<string, Sprite> s_cache = new();
    private static readonly Dictionary<string, PendingLoad> s_pending = new();

    private static Host s_host;
    private static int s_nextRequestId;

    public static int CachedCount => s_cache.Count;

    public static string NormalizeUrl(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }

    public static bool TryGet(string rawUrl, int imageSize, out Sprite sprite)
    {
        string url = NormalizeUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            sprite = null;
            return false;
        }

        return s_cache.TryGetValue(BuildCacheKey(url, imageSize), out sprite) && sprite != null;
    }

    public static int Request(string rawUrl, int imageSize, Action<Sprite, string> onDone)
    {
        string url = NormalizeUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            onDone?.Invoke(null, "Image url is empty.");
            return 0;
        }

        string key = BuildCacheKey(url, imageSize);
        if (s_cache.TryGetValue(key, out var cachedSprite) && cachedSprite != null)
        {
            onDone?.Invoke(cachedSprite, null);
            return 0;
        }

        int requestId = ++s_nextRequestId;

        if (!s_pending.TryGetValue(key, out var pending))
        {
            pending = new PendingLoad
            {
                url = url,
                imageSize = imageSize
            };

            s_pending[key] = pending;
            EnsureHost().StartCoroutine(DownloadAndPublish(key, pending));
        }

        pending.subscribers.Add(new Subscriber
        {
            id = requestId,
            onDone = onDone
        });

        return requestId;
    }

    public static void Cancel(int requestId)
    {
        if (requestId == 0)
            return;

        foreach (var pending in s_pending.Values)
        {
            for (int i = pending.subscribers.Count - 1; i >= 0; i--)
            {
                if (pending.subscribers[i].id != requestId)
                    continue;

                pending.subscribers.RemoveAt(i);
                return;
            }
        }
    }

    private static IEnumerator DownloadAndPublish(string key, PendingLoad pending)
    {
        Texture2D downloadedTexture = null;
        string error = null;

        yield return DownloadTexture(pending.url, (texture, requestError) =>
        {
            downloadedTexture = texture;
            error = requestError;
        });

        if (downloadedTexture == null && ShouldTryImageProxy(pending.url))
        {
            string proxyUrl = BuildImageProxyUrl(pending.url);
            yield return DownloadTexture(proxyUrl, (texture, requestError) =>
            {
                downloadedTexture = texture;
                error = requestError;
            });
        }

        Sprite sprite = null;
        if (downloadedTexture != null)
        {
            var resizedTexture = downloadedTexture.Resize(pending.imageSize);
            UnityEngine.Object.Destroy(downloadedTexture);

            if (resizedTexture != null)
            {
                sprite = Sprite.Create(
                    resizedTexture,
                    new Rect(0, 0, resizedTexture.width, resizedTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                s_cache[key] = sprite;
            }
            else
            {
                error = "Resize image failed.";
            }
        }

        s_pending.Remove(key);
        Publish(pending, sprite, error);
    }

    private static IEnumerator DownloadTexture(string url, Action<Texture2D, string> onDone)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url, false))
        {
            req.timeout = 20;
            req.SetRequestHeader("Accept", "image/jpeg,image/png,image/*,*/*");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(null, req.error);
                yield break;
            }

            try
            {
                onDone?.Invoke(DownloadHandlerTexture.GetContent(req), null);
            }
            catch (Exception e)
            {
                onDone?.Invoke(null, e.Message);
            }
        }
    }

    private static void Publish(PendingLoad pending, Sprite sprite, string error)
    {
        var subscribers = pending.subscribers.ToArray();
        pending.subscribers.Clear();

        for (int i = 0; i < subscribers.Length; i++)
        {
            try
            {
                subscribers[i].onDone?.Invoke(sprite, error);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private static Host EnsureHost()
    {
        if (s_host != null)
            return s_host;

        var go = new GameObject("~CourseImageRuntimeCache");
        UnityEngine.Object.DontDestroyOnLoad(go);
        s_host = go.AddComponent<Host>();
        return s_host;
    }

    private static string BuildCacheKey(string url, int imageSize)
    {
        return imageSize + "|" + url;
    }

    private static bool ShouldTryImageProxy(string url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
               url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
               url.IndexOf("wsrv.nl", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static string BuildImageProxyUrl(string url)
    {
        string source = (url ?? "").Trim();

        if (source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            source = source.Substring("https://".Length);
        else if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            source = source.Substring("http://".Length);

        return "https://wsrv.nl/?url=" + UnityWebRequest.EscapeURL(source).Replace("%2F", "/") + "&output=jpg";
    }
}
