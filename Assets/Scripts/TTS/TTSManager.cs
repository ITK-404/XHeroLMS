using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TTSManager : MonoBehaviour
{
    public static TTSManager I;

    [Header("Default Vietnamese tone")]
    [Range(0.2f, 2.0f)] public float rate = 1.0f;   // iOS: ~0.4..0.6 là dễ nghe; Android tùy máy
    [Range(0.5f, 2.0f)] public float pitch = 1.0f;  // 1.0 = bình thường

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
        iOSTTS_SetRatePitch(rate, pitch);
#endif
    }

    public void SetRatePitch(float newRate, float newPitch)
    {
        rate = newRate; pitch = newPitch;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.SetRatePitch(rate, pitch);
#elif UNITY_IOS && !UNITY_EDITOR
        iOSTTS_SetRatePitch(rate, pitch);
#endif
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

        var parts = ParseTextWithPauses(text); // list of (string segment, int pauseMsAfter)
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

        // First split by explicit [pause=ms]
        var rx = new Regex(@"\[pause\s*=\s*(\d+)\s*\]", RegexOptions.IgnoreCase);
        int last = 0;
        foreach (Match m in rx.Matches(input))
        {
            var chunk = input.Substring(last, m.Index - last);
            AddChunkWithImplicitPauses(chunk, parts);

            int p = int.Parse(m.Groups[1].Value);
            // attach pause to previous part if exists
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

        // remove empty
        parts.RemoveAll(p => string.IsNullOrWhiteSpace(p.text));
        return parts;
    }

    static void AddChunkWithImplicitPauses(string chunk, List<SpeakPart> parts)
    {
        if (string.IsNullOrWhiteSpace(chunk)) return;

        // Split into sentences-ish while keeping punctuation
        // Very simple approach: walk char by char
        string acc = "";
        for (int i = 0; i < chunk.Length; i++)
        {
            char c = chunk[i];
            acc += c;

            int pause = 0;
            if (c == ',') pause = 200;
            if (c == ';' || c == ':') pause = 250;

            // handle ellipsis ...
            if (c == '.' && i + 2 < chunk.Length && chunk[i + 1] == '.' && chunk[i + 2] == '.')
            {
                acc += "..";
                i += 2;
                pause = 600;
            }
            else if (c == '.' || c == '!' || c == '?' || c == '\n')
            {
                pause = 400;
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
    // iOS native bindings
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
