#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>
#import <QuartzCore/QuartzCore.h>
#import <AVFoundation/AVFoundation.h>
#import <CoreMedia/CoreMedia.h>
#import <CoreVideo/CoreVideo.h>

#include <math.h>
#include <stdlib.h>
#include <string.h>

@interface XHeroWVDelegate : NSObject <WKNavigationDelegate>
@end

@interface XHeroWVResolverDelegate : NSObject <WKNavigationDelegate>
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

static AVPlayer *xheroPlayer = nil;
static AVPlayerItem *xheroPlayerItem = nil;
static AVPlayerItemVideoOutput *xheroVideoOutput = nil;
static NSURLSessionDataTask *xheroResolveTask = nil;
static NSString *xheroResolvedStreamURL = nil;
static BOOL xheroNativePlayerReady = NO;

static WKWebView *xheroResolverWebView = nil;
static XHeroWVResolverDelegate *xheroResolverDelegate = nil;
static NSTimer *xheroResolverTimer = nil;
static NSString *xheroResolverResolvedURL = @"";
static NSString *xheroResolverLastError = @"";
static NSString *xheroResolverCurrentURL = nil;
static BOOL xheroResolverStopping = NO;
static BOOL xheroResolverNavigationFinished = NO;
static NSUInteger xheroResolverGeneration = 0;
static NSInteger xheroResolverFailureCount = 0;

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
static char xheroResolverURLCString[4096] = {0};
static char xheroResolverErrorCString[4096] = {0};

extern "C" void XHeroWV_Stop(void);
extern "C" void XHeroWVResolver_Stop(void);

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

static void XHeroResolverSetResolvedURL(NSString *value) {
    @synchronized ([XHeroWVResolverDelegate class]) {
        xheroResolverResolvedURL = value != nil ? [value copy] : @"";
    }
}

static void XHeroResolverSetError(NSString *value) {
    @synchronized ([XHeroWVResolverDelegate class]) {
        xheroResolverLastError = value != nil ? [value copy] : @"";
    }
}

static const char *XHeroResolverCopyResolvedURLCString(void) {
    @synchronized ([XHeroWVResolverDelegate class]) {
        const char *source = [(xheroResolverResolvedURL ?: @"") UTF8String];
        strlcpy(xheroResolverURLCString, source != NULL ? source : "", sizeof(xheroResolverURLCString));
        return xheroResolverURLCString;
    }
}

static const char *XHeroResolverCopyErrorCString(void) {
    @synchronized ([XHeroWVResolverDelegate class]) {
        const char *source = [(xheroResolverLastError ?: @"") UTF8String];
        strlcpy(xheroResolverErrorCString, source != NULL ? source : "", sizeof(xheroResolverErrorCString));
        return xheroResolverErrorCString;
    }
}

static BOOL XHeroResolverIsPlayableStreamURL(NSString *url) {
    if (url == nil || url.length == 0) {
        return NO;
    }

    NSString *lower = url.lowercaseString;
    if ([lower hasPrefix:@"blob:"] ||
        [lower containsString:@".m4s"] ||
        [lower containsString:@".ts?"] ||
        [lower hasSuffix:@".ts"]) {
        return NO;
    }

    return [lower containsString:@".m3u8"] || [lower containsString:@".mp4"];
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

static NSString *XHeroResolverScript(void) {
    return
        @"(function(){"
        @"try{"
        @"function clean(u){return (u||'').toString();}"
        @"function ok(u){"
        @"u=clean(u);"
        @"var l=u.toLowerCase();"
        @"return u&&"
        @"l.indexOf('blob:')!==0&&"
        @"(l.indexOf('.m3u8')>=0||l.indexOf('.mp4')>=0)&&"
        @"l.indexOf('.m4s')<0&&"
        @"l.indexOf('.ts?')<0&&"
        @"!/\\.ts($|[?#])/.test(l);"
        @"}"
        @"function pick(u){return ok(u)?u:'';}"
        @"var nodes=document.querySelectorAll('video,source');"
        @"for(var i=0;i<nodes.length;i++){"
        @"var n=nodes[i];"
        @"var u=pick(n.currentSrc||n.src||n.getAttribute('src'));"
        @"if(u){return u;}"
        @"if(n.tagName&&n.tagName.toLowerCase()==='video'){"
        @"try{"
        @"n.muted=true;"
        @"n.playsInline=true;"
        @"n.autoplay=true;"
        @"n.setAttribute('muted','');"
        @"n.setAttribute('playsinline','');"
        @"if(n.play){n.play().catch(function(){});}"
        @"}catch(e){}"
        @"}"
        @"}"
        @"if(window.performance&&performance.getEntriesByType){"
        @"var rs=performance.getEntriesByType('resource');"
        @"for(var j=0;j<rs.length;j++){"
        @"var r=rs[j];"
        @"var u2=pick(r&&r.name);"
        @"if(u2){return u2;}"
        @"}"
        @"}"
        @"return '';"
        @"}catch(e){"
        @"return 'error:'+String(e&&e.message?e.message:e);"
        @"}"
        @"})()";
}

static void XHeroResolverPoll(void) {
    if (xheroResolverWebView == nil || xheroResolverStopping) {
        return;
    }

    NSUInteger generation = xheroResolverGeneration;
    NSString *script = XHeroResolverScript();

    [xheroResolverWebView evaluateJavaScript:script
                           completionHandler:^(id result, NSError *error) {
        if (generation != xheroResolverGeneration ||
            xheroResolverStopping ||
            xheroResolverWebView == nil) {
            return;
        }

        if (error != nil) {
            if (xheroResolverNavigationFinished) {
                xheroResolverFailureCount++;

                if (xheroResolverFailureCount >= 8) {
                    XHeroResolverSetError([NSString stringWithFormat:@"iOS Bunny resolver JavaScript failed repeatedly: %@", error.localizedDescription]);
                }
            }

            return;
        }

        if (![result isKindOfClass:[NSString class]]) {
            return;
        }

        NSString *value = (NSString *)result;
        if (XHeroResolverIsPlayableStreamURL(value)) {
            XHeroResolverSetResolvedURL(value);
            XHeroResolverSetError(@"");
            return;
        }

        if ([value hasPrefix:@"error:"]) {
            xheroResolverFailureCount++;

            if (xheroResolverFailureCount >= 8) {
                XHeroResolverSetError(value);
            }
        }
    }];
}

static void XHeroResolverRestartTimer(void) {
    if (![NSThread isMainThread]) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XHeroResolverRestartTimer();
        });
        return;
    }

    if (xheroResolverTimer != nil) {
        [xheroResolverTimer invalidate];
        xheroResolverTimer = nil;
    }

    xheroResolverTimer = [NSTimer timerWithTimeInterval:0.25
                                                repeats:YES
                                                  block:^(NSTimer *timer) {
        XHeroResolverPoll();
    }];

    [[NSRunLoop mainRunLoop] addTimer:xheroResolverTimer
                              forMode:NSRunLoopCommonModes];
}

static void XHeroResolverStopOnMainThread(void) {
    xheroResolverStopping = YES;
    xheroResolverGeneration++;

    if (xheroResolverTimer != nil) {
        [xheroResolverTimer invalidate];
        xheroResolverTimer = nil;
    }

    xheroResolverNavigationFinished = NO;

    if (xheroResolverWebView != nil) {
        xheroResolverWebView.navigationDelegate = nil;
        [xheroResolverWebView stopLoading];
        [xheroResolverWebView removeFromSuperview];
        xheroResolverWebView = nil;
    }

    xheroResolverDelegate = nil;
    xheroResolverCurrentURL = nil;
    xheroResolverFailureCount = 0;

    XHeroResolverSetResolvedURL(@"");
    XHeroResolverSetError(@"");

    xheroResolverStopping = NO;
}

// -----------------------------------------------------------------------------
// Native AVPlayer texture bridge
// -----------------------------------------------------------------------------

static NSString *XHeroExtractPlayableStreamURLFromText(NSString *text) {
    if (text == nil || text.length == 0) {
        return nil;
    }

    NSError *regexError = nil;
    NSRegularExpression *regex = [NSRegularExpression
        regularExpressionWithPattern:@"https?://[^\\\"'<>\\s\\\\]+\\.(?:m3u8|mp4)(?:[^\\\"'<>\\s\\\\]*)?"
                             options:NSRegularExpressionCaseInsensitive
                               error:&regexError];

    if (regex == nil || regexError != nil) {
        return nil;
    }

    NSArray<NSTextCheckingResult *> *matches = [regex matchesInString:text
                                                               options:0
                                                                 range:NSMakeRange(0, text.length)];

    for (NSTextCheckingResult *match in matches) {
        if (match.range.location == NSNotFound || NSMaxRange(match.range) > text.length) {
            continue;
        }

        NSString *candidate = [text substringWithRange:match.range];
        candidate = [candidate stringByReplacingOccurrencesOfString:@"&amp;" withString:@"&"];
        candidate = [candidate stringByReplacingOccurrencesOfString:@"\\/" withString:@"/"];

        if (XHeroResolverIsPlayableStreamURL(candidate)) {
            return candidate;
        }
    }

    return nil;
}

static double XHeroSecondsFromTime(CMTime time) {
    if (!CMTIME_IS_NUMERIC(time) || CMTIME_IS_INDEFINITE(time)) {
        return 0.0;
    }

    double value = CMTimeGetSeconds(time);
    return isfinite(value) && value > 0.0 ? value : 0.0;
}

static void XHeroUpdateAVPlayerStateWithSize(int width, int height) {
    if (xheroPlayer == nil || xheroPlayerItem == nil) {
        return;
    }

    double current = XHeroSecondsFromTime(xheroPlayer.currentTime);
    double duration = XHeroSecondsFromTime(xheroPlayerItem.duration);
    BOOL playing = xheroPlayer.rate > 0.01;

    if ((width <= 0 || height <= 0) && !CGSizeEqualToSize(xheroPlayerItem.presentationSize, CGSizeZero)) {
        width = (int)lrint(xheroPlayerItem.presentationSize.width);
        height = (int)lrint(xheroPlayerItem.presentationSize.height);
    }

    if (width > 0 && height > 0) {
        xheroWidth = width;
        xheroHeight = height;
    }

    XHeroSetState([NSString stringWithFormat:@"%.6f|%.6f|%d|%d|%d|%d",
        current,
        duration,
        playing ? 1 : 0,
        xheroFps,
        MAX(0, xheroWidth),
        MAX(0, xheroHeight)
    ]);
}

static void XHeroStopAVPlayer(void) {
    if (xheroResolveTask != nil) {
        [xheroResolveTask cancel];
        xheroResolveTask = nil;
    }

    if (xheroPlayer != nil) {
        [xheroPlayer pause];
    }

    if (xheroPlayerItem != nil && xheroVideoOutput != nil) {
        [xheroPlayerItem removeOutput:xheroVideoOutput];
    }

    xheroPlayer = nil;
    xheroPlayerItem = nil;
    xheroVideoOutput = nil;
    xheroResolvedStreamURL = nil;
    xheroNativePlayerReady = NO;
}

static void XHeroStartAVPlayerWithStreamURL(NSString *streamURL) {
    if (![NSThread isMainThread]) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XHeroStartAVPlayerWithStreamURL(streamURL);
        });
        return;
    }

    if (streamURL == nil || streamURL.length == 0) {
        XHeroSetError(@"iOS AVPlayer cannot start because Link 1 stream URL is empty.");
        return;
    }

    NSURL *url = [NSURL URLWithString:streamURL];
    if (url == nil) {
        XHeroSetError([NSString stringWithFormat:@"iOS AVPlayer stream URL is invalid: %@", streamURL]);
        return;
    }

    XHeroStopAVPlayer();
    xheroResolvedStreamURL = [streamURL copy];

    NSError *audioError = nil;
    [[AVAudioSession sharedInstance] setCategory:AVAudioSessionCategoryPlayback error:&audioError];
    [[AVAudioSession sharedInstance] setActive:YES error:nil];

    if (audioError != nil) {
        NSLog(@"[XHeroWV] AVAudioSession category failed: %@", audioError.localizedDescription);
    }

    NSString *referer = xheroCurrentURL.length > 0
        ? xheroCurrentURL
        : @"https://iframe.mediadelivery.net/";

    NSDictionary *headers = @{
        @"Referer": referer,
        @"User-Agent": @"Mozilla/5.0 XHeroLMS/iOSAVPlayer"
    };

    NSDictionary *assetOptions = @{
        AVURLAssetHTTPHeaderFieldsKey: headers
    };

    AVURLAsset *asset = [AVURLAsset URLAssetWithURL:url options:assetOptions];
    xheroPlayerItem = [AVPlayerItem playerItemWithAsset:asset];

    NSDictionary *pixelAttributes = @{
        (id)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA)
    };

    xheroVideoOutput = [[AVPlayerItemVideoOutput alloc] initWithPixelBufferAttributes:pixelAttributes];
    [xheroPlayerItem addOutput:xheroVideoOutput];

    xheroPlayer = [AVPlayer playerWithPlayerItem:xheroPlayerItem];
    xheroPlayer.actionAtItemEnd = AVPlayerActionAtItemEndPause;
    xheroPlayer.volume = 1.0f;
    xheroPlayer.muted = NO;

    xheroNativePlayerReady = YES;
    XHeroSetError(@"");
    XHeroUpdateAVPlayerStateWithSize(0, 0);
    XHeroRestartFrameTimer(xheroFps);

    [xheroPlayer play];

    NSLog(@"[XHeroWV] Started Link 1 through AVPlayer. stream=%@ referer=%@", streamURL, referer);
}

static void XHeroResolveLink1AndStartAVPlayer(NSString *urlString) {
    if (urlString == nil || urlString.length == 0) {
        XHeroSetError(@"Missing Link 1 URL for iOS AVPlayer.");
        return;
    }

    if (XHeroResolverIsPlayableStreamURL(urlString)) {
        XHeroStartAVPlayerWithStreamURL(urlString);
        return;
    }

    NSURL *url = [NSURL URLWithString:urlString];
    if (url == nil) {
        XHeroSetError(@"Invalid Link 1 iframe URL for iOS AVPlayer.");
        return;
    }

    NSUInteger generation = xheroGeneration;
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:url
                                                           cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
                                                       timeoutInterval:15.0];
    [request setValue:@"Mozilla/5.0 XHeroLMS/iOSAVPlayerResolver" forHTTPHeaderField:@"User-Agent"];

    if (xheroResolveTask != nil) {
        [xheroResolveTask cancel];
        xheroResolveTask = nil;
    }

    xheroResolveTask = [[NSURLSession sharedSession]
        dataTaskWithRequest:request
          completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (generation != xheroGeneration || xheroStopping) {
                return;
            }

            xheroResolveTask = nil;

            if (error != nil) {
                XHeroSetError([NSString stringWithFormat:@"iOS Link 1 iframe HTML request failed: %@", error.localizedDescription]);
                return;
            }

            NSString *html = data != nil
                ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding]
                : nil;

            if (html.length == 0 && data != nil) {
                html = [[NSString alloc] initWithData:data encoding:NSISOLatin1StringEncoding];
            }

            NSString *streamURL = XHeroExtractPlayableStreamURLFromText(html);
            if (streamURL.length == 0) {
                XHeroSetError(@"iOS Link 1 iframe HTML did not expose a playable HLS/MP4 stream URL.");
                return;
            }

            NSLog(@"[XHeroWV] Resolved Link 1 iframe to stream=%@", streamURL);
            XHeroStartAVPlayerWithStreamURL(streamURL);
        });
    }];

    [xheroResolveTask resume];
}

static void XHeroCaptureAVPlayerFrame(void) {
    if (xheroPlayer == nil ||
        xheroPlayerItem == nil ||
        xheroVideoOutput == nil ||
        xheroStopping ||
        !xheroNativePlayerReady) {
        return;
    }

    if (xheroPlayerItem.status == AVPlayerItemStatusFailed) {
        NSString *message = xheroPlayerItem.error != nil
            ? xheroPlayerItem.error.localizedDescription
            : @"unknown AVPlayerItem failure";

        XHeroSetError([NSString stringWithFormat:@"iOS AVPlayer item failed: %@", message]);
        return;
    }

    CFTimeInterval hostTime = CACurrentMediaTime();
    CMTime itemTime = [xheroVideoOutput itemTimeForHostTime:hostTime];

    if (![xheroVideoOutput hasNewPixelBufferForItemTime:itemTime]) {
        XHeroUpdateAVPlayerStateWithSize(0, 0);
        return;
    }

    CVPixelBufferRef pixelBuffer = [xheroVideoOutput copyPixelBufferForItemTime:itemTime
                                                             itemTimeForDisplay:NULL];
    if (pixelBuffer == NULL) {
        XHeroUpdateAVPlayerStateWithSize(0, 0);
        return;
    }

    CVPixelBufferLockBaseAddress(pixelBuffer, kCVPixelBufferLock_ReadOnly);

    size_t width = CVPixelBufferGetWidth(pixelBuffer);
    size_t height = CVPixelBufferGetHeight(pixelBuffer);
    size_t stride = CVPixelBufferGetBytesPerRow(pixelBuffer);
    unsigned char *base = (unsigned char *)CVPixelBufferGetBaseAddress(pixelBuffer);

    if (base != NULL && width > 0 && height > 0) {
        size_t rowBytes = width * 4;
        NSMutableData *frame = [NSMutableData dataWithLength:rowBytes * height];
        unsigned char *dst = (unsigned char *)frame.mutableBytes;

        for (size_t y = 0; y < height; y++) {
            memcpy(dst + y * rowBytes, base + y * stride, rowBytes);
        }

        @synchronized ([XHeroWVDelegate class]) {
            xheroLastFrame = frame;
        }

        XHeroUpdateAVPlayerStateWithSize((int)width, (int)height);
    }

    CVPixelBufferUnlockBaseAddress(pixelBuffer, kCVPixelBufferLock_ReadOnly);
    CVPixelBufferRelease(pixelBuffer);
}

// -----------------------------------------------------------------------------
// Snapshot capture
// -----------------------------------------------------------------------------

static void XHeroCaptureFrame(void) {
    if (xheroPlayer != nil) {
        XHeroCaptureAVPlayerFrame();
        return;
    }

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

@implementation XHeroWVResolverDelegate

- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation {
    if (webView != xheroResolverWebView || xheroResolverStopping) {
        return;
    }

    xheroResolverNavigationFinished = YES;
    xheroResolverFailureCount = 0;
    XHeroResolverPoll();
}

- (void)webView:(WKWebView *)webView
didFailNavigation:(WKNavigation *)navigation
      withError:(NSError *)error {
    if (webView != xheroResolverWebView) {
        return;
    }

    XHeroResolverSetError(error != nil
        ? [NSString stringWithFormat:@"iOS Bunny resolver navigation failed: %@", error.localizedDescription]
        : @"iOS Bunny resolver navigation failed.");
}

- (void)webView:(WKWebView *)webView
didFailProvisionalNavigation:(WKNavigation *)navigation
      withError:(NSError *)error {
    if (webView != xheroResolverWebView) {
        return;
    }

    XHeroResolverSetError(error != nil
        ? [NSString stringWithFormat:@"iOS Bunny resolver provisional navigation failed: %@", error.localizedDescription]
        : @"iOS Bunny resolver provisional navigation failed.");
}

- (void)webViewWebContentProcessDidTerminate:(WKWebView *)webView {
    if (webView != xheroResolverWebView || xheroResolverStopping) {
        return;
    }

    XHeroResolverSetError(@"iOS Bunny resolver WebContent process terminated.");
}

@end

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

extern "C" bool XHeroWVResolver_Start(const char *url) {
    if (url == NULL || strlen(url) == 0) {
        XHeroResolverSetError(@"Missing Bunny resolver URL.");
        return false;
    }

    NSString *urlString = [NSString stringWithUTF8String:url];
    NSURL *parsedURL = [NSURL URLWithString:urlString];

    if (urlString.length == 0 || parsedURL == nil) {
        XHeroResolverSetError(@"Invalid Bunny resolver URL.");
        return false;
    }

    __block BOOL started = NO;

    void (^startBlock)(void) = ^{
        XHeroWVResolver_Stop();

        xheroResolverGeneration++;
        xheroResolverStopping = NO;
        xheroResolverNavigationFinished = NO;
        xheroResolverFailureCount = 0;
        xheroResolverCurrentURL = [urlString copy];

        XHeroResolverSetResolvedURL(@"");
        XHeroResolverSetError(@"");

        UIWindow *window = XHeroActiveWindow();
        UIViewController *controller = XHeroTopViewController(window.rootViewController);

        if (window == nil || controller == nil || controller.view == nil) {
            XHeroResolverSetError(@"Cannot find an active iOS window/root view controller for Bunny resolver.");
            return;
        }

        WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
        configuration.allowsInlineMediaPlayback = YES;
        configuration.websiteDataStore = [WKWebsiteDataStore defaultDataStore];
        configuration.applicationNameForUserAgent = @"XHeroLMS";

        if (@available(iOS 10.0, *)) {
            configuration.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeNone;
        }

        CGRect frame = CGRectMake(0.0, 0.0, 2.0, 2.0);
        xheroResolverWebView = [[WKWebView alloc] initWithFrame:frame configuration:configuration];
        xheroResolverWebView.opaque = NO;
        xheroResolverWebView.backgroundColor = UIColor.clearColor;
        xheroResolverWebView.scrollView.backgroundColor = UIColor.clearColor;
        xheroResolverWebView.scrollView.scrollEnabled = NO;
        xheroResolverWebView.scrollView.bounces = NO;
        xheroResolverWebView.userInteractionEnabled = NO;
        xheroResolverWebView.hidden = NO;
        xheroResolverWebView.alpha = 0.01;

        xheroResolverDelegate = [[XHeroWVResolverDelegate alloc] init];
        xheroResolverWebView.navigationDelegate = xheroResolverDelegate;

        [controller.view addSubview:xheroResolverWebView];
        [controller.view sendSubviewToBack:xheroResolverWebView];

        NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:parsedURL
                                                              cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
                                                          timeoutInterval:30.0];

        [request setValue:@"https://iframe.mediadelivery.net/"
       forHTTPHeaderField:@"Referer"];

        [xheroResolverWebView loadRequest:request];
        XHeroResolverRestartTimer();

        started = YES;
    };

    if ([NSThread isMainThread]) {
        startBlock();
    } else {
        dispatch_sync(dispatch_get_main_queue(), startBlock);
    }

    return started;
}

extern "C" void XHeroWVResolver_Stop(void) {
    if ([NSThread isMainThread]) {
        XHeroResolverStopOnMainThread();
    } else {
        dispatch_sync(dispatch_get_main_queue(), ^{
            XHeroResolverStopOnMainThread();
        });
    }
}

extern "C" const char *XHeroWVResolver_GetResolvedUrl(void) {
    return XHeroResolverCopyResolvedURLCString();
}

extern "C" const char *XHeroWVResolver_GetLastError(void) {
    return XHeroResolverCopyErrorCString();
}

extern "C" bool XHeroWV_Start(const char *url, int width, int height, int fps) {
    if (url == NULL || strlen(url) == 0) {
        XHeroSetError(@"Missing Link 1 URL.");
        return false;
    }

    NSString *urlString = [NSString stringWithUTF8String:url];
    NSURL *parsedURL = [NSURL URLWithString:urlString];

    if (urlString.length == 0 || parsedURL == nil) {
        XHeroSetError(@"Invalid Link 1 URL.");
        return false;
    }

    __block BOOL started = NO;

    void (^startBlock)(void) = ^{
        XHeroWV_Stop();

        xheroGeneration++;
        xheroStopping = NO;

        // Start with the dimensions/FPS requested by Unity. AVPlayer replaces
        // these with the decoded source size as soon as frames are available.
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

        XHeroResolveLink1AndStartAVPlayer(urlString);
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

        XHeroStopAVPlayer();

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

extern "C" void XHeroWV_Play(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (xheroPlayer != nil && !xheroStopping) {
            [xheroPlayer play];
            XHeroUpdateAVPlayerStateWithSize(0, 0);
        }
    });
}

extern "C" void XHeroWV_Pause(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (xheroPlayer != nil) {
            [xheroPlayer pause];
            XHeroUpdateAVPlayerStateWithSize(0, 0);
        }
    });
}

extern "C" void XHeroWV_Seek(double time) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (xheroPlayer == nil || !isfinite(time) || time < 0.0) {
            return;
        }

        CMTime target = CMTimeMakeWithSeconds(time, 600);
        [xheroPlayer seekToTime:target
                toleranceBefore:kCMTimeZero
                 toleranceAfter:kCMTimeZero
              completionHandler:^(BOOL finished) {
            XHeroUpdateAVPlayerStateWithSize(0, 0);
        }];
    });
}

extern "C" void XHeroWV_SetVolume(float volume) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (xheroPlayer != nil) {
            float clamped = fmaxf(0.0f, fminf(1.0f, volume));
            xheroPlayer.volume = clamped;
            xheroPlayer.muted = clamped <= 0.0001f;
            XHeroUpdateAVPlayerStateWithSize(0, 0);
        }
    });
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
        XHeroSetError(@"Cannot allocate memory for the iOS native video frame.");
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

    if (xheroPlayer != nil) {
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
    if (xheroPlayer != nil) {
        XHeroUpdateAVPlayerStateWithSize(0, 0);
        return;
    }

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
