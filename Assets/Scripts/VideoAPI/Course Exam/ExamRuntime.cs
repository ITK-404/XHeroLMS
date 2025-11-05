using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net; // WebUtility.HtmlDecode

[Serializable] public enum ExamQuestionType {
    SINGLE_CHOICE, MULTIPLE_CHOICE, MATCHING, ESSAY, UNKNOWN
}

[Serializable] public class ExamPaper {
    public List<ExamQuestion> questions = new();

    // ---- APIs tiện dùng ----
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

[Serializable] public class ExamQuestion {
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
    // JsonUtility cần wrapper & fields (không phải property).
    [Serializable] class Root { public QuestionRaw[] questions; }
    [Serializable] class QuestionRaw {
        public string[] answers; public string[] tag; public string _id;
        public string title; public string keyword; public string type; public string explain;
        public string createdBy; public string createdAt; public string updatedAt; public int __v;
    }

    public static ExamPaper Parse(string json)
    {
        // B1: bóc JSON thô -> model tạm
        var root = UnityEngine.JsonUtility.FromJson<Root>(json);
        var paper = new ExamPaper();

        if (root?.questions == null) return paper;

        foreach (var qr in root.questions)
        {
            var q = new ExamQuestion
            {
                id = qr._id ?? "",
                title = HtmlToText(qr.title),
                type = MapType(qr.type),
                explain = HtmlToText(qr.explain),
                rawAnswersHtml = qr.answers != null ? new List<string>(qr.answers) : new List<string>()
            };

            // B2: parse answers theo type
            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    q.options = ExtractOptions(q.rawAnswersHtml);
                    break;

                case ExamQuestionType.MATCHING:
                    // BE mẫu: answers có 2 phần tử, mỗi phần tử là nhiều <p>…</p> ghép bằng dấu “-”
                    // Ví dụ: "Kim</p>-<p>Thủy</p>-<p>Mộc..."
                    SplitMatching(q.rawAnswersHtml, out q.matchingLeft, out q.matchingRight);
                    break;

                case ExamQuestionType.ESSAY:
                    // không có options
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
            case "SINGLE_CHOICE": return ExamQuestionType.SINGLE_CHOICE;
            case "MULTIPLE_CHOICE": return ExamQuestionType.MULTIPLE_CHOICE;
            case "MATCHING": return ExamQuestionType.MATCHING;
            case "ESSAY": return ExamQuestionType.ESSAY;
            default: return ExamQuestionType.UNKNOWN;
        }
    }

    // Trích tất cả <p>…</p> trong mỗi câu trả lời, gộp thành list thuần text
    static List<string> ExtractOptions(List<string> htmlList)
    {
        var list = new List<string>();
        if (htmlList == null) return list;

        foreach (var html in htmlList)
        {
            // Nhiều API format 1 lựa chọn/1 <p>, có thể có nhiều <p> trong 1 string
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
        // Bóc từng p-block
        var rx = new Regex(@"<p[^>]*>(.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var m = rx.Matches(html);
        if (m.Count > 0)
        {
            foreach (Match mm in m) yield return mm.Groups[1].Value;
        }
        else
        {
            // không có <p> thì trả luôn
            yield return html;
        }
    }

    // MATCHING: answers[0] = cột trái, answers[1] = cột phải
    static void SplitMatching(List<string> htmlList, out List<string> left, out List<string> right)
    {
        left = new List<string>(); right = new List<string>();
        if (htmlList == null || htmlList.Count == 0) return;

        // cột trái
        if (htmlList.Count >= 1)
            left = SplitMatchingColumn(htmlList[0]);

        // cột phải
        if (htmlList.Count >= 2)
            right = SplitMatchingColumn(htmlList[1]);
    }

    // Cột MATCHING có dạng: "<p>A</p>-<p>B</p>-<p>C</p>" hoặc 1 block <p>…</p> nhiều
    static List<string> SplitMatchingColumn(string html)
    {
        var items = new List<string>();
        if (string.IsNullOrEmpty(html)) return items;

        // chia tại “</p> - <p>”
        var chunks = Regex.Split(html, @"</p>\s*-\s*<p>", RegexOptions.IgnoreCase);

        if (chunks.Length <= 1)
        {
            // fallback: không có dấu -, cứ extract p
            foreach (var p in ExtractPTags(html))
            {
                var t = HtmlToText(p).Trim();
                if (!string.IsNullOrEmpty(t)) items.Add(t);
            }
        }
        else
        {
            // Chỉnh 2 đầu (vì split bỏ mất <p> đầu / </p> cuối)
            // Đảm bảo strip sạch HTML
            // head: remove leading <p>
            chunks[0] = Regex.Replace(chunks[0], @"^\s*<p[^>]*>", "", RegexOptions.IgnoreCase);
            // tail: remove trailing </p>
            int last = chunks.Length - 1;
            chunks[last] = Regex.Replace(chunks[last], @"</p>\s*$", "", RegexOptions.IgnoreCase);

            foreach (var ch in chunks)
            {
                var t = HtmlToText(ch).Trim();
                if (!string.IsNullOrEmpty(t)) items.Add(t);
            }
        }
        return items;
    }

    // Bóc HTML -> text (giữ ký tự, bỏ tag, decode entities)
    static string HtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        // loại bỏ thẻ
        var noTag = Regex.Replace(html, "<.*?>", string.Empty, RegexOptions.Singleline);
        // decode entities (&nbsp; &lt; …)
        noTag = WebUtility.HtmlDecode(noTag);
        // normalize space
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
