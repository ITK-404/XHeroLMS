#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>
#import <QuartzCore/QuartzCore.h>

#include <math.h>
#include <stdlib.h>
#include <string.h>

@interface XHeroWVDelegate : NSObject <WKNavigationDelegate>
@end

// -----------------------------------------------------------------------------
// Global bridge state
// -----------------------------------------------------------------------------

static WKWebView *xheroWebView = nil;
static XHeroWVDelegate *xheroDelegate = nil;
static NSTimer *xheroFrameTimer = nil;

static NSData *xheroLastFrame = nil;
static NSData *xheroReadFrame = nil;

static NSString *xheroState = @"0|0|0|0|0|0";
static NSString *xheroLastError = @"";
static NSString *xheroCurrentURL = nil;

static int xheroWidth = 1920;
static int xheroHeight = 1080;
static int xheroFps = 30;

static int xheroRequestedWidth = 1920;
static int xheroRequestedHeight = 1080;
static int xheroRequestedFps = 30;

// Follow the dimensions and measured frame rate of the decoded HTML5 video.
// No 720p/15 FPS production cap is applied by this native bridge.
static BOOL xheroFollowSourceResolution = YES;
static BOOL xheroFollowSourceFrameRate = YES;

// Lossless encoded frames preserve snapshot quality for Texture2D.LoadImage.
// This is substantially heavier than JPEG, especially for 4K content.
static BOOL xheroUseLosslessFrames = YES;

static BOOL xheroSnapshotInFlight = NO;
static BOOL xheroStopping = NO;
static BOOL xheroNavigationFinished = NO;

static NSInteger xheroSnapshotFailureCount = 0;
static NSInteger xheroStateFailureCount = 0;
static NSInteger xheroWebProcessRestartCount = 0;

static NSUInteger xheroGeneration = 0;
static CFTimeInterval xheroLastPatchAttemptTime = 0.0;

static char xheroStateCString[2048] = {0};
static char xheroErrorCString[4096] = {0};

extern "C" void XHeroWV_Stop(void);

// -----------------------------------------------------------------------------
// Thread-safe shared state helpers
// -----------------------------------------------------------------------------

static void XHeroSetState(NSString *value) {
    @synchronized ([XHeroWVDelegate class]) {
        xheroState = value != nil ? [value copy] : @"0|0|0|0|0|0";
    }
}

static void XHeroSetError(NSString *value) {
    @synchronized ([XHeroWVDelegate class]) {
        xheroLastError = value != nil ? [value copy] : @"";
    }
}

static NSString *XHeroGetError(void) {
    @synchronized ([XHeroWVDelegate class]) {
        return xheroLastError != nil ? [xheroLastError copy] : @"";
    }
}

static const char *XHeroCopyStateCString(void) {
    @synchronized ([XHeroWVDelegate class]) {
        const char *source = [(xheroState ?: @"0|0|0|0|0|0") UTF8String];
        strlcpy(xheroStateCString, source != NULL ? source : "", sizeof(xheroStateCString));
        return xheroStateCString;
    }
}

static const char *XHeroCopyErrorCString(void) {
    @synchronized ([XHeroWVDelegate class]) {
        const char *source = [(xheroLastError ?: @"") UTF8String];
        strlcpy(xheroErrorCString, source != NULL ? source : "", sizeof(xheroErrorCString));
        return xheroErrorCString;
    }
}

// -----------------------------------------------------------------------------
// Window / view-controller helpers
// -----------------------------------------------------------------------------

static UIWindow *XHeroActiveWindow(void) {
    UIApplication *application = UIApplication.sharedApplication;

    if (@available(iOS 13.0, *)) {
        UIWindow *fallbackWindow = nil;

        for (UIScene *scene in application.connectedScenes) {
            if (![scene isKindOfClass:[UIWindowScene class]]) {
                continue;
            }

            UIWindowScene *windowScene = (UIWindowScene *)scene;

            if (windowScene.activationState != UISceneActivationStateForegroundActive &&
                windowScene.activationState != UISceneActivationStateForegroundInactive) {
                continue;
            }

            for (UIWindow *window in windowScene.windows) {
                if (fallbackWindow == nil && !window.hidden) {
                    fallbackWindow = window;
                }

                if (window.isKeyWindow) {
                    return window;
                }
            }
        }

        if (fallbackWindow != nil) {
            return fallbackWindow;
        }
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    if (application.keyWindow != nil) {
        return application.keyWindow;
    }
#pragma clang diagnostic pop

    for (UIWindow *window in application.windows) {
        if (!window.hidden) {
            return window;
        }
    }

    return nil;
}

static UIViewController *XHeroTopViewController(UIViewController *controller) {
    if (controller == nil) {
        return nil;
    }

    if (controller.presentedViewController != nil) {
        return XHeroTopViewController(controller.presentedViewController);
    }

    if ([controller isKindOfClass:[UINavigationController class]]) {
        return XHeroTopViewController(((UINavigationController *)controller).visibleViewController);
    }

    if ([controller isKindOfClass:[UITabBarController class]]) {
        return XHeroTopViewController(((UITabBarController *)controller).selectedViewController);
    }

    for (UIViewController *child in controller.childViewControllers) {
        if (child.view.window != nil) {
            return XHeroTopViewController(child);
        }
    }

    return controller;
}

static CGRect XHeroAspectFitFrame(CGRect bounds, CGFloat aspectRatio) {
    CGFloat availableWidth = MAX(1.0, CGRectGetWidth(bounds));
    CGFloat availableHeight = MAX(1.0, CGRectGetHeight(bounds));

    CGFloat width = availableWidth;
    CGFloat height = width / MAX(0.01, aspectRatio);

    if (height > availableHeight) {
        height = availableHeight;
        width = height * aspectRatio;
    }

    CGFloat x = CGRectGetMinX(bounds) + (availableWidth - width) * 0.5;
    CGFloat y = CGRectGetMinY(bounds) + (availableHeight - height) * 0.5;

    return CGRectIntegral(CGRectMake(x, y, width, height));
}

static void XHeroKeepWebViewRenderable(void) {
    if (xheroWebView == nil) {
        return;
    }

    UIWindow *window = XHeroActiveWindow();
    UIViewController *controller = XHeroTopViewController(window.rootViewController);

    if (window == nil || controller == nil || controller.view == nil) {
        return;
    }

    CGFloat aspect = (CGFloat)xheroWidth / (CGFloat)MAX(1, xheroHeight);
    CGRect targetFrame = XHeroAspectFitFrame(controller.view.bounds, aspect);

    if (!CGRectEqualToRect(xheroWebView.frame, targetFrame)) {
        xheroWebView.frame = targetFrame;
    }

    // Keep the WKWebView inside the active view hierarchy so WebKit does not
    // suspend its WebContent/GPU process merely because the view is fully offscreen.
    if (xheroWebView.superview != controller.view) {
        [xheroWebView removeFromSuperview];
        [controller.view addSubview:xheroWebView];
    }

    xheroWebView.hidden = NO;
    xheroWebView.alpha = 1.0;
    xheroWebView.userInteractionEnabled = NO;

    // Unity's Metal view remains visually on top. The WKWebView stays renderable
    // behind it and is captured through takeSnapshotWithConfiguration.
    [controller.view sendSubviewToBack:xheroWebView];
}

static void XHeroCaptureFrame(void);

static int XHeroNormalizeCaptureFps(double fps) {
    if (!isfinite(fps) || fps <= 0.0) {
        return MAX(1, xheroRequestedFps);
    }

    // Do not introduce the former 15 FPS cap. The current Unity C# bridge
    // consumes at up to 60 FPS, so values above 60 would only build pressure.
    return MAX(1, MIN(60, (int)lrint(fps)));
}

static void XHeroRestartFrameTimer(int fps) {
    if (![NSThread isMainThread]) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XHeroRestartFrameTimer(fps);
        });
        return;
    }

    int normalized = XHeroNormalizeCaptureFps((double)fps);
    if (xheroFrameTimer != nil && xheroFps == normalized) {
        return;
    }

    if (xheroFrameTimer != nil) {
        [xheroFrameTimer invalidate];
        xheroFrameTimer = nil;
    }

    xheroFps = normalized;
    NSTimeInterval interval = 1.0 / (double)MAX(1, xheroFps);

    xheroFrameTimer = [NSTimer timerWithTimeInterval:interval
                                            repeats:YES
                                              block:^(NSTimer *timer) {
        XHeroCaptureFrame();
    }];

    [[NSRunLoop mainRunLoop] addTimer:xheroFrameTimer
                              forMode:NSRunLoopCommonModes];
}

// -----------------------------------------------------------------------------
// JavaScript video/canvas bridge
// -----------------------------------------------------------------------------

static NSString *XHeroVideoPatchScript(void)
{
    NSMutableString *script = [NSMutableString stringWithString:
        @"(function(){"
        @"try{"
            @"window.xheroUserPaused=(window.xheroUserPaused===true);"
            @"window.xheroAutoplayStarted=(window.xheroAutoplayStarted===true);"
            @"window.xheroEstimatedFps=window.xheroEstimatedFps||0;"
            @"window.xheroFrameCount=window.xheroFrameCount||0;"
            @"window.xheroFrameTime=window.xheroFrameTime||0;"
            @"window.xheroCanvasError=window.xheroCanvasError||'';"
            @"window.xheroCanvasErrorCount=window.xheroCanvasErrorCount||0;"

            @"var style=document.getElementById('xhero-video-css');"
            @"if(!style){"
                @"style=document.createElement('style');"
                @"style.id='xhero-video-css';"
                @"(document.head||document.documentElement).appendChild(style);"
            @"}"

            @"style.textContent="
                @"'html,body{margin:0!important;padding:0!important;width:100%!important;height:100%!important;overflow:hidden!important;background:#000!important;}' +"
                @"'video{position:fixed!important;left:0!important;top:0!important;width:100vw!important;height:100vh!important;object-fit:contain!important;background:#000!important;z-index:1!important;pointer-events:none!important;}' +"
                @"'#xhero-video-canvas{position:fixed!important;left:0!important;top:0!important;width:100vw!important;height:100vh!important;background:#000!important;z-index:2147483647!important;pointer-events:none!important;}' +"
                @"'.plyr__controls,.plyr__control,.plyr__menu,.plyr__progress,.plyr__volume,.plyr__poster,.plyr__control--overlaid{display:none!important;}' +"
                @"'video::-webkit-media-controls{display:none!important;}';"

            @"var root=document.body||document.documentElement;"
            @"var canvas=document.getElementById('xhero-video-canvas');"

            @"if(!canvas){"
                @"canvas=document.createElement('canvas');"
                @"canvas.id='xhero-video-canvas';"
                @"root.appendChild(canvas);"
            @"}"
    ];

    [script appendFormat:
        @"function xheroResize(video){"
            @"var fallbackW=%d;"
            @"var fallbackH=%d;"
            @"var w=(video&&video.videoWidth>0)?video.videoWidth:fallbackW;"
            @"var h=(video&&video.videoHeight>0)?video.videoHeight:fallbackH;"

            @"if(canvas.width!==w||canvas.height!==h){"
                @"canvas.width=w;"
                @"canvas.height=h;"
            @"}"

            @"window.xheroCanvasWidth=w;"
            @"window.xheroCanvasHeight=h;"
        @"}",
        xheroRequestedWidth,
        xheroRequestedHeight
    ];

    [script appendString:
        @"xheroResize(document.querySelector('video'));"

        @"function xheroPrepareVideo(){"
            @"var video=document.querySelector('video');"

            @"if(!video){"
                @"return null;"
            @"}"

            @"video.controls=false;"
            @"video.playsInline=true;"
            @"video.setAttribute('playsinline','');"
            @"video.setAttribute('webkit-playsinline','');"
            @"video.autoplay=true;"

            // Muted autoplay is substantially more reliable on iOS.
            // Unity can request volume/unmute after the first valid frame
            // through XHeroWV_Evaluate.
            @"if(window.xheroAutoplayStarted!==true){"
                @"window.xheroAutoplayStarted=true;"
                @"video.muted=true;"

                @"video.play().catch(function(e){"
                    @"window.xheroCanvasError='autoplay: '+String(e);"
                @"});"
            @"}"
            @"else if(window.xheroUserPaused!==true&&video.paused){"
                @"video.play().catch(function(){});"
            @"}"

            @"return video;"
        @"}"

        @"xheroPrepareVideo();"

        @"if(window.xheroVideoCanvasLoop!==true){"
            @"window.xheroVideoCanvasLoop=true;"

            @"(function xheroLoop(){"
                @"try{"
                    @"var video=xheroPrepareVideo();"
                    @"xheroResize(video);"

                    @"var cv=document.getElementById('xhero-video-canvas');"

                    @"if(cv&&video&&video.readyState>=2){"
                        @"var now=(window.performance&&performance.now)"
                            @"?performance.now()"
                            @":Date.now();"

                        @"var quality=video.getVideoPlaybackQuality"
                            @"?video.getVideoPlaybackQuality()"
                            @":null;"

                        @"if(quality&&quality.totalVideoFrames>0){"
                            @"if(!window.xheroFrameTime){"
                                @"window.xheroFrameTime=now;"
                                @"window.xheroFrameCount=quality.totalVideoFrames;"
                            @"}"
                            @"else if(now-window.xheroFrameTime>=1000){"
                                @"var measured="
                                    @"(quality.totalVideoFrames-window.xheroFrameCount)"
                                    @"*1000/"
                                    @"(now-window.xheroFrameTime);"

                                @"if(measured>=1&&measured<=120){"
                                    @"window.xheroEstimatedFps=measured;"
                                @"}"

                                @"window.xheroFrameTime=now;"
                                @"window.xheroFrameCount=quality.totalVideoFrames;"
                            @"}"
                        @"}"

                        @"var ctx=cv.getContext("
                            @"'2d',"
                            @"{alpha:false,desynchronized:true}"
                        @");"

                        @"if(ctx){"
                            @"ctx.imageSmoothingEnabled=true;"
                            @"ctx.imageSmoothingQuality='high';"
                            @"ctx.fillStyle='#000';"
                            @"ctx.fillRect(0,0,cv.width,cv.height);"

                            @"var vw=video.videoWidth||cv.width;"
                            @"var vh=video.videoHeight||cv.height;"
                            @"var scale=Math.min(cv.width/vw,cv.height/vh);"
                            @"var dw=vw*scale;"
                            @"var dh=vh*scale;"
                            @"var dx=(cv.width-dw)*0.5;"
                            @"var dy=(cv.height-dh)*0.5;"

                            @"ctx.drawImage(video,dx,dy,dw,dh);"

                            @"window.xheroCanvasError='';"
                            @"window.xheroCanvasErrorCount=0;"
                        @"}"
                    @"}"
                @"}"
                @"catch(e){"
                    @"window.xheroCanvasError=String("
                        @"e&&e.message"
                        @"?e.message"
                        @":e"
                    @");"

                    @"window.xheroCanvasErrorCount="
                        @"(window.xheroCanvasErrorCount||0)+1;"
                @"}"

                @"window.requestAnimationFrame(xheroLoop);"
            @"})();"
        @"}"

        @"return 'ok';"
        @"}"
        @"catch(e){"
            @"return 'patch-error:'+String("
                @"e&&e.message"
                @"?e.message"
                @":e"
            @");"
        @"}"
        @"})()"
    ];

    return script;
}

static void XHeroInjectVideoPatch(BOOL force) {
    if (xheroWebView == nil || xheroStopping) {
        return;
    }

    CFTimeInterval now = CACurrentMediaTime();
    if (!force && now - xheroLastPatchAttemptTime < 0.75) {
        return;
    }

    xheroLastPatchAttemptTime = now;
    NSUInteger generation = xheroGeneration;
    NSString *script = XHeroVideoPatchScript();

    [xheroWebView evaluateJavaScript:script completionHandler:^(id result, NSError *error) {
        if (generation != xheroGeneration || xheroStopping) {
            return;
        }

        if (error != nil) {
            xheroStateFailureCount++;
            if (xheroStateFailureCount >= 8) {
                XHeroSetError([NSString stringWithFormat:@"iOS video patch injection failed repeatedly: %@", error.localizedDescription]);
            }
            return;
        }

        if ([result isKindOfClass:[NSString class]] &&
            [(NSString *)result hasPrefix:@"patch-error:"]) {
            XHeroSetError((NSString *)result);
            return;
        }

        xheroStateFailureCount = 0;
    }];
}

// -----------------------------------------------------------------------------
// Snapshot capture
// -----------------------------------------------------------------------------

static void XHeroCaptureFrame(void) {
    if (xheroWebView == nil ||
        xheroStopping ||
        !xheroNavigationFinished ||
        xheroSnapshotInFlight) {
        return;
    }

    XHeroKeepWebViewRenderable();
    XHeroInjectVideoPatch(NO);

    xheroSnapshotInFlight = YES;
    NSUInteger generation = xheroGeneration;

    WKSnapshotConfiguration *configuration = [[WKSnapshotConfiguration alloc] init];
    configuration.rect = xheroWebView.bounds;

    if (@available(iOS 11.0, *)) {
        configuration.snapshotWidth = @(xheroWidth);
    }

    [xheroWebView takeSnapshotWithConfiguration:configuration
                              completionHandler:^(UIImage *snapshot, NSError *error) {
        if (generation != xheroGeneration || xheroStopping) {
            xheroSnapshotInFlight = NO;
            return;
        }

        xheroSnapshotInFlight = NO;

        if (snapshot == nil || error != nil) {
            xheroSnapshotFailureCount++;

            if (xheroSnapshotFailureCount >= 12) {
                NSString *message = error != nil
                    ? error.localizedDescription
                    : @"WKWebView returned an empty snapshot.";

                XHeroSetError([NSString stringWithFormat:@"iOS WKWebView snapshot failed repeatedly: %@", message]);
            }
            return;
        }

        xheroSnapshotFailureCount = 0;

        @autoreleasepool {
            NSData *encoded = nil;

            if (xheroUseLosslessFrames) {
                encoded = UIImagePNGRepresentation(snapshot);
            } else {
                encoded = UIImageJPEGRepresentation(snapshot, 0.95);
            }

            if (encoded == nil || encoded.length == 0) {
                encoded = xheroUseLosslessFrames
                    ? UIImageJPEGRepresentation(snapshot, 0.95)
                    : UIImagePNGRepresentation(snapshot);
            }

            if (encoded != nil && encoded.length > 0) {
                @synchronized ([XHeroWVDelegate class]) {
                    // Keep only the newest frame. Slow Unity consumers do not build a queue.
                    xheroLastFrame = encoded;
                }
            }
        }
    }];
}

// -----------------------------------------------------------------------------
// WKNavigationDelegate
// -----------------------------------------------------------------------------

@implementation XHeroWVDelegate

- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation {
    if (webView != xheroWebView || xheroStopping) {
        return;
    }

    xheroNavigationFinished = YES;
    xheroSnapshotFailureCount = 0;
    xheroStateFailureCount = 0;

    XHeroInjectVideoPatch(YES);

    // Bunny/player pages often insert the <video> element after navigation finishes.
    NSUInteger generation = xheroGeneration;
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.35 * NSEC_PER_SEC)),
                   dispatch_get_main_queue(), ^{
        if (generation == xheroGeneration && !xheroStopping) {
            XHeroInjectVideoPatch(YES);
        }
    });

    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(1.0 * NSEC_PER_SEC)),
                   dispatch_get_main_queue(), ^{
        if (generation == xheroGeneration && !xheroStopping) {
            XHeroInjectVideoPatch(YES);
        }
    });
}

- (void)webView:(WKWebView *)webView
didFailNavigation:(WKNavigation *)navigation
      withError:(NSError *)error {
    if (webView != xheroWebView) {
        return;
    }

    XHeroSetError(error != nil
        ? [NSString stringWithFormat:@"iOS WKWebView navigation failed: %@", error.localizedDescription]
        : @"iOS WKWebView navigation failed.");
}

- (void)webView:(WKWebView *)webView
didFailProvisionalNavigation:(WKNavigation *)navigation
      withError:(NSError *)error {
    if (webView != xheroWebView) {
        return;
    }

    XHeroSetError(error != nil
        ? [NSString stringWithFormat:@"iOS WKWebView provisional navigation failed: %@", error.localizedDescription]
        : @"iOS WKWebView provisional navigation failed.");
}

- (void)webViewWebContentProcessDidTerminate:(WKWebView *)webView {
    if (webView != xheroWebView || xheroStopping) {
        return;
    }

    xheroNavigationFinished = NO;
    xheroSnapshotInFlight = NO;

    // Perform one automatic recovery. A second termination is surfaced to Unity so
    // CourseListView can use its normal Link 2 fallback instead of waiting 35 seconds.
    if (xheroWebProcessRestartCount < 1 && xheroCurrentURL.length > 0) {
        xheroWebProcessRestartCount++;

        NSUInteger generation = xheroGeneration;
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.25 * NSEC_PER_SEC)),
                       dispatch_get_main_queue(), ^{
            if (generation != xheroGeneration || xheroStopping || xheroWebView == nil) {
                return;
            }

            NSURL *url = [NSURL URLWithString:xheroCurrentURL];
            if (url == nil) {
                XHeroSetError(@"iOS WebContent process terminated and the reload URL is invalid.");
                return;
            }

            XHeroSetError(@"");
            [xheroWebView loadRequest:[NSURLRequest requestWithURL:url
                                                      cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
                                                  timeoutInterval:30.0]];
        });

        return;
    }

    XHeroSetError(@"iOS WKWebView WebContent process terminated repeatedly.");
}

@end

// -----------------------------------------------------------------------------
// Exported C ABI used by XHeroWebViewTexturePlayer.cs
// -----------------------------------------------------------------------------

extern "C" bool XHeroWV_Start(const char *url, int width, int height, int fps) {
    if (url == NULL || strlen(url) == 0) {
        XHeroSetError(@"Missing WebView URL.");
        return false;
    }

    NSString *urlString = [NSString stringWithUTF8String:url];
    NSURL *parsedURL = [NSURL URLWithString:urlString];

    if (urlString.length == 0 || parsedURL == nil) {
        XHeroSetError(@"Invalid WebView URL.");
        return false;
    }

    __block BOOL started = NO;

    void (^startBlock)(void) = ^{
        XHeroWV_Stop();

        xheroGeneration++;
        xheroStopping = NO;

        // Start with the dimensions/FPS requested by Unity. Once the HTML5 video
        // reports videoWidth/videoHeight and measured FPS, the bridge follows that
        // decoded source automatically.
        xheroRequestedWidth = MAX(1, width);
        xheroRequestedHeight = MAX(1, height);
        xheroRequestedFps = MAX(1, fps);

        xheroWidth = xheroRequestedWidth;
        xheroHeight = xheroRequestedHeight;
        xheroFps = XHeroNormalizeCaptureFps((double)xheroRequestedFps);

        xheroCurrentURL = [urlString copy];

        XHeroSetState(@"0|0|0|0|0|0");
        XHeroSetError(@"");

        @synchronized ([XHeroWVDelegate class]) {
            xheroLastFrame = nil;
            xheroReadFrame = nil;
        }

        xheroSnapshotInFlight = NO;
        xheroNavigationFinished = NO;
        xheroSnapshotFailureCount = 0;
        xheroStateFailureCount = 0;
        xheroWebProcessRestartCount = 0;
        xheroLastPatchAttemptTime = 0.0;

        UIWindow *window = XHeroActiveWindow();
        UIViewController *controller = XHeroTopViewController(window.rootViewController);

        if (window == nil || controller == nil || controller.view == nil) {
            XHeroSetError(@"Cannot find an active iOS window/root view controller for WKWebView.");
            return;
        }

        WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
        configuration.allowsInlineMediaPlayback = YES;
        configuration.websiteDataStore = [WKWebsiteDataStore defaultDataStore];
        configuration.applicationNameForUserAgent = @"XHeroLMS";

        if (@available(iOS 10.0, *)) {
            configuration.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeNone;
        }

        CGFloat aspect = (CGFloat)xheroWidth / (CGFloat)MAX(1, xheroHeight);
        CGRect frame = XHeroAspectFitFrame(controller.view.bounds, aspect);

        xheroWebView = [[WKWebView alloc] initWithFrame:frame configuration:configuration];
        xheroWebView.opaque = YES;
        xheroWebView.backgroundColor = UIColor.blackColor;
        xheroWebView.scrollView.backgroundColor = UIColor.blackColor;
        xheroWebView.scrollView.scrollEnabled = NO;
        xheroWebView.scrollView.bounces = NO;
        xheroWebView.userInteractionEnabled = NO;
        xheroWebView.hidden = NO;
        xheroWebView.alpha = 1.0;

        xheroDelegate = [[XHeroWVDelegate alloc] init];
        xheroWebView.navigationDelegate = xheroDelegate;

        [controller.view addSubview:xheroWebView];
        [controller.view sendSubviewToBack:xheroWebView];

        NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:parsedURL
                                                              cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
                                                          timeoutInterval:30.0];

        [request setValue:@"https://iframe.mediadelivery.net/"
       forHTTPHeaderField:@"Referer"];

        [xheroWebView loadRequest:request];

        XHeroRestartFrameTimer(xheroFps);

        started = YES;
    };

    if ([NSThread isMainThread]) {
        startBlock();
    } else {
        dispatch_sync(dispatch_get_main_queue(), startBlock);
    }

    return started;
}

extern "C" void XHeroWV_Stop(void) {
    void (^stopBlock)(void) = ^{
        xheroStopping = YES;
        xheroGeneration++;

        if (xheroFrameTimer != nil) {
            [xheroFrameTimer invalidate];
            xheroFrameTimer = nil;
        }

        xheroSnapshotInFlight = NO;
        xheroNavigationFinished = NO;

        if (xheroWebView != nil) {
            xheroWebView.navigationDelegate = nil;
            [xheroWebView stopLoading];
            [xheroWebView removeFromSuperview];
            xheroWebView = nil;
        }

        xheroDelegate = nil;
        xheroCurrentURL = nil;

        @synchronized ([XHeroWVDelegate class]) {
            xheroLastFrame = nil;
            xheroReadFrame = nil;
            xheroState = @"0|0|0|0|0|0";
        }

        xheroSnapshotFailureCount = 0;
        xheroStateFailureCount = 0;
        xheroWebProcessRestartCount = 0;
        xheroLastPatchAttemptTime = 0.0;

        xheroStopping = NO;
    };

    if ([NSThread isMainThread]) {
        stopBlock();
    } else {
        dispatch_sync(dispatch_get_main_queue(), stopBlock);
    }
}

extern "C" int XHeroWV_GetFrameLength(void) {
    @synchronized ([XHeroWVDelegate class]) {
        // Stage one immutable frame so GetFrameLength and CopyFrame always refer
        // to the same NSData even if a newer snapshot arrives between both calls.
        if (xheroReadFrame == nil && xheroLastFrame != nil) {
            xheroReadFrame = xheroLastFrame;
            xheroLastFrame = nil;
        }

        return xheroReadFrame != nil ? (int)xheroReadFrame.length : 0;
    }
}

extern "C" unsigned char *XHeroWV_CopyFrame(void) {
    NSData *frame = nil;

    @synchronized ([XHeroWVDelegate class]) {
        frame = xheroReadFrame;
        xheroReadFrame = nil;
    }

    if (frame == nil || frame.length == 0) {
        return NULL;
    }

    unsigned char *copy = (unsigned char *)malloc(frame.length);
    if (copy == NULL) {
        XHeroSetError(@"Cannot allocate memory for the iOS WebView frame.");
        return NULL;
    }

    memcpy(copy, frame.bytes, frame.length);
    return copy;
}

extern "C" void XHeroWV_ReleaseFrame(void *frame) {
    if (frame != NULL) {
        free(frame);
    }
}

extern "C" void XHeroWV_Evaluate(const char *script) {
    if (script == NULL) {
        return;
    }

    NSString *javaScript = [NSString stringWithUTF8String:script];
    if (javaScript.length == 0) {
        return;
    }

    NSUInteger generation = xheroGeneration;

    dispatch_async(dispatch_get_main_queue(), ^{
        if (generation != xheroGeneration || xheroStopping || xheroWebView == nil) {
            return;
        }

        [xheroWebView evaluateJavaScript:javaScript
                      completionHandler:^(id result, NSError *error) {
            if (generation != xheroGeneration || xheroStopping) {
                return;
            }

            // Control scripts may fail transiently while the page is navigating.
            // Only store persistent errors after the page has finished loading.
            if (error != nil && xheroNavigationFinished) {
                xheroStateFailureCount++;

                if (xheroStateFailureCount >= 8) {
                    XHeroSetError([NSString stringWithFormat:@"iOS WebView JavaScript control failed repeatedly: %@", error.localizedDescription]);
                }
            }
        }];
    });
}

extern "C" void XHeroWV_RequestState(void) {
    NSUInteger generation = xheroGeneration;

    dispatch_async(dispatch_get_main_queue(), ^{
        if (generation != xheroGeneration || xheroStopping || xheroWebView == nil) {
            return;
        }

        NSString *script =
            @"(function(){"
              "try{"
                "var video=document.querySelector('video');"
                "var canvasError=window.xheroCanvasError||'';"
                "var canvasErrorCount=window.xheroCanvasErrorCount||0;"

                "if(video){"
                  "return ["
                    "(video.currentTime||0),"
                    "(isFinite(video.duration)?video.duration:0),"
                    "(!video.paused?1:0),"
                    "(window.xheroEstimatedFps||0),"
                    "(video.videoWidth||0),"
                    "(video.videoHeight||0),"
                    "(window.xheroCanvasWidth||0),"
                    "(window.xheroCanvasHeight||0),"
                    "encodeURIComponent(canvasError),"
                    "canvasErrorCount"
                  "].join('|');"
                "}"

                "return ['0','0','0','0','0','0','0','0',encodeURIComponent(canvasError),canvasErrorCount].join('|');"
              "}catch(e){"
                "return 'STATE_ERROR|'+encodeURIComponent(String(e&&e.message?e.message:e));"
              "}"
            "})()";

        [xheroWebView evaluateJavaScript:script
                      completionHandler:^(id result, NSError *error) {
            if (generation != xheroGeneration || xheroStopping) {
                return;
            }

            if (error != nil) {
                xheroStateFailureCount++;

                if (xheroStateFailureCount >= 8) {
                    XHeroSetError([NSString stringWithFormat:@"iOS WKWebView state polling failed repeatedly: %@", error.localizedDescription]);
                }
                return;
            }

            if (![result isKindOfClass:[NSString class]]) {
                return;
            }

            NSString *stateResult = (NSString *)result;

            if ([stateResult hasPrefix:@"STATE_ERROR|"]) {
                NSString *encoded = [stateResult substringFromIndex:[@"STATE_ERROR|" length]];
                NSString *decoded = [encoded stringByRemovingPercentEncoding] ?: encoded;
                XHeroSetError([NSString stringWithFormat:@"iOS video state script failed: %@", decoded]);
                return;
            }

            NSArray<NSString *> *parts = [stateResult componentsSeparatedByString:@"|"];
            if (parts.count < 6) {
                return;
            }

            NSString *sixPartState = [[parts subarrayWithRange:NSMakeRange(0, 6)]
                componentsJoinedByString:@"|"];

            XHeroSetState(sixPartState);
            xheroStateFailureCount = 0;

            double measuredFps = [parts[3] doubleValue];
            int sourceWidth = [parts[4] intValue];
            int sourceHeight = [parts[5] intValue];

            if (xheroFollowSourceResolution &&
                sourceWidth > 0 &&
                sourceHeight > 0 &&
                (sourceWidth != xheroWidth || sourceHeight != xheroHeight)) {
                xheroWidth = sourceWidth;
                xheroHeight = sourceHeight;

                XHeroKeepWebViewRenderable();
                XHeroInjectVideoPatch(YES);
            }

            if (xheroFollowSourceFrameRate && measuredFps > 0.0) {
                int sourceFps = XHeroNormalizeCaptureFps(measuredFps);
                XHeroRestartFrameTimer(sourceFps);
            }

            if (parts.count >= 10) {
                NSString *encodedError = parts[8];
                NSInteger canvasErrorCount = [parts[9] integerValue];

                if (canvasErrorCount >= 120 && encodedError.length > 0) {
                    NSString *decodedError =
                        [encodedError stringByRemovingPercentEncoding] ?: encodedError;

                    XHeroSetError([NSString stringWithFormat:
                        @"iOS video-to-canvas rendering failed repeatedly: %@",
                        decodedError]);
                }
            }
        }];
    });
}

extern "C" const char *XHeroWV_GetState(void) {
    return XHeroCopyStateCString();
}

extern "C" const char *XHeroWV_GetLastError(void) {
    return XHeroCopyErrorCString();
}
