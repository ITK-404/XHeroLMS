package com.unity.localproxy;

import android.util.Log;

import java.io.FilterInputStream;
import java.io.InputStream;
import java.net.URLDecoder;
import java.util.Map;
import java.util.concurrent.TimeUnit;

import fi.iki.elonen.NanoHTTPD;
import okhttp3.OkHttpClient;
import okhttp3.Request;

public class LocalVideoProxy extends NanoHTTPD {

    private static final String TAG = "LocalVideoProxy";
    private static volatile LocalVideoProxy instance;

    private final OkHttpClient client;

    public static boolean startProxy(int port) {
        try {
            if (instance != null) {
                Log.i(TAG, "Already started");
                return true;
            }
            instance = new LocalVideoProxy(port);
            instance.start(SOCKET_READ_TIMEOUT, false);
            Log.i(TAG, "Started at http://127.0.0.1:" + port);
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Start failed", e);
            return false;
        }
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
        try {
            if (!"/video".equals(session.getUri())) {
                return newFixedLengthResponse(NanoHTTPD.Response.Status.NOT_FOUND, "text/plain", "Not Found");
            }

            String qs = session.getQueryParameterString();
            String enc = getParam(qs, "u");
            if (enc == null) {
                return newFixedLengthResponse(NanoHTTPD.Response.Status.BAD_REQUEST, "text/plain", "Missing param: u");
            }

            String originUrl = URLDecoder.decode(enc, "UTF-8");

            Map<String, String> headers = session.getHeaders();
            String range = headers.get("range");
            if (range == null) range = "bytes=0-";

            Log.i(TAG, "REQUEST range=" + range + " url=" + originUrl);

            Request req = new Request.Builder()
                    .url(originUrl)
                    .get()
                    .header("Range", range)
                    .header("Accept", "*/*")
                    .header("Connection", "close")
                    .header("User-Agent", "UnityLocalProxy/1.0")
                    .build();

            final okhttp3.Response upstream = client.newCall(req).execute();
            final okhttp3.ResponseBody body = upstream.body();

            int code = upstream.code();
            String contentType = upstream.header("Content-Type", "video/mp4");
            String contentRange = upstream.header("Content-Range");

            if (body == null) {
                upstream.close();
                return newFixedLengthResponse(NanoHTTPD.Response.Status.INTERNAL_ERROR, "text/plain", "Upstream body null");
            }

            NanoHTTPD.Response.Status status;
            if (code == 206) status = NanoHTTPD.Response.Status.PARTIAL_CONTENT;
            else if (code == 200) status = NanoHTTPD.Response.Status.OK;
            else {
                String err = "";
                try { err = body.string(); } catch (Exception ignored) {}
                upstream.close();
                return newFixedLengthResponse(
                        NanoHTTPD.Response.Status.INTERNAL_ERROR,
                        "text/plain",
                        "Upstream error " + code + "\n" + err
                );
            }

            InputStream stream = new FilterInputStream(body.byteStream()) {
                @Override
                public void close() {
                    try { super.close(); } catch (Exception ignored) {}
                    upstream.close();
                }
            };

            // Unity/ExoPlayer thường FAIL với chunked ở vài máy -> tốt nhất là cố gắng trả fixedLength.
            // Nhưng nếu upstream không có Content-Length/Content-Range hợp lệ, ta vẫn chunked để chạy.
            long fixedLen = parseContentRangeLen(contentRange);
            NanoHTTPD.Response resp;
            if (fixedLen > 0) {
                resp = newFixedLengthResponse(status, contentType, stream, fixedLen);
                resp.addHeader("Content-Length", String.valueOf(fixedLen));
            } else {
                resp = newChunkedResponse(status, contentType, stream);
            }

            resp.addHeader("Accept-Ranges", "bytes");
            resp.addHeader("Connection", "close");
            resp.addHeader("Cache-Control", "no-cache");
            if (contentRange != null) resp.addHeader("Content-Range", contentRange);

            return resp;

        } catch (Exception e) {
            Log.e(TAG, "serve exception", e);
            return newFixedLengthResponse(NanoHTTPD.Response.Status.INTERNAL_ERROR, "text/plain", "Proxy exception: " + e.getMessage());
        }
    }

    private static long parseContentRangeLen(String contentRange) {
        // "bytes start-end/total" -> len=end-start+1
        if (contentRange == null) return -1;
        try {
            // very simple parse
            // bytes 0-1023/9999
            String s = contentRange.trim();
            if (!s.startsWith("bytes")) return -1;
            int sp = s.indexOf(' ');
            int dash = s.indexOf('-', sp + 1);
            int slash = s.indexOf('/', dash + 1);
            long start = Long.parseLong(s.substring(sp + 1, dash).trim());
            long end = Long.parseLong(s.substring(dash + 1, slash).trim());
            if (end >= start) return (end - start + 1);
        } catch (Exception ignored) {}
        return -1;
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
