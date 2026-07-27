#if UNITY_EDITOR_WIN || (UNITY_IOS && !UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class XHeroLocalHlsProxy
{
    private const int DefaultPort = 18080;
    private const int MaxPortProbeCount = 20;
    private const int HeaderReadLimitBytes = 64 * 1024;
    private const int CopyBufferBytes = 128 * 1024;

    private static readonly object Gate = new object();
    private static readonly Regex UriAttributeRegex = new Regex(
        "URI\\s*=\\s*(\"(?<dq>[^\"]*)\"|'(?<sq>[^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static TcpListener listener;
    private static CancellationTokenSource cts;
    private static Task acceptTask;
    private static int activePort;
    private static bool started;

    public static int Port
    {
        get
        {
            lock (Gate)
            {
                return activePort > 0 ? activePort : DefaultPort;
            }
        }
    }

    public static bool EnsureStarted(int preferredPort = DefaultPort)
    {
        lock (Gate)
        {
            if (started && listener != null)
                return true;

            StopLocked();

            int firstPort = preferredPort > 0 ? preferredPort : DefaultPort;
            for (int i = 0; i < MaxPortProbeCount; i++)
            {
                int candidatePort = firstPort + i;

                try
                {
                    listener = new TcpListener(IPAddress.Loopback, candidatePort);
                    listener.Server.NoDelay = true;
                    listener.Start(64);

                    cts = new CancellationTokenSource();
                    activePort = candidatePort;
                    started = true;
                    acceptTask = Task.Run(() => AcceptLoop(cts.Token));

                    Debug.Log("[XHeroLocalHlsProxy] Started http://127.0.0.1:" + activePort);
                    return true;
                }
                catch (Exception e)
                {
                    StopLocked();

                    if (i == MaxPortProbeCount - 1)
                        Debug.LogWarning("[XHeroLocalHlsProxy] Start failed: " + e.Message);
                }
            }

            return false;
        }
    }

    public static string WrapStreamUrl(string originUrl, string referer = null)
    {
        if (string.IsNullOrWhiteSpace(originUrl))
            return originUrl;

        if (!EnsureStarted(Port))
            return originUrl;

        if (IsLocalProxyUrl(originUrl))
            return originUrl;

        string url =
            "http://127.0.0.1:" + Port +
            GetStreamEndpoint(originUrl) +
            "?u=" + Uri.EscapeDataString(originUrl);

        if (!string.IsNullOrWhiteSpace(referer))
            url += "&r=" + Uri.EscapeDataString(referer);

        return url;
    }

    public static void Stop()
    {
        lock (Gate)
        {
            StopLocked();
        }
    }

    private static void StopLocked()
    {
        started = false;

        try { cts?.Cancel(); } catch { }
        try { listener?.Stop(); } catch { }

        listener = null;
        cts = null;
        acceptTask = null;
        activePort = 0;
    }

    private static void AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client = null;

            try
            {
                TcpListener currentListener;
                lock (Gate)
                {
                    currentListener = listener;
                }

                if (currentListener == null)
                    break;

                client = currentListener.AcceptTcpClient();
                client.NoDelay = true;
                Task.Run(() => HandleClient(client, token), token);
            }
            catch
            {
                try { client?.Close(); } catch { }

                if (token.IsCancellationRequested)
                    break;
            }
        }
    }

    private static void HandleClient(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 15000;
                client.SendTimeout = 30000;

                NetworkStream stream = client.GetStream();
                HttpRequest request = ReadHttpRequest(stream);

                if (request == null)
                {
                    WriteTextResponse(stream, 400, "Bad Request", "text/plain", "Bad Request");
                    return;
                }

                if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    WriteHeader(stream, 204, "No Content", "text/plain", 0, null);
                    return;
                }

                if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    WriteTextResponse(stream, 405, "Method Not Allowed", "text/plain", "Method Not Allowed");
                    return;
                }

                Uri localUri = new Uri("http://127.0.0.1" + request.Target);
                if (!localUri.AbsolutePath.StartsWith("/stream", StringComparison.OrdinalIgnoreCase))
                {
                    WriteTextResponse(stream, 404, "Not Found", "text/plain", "Not Found");
                    return;
                }

                Dictionary<string, string> query = ParseQuery(localUri.Query);
                if (!query.TryGetValue("u", out string originUrl) || string.IsNullOrWhiteSpace(originUrl))
                {
                    WriteTextResponse(stream, 400, "Bad Request", "text/plain", "Missing query parameter: u");
                    return;
                }

                query.TryGetValue("r", out string referer);
                ServeOrigin(stream, request, originUrl, referer, token);
            }
            catch (Exception e)
            {
                try
                {
                    WriteTextResponse(client.GetStream(), 500, "Internal Server Error", "text/plain", "Proxy exception: " + e.Message);
                }
                catch { }
            }
        }
    }

    private static HttpRequest ReadHttpRequest(Stream stream)
    {
        byte[] buffer = new byte[4096];
        using (MemoryStream header = new MemoryStream())
        {
            while (header.Length < HeaderReadLimitBytes)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return null;

                header.Write(buffer, 0, read);

                byte[] data = header.ToArray();
                if (IndexOfHeaderEnd(data) >= 0)
                    break;
            }

            string text = Encoding.ASCII.GetString(header.ToArray());
            int headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0)
                return null;

            string[] lines = text.Substring(0, headerEnd).Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0)
                return null;

            string[] first = lines[0].Split(' ');
            if (first.Length < 2)
                return null;

            HttpRequest request = new HttpRequest
            {
                Method = first[0],
                Target = first[1],
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int colon = line.IndexOf(':');
                if (colon <= 0)
                    continue;

                string key = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                request.Headers[key] = value;
            }

            return request;
        }
    }

    private static int IndexOfHeaderEnd(byte[] data)
    {
        for (int i = 3; i < data.Length; i++)
        {
            if (data[i - 3] == '\r' &&
                data[i - 2] == '\n' &&
                data[i - 1] == '\r' &&
                data[i] == '\n')
            {
                return i - 3;
            }
        }

        return -1;
    }

    private static void ServeOrigin(Stream clientStream, HttpRequest clientRequest, string originUrl, string referer, CancellationToken token)
    {
        HttpWebResponse response = null;

        try
        {
            bool playlist = IsLikelyPlaylist(originUrl);
            bool clientHead = string.Equals(clientRequest.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
            HttpWebRequest upstream = (HttpWebRequest)WebRequest.Create(originUrl);
            upstream.Method = playlist || !clientHead ? "GET" : "HEAD";
            upstream.Timeout = 15000;
            upstream.ReadWriteTimeout = 30000;
            upstream.AllowAutoRedirect = true;
            upstream.UserAgent = "Mozilla/5.0 XHeroLMS/LocalHlsProxy";
            upstream.Accept = "*/*";

            if (!string.IsNullOrWhiteSpace(referer))
                upstream.Referer = referer;

            TrySetHeader(upstream, HttpRequestHeader.AcceptEncoding, "identity");

            if (!playlist && clientRequest.Headers.TryGetValue("Range", out string rangeHeader))
                TryApplyRange(upstream, rangeHeader);

            response = (HttpWebResponse)upstream.GetResponse();
            string contentType = response.ContentType ?? "";
            bool responseIsPlaylist = playlist || IsPlaylistContentType(contentType);

            if (responseIsPlaylist)
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    byte[] body = ReadAllBytes(responseStream);
                    string playlistText = Encoding.UTF8.GetString(body);
                    string rewritten = RewriteHlsPlaylist(originUrl, playlistText, referer);
                    byte[] rewrittenBytes = Encoding.UTF8.GetBytes(rewritten);

                    WriteHeader(
                        clientStream,
                        200,
                        "OK",
                        "application/vnd.apple.mpegurl",
                        rewrittenBytes.Length,
                        null);

                    if (!clientHead)
                        clientStream.Write(rewrittenBytes, 0, rewrittenBytes.Length);
                }

                return;
            }

            Dictionary<string, string> extraHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string contentRange = response.Headers["Content-Range"];
            if (!string.IsNullOrWhiteSpace(contentRange))
                extraHeaders["Content-Range"] = contentRange;

            string acceptRanges = response.Headers["Accept-Ranges"];
            extraHeaders["Accept-Ranges"] = !string.IsNullOrWhiteSpace(acceptRanges) ? acceptRanges : "bytes";

            long contentLength = response.ContentLength;
            WriteHeader(
                clientStream,
                (int)response.StatusCode,
                response.StatusDescription,
                GuessContentType(originUrl, contentType),
                contentLength,
                extraHeaders);

            if (clientHead)
                return;

            using (Stream responseStream = response.GetResponseStream())
            {
                CopyStream(responseStream, clientStream, token);
            }
        }
        catch (WebException e)
        {
            response = e.Response as HttpWebResponse;
            string message = "Upstream error: " + e.Message;
            int status = 502;
            string reason = "Bad Gateway";

            if (response != null)
            {
                status = (int)response.StatusCode;
                reason = response.StatusDescription;
            }

            WriteTextResponse(clientStream, status, reason, "text/plain", message);
        }
        finally
        {
            try { response?.Close(); } catch { }
        }
    }

    private static string RewriteHlsPlaylist(string originUrl, string playlist, string referer)
    {
        if (string.IsNullOrEmpty(playlist))
            return playlist;

        string[] lines = playlist.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        StringBuilder output = new StringBuilder(playlist.Length + 1024);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();
            string rewritten = line;

            if (trimmed.Length > 0)
            {
                if (trimmed.StartsWith("#", StringComparison.Ordinal))
                    rewritten = RewriteHlsUriAttributes(originUrl, line, referer);
                else
                    rewritten = ToLocalStreamUrl(ResolveUrl(originUrl, trimmed), referer);
            }

            output.Append(rewritten);

            if (i < lines.Length - 1)
                output.Append('\n');
        }

        return output.ToString();
    }

    private static string RewriteHlsUriAttributes(string originUrl, string line, string referer)
    {
        return UriAttributeRegex.Replace(line, match =>
        {
            string raw = match.Groups["dq"].Success
                ? match.Groups["dq"].Value
                : match.Groups["sq"].Value;

            string quote = match.Groups["dq"].Success ? "\"" : "'";
            string local = ToLocalStreamUrl(ResolveUrl(originUrl, raw), referer);
            return "URI=" + quote + local + quote;
        });
    }

    private static string ToLocalStreamUrl(string absoluteUrl, string referer)
    {
        if (string.IsNullOrWhiteSpace(absoluteUrl))
            return absoluteUrl;

        if (IsLocalProxyUrl(absoluteUrl))
            return absoluteUrl;

        string url =
            "http://127.0.0.1:" + Port +
            GetStreamEndpoint(absoluteUrl) +
            "?u=" + Uri.EscapeDataString(absoluteUrl);

        if (!string.IsNullOrWhiteSpace(referer))
            url += "&r=" + Uri.EscapeDataString(referer);

        return url;
    }

    private static string ResolveUrl(string baseUrl, string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return rawUrl;

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri absolute))
            return absolute.ToString();

        try
        {
            return new Uri(new Uri(baseUrl), rawUrl).ToString();
        }
        catch
        {
            return rawUrl;
        }
    }

    private static string GetStreamEndpoint(string url)
    {
        string lower = StripQueryAndFragment(url).ToLowerInvariant();

        if (lower.Contains(".m3u8"))
            return "/stream.m3u8";

        if (lower.Contains(".m4s"))
            return "/stream.m4s";

        if (lower.Contains(".mp4"))
            return "/stream.mp4";

        if (lower.Contains(".vtt"))
            return "/stream.vtt";

        if (lower.Contains("/key/") || lower.Contains(".key"))
            return "/stream.key";

        if (lower.Contains(".ts") || lower.Contains(".dts"))
            return "/stream.ts";

        return "/stream";
    }

    private static string GuessContentType(string url, string upstreamContentType)
    {
        string lower = StripQueryAndFragment(url).ToLowerInvariant();

        if (lower.Contains(".m3u8"))
            return "application/vnd.apple.mpegurl";

        if (lower.Contains(".ts") || lower.Contains(".dts"))
            return "video/mp2t";

        if (lower.Contains(".m4s"))
            return "video/iso.segment";

        if (lower.Contains(".mp4"))
            return "video/mp4";

        if (lower.Contains(".vtt"))
            return "text/vtt";

        if (lower.Contains("/key/") || lower.Contains(".key"))
            return "application/octet-stream";

        return string.IsNullOrWhiteSpace(upstreamContentType)
            ? "application/octet-stream"
            : upstreamContentType;
    }

    private static bool IsLikelyPlaylist(string url)
    {
        return StripQueryAndFragment(url).IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsPlaylistContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        string lower = contentType.ToLowerInvariant();
        return lower.Contains("mpegurl") || lower.Contains("m3u8") || lower.Contains("vnd.apple");
    }

    private static bool IsLocalProxyUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase) &&
               url.IndexOf("/stream", StringComparison.OrdinalIgnoreCase) >= 0 &&
               url.IndexOf("?u=", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string StripQueryAndFragment(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        int query = url.IndexOfAny(new[] { '?', '#' });
        return query >= 0 ? url.Substring(0, query) : url;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
            return values;

        string trimmed = query[0] == '?' ? query.Substring(1) : query;
        string[] pairs = trimmed.Split('&');

        foreach (string pair in pairs)
        {
            if (string.IsNullOrEmpty(pair))
                continue;

            int equals = pair.IndexOf('=');
            string key = equals >= 0 ? pair.Substring(0, equals) : pair;
            string value = equals >= 0 ? pair.Substring(equals + 1) : "";

            key = Uri.UnescapeDataString(key.Replace("+", " "));
            value = Uri.UnescapeDataString(value.Replace("+", " "));

            values[key] = value;
        }

        return values;
    }

    private static void TrySetHeader(HttpWebRequest request, HttpRequestHeader header, string value)
    {
        try { request.Headers[header] = value; } catch { }
    }

    private static void TryApplyRange(HttpWebRequest request, string rangeHeader)
    {
        if (string.IsNullOrWhiteSpace(rangeHeader))
            return;

        Match match = Regex.Match(rangeHeader.Trim(), "^bytes=(?<start>\\d*)-(?<end>\\d*)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return;

        string startText = match.Groups["start"].Value;
        string endText = match.Groups["end"].Value;

        try
        {
            if (!string.IsNullOrEmpty(startText) && long.TryParse(startText, out long start))
            {
                if (!string.IsNullOrEmpty(endText) && long.TryParse(endText, out long end))
                    request.AddRange(start, end);
                else
                    request.AddRange(start);
            }
        }
        catch { }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream == null)
            return Array.Empty<byte>();

        using (MemoryStream memory = new MemoryStream())
        {
            CopyStream(stream, memory, CancellationToken.None);
            return memory.ToArray();
        }
    }

    private static void CopyStream(Stream input, Stream output, CancellationToken token)
    {
        byte[] buffer = new byte[CopyBufferBytes];

        while (!token.IsCancellationRequested)
        {
            int read = input.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                break;

            output.Write(buffer, 0, read);
        }
    }

    private static void WriteTextResponse(Stream stream, int status, string reason, string contentType, string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body ?? "");
        WriteHeader(stream, status, reason, contentType, bytes.Length, null);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteHeader(Stream stream, int status, string reason, string contentType, long contentLength, Dictionary<string, string> extraHeaders)
    {
        StringBuilder header = new StringBuilder();
        header.Append("HTTP/1.1 ").Append(status).Append(' ').Append(string.IsNullOrWhiteSpace(reason) ? "OK" : reason).Append("\r\n");
        header.Append("Content-Type: ").Append(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType).Append("\r\n");
        header.Append("Connection: close\r\n");
        header.Append("Cache-Control: no-cache\r\n");
        header.Append("Pragma: no-cache\r\n");
        header.Append("Access-Control-Allow-Origin: *\r\n");
        header.Append("Access-Control-Allow-Headers: Range, Origin, Accept, Content-Type\r\n");
        header.Append("Access-Control-Expose-Headers: Content-Length, Content-Range, Accept-Ranges\r\n");

        if (contentLength >= 0)
            header.Append("Content-Length: ").Append(contentLength).Append("\r\n");

        if (extraHeaders != null)
        {
            foreach (KeyValuePair<string, string> item in extraHeaders)
            {
                if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                    header.Append(item.Key).Append(": ").Append(item.Value).Append("\r\n");
            }
        }

        header.Append("\r\n");

        byte[] bytes = Encoding.ASCII.GetBytes(header.ToString());
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed class HttpRequest
    {
        public string Method;
        public string Target;
        public Dictionary<string, string> Headers;
    }
}
#endif
