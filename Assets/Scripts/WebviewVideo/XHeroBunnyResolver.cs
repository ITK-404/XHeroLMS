using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Resolve-only bridge for Bunny (iframe.mediadelivery.net) embeds.
/// First extracts the real .m3u8/.mp4 stream URL from iframe HTML. Android keeps
/// a hidden WebView resolver only as a fallback, without rendering any overlay
/// and without the OpenGL native-texture path.
/// The resolved URL is then played through Unity VideoPlayer + the local proxy.
/// </summary>
public static class XHeroBunnyResolver
{
    private const string BridgeClass = "com.xherozone.webviewvideo.XHeroNativeTexturePlayer";
    private static readonly Regex SourceUrlRegex = new Regex(
        "<source\\b[^>]*\\bsrc\\s*=\\s*[\"'](?<url>https?://[^\"']+)[\"'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex PlaylistVariableRegex = new Regex(
        "\\b(?:urlPlaylistUrl|source)\\s*=\\s*[\"'](?<url>https?://[^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex PlayableUrlRegex = new Regex(
        "https?://[^\"'<>\\s\\\\]+\\.(?:m3u8|mp4)(?:[^\"'<>\\s\\\\]*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryExtractStreamUrlFromHtml(string html, out string streamUrl)
    {
        streamUrl = null;

        if (string.IsNullOrWhiteSpace(html))
            return false;

        if (TryExtractFromMatchCollection(SourceUrlRegex.Matches(html), out streamUrl))
            return true;

        if (TryExtractFromMatchCollection(PlaylistVariableRegex.Matches(html), out streamUrl))
            return true;

        return TryExtractFromMatchCollection(PlayableUrlRegex.Matches(html), out streamUrl);
    }

    public static bool IsPlayableStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        string lower = url.Trim().ToLowerInvariant();
        if (lower.Contains(".m4s") || lower.Contains(".ts?") || lower.EndsWith(".ts"))
            return false;

        return lower.Contains(".m3u8") || lower.Contains(".mp4");
    }

    private static bool TryExtractFromMatchCollection(MatchCollection matches, out string streamUrl)
    {
        streamUrl = null;

        foreach (Match match in matches)
        {
            string raw = match.Groups["url"].Success ? match.Groups["url"].Value : match.Value;
            string normalized = NormalizeUrl(raw);
            if (!IsPlayableStreamUrl(normalized))
                continue;

            streamUrl = normalized;
            return true;
        }

        return false;
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        string value = WebUtility.HtmlDecode(url.Trim());
        value = value.Replace("\\/", "/");
        return value;
    }

    public static bool StartResolve(string iframeUrl)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass(BridgeClass))
                return jc.CallStatic<bool>("resolveOnly", iframeUrl);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[XHeroBunnyResolver] StartResolve failed: " + e.Message);
            return false;
        }
#else
        return false;
#endif
    }

    public static string GetResolvedUrl()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass(BridgeClass))
                return jc.CallStatic<string>("getResolvedUrl");
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }

    public static string GetError()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass(BridgeClass))
                return jc.CallStatic<string>("getLastError");
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }

    public static void StopResolve()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass(BridgeClass))
                jc.CallStatic("stopResolveOnly");
        }
        catch { }
#endif
    }
}
