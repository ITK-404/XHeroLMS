package com.unity.localproxy;

import android.util.Log;

import java.io.IOException;
import java.io.InputStream;
import java.net.URLDecoder;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;

import fi.iki.elonen.NanoHTTPD;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.ResponseBody;

public class LocalVideoProxy extends NanoHTTPD {

    private static final String TAG = "LocalVideoProxy";
    private static volatile LocalVideoProxy instance;

    private final OkHttpClient client;
    private static final AtomicLong REQ_ID = new AtomicLong(0);

    private static void I(String s) { Log.i(TAG, s); }
    private static void W(String s) { Log.w(TAG, s); }
    private static void E(String s, Throwable t) { Log.e(TAG, s, t); }

    public static boolean startProxy(int port) {
        try {
            if (instance != null) {
                I("Already started");
                return true;
            }
            instance = new LocalVideoProxy(port);
            instance.start(SOCKET_READ_TIMEOUT, false);
            I("Started at http://127.0.0.1:" + port);
            return true;
        } catch (Exception e) {
            E("Start failed", e);
            return false;
        }
    }

    public static void stopProxy() {
        try {
            if (instance != null) {
                instance.stop();
                instance = null;
                I("Stopped");
            }
        } catch (Exception ignored) {}
    }

    private LocalVideoProxy(int port) {
        super(port);
        client = new OkHttpClient.Builder()
                .connectTimeout(15, TimeUnit.SECONDS)
                .readTimeout(0, TimeUnit.SECONDS)
                .writeTimeout(0, TimeUnit.SECONDS)
                .retryOnConnectionFailure(true)
                .followRedirects(true)
                .build();
    }

    @Override
    public NanoHTTPD.Response serve(IHTTPSession session) {
        final long id = REQ_ID.incrementAndGet();
        okhttp3.Response firstUp = null;

        try {
            String uri = session.getUri();

            if ("/ping".equals(uri)) {
                I("#" + id + " PING");
                return newFixedLengthResponse(NanoHTTPD.Response.Status.OK, "text/plain", "pong");
            }

            if (!"/video".equals(uri)) {
                W("#" + id + " 404 uri=" + uri);
                return newFixedLengthResponse(NanoHTTPD.Response.Status.NOT_FOUND, "text/plain", "Not Found");
            }

            String qs = session.getQueryParameterString();
            String enc = getParam(qs, "u");
            if (enc == null) {
                W("#" + id + " 400 missing u. qs=" + qs);
                return newFixedLengthResponse(NanoHTTPD.Response.Status.BAD_REQUEST, "text/plain", "Missing param: u");
            }

            String originUrl = URLDecoder.decode(enc, "UTF-8");
            Map<String, String> headers = session.getHeaders();

            String clientRange = headers.get("range"); // nanohttpd lowercases
            if (clientRange == null || clientRange.trim().isEmpty())
                clientRange = "bytes=0-";

            RangeSpec clientSpec = RangeSpec.parse(clientRange);
            if (clientSpec == null) {
                W("#" + id + " Unsupported Range: " + clientRange);
                return newFixedLengthResponse(NanoHTTPD.Response.Status.RANGE_NOT_SATISFIABLE, "text/plain",
                        "Bad Range");
            }

            I("#" + id + " IN url=" + originUrl
                    + " clientRange=" + clientRange
                    + " ua=" + headers.get("user-agent"));

            // First upstream request (starting at requested start)
            firstUp = openUpstream(originUrl, clientSpec.start, clientSpec.endInclusive);
            ResponseBody body = firstUp.body();
            if (body == null) {
                closeQuietly(firstUp);
                return newFixedLengthResponse(NanoHTTPD.Response.Status.INTERNAL_ERROR, "text/plain",
                        "Upstream body null");
            }

            int code = firstUp.code();
            String ct = safeHeader(firstUp, "Content-Type", "video/mp4");
            String cr = firstUp.header("Content-Range");
            long bodyLen = body.contentLength();

            ContentRange upCR = ContentRange.parse(cr); // may be null
            long total = (upCR != null && upCR.total > 0) ? upCR.total : -1;

            I("#" + id + " UP first code=" + code
                    + " ct=" + ct
                    + " cr=" + (cr != null ? cr : "<null>")
                    + " bodyLen=" + bodyLen);

            if (code != 200 && code != 206) {
                String err = "";
                try {
                    err = body.string();
                } catch (Exception ignored) {
                }
                closeQuietly(firstUp);
                return newFixedLengthResponse(NanoHTTPD.Response.Status.INTERNAL_ERROR, "text/plain",
                        "Upstream error " + code + "\n" + err);
            }

            long start = clientSpec.start;
            long endInclusive;

            if (clientSpec.hasEnd) {
                endInclusive = clientSpec.endInclusive;
            } else {
                if (total > 0)
                    endInclusive = total - 1;
                else
                    endInclusive = -1;
            }

            final long expectedLen = (endInclusive >= 0 && endInclusive >= start) ? (endInclusive - start + 1) : -1;

            // Build stitched stream: will auto-reconnect to upstream if it stops early (10MB cap)
            AutoResumeInputStream stitched = new AutoResumeInputStream(
                    id, originUrl, client, start, endInclusive, firstUp);

            // IMPORTANT: Reply status 206 so stagefright treats it as ranged stream.
            NanoHTTPD.Response.Status outStatus = NanoHTTPD.Response.Status.PARTIAL_CONTENT;

            NanoHTTPD.Response resp;
            if (expectedLen > 0) {
                resp = newFixedLengthResponse(outStatus, ct, stitched, expectedLen);
                resp.addHeader("Content-Length", String.valueOf(expectedLen));

                if (total > 0) {
                    String outCR = "bytes " + start + "-" + endInclusive + "/" + total;
                    resp.addHeader("Content-Range", outCR);
                    I("#" + id + " OUT fixedLen=" + expectedLen + " outCR=" + outCR);
                } else {
                    I("#" + id + " OUT fixedLen=" + expectedLen + " (total unknown)");
                }
            } else {
                // last resort: chunked
                resp = newChunkedResponse(outStatus, ct, stitched);
                if (total > 0 && upCR != null) {
                    String outCR = "bytes " + start + "-" + upCR.end + "/" + total;
                    resp.addHeader("Content-Range", outCR);
                }
                W("#" + id + " OUT chunked (expectedLen unknown) total=" + total);
            }

            resp.addHeader("Accept-Ranges", "bytes");
            resp.addHeader("Cache-Control", "no-cache");

            return resp;

        } catch (Exception e) {
            E("#" + id + " serve exception", e);
            closeQuietly(firstUp);
            return newFixedLengthResponse(NanoHTTPD.Response.Status.INTERNAL_ERROR, "text/plain",
                    "Proxy exception: " + (e.getMessage() != null ? e.getMessage() : e.toString()));
        }
    }

    private static class AutoResumeInputStream extends InputStream {
        private final long id;
        private final String url;
        private final OkHttpClient client;

        private final long endInclusive; // -1 if unknown
        private long pos; // next absolute byte to read

        private okhttp3.Response currentResp;
        private InputStream currentStream;

        private boolean closed = false;

        AutoResumeInputStream(long id, String url, OkHttpClient client,
                              long start, long endInclusive, okhttp3.Response firstResp) throws IOException {
            this.id = id;
            this.url = url;
            this.client = client;
            this.pos = start;
            this.endInclusive = endInclusive;

            this.currentResp = firstResp;
            ResponseBody body = firstResp.body();
            if (body == null) throw new IOException("first upstream body null");
            this.currentStream = body.byteStream();
        }

        @Override
        public int read() throws IOException {
            byte[] one = new byte[1];
            int n = read(one, 0, 1);
            return (n <= 0) ? -1 : (one[0] & 0xFF);
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            if (closed) return -1;

            if (endInclusive >= 0) {
                long remaining = (endInclusive - pos + 1);
                if (remaining <= 0) return -1;
                if (len > remaining) len = (int)Math.min(len, remaining);
            }

            while (true) {
                int n;
                try {
                    n = currentStream.read(b, off, len);
                } catch (IOException ioe) {
                    W("#" + id + " STITCH read IOException at pos=" + pos + " err=" + ioe.getMessage());
                    reconnect();
                    continue;
                }

                if (n > 0) {
                    pos += n;
                    return n;
                }

                // n == -1 => segment ended early (your 10MB cap case)
                if (endInclusive >= 0 && pos > endInclusive) {
                    I("#" + id + " STITCH done pos=" + pos + " end=" + endInclusive);
                    return -1;
                }

                reconnect();
            }
        }

        private void reconnect() throws IOException {
            closeCurrent();

            if (endInclusive >= 0 && pos > endInclusive) return;

            currentResp = openUpstream(url, pos, endInclusive >= 0 ? endInclusive : -1);
            ResponseBody body = currentResp.body();
            if (body == null) {
                closeQuietly(currentResp);
                throw new IOException("upstream body null on reconnect");
            }

            int code = currentResp.code();
            String cr = currentResp.header("Content-Range");
            long bodyLen = body.contentLength();

            I("#" + id + " STITCH reconnect rangeStart=" + pos
                    + " code=" + code
                    + " cr=" + (cr != null ? cr : "<null>")
                    + " bodyLen=" + bodyLen);

            if (code != 200 && code != 206) {
                String err = "";
                try { err = body.string(); } catch (Exception ignored) {}
                closeQuietly(currentResp);
                throw new IOException("Upstream reconnect error " + code + " body=" + err);
            }

            currentStream = body.byteStream();
        }

        private void closeCurrent() {
            try { if (currentStream != null) currentStream.close(); } catch (Exception ignored) {}
            closeQuietly(currentResp);
            currentStream = null;
            currentResp = null;
        }

        @Override
        public void close() throws IOException {
            closed = true;
            closeCurrent();
            super.close();
        }
    }

    // ======= Upstream open helper =======
    private static okhttp3.Response openUpstream(String url, long start, long endInclusiveOrNeg1) throws IOException {
        String range;
        if (endInclusiveOrNeg1 >= 0 && endInclusiveOrNeg1 >= start) {
            range = "bytes=" + start + "-" + endInclusiveOrNeg1;
        } else {
            range = "bytes=" + start + "-";
        }

        Request req = new Request.Builder()
                .url(url)
                .get()
                .header("Range", range)
                .header("Accept", "*/*")
                .header("Accept-Encoding", "identity")
                .header("User-Agent", "UnityLocalProxy/2.0")
                .build();

        return instance.client.newCall(req).execute();
    }

    private static void closeQuietly(okhttp3.Response r) {
        try { if (r != null) r.close(); } catch (Exception ignored) {}
    }

    private static String safeHeader(okhttp3.Response r, String k, String def) {
        try {
            String v = r.header(k);
            return (v != null && !v.isEmpty()) ? v : def;
        } catch (Exception ignored) {}
        return def;
    }

    private static class RangeSpec {
        long start;
        long endInclusive;
        boolean hasEnd;

        static RangeSpec parse(String h) {
            // supports: bytes=START-  or bytes=START-END
            if (h == null) return null;
            h = h.trim();
            if (!h.startsWith("bytes=")) return null;
            String v = h.substring("bytes=".length()).trim();
            if (v.contains(",")) return null; // reject multi-range

            int dash = v.indexOf('-');
            if (dash < 0) return null;

            String a = v.substring(0, dash).trim();
            String b = v.substring(dash + 1).trim();

            if (a.isEmpty()) return null; // no suffix-range support

            try {
                RangeSpec rs = new RangeSpec();
                rs.start = Long.parseLong(a);
                if (!b.isEmpty()) {
                    rs.endInclusive = Long.parseLong(b);
                    rs.hasEnd = true;
                } else {
                    rs.endInclusive = -1;
                    rs.hasEnd = false;
                }
                if (rs.start < 0) return null;
                if (rs.hasEnd && rs.endInclusive < rs.start) return null;
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
            // bytes START-END/TOTAL
            if (cr == null) return null;
            cr = cr.trim();
            if (!cr.startsWith("bytes")) return null;
            int sp = cr.indexOf(' ');
            int dash = cr.indexOf('-', sp + 1);
            int slash = cr.indexOf('/', dash + 1);
            if (sp < 0 || dash < 0 || slash < 0) return null;
            try {
                ContentRange r = new ContentRange();
                r.start = Long.parseLong(cr.substring(sp + 1, dash).trim());
                r.end = Long.parseLong(cr.substring(dash + 1, slash).trim());
                String tot = cr.substring(slash + 1).trim();
                r.total = tot.equals("*") ? -1 : Long.parseLong(tot);
                return r;
            } catch (Exception e) {
                return null;
            }
        }
    }

    private static String getParam(String qs, String key) {
        if (qs == null || qs.isEmpty()) return null;
        String[] parts = qs.split("&");
        for (String p : parts) {
            int i = p.indexOf('=');
            if (i <= 0) continue;
            String k = p.substring(0, i);
            if (k.equals(key)) return p.substring(i + 1);
        }
        return null;
    }
}
