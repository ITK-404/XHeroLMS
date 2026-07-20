package com.xherozone.webviewvideo;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.graphics.Color;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.text.TextUtils;
import android.util.Log;
import android.view.Surface;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.CookieManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;

import androidx.annotation.NonNull;
import androidx.media3.common.C;
import androidx.media3.common.MediaItem;
import androidx.media3.common.PlaybackException;
import androidx.media3.common.Player;
import androidx.media3.common.VideoSize;
import androidx.media3.datasource.DefaultHttpDataSource;
import androidx.media3.exoplayer.ExoPlayer;
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory;
import androidx.media3.exoplayer.trackselection.DefaultTrackSelector;

import com.unity3d.player.UnityPlayer;

import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;

public final class XHeroNativeTexturePlayer {
    private static final String TAG = "XHeroNativeTexture";
    private static final Handler MAIN = new Handler(Looper.getMainLooper());

    private static Activity activity;
    private static WebView resolverWebView;
    private static ExoPlayer player;
    private static Surface playerSurface;
    private static String iframeUrl;
    private static String resolvedStreamUrl;
    private static String userAgent;
    private static String state = "0|0|0|0|0|0";
    private static String lastError = "";
    private static int width;
    private static int height;
    private static int sourceWidth;
    private static int sourceHeight;
    private static boolean active;
    private static boolean playerReady;
    private static boolean streamStarted;
    private static boolean resolverReleaseScheduled;
    private static boolean lifecyclePaused;
    private static boolean playerStoppedByLifecycle;
    private static long lifecycleResumePositionMs;
    private static int playGeneration;
    private static boolean resolveOnlyMode;

    static {
        System.loadLibrary("xheronativetexture");
    }

    private XHeroNativeTexturePlayer() {
    }

    private static native Surface nativeCreateSurface(int width, int height);
    private static native void nativeReleaseSurface();

    public static boolean start(final String url, final int requestedWidth, final int requestedHeight, final int fps) {
        if (TextUtils.isEmpty(url)) {
            lastError = "Missing iframe URL.";
            return false;
        }

        activity = UnityPlayer.currentActivity;
        if (activity == null) {
            lastError = "Unity activity is null.";
            return false;
        }

        width = clamp(requestedWidth, 320, 1920);
        height = clamp(requestedHeight, 180, 1080);
        iframeUrl = url;
        lastError = "";
        state = "0|0|0|0|0|0";
        active = true;
        playerReady = false;
        streamStarted = false;
        resolverReleaseScheduled = false;
        lifecyclePaused = false;
        playerStoppedByLifecycle = false;
        lifecycleResumePositionMs = 0L;
        resolvedStreamUrl = "";
        sourceWidth = 0;
        sourceHeight = 0;
        playGeneration++;

        MAIN.post(new Runnable() {
            @Override public void run() {
                    try {
                        stopOnMainThread();
                        active = true;
                        lifecyclePaused = false;
                        iframeUrl = url;
                        width = clamp(requestedWidth, 320, 1920);
                        height = clamp(requestedHeight, 180, 1080);
                    playerSurface = nativeCreateSurface(width, height);
                    if (playerSurface == null) {
                        lastError = "Native SurfaceTexture is not ready.";
                        return;
                    }

                    createResolverWebView();
                    resolverWebView.loadUrl(url);
                    Log.w(TAG, "start iframe resolver url=" + url + " texture=" + width + "x" + height);
                } catch (Throwable t) {
                    lastError = "Native texture player start failed: " + t.getMessage();
                    Log.e(TAG, lastError, t);
                    stopOnMainThread();
                }
            }
        });

        return true;
    }

    public static boolean resolveOnly(final String url) {
        if (TextUtils.isEmpty(url)) {
            lastError = "Missing iframe URL.";
            return false;
        }

        activity = UnityPlayer.currentActivity;
        if (activity == null) {
            lastError = "Unity activity is null.";
            return false;
        }

        iframeUrl = url;
        lastError = "";
        resolvedStreamUrl = "";
        active = true;
        resolveOnlyMode = true;
        streamStarted = false;
        playGeneration++;

        MAIN.post(new Runnable() {
            @Override public void run() {
                try {
                    stopOnMainThread();
                    active = true;
                    resolveOnlyMode = true;
                    streamStarted = false;
                    resolvedStreamUrl = "";
                    iframeUrl = url;
                    createResolverWebView();
                    resolverWebView.loadUrl(url);
                    Log.w(TAG, "resolveOnly start url=" + url);
                } catch (Throwable t) {
                    lastError = "resolveOnly start failed: " + t.getMessage();
                    Log.e(TAG, lastError, t);
                    stopOnMainThread();
                }
            }
        });

        return true;
    }

    public static String getResolvedUrl() {
        return (resolveOnlyMode && streamStarted && resolvedStreamUrl != null) ? resolvedStreamUrl : "";
    }

    public static void stopResolveOnly() {
        stop();
    }

    public static void stop() {
        if (Looper.myLooper() == MAIN.getLooper()) {
            stopOnMainThread();
            return;
        }

        final CountDownLatch latch = new CountDownLatch(1);
        MAIN.post(new Runnable() {
            @Override public void run() {
                try {
                    stopOnMainThread();
                } finally {
                    latch.countDown();
                }
            }
        });

        try {
            latch.await(1500, TimeUnit.MILLISECONDS);
        } catch (InterruptedException ignored) {
            Thread.currentThread().interrupt();
        }
    }

    public static void play() {
        MAIN.post(new Runnable() {
            @Override public void run() {
                if (player != null && !lifecyclePaused) {
                    player.setVolume(1f);
                    player.play();
                    updateState();
                }
            }
        });
    }

    public static void pause() {
        MAIN.post(new Runnable() {
            @Override public void run() {
                if (player != null) {
                    player.pause();
                    updateState();
                }
            }
        });
    }

    public static void seek(final double seconds) {
        MAIN.post(new Runnable() {
            @Override public void run() {
                if (player != null) {
                    player.seekTo(Math.max(0L, (long)(seconds * 1000.0)));
                    updateState();
                }
            }
        });
    }

    public static void setVolume(final float volume) {
        MAIN.post(new Runnable() {
            @Override public void run() {
                if (player != null) {
                    player.setVolume(lifecyclePaused ? 0f : Math.max(0f, Math.min(1f, volume)));
                }
            }
        });
    }

    public static void lifecyclePause() {
        MAIN.post(new Runnable() {
            @Override public void run() {
                lifecyclePauseOnMainThread();
            }
        });
    }

    public static void lifecycleResume(final boolean shouldResume) {
        MAIN.post(new Runnable() {
            @Override public void run() {
                lifecycleResumeOnMainThread(shouldResume);
            }
        });
    }

    public static void requestState() {
        MAIN.post(new Runnable() {
            @Override public void run() {
                updateState();
            }
        });
    }

    public static String getState() {
        return state;
    }

    public static String getLastError() {
        return lastError;
    }

    @SuppressLint({"SetJavaScriptEnabled", "AddJavascriptInterface"})
    private static void createResolverWebView() {
        resolverWebView = new WebView(activity);
        resolverWebView.setBackgroundColor(Color.TRANSPARENT);
        resolverWebView.setAlpha(0f);
        resolverWebView.setVisibility(View.GONE);
        resolverWebView.setLayerType(View.LAYER_TYPE_SOFTWARE, null);
        resolverWebView.setOverScrollMode(WebView.OVER_SCROLL_NEVER);
        muteResolverWebViewAudio(resolverWebView);
        resolverWebView.addJavascriptInterface(new ResolverBridge(), "XHeroResolver");

        WebSettings settings = resolverWebView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setMediaPlaybackRequiresUserGesture(false);
        settings.setLoadWithOverviewMode(true);
        settings.setUseWideViewPort(true);
        settings.setAllowContentAccess(true);
        settings.setAllowFileAccess(false);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            settings.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        }
        userAgent = settings.getUserAgentString() + " XHeroLMSNativeTexture";
        settings.setUserAgentString(userAgent);

        CookieManager cookieManager = CookieManager.getInstance();
        cookieManager.setAcceptCookie(true);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            cookieManager.setAcceptThirdPartyCookies(resolverWebView, true);
        }

        resolverWebView.setWebChromeClient(new WebChromeClient());
        resolverWebView.setWebViewClient(new WebViewClient() {
            @Override public void onLoadResource(WebView view, String url) {
                maybeUseStream(url);
            }

            @Override public android.webkit.WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
                if (request != null && request.getUrl() != null) {
                    maybeUseStream(request.getUrl().toString());
                }
                return super.shouldInterceptRequest(view, request);
            }

            @Override public void onPageFinished(WebView view, String url) {
                injectResolverScript();
            }
        });

        ViewGroup decor = (ViewGroup)activity.getWindow().getDecorView();
        FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(1, 1);
        lp.leftMargin = -64;
        lp.topMargin = -64;
        decor.addView(resolverWebView, lp);
    }

    private static void injectResolverScript() {
        if (resolverWebView == null) {
            return;
        }

        String script =
                "(function(){" +
                "if(window.xheroNativeResolverInstalled)return;" +
                "window.xheroNativeResolverInstalled=true;" +
                "function send(u){try{if(u&&u.indexOf('blob:')!==0&&window.XHeroResolver){window.XHeroResolver.onStream(u);}}catch(e){}}" +
                "function mute(v){try{v.controls=false;v.muted=true;v.defaultMuted=true;v.volume=0;v.autoplay=false;v.playsInline=true;v.setAttribute('muted','');v.setAttribute('playsinline','');v.setAttribute('preload','metadata');}catch(e){}}" +
                "setInterval(function(){try{var v=document.querySelector('video');if(v){mute(v);send(v.currentSrc||v.src);}}catch(e){}},250);" +
                "})();";

        try {
            resolverWebView.evaluateJavascript(script, null);
        } catch (Throwable ignored) {
        }
    }

    private static void maybeUseStream(String url) {
        if (!active || streamStarted || TextUtils.isEmpty(url)) {
            return;
        }

        String lower = url.toLowerCase(Locale.US);
        boolean isHls = lower.contains(".m3u8");
        boolean isMp4 = lower.contains(".mp4");
        if (!isHls && !isMp4) {
            return;
        }

        if (lower.contains(".m4s") || lower.contains(".ts?") || lower.endsWith(".ts")) {
            return;
        }

        final String streamUrl = url;
        resolvedStreamUrl = streamUrl;
        streamStarted = true;
        Log.w(TAG, "resolved stream=" + streamUrl);
        MAIN.post(new Runnable() {
            @Override public void run() {
                if (!active || !streamStarted || !streamUrl.equals(resolvedStreamUrl)) {
                    return;
                }

                releaseResolverWebViewOnly();

                if (resolveOnlyMode) {
                    Log.w(TAG, "resolveOnly resolved stream=" + streamUrl);
                    return;
                }

                startExoPlayer(streamUrl);
            }
        });
    }

    private static void startExoPlayer(String streamUrl) {
        if (playerSurface == null) {
            lastError = "Missing native video surface.";
            return;
        }

        releasePlayerOnly();

        try {
            Map<String, String> headers = new HashMap<>();
            headers.put("Referer", iframeUrl);
            if (!TextUtils.isEmpty(userAgent)) {
                headers.put("User-Agent", userAgent);
            }

            String cookie = CookieManager.getInstance().getCookie(streamUrl);
            if (!TextUtils.isEmpty(cookie)) {
                headers.put("Cookie", cookie);
            }

            DefaultHttpDataSource.Factory dataSourceFactory = new DefaultHttpDataSource.Factory()
                    .setAllowCrossProtocolRedirects(true)
                    .setUserAgent(userAgent)
                    .setDefaultRequestProperties(headers);

            DefaultTrackSelector trackSelector = new DefaultTrackSelector(activity);
            trackSelector.setParameters(
                    trackSelector.buildUponParameters()
                            .setMaxVideoSize(width, height)
                            .setForceHighestSupportedBitrate(true));

            player = new ExoPlayer.Builder(activity)
                    .setTrackSelector(trackSelector)
                    .setMediaSourceFactory(new DefaultMediaSourceFactory(dataSourceFactory))
                    .build();
            player.setVideoSurface(playerSurface);
            player.setVolume(lifecyclePaused ? 0f : 1f);
            player.addListener(new Player.Listener() {
                @Override public void onPlaybackStateChanged(int playbackState) {
                    playerReady = playbackState == Player.STATE_READY || playerReady;
                    if (playbackState == Player.STATE_READY) {
                        scheduleResolverReleaseAfterPlayerReady();
                    }
                    updateState();
                }

                @Override public void onIsPlayingChanged(boolean isPlaying) {
                    updateState();
                }

                @Override public void onVideoSizeChanged(@NonNull VideoSize videoSize) {
                    sourceWidth = videoSize.width;
                    sourceHeight = videoSize.height;
                    updateState();
                }

                @Override public void onPlayerError(@NonNull PlaybackException error) {
                    lastError = "ExoPlayer error: " + error.getErrorCodeName() + " " + error.getMessage();
                    Log.e(TAG, lastError, error);
                }
            });

            player.setMediaItem(MediaItem.fromUri(Uri.parse(streamUrl)));
            player.prepare();
            if (lifecyclePaused) {
                player.pause();
            } else {
                player.play();
            }
            updateState();
        } catch (Throwable t) {
            lastError = "ExoPlayer start failed: " + t.getMessage();
            Log.e(TAG, lastError, t);
        }
    }

    private static void updateState() {
        if (player == null) {
            state = "0|0|0|0|0|0";
            return;
        }

        long durationMs = player.getDuration();
        if (durationMs == C.TIME_UNSET || durationMs < 0) {
            durationMs = 0;
        }

        long positionMs = Math.max(0L, player.getCurrentPosition());
        int playing = player.isPlaying() ? 1 : 0;
        state = (positionMs / 1000.0) + "|" +
                (durationMs / 1000.0) + "|" +
                playing + "|30|" +
                sourceWidth + "|" +
                sourceHeight;
    }

    private static void stopOnMainThread() {
        active = false;
        resolveOnlyMode = false;
        streamStarted = false;
        playerReady = false;
        resolverReleaseScheduled = false;
        lifecyclePaused = false;
        playerStoppedByLifecycle = false;
        lifecycleResumePositionMs = 0L;
        resolvedStreamUrl = "";
        state = "0|0|0|0|0|0";

        releasePlayerOnly();

        if (resolverWebView != null) {
            releaseResolverWebViewOnly();
        }

        playerSurface = null;
        try { nativeReleaseSurface(); } catch (Throwable ignored) { }
    }

    private static void lifecyclePauseOnMainThread() {
        lifecyclePaused = true;

        if (player != null) {
            try {
                lifecycleResumePositionMs = Math.max(0L, player.getCurrentPosition());
                player.setVolume(0f);
                player.setPlayWhenReady(false);
                player.pause();
                player.stop();
                playerStoppedByLifecycle = true;
            } catch (Throwable ignored) {
            }
        }

        if (resolverWebView != null) {
            releaseResolverWebViewOnly();
        }

        updateState();
        Log.w(TAG, "Lifecycle pause native player. stoppedCodec=" + playerStoppedByLifecycle);
    }

    private static void lifecycleResumeOnMainThread(boolean shouldResume) {
        if (resolverWebView != null) {
            try {
                resolverWebView.onResume();
            } catch (Throwable ignored) {
            }
        }

        lifecyclePaused = false;

        if (player != null) {
            try {
                player.setVolume(1f);
                if (playerStoppedByLifecycle) {
                    player.prepare();
                    if (lifecycleResumePositionMs > 0L) {
                        player.seekTo(lifecycleResumePositionMs);
                    }
                    playerStoppedByLifecycle = false;
                }

                if (shouldResume && active) {
                    player.play();
                }
            } catch (Throwable ignored) {
            }
        }

        updateState();
        Log.w(TAG, "Lifecycle resume native player. shouldResume=" + shouldResume);
    }

    private static void scheduleResolverReleaseAfterPlayerReady() {
        if (resolverReleaseScheduled || resolverWebView == null) {
            return;
        }

        resolverReleaseScheduled = true;
        final int releaseGeneration = playGeneration;
        Log.w(TAG, "ExoPlayer ready; release iframe resolver after native texture warm-up.");
        MAIN.postDelayed(new Runnable() {
            @Override public void run() {
                if (!active || releaseGeneration != playGeneration) {
                    return;
                }
                releaseResolverWebViewOnly();
            }
        }, 1200);
    }

    private static void releaseResolverWebViewOnly() {
        if (resolverWebView == null) {
            return;
        }

        WebView view = resolverWebView;
        resolverWebView = null;

        try {
            view.stopLoading();
            view.evaluateJavascript(
                    "(function(){try{var vs=document.querySelectorAll('video,audio');for(var i=0;i<vs.length;i++){vs[i].muted=true;vs[i].volume=0;vs[i].pause&&vs[i].pause();vs[i].removeAttribute('src');vs[i].load&&vs[i].load();}}catch(e){}})();",
                    null);
        } catch (Throwable ignored) {
        }

        try {
            view.onPause();
        } catch (Throwable ignored) {
        }

        try {
            ViewGroup parent = (ViewGroup)view.getParent();
            if (parent != null) {
                parent.removeView(view);
            }
        } catch (Throwable ignored) {
        }

        try {
            view.stopLoading();
            view.loadDataWithBaseURL("about:blank", "", "text/html", "UTF-8", null);
            view.clearHistory();
        } catch (Throwable ignored) {
        }

        MAIN.postDelayed(new Runnable() {
            @Override public void run() {
                try {
                    view.destroy();
                } catch (Throwable ignored) {
                }
            }
        }, 350);
    }

    private static void muteResolverWebViewAudio(WebView view) {
        if (view == null) {
            return;
        }

        try {
            java.lang.reflect.Method method = WebView.class.getMethod("setAudioMuted", boolean.class);
            method.invoke(view, true);
        } catch (Throwable ignored) {
        }
    }

    private static void releasePlayerOnly() {
        if (player != null) {
            try {
                player.stop();
                player.clearVideoSurface();
                player.release();
            } catch (Throwable ignored) {
            }
        }
        player = null;
    }

    private static int clamp(int value, int min, int max) {
        return Math.max(min, Math.min(max, value));
    }

    private static final class ResolverBridge {
        @JavascriptInterface public void onStream(String url) {
            maybeUseStream(url);
        }
    }
}
