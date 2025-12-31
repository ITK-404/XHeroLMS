#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *synth;
static NSString *voiceLocale = @"vi-VN";
static float gRate = 0.50f;   // iOS: 0.4~0.6 thường ổn
static float gPitch = 1.0f;

extern "C" {

void iOSTTS_Init() {
    if (!synth) synth = [AVSpeechSynthesizer new];
}

void iOSTTS_SetVoice(const char* locale) {
    if (locale) voiceLocale = [NSString stringWithUTF8String:locale];
}

void iOSTTS_SetRatePitch(float rate, float pitch) {
    gRate = rate;
    gPitch = pitch;
}

void iOSTTS_Stop() {
    if (!synth) return;
    [synth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}

static void SpeakOne(NSString *text, int pauseMsAfter) {
    if (!synth || text.length == 0) return;

    AVSpeechUtterance *utt = [AVSpeechUtterance speechUtteranceWithString:text];
    AVSpeechSynthesisVoice *voice = [AVSpeechSynthesisVoice voiceWithLanguage:voiceLocale];
    if (voice) utt.voice = voice;

    utt.rate = MAX(0.1f, MIN(gRate, 0.6f));
    utt.pitchMultiplier = MAX(0.5f, MIN(gPitch, 2.0f));

    // Rhythm controls
    utt.preUtteranceDelay = 0.0;
    utt.postUtteranceDelay = MAX(0.0, pauseMsAfter / 1000.0);

    [synth speakUtterance:utt];
}

void iOSTTS_Speak(const char* text) {
    iOSTTS_Init();
    if (!text) return;
    NSString *s = [NSString stringWithUTF8String:text];
    SpeakOne(s, 0);
}

void iOSTTS_SpeakPart(const char* text, int pauseMsAfter) {
    iOSTTS_Init();
    if (!text) return;
    NSString *s = [NSString stringWithUTF8String:text];
    SpeakOne(s, pauseMsAfter);
}

}
