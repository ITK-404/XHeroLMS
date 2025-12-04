using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net; // WebUtility.HtmlDecode

[Serializable]
public enum ExamQuestionType {
    SINGLE_CHOICE, MULTIPLE_CHOICE, MATCHING, ESSAY, UNKNOWN
}

[Serializable]
public class ExamPaper {
    public List<ExamQuestion> questions = new();

    public int Count => questions?.Count ?? 0;

    public int CountByType(ExamQuestionType t) {
        if (questions == null) return 0;
        int n = 0; foreach (var q in questions) if (q.type == t) n++;
        return n;
    }

    public Dictionary<ExamQuestionType,int> SummaryByType() {
        var d = new Dictionary<ExamQuestionType,int>();
        foreach (var q in questions) {
            d.TryGetValue(q.type, out var c);
            d[q.type] = c + 1;
        }
        return d;
    }
}

[Serializable]
public class ExamQuestion {
    public string id;
    public string title;                  // plain text
    public ExamQuestionType type;
    public List<string> options;          // SINGLE/MULTI: các lựa chọn đã strip HTML
    public List<string> matchingLeft;     // MATCHING: cột trái
    public List<string> matchingRight;    // MATCHING: cột phải
    public string explain;                // plain text (nếu có)
    public List<string> rawAnswersHtml;   // giữ nguyên HTML thô (chỉ trong RAM)
}

// ===== Parser cốt lõi: từ JSON -> ExamPaper =====
public static class ExamParser
{
    [Serializable] class Root { public QuestionRaw[] questions; }

    [Serializable] class QuestionRaw {
        public string[] answers; public string[] tag; public string _id;
        public string title; public string keyword; public string type; public string explain;
        public string createdBy; public string createdAt; public string updatedAt; public int __v;
    }

    public static ExamPaper Parse(string json)
    {
        var root  = UnityEngine.JsonUtility.FromJson<Root>(json);
        var paper = new ExamPaper();
        if (root?.questions == null) return paper;

        foreach (var qr in root.questions)
        {
            var mappedType = MapType(qr.type);

            // ====== HEURISTIC: ép một số SINGLE_CHOICE thành MATCHING ======
            if (mappedType == ExamQuestionType.SINGLE_CHOICE &&
                LooksLikeMatching(qr))
            {
                mappedType = ExamQuestionType.MATCHING;
            }
            // ===============================================================

            UnityEngine.Debug.Log(
                $"[Parse] rawId={qr._id}, rawType={qr.type}, mappedType={mappedType}"
            );

            var q = new ExamQuestion
            {
                id      = qr._id ?? "",
                title   = HtmlToText(qr.title),
                type    = mappedType,
                explain = HtmlToText(qr.explain),
                rawAnswersHtml = qr.answers != null
                    ? new List<string>(qr.answers)
                    : new List<string>()
            };

            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    q.options = ExtractOptions(q.rawAnswersHtml);
                    break;

                case ExamQuestionType.MATCHING:
                    SplitMatching(q.rawAnswersHtml, out q.matchingLeft, out q.matchingRight);
                    break;

                case ExamQuestionType.ESSAY:
                    q.options = new List<string>();
                    break;

                default:
                    q.options = ExtractOptions(q.rawAnswersHtml);
                    break;
            }

            paper.questions.Add(q);
        }

        return paper;
    }

    static ExamQuestionType MapType(string t)
    {
        if (string.IsNullOrEmpty(t)) return ExamQuestionType.UNKNOWN;
        switch (t.Trim().ToUpperInvariant())
        {
            case "SINGLE_CHOICE":   return ExamQuestionType.SINGLE_CHOICE;
            case "MULTIPLE_CHOICE": return ExamQuestionType.MULTIPLE_CHOICE;
            case "MATCHING":        return ExamQuestionType.MATCHING;
            case "ESSAY":           return ExamQuestionType.ESSAY;
            default:                return ExamQuestionType.UNKNOWN;
        }
    }

    // Heuristic nhận diện MATCHING khi BE vẫn trả SINGLE_CHOICE
    static bool LooksLikeMatching(QuestionRaw qr)
    {
        if (qr.answers == null || qr.answers.Length < 2) return false;

        bool HasMatchingPattern(string html)
        {
            if (string.IsNullOrEmpty(html)) return false;

            // dạng "</p> - <p>"
            if (Regex.IsMatch(html, @"</p>\s*-\s*<p>", RegexOptions.IgnoreCase))
                return true;

            // dạng text "A-B-C" với mọi loại gạch ngang Unicode
            var plain = HtmlToText(html);
            if (Regex.IsMatch(plain, @"[-–—-]")) return true;

            // hoặc có nhiều từ với dấu phẩy
            if (plain.Contains(",")) return true;

            // hoặc có >= 4 token (Kim Thuỷ Mộc Hoả Thổ)
            var tokens = plain.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length >= 4;
        }

        return HasMatchingPattern(qr.answers[0]) && HasMatchingPattern(qr.answers[1]);
    }

    // Trích tất cả <p>…</p> trong mỗi câu trả lời, gộp thành list thuần text
    static List<string> ExtractOptions(List<string> htmlList)
    {
        var list = new List<string>();
        if (htmlList == null) return list;

        foreach (var html in htmlList)
        {
            foreach (var piece in ExtractPTags(html))
            {
                var txt = HtmlToText(piece).Trim();
                if (!string.IsNullOrEmpty(txt))
                    list.Add(txt);
            }
        }
        return list;
    }

    static IEnumerable<string> ExtractPTags(string html)
    {
        if (string.IsNullOrEmpty(html)) yield break;

        var rx = new Regex(@"<p[^>]*>(.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var m  = rx.Matches(html);
        if (m.Count > 0)
        {
            foreach (Match mm in m) yield return mm.Groups[1].Value;
        }
        else
        {
            yield return html;
        }
    }

    // MATCHING: answers[0] = cột trái, answers[1] = cột phải
    static void SplitMatching(List<string> htmlList, out List<string> left, out List<string> right)
    {
        left  = new List<string>();
        right = new List<string>();
        if (htmlList == null || htmlList.Count == 0) return;

        if (htmlList.Count >= 1)
            left = SplitMatchingColumn(htmlList[0]);

        if (htmlList.Count >= 2)
            right = SplitMatchingColumn(htmlList[1]);
    }

    // Cột MATCHING có thể là:
    //  - "<p>A</p>-<p>B</p>-<p>C</p>"
    //  - "<p>Kim-Thuỷ-Mộc-Hoả-Thổ</p>"
    //  - "Kim–Thuỷ–Mộc–Hoả–Thổ"
    //  - "Kim, Thuỷ, Mộc, Hoả, Thổ"
    //  - "Kim Thuỷ Mộc Hoả Thổ"
    static List<string> SplitMatchingColumn(string html)
    {
        var items = new List<string>();
        if (string.IsNullOrWhiteSpace(html)) return items;

        // 1) Thử pattern "</p> - <p>"
        var chunks = Regex.Split(html, @"</p>\s*-\s*<p>", RegexOptions.IgnoreCase);
        if (chunks.Length > 1)
        {
            chunks[0] = Regex.Replace(chunks[0], @"^\s*<p[^>]*>", "", RegexOptions.IgnoreCase);
            int last = chunks.Length - 1;
            chunks[last] = Regex.Replace(chunks[last], @"</p>\s*$", "", RegexOptions.IgnoreCase);

            foreach (var ch in chunks)
            {
                var t = HtmlToText(ch).Trim();
                if (!string.IsNullOrEmpty(t)) items.Add(t);
            }
            return items;
        }

        // 2) Không có "</p>-<p>" → strip HTML sang plain text
        var plain = HtmlToText(html);   // ví dụ: "Kim-Thuỷ-Mộc-Hoả-Thổ"

        // Chuẩn hoá các loại gạch ngang thành '-'
        plain = plain.Replace('–', '-').Replace('—', '-');

        // Ưu tiên tách theo '-'
        if (plain.Contains("-"))
        {
            var parts = plain.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var t = part.Trim();
                if (!string.IsNullOrEmpty(t))
                    items.Add(t);
            }
        }
        else if (plain.Contains(","))   // nếu có dấu phẩy
        {
            var parts = plain.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var t = part.Trim();
                if (!string.IsNullOrEmpty(t))
                    items.Add(t);
            }
        }
        else
        {
            // cuối cùng: tách theo khoảng trắng
            var parts = plain.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var t = part.Trim();
                if (!string.IsNullOrEmpty(t))
                    items.Add(t);
            }
        }

        // nếu vẫn rỗng thì add nguyên chuỗi để tránh crash
        if (items.Count == 0 && !string.IsNullOrEmpty(plain))
            items.Add(plain);

        UnityEngine.Debug.Log($"[SplitMatchingColumn] plain='{plain}' -> {items.Count} items");

        return items;
    }

    static string HtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var noTag = Regex.Replace(html, "<.*?>", string.Empty, RegexOptions.Singleline);
        noTag = WebUtility.HtmlDecode(noTag);
        return Regex.Replace(noTag, @"\s+", " ").Trim();
    }
}

// ===== Cache thuần RAM (không ghi disk) =====
public static class ExamSession
{
    public static ExamPaper Current { get; private set; }

    public static void LoadFromJson(string json)
        => Current = ExamParser.Parse(json);

    public static void Clear()
        => Current = null;
}
