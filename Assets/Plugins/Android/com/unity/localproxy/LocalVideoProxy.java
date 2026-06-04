package com.unity.localproxy;

import android.util.Log;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.RandomAccessFile;
import java.lang.reflect.Field;
import java.net.URLDecoder;
import java.security.MessageDigest;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;

import fi.iki.elonen.NanoHTTPD;
import okhttp3.Call;
import okhttp3.ConnectionPool;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.ResponseBody;

public class LocalVideoProxy extends NanoHTTPD {

    private static final String TAG = "LocalVideoProxy";

    /*
     * Multi-thread disk cache version.
     * - Starter thread tải đoạn đầu nhỏ để VideoPlayer bắt đầu nhanh.
     * - Booster threads tải các chunk phía sau xuống file cache trên disk.
     * - Khi qua bài mới, C# gọi setActiveUrl/release để cancel bài cũ + xóa cache cũ.
     */
    private static final boolean DEBUG = false;

    private static final long STARTUP_WAIT_MS = 8000L;
    private static final long READ_WAIT_MS = 15000L;
    private static final long FAR_SEEK_GAP = 8L * 1024L * 1024L;

    private static final int IO_BUFFER_SIZE = 128 * 1024;
    private static final int MIN_BOOSTER_THREADS = 1;
    private static final int MAX_BOOSTER_THREADS = 3;

    private static volatile LocalVideoProxy instance;

    private final OkHttpClient client;
    private final ConcurrentHashMap<String, StreamCache> caches = new ConcurrentHashMap<>();

    private volatile long startupBytes = 2L * 1024L * 1024L;
    private volatile long chunkBytes = 2L * 1024L * 1024L;
    private volatile int boosterThreads = 3;

    private static final AtomicLong REQ_ID = new AtomicLong(0);

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
        return "LocalVideoProxy/MultiDiskCache/2026-06-04";
    }

    public static synchronized boolean startProxy(int port) {
        try {
            if (instance != null) {
                I("Already started");
                return true;
            }

            LocalVideoProxy proxy = new LocalVideoProxy(port);
            proxy.start(SOCKET_READ_TIMEOUT, false);
            instance = proxy;

            I("Started at http://127.0.0.1:" + port);
            return true;

        } catch (Exception e) {
            instance = null;
            E("Start failed", e);
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
                I("Stopped");
            }
        } catch (Exception ignored) {
        }
    }

    /**
     * Gọi từ C# trước khi preload.
     * startupBytes: đoạn đầu cần có để bắt đầu phát nhanh, ví dụ 1-3MB.
     * boosterThreadCount: số luồng booster phía sau, khuyến nghị 2-3.
     * chunkBytes: mỗi booster tải từng cục bao nhiêu byte, ví dụ 1-2MB.
     */
    public static boolean configure(long startupBytes, int boosterThreadCount, long chunkBytes) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("configure failed: proxy not started");
                return false;
            }

            p.startupBytes = Math.max(512L * 1024L, startupBytes);
            p.chunkBytes = Math.max(512L * 1024L, chunkBytes);
            p.boosterThreads = clamp(boosterThreadCount, MIN_BOOSTER_THREADS, MAX_BOOSTER_THREADS);

            I("configure startup=" + p.startupBytes
                    + " chunk=" + p.chunkBytes
                    + " boosters=" + p.boosterThreads);
            return true;

        } catch (Exception e) {
            E("configure failed", e);
            return false;
        }
    }

    /**
     * Gọi khi chuẩn bị mở bài mới.
     * Nếu deleteOldCaches=true, mọi cache của bài khác sẽ bị cancel/close/xóa file.
     */
    public static boolean setActiveUrl(String originUrl, boolean deleteOldCaches) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("setActiveUrl failed: proxy not started");
                return false;
            }

            p.releaseAllExceptInternal(originUrl, deleteOldCaches);
            return true;

        } catch (Exception e) {
            E("setActiveUrl failed", e);
            return false;
        }
    }

    /**
     * Gọi từ C# khi đổi bài hoặc mở PDF/bài thi để dọn bài video cũ.
     */
    public static boolean release(String originUrl, boolean deleteFile) {
        try {
            LocalVideoProxy p = instance;

            if (p == null || originUrl == null || originUrl.length() == 0) {
                return false;
            }

            return p.releaseInternal(originUrl, deleteFile);

        } catch (Exception e) {
            E("release failed", e);
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
            E("releaseAllExcept failed", e);
            return 0;
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

            return count;

        } catch (Exception e) {
            E("clearDiskCache failed", e);
            return 0;
        }
    }

    /**
     * Gọi từ C# để preload trước khi gán VideoPlayer.url.
     * Hàm này chỉ start worker; dữ liệu được ghi xuống disk cache.
     */
    public static boolean preload(String originUrl, long start) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                W("preload failed: proxy not started");
                return false;
            }

            StreamCache cache = p.getCache(originUrl);
            cache.ensureWindow(start);

            I("preload url=" + originUrl + " start=" + start);
            return true;

        } catch (Exception e) {
            E("preload failed", e);
            return false;
        }
    }

    /**
     * Trả về byte contiguous đã sẵn sàng từ windowStart.
     * Đây là mốc an toàn để VideoPlayer đọc một mạch, không tính các chunk booster đã xong nhưng còn gap phía trước.
     */
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

    private LocalVideoProxy(int port) {
        super(port);

        client = new OkHttpClient.Builder()
                .connectTimeout(12, TimeUnit.SECONDS)
                .readTimeout(0, TimeUnit.SECONDS)
                .writeTimeout(30, TimeUnit.SECONDS)
                .retryOnConnectionFailure(true)
                .followRedirects(true)
                .connectionPool(new ConnectionPool(4, 30, TimeUnit.SECONDS))
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

            if (!"/video".equals(uri) && !"/warm".equals(uri)) {
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

            requestStart = clientSpec.start;

            I("#" + id + " IN url=" + originUrl
                    + " range=" + clientRange
                    + " ua=" + headers.get("user-agent"));

            StreamCache cache = getCache(originUrl);
            cache.ensureWindow(requestStart);

            long total = cache.waitForTotal(STARTUP_WAIT_MS);

            if (total <= 0) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.SERVICE_UNAVAILABLE,
                        "text/plain",
                        "Proxy could not determine total video size"
                );
            }

            long outStart = requestStart;
            long outEnd;

            if (clientSpec.hasEnd) {
                outEnd = Math.min(clientSpec.endInclusive, total - 1);
            } else {
                outEnd = total - 1;
            }

            if (outStart < 0 || outStart >= total || outEnd < outStart) {
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.RANGE_NOT_SATISFIABLE,
                        "text/plain",
                        "Range out of file"
                );
            }

            long outLen = outEnd - outStart + 1;

            CacheReadInputStream in = new CacheReadInputStream(
                    id,
                    cache,
                    outStart,
                    outEnd
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

            I("#" + id
                    + " OUT from cache start=" + outStart
                    + " end=" + outEnd
                    + " len=" + outLen
                    + " contiguousUntil=" + cache.getContiguousUntil()
                    + " total=" + total);

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
            return false;
        }

        cache.close(deleteFile);
        client.connectionPool().evictAll();

        I("release url=" + originUrl + " deleteFile=" + deleteFile);
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

        I("releaseAllExcept keep=" + keepOriginUrl + " count=" + count + " deleteFile=" + deleteFile);
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

        private final ConcurrentHashMap<Long, ChunkState> chunks = new ConcurrentHashMap<>();
        private final ConcurrentHashMap<Long, Call> calls = new ConcurrentHashMap<>();

        private long windowStart = -1L;
        private long contiguousUntil = -1L;
        private long nextReserve = -1L;
        private long total = -1L;

        private String contentType = "video/mp4";

        private boolean workersRunning = false;
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
            this.startupBytes = Math.max(512L * 1024L, startupBytes);
            this.boosterThreads = clamp(boosterThreads, MIN_BOOSTER_THREADS, MAX_BOOSTER_THREADS);
            this.chunkBytes = Math.max(512L * 1024L, chunkBytes);
            this.raf = new RandomAccessFile(file, "rw");
        }

        void ensureWindow(long start) throws IOException {
            synchronized (lock) {
                if (closed) {
                    throw new IOException("Cache already closed");
                }

                boolean empty = windowStart < 0;
                boolean beforeWindow = !empty && start < windowStart;
                boolean farAhead = !empty && start > contiguousUntil + FAR_SEEK_GAP;

                if (empty || beforeWindow || farAhead) {
                    resetWindowLocked(start);
                }

                startWorkersLocked();
            }
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

        int read(
                long absolutePos,
                byte[] buffer,
                int offset,
                int length,
                long timeoutMs
        ) throws IOException {
            long deadline = System.currentTimeMillis() + timeoutMs;

            synchronized (lock) {
                while (!closed && absolutePos >= contiguousUntil) {
                    if (total > 0 && absolutePos >= total) {
                        return -1;
                    }

                    startWorkersLocked();

                    long wait = deadline - System.currentTimeMillis();

                    if (wait <= 0) {
                        throw new IOException(
                                "Cache wait timeout. pos="
                                        + absolutePos
                                        + " contiguousUntil="
                                        + contiguousUntil
                                        + " total="
                                        + total
                        );
                    }

                    try {
                        lock.wait(Math.min(wait, 250L));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        throw new IOException("Interrupted while reading cache", e);
                    }
                }

                if (closed) {
                    return -1;
                }

                if (absolutePos < windowStart) {
                    throw new IOException(
                            "Cache window moved. pos="
                                    + absolutePos
                                    + " windowStart="
                                    + windowStart
                    );
                }

                long relative = absolutePos - windowStart;

                int available = (int) Math.min(
                        (long) length,
                        contiguousUntil - absolutePos
                );

                raf.seek(relative);
                return raf.read(buffer, offset, available);
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

        String getContentType() {
            synchronized (lock) {
                return contentType;
            }
        }

        private void resetWindowLocked(long start) throws IOException {
            generation++;

            for (Call call : calls.values()) {
                try {
                    call.cancel();
                } catch (Exception ignored) {
                }
            }

            calls.clear();
            chunks.clear();

            workersRunning = false;

            windowStart = start;
            contiguousUntil = start;
            nextReserve = start;

            raf.setLength(0);

            I("RESET cache window start=" + start + " file=" + file.getAbsolutePath());
            lock.notifyAll();
        }

        private void startWorkersLocked() {
            if (closed || workersRunning) {
                return;
            }

            workersRunning = true;
            final long myGen = generation;

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

        private void workerLoop(long myGen, boolean starter, int workerIndex) {
            boolean useStartupChunk = starter;

            while (true) {
                ChunkState chunk;

                synchronized (lock) {
                    if (closed || myGen != generation) {
                        return;
                    }

                    if (total > 0 && nextReserve >= total) {
                        return;
                    }

                    long start = nextReserve;
                    long len = useStartupChunk ? startupBytes : chunkBytes;
                    useStartupChunk = false;

                    long end = start + len - 1L;

                    if (total > 0) {
                        end = Math.min(end, total - 1L);
                    }

                    if (end < start) {
                        return;
                    }

                    chunk = new ChunkState(start, end);
                    chunks.put(start, chunk);
                    nextReserve = end + 1L;
                }

                boolean ok = downloadChunk(myGen, chunk, workerIndex);

                if (!ok) {
                    return;
                }
            }
        }

        private boolean downloadChunk(long myGen, ChunkState chunk, int workerIndex) {
            int failCount = 0;

            while (true) {
                synchronized (lock) {
                    if (closed || myGen != generation) {
                        return false;
                    }
                }

                okhttp3.Response resp = null;
                ResponseBody body = null;
                Call call = null;

                try {
                    Request req = new Request.Builder()
                            .url(url)
                            .get()
                            .header("Range", "bytes=" + chunk.start + "-" + chunk.endInclusive)
                            .header("Accept", "*/*")
                            .header("Accept-Encoding", "identity")
                            .header("User-Agent", "UnityLocalProxy/MultiDiskCache/1.0")
                            .build();

                    call = client.newCall(req);
                    calls.put(chunk.start, call);

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
                                total = chunk.start;
                            }

                            chunks.remove(chunk.start);
                            advanceContiguousLocked();
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

                    if (chunk.start > 0 && code == 200) {
                        throw new IOException("Upstream ignored Range at pos=" + chunk.start);
                    }

                    String ct = safeHeader(resp, "Content-Type", "video/mp4");
                    String cr = resp.header("Content-Range");
                    long bodyLen = body.contentLength();
                    ContentRange parsed = ContentRange.parse(cr);

                    synchronized (lock) {
                        if (closed || myGen != generation) {
                            return false;
                        }

                        if (ct != null && ct.length() > 0) {
                            contentType = ct;
                        }

                        if (parsed != null && parsed.total > 0) {
                            total = parsed.total;
                        } else if (code == 200 && bodyLen > 0 && chunk.start == 0) {
                            total = bodyLen;
                        }

                        lock.notifyAll();
                    }

                    I("WORKER " + workerIndex
                            + " DOWN chunk=" + chunk.start + "-" + chunk.endInclusive
                            + " code=" + code
                            + " cr=" + (cr != null ? cr : "<null>")
                            + " len=" + bodyLen
                            + " total=" + getTotal());

                    InputStream in = body.byteStream();
                    byte[] buf = new byte[IO_BUFFER_SIZE];
                    long pos = chunk.start;
                    long maxBytes = chunk.endInclusive - chunk.start + 1L;
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

                            long relative = pos - windowStart;

                            if (relative < 0) {
                                return false;
                            }

                            raf.seek(relative);
                            raf.write(buf, 0, n);

                            pos += n;
                            written += n;
                            lock.notifyAll();
                        }
                    }

                    synchronized (lock) {
                        if (closed || myGen != generation) {
                            return false;
                        }

                        if (written > 0) {
                            chunk.endInclusive = chunk.start + written - 1L;
                            chunk.done = true;
                        } else {
                            chunks.remove(chunk.start);
                        }

                        if (total > 0 && chunk.endInclusive >= total) {
                            chunk.endInclusive = total - 1L;
                        }

                        advanceContiguousLocked();
                        lock.notifyAll();
                    }

                    return true;

                } catch (Exception e) {
                    IOException ioe = e instanceof IOException ? (IOException) e : new IOException(e);

                    synchronized (lock) {
                        if (closed || myGen != generation) {
                            return false;
                        }
                    }

                    failCount++;
                    long delay = Math.min(2000L, 200L * failCount);

                    W("WORKER " + workerIndex
                            + " DOWN error chunk=" + chunk.start + "-" + chunk.endInclusive
                            + " fail=" + failCount
                            + " delay=" + delay
                            + " msg=" + ioe.getMessage());

                    sleepQuietly(delay);

                } finally {
                    closeQuietly(resp);
                    calls.remove(chunk.start, call);
                }
            }
        }

        private void advanceContiguousLocked() {
            while (true) {
                if (total > 0 && contiguousUntil >= total) {
                    contiguousUntil = total;
                    return;
                }

                ChunkState c = chunks.get(contiguousUntil);

                if (c == null || !c.done || c.start != contiguousUntil) {
                    return;
                }

                long next = c.endInclusive + 1L;
                chunks.remove(c.start);

                if (next <= contiguousUntil) {
                    return;
                }

                if (total > 0) {
                    next = Math.min(next, total);
                }

                contiguousUntil = next;
            }
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
                chunks.clear();
                workersRunning = false;
                lock.notifyAll();

                try {
                    raf.close();
                } catch (Exception ignored) {
                }
            }

            if (deleteFile) {
                try {
                    if (file.exists() && !file.delete()) {
                        W("Cannot delete cache file: " + file.getAbsolutePath());
                    }
                } catch (Exception ignored) {
                }
            }
        }
    }

    private static class ChunkState {
        final long start;
        long endInclusive;
        boolean done;

        ChunkState(long start, long endInclusive) {
            this.start = start;
            this.endInclusive = endInclusive;
            this.done = false;
        }
    }

    private static class CacheReadInputStream extends InputStream {
        private final long id;
        private final StreamCache cache;
        private final long endInclusive;

        private long pos;
        private boolean closed = false;

        CacheReadInputStream(
                long id,
                StreamCache cache,
                long start,
                long endInclusive
        ) {
            this.id = id;
            this.cache = cache;
            this.pos = start;
            this.endInclusive = endInclusive;
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

            try {
                int n = cache.read(pos, b, off, max, READ_WAIT_MS);

                if (n > 0) {
                    pos += n;
                }

                return n;

            } catch (IOException e) {
                String msg = e.getMessage();

                if (msg != null && msg.toLowerCase().contains("broken pipe")) {
                    W("#" + id + " broken pipe while reading cache");
                } else {
                    W("#" + id + " cache read error: " + msg);
                }

                throw e;
            }
        }

        @Override
        public void close() throws IOException {
            closed = true;
            super.close();
        }
    }

    private static class RangeSpec {
        long start;
        long endInclusive;
        boolean hasEnd;

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

            if (a.length() == 0) {
                return null;
            }

            try {
                RangeSpec rs = new RangeSpec();
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

    private static void sleepQuietly(long ms) {
        try {
            Thread.sleep(ms);
        } catch (Exception ignored) {
        }
    }
}
