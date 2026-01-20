#import <UIKit/UIKit.h>

extern "C"
{
    bool _CanOpenURL(const char* url)
    {
        NSString* nsUrl =
            [NSString stringWithUTF8String:url];
        NSURL* testUrl = [NSURL URLWithString:nsUrl];

        return [[UIApplication sharedApplication] canOpenURL:testUrl];
    }
}