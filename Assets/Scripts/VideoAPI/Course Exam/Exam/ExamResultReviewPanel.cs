using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultReviewPanel : ExamQuestionManager
{
    [Header("Review Header (tuỳ chọn)")]
    [SerializeField] private TMP_Text titleTmp;
    [SerializeField] private TMP_Text counterTmp;

    [Header("Badges (tuỳ chọn)")]
    [SerializeField] private GameObject correctStatusObj;
    [SerializeField] private GameObject wrongStatusObj;
    [SerializeField] private GameObject informationPanel;

    [Header("Close")]
    [SerializeField] private Button closeBtn;

    [Header("Root")]
    [SerializeField] private GameObject reviewRoot;
    [SerializeField] private CertificatesExamUI certificatesExamUI;

    [Header("Matching Review (optional override)")]
    [SerializeField] private GameObject matchingReviewPrefab;  // prefab có MatchingElementHandler

    [Header("Statistics (Thông số)")]
    [SerializeField] private GameObject statisticsObj;     // object thông số (root)
    [SerializeField] private TMP_Text txtCorrectCount;     // txt số câu đúng
    [SerializeField] private TMP_Text txtWrongCount;       // txt số câu sai
    [SerializeField] private TMP_Text txtSkippedCount;     // txt số câu bỏ qua

    // ----- state review -----
    private ExamPaper _paper;
    private Dictionary<string, HashSet<int>> _userPicked;
    private Dictionary<string, HashSet<int>> _correctPicked;
    private Dictionary<string, HashSet<string>> _correctByText;

    // đáp án matching user đã nối: q.id -> (leftIndex -> rightIndex)
    private Dictionary<string, Dictionary<int, int>> _matchingUserPairs;
    // correct strings (dùng đặc biệt cho MATCHING)
    private Dictionary<string, List<string>> _correctMatchingStrings;


    private Dictionary<string, string> _essayMapReview = new();

    private int _idx;
    public static bool FlagContinue { get; set; }

    private bool _navHijacked;

    public void ShowReview(
        ExamPaper paper,
        Dictionary<string, HashSet<int>> userPicked,
        Dictionary<string, List<int>>   correctAnswers,
        int startIndex = 0,
        Dictionary<string, List<string>> correctAnswerTexts = null,
        Dictionary<string, string>       essayMapFromExam   = null,
        Dictionary<string, Dictionary<int,int>> matchingUserPairs = null  // <- THAM SỐ MỚI
    )
    {
        SetReviewMode(true);

        if (typeHintRoot != null)
            typeHintRoot.SetActive(false);

        // Lưu matching pairs
        _matchingUserPairs = matchingUserPairs ?? new();

        // copy bài tự luận
        _essayMapReview = new Dictionary<string, string>();
        if (essayMapFromExam != null)
        {
            foreach (var kv in essayMapFromExam)
                _essayMapReview[kv.Key] = kv.Value;
        }

        _paper      = paper;
        _userPicked = userPicked ?? new();

        _correctPicked = new();
        if (correctAnswers != null)
        {
            foreach (var kv in correctAnswers)
            {
                var list = kv.Value ?? new List<int>();
                _correctPicked[kv.Key] = new HashSet<int>(list);
            }
        }

        _correctByText = new();
        _correctMatchingStrings = new();

        if (correctAnswerTexts != null)
        {
            foreach (var kv in correctAnswerTexts)
            {
                // Lưu raw list cho MATCHING
                _correctMatchingStrings[kv.Key] = new List<string>(kv.Value ?? new List<string>());

                // Cũ: set text normalize cho MULTI/SINGLE
                var set  = new HashSet<string>();
                var list = kv.Value ?? new List<string>();
                for (int i = 0; i < list.Count; i++)
                {
                    var cleaned = ExamFormat.CleanOptionText(list[i]) ?? "";
                    cleaned = cleaned.Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        set.Add(NormalizeForCompare(cleaned));
                }
                _correctByText[kv.Key] = set;
            }
        }

        _idx = Mathf.Clamp(startIndex, 0, (_paper?.questions?.Count ?? 1) - 1);

        var root = reviewRoot != null ? reviewRoot : gameObject;
        root.SetActive(true);

        RebuildNavForReview();
        HijackBaseNav();
        RenderReview();

        UpdateStatisticsPanel();

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

        _paper             = null;
        _userPicked        = null;
        _correctPicked     = null;
        _correctByText     = null;
        _correctMatchingStrings    = null;
        _matchingUserPairs = null;
        _essayMapReview    = new Dictionary<string, string>();
        _idx               = 0;

        if (correctStatusObj)  correctStatusObj.SetActive(false);
        if (wrongStatusObj)    wrongStatusObj.SetActive(false);
        if (informationPanel)  informationPanel.gameObject.SetActive(false);
        certificatesExamUI.Hide();

        ClearContent();

        var root = reviewRoot != null ? reviewRoot : gameObject;
        root.SetActive(false);

        FlagContinue = true;
    }

    // ----------------- render review (read-only) -----------------
    private void RenderReview()
    {
        if (_paper == null || _paper.questions == null || _paper.questions.Count == 0)
            return;

        _idx = Mathf.Clamp(_idx, 0, _paper.questions.Count - 1);
        var q = _paper.questions[_idx];

        ClearContent();

        if (titleTmp) titleTmp.text = "XEM KẾT QUẢ";
        UpdateReviewCounters();

        if (counterTmp)
        {
            int total = _paper.questions.Count;
            int width = total.ToString().Length;
            counterTmp.text =
                $"{(_idx + 1).ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
        }

        var head = Instantiate(prefabCauHoi, content);
        head.text = $"{_idx + 1}. {q.title}";

        var effectiveType = q.type;
        if (_essayMapReview != null && _essayMapReview.ContainsKey(q.id))
            effectiveType = ExamQuestionType.ESSAY;

        _userPicked.TryGetValue(q.id, out var userSet);
        userSet ??= new HashSet<int>();

        _correctPicked.TryGetValue(q.id, out var correctSetRaw);
        var correctSet = NormalizeCorrectIndexSet(q, correctSetRaw);

        HashSet<string> correctTextSet = null;
        if (_correctByText != null) _correctByText.TryGetValue(q.id, out correctTextSet);

        bool exactlyCorrect;

        if (effectiveType == ExamQuestionType.ESSAY)
        {
            if (_essayMapReview != null &&
                _essayMapReview.TryGetValue(q.id, out var txt) &&
                !string.IsNullOrWhiteSpace(txt))
                exactlyCorrect = true;
            else
                exactlyCorrect = false;
        }
        else if (effectiveType == ExamQuestionType.MATCHING)
        {
            // chấm riêng cho MATCHING
            exactlyCorrect = IsMatchingExactlyCorrect(q);
        }
        else
        {
            exactlyCorrect = IsExactlyCorrect(q, userSet, correctSet, correctTextSet);
        }


        if (correctStatusObj)  correctStatusObj.SetActive(exactlyCorrect);
        if (wrongStatusObj)    wrongStatusObj.SetActive(!exactlyCorrect);
        if (informationPanel)  informationPanel.SetActive(true);

        switch (effectiveType)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                RenderChoicesReadOnly(q, userSet, correctSet, correctTextSet);
                break;

            case ExamQuestionType.MATCHING:
                RenderMatchingReadOnly(q);
                break;

            case ExamQuestionType.ESSAY:
                RenderEssayReadOnly(q);
                break;

            default:
                var note = Instantiate(prefabCauHoi, content);
                note.text = $"(Type {effectiveType} chưa hỗ trợ review)";
                break;
        }

        if (btnBack) btnBack.interactable = _idx > 0;
        if (btnNext) btnNext.interactable = _idx < _paper.questions.Count - 1;

        ReviewHighlightNavIndex(_idx);
        UpdateStatisticsPanel();

    }

    private void RenderMatchingReadOnly(ExamQuestion q)
    {
        // Ưu tiên dùng prefab riêng cho review; nếu không có, dùng prefabMatching của base
        GameObject prefab = matchingReviewPrefab != null ? matchingReviewPrefab : prefabMatching;

        if (!prefab)
        {
            var note = Instantiate(prefabCauHoi, content);
            note.text = "(Thiếu prefabMatching / matchingReviewPrefab cho câu nối cặp)";
            return;
        }

        // Tạo panel matching
        var go = Instantiate(prefab, content);
        var handler = go.GetComponent<MatchingElementHandler>();

        if (handler == null)
        {
            var note = Instantiate(prefabCauHoi, content);
            note.text = "(Prefab matching không có MatchingElementHandler)";
            return;
        }

        // Lấy pairs user đã nối cho câu này (nếu có)
        Dictionary<int, int> userPairs = null;
        if (_matchingUserPairs != null)
            _matchingUserPairs.TryGetValue(q.id, out userPairs);

        Debug.Log($"[ReviewMatching] QID={q.id}, pairsCount={(userPairs == null ? 0 : userPairs.Count)}");

        // Setup giống lúc làm bài nhưng không dùng callback
        handler.SetupQuestion(
            q,
            userPairs,
            _ => { } // review chỉ xem
        );

        // Bật chế độ read only
        handler.SetReadOnly(true);

        // khóa raycast để lỡ có chạm cũng không ăn
        var handlerImg = handler.GetComponent<Image>();
        if (handlerImg) handlerImg.raycastTarget = false;
    }

    private void RenderChoicesReadOnly(ExamQuestion q,
                                       HashSet<int> userSet,
                                       HashSet<int> correctSet,
                                       HashSet<string> correctTextSet)
    {
        if (q.options == null || q.options.Count == 0)
        {
            var no = Instantiate(prefabCauHoi, content);
            no.text = "(Không có đáp án)";
            return;
        }

        bool isSingle = q.type == ExamQuestionType.SINGLE_CHOICE;

        if (userSet == null || userSet.Count == 0)
        {
            var hint = Instantiate(prefabCauHoi, content);
            hint.text = "<i>(Bạn không chọn đáp án nào)</i>";
        }

        for (int i = 0; i < q.options.Count; i++)
        {
            var btn = Instantiate(prefabCauTraLoi, content);
            if (isSingle) btn.ActiveSingleChoice(); else btn.ActiveMultipleChoice();

            string cleanShown = ExamFormat.CleanOptionText(q.options[i]) ?? "";
            string normalized = NormalizeForCompare(cleanShown);
            btn.SetText(cleanShown);

            // khoá tương tác
            var clickable = btn.GetComponentInChildren<Button>(true);
            if (clickable) clickable.interactable = false;
            btn.OnSelectButton = null;

            bool pickedByUser = userSet != null && userSet.Contains(i);

            // user chọn đúng hay không?
            bool userPickedCorrect =
                pickedByUser &&
                (
                    (correctSet != null && correctSet.Contains(i)) ||
                    (correctTextSet != null && correctTextSet.Contains(normalized))
                );

            // highlight user chọn
            btn.ActiveSelect(pickedByUser);

            // tô màu CHỈ nếu user chọn
            if (pickedByUser)
            {
                if (userPickedCorrect)
                    btn.SetCorrectColor();      // user chọn đúng -> xanh
                else
                    btn.SetInCorrectColor();    // user chọn sai -> đỏ
            }
        }
    }

    private void RenderEssayReadOnly(ExamQuestion q)
    {
        if (!prefabCauTraLoiTuLuan)
        {
            var t = Instantiate(prefabCauHoi, content);
            t.text = "(Thiếu prefabCauTraLoiTuLuan cho review)";
            return;
        }

        var go = Instantiate(prefabCauTraLoiTuLuan, content);
        var input = go.GetComponentInChildren<TMP_InputField>();

        if (input == null)
        {
            var t = Instantiate(prefabCauHoi, content);
            t.text = "(Prefab essay không có TMP_InputField)";
            return;
        }

        input.interactable = false;
        input.readOnly     = true;

        string saved = null;
        if (_essayMapReview != null)
            _essayMapReview.TryGetValue(q.id, out saved);

        if (!string.IsNullOrWhiteSpace(saved))
            input.text = saved;
        else
            input.text = "(Bạn chưa nhập câu trả lời)";
    }

    private static HashSet<int> NormalizeCorrectIndexSet(ExamQuestion q, HashSet<int> raw)
    {
        var result = new HashSet<int>();
        if (q == null || q.options == null || raw == null) return result;

        bool looksOneBased = false;
        foreach (var v in raw)
        {
            if (v == q.options.Count) { looksOneBased = true; break; }
        }

        foreach (var v in raw)
        {
            int idx = looksOneBased ? (v - 1) : v;
            if (idx >= 0 && idx < q.options.Count)
                result.Add(idx);
        }
        return result;
    }

    private static bool IsExactlyCorrect(ExamQuestion q,
                                         HashSet<int> userSet,
                                         HashSet<int> correctIndexSet0Based,
                                         HashSet<string> correctTextSet)
    {
        if (q?.options == null) return false;

        userSet ??= new HashSet<int>();

        var combinedCorrect = new HashSet<int>();
        if (correctIndexSet0Based != null)
            foreach (var idx in correctIndexSet0Based) combinedCorrect.Add(idx);

        if (correctTextSet != null)
        {
            for (int i = 0; i < q.options.Count; i++)
            {
                string normal = NormalizeForCompare(ExamFormat.CleanOptionText(q.options[i]) ?? "");
                if (correctTextSet.Contains(normal)) combinedCorrect.Add(i);
            }
        }

        if (userSet.Count == 0) return false;
        if (combinedCorrect.Count == 0) return false;

        if (userSet.Count != combinedCorrect.Count) return false;
        foreach (var v in userSet) if (!combinedCorrect.Contains(v)) return false;
        return true;
    }

    private void UpdateReviewCounters()
    {
        if (_paper == null || _paper.questions == null) return;

        int total   = _paper.questions.Count;
        int current = Mathf.Clamp(_idx + 1, 1, total);
        int width   = total.ToString().Length;

        if (counterTmp)
            counterTmp.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";

        if (textQuestionCounter)
            textQuestionCounter.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
    }

    public static string NormalizeForCompare(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = ExamFormat.CleanOptionText(s);
        s = s.Replace('\u00A0', ' ');
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^[\-\–\•\●]\s*", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
        return s.ToLowerInvariant();
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

    private bool IsMatchingExactlyCorrect(ExamQuestion q)
    {
        if (q == null) return false;

        // raw pairs user: có thể left->right hoặc right->left tuỳ nơi lưu
        if (_matchingUserPairs == null ||
            !_matchingUserPairs.TryGetValue(q.id, out var rawPairs) ||
            rawPairs == null || rawPairs.Count == 0)
            return false;

        // correct strings từ server (format: "<p>LEFT</p>-<p>RIGHT</p>")
        if (_correctMatchingStrings == null ||
            !_correctMatchingStrings.TryGetValue(q.id, out var correctList) ||
            correctList == null || correctList.Count == 0)
            return false;

        // Build correct set
        var correctSet = new HashSet<string>();
        foreach (var s in correctList)
        {
            var norm = NormalizeMatchingPair_SubmitStyle(s);
            if (!string.IsNullOrEmpty(norm))
                correctSet.Add(norm);
        }

        // Split sides đúng kiểu submit/service: options[0] / options[1]
        GetMatchingSides_SubmitStyleSafe(q, out var left, out var right);
        if (left.Count == 0 || right.Count == 0) return false;

        // Heuristic xác định rawPairs đang là right->left hay left->right
        int scoreRightLeft = 0;
        int scoreLeftRight = 0;

        foreach (var kv in rawPairs)
        {
            int a = kv.Key;
            int b = kv.Value;

            // a=right, b=left
            if (a >= 0 && a < right.Count && b >= 0 && b < left.Count) scoreRightLeft++;
            // a=left, b=right
            if (a >= 0 && a < left.Count && b >= 0 && b < right.Count) scoreLeftRight++;
        }

        bool isRightLeft = scoreRightLeft > scoreLeftRight;

        // Build user set
        var userSet = new HashSet<string>();

        foreach (var kv in rawPairs)
        {
            int leftIndex, rightIndex;

            if (isRightLeft)
            {
                // raw: right -> left
                rightIndex = kv.Key;
                leftIndex = kv.Value;
            }
            else
            {
                // raw: left -> right
                leftIndex = kv.Key;
                rightIndex = kv.Value;
            }

            // bounds check tránh crash
            if (leftIndex < 0 || leftIndex >= left.Count) continue;
            if (rightIndex < 0 || rightIndex >= right.Count) continue;

            string pair = $"<p>{left[leftIndex]}</p>-<p>{right[rightIndex]}</p>";
            var norm = NormalizeMatchingPair_SubmitStyle(pair);
            if (!string.IsNullOrEmpty(norm))
                userSet.Add(norm);
        }

        if (userSet.Count == 0) return false;
        return userSet.SetEquals(correctSet);
    }

    private static string NormalizeMatchingPair_SubmitStyle(string pair)
    {
        if (string.IsNullOrWhiteSpace(pair)) return "";

        const string sep = "</p>-<p>";
        int idx = pair.IndexOf(sep);
        if (idx < 0)
        {
            // fallback: normalize cả chuỗi
            return NormalizeForCompare(pair);
        }

        // left part includes "</p>"
        string leftPart = pair.Substring(0, idx + 4);
        // right part starts after sep; add "<p>" back for StripP
        string rightPart = "<p>" + pair.Substring(idx + sep.Length);

        string l = StripP(leftPart);
        string r = StripP(rightPart);

        return $"{NormalizeForCompare(l)}|{NormalizeForCompare(r)}";
    }

    private static void GetMatchingSides_SubmitStyleSafe(ExamQuestion q, out List<string> left, out List<string> right)
    {
        left = new List<string>();
        right = new List<string>();

        if (q == null || q.options == null || q.options.Count < 2) return;

        left = SplitRawSide(q.options[0]);
        right = SplitRawSide(q.options[1]);
    }

    private static List<string> SplitRawSide(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(raw)) return list;

        // raw dùng dấu '-' để tách, mỗi phần thường là "<p>...</p>"
        var parts = raw.Split(new[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var s = StripP(p).Trim();
            if (!string.IsNullOrEmpty(s))
                list.Add(s);
        }
        return list;
    }

    private static string StripP(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("<p>", "").Replace("</p>", "");
    }
    public bool IsQuestionAnsweredInReview(int questionIndex)
    {
        if (_paper?.questions == null) return false;
        if (questionIndex < 0 || questionIndex >= _paper.questions.Count) return false;

        var q = _paper.questions[questionIndex];

        // ESSAY: có text
        if (_essayMapReview != null &&
            _essayMapReview.TryGetValue(q.id, out var txt) &&
            !string.IsNullOrWhiteSpace(txt))
            return true;

        // còn lại: có chọn
        HashSet<int> set = null;
        if (_userPicked != null) _userPicked.TryGetValue(q.id, out set);

        return set != null && set.Count > 0;
    }

    public bool IsQuestionCorrectInReview(int questionIndex)
    {
        if (_paper?.questions == null) return false;
        if (questionIndex < 0 || questionIndex >= _paper.questions.Count) return false;

        var q = _paper.questions[questionIndex];

        // effective type (ESSAY có thể override)
        var effectiveType = q.type;
        if (_essayMapReview != null && _essayMapReview.ContainsKey(q.id))
            effectiveType = ExamQuestionType.ESSAY;

        // userSet
        HashSet<int> userSet = null;
        if (_userPicked != null) _userPicked.TryGetValue(q.id, out userSet);
        userSet ??= new HashSet<int>();

        // correctSetRaw
        HashSet<int> correctSetRaw = null;
        if (_correctPicked != null) _correctPicked.TryGetValue(q.id, out correctSetRaw);

        // correctSet (0-based)
        var correctSet = NormalizeCorrectIndexSet(q, correctSetRaw);

        // correctTextSet
        HashSet<string> correctTextSet = null;
        if (_correctByText != null) _correctByText.TryGetValue(q.id, out correctTextSet);

        if (effectiveType == ExamQuestionType.ESSAY)
        {
            // logic hiện tại: có bài essay => đúng
            return _essayMapReview != null &&
                   _essayMapReview.TryGetValue(q.id, out var txt) &&
                   !string.IsNullOrWhiteSpace(txt);
        }

        if (effectiveType == ExamQuestionType.MATCHING)
            return IsMatchingExactlyCorrect(q);

        return IsExactlyCorrect(q, userSet, correctSet, correctTextSet);
    }
    private void UpdateStatisticsPanel()
    {
        if (statisticsObj == null)
            return;

        // bật object thông số khi review đang chạy
        statisticsObj.SetActive(true);

        if (_paper?.questions == null || _paper.questions.Count == 0)
        {
            if (txtCorrectCount) txtCorrectCount.text = "0";
            if (txtWrongCount) txtWrongCount.text = "0";
            if (txtSkippedCount) txtSkippedCount.text = "0";
            return;
        }

        int correct = 0;
        int wrong = 0;
        int skipped = 0;

        for (int i = 0; i < _paper.questions.Count; i++)
        {
            bool answered = IsQuestionAnsweredInReview(i);

            if (!answered)
            {
                skipped++;
                continue;
            }

            bool isCorrect = IsQuestionCorrectInReview(i);
            if (isCorrect) correct++;
            else wrong++;
        }

        if (txtCorrectCount) txtCorrectCount.text = $"{correct} câu";
        if (txtWrongCount)   txtWrongCount.text = $"{wrong} câu";
        if (txtSkippedCount) txtSkippedCount.text = $"{skipped} câu";
    }
}
