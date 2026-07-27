#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class XHeroEditorHlsLiveProxy
{
    public enum StreamContainer
    {
        FragmentedMp4,
        MpegTs
    }

    private const int DefaultPort = 18180;
    private const int MaxPortProbeCount = 20;
    private const int HeaderReadLimitBytes = 64 * 1024;
    private const int CopyBufferBytes = 128 * 1024;

    private static readonly object Gate = new object();
    private static readonly List<Process> ActiveProcesses = new List<Process>();

    private static TcpListener listener;
    private static CancellationTokenSource cts;
    private static Task acceptTask;
    private static int activePort;
    private static bool started;
    private static string ffmpegPath;

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

            ffmpegPath = FindFfmpegExecutable();
            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                UnityEngine.Debug.LogWarning("[XHeroEditorHlsLiveProxy] Cannot find ffmpeg.exe. Set XHERO_FFMPEG_PATH or install ffmpeg at C:\\ffmpeg\\ffmpeg.exe.");
                return false;
            }

            StopLocked();

            int firstPort = preferredPort > 0 ? preferredPort : DefaultPort;
            for (int i = 0; i < MaxPortProbeCount; i++)
            {
                int candidatePort = firstPort + i;

                try
                {
                    listener = new TcpListener(IPAddress.Loopback, candidatePort);
                    listener.Server.NoDelay = true;
                    listener.Start(16);

                    cts = new CancellationTokenSource();
                    activePort = candidatePort;
                    started = true;
                    acceptTask = Task.Run(() => AcceptLoop(cts.Token));

                    UnityEngine.Debug.Log("[XHeroEditorHlsLiveProxy] Started http://127.0.0.1:" + activePort + " ffmpeg=" + ffmpegPath);
                    return true;
                }
                catch (Exception e)
                {
                    StopLocked();

                    if (i == MaxPortProbeCount - 1)
                        UnityEngine.Debug.LogWarning("[XHeroEditorHlsLiveProxy] Start failed: " + e.Message);
                }
            }

            return false;
        }
    }

    public static string WrapStreamUrl(string hlsUrl, string referer, StreamContainer container)
    {
        if (string.IsNullOrWhiteSpace(hlsUrl))
            return hlsUrl;

        if (!EnsureStarted(Port))
            return hlsUrl;

        string endpoint = container == StreamContainer.MpegTs ? "/live.ts" : "/live.mp4";
        string format = container == StreamContainer.MpegTs ? "ts" : "fmp4";
        string url =
            "http://127.0.0.1:" + Port +
            endpoint +
            "?fmt=" + format +
            "&u=" + Uri.EscapeDataString(hlsUrl);

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

        foreach (Process process in ActiveProcesses.ToArray())
            KillProcess(process);

        ActiveProcesses.Clear();
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
                    WriteTextResponse(stream, 400, "Bad Request", "Bad Request");
                    return;
                }

                if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    WriteHeader(stream, 204, "No Content", "text/plain", 0);
                    return;
                }

                if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    WriteTextResponse(stream, 405, "Method Not Allowed", "Method Not Allowed");
                    return;
                }

                Uri localUri = CreateLocalUri(request.Target);
                if (localUri == null ||
                    !localUri.AbsolutePath.StartsWith("/live", StringComparison.OrdinalIgnoreCase))
                {
                    WriteTextResponse(stream, 404, "Not Found", "Not Found");
                    return;
                }

                Dictionary<string, string> query = ParseQuery(localUri.Query);
                if (!query.TryGetValue("u", out string hlsUrl) || string.IsNullOrWhiteSpace(hlsUrl))
                {
                    WriteTextResponse(stream, 400, "Bad Request", "Missing query parameter: u");
                    return;
                }

                query.TryGetValue("r", out string referer);
                query.TryGetValue("fmt", out string format);

                StreamContainer container =
                    string.Equals(format, "ts", StringComparison.OrdinalIgnoreCase) ||
                    localUri.AbsolutePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                        ? StreamContainer.MpegTs
                        : StreamContainer.FragmentedMp4;

                ServeLiveStream(stream, request, hlsUrl, referer, container, token);
            }
            catch (Exception e)
            {
                try
                {
                    WriteTextResponse(client.GetStream(), 500, "Internal Server Error", "Proxy exception: " + e.Message);
                }
                catch { }
            }
        }
    }

    private static void ServeLiveStream(Stream clientStream, HttpRequest clientRequest, string hlsUrl, string referer, StreamContainer container, CancellationToken token)
    {
        bool clientHead = string.Equals(clientRequest.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
        string contentType = container == StreamContainer.MpegTs ? "video/mp2t" : "video/mp4";

        if (clientHead)
        {
            WriteHeader(clientStream, 200, "OK", contentType, -1);
            return;
        }

        string localFfmpegPath;
        lock (Gate)
        {
            localFfmpegPath = ffmpegPath;
        }

        if (string.IsNullOrWhiteSpace(localFfmpegPath) || !File.Exists(localFfmpegPath))
        {
            WriteTextResponse(clientStream, 500, "Internal Server Error", "ffmpeg.exe is not available.");
            return;
        }

        Process process = null;
        List<string> lastErrors = new List<string>();
        long streamedBytes = 0L;
        bool clientClosed = false;
        string containerName = container == StreamContainer.MpegTs ? "mpegts" : "fmp4";

        try
        {
            process = new Process();
            process.StartInfo.FileName = localFfmpegPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.Arguments = BuildFfmpegArguments(hlsUrl, referer, container);
            process.ErrorDataReceived += (_, e) => RememberLastLine(lastErrors, e.Data);

            if (!process.Start())
            {
                WriteTextResponse(clientStream, 500, "Internal Server Error", "ffmpeg process did not start.");
                return;
            }

            AddActiveProcess(process);
            process.BeginErrorReadLine();

            UnityEngine.Debug.Log(
                "[XHeroEditorHlsLiveProxy] Start live " + containerName + " stream. " +
                "url=" + hlsUrl + " referer=" + referer
            );

            WriteHeader(clientStream, 200, "OK", contentType, -1);
            streamedBytes = CopyStream(process.StandardOutput.BaseStream, clientStream, token);
        }
        catch (IOException)
        {
            clientClosed = true;
        }
        catch (SocketException)
        {
            clientClosed = true;
        }
        catch (ObjectDisposedException)
        {
            clientClosed = true;
        }
        catch (Exception e)
        {
            try { WriteTextResponse(clientStream, 500, "Internal Server Error", "ffmpeg live proxy exception: " + e.Message); } catch { }
            RememberLastLine(lastErrors, e.Message);
        }
        finally
        {
            if (process != null)
            {
                if (!process.HasExited)
                    KillProcess(process);

                int exitCode = TryGetExitCode(process);
                RemoveActiveProcess(process);

                string tail = JoinLastLines(lastErrors);
                if (!clientClosed && streamedBytes <= 0)
                {
                    UnityEngine.Debug.LogWarning(
                        "[XHeroEditorHlsLiveProxy] Live " + containerName + " ended without bytes. " +
                        "exitCode=" + exitCode + " log=" + tail
                    );
                }
                else
                {
                    UnityEngine.Debug.Log(
                        "[XHeroEditorHlsLiveProxy] Live " + containerName + " stopped. " +
                        "bytes=" + FormatBytes(streamedBytes) +
                        " exitCode=" + exitCode +
                        " clientClosed=" + clientClosed
                    );
                }

                try { process.Dispose(); } catch { }
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

    private static Uri CreateLocalUri(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return null;

        try
        {
            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(target);
            }

            return new Uri("http://127.0.0.1" + target);
        }
        catch
        {
            return null;
        }
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

    private static long CopyStream(Stream input, Stream output, CancellationToken token)
    {
        byte[] buffer = new byte[CopyBufferBytes];
        long copied = 0L;

        while (!token.IsCancellationRequested)
        {
            int read = input.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                break;

            output.Write(buffer, 0, read);
            copied += read;
        }

        return copied;
    }

    private static void WriteTextResponse(Stream stream, int status, string reason, string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body ?? "");
        WriteHeader(stream, status, reason, "text/plain", bytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteHeader(Stream stream, int status, string reason, string contentType, long contentLength)
    {
        StringBuilder header = new StringBuilder();
        header.Append("HTTP/1.1 ").Append(status).Append(' ').Append(string.IsNullOrWhiteSpace(reason) ? "OK" : reason).Append("\r\n");
        header.Append("Content-Type: ").Append(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType).Append("\r\n");
        header.Append("Connection: close\r\n");
        header.Append("Cache-Control: no-store, no-cache, must-revalidate\r\n");
        header.Append("Pragma: no-cache\r\n");
        header.Append("Accept-Ranges: none\r\n");
        header.Append("Access-Control-Allow-Origin: *\r\n");
        header.Append("Access-Control-Allow-Headers: Range, Origin, Accept, Content-Type\r\n");
        header.Append("Access-Control-Expose-Headers: Content-Length, Content-Range, Accept-Ranges\r\n");

        if (contentLength >= 0)
            header.Append("Content-Length: ").Append(contentLength).Append("\r\n");

        header.Append("\r\n");

        byte[] bytes = Encoding.ASCII.GetBytes(header.ToString());
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string BuildFfmpegArguments(string hlsUrl, string referer, StreamContainer container)
    {
        string args =
            "-hide_banner -nostdin -loglevel warning " +
            "-fflags +genpts " +
            "-extension_picky 0 -allowed_extensions ALL " +
            "-headers " + QuoteArg(BuildHeaderArgument(referer)) + " " +
            "-i " + QuoteArg(hlsUrl) + " " +
            "-map 0:v:0? -map 0:a:0? -dn -sn " +
            "-c copy ";

        if (container == StreamContainer.MpegTs)
            return args + "-f mpegts pipe:1";

        return args + "-bsf:a aac_adtstoasc -movflags empty_moov+default_base_moof+frag_keyframe -f mp4 pipe:1";
    }

    private static string BuildHeaderArgument(string referer)
    {
        string headers = "";

        if (!string.IsNullOrWhiteSpace(referer))
            headers += "Referer: " + referer + "\r\n";

        headers += "User-Agent: Mozilla/5.0 XHeroLMS/EditorFfmpegLive\r\n";
        return headers;
    }

    private static string FindFfmpegExecutable()
    {
        string envPath = Environment.GetEnvironmentVariable("XHERO_FFMPEG_PATH");
        if (File.Exists(envPath))
            return envPath;

        string[] directCandidates =
        {
            @"C:\ffmpeg\ffmpeg.exe",
            @"C:\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin", "ffmpeg.exe")
        };

        foreach (string candidate in directCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        string path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (string dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            try
            {
                string candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch { }
        }

        return null;
    }

    private static string QuoteArg(string value)
    {
        if (value == null)
            return "\"\"";

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static void AddActiveProcess(Process process)
    {
        lock (Gate)
        {
            if (process != null && !ActiveProcesses.Contains(process))
                ActiveProcesses.Add(process);
        }
    }

    private static void RemoveActiveProcess(Process process)
    {
        lock (Gate)
        {
            ActiveProcesses.Remove(process);
        }
    }

    private static void KillProcess(Process process)
    {
        if (process == null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill();
        }
        catch { }
    }

    private static int TryGetExitCode(Process process)
    {
        try
        {
            return process != null && process.HasExited ? process.ExitCode : -1;
        }
        catch
        {
            return -1;
        }
    }

    private static void RememberLastLine(List<string> lines, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        lock (lines)
        {
            lines.Add(line.Trim());
            while (lines.Count > 12)
                lines.RemoveAt(0);
        }
    }

    private static string JoinLastLines(List<string> lines)
    {
        lock (lines)
        {
            return string.Join(" | ", lines);
        }
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB) return $"{bytes / (float)GB:F2}GB";
        if (bytes >= MB) return $"{bytes / (float)MB:F2}MB";
        if (bytes >= KB) return $"{bytes / (float)KB:F2}KB";
        return bytes + "B";
    }

    private sealed class HttpRequest
    {
        public string Method;
        public string Target;
        public Dictionary<string, string> Headers;
    }
}
#endif
