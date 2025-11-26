using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class SrtReader
{
    // Splits entries by one or more blank lines
    private static readonly Regex EntrySplitRegex = new(@"\r?\n\s*\r?\n", RegexOptions.Compiled);
    private static readonly Regex TimeLineRegex = new(@"\s*(\d{1,2}:\d{2}:\d{2}[.,]\d{1,3})\s*-->\s*(\d{1,2}:\d{2}:\d{2}[.,]\d{1,3})", RegexOptions.Compiled);

    public static List<SrtEntry> Parse(TextAsset asset)
    {
        if (asset == null) return new List<SrtEntry>();
        return Parse(asset.text);
    }

    public static List<SrtEntry> Parse(string text)
    {
        var result = new List<SrtEntry>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        var blocks = EntrySplitRegex.Split(text.Trim());

        foreach (var rawBlock in blocks)
        {
            var lines = rawBlock.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) continue;

            // find the line containing the time-range
            string timeLine = null;
            int timeLineIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("-->"))
                {
                    timeLine = lines[i].Trim();
                    timeLineIndex = i;
                    break;
                }
            }
            if (timeLine == null) continue;

            var m = TimeLineRegex.Match(timeLine);
            if (!m.Success) continue;

            var startSec = ParseTimeToSeconds(m.Groups[1].Value);
            var endSec = ParseTimeToSeconds(m.Groups[2].Value);

            // remaining lines after the time-line are the subtitle text
            var textLines = new List<string>();
            for (int i = timeLineIndex + 1; i < lines.Length; i++)
                textLines.Add(lines[i].TrimEnd());

            var content = string.Join("\n", textLines);

            result.Add(new SrtEntry
            {
                Start = startSec,
                End = endSec,
                Text = content
            });
        }

        return result;
    }

    // Parses an SRT time token like "00:00:01,560" into seconds (float).
    private static float ParseTimeToSeconds(string token)
    {
        // normalize separators
        token = token.Trim();
        var parts = token.Split(new[] { ':', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return 0f;

        if (!int.TryParse(parts[0], out var hh)) hh = 0;
        if (!int.TryParse(parts[1], out var mm)) mm = 0;
        if (!int.TryParse(parts[2], out var ss)) ss = 0;
        var msPart = parts[3];
        // ensure milliseconds is 3 digits by padding/right-truncating
        if (msPart.Length < 3) msPart = msPart.PadRight(3, '0');
        if (msPart.Length > 3) msPart = msPart.Substring(0, 3);
        if (!int.TryParse(msPart, out var ms)) ms = 0;

        var totalSeconds = hh * 3600 + mm * 60 + ss + ms / 1000f;
        return totalSeconds;
    }
}