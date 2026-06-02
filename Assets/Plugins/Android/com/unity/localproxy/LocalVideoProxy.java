package com.unity.localproxy;

import android.util.Log;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.RandomAccessFile;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
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
     * Bật true khi cần soi log.
     * Khi test video thật nên để false, vì log Android quá nhiều sẽ làm tụt FPS.
     */
    private static final boolean DEBUG = false;

    private static final long STARTUP_WAIT_MS = 8000L;
    private static final long READ_WAIT_MS = 15000L;

    /*
     * Nếu VideoPlayer nhảy xa hơn vùng đã cache quá mức này,
     * proxy sẽ bỏ window cũ và tải lại từ offset mới.
     */
    private static final long FAR_SEEK_GAP = 8L * 1024L * 1024L;

    private static final int IO_BUFFER_SIZE = 256 * 1024;

    private static volatile LocalVideoProxy instance;

    private final OkHttpClient client;
    private final ConcurrentHashMap<String, StreamCache> caches = new ConcurrentHashMap<>();

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
                instance.closeAllCaches();
                instance.stop();
                instance = null;
                I("Stopped");
            }
        } catch (Exception ignored) {
        }
    }

    /*
     * Gọi từ C# để preload trước khi gán VideoPlayer.url.
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

    public static long getCachedUntil(String originUrl) {
        try {
            LocalVideoProxy p = instance;

            if (p == null) {
                return -1L;
            }

            StreamCache cache = p.getCache(originUrl);
            return cache.getDownloadedUntil();

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

    private LocalVideoProxy(int port) {
        super(port);

        client = new OkHttpClient.Builder()
                .connectTimeout(12, TimeUnit.SECONDS)
                .readTimeout(0, TimeUnit.SECONDS)
                .writeTimeout(30, TimeUnit.SECONDS)
                .retryOnConnectionFailure(true)
                .followRedirects(true)
                .connectionPool(new ConnectionPool(8, 5, TimeUnit.MINUTES))
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

            /*
             * Quan trọng:
             * Proxy bắt đầu tải nền từ offset VideoPlayer đang cần.
             */
            cache.ensureWindow(requestStart);

            /*
             * Chờ lấy được total từ Content-Range upstream.
             * Android VideoPlayer cần Content-Range/Content-Length ổn định.
             */
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
                    + " cachedUntil=" + cache.getDownloadedUntil()
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

                cache = new StreamCache(client, originUrl, file);
                caches.put(key, cache);
            }

            return cache;
        }
    }

    private void closeAllCaches() {
        try {
            for (StreamCache c : caches.values()) {
                c.close();
            }

            caches.clear();

        } catch (Exception ignored) {
        }
    }

    private static File getProxyCacheDir() throws IOException {
        File base = null;

        /*
         * Dùng reflection để không cần import trực tiếp com.unity3d.player.UnityPlayer.
         */
        try {
            Class<?> unityPlayer = Class.forName("com.unity3d.player.UnityPlayer");
            Field activityField = unityPlayer.getField("currentActivity");
            Object activity = activityField.get(null);

            if (activity != null) {
                // Method getCacheDir = activity.getClass().getMethod("getCacheDir");
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

        private long windowStart = -1L;
        private long downloadedUntil = -1L;
        private long total = -1L;

        private String contentType = "video/mp4";

        private boolean downloading = false;
        private long generation = 0L;

        private Call currentCall;
        private IOException lastError;

        StreamCache(
                OkHttpClient client,
                String url,
                File file
        ) throws IOException {
            this.client = client;
            this.url = url;
            this.file = file;
            this.raf = new RandomAccessFile(file, "rw");
        }

        void ensureWindow(long start) throws IOException {
            synchronized (lock) {
                boolean empty = windowStart < 0;
                boolean beforeWindow = !empty && start < windowStart;
                boolean farAhead = !empty && start > downloadedUntil + FAR_SEEK_GAP;

                if (empty || beforeWindow || farAhead) {
                    resetWindowLocked(start);
                }

                if (!downloading && (total < 0 || downloadedUntil < total)) {
                    long from = Math.max(downloadedUntil, start);

                    if (from < windowStart) {
                        from = windowStart;
                    }

                    startDownloadLocked(from);
                }
            }
        }

        long waitForTotal(long timeoutMs) throws IOException {
            long deadline = System.currentTimeMillis() + timeoutMs;

            synchronized (lock) {
                while (total <= 0) {
                    if (!downloading) {
                        long from = downloadedUntil >= 0 ? downloadedUntil : 0L;
                        startDownloadLocked(from);
                    }

                    long wait = deadline - System.currentTimeMillis();

                    if (wait <= 0) {
                        return total;
                    }

                    try {
                        lock.wait(Math.min(wait, 500L));
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
                while (absolutePos >= downloadedUntil) {
                    if (total > 0 && absolutePos >= total) {
                        return -1;
                    }

                    if (!downloading) {
                        long from = Math.max(downloadedUntil, absolutePos);

                        if (from < windowStart) {
                            from = windowStart;
                        }

                        startDownloadLocked(from);
                    }

                    long wait = deadline - System.currentTimeMillis();

                    if (wait <= 0) {
                        throw new IOException(
                                "Cache wait timeout. pos="
                                        + absolutePos
                                        + " downloadedUntil="
                                        + downloadedUntil
                                        + " total="
                                        + total
                        );
                    }

                    try {
                        lock.wait(Math.min(wait, 500L));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        throw new IOException("Interrupted while reading cache", e);
                    }
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
                        downloadedUntil - absolutePos
                );

                raf.seek(relative);

                return raf.read(buffer, offset, available);
            }
        }

        long getDownloadedUntil() {
            synchronized (lock) {
                return downloadedUntil;
            }
        }

        long getTotal() {
            synchronized (lock) {
                return total;
            }
        }

        String getContentType() {
            synchronized (lock) {
                return contentType;
            }
        }

        private void resetWindowLocked(long start) throws IOException {
            generation++;

            if (currentCall != null) {
                try {
                    currentCall.cancel();
                } catch (Exception ignored) {
                }

                currentCall = null;
            }

            downloading = false;
            lastError = null;

            windowStart = start;
            downloadedUntil = start;

            raf.setLength(0);

            I("RESET cache window start=" + start + " file=" + file.getAbsolutePath());

            lock.notifyAll();
        }

        private void startDownloadLocked(long from) {
            if (downloading) {
                return;
            }

            downloading = true;
            lastError = null;

            final long myGen = generation;
            final long startFrom = from;

            Thread t = new Thread(new Runnable() {
                @Override
                public void run() {
                    downloadLoop(myGen, startFrom);
                }
            }, "LocalVideoProxy-Downloader");

            t.setDaemon(true);
            t.start();
        }

        private void downloadLoop(long myGen, long startFrom) {
            long pos = startFrom;
            int failCount = 0;

            while (true) {
                synchronized (lock) {
                    if (myGen != generation) {
                        return;
                    }

                    if (total > 0 && pos >= total) {
                        downloading = false;
                        currentCall = null;
                        lock.notifyAll();
                        return;
                    }
                }

                okhttp3.Response resp = null;
                ResponseBody body = null;
                Call call = null;

                try {
                    Request req = new Request.Builder()
                            .url(url)
                            .get()
                            .header("Range", "bytes=" + pos + "-")
                            .header("Accept", "*/*")
                            .header("Accept-Encoding", "identity")
                            .header("User-Agent", "UnityLocalProxy/CacheAhead/1.0")
                            .build();

                    call = client.newCall(req);

                    synchronized (lock) {
                        if (myGen != generation) {
                            call.cancel();
                            return;
                        }

                        currentCall = call;
                    }

                    resp = call.execute();
                    body = resp.body();

                    if (body == null) {
                        throw new IOException("Upstream body null");
                    }

                    int code = resp.code();

                    if (code != 200 && code != 206) {
                        String err = "";

                        try {
                            err = body.string();
                        } catch (Exception ignored) {
                        }

                        throw new IOException("Upstream error " + code + " " + err);
                    }

                    if (pos > 0 && code == 200) {
                        throw new IOException("Upstream ignored Range at pos=" + pos);
                    }

                    String ct = safeHeader(resp, "Content-Type", "video/mp4");
                    String cr = resp.header("Content-Range");
                    long bodyLen = body.contentLength();

                    ContentRange parsed = ContentRange.parse(cr);

                    synchronized (lock) {
                        if (myGen != generation) {
                            return;
                        }

                        if (ct != null && ct.length() > 0) {
                            contentType = ct;
                        }

                        if (parsed != null && parsed.total > 0) {
                            total = parsed.total;
                        } else if (code == 200 && bodyLen > 0 && pos == 0) {
                            total = bodyLen;
                        }

                        lock.notifyAll();
                    }

                    I("DOWN start pos="
                            + pos
                            + " code="
                            + code
                            + " cr="
                            + (cr != null ? cr : "<null>")
                            + " len="
                            + bodyLen
                            + " total="
                            + getTotal());

                    InputStream in = body.byteStream();
                    byte[] buf = new byte[IO_BUFFER_SIZE];

                    int n;

                    while ((n = in.read(buf)) != -1) {
                        synchronized (lock) {
                            if (myGen != generation) {
                                return;
                            }

                            long relative = pos - windowStart;

                            if (relative < 0) {
                                return;
                            }

                            raf.seek(relative);
                            raf.write(buf, 0, n);

                            pos += n;

                            if (pos > downloadedUntil) {
                                downloadedUntil = pos;
                            }

                            lock.notifyAll();
                        }
                    }

                    failCount = 0;

                    synchronized (lock) {
                        if (myGen != generation) {
                            return;
                        }

                        if (total > 0 && pos >= total) {
                            downloading = false;
                            currentCall = null;
                            lock.notifyAll();
                            return;
                        }
                    }

                    /*
                     * Upstream đóng sớm nhưng chưa hết file.
                     * Đây là trường hợp server/CDN giới hạn mỗi response.
                     * Proxy sẽ tự nối nền, VideoPlayer vẫn đọc từ cache.
                     */
                    sleepQuietly(80L);

                } catch (Exception e) {
                    IOException ioe;

                    if (e instanceof IOException) {
                        ioe = (IOException) e;
                    } else {
                        ioe = new IOException(e);
                    }

                    synchronized (lock) {
                        if (myGen != generation) {
                            return;
                        }

                        lastError = ioe;
                        lock.notifyAll();
                    }

                    failCount++;

                    long delay = Math.min(2000L, 200L * failCount);

                    W("DOWN error at pos="
                            + pos
                            + " fail="
                            + failCount
                            + " delay="
                            + delay
                            + " msg="
                            + ioe.getMessage());

                    sleepQuietly(delay);

                } finally {
                    closeQuietly(resp);

                    synchronized (lock) {
                        if (currentCall == call) {
                            currentCall = null;
                        }
                    }
                }
            }
        }

        void close() {
            synchronized (lock) {
                generation++;

                if (currentCall != null) {
                    try {
                        currentCall.cancel();
                    } catch (Exception ignored) {
                    }

                    currentCall = null;
                }

                downloading = false;
                lock.notifyAll();

                try {
                    raf.close();
                } catch (Exception ignored) {
                }
            }
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

    private static void sleepQuietly(long ms) {
        try {
            Thread.sleep(ms);
        } catch (Exception ignored) {
        }
    }
}