using System.Collections.Generic;
using System.Net;
using System.Text;

public static class TMPMailTextFormatter
{
    private static readonly Dictionary<string, string> IconMap = new()
    {
        // Backend tags
        ["[red_dot]"] = "red_dot",
        ["[red_circle]"] = "red_dot",
        ["[red_triangle]"] = "red_triangle",
        ["[heart]"] = "heart",
        ["[clock]"] = "clock",
        ["[trash]"] = "trash",
        ["[check]"] = "check",
        ["[fail]"] = "fail",
        ["[warning]"] = "warning",
        ["[gift]"] = "gift",
        ["[star]"] = "star",
        ["[fire]"] = "fire",
        ["[coin]"] = "coin",
        ["[video]"] = "video",
        ["[mail]"] = "mail",
        ["[link]"] = "link",

        // Emoji / symbols
        ["🔴"] = "red_dot",
        ["●"] = "red_dot",

        ["🔻"] = "red_triangle",
        ["▼"] = "red_triangle",
        ["▾"] = "red_triangle",

        ["❤️"] = "heart",
        ["❤"] = "heart",
        ["♥"] = "heart",

        ["⏰"] = "clock",
        ["🕒"] = "clock",
        ["🕐"] = "clock",
        ["⌚"] = "clock",

        ["✅"] = "check",
        ["✔"] = "check",
        ["☑"] = "check",

        ["❌"] = "fail",
        ["✖"] = "fail",

        ["⚠"] = "warning",

        ["🎁"] = "gift",
        ["⭐"] = "star",
        ["🌟"] = "star",
        ["🔥"] = "fire",
        ["💰"] = "coin",
        ["🪙"] = "coin",
        ["🎥"] = "video",
        ["🎬"] = "video",
        ["📌"] = "pin",
        ["📍"] = "pin",
        ["📢"] = "announce",
        ["🔔"] = "bell",
        ["📩"] = "mail",
        ["✉"] = "mail",
        ["🔗"] = "link",
        ["🗑"] = "trash"
    };

    public static string Format(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        string text = WebUtility.HtmlDecode(raw);

        // Chuẩn hóa emoji có variation selector.
        text = RemoveVariationSelectors(text);

        foreach (var pair in IconMap)
        {
            text = text.Replace(pair.Key, ToSprite(pair.Value));
        }

        return text;
    }

    private static string ToSprite(string spriteName)
    {
        return $"<sprite name=\"{spriteName}\">";
    }

    private static string RemoveVariationSelectors(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        StringBuilder sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            // FE0E = text style variation selector
            // FE0F = emoji style variation selector
            if (c == '\uFE0E' || c == '\uFE0F')
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }
}