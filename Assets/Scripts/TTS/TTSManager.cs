using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TTSManager : MonoBehaviour
{
    public static TTSManager I;

    [Range(0.2f, 2.0f)] float rate = 1.0f;   // Android scale
    [Range(0.5f, 2.0f)] float pitch = 1.0f;

    [Range(0.3f, 1.0f)] float iosRateMultiplier = 0.6f;

    [Range(0.2f, 0.9f)] float iosRateMin = 0.40f;

    [Range(0.2f, 0.9f)] float iosRateMax = 0.60f;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.Init();
        AndroidTTS.SetLanguage("vi", "VN"); // vi-VN
        AndroidTTS.SetRatePitch(rate, pitch);
#elif UNITY_IOS && !UNITY_EDITOR
        iOSTTS_Init();
        iOSTTS_SetVoice("vi-VN");
        ApplyIOSRatePitch(); // dùng mapping riêng cho iOS
#endif
    }

    public void SetRatePitch(float newRate, float newPitch)
    {
        rate = newRate;
        pitch = newPitch;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.SetRatePitch(rate, pitch);
#elif UNITY_IOS && !UNITY_EDITOR
        ApplyIOSRatePitch();
#endif
    }

    void ApplyIOSRatePitch()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // rate (Android-like 0.2..2.0) -> iOS AVSpeechUtteranceRate-ish 0.4..0.6 (dễ nghe)
        float r = MapAndroidRateToIOS(rate) * iosRateMultiplier;
        r = Mathf.Clamp(r, iosRateMin, iosRateMax);

        float p = Mathf.Clamp(pitch, 0.5f, 2.0f); // iOS pitchMultiplier thường ok 0.5..2.0

        iOSTTS_SetRatePitch(r, p);
#endif
    }

    // Map thô: Android 0.2..2.0 -> iOS 0.35..0.70 (sau đó multiplier + clamp)
    static float MapAndroidRateToIOS(float androidRate)
    {
        float a = Mathf.Clamp(androidRate, 0.2f, 2.0f);
        float t = Mathf.InverseLerp(0.2f, 2.0f, a);
        return Mathf.Lerp(0.35f, 0.70f, t);
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.Stop();
#elif UNITY_IOS && !UNITY_EDITOR
        iOSTTS_Stop();
#endif
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var parts = ParseTextWithPauses(text);

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.SpeakParts(parts);
#elif UNITY_IOS && !UNITY_EDITOR
        iOSTTS_SpeakParts(parts);
#else
        Debug.Log("[TTS] " + text);
#endif
    }

    // ------------------ Rhythm parser ------------------
    public struct SpeakPart
    {
        public string text;
        public int pauseMsAfter;
        public SpeakPart(string t, int p) { text = t; pauseMsAfter = p; }
    }

    static List<SpeakPart> ParseTextWithPauses(string input)
    {
        var parts = new List<SpeakPart>();

        var rx = new Regex(@"\[pause\s*=\s*(\d+)\s*\]", RegexOptions.IgnoreCase);
        int last = 0;
        foreach (Match m in rx.Matches(input))
        {
            var chunk = input.Substring(last, m.Index - last);
            AddChunkWithImplicitPauses(chunk, parts);

            int p = int.Parse(m.Groups[1].Value);
            if (parts.Count > 0)
            {
                var prev = parts[parts.Count - 1];
                prev.pauseMsAfter = Math.Max(prev.pauseMsAfter, p);
                parts[parts.Count - 1] = prev;
            }
            last = m.Index + m.Length;
        }

        var tail = input.Substring(last);
        AddChunkWithImplicitPauses(tail, parts);

        parts.RemoveAll(p => string.IsNullOrWhiteSpace(p.text));
        return parts;
    }

    static void AddChunkWithImplicitPauses(string chunk, List<SpeakPart> parts)
    {
        if (string.IsNullOrWhiteSpace(chunk)) return;

        string acc = "";
        for (int i = 0; i < chunk.Length; i++)
        {
            char c = chunk[i];
            acc += c;

            int pause = 0;
            if (c == ',') pause = 100;
            if (c == ';' || c == ':') pause = 125;

            if (c == '.' && i + 2 < chunk.Length && chunk[i + 1] == '.' && chunk[i + 2] == '.')
            {
                acc += "..";
                i += 2;
                pause = 300;
            }
            else if (c == '.' || c == '!' || c == '?' || c == '\n')
            {
                pause = 200;
            }

            if (pause > 0)
            {
                parts.Add(new SpeakPart(acc.Trim(), pause));
                acc = "";
            }
        }

        if (!string.IsNullOrWhiteSpace(acc))
            parts.Add(new SpeakPart(acc.Trim(), 0));
    }

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_Init();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_SetVoice(string locale);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_SetRatePitch(float rate, float pitch);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_Stop();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_Speak(string text);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void iOSTTS_SpeakPart(string text, int pauseMsAfter);

    static void iOSTTS_SpeakParts(List<SpeakPart> parts)
    {
        foreach (var p in parts)
            iOSTTS_SpeakPart(p.text, p.pauseMsAfter);
    }
#endif
}
