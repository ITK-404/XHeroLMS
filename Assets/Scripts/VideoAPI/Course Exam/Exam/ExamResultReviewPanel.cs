using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultReviewPanel : ExamQuestionManager
{
    [Header("Review Header")]
    [SerializeField] private TMP_Text titleTmp;
    [SerializeField] private TMP_Text counterTmp;

    [Header("Badges")]
    [SerializeField] private GameObject correctStatusObj;
    [SerializeField] private GameObject wrongStatusObj;
    [SerializeField] private GameObject informationPanel;

    [Header("Close")]
    [SerializeField] private Button closeBtn;

    [Header("Root")]
    [SerializeField] private GameObject reviewRoot;
    [SerializeField] private CertificatesExamUI certificatesExamUI;

    [Header("Matching Review")]
    [SerializeField] private GameObject matchingReviewPrefab;

    // ================= STATE =================
    private ExamPaper _paper;
    private Dictionary<string, HashSet<int>> _userPicked;
    private Dictionary<string, HashSet<int>> _correctPicked;
    private Dictionary<string, HashSet<string>> _correctByText;

    // MATCHING
    private Dictionary<string, Dictionary<int, int>> _matchingUserPairs;
    private Dictionary<string, List<string>> _correctMatchingStrings;

    private Dictionary<string, string> _essayMapReview = new();

    private int _idx;
    public static bool FlagContinue { get; set; }
    private bool _navHijacked;

    // ================= PUBLIC =================
    public void ShowReview(
        ExamPaper paper,
        Dictionary<string, HashSet<int>> userPicked,
        Dictionary<string, List<int>> correctAnswers,
        int startIndex = 0,
        Dictionary<string, List<string>> correctAnswerTexts = null,
        Dictionary<string, string> essayMapFromExam = null,
        Dictionary<string, Dictionary<int, int>> matchingUserPairs = null
    )
    {
        SetReviewMode(true);

        if (typeHintRoot) typeHintRoot.SetActive(false);

        _paper = paper;
        _userPicked = userPicked ?? new();
        _matchingUserPairs = matchingUserPairs ?? new();

        _essayMapReview = essayMapFromExam != null
            ? new Dictionary<string, string>(essayMapFromExam)
            : new Dictionary<string, string>();

        _correctPicked = new();
        if (correctAnswers != null)
        {
            foreach (var kv in correctAnswers)
                _correctPicked[kv.Key] = new HashSet<int>(kv.Value);
        }

        _correctByText = new();
        _correctMatchingStrings = new();

        if (correctAnswerTexts != null)
        {
            foreach (var kv in correctAnswerTexts)
            {
                _correctMatchingStrings[kv.Key] = new List<string>(kv.Value);

                var set = new HashSet<string>();
                foreach (var s in kv.Value)
                {
                    var cleaned = NormalizeForCompare(s);
                    if (!string.IsNullOrEmpty(cleaned))
                        set.Add(cleaned);
                }
                _correctByText[kv.Key] = set;
            }
        }

        _idx = Mathf.Clamp(startIndex, 0, (_paper?.questions.Count ?? 1) - 1);

        (reviewRoot ? reviewRoot : gameObject).SetActive(true);

        RebuildNavForReview();
        HijackBaseNav();
        RenderReview();

        if (closeBtn)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(HideReview);
        }
    }

    public void HideReview()
    {
        RestoreBaseNav();
        SetReviewMode(false);

        _paper = null;
        _userPicked = null;
        _correctPicked = null;
        _correctByText = null;
        _correctMatchingStrings = null;
        _matchingUserPairs = null;
        _essayMapReview.Clear();

        if (correctStatusObj) correctStatusObj.SetActive(false);
        if (wrongStatusObj) wrongStatusObj.SetActive(false);
        if (informationPanel) informationPanel.SetActive(false);

        certificatesExamUI?.Hide();
        ClearContent();

        (reviewRoot ? reviewRoot : gameObject).SetActive(false);
        FlagContinue = true;
    }

    // ================= RENDER =================
    private void RenderReview()
    {
        if (_paper == null || _paper.questions.Count == 0) return;

        var q = _paper.questions[_idx];
        ClearContent();

        if (titleTmp) titleTmp.text = "XEM KẾT QUẢ";
        UpdateReviewCounters();

        var head = Instantiate(prefabCauHoi, content);
        head.text = $"{_idx + 1}. {q.title}";

        bool correct;
        if (q.type == ExamQuestionType.MATCHING)
            correct = IsMatchingExactlyCorrect(q);
        else
            correct = true; // các loại khác giữ nguyên logic cũ

        if (correctStatusObj) correctStatusObj.SetActive(correct);
        if (wrongStatusObj) wrongStatusObj.SetActive(!correct);
        if (informationPanel) informationPanel.SetActive(true);

        if (q.type == ExamQuestionType.MATCHING)
            RenderMatchingReadOnly(q);

        if (btnBack) btnBack.interactable = _idx > 0;
        if (btnNext) btnNext.interactable = _idx < _paper.questions.Count - 1;

        ReviewHighlightNavIndex(_idx);
    }

    private void RenderMatchingReadOnly(ExamQuestion q)
    {
        var prefab = matchingReviewPrefab ? matchingReviewPrefab : prefabMatching;
        if (!prefab) return;

        var go = Instantiate(prefab, content);
        var handler = go.GetComponent<MatchingElementHandler>();
        if (!handler) return;

        _matchingUserPairs.TryGetValue(q.id, out var pairs);
        handler.SetupQuestion(q, pairs, _ => { });
        handler.SetReadOnly(true);
    }

    // ================= MATCHING CORE =================
    private bool IsMatchingExactlyCorrect(ExamQuestion q)
    {
        if (!_matchingUserPairs.TryGetValue(q.id, out var userPairs) || userPairs.Count == 0)
            return false;

        if (!_correctMatchingStrings.TryGetValue(q.id, out var correctList) || correctList.Count == 0)
            return false;

        var correctSet = new HashSet<string>();
        foreach (var s in correctList)
            correctSet.Add(NormalizeMatchingPair_SubmitStyle(s));

        GetMatchingSides_SubmitStyle(q, out var left, out var right);

        var userSet = new HashSet<string>();
        foreach (var kv in userPairs)
        {
            string pair = $"<p>{left[kv.Key]}</p>-<p>{right[kv.Value]}</p>";
            userSet.Add(NormalizeMatchingPair_SubmitStyle(pair));
        }

        return userSet.SetEquals(correctSet);
    }

    private static string NormalizeMatchingPair_SubmitStyle(string pair)
    {
        const string sep = "</p>-<p>";
        int idx = pair.IndexOf(sep);
        if (idx < 0) return NormalizeForCompare(pair);

        string l = StripP(pair.Substring(0, idx + 4));
        string r = StripP("<p>" + pair[(idx + sep.Length)..]);

        return $"{NormalizeForCompare(l)}|{NormalizeForCompare(r)}";
    }

    private static void GetMatchingSides_SubmitStyle(ExamQuestion q, out List<string> left, out List<string> right)
    {
        left = SplitRaw(q.options[0]);
        right = SplitRaw(q.options[1]);
    }

    private static List<string> SplitRaw(string raw)
    {
        var list = new List<string>();
        foreach (var p in raw.Split('-'))
            list.Add(StripP(p).Trim());
        return list;
    }

    private static string StripP(string s) => s.Replace("<p>", "").Replace("</p>", "");

    public static string NormalizeForCompare(string s)
    {
        s = ExamFormat.CleanOptionText(s);
        return string.IsNullOrWhiteSpace(s)
            ? ""
            : System.Text.RegularExpressions.Regex.Replace(s.ToLower(), @"\s+", " ").Trim();
    }

    private void RebuildNavForReview()
    {
        RebuildQuestionNavIfNeeded();

        // Rebind click của từng “chấm”
        for (int i = 0; i < _navItems.Count; i++)
        {
            int t = i;
            var b = _navItems[i]?.GetButton();
            if (b == null) continue;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                _idx = t;
                RenderReview();
            });
        }

        ReviewHighlightNavIndex(_idx);
    }

    private void UpdateReviewCounters()
    {
        if (_paper == null || _paper.questions == null) return;

        int total = _paper.questions.Count;
        int current = Mathf.Clamp(_idx + 1, 1, total);
        int width = total.ToString().Length;

        if (counterTmp)
            counterTmp.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";

        if (textQuestionCounter)
            textQuestionCounter.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
    }

    private void HijackBaseNav()
    {
        if (_navHijacked) return;

        if (btnBack)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(() =>
            {
                if (_paper == null || _paper.questions == null || _paper.questions.Count == 0)
                    return;

                _idx = Mathf.Clamp(_idx - 1, 0, _paper.questions.Count - 1);
                RenderReview();
            });
        }

        if (btnNext)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(() =>
            {
                if (_paper == null || _paper.questions == null || _paper.questions.Count == 0)
                    return;

                _idx = Mathf.Clamp(_idx + 1, 0, _paper.questions.Count - 1);
                RenderReview();
            });
        }

        _navHijacked = true;
    }
    
    private void RestoreBaseNav()
    {
        if (!_navHijacked) return;

        if (btnBack) btnBack.onClick.RemoveAllListeners();
        if (btnNext) btnNext.onClick.RemoveAllListeners();

        _navHijacked = false;
    }
}
