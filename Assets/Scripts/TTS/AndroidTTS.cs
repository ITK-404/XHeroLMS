#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AndroidTTS
{
    static AndroidJavaObject tts;
    static AndroidJavaObject activity;
    static bool ready;
    static string lang = "vi";
    static string country = "VN";

    public static void Init()
    {
        if (tts != null) return;

        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // new TextToSpeech(context, OnInitListener)
        var ttsClass = new AndroidJavaClass("android.speech.tts.TextToSpeech");
        var listener = new OnInitListener(code =>
        {
            ready = (code == 0);
            if (ready) SetLanguage(lang, country);
        });

        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
    }

    public static void SetLanguage(string language, string region)
    {
        lang = language; country = region;
        if (tts == null) return;

        using var locale = new AndroidJavaObject("java.util.Locale", language, region);
        tts.Call<int>("setLanguage", locale);
    }

    public static void SetRatePitch(float rate, float pitch)
    {
        if (tts == null) return;
        tts.Call<int>("setSpeechRate", Mathf.Clamp(rate, 0.2f, 2.0f));
        tts.Call<int>("setPitch", Mathf.Clamp(pitch, 0.5f, 2.0f));
    }

    public static void Stop()
    {
        if (tts == null) return;
        tts.Call<int>("stop");
    }

    public static void SpeakParts(List<TTSManager.SpeakPart> parts)
    {
        if (tts == null || !ready) return;

        int idx = 0;
        foreach (var p in parts)
        {
            string utterId = "utt_" + (idx++);
            SpeakInternal(p.text, utterId);

            if (p.pauseMsAfter > 0)
            {
                // play silent
                try
                {
                    tts.Call<int>("playSilentUtterance", (long)p.pauseMsAfter, 1 /*QUEUE_ADD*/, "sil_" + utterId);
                }
                catch
                {
                    // fallback: do nothing (or add "..." into text before parsing)
                }
            }
        }
    }

    static void SpeakInternal(string text, string utterId)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // speak(text, QUEUE_ADD, params, utteranceId) on API 21+
        using var bundle = new AndroidJavaObject("android.os.Bundle");
        bundle.Call("putString", "utteranceId", utterId);

        try
        {
            tts.Call<int>("speak", text, 1 /*QUEUE_ADD*/, bundle, utterId);
        }
        catch
        {
            // old API speak(text, QUEUE_ADD, HashMap)
            using var map = new AndroidJavaObject("java.util.HashMap");
            map.Call("put", "utteranceId", utterId);
            tts.Call<int>("speak", text, 1 /*QUEUE_ADD*/, map);
        }
    }

    class OnInitListener : AndroidJavaProxy
    {
        readonly Action<int> cb;
        public OnInitListener(Action<int> cb) : base("android.speech.tts.TextToSpeech$OnInitListener") { this.cb = cb; }
        void onInit(int status) => cb?.Invoke(status);
    }
}
#endif
