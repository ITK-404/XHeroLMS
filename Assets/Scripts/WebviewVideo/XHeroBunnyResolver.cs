using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if UNITY_EDITOR_WIN
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endif

/// <summary>
/// Resolve-only bridge for Bunny (iframe.mediadelivery.net) embeds.
/// First extracts the real .m3u8/.mp4 stream URL from iframe HTML. If static HTML
/// does not expose it, a hidden platform resolver loads the iframe and watches the
/// runtime video/network URLs without rendering a WebView texture.
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

#if UNITY_EDITOR_WIN
    private static EditorBunnyResolverBridge editorBridge;
#endif

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
            Debug.LogWarning("[XHeroBunnyResolver] Android StartResolve failed: " + e.Message);
            return false;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        try
        {
            return XHeroWVResolver_Start(iframeUrl);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[XHeroBunnyResolver] iOS StartResolve failed: " + e.Message);
            return false;
        }
#elif UNITY_EDITOR_WIN
        StopResolve();
        editorBridge = new EditorBunnyResolverBridge();
        if (editorBridge.Start(iframeUrl))
            return true;

        string error = editorBridge.LastError;
        editorBridge = null;
        Debug.LogWarning("[XHeroBunnyResolver] Editor StartResolve failed: " + error);
        return false;
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
#elif UNITY_IOS && !UNITY_EDITOR
        return PtrToString(XHeroWVResolver_GetResolvedUrl());
#elif UNITY_EDITOR_WIN
        return editorBridge != null ? editorBridge.ResolvedUrl : null;
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
#elif UNITY_IOS && !UNITY_EDITOR
        return PtrToString(XHeroWVResolver_GetLastError());
#elif UNITY_EDITOR_WIN
        return editorBridge != null ? editorBridge.LastError : null;
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
#elif UNITY_IOS && !UNITY_EDITOR
        try { XHeroWVResolver_Stop(); } catch { }
#elif UNITY_EDITOR_WIN
        if (editorBridge != null)
        {
            editorBridge.Stop();
            editorBridge = null;
        }
#endif
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

    private static bool TryExtractStreamUrlFromText(string text, out string streamUrl)
    {
        streamUrl = null;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (TryExtractStreamUrlFromHtml(text, out streamUrl))
            return true;

        try
        {
            string decoded = Regex.Unescape(text).Replace("\\/", "/");
            return TryExtractStreamUrlFromHtml(decoded, out streamUrl);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        string value = WebUtility.HtmlDecode(url.Trim());
        value = value.Replace("\\/", "/");
        return value;
    }

#if UNITY_IOS && !UNITY_EDITOR
    private static string PtrToString(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    [DllImport("__Internal")] private static extern bool XHeroWVResolver_Start(string url);
    [DllImport("__Internal")] private static extern void XHeroWVResolver_Stop();
    [DllImport("__Internal")] private static extern IntPtr XHeroWVResolver_GetResolvedUrl();
    [DllImport("__Internal")] private static extern IntPtr XHeroWVResolver_GetLastError();
#endif

#if UNITY_EDITOR_WIN
    private sealed class EditorBunnyResolverBridge
    {
        private const string ResolveScript =
            "(function(){try{" +
            "function clean(u){return (u||'').toString();}" +
            "function ok(u){u=clean(u);var l=u.toLowerCase();return u&&l.indexOf('blob:')!==0&&(l.indexOf('.m3u8')>=0||l.indexOf('.mp4')>=0)&&l.indexOf('.m4s')<0&&l.indexOf('.ts?')<0&&!/\\.ts($|[?#])/.test(l);}" +
            "function pick(u){return ok(u)?u:'';}" +
            "var nodes=document.querySelectorAll('video,source');" +
            "for(var i=0;i<nodes.length;i++){var n=nodes[i];var u=pick(n.currentSrc||n.src||n.getAttribute('src'));if(u)return u;if(n.tagName&&n.tagName.toLowerCase()==='video'){try{n.muted=true;n.playsInline=true;n.autoplay=true;n.play&&n.play().catch(function(){});}catch(e){}}}" +
            "if(window.performance&&performance.getEntriesByType){var rs=performance.getEntriesByType('resource');for(var j=0;j<rs.length;j++){var r=rs[j];var u2=pick(r&&r.name);if(u2)return u2;}}" +
            "return '';" +
            "}catch(e){return 'error:'+String(e&&e.message?e.message:e);}})()";

        private System.Diagnostics.Process chromeProcess;
        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;
        private Task worker;
        private string tempUserDataDir;
        private int commandId;

        public string ResolvedUrl { get; private set; }
        public string LastError { get; private set; } = "";

        public bool Start(string url)
        {
            string chromePath = FindChromeExecutable();
            if (string.IsNullOrWhiteSpace(chromePath))
            {
                LastError = "Windows Editor Bunny resolver requires Chrome/Edge/Chromium executable.";
                return false;
            }

            try
            {
                int port = GetFreeTcpPort();
                tempUserDataDir = Path.Combine(Path.GetTempPath(), "xhero-bunny-resolver-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempUserDataDir);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = chromePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments =
                        $"--remote-debugging-port={port} " +
                        $"--user-data-dir=\"{tempUserDataDir}\" " +
                        "--no-first-run --disable-extensions --disable-background-networking " +
                        "--autoplay-policy=no-user-gesture-required --hide-scrollbars " +
                        "--window-size=960,540 --window-position=-32000,-32000 " +
                        $"\"{url}\""
                };

                chromeProcess = System.Diagnostics.Process.Start(psi);
                cts = new CancellationTokenSource();
                worker = Task.Run(() => RunResolveLoop(port, url, cts.Token));
                return true;
            }
            catch (Exception e)
            {
                LastError = "Windows Editor Bunny resolver start failed: " + e.Message;
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            try { cts?.Cancel(); } catch { }

            try
            {
                webSocket?.Abort();
                webSocket?.Dispose();
            }
            catch { }

            try
            {
                if (chromeProcess != null && !chromeProcess.HasExited)
                    chromeProcess.Kill();
            }
            catch { }

            try { chromeProcess?.Dispose(); } catch { }

            if (!string.IsNullOrWhiteSpace(tempUserDataDir))
            {
                try { Directory.Delete(tempUserDataDir, true); } catch { }
            }

            chromeProcess = null;
            webSocket = null;
            cts = null;
            worker = null;
            tempUserDataDir = null;
        }

        private async Task RunResolveLoop(int port, string url, CancellationToken token)
        {
            try
            {
                string wsUrl = await WaitForPageWebSocketUrl(port, token);
                if (string.IsNullOrWhiteSpace(wsUrl))
                {
                    LastError = "Windows Editor Bunny resolver cannot find Chrome DevTools page target.";
                    return;
                }

                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(wsUrl), token);

                await SendCommand("Page.enable", null, token);
                await SendCommand("Network.enable", null, token);
                await SendCommand("Runtime.enable", null, token);
                await SendCommand("Page.navigate", $"{{\"url\":\"{JsonEscape(url)}\"}}", token);

                await Task.Delay(500, token);

                while (!token.IsCancellationRequested && string.IsNullOrWhiteSpace(ResolvedUrl))
                {
                    await SendCommand(
                        "Runtime.evaluate",
                        $"{{\"expression\":\"{JsonEscape(ResolveScript)}\",\"returnByValue\":true,\"awaitPromise\":false}}",
                        token);

                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                LastError = "Windows Editor Bunny resolver failed: " + e.Message;
            }
        }

        private void TryRememberStream(string text)
        {
            if (!string.IsNullOrWhiteSpace(ResolvedUrl))
                return;

            if (XHeroBunnyResolver.TryExtractStreamUrlFromText(text, out string streamUrl))
            {
                ResolvedUrl = streamUrl;
                LastError = "";
            }
        }

        private async Task<string> SendCommand(string method, string parameters, CancellationToken token)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open)
                return null;

            int id = Interlocked.Increment(ref commandId);
            string json = parameters == null
                ? $"{{\"id\":{id},\"method\":\"{method}\"}}"
                : $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{parameters}}}";

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);

            while (!token.IsCancellationRequested)
            {
                string response = await ReceiveTextMessage(token);
                TryRememberStream(response);

                if (Regex.IsMatch(response, "\"id\"\\s*:\\s*" + id + "(\\D|$)"))
                    return response;
            }

            return null;
        }

        private async Task<string> ReceiveTextMessage(CancellationToken token)
        {
            byte[] buffer = new byte[1024 * 1024];
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new IOException("Chrome DevTools websocket closed.");

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static async Task<string> WaitForPageWebSocketUrl(int port, CancellationToken token)
        {
            string endpoint = "http://127.0.0.1:" + port + "/json";
            for (int i = 0; i < 80 && !token.IsCancellationRequested; i++)
            {
                try
                {
                    string json = await ReadHttpText(endpoint);
                    foreach (Match item in Regex.Matches(json, "\\{[^\\{\\}]*\"type\"\\s*:\\s*\"page\"[^\\{\\}]*\\}"))
                    {
                        string ws = ExtractJsonString(item.Value, "webSocketDebuggerUrl");
                        if (!string.IsNullOrWhiteSpace(ws))
                            return ws;
                    }

                    string fallback = ExtractJsonString(json, "webSocketDebuggerUrl");
                    if (!string.IsNullOrWhiteSpace(fallback))
                        return fallback;
                }
                catch { }

                await Task.Delay(100, token);
            }

            return null;
        }

        private static async Task<string> ReadHttpText(string url)
        {
            var request = WebRequest.CreateHttp(url);
            request.Timeout = 1000;
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindChromeExecutable()
        {
            string envPath = Environment.GetEnvironmentVariable("XHERO_CHROME_PATH");
            if (File.Exists(envPath))
                return envPath;

            string[] directCandidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView", "Application", "msedgewebview2.exe")
            };

            foreach (string candidate in directCandidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            string codeiumDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codeium",
                "ws-browser");

            if (Directory.Exists(codeiumDir))
            {
                try
                {
                    string[] matches = Directory.GetFiles(codeiumDir, "chrome.exe", SearchOption.AllDirectories);
                    if (matches.Length > 0)
                        return matches[0];
                }
                catch { }
            }

            return null;
        }

        private static string JsonEscape(string value)
        {
            if (value == null)
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;

            Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
            if (!match.Success)
                return null;

            return Regex.Unescape(match.Groups[1].Value);
        }
    }
#endif
}
