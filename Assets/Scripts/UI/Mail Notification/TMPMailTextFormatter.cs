using System.Collections.Generic;
using System.Net;
using System.Text;

public static class TMPMailTextFormatter
{
    private static readonly Dictionary<string, string> IconMap = new()
    {
        // ======================
        // Backend tags
        // ======================
        ["[red_dot]"] = "<color=#E00000>●</color>",
        ["[red_circle]"] = "<color=#E00000>●</color>",
        ["[red_triangle]"] = "<color=#E00000>▼</color>",
        ["[heart]"] = "<color=#E00000>♥</color>",
        ["[clock]"] = "◷",
        ["[trash]"] = "⌫",
        ["[check]"] = "<color=#00A651>✓</color>",
        ["[fail]"] = "<color=#E00000>✕</color>",
        ["[warning]"] = "<color=#F2A900>⚠</color>",
        ["[gift]"] = "▣",
        ["[star]"] = "<color=#F2C200>★</color>",
        ["[sparkle]"] = "<color=#F2C200>✦</color>",
        ["[sparkles]"] = "<color=#F2C200>✦</color>",
        ["[fire]"] = "<color=#FF5A00>▲</color>",
        ["[coin]"] = "<color=#F2C200>●</color>",
        ["[video]"] = "▻",
        ["[mail]"] = "✉",
        ["[link]"] = "↗",

        // ======================
        // Red indicators
        // ======================
        ["🔴"] = "<color=#E00000>●</color>",
        ["●"] = "<color=#E00000>●</color>",

        ["🔻"] = "<color=#E00000>▼</color>",
        ["▼"] = "<color=#E00000>▼</color>",
        ["▾"] = "<color=#E00000>▼</color>",

        // ======================
        // Heart
        // ======================
        ["❤️"] = "<color=#E00000>♥</color>",
        ["❤"] = "<color=#E00000>♥</color>",
        ["♥"] = "<color=#E00000>♥</color>",

        // ======================
        // Time
        // ======================
        ["⏰"] = "◷",
        ["🕒"] = "◷",
        ["🕐"] = "◷",
        ["⌚"] = "◷",

        // ======================
        // Status
        // ======================
        ["✅"] = "<color=#00A651>✓</color>",
        ["✔"] = "<color=#00A651>✓</color>",
        ["☑"] = "<color=#00A651>✓</color>",

        ["❌"] = "<color=#E00000>✕</color>",
        ["✖"] = "<color=#E00000>✕</color>",

        ["⚠"] = "<color=#F2A900>⚠</color>",

        // ======================
        // Star / Sparkle (🔥 CASE CỦA BẠN)
        // ======================
        ["⭐"] = "<color=#F2C200>★</color>",
        ["🌟"] = "<color=#F2C200>★</color>",
        ["✨"] = "<color=#F2C200>✦</color>",
        ["💫"] = "<color=#F2C200>✦</color>",
        ["🌠"] = "<color=#F2C200>✦</color>",
        ["★"] = "<color=#F2C200>★</color>",
        ["☆"] = "<color=#F2C200>★</color>",
        ["✦"] = "<color=#F2C200>✦</color>",
        ["✧"] = "<color=#F2C200>✦</color>",

        // ======================
        // Common icons
        // ======================
        ["🎁"] = "▣",
        ["🔥"] = "<color=#FF5A00>▲</color>",
        ["💰"] = "<color=#F2C200>●</color>",
        ["🪙"] = "<color=#F2C200>●</color>",
        ["🎥"] = "▻",
        ["🎬"] = "▻",

        ["📌"] = "<color=#E00000>◆</color>",
        ["📍"] = "<color=#E00000>◆</color>",

        ["📢"] = "!",
        ["🔔"] = "!",

        ["📩"] = "✉",
        ["✉"] = "✉",

        ["🔗"] = "↗",
        ["🗑"] = "⌫"
    };

    public static string Format(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        // Decode HTML
        string text = WebUtility.HtmlDecode(raw);

        // Remove emoji variation selectors
        text = RemoveVariationSelectors(text);

        // Replace icons
        foreach (var pair in IconMap)
        {
            text = text.Replace(pair.Key, pair.Value);
        }

        return text;
    }

    private static string RemoveVariationSelectors(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        StringBuilder sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            // Remove FE0E / FE0F
            if (c == '\uFE0E' || c == '\uFE0F')
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }
}