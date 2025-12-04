using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ExamFormat
{
public static ExamQuestionType FormatStringToExamQuestion(string t)
{
    if (string.IsNullOrEmpty(t))
        return ExamQuestionType.SINGLE_CHOICE; // fallback

    if (string.Equals(t, "SINGLE_CHOICE", StringComparison.OrdinalIgnoreCase))
        return ExamQuestionType.SINGLE_CHOICE;

    if (string.Equals(t, "MULTIPLE_CHOICE", StringComparison.OrdinalIgnoreCase))
        return ExamQuestionType.MULTIPLE_CHOICE;

    if (string.Equals(t, "MATCHING", StringComparison.OrdinalIgnoreCase))
        return ExamQuestionType.MATCHING;

    if (string.Equals(t, "ESSAY", StringComparison.OrdinalIgnoreCase))
        return ExamQuestionType.ESSAY;

    // fallback nếu BE thêm type mới mà mình chưa support
    return ExamQuestionType.SINGLE_CHOICE;
}

    public static string CleanHtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        string s = html;

        s = Regex.Replace(s, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"</\s*p\s*>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<\s*p[^>]*>", "", RegexOptions.IgnoreCase);

        s = Regex.Replace(s, @"&nbsp;", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<[^>]+>", "");
        s = WebUtility.HtmlDecode(s);
        s = Regex.Replace(s, "[\"“”‘’«»]+", "");
        s = Regex.Replace(s, @"[ \t]+\n", "\n");
        s = Regex.Replace(s, @"\n{3,}", "\n\n");

        return s.Trim();
    }
    
    public static int ExtractIntField(string raw, string field, int fallback = 0)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(field)) return fallback;
        try
        {
            var m = Regex.Match(raw, $"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int v)) return v;
        }
        catch
        {
        }

        return fallback;
    }

    public static string ExtractStringField(string raw, string field)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(field)) return null;
        try
        {
            var pattern = $"\"{Regex.Escape(field)}\"\\s*:\\s*\"(?<val>(?:\\\\.|[^\\" + "\"\\\\])*)\"";
            var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success)
            {
                var val = m.Groups["val"].Value;
                val = Regex.Unescape(val);
                return val;
            }
        }
        catch
        {
        }

        return null;
    }
    public static int ExtractExamDuration(string raw)
    {
        int durationSec = 0;
        var durMatch = Regex.Match(raw, @"""duration""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        if (durMatch.Success) int.TryParse(durMatch.Groups[1].Value, out durationSec);

        return durationSec;
    }
    
    public static string ExtractQuestionsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            Debug.LogWarning("[ExamUI] ExtractQuestionsArray: raw is NULL/Empty.");
            return null;
        }

        try
        {
            var key = "\"questions\"";
            int i = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                Debug.LogError("[ExamUI] Can't find \"questions\" key in JSON.");
                return null;
            }

            int s = raw.IndexOf('[', i);
            if (s < 0)
            {
                Debug.LogError("[ExamUI] Can't find '[' after \"questions\".");
                return null;
            }

            int depth = 0;
            for (int p = s; p < raw.Length; p++)
            {
                if (raw[p] == '[') depth++;
                else if (raw[p] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string arr = raw.Substring(s, p - s + 1);
                        Debug.Log($"[ExamUI] Extracted questions array length={arr.Length}");
                        return arr;
                    }
                }
            }

            Debug.LogError("[ExamUI] Could not match the closing ']' for questions array.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ExamUI] ExtractQuestionsArray EXCEPTION: " + ex.Message);
        }

        return null;
    }
    
    public static string CleanOptionText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        html = Regex.Replace(html, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</?\s*p\s*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", "");
        return WebUtility.HtmlDecode(html).Trim();
    }
}
public partial class ExamUIController
{
    [Serializable]
    private class QuestionsWrapper
    {
        public List<QuestionRaw> questions;
    }

    [Serializable]
    private class QuestionRaw
    {
        public string _id;
        public string title;
        public string type;
        public List<string> answers;
    }

    public static ExamPaper FallbackParseToPaper(string questionsJson)
    {
        string wrapped = questionsJson.TrimStart().StartsWith("[")
            ? "{\"questions\":" + questionsJson + "}"
            : questionsJson;

        var wrapper = JsonUtility.FromJson<QuestionsWrapper>(wrapped);
        var result = new ExamPaper { questions = new List<ExamQuestion>() };

        if (wrapper?.questions == null) return result;

        foreach (var q in wrapper.questions)
        {
            var eq = new ExamQuestion
            {
                id = string.IsNullOrEmpty(q._id) ? Guid.NewGuid().ToString() : q._id,
                title = q.title ?? "",
                type = ExamFormat.FormatStringToExamQuestion(q.type),
                options = new List<string>()
            };

            if (q.answers != null)
                foreach (var a in q.answers)
                    eq.options.Add(ExamFormat.CleanOptionText(a));

            result.questions.Add(eq);
        }

        return result;
    }

    
   
}