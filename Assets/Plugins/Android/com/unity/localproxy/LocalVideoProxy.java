package com.unity.localproxy;

import android.util.Log;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.RandomAccessFile;
import java.lang.reflect.Field;
import java.net.URLDecoder;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.Map;
import java.util.NavigableMap;
import java.util.TreeMap;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;

import fi.iki.elonen.NanoHTTPD;
import okhttp3.Call;
import okhttp3.ConnectionPool;
import okhttp3.OkHttpClient;
import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.ResponseBody;

public class LocalVideoProxy extends NanoHTTPD {

    private static final String TAG = "LocalVideoProxy";

    private static final boolean DEBUG = false;

private static final long MB = 1024L * 1024L;

private static final long DEFAULT_STARTUP_BYTES = 2L * MB;
private static final long DEFAULT_CHUNK_BYTES = 1L * MB;
private static final int DEFAULT_BOOSTER_THREADS = 3;

private static final long STARTUP_WAIT_MS = 8000L;

// Cache miss thì đừng chờ lâu. Player cần mượt thì cho read-through nhanh.
private static final long CACHE_WAIT_BEFORE_READ_THROUGH_MS = 700L;
private static final long BOOSTER_START_AFTER_BYTES = 512L * 1024L;

private static final long BOOTSTRAP_HEAD_LIMIT_BYTES = 16L * MB;
private static final long MIN_TAIL_METADATA_WINDOW_BYTES = 8L * MB;

// Slide sớm hơn, tránh để player đuổi sát cache rồi mới kéo tiếp.
private static final long MIN_WINDOW_SLIDE_BYTES = 8L * MB;

private static final int IO_BUFFER_SIZE = 128 * 1024;

private static final long MIN_MULTI_RANGE_AHEAD_BYTES = 24L * MB;
private static final long MAX_MULTI_RANGE_AHEAD_BYTES = 64L * MB;

// Request lùi nhẹ vài MB của Android decoder không được xem là seek thật.
// Chỉ khi lùi rất xa mới reset playback window.
private static final long BACKWARD_SEEK_RESET_THRESHOLD_BYTES = 32L * MB;

private static final int MIN_BOOSTER_THREADS = 0;
private static final int MAX_BOOSTER_THREADS = 3;

    private static volatile LocalVideoProxy instance;

    private final OkHttpClient client;
    private final ConcurrentHashMap<String, StreamCache> caches = new ConcurrentHashMap<>();
    private final int listenPort;

    private volatile long startupBytes = DEFAULT_STARTUP_BYTES;
    private volatile long chunkBytes = DEFAULT_CHUNK_BYTES;
    private volatile int boosterThreads = DEFAULT_BOOSTER_THREADS;

    private static final AtomicLong REQ_ID = new AtomicLong(0);
    private static final AtomicLong CALL_KEY = new AtomicLong(0);

    private static void I(String s) {
        if (DEBUG) Log.i(TAG, s);
    }

    private static void W(String s) {
        Log.w(TAG, s);
    }

    private static void E(String s, Throwable t) {
        Log.e(TAG, s, t);
    }

    public static String version() {
        return "LocalVideoProxy/FastFrameGuardedAhead-NoCacheStream/2026-07-14";
    }

    public static synchronized boolean startProxy(int port) {
        try {
            if (instance != null) {
                W("[PROXY_START] already started version=" + version());
                return true;
            }

            LocalVideoProxy proxy = new LocalVideoProxy(port);
            proxy.start(SOCKET_READ_TIMEOUT, false);
            instance = proxy;

            W("[PROXY_START] http://127.0.0.1:" + port + " version=" + version());
            return true;

        } catch (Exception e) {
            instance = null;
            E("[PROXY_START] failed", e);
            return false;
        }
    }

    public static synchronized void stopProxy() {
        try {
            if (instance != null) {
                instance.closeAllCaches(true);
                instance.stop();
                instance.client.connectionPool().evictAll();
                instance = null;
                W("[PROXY_STOP]");
            }
        } catch (Exception ignored) {
        }
    }

    public static boolean configure(long startupBytes, int boosterThreadCount, long chunkBytes) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("[CONFIGURE] failed: proxy not started");
                return false;
            }

            p.startupBytes = startupBytes > 0L
                    ? Math.max(256L * 1024L, startupBytes)
                    : DEFAULT_STARTUP_BYTES;

            p.chunkBytes = chunkBytes > 0L
                    ? Math.max(256L * 1024L, chunkBytes)
                    : DEFAULT_CHUNK_BYTES;

            // New default behavior: if old C# still sends 0 booster, keep playback-ahead mode alive.
            // Use configure(..., 1/2/3, ...) to override explicitly.
            int requestedBoosters = boosterThreadCount <= 0
                    ? DEFAULT_BOOSTER_THREADS
                    : boosterThreadCount;

            p.boosterThreads = clamp(requestedBoosters, MIN_BOOSTER_THREADS, MAX_BOOSTER_THREADS);

            W("[CONFIGURE] startup=" + p.startupBytes
                    + " chunk=" + p.chunkBytes
                    + " boosters=" + p.boosterThreads
                    + " requestedBoosters=" + boosterThreadCount
                    + " mode=multi-range-ahead");

            return true;

        } catch (Exception e) {
            E("[CONFIGURE] failed", e);
            return false;
        }
    }

    public static boolean setActiveUrl(String originUrl, boolean deleteOldCaches) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("[SET_ACTIVE] failed: proxy not started");
                return false;
            }

            p.releaseAllExceptInternal(originUrl, deleteOldCaches);
            return true;

        } catch (Exception e) {
            E("[SET_ACTIVE] failed", e);
            return false;
        }
    }

    public static boolean release(String originUrl, boolean deleteFile) {
        try {
            LocalVideoProxy p = instance;

            if (p == null || originUrl == null || originUrl.length() == 0) {
                return false;
            }

            return p.releaseInternal(originUrl, deleteFile);

        } catch (Exception e) {
            E("[RELEASE] failed", e);
            return false;
        }
    }

    public static int releaseAllExcept(String keepOriginUrl, boolean deleteFile) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return 0;
            }

            return p.releaseAllExceptInternal(keepOriginUrl, deleteFile);

        } catch (Exception e) {
            E("[RELEASE_ALL_EXCEPT] failed", e);
            return 0;
        }
    }

    public static boolean onNetworkChanged() {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return false;
            }

            p.client.connectionPool().evictAll();

            int count = 0;

            for (StreamCache cache : p.caches.values()) {
                if (cache != null) {
                    cache.restartAfterNetworkChanged();
                    count++;
                }
            }

            W("[NETWORK_CHANGED] evictAll + restart caches=" + count);
            return true;

        } catch (Exception e) {
            E("[NETWORK_CHANGED] failed", e);
            return false;
        }
    }

    public static int clearDiskCache() {
        try {
            LocalVideoProxy p = instance;

            if (p != null) {
                p.closeAllCaches(true);
                p.client.connectionPool().evictAll();
            }

            File dir = getProxyCacheDir();
            int count = 0;
            File[] files = dir.listFiles();

            if (files != null) {
                for (File f : files) {
                    if (f != null && f.isFile() && f.delete()) {
                        count++;
                    }
                }
            }

            W("[CLEAR_DISK_CACHE] deleted=" + count);
            return count;

        } catch (Exception e) {
            E("[CLEAR_DISK_CACHE] failed", e);
            return 0;
        }
    }

    public static boolean preload(String originUrl, long start) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("[PRELOAD] failed: proxy not started");
                return false;
            }

            StreamCache cache = p.getCache(originUrl);
            cache.ensureWindow(start);

            I("[PRELOAD] url=" + originUrl + " start=" + start);
            return true;

        } catch (Exception e) {
            E("[PRELOAD] failed", e);
            return false;
        }
    }

    public static boolean preloadRange(String originUrl, long start, long length) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("[PRELOAD_RANGE] failed: proxy not started");
                return false;
            }

            StreamCache cache = p.getCache(originUrl);
            cache.preloadRange(start, length);

            I("[PRELOAD_RANGE] url=" + originUrl + " start=" + start + " length=" + length);
            return true;

        } catch (Exception e) {
            E("[PRELOAD_RANGE] failed", e);
            return false;
        }
    }

    public static long getCachedUntil(String originUrl) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getContiguousUntil();

        } catch (Exception e) {
            return -1L;
        }
    }

    public static long getTotalBytes(String originUrl) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getTotal();

        } catch (Exception e) {
            return -1L;
        }
    }

    public static long getCacheFileBytes(String originUrl) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getFileLength();

        } catch (Exception e) {
            return -1L;
        }
    }

    public static long getCachedBytes(String originUrl) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getCachedBytes();

        } catch (Exception e) {
            return -1L;
        }
    }

    public static long getCachedUntilFrom(String originUrl, long start) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getCachedUntilFrom(start);

        } catch (Exception e) {
            return -1L;
        }
    }

    private LocalVideoProxy(int port) {
        super(port);

        listenPort = port;

        client = new OkHttpClient.Builder()
                .connectTimeout(12, TimeUnit.SECONDS)
                .readTimeout(0, TimeUnit.SECONDS)
                .writeTimeout(30, TimeUnit.SECONDS)
                .retryOnConnectionFailure(true)
                .followRedirects(true)
                .protocols(Arrays.asList(Protocol.HTTP_1_1))
                .connectionPool(new ConnectionPool(8, 30, TimeUnit.SECONDS))
                .build();
    }

    @Override
    public NanoHTTPD.Response serve(IHTTPSession session) {
        final long id = REQ_ID.incrementAndGet();

        try {
            String uri = session.getUri();

            if ("/ping".equals(uri)) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.OK,
                        "text/plain",
                        "pong"
                );
            }

            if (!"/video".equals(uri) && !"/warm".equals(uri) && !"/stream".equals(uri)) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.NOT_FOUND,
                        "text/plain",
                        "Not Found"
                );
            }

            String qs = session.getQueryParameterString();
            String enc = getParam(qs, "u");

            if (enc == null || enc.length() == 0) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.BAD_REQUEST,
                        "text/plain",
                        "Missing param: u"
                );
            }

            String originUrl = URLDecoder.decode(enc, "UTF-8");
            long requestStart = 0L;

            if ("/stream".equals(uri)) {
                return serveNoCacheStream(id, session, originUrl);
            }

            if ("/warm".equals(uri)) {
                String s = getParam(qs, "s");

                if (s != null && s.length() > 0) {
                    try {
                        requestStart = Long.parseLong(s);
                    } catch (Exception ignored) {
                    }
                }

                StreamCache cache = getCache(originUrl);
                cache.ensureWindow(requestStart);

                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.OK,
                        "text/plain",
                        "warming"
                );
            }

            Map<String, String> headers = session.getHeaders();
            String clientRange = headers.get("range");

            if (clientRange == null || clientRange.trim().length() == 0) {
                clientRange = "bytes=0-";
            }

            RangeSpec clientSpec = RangeSpec.parse(clientRange);

            if (clientSpec == null) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.RANGE_NOT_SATISFIABLE,
                        "text/plain",
                        "Bad Range"
                );
            }

            StreamCache cache = getCache(originUrl);

long preferredStart = clientSpec.isSuffix ? 0L : Math.max(0L, clientSpec.start);
long knownTotal = cache.getTotal();
long bootstrapStart = preferredStart;

// Nếu chưa biết total thì cần bootstrap để lấy Content-Range / total.
// Nhưng nếu đã biết total rồi thì suffix/tail metadata KHÔNG được kéo playback window về 0.
if (knownTotal <= 0) {
    if (clientSpec.isSuffix || preferredStart > BOOTSTRAP_HEAD_LIMIT_BYTES) {
        bootstrapStart = 0L;
    }

    cache.ensureWindow(bootstrapStart);
} else if (!clientSpec.isSuffix && !isTailMetadataRange(preferredStart, -1L, knownTotal)) {
    cache.ensureWindow(preferredStart);
}

long total = cache.waitForTotal(STARTUP_WAIT_MS);

            if (total <= 0) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.SERVICE_UNAVAILABLE,
                        "text/plain",
                        "Proxy could not determine total video size"
                );
            }

            long outStart;
            long outEnd;

            if (clientSpec.isSuffix) {
                long suffixLen = Math.min(clientSpec.suffixLength, total);
                outStart = Math.max(0L, total - suffixLen);
                outEnd = total - 1L;
            } else if (clientSpec.hasEnd) {
                outStart = clientSpec.start;
                outEnd = Math.min(clientSpec.endInclusive, total - 1);
            } else {
                outStart = clientSpec.start;
                outEnd = total - 1;
            }

            requestStart = outStart;

            if (outStart < 0 || outStart >= total || outEnd < outStart) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.RANGE_NOT_SATISFIABLE,
                        "text/plain",
                        "Range out of file"
                );
            }

            long outLen = outEnd - outStart + 1;
            boolean drivesPlaybackWindow = !clientSpec.isSuffix && !isTailMetadataRange(outStart, outLen, total);

            if (drivesPlaybackWindow) {
                cache.ensureWindow(outStart);
            } else {
                cache.preloadRange(outStart, Math.min(outLen, Math.max(chunkBytes, startupBytes)));
            }

            RangeCacheInputStream in = new RangeCacheInputStream(
                    id,
                    cache,
                    client,
                    originUrl,
                    outStart,
                    outEnd,
                    drivesPlaybackWindow
            );

            NanoHTTPD.Response resp = newFixedLengthResponse(
                    NanoHTTPD.Response.Status.PARTIAL_CONTENT,
                    cache.getContentType(),
                    in,
                    outLen
            );

            resp.addHeader("Content-Length", String.valueOf(outLen));
            resp.addHeader("Content-Range", "bytes " + outStart + "-" + outEnd + "/" + total);
            resp.addHeader("Accept-Ranges", "bytes");
            resp.addHeader("Cache-Control", "no-cache");
            resp.addHeader("Pragma", "no-cache");

            return resp;

        } catch (Exception e) {
            String msg = e.getMessage();

            if (msg != null && msg.toLowerCase().contains("broken pipe")) {
                W("#" + id + " client closed socket: broken pipe");
            } else {
                E("#" + id + " serve exception", e);
            }

            return newFixedLengthResponse(
                    NanoHTTPD.Response.Status.INTERNAL_ERROR,
                    "text/plain",
                    "Proxy exception: " + (msg != null ? msg : e.toString())
            );
        }
    }

    private NanoHTTPD.Response serveNoCacheStream(long id, IHTTPSession session, String originUrl) {
        okhttp3.Response upstream = null;

        try {
            Map<String, String> headers = session.getHeaders();
            String clientRange = headers.get("range");
            boolean likelyPlaylist = isLikelyHlsPlaylistUrl(originUrl);

            Request.Builder builder = new Request.Builder()
                    .url(originUrl)
                    .get()
                    .header("Accept", "*/*")
                    .header("Accept-Encoding", "identity")
                    .header("User-Agent", "UnityLocalProxy/NoCacheStream/1.0");

            if (!likelyPlaylist && clientRange != null && clientRange.trim().length() > 0) {
                builder.header("Range", clientRange);
            }

            upstream = client.newCall(builder.build()).execute();
            ResponseBody body = upstream.body();

            if (body == null) {
                closeQuietly(upstream);
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.INTERNAL_ERROR,
                        "text/plain",
                        "Upstream body is null"
                );
            }

            int code = upstream.code();

            if (code < 200 || code >= 300) {
                String err = "";

                try {
                    err = body.string();
                } catch (Exception ignored) {
                }

                closeQuietly(upstream);
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.INTERNAL_ERROR,
                        "text/plain",
                        "Upstream error " + code + " " + err
                );
            }

            String contentType = safeHeader(upstream, "Content-Type", "application/octet-stream");

            if (likelyPlaylist || isHlsPlaylistContent(contentType)) {
                String playlist = body.string();
                closeQuietly(upstream);

                NanoHTTPD.Response resp = newFixedLengthResponse(
                        NanoHTTPD.Response.Status.OK,
                        "application/vnd.apple.mpegurl",
                        rewriteHlsPlaylist(session, originUrl, playlist)
                );

                resp.addHeader("Cache-Control", "no-cache");
                resp.addHeader("Pragma", "no-cache");
                return resp;
            }

            long length = body.contentLength();
            NanoHTTPD.Response.Status status = code == 206
                    ? NanoHTTPD.Response.Status.PARTIAL_CONTENT
                    : NanoHTTPD.Response.Status.OK;

            NanoHTTPD.Response resp = length >= 0
                    ? newFixedLengthResponse(status, contentType, new ResponseBodyInputStream(upstream, body), length)
                    : newChunkedResponse(status, contentType, new ResponseBodyInputStream(upstream, body));

            String contentRange = upstream.header("Content-Range");
            if (contentRange != null && contentRange.length() > 0) {
                resp.addHeader("Content-Range", contentRange);
            }

            String acceptRanges = upstream.header("Accept-Ranges");
            resp.addHeader("Accept-Ranges", acceptRanges != null && acceptRanges.length() > 0 ? acceptRanges : "bytes");
            resp.addHeader("Cache-Control", "no-cache");
            resp.addHeader("Pragma", "no-cache");

            if (length >= 0) {
                resp.addHeader("Content-Length", String.valueOf(length));
            }

            return resp;

        } catch (Exception e) {
            closeQuietly(upstream);
            E("#" + id + " no-cache stream exception", e);

            return newFixedLengthResponse(
                    NanoHTTPD.Response.Status.INTERNAL_ERROR,
                    "text/plain",
                    "No-cache stream exception: " + e
            );
        }
    }

    private String rewriteHlsPlaylist(IHTTPSession session, String originUrl, String playlist) {
        if (playlist == null || playlist.length() == 0) {
            return playlist;
        }

        String[] lines = playlist.split("\\r?\\n", -1);
        StringBuilder out = new StringBuilder(playlist.length() + 256);

        for (int i = 0; i < lines.length; i++) {
            String line = lines[i];
            String trimmed = line.trim();
            String rewritten = line;

            if (trimmed.length() == 0) {
                rewritten = line;
            } else if (trimmed.startsWith("#")) {
                rewritten = rewriteHlsUriAttributes(session, originUrl, line);
            } else {
                rewritten = toLocalStreamUrl(session, resolveUrl(originUrl, trimmed));
            }

            out.append(rewritten);

            if (i < lines.length - 1) {
                out.append('\n');
            }
        }

        return out.toString();
    }

    private String rewriteHlsUriAttributes(IHTTPSession session, String originUrl, String line) {
        String marker = "URI=\"";
        int search = 0;
        StringBuilder out = null;

        while (true) {
            int start = line.indexOf(marker, search);

            if (start < 0) {
                break;
            }

            int valueStart = start + marker.length();
            int valueEnd = line.indexOf('"', valueStart);

            if (valueEnd < 0) {
                break;
            }

            String rawUrl = line.substring(valueStart, valueEnd);
            String localUrl = toLocalStreamUrl(session, resolveUrl(originUrl, rawUrl));

            if (out == null) {
                out = new StringBuilder(line.length() + 128);
                out.append(line, 0, valueStart);
            } else {
                out.append(line, search, valueStart);
            }

            out.append(localUrl);
            search = valueEnd;
        }

        if (out == null) {
            return line;
        }

        out.append(line.substring(search));
        return out.toString();
    }

    private String toLocalStreamUrl(IHTTPSession session, String absoluteUrl) {
        if (absoluteUrl == null || absoluteUrl.length() == 0) {
            return absoluteUrl;
        }

        if (absoluteUrl.startsWith("http://127.0.0.1:") && absoluteUrl.contains("/stream?u=")) {
            return absoluteUrl;
        }

        String host = null;

        try {
            host = session.getHeaders().get("host");
        } catch (Exception ignored) {
        }

        if (host == null || host.length() == 0) {
            host = "127.0.0.1:" + listenPort;
        }

        try {
            return "http://" + host + "/stream?u=" + java.net.URLEncoder.encode(absoluteUrl, "UTF-8");
        } catch (Exception e) {
            return absoluteUrl;
        }
    }

    private static boolean isLikelyHlsPlaylistUrl(String url) {
        if (url == null) {
            return false;
        }

        String lower = url.toLowerCase();
        return lower.contains(".m3u8");
    }

    private static boolean isHlsPlaylistContent(String contentType) {
        if (contentType == null) {
            return false;
        }

        String lower = contentType.toLowerCase();
        return lower.contains("mpegurl") ||
                lower.contains("m3u8") ||
                lower.contains("x-mpegurl") ||
                lower.contains("vnd.apple");
    }

    private static String resolveUrl(String baseUrl, String rawUrl) {
        if (rawUrl == null || rawUrl.length() == 0) {
            return rawUrl;
        }

        String lower = rawUrl.toLowerCase();

        if (lower.startsWith("http://") ||
                lower.startsWith("https://") ||
                lower.startsWith("data:")) {
            return rawUrl;
        }

        try {
            return new java.net.URL(new java.net.URL(baseUrl), rawUrl).toString();
        } catch (Exception e) {
            return rawUrl;
        }
    }

    private StreamCache getCache(String originUrl) throws IOException {
        String key = sha1(originUrl);
        StreamCache cache = caches.get(key);

        if (cache != null) {
            return cache;
        }

        synchronized (caches) {
            cache = caches.get(key);

            if (cache == null) {
                File dir = getProxyCacheDir();
                File file = new File(dir, key + ".bin");

                cache = new StreamCache(
                        client,
                        originUrl,
                        file,
                        startupBytes,
                        boosterThreads,
                        chunkBytes
                );

                caches.put(key, cache);
            }

            return cache;
        }
    }

    private boolean releaseInternal(String originUrl, boolean deleteFile) {
        String key = sha1(originUrl);
        StreamCache cache = caches.remove(key);

        if (cache == null) {
            W("[RELEASE] cache not found url=" + originUrl);
            return false;
        }

        cache.close(deleteFile);
        client.connectionPool().evictAll();

        W("[RELEASE] url=" + originUrl
                + " deleteFile=" + deleteFile
                + " cacheLeft=" + caches.size());

        return true;
    }

    private int releaseAllExceptInternal(String keepOriginUrl, boolean deleteFile) {
        String keepKey = keepOriginUrl == null || keepOriginUrl.length() == 0 ? null : sha1(keepOriginUrl);
        int count = 0;

        for (Map.Entry<String, StreamCache> entry : caches.entrySet()) {
            String key = entry.getKey();

            if (keepKey != null && keepKey.equals(key)) {
                continue;
            }

            StreamCache cache = caches.remove(key);

            if (cache != null) {
                cache.close(deleteFile);
                count++;
            }
        }

        if (count > 0) {
            client.connectionPool().evictAll();
        }

        W("[RELEASE_ALL_EXCEPT] keep=" + keepOriginUrl
                + " released=" + count
                + " deleteFile=" + deleteFile
                + " cacheLeft=" + caches.size());

        return count;
    }

    private void closeAllCaches(boolean deleteFile) {
        try {
            for (StreamCache c : caches.values()) {
                c.close(deleteFile);
            }

            caches.clear();

        } catch (Exception ignored) {
        }
    }

    private static File getProxyCacheDir() throws IOException {
        File base = null;

        try {
            Class<?> unityPlayer = Class.forName("com.unity3d.player.UnityPlayer");
            Field activityField = unityPlayer.getField("currentActivity");
            Object activity = activityField.get(null);

            if (activity != null) {
                java.lang.reflect.Method getCacheDir = activity.getClass().getMethod("getCacheDir");
                Object result = getCacheDir.invoke(activity);

                if (result instanceof File) {
                    base = (File) result;
                }
            }
        } catch (Exception ignored) {
        }

        if (base == null) {
            String tmp = System.getProperty("java.io.tmpdir");

            if (tmp != null && tmp.length() > 0) {
                base = new File(tmp);
            } else {
                base = new File("/data/local/tmp");
            }
        }

        File dir = new File(base, "local_video_proxy_cache");

        if (!dir.exists() && !dir.mkdirs()) {
            throw new IOException("Cannot create proxy cache dir: " + dir.getAbsolutePath());
        }

        return dir;
    }

    private static class StreamCache {
        private final Object lock = new Object();

        private final OkHttpClient client;
        private final String url;
        private final File file;
        private final RandomAccessFile raf;

        private final long startupBytes;
        private final int boosterThreads;
        private final long chunkBytes;
        private final long cacheAheadBytes;

        private final NavigableMap<Long, RangeState> ranges = new TreeMap<>();
        private final ConcurrentHashMap<Long, Call> calls = new ConcurrentHashMap<>();

        private long windowStart = -1L;
        private long contiguousUntil = -1L;
        private long nextReserve = -1L;
        private long total = -1L;
        private long windowSerial = 0L;

        private String contentType = "video/mp4";

        private boolean workersRunning = false;
        private int activeWorkers = 0;
        private boolean closed = false;
        private long generation = 0L;

        StreamCache(
                OkHttpClient client,
                String url,
                File file,
                long startupBytes,
                int boosterThreads,
                long chunkBytes
        ) throws IOException {
            this.client = client;
            this.url = url;
            this.file = file;
            this.startupBytes = Math.max(256L * 1024L, startupBytes);
            this.boosterThreads = clamp(boosterThreads, MIN_BOOSTER_THREADS, MAX_BOOSTER_THREADS);
            this.chunkBytes = Math.max(256L * 1024L, chunkBytes);
            long desiredAhead = this.startupBytes
                    + (this.chunkBytes * Math.max(1, this.boosterThreads));

            long minimumAhead = Math.max(
                    MIN_MULTI_RANGE_AHEAD_BYTES,
                    this.startupBytes + this.chunkBytes
            );

            this.cacheAheadBytes = clampLong(
                    Math.max(desiredAhead, minimumAhead),
                    minimumAhead,
                    MAX_MULTI_RANGE_AHEAD_BYTES
            );
            this.raf = new RandomAccessFile(file, "rw");
        }

        void ensureWindow(long start) throws IOException {
            synchronized (lock) {
                if (closed) {
                    throw new IOException("Cache already closed");
                }

                long s = Math.max(0L, start);

                if (total > 0 && s >= total) {
                    s = Math.max(0L, total - 1L);
                }

                if (windowStart < 0) {
                    resetWindowLocked(s);
                } else {
                    long cachedEnd = getCachedEndFromLocked(windowStart);

                    if (cachedEnd > contiguousUntil) {
                        contiguousUntil = cachedEnd;
                    }

                    long windowLimit = windowStart + cacheAheadBytes;

                    if (total > 0) {
                        windowLimit = Math.min(windowLimit, total);
                    }

long slideDistance = Math.max(MIN_WINDOW_SLIDE_BYTES, cacheAheadBytes / 2L);
long remainingWindow = windowLimit - s;

boolean beforeWindow = s + Math.max(chunkBytes, startupBytes) < windowStart;

// Đây là fix chính:
// Android VideoPlayer có thể request lùi nhẹ vài MB để đọc sample/index.
// Không được xem mấy request đó là playback chính.
// Nếu reset window theo nó thì cache bị kéo 8MB -> 5MB -> 8MB như log của master.
boolean isRealBackwardSeek = beforeWindow
        && (windowStart - s) >= BACKWARD_SEEK_RESET_THRESHOLD_BYTES;

boolean beyondWindow = s >= windowLimit;
boolean shouldSlideForward = s > windowStart
        && (s - windowStart >= slideDistance || remainingWindow <= cacheAheadBytes / 3L);

if (isRealBackwardSeek || beyondWindow) {
    resetWindowLocked(s);
} else if (shouldSlideForward) {
    slideWindowForwardLocked(s);
} else if (beforeWindow) {
    W("[CACHE_WINDOW] ignore backward/random range url=" + url
            + " requestStart=" + s
            + " windowStart=" + windowStart
            + " delta=" + (windowStart - s)
            + " cachedUntil=" + contiguousUntil);
}
                }

                startWorkersLocked();
                lock.notifyAll();
            }
        }

        void preloadRange(long start, long length) throws IOException {
            if (length <= 0) {
                return;
            }

            final ChunkPlan chunk;
            final long myGen;

            synchronized (lock) {
                if (closed) {
                    throw new IOException("Cache already closed");
                }

                long s = Math.max(0L, start);
                long e = s + length - 1L;

                if (total > 0) {
                    if (s >= total) {
                        return;
                    }

                    e = Math.min(e, total - 1L);
                }

                if (getCachedEndFromLocked(s) > e) {
                    return;
                }

                chunk = new ChunkPlan(s, e, -1L);
                myGen = generation;
            }

            Thread t = new Thread(new Runnable() {
                @Override
                public void run() {
                    downloadChunk(myGen, chunk, 90);
                }
            }, "LocalVideoProxy-RangeWarm");

            t.setDaemon(true);
            t.start();
        }

        long waitForTotal(long timeoutMs) throws IOException {
            long deadline = System.currentTimeMillis() + timeoutMs;

            synchronized (lock) {
                while (!closed && total <= 0) {
                    startWorkersLocked();

                    long wait = deadline - System.currentTimeMillis();

                    if (wait <= 0) {
                        return total;
                    }

                    try {
                        lock.wait(Math.min(wait, 250L));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        throw new IOException("Interrupted while waiting total", e);
                    }
                }

                return total;
            }
        }

        int readCached(long absolutePos, byte[] buffer, int offset, int length) throws IOException {
            synchronized (lock) {
                if (closed) {
                    return -1;
                }

                RangeState r = findRangeContainingLocked(absolutePos);

                if (r == null) {
                    return 0;
                }

                int available = (int) Math.min(
                        (long) length,
                        r.endExclusive - absolutePos
                );

                if (available <= 0) {
                    return 0;
                }

                r.lastAccessMs = System.currentTimeMillis();

                raf.seek(absolutePos);
                return raf.read(buffer, offset, available);
            }
        }

        int readCachedOrWait(
                long absolutePos,
                byte[] buffer,
                int offset,
                int length,
                long timeoutMs
        ) throws IOException {
            long deadline = System.currentTimeMillis() + Math.max(0L, timeoutMs);

            synchronized (lock) {
                while (true) {
                    if (closed) {
                        return -1;
                    }

                    RangeState r = findRangeContainingLocked(absolutePos);

                    if (r != null) {
                        int available = (int) Math.min(
                                (long) length,
                                r.endExclusive - absolutePos
                        );

                        if (available > 0) {
                            r.lastAccessMs = System.currentTimeMillis();
                            raf.seek(absolutePos);
                            return raf.read(buffer, offset, available);
                        }
                    }

                    long wait = deadline - System.currentTimeMillis();

                    if (wait <= 0L) {
                        return 0;
                    }

                    try {
                        lock.wait(Math.min(wait, 50L));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        throw new IOException("Interrupted while waiting cached bytes", e);
                    }
                }
            }
        }

        void writeCache(long absolutePos, byte[] buffer, int offset, int length) throws IOException {
            if (length <= 0) {
                return;
            }

            synchronized (lock) {
                if (closed) {
                    throw new IOException("Cache closed");
                }

                raf.seek(absolutePos);
                raf.write(buffer, offset, length);

                markRangeLocked(absolutePos, absolutePos + length);

                lock.notifyAll();
            }
        }

        void updateResponseInfo(okhttp3.Response resp, int code, long requestStart, long bodyLen) {
            synchronized (lock) {
                String ct = safeHeader(resp, "Content-Type", "video/mp4");
                String cr = resp.header("Content-Range");
                ContentRange parsed = ContentRange.parse(cr);

                if (ct != null && ct.length() > 0) {
                    contentType = ct;
                }

                if (parsed != null && parsed.total > 0) {
                    total = parsed.total;
                } else if (code == 200 && bodyLen > 0 && requestStart == 0) {
                    total = bodyLen;
                }

                lock.notifyAll();
            }
        }

        long registerCall(Call call) {
            long key = -CALL_KEY.incrementAndGet();
            calls.put(key, call);
            return key;
        }

        void unregisterCall(long key, Call call) {
            calls.remove(key, call);
        }

        void restartAfterNetworkChanged() {
            synchronized (lock) {
                if (closed) {
                    return;
                }

                generation++;

                for (Call call : calls.values()) {
                    try {
                        call.cancel();
                    } catch (Exception ignored) {
                    }
                }

                calls.clear();
                workersRunning = false;
                activeWorkers = 0;

                if (windowStart >= 0) {
                    resetWindowLocked(windowStart);
                }

                startWorkersLocked();

                W("[CACHE_NETWORK_RESTART] url=" + url
                        + " windowStart=" + windowStart
                        + " contiguousUntil=" + contiguousUntil
                        + " cachedBytes=" + getCachedBytesLocked());

                lock.notifyAll();
            }
        }

        long getContiguousUntil() {
            synchronized (lock) {
                return contiguousUntil;
            }
        }

        long getTotal() {
            synchronized (lock) {
                return total;
            }
        }

        long getFileLength() {
            try {
                synchronized (lock) {
                    return raf.length();
                }
            } catch (Exception e) {
                return -1L;
            }
        }

        long getCachedBytes() {
            synchronized (lock) {
                return getCachedBytesLocked();
            }
        }

        long getCachedUntilFrom(long start) {
            synchronized (lock) {
                return getCachedEndFromLocked(Math.max(0L, start));
            }
        }

        String getContentType() {
            synchronized (lock) {
                return contentType;
            }
        }

private void startWorkersLocked() {
    if (closed || workersRunning) {
        return;
    }

    if (!hasPendingWindowWorkLocked()) {
        return;
    }

    // Nếu window hiện tại đã full tới aheadTarget thì không cần tạo worker mới.
    // Trước đó có thể bị spam start/idle liên tục khi VideoPlayer mở nhiều range.
    workersRunning = true;
    activeWorkers = 1 + boosterThreads;
    final long myGen = generation;

    W("[MULTI_RANGE_WORKERS] start url=" + url
            + " startup=" + startupBytes
            + " chunk=" + chunkBytes
            + " boosters=" + boosterThreads
            + " activeWorkers=" + activeWorkers
            + " aheadTarget=" + cacheAheadBytes);

    Thread starter = new Thread(new Runnable() {
        @Override
        public void run() {
            workerLoop(myGen, true, 0);
        }
    }, "LocalVideoProxy-Starter");
    starter.setDaemon(true);
    starter.start();

    for (int i = 0; i < boosterThreads; i++) {
        final int idx = i + 1;

        Thread booster = new Thread(new Runnable() {
            @Override
            public void run() {
                workerLoop(myGen, false, idx);
            }
        }, "LocalVideoProxy-Booster-" + idx);

        booster.setDaemon(true);
        booster.start();
    }
}

        private boolean hasPendingWindowWorkLocked() {
            if (closed) {
                return false;
            }

            if (windowStart < 0) {
                windowStart = 0L;
            }

            long windowLimit = windowStart + cacheAheadBytes;

            if (total > 0) {
                windowLimit = Math.min(windowLimit, total);
            }

            if (nextReserve < 0) {
                nextReserve = getCachedEndFromLocked(windowStart);

                if (nextReserve < windowStart) {
                    nextReserve = windowStart;
                }
            }

            long cachedEnd = getCachedEndFromLocked(nextReserve);
            while (cachedEnd > nextReserve) {
                nextReserve = cachedEnd;
                cachedEnd = getCachedEndFromLocked(nextReserve);
            }

            if (total > 0 && nextReserve >= total) {
                return false;
            }

            return nextReserve < windowLimit;
        }

        private void onWorkerFinished(long myGen) {
            synchronized (lock) {
                if (myGen != generation) {
                    return;
                }

                if (activeWorkers > 0) {
                    activeWorkers--;
                }

                if (activeWorkers <= 0) {
                    activeWorkers = 0;
                    workersRunning = false;
                    lock.notifyAll();

                    W("[MULTI_RANGE_WORKERS] idle url=" + url
                            + " windowStart=" + windowStart
                            + " contiguousUntil=" + contiguousUntil
                            + " nextReserve=" + nextReserve
                            + " cachedBytes=" + getCachedBytesLocked());

                    if (hasPendingWindowWorkLocked()) {
                        startWorkersLocked();
                    }
                }
            }
        }

        private void workerLoop(long myGen, boolean starter, int workerIndex) {
            try {
                boolean useStartupChunk = starter;

                while (true) {
                    ChunkPlan chunk;

                synchronized (lock) {
                    if (closed || myGen != generation) {
                        return;
                    }

                    if (windowStart < 0) {
                        windowStart = 0L;
                    }

                    if (nextReserve < 0) {
                        nextReserve = getCachedEndFromLocked(windowStart);

                        if (nextReserve < windowStart) {
                            nextReserve = windowStart;
                        }
                    }

                    if (!starter && !waitForStartupWindowLocked(myGen)) {
                        return;
                    }

                    long cachedEnd = getCachedEndFromLocked(nextReserve);
                    while (cachedEnd > nextReserve) {
                        nextReserve = cachedEnd;
                        cachedEnd = getCachedEndFromLocked(nextReserve);
                    }

                    long windowLimit = windowStart + cacheAheadBytes;

                    if (total > 0) {
                        windowLimit = Math.min(windowLimit, total);
                    }

                    if (total > 0 && nextReserve >= total) {
                        return;
                    }

                    if (nextReserve >= windowLimit) {
                        return;
                    }

                    long start = nextReserve;
                    long len = useStartupChunk ? startupBytes : chunkBytes;
                    useStartupChunk = false;

                    long endInclusive = start + len - 1L;
                    endInclusive = Math.min(endInclusive, windowLimit - 1L);

                    if (total > 0) {
                        endInclusive = Math.min(endInclusive, total - 1L);
                    }

                    if (endInclusive < start) {
                        return;
                    }

                    chunk = new ChunkPlan(start, endInclusive, windowSerial);
                    nextReserve = endInclusive + 1L;
                }

                    boolean ok = downloadChunk(myGen, chunk, workerIndex);

                    if (!ok) {
                        return;
                    }
                }
            } finally {
                onWorkerFinished(myGen);
            }
        }

        private boolean waitForStartupWindowLocked(long myGen) {
            while (!closed && myGen == generation) {
                if (windowStart < 0) {
                    return true;
                }

                long target = windowStart + Math.min(startupBytes, BOOSTER_START_AFTER_BYTES);

                if (total > 0) {
                    target = Math.min(target, total);
                }

                if (target <= windowStart) {
                    return true;
                }

                long cachedEnd = getCachedEndFromLocked(windowStart);

                if (cachedEnd > contiguousUntil) {
                    contiguousUntil = cachedEnd;
                }

                if (cachedEnd >= target) {
                    return true;
                }

                try {
                    lock.wait(150L);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    return false;
                }
            }

            return false;
        }

        private boolean downloadChunk(long myGen, ChunkPlan chunk, int workerIndex) {
            int failCount = 0;

            while (true) {
                long requestStart;

                synchronized (lock) {
                    if (closed || myGen != generation) {
                        return false;
                    }

                    if (chunk.windowSerial >= 0 && chunk.windowSerial != windowSerial) {
                        return true;
                    }

                    long cachedEnd = getCachedEndFromLocked(chunk.start);
                    if (cachedEnd > chunk.endInclusive) {
                        return true;
                    }

                    requestStart = Math.max(chunk.start, cachedEnd);
                }

                okhttp3.Response resp = null;
                ResponseBody body = null;
                Call call = null;
                long callKey = 0L;

                try {
                    Request req = new Request.Builder()
                            .url(url)
                            .get()
                            .header("Range", "bytes=" + requestStart + "-" + chunk.endInclusive)
                            .header("Accept", "*/*")
                            .header("Accept-Encoding", "identity")
                            .header("User-Agent", "UnityLocalProxy/StableCacheAhead/1.0")
                            .build();

                    call = client.newCall(req);
                    callKey = registerCall(call);

                    resp = call.execute();
                    body = resp.body();

                    if (body == null) {
                        throw new IOException("Upstream body null");
                    }

                    int code = resp.code();

                    if (code == 416) {
                        String cr416 = resp.header("Content-Range");
                        long parsedTotal = parse416Total(cr416);

                        synchronized (lock) {
                            if (closed || myGen != generation) {
                                return false;
                            }

                            if (parsedTotal > 0) {
                                total = parsedTotal;
                            } else if (total <= 0) {
                                total = requestStart;
                            }

                            lock.notifyAll();
                        }

                        return false;
                    }

                    if (code != 200 && code != 206) {
                        String err = "";

                        try {
                            err = body.string();
                        } catch (Exception ignored) {
                        }

                        throw new IOException("Upstream error " + code + " " + err);
                    }

                    if (requestStart > 0 && code == 200) {
                        throw new IOException("Upstream ignored Range at pos=" + requestStart);
                    }

                    long bodyLen = body.contentLength();
                    updateResponseInfo(resp, code, requestStart, bodyLen);

                    InputStream in = body.byteStream();
                    byte[] buf = new byte[IO_BUFFER_SIZE];

                    long pos = requestStart;
                    long maxBytes = chunk.endInclusive - requestStart + 1L;
                    long written = 0L;

                    while (written < maxBytes) {
                        int want = (int) Math.min((long) buf.length, maxBytes - written);
                        int n = in.read(buf, 0, want);

                        if (n == -1) {
                            break;
                        }

                        synchronized (lock) {
                            if (closed || myGen != generation) {
                                return false;
                            }

                            if (chunk.windowSerial >= 0 && chunk.windowSerial != windowSerial) {
                                return true;
                            }
                        }

                        writeCache(pos, buf, 0, n);

                        pos += n;
                        written += n;
                    }

                    if (pos <= chunk.endInclusive) {
                        throw new IOException("Upstream closed early at pos=" + pos
                                + " target=" + chunk.endInclusive);
                    }

                    return true;

                } catch (Exception e) {
                    IOException ioe = e instanceof IOException ? (IOException) e : new IOException(e);

                    synchronized (lock) {
                        if (closed || myGen != generation) {
                            return false;
                        }

                        if (chunk.windowSerial >= 0 && chunk.windowSerial != windowSerial) {
                            return true;
                        }
                    }

                    boolean madeProgress = false;

                    synchronized (lock) {
                        if (!closed && myGen == generation) {
                            madeProgress = getCachedEndFromLocked(requestStart) > requestStart;
                        }
                    }

                    if (madeProgress) {
                        failCount = 0;
                        sleepQuietly(15L);
                        continue;
                    }

                    failCount++;
                    long delay = Math.min(3000L, 250L * failCount);

                    W("WORKER " + workerIndex
                            + " DOWN error chunk=" + requestStart + "-" + chunk.endInclusive
                            + " fail=" + failCount
                            + " delay=" + delay
                            + " msg=" + ioe.getMessage());

                    sleepQuietly(delay);

                } finally {
                    closeQuietly(resp);

                    if (callKey != 0L && call != null) {
                        unregisterCall(callKey, call);
                    }
                }
            }
        }

        private RangeState findRangeContainingLocked(long pos) {
            Map.Entry<Long, RangeState> e = ranges.floorEntry(pos);

            if (e == null) {
                return null;
            }

            RangeState r = e.getValue();

            if (r.start <= pos && pos < r.endExclusive) {
                return r;
            }

            return null;
        }

        private void slideWindowForwardLocked(long start) {
            long oldStart = windowStart;
            windowStart = Math.max(windowStart, Math.max(0L, start));

            if (total > 0 && windowStart >= total) {
                windowStart = Math.max(0L, total - 1L);
            }

            contiguousUntil = getCachedEndFromLocked(windowStart);

            if (contiguousUntil < windowStart) {
                contiguousUntil = windowStart;
            }

            if (nextReserve < contiguousUntil) {
                nextReserve = contiguousUntil;
            }

            W("[CACHE_WINDOW] slide url=" + url
                    + " oldStart=" + oldStart
                    + " start=" + windowStart
                    + " cachedUntil=" + contiguousUntil
                    + " nextReserve=" + nextReserve
                    + " aheadTarget=" + cacheAheadBytes
                    + " chunk=" + chunkBytes
                    + " boosters=" + boosterThreads
                    + " cachedBytes=" + getCachedBytesLocked());
        }

        private void resetWindowLocked(long start) {
            windowStart = Math.max(0L, start);
            contiguousUntil = getCachedEndFromLocked(windowStart);

            if (contiguousUntil < windowStart) {
                contiguousUntil = windowStart;
            }

            nextReserve = contiguousUntil;
            windowSerial++;

            W("[CACHE_WINDOW] url=" + url
                    + " start=" + windowStart
                    + " cachedUntil=" + contiguousUntil
                    + " nextReserve=" + nextReserve
                    + " aheadTarget=" + cacheAheadBytes
                    + " chunk=" + chunkBytes
                    + " boosters=" + boosterThreads
                    + " cachedBytes=" + getCachedBytesLocked());
        }

        private long getCachedEndFromLocked(long pos) {
            RangeState r = findRangeContainingLocked(pos);

            if (r == null) {
                return pos;
            }

            long end = r.endExclusive;

            while (true) {
                Map.Entry<Long, RangeState> next = ranges.floorEntry(end);

                if (next != null) {
                    RangeState nr = next.getValue();

                    if (nr.start <= end && nr.endExclusive > end) {
                        end = nr.endExclusive;
                        continue;
                    }
                }

                Map.Entry<Long, RangeState> ceil = ranges.ceilingEntry(end);

                if (ceil != null) {
                    RangeState cr = ceil.getValue();

                    if (cr.start <= end && cr.endExclusive > end) {
                        end = cr.endExclusive;
                        continue;
                    }
                }

                return end;
            }
        }

        private void markRangeLocked(long start, long endExclusive) {
            if (endExclusive <= start) {
                return;
            }

            long ns = start;
            long ne = endExclusive;
            long now = System.currentTimeMillis();

            Map.Entry<Long, RangeState> floor = ranges.floorEntry(ns);
            if (floor != null) {
                RangeState r = floor.getValue();

                if (r.endExclusive >= ns) {
                    ns = Math.min(ns, r.start);
                    ne = Math.max(ne, r.endExclusive);
                    ranges.remove(r.start);
                }
            }

            while (true) {
                Map.Entry<Long, RangeState> ceil = ranges.ceilingEntry(ns);

                if (ceil == null) {
                    break;
                }

                RangeState r = ceil.getValue();

                if (r.start > ne) {
                    break;
                }

                ne = Math.max(ne, r.endExclusive);
                ranges.remove(r.start);
            }

            RangeState merged = new RangeState(ns, ne);
            merged.lastAccessMs = now;
            ranges.put(merged.start, merged);

            if (windowStart >= 0) {
                long c = getCachedEndFromLocked(windowStart);

                if (c > contiguousUntil) {
                    contiguousUntil = c;
                }
            }
        }

        private long getCachedBytesLocked() {
            long sum = 0L;

            for (RangeState r : ranges.values()) {
                sum += Math.max(0L, r.endExclusive - r.start);
            }

            return sum;
        }

        void close(boolean deleteFile) {
            synchronized (lock) {
                closed = true;
                generation++;

                for (Call call : calls.values()) {
                    try {
                        call.cancel();
                    } catch (Exception ignored) {
                    }
                }

                calls.clear();
                ranges.clear();
                workersRunning = false;
                activeWorkers = 0;
                lock.notifyAll();

                try {
                    raf.close();
                } catch (Exception ignored) {
                }
            }

            if (deleteFile) {
                try {
                    boolean existed = file.exists();
                    boolean deleted = !existed || file.delete();

                    W("[CACHE_DELETE] url=" + url
                            + " existed=" + existed
                            + " deleted=" + deleted
                            + " path=" + file.getAbsolutePath());
                } catch (Exception e) {
                    W("[CACHE_DELETE] failed url=" + url + " msg=" + e.getMessage());
                }
            }
        }
    }

    private static class RangeState {
        final long start;
        long endExclusive;
        long lastAccessMs;

        RangeState(long start, long endExclusive) {
            this.start = start;
            this.endExclusive = endExclusive;
            this.lastAccessMs = System.currentTimeMillis();
        }
    }

    private static class ChunkPlan {
        final long start;
        final long endInclusive;
        final long windowSerial;

        ChunkPlan(long start, long endInclusive, long windowSerial) {
            this.start = start;
            this.endInclusive = endInclusive;
            this.windowSerial = windowSerial;
        }
    }

    private static class RangeCacheInputStream extends InputStream {
        private final long id;
        private final StreamCache cache;
        private final OkHttpClient client;
        private final String url;
        private final long endInclusive;
        private final boolean drivesPlaybackWindow;

        private long pos;
        private boolean closed = false;

        private okhttp3.Response upstreamResponse;
        private ResponseBody upstreamBody;
        private InputStream upstreamInput;
        private Call upstreamCall;
        private long upstreamCallKey = 0L;
        private long upstreamExpectedPos = -1L;

        RangeCacheInputStream(
                long id,
                StreamCache cache,
                OkHttpClient client,
                String url,
                long start,
                long endInclusive,
                boolean drivesPlaybackWindow
        ) {
            this.id = id;
            this.cache = cache;
            this.client = client;
            this.url = url;
            this.pos = start;
            this.endInclusive = endInclusive;
            this.drivesPlaybackWindow = drivesPlaybackWindow;
        }

        @Override
        public int read() throws IOException {
            byte[] one = new byte[1];
            int n = read(one, 0, 1);
            return n <= 0 ? -1 : (one[0] & 0xFF);
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            if (closed) {
                return -1;
            }

            if (pos > endInclusive) {
                return -1;
            }

            int max = (int) Math.min(
                    (long) len,
                    endInclusive - pos + 1
            );

            if (drivesPlaybackWindow) {
                cache.ensureWindow(pos);
            }

            int cached = cache.readCachedOrWait(pos, b, off, max, CACHE_WAIT_BEFORE_READ_THROUGH_MS);

            if (cached > 0) {
                pos += cached;

                if (upstreamInput != null && upstreamExpectedPos != pos) {
                    closeUpstreamOnly();
                }

                return cached;
            }

            int attempts = 0;

            while (attempts < 3) {
                try {
                    openUpstreamIfNeeded(pos);

                    int n = upstreamInput.read(b, off, max);

                    if (n > 0) {
                        cache.writeCache(pos, b, off, n);
                        pos += n;
                        upstreamExpectedPos = pos;
                        return n;
                    }

                    if (n == -1 && pos <= endInclusive) {
                        attempts++;
                        closeUpstreamOnly();

                        if (attempts < 3) {
                            sleepQuietly(120L * attempts);
                            continue;
                        }
                    }

                    return n;

                } catch (IOException e) {
                    attempts++;
                    closeUpstreamOnly();

                    if (attempts >= 3) {
                        throw e;
                    }

                    W("#" + id + " READ_THROUGH retry pos=" + pos
                            + " attempt=" + attempts
                            + " msg=" + e.getMessage());

                    sleepQuietly(150L * attempts);
                }
            }

            return -1;
        }

        private void openUpstreamIfNeeded(long start) throws IOException {
            if (upstreamInput != null && upstreamExpectedPos == start) {
                return;
            }

            closeUpstreamOnly();

            upstreamExpectedPos = start;

            Request req = new Request.Builder()
                    .url(url)
                    .get()
                    .header("Range", "bytes=" + start + "-" + endInclusive)
                    .header("Accept", "*/*")
                    .header("Accept-Encoding", "identity")
                    .header("User-Agent", "UnityLocalProxy/StableReadThrough/1.0")
                    .build();

            upstreamCall = client.newCall(req);
            upstreamCallKey = cache.registerCall(upstreamCall);

            upstreamResponse = upstreamCall.execute();
            upstreamBody = upstreamResponse.body();

            if (upstreamBody == null) {
                throw new IOException("RangeCache upstream body null");
            }

            int code = upstreamResponse.code();

            if (code != 200 && code != 206) {
                String err = "";

                try {
                    err = upstreamBody.string();
                } catch (Exception ignored) {
                }

                throw new IOException("RangeCache upstream error " + code + " " + err);
            }

            if (start > 0 && code == 200) {
                throw new IOException("RangeCache upstream ignored Range at pos=" + start);
            }

            long bodyLen = upstreamBody.contentLength();
            cache.updateResponseInfo(upstreamResponse, code, start, bodyLen);

            upstreamInput = upstreamBody.byteStream();

            I("#" + id + " CACHE_MISS upstream start=" + start
                    + " end=" + endInclusive
                    + " code=" + code
                    + " len=" + bodyLen);
        }

        @Override
        public void close() throws IOException {
            closed = true;
            closeUpstreamOnly();
            super.close();
        }

        private void closeUpstreamOnly() {
            try {
                if (upstreamInput != null) {
                    upstreamInput.close();
                }
            } catch (Exception ignored) {
            }

            try {
                if (upstreamBody != null) {
                    upstreamBody.close();
                }
            } catch (Exception ignored) {
            }

            try {
                if (upstreamResponse != null) {
                    upstreamResponse.close();
                }
            } catch (Exception ignored) {
            }

            if (upstreamCallKey != 0L && upstreamCall != null) {
                cache.unregisterCall(upstreamCallKey, upstreamCall);
            }

            upstreamInput = null;
            upstreamBody = null;
            upstreamResponse = null;
            upstreamCall = null;
            upstreamCallKey = 0L;
            upstreamExpectedPos = -1L;
        }
    }

    private static class RangeSpec {
        long start;
        long endInclusive;
        boolean hasEnd;
        boolean isSuffix;
        long suffixLength;

        static RangeSpec parse(String h) {
            if (h == null) {
                return null;
            }

            h = h.trim();

            if (!h.startsWith("bytes=")) {
                return null;
            }

            String v = h.substring("bytes=".length()).trim();

            if (v.contains(",")) {
                return null;
            }

            int dash = v.indexOf('-');

            if (dash < 0) {
                return null;
            }

            String a = v.substring(0, dash).trim();
            String b = v.substring(dash + 1).trim();

            try {
                RangeSpec rs = new RangeSpec();

                if (a.length() == 0) {
                    if (b.length() == 0) {
                        return null;
                    }

                    rs.suffixLength = Long.parseLong(b);

                    if (rs.suffixLength <= 0) {
                        return null;
                    }

                    rs.isSuffix = true;
                    rs.start = 0L;
                    rs.endInclusive = -1L;
                    rs.hasEnd = false;
                    return rs;
                }

                rs.start = Long.parseLong(a);

                if (b.length() > 0) {
                    rs.endInclusive = Long.parseLong(b);
                    rs.hasEnd = true;
                } else {
                    rs.endInclusive = -1L;
                    rs.hasEnd = false;
                }

                if (rs.start < 0) {
                    return null;
                }

                if (rs.hasEnd && rs.endInclusive < rs.start) {
                    return null;
                }

                return rs;

            } catch (Exception e) {
                return null;
            }
        }
    }

    private static class ContentRange {
        long start;
        long end;
        long total;

        static ContentRange parse(String cr) {
            if (cr == null) {
                return null;
            }

            cr = cr.trim();

            if (!cr.startsWith("bytes")) {
                return null;
            }

            int sp = cr.indexOf(' ');
            int dash = cr.indexOf('-', sp + 1);
            int slash = cr.indexOf('/', dash + 1);

            if (sp < 0 || dash < 0 || slash < 0) {
                return null;
            }

            try {
                ContentRange r = new ContentRange();
                r.start = Long.parseLong(cr.substring(sp + 1, dash).trim());
                r.end = Long.parseLong(cr.substring(dash + 1, slash).trim());

                String totalStr = cr.substring(slash + 1).trim();
                r.total = totalStr.equals("*") ? -1L : Long.parseLong(totalStr);

                return r;

            } catch (Exception e) {
                return null;
            }
        }
    }

    private static void closeQuietly(okhttp3.Response r) {
        try {
            if (r != null) {
                r.close();
            }
        } catch (Exception ignored) {
        }
    }

    private static class ResponseBodyInputStream extends InputStream {
        private final okhttp3.Response response;
        private final ResponseBody body;
        private final InputStream in;

        ResponseBodyInputStream(okhttp3.Response response, ResponseBody body) {
            this.response = response;
            this.body = body;
            this.in = body.byteStream();
        }

        @Override
        public int read() throws IOException {
            return in.read();
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            return in.read(b, off, len);
        }

        @Override
        public void close() throws IOException {
            IOException thrown = null;

            try {
                in.close();
            } catch (IOException e) {
                thrown = e;
            }

            try {
                body.close();
            } catch (Exception ignored) {
            }

            closeQuietly(response);

            if (thrown != null) {
                throw thrown;
            }
        }
    }

    private static String safeHeader(okhttp3.Response r, String k, String def) {
        try {
            String v = r.header(k);
            return v != null && v.length() > 0 ? v : def;
        } catch (Exception ignored) {
        }

        return def;
    }

    private static long parse416Total(String contentRange) {
        if (contentRange == null) {
            return -1L;
        }

        try {
            int slash = contentRange.indexOf('/');

            if (slash < 0) {
                return -1L;
            }

            String totalStr = contentRange.substring(slash + 1).trim();

            if (totalStr.length() == 0 || "*".equals(totalStr)) {
                return -1L;
            }

            return Long.parseLong(totalStr);

        } catch (Exception e) {
            return -1L;
        }
    }

    private static boolean isTailMetadataRange(long start, long length, long total) {
        if (total <= 0L || start <= 0L || start >= total) {
            return false;
        }

        long tailWindow = Math.max(
                MIN_TAIL_METADATA_WINDOW_BYTES,
                Math.min(64L * 1024L * 1024L, total / 20L)
        );

        if (start < total - tailWindow) {
            return false;
        }

        if (length < 0L) {
            return true;
        }

        long metadataReadLimit = Math.max(
                2L * 1024L * 1024L,
                Math.min(16L * 1024L * 1024L, tailWindow)
        );

        return length <= metadataReadLimit;
    }

    private static String getParam(String qs, String key) {
        if (qs == null || qs.length() == 0) {
            return null;
        }

        String[] parts = qs.split("&");

        for (String p : parts) {
            int i = p.indexOf('=');

            if (i <= 0) {
                continue;
            }

            String k = p.substring(0, i);

            if (k.equals(key)) {
                return p.substring(i + 1);
            }
        }

        return null;
    }

    private static String sha1(String text) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-1");
            byte[] bytes = md.digest(text.getBytes("UTF-8"));

            StringBuilder sb = new StringBuilder();

            for (byte b : bytes) {
                String h = Integer.toHexString(b & 0xff);

                if (h.length() == 1) {
                    sb.append('0');
                }

                sb.append(h);
            }

            return sb.toString();

        } catch (Exception e) {
            return String.valueOf(text.hashCode());
        }
    }

    private static int clamp(int v, int min, int max) {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    private static long clampLong(long v, long min, long max) {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    private static void sleepQuietly(long ms) {
        try {
            Thread.sleep(ms);
        } catch (Exception ignored) {
        }
    }
}
