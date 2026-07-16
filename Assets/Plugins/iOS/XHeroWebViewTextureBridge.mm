#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

@interface XHeroWVDelegate : NSObject <WKNavigationDelegate>
@end

static WKWebView *xheroWebView = nil;
static XHeroWVDelegate *xheroDelegate = nil;
static NSTimer *xheroFrameTimer = nil;
static NSData *xheroLastFrame = nil;
static NSString *xheroState = @"0|0|0|0|0|0";
static NSString *xheroLastError = @"";
static int xheroWidth = 1920;
static int xheroHeight = 1080;
static int xheroFps = 30;

extern "C" void XHeroWV_Stop(void);

static const char *XHeroCopyCString(NSString *value) {
    if (value == nil) {
        value = @"";
    }

    return [value UTF8String];
}

static void XHeroInjectVideoPatch(void) {
    if (xheroWebView == nil) {
        return;
    }

    NSMutableString *script = [NSMutableString stringWithString:
    @"(function(){"
    @"if(window.xheroVideoPatchInstalled!==true){window.xheroVideoPatchInstalled=true;window.xheroVideoCanvasLoop=false;window.xheroUserPaused=false;window.xheroAutoplayStarted=false;window.xheroEstimatedFps=0;window.xheroFrameCount=0;window.xheroFrameTime=0;}"
    @"var style=document.getElementById('xhero-video-css');"
    @"if(!style){style=document.createElement('style');style.id='xhero-video-css';document.head.appendChild(style);}"
    @"style.textContent='html,body{margin:0!important;padding:0!important;overflow:hidden!important;background:#000!important;}'+"
    @"'video{position:absolute!important;inset:0!important;width:100%!important;height:100%!important;object-fit:contain!important;background:#000!important;opacity:.01!important;pointer-events:none!important;}'+"
    @"'#xhero-video-canvas{position:fixed!important;left:0!important;top:0!important;width:100vw!important;height:100vh!important;background:#000!important;z-index:2147483647!important;pointer-events:none!important;}'+"
    @"'.plyr__controls,.plyr__control,.plyr__menu,.plyr__progress,.plyr__volume,.plyr__poster,.plyr__control--overlaid{display:none!important;}'+"
    @"'video::-webkit-media-controls{display:none!important;}';"
    @"var c=document.getElementById('xhero-video-canvas');"
    @"if(!c){c=document.createElement('canvas');c.id='xhero-video-canvas';(document.body||document.documentElement).appendChild(c);}"
    ];

    [script appendFormat:@"function rz(){var w=%d;var h=%d;if(c.width!==w||c.height!==h){c.width=w;c.height=h;}}", xheroWidth, xheroHeight];
    [script appendString:
    @"rz();"
    @"var v=document.querySelector('video');"
    @"if(v){v.controls=false;v.playsInline=true;v.autoplay=true;v.muted=false;if(window.xheroUserPaused!==true&&window.xheroAutoplayStarted!==true){window.xheroAutoplayStarted=true;v.play().catch(function(){});}}"
    @"if(!window.xheroVideoCanvasLoop){window.xheroVideoCanvasLoop=true;(function loop(){try{rz();var cv=document.getElementById('xhero-video-canvas');var vv=document.querySelector('video');if(cv&&vv&&vv.readyState>=2){var now=(window.performance&&performance.now)?performance.now():Date.now();var q=vv.getVideoPlaybackQuality?vv.getVideoPlaybackQuality():null;if(q&&q.totalVideoFrames>0){if(!window.xheroFrameTime){window.xheroFrameTime=now;window.xheroFrameCount=q.totalVideoFrames;}else if(now-window.xheroFrameTime>=1000){var f=(q.totalVideoFrames-window.xheroFrameCount)*1000/(now-window.xheroFrameTime);if(f>=10&&f<=60){window.xheroEstimatedFps=f;}window.xheroFrameTime=now;window.xheroFrameCount=q.totalVideoFrames;}}var ctx=cv.getContext('2d',{alpha:false});ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality='high';ctx.fillStyle='#000';ctx.fillRect(0,0,cv.width,cv.height);var vw=vv.videoWidth||cv.width;var vh=vv.videoHeight||cv.height;var s=Math.min(cv.width/vw,cv.height/vh);var dw=vw*s;var dh=vh*s;var dx=(cv.width-dw)/2;var dy=(cv.height-dh)/2;ctx.drawImage(vv,dx,dy,dw,dh);}}catch(e){}requestAnimationFrame(loop);})();}"
    @"})()"
    ];

    [xheroWebView evaluateJavaScript:script completionHandler:nil];
}

static void XHeroCaptureFrame(void) {
    if (xheroWebView == nil) {
        return;
    }

    XHeroInjectVideoPatch();

    WKSnapshotConfiguration *config = [[WKSnapshotConfiguration alloc] init];
    config.rect = CGRectMake(0, 0, xheroWidth, xheroHeight);

    [xheroWebView takeSnapshotWithConfiguration:config completionHandler:^(UIImage *snapshot, NSError *error) {
        if (snapshot == nil || error != nil) {
            return;
        }

        NSData *png = UIImagePNGRepresentation(snapshot);
        if (png != nil) {
            @synchronized ([XHeroWVDelegate class]) {
                xheroLastFrame = png;
            }
        }
    }];
}

@implementation XHeroWVDelegate
- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation {
    XHeroInjectVideoPatch();
}

- (void)webView:(WKWebView *)webView didFailNavigation:(WKNavigation *)navigation withError:(NSError *)error {
    xheroLastError = error != nil ? error.localizedDescription : @"iOS WKWebView load failed.";
}

- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(WKNavigation *)navigation withError:(NSError *)error {
    xheroLastError = error != nil ? error.localizedDescription : @"iOS WKWebView provisional load failed.";
}
@end

extern "C" bool XHeroWV_Start(const char *url, int width, int height, int fps) {
    if (url == NULL) {
        xheroLastError = @"Missing WebView URL.";
        return false;
    }

    dispatch_async(dispatch_get_main_queue(), ^{
        XHeroWV_Stop();

        xheroWidth = MAX(320, MIN(1920, width));
        xheroHeight = MAX(180, MIN(1080, height));
        xheroFps = MAX(5, MIN(30, fps));
        xheroState = @"0|0|0|0|0|0";
        xheroLastError = @"";

        WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
        configuration.allowsInlineMediaPlayback = YES;
        if (@available(iOS 10.0, *)) {
            configuration.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeNone;
        }

        CGRect frame = CGRectMake(-xheroWidth - 64, 0, xheroWidth, xheroHeight);
        xheroWebView = [[WKWebView alloc] initWithFrame:frame configuration:configuration];
        xheroWebView.opaque = NO;
        xheroWebView.backgroundColor = UIColor.blackColor;
        xheroWebView.scrollView.scrollEnabled = NO;
        xheroWebView.scrollView.bounces = NO;

        xheroDelegate = [[XHeroWVDelegate alloc] init];
        xheroWebView.navigationDelegate = xheroDelegate;

        UIViewController *controller = UIApplication.sharedApplication.keyWindow.rootViewController;
        [controller.view addSubview:xheroWebView];

        NSString *urlString = [NSString stringWithUTF8String:url];
        NSURL *nsUrl = [NSURL URLWithString:urlString];
        [xheroWebView loadRequest:[NSURLRequest requestWithURL:nsUrl]];

        NSTimeInterval interval = 1.0 / (double)xheroFps;
        xheroFrameTimer = [NSTimer scheduledTimerWithTimeInterval:interval repeats:YES block:^(NSTimer *timer) {
            XHeroCaptureFrame();
        }];
    });

    return true;
}

extern "C" void XHeroWV_Stop(void) {
    if (![NSThread isMainThread]) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XHeroWV_Stop();
        });
        return;
    }

    if (xheroFrameTimer != nil) {
        [xheroFrameTimer invalidate];
        xheroFrameTimer = nil;
    }

    if (xheroWebView != nil) {
        xheroWebView.navigationDelegate = nil;
        [xheroWebView stopLoading];
        [xheroWebView removeFromSuperview];
        xheroWebView = nil;
    }

    xheroDelegate = nil;
    xheroLastFrame = nil;
    xheroState = @"0|0|0|0|0|0";
}

extern "C" unsigned char *XHeroWV_CopyFrame(void) {
    NSData *frame = nil;
    @synchronized ([XHeroWVDelegate class]) {
        frame = xheroLastFrame;
        xheroLastFrame = nil;
    }

    if (frame == nil || frame.length == 0) {
        return NULL;
    }

    unsigned char *copy = (unsigned char *)malloc(frame.length);
    memcpy(copy, frame.bytes, frame.length);
    return copy;
}

extern "C" int XHeroWV_GetFrameLength(void) {
    @synchronized ([XHeroWVDelegate class]) {
        return xheroLastFrame != nil ? (int)xheroLastFrame.length : 0;
    }
}

extern "C" void XHeroWV_ReleaseFrame(void *frame) {
    if (frame != NULL) {
        free(frame);
    }
}

extern "C" void XHeroWV_Evaluate(const char *script) {
    if (script == NULL || xheroWebView == nil) {
        return;
    }

    NSString *js = [NSString stringWithUTF8String:script];
    dispatch_async(dispatch_get_main_queue(), ^{
        [xheroWebView evaluateJavaScript:js completionHandler:nil];
    });
}

extern "C" void XHeroWV_RequestState(void) {
    if (xheroWebView == nil) {
        return;
    }

    NSString *script =
    @"(function(){"
    @"var v=document.querySelector('video');"
    @"if(v){return [(v.currentTime||0),(isFinite(v.duration)?v.duration:0),(!v.paused?1:0),(window.xheroEstimatedFps||0),(v.videoWidth||0),(v.videoHeight||0)].join('|');}"
    @"return '0|0|0|0|0|0';"
    @"})()";

    dispatch_async(dispatch_get_main_queue(), ^{
        [xheroWebView evaluateJavaScript:script completionHandler:^(id result, NSError *error) {
            if ([result isKindOfClass:[NSString class]]) {
                xheroState = (NSString *)result;
            }
        }];
    });
}

extern "C" const char *XHeroWV_GetState(void) {
    return XHeroCopyCString(xheroState);
}

extern "C" const char *XHeroWV_GetLastError(void) {
    return XHeroCopyCString(xheroLastError);
}
