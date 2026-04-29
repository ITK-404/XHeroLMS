using System.Collections.Generic;
using System.Net;
using System.Text;

public static class TMPMailTextFormatter
{
    private static readonly Dictionary<string, string> IconMap = new()
    {
        // Backend tags
        ["[red_dot]"] = "<color=#E00000>o</color>",
        ["[red_circle]"] = "<color=#E00000>o</color>",
        ["[red_triangle]"] = "<color=#E00000>v</color>",
        ["[heart]"] = "<color=#E00000><3</color>",
        ["[clock]"] = "<color=#888888>@</color>",
        ["[trash]"] = "<color=#888888>x</color>",
        ["[check]"] = "<color=#00A651>v</color>",
        ["[fail]"] = "<color=#E00000>x</color>",
        ["[warning]"] = "<color=#F2A900>!</color>",
        ["[gift]"] = "<color=#F2A900>#</color>",
        ["[star]"] = "<color=#F2C200>*</color>",
        ["[sparkle]"] = "<color=#F2C200>*</color>",
        ["[sparkles]"] = "<color=#F2C200>*</color>",
        ["[fire]"] = "<color=#FF5A00>^</color>",
        ["[coin]"] = "<color=#F2C200>o</color>",
        ["[video]"] = ">",
        ["[mail]"] = "@",
        ["[link]"] = ">",

        // Emoji / symbols từ API
        ["🔴"] = "<color=#E00000>o</color>",
        ["●"] = "<color=#E00000>o</color>",

        ["🔻"] = "<color=#E00000>v</color>",
        ["▼"] = "<color=#E00000>v</color>",
        ["▾"] = "<color=#E00000>v</color>",

        ["❤️"] = "<color=#E00000><3</color>",
        ["❤"] = "<color=#E00000><3</color>",
        ["♥"] = "<color=#E00000><3</color>",

        ["⏰"] = "<color=#888888>@</color>",
        ["🕒"] = "<color=#888888>@</color>",
        ["🕐"] = "<color=#888888>@</color>",
        ["⌚"] = "<color=#888888>@</color>",

        ["✅"] = "<color=#00A651>v</color>",
        ["✔"] = "<color=#00A651>v</color>",
        ["☑"] = "<color=#00A651>v</color>",

        ["❌"] = "<color=#E00000>x</color>",
        ["✖"] = "<color=#E00000>x</color>",

        ["⚠"] = "<color=#F2A900>!</color>",

        // Star / Sparkle
        ["⭐"] = "<color=#F2C200>*</color>",
        ["🌟"] = "<color=#F2C200>*</color>",
        ["✨"] = "<color=#F2C200>*</color>",
        ["💫"] = "<color=#F2C200>*</color>",
        ["🌠"] = "<color=#F2C200>*</color>",
        ["★"] = "<color=#F2C200>*</color>",
        ["☆"] = "<color=#F2C200>*</color>",
        ["✦"] = "<color=#F2C200>*</color>",
        ["✧"] = "<color=#F2C200>*</color>",

        // Common icons
        ["🎁"] = "<color=#F2A900>#</color>",
        ["🔥"] = "<color=#FF5A00>^</color>",
        ["💰"] = "<color=#F2C200>o</color>",
        ["🪙"] = "<color=#F2C200>o</color>",
        ["🎥"] = ">",
        ["🎬"] = ">",

        ["📌"] = "<color=#E00000>*</color>",
        ["📍"] = "<color=#E00000>*</color>",

        ["📢"] = "<color=#F2A900>!</color>",
        ["🔔"] = "<color=#F2A900>!</color>",

        ["📩"] = "@",
        ["✉"] = "@",

        ["🔗"] = ">",
        ["🗑"] = "x"
    };

    public static string Format(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        string text = WebUtility.HtmlDecode(raw);
        text = RemoveVariationSelectors(text);

        foreach (var pair in IconMap)
            text = text.Replace(pair.Key, pair.Value);

        return text;
    }

    private static string RemoveVariationSelectors(string input)
    {
        StringBuilder sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (c == '\uFE0E' || c == '\uFE0F')
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }
}