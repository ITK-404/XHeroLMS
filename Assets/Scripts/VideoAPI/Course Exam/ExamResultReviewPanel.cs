using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultReviewPanel : ExamQuestionManager
{
    [Header("Review Header (tuỳ chọn)")]
    [SerializeField] private TMP_Text titleTmp;          // ví dụ: "XEM KẾT QUẢ"
    [SerializeField] private TMP_Text counterTmp;        // ví dụ: "01/40"

    [Header("Badges (tuỳ chọn)")]
    [SerializeField] private GameObject correctStatusObj;
    [SerializeField] private GameObject wrongStatusObj;
    [SerializeField] private GameObject informationPanel;
    [Header("Close")]
    [SerializeField] private Button closeBtn;

    [Header("Root")]
    [SerializeField] private GameObject reviewRoot;
    [SerializeField] private CertificatesExamUI certificatesExamUI;
    // ----- state review -----
    private ExamPaper _paper;
    private Dictionary<string, HashSet<int>> _userPicked;     // q.id -> indices user chọn (0-based)
    private Dictionary<string, HashSet<int>> _correctPicked;  // q.id -> indices đúng (có thể 0/1-based từ API, sẽ convert khi dùng)
    private Dictionary<string, HashSet<string>> _correctByText; // q.id -> normalized correct texts
    private int _idx;

    public static bool FlagContinue { get; set; }

    // cờ đang override hành vi nút của base
    private bool _navHijacked;

    public void ShowReview(
        ExamPaper paper,
        Dictionary<string, HashSet<int>> userPicked,
        Dictionary<string, List<int>> correctAnswers,
        int startIndex = 0,
        Dictionary<string, List<string>> correctAnswerTexts = null)
    {
        SetReviewMode(true);

        if (typeHintRoot != null)
            typeHintRoot.SetActive(false);

        _paper = paper;
        _userPicked = userPicked ?? new();

        // Lưu tạm set đáp án đúng dạng raw index (có thể 0-based hoặc 1-based tuỳ API)
        _correctPicked = new();
        if (correctAnswers != null)
        {
            foreach (var kv in correctAnswers)
            {
                var list = kv.Value ?? new List<int>();
                _correctPicked[kv.Key] = new HashSet<int>(list);
            }
        }

        // Build map theo TEXT (đã clean/normalize), bỏ rỗng
        _correctByText = new();
        if (correctAnswerTexts != null)
        {
            foreach (var kv in correctAnswerTexts)
            {
                var set = new HashSet<string>();
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
        // gameObject.SetActive(true);

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
        // trả lại behavior cũ cho btn Back/Next
        RestoreBaseNav();

        // tắt review mode -> trả lại UI làm bài
        SetReviewMode(false);

        // reset trạng thái review
        _paper         = null;
        _userPicked    = null;
        _correctPicked = null;
        _correctByText = null;
        _idx           = 0;

        if (correctStatusObj) correctStatusObj.SetActive(false);
        if (wrongStatusObj)  wrongStatusObj.SetActive(false);
        if(informationPanel) informationPanel.gameObject.SetActive(false);
        certificatesExamUI.Hide();
        // xóa hết nội dung câu hỏi/đáp án đang spawn
        ClearContent();

        // tắt toàn bộ panel review (root)
        var root = reviewRoot != null ? reviewRoot : gameObject;
        root.SetActive(false);

        FlagContinue = true;
    }

    // ----------------- điều hướng dùng lại nút của base -----------------
    private void HijackBaseNav()
    {
        if (_navHijacked) return;

        if (btnBack)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(() =>
            {
                _idx = Mathf.Max(0, _idx - 1);
                RenderReview();
            });
        }
        if (btnNext)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(() =>
            {
                if (_paper != null)
                    _idx = Mathf.Min(_paper.questions.Count - 1, _idx + 1);
                RenderReview();
            });
        }
        _navHijacked = true;
    }

    private void RestoreBaseNav()
    {
        if (!_navHijacked) return;

        // trả lại hành vi làm bài gốc của ExamQuestionManager
        if (btnBack)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(OnBack);
        }
        if (btnNext)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(OnNext);
        }
        _navHijacked = false;
    }

    // ----------------- render review (read-only) -----------------
    private void RenderReview()
    {
        // if (typeHintRoot != null)
        //     typeHintRoot.SetActive(false);
        
        if (_paper == null || _paper.questions == null || _paper.questions.Count == 0)
            return;

        _idx = Mathf.Clamp(_idx, 0, _paper.questions.Count - 1);
        var q = _paper.questions[_idx];

        ClearContent();

        // header
        if (titleTmp) titleTmp.text = "XEM KẾT QUẢ";
        UpdateReviewCounters();

        if (counterTmp)
        {
            int total = _paper.questions.Count;
            int width = total.ToString().Length;
            counterTmp.text =
                $"{(_idx + 1).ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
        }

        // tiêu đề câu hỏi
        var head = Instantiate(prefabCauHoi, content);
        head.text = $"{_idx + 1}. {q.title}";

        // map chọn/đúng
        _userPicked.TryGetValue(q.id, out var userSet);
        userSet ??= new HashSet<int>();

        _correctPicked.TryGetValue(q.id, out var correctSetRaw);
        // Chuyển correctSet về 0-based theo từng câu hỏi (nếu API là 1-based)
        var correctSet = NormalizeCorrectIndexSet(q, correctSetRaw);

        // Lấy set TEXT đáp án đúng (nếu có)
        HashSet<string> correctTextSet = null;
        if (_correctByText != null) _correctByText.TryGetValue(q.id, out correctTextSet);

        // Chỉ dùng để xét đúng/sai toàn câu (KHÔNG hiển thị chi tiết đáp án)
        bool exactlyCorrect = IsExactlyCorrect(q, userSet, correctSet, correctTextSet);
        if (correctStatusObj) correctStatusObj.SetActive(exactlyCorrect);
        if (wrongStatusObj) wrongStatusObj.SetActive(!exactlyCorrect);
        if (informationPanel) informationPanel.SetActive(true);

        if (certificatesExamUI && exactlyCorrect)
        {
            certificatesExamUI.Show();
        }

        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                // Truyền correctSet/correctTextSet vào cho đủ params, nhưng RenderChoicesReadOnly sẽ KHÔNG dùng
                RenderChoicesReadOnly(q, userSet, correctSet, correctTextSet);
                break;

            case ExamQuestionType.ESSAY:
                RenderEssayReadOnly(q);
                break;

            default:
                var note = Instantiate(prefabCauHoi, content);
                note.text = $"(Type {q.type} chưa hỗ trợ review)";
                break;
        }

        // cập nhật trạng thái enable cho 2 nút (vẫn là nút của base)
        if (btnBack) btnBack.interactable = _idx > 0;
        if (btnNext) btnNext.interactable = _idx < _paper.questions.Count - 1;

        ReviewHighlightNavIndex(_idx);
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
        var input = Instantiate(prefabCauTraLoiTuLuan, content);
        input.interactable = false;
        input.readOnly = true;
        input.text = "(Xem lại bài tự luận)";
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

        // userSet null -> coi như rỗng
        userSet ??= new HashSet<int>();

        // Gộp tập chỉ số đúng từ cả index (đã 0-based) & text
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

        int total = _paper.questions.Count;
        int current = Mathf.Clamp(_idx + 1, 1, total);
        int width = total.ToString().Length;

        // Header counter: "01/40"
        if (counterTmp)
            counterTmp.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";

        // Counter của base ở dưới khung (khi review, base không tự update nên ta set tay)
        if (textQuestionCounter)
            textQuestionCounter.text = $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
    }

    internal static string NormalizeForCompare(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // Giữ Clean để an toàn khi caller chưa clean
        s = ExamFormat.CleanOptionText(s);
        // gom nhiều space thành 1, bỏ ký hiệu đầu dòng phổ biến
        s = s.Replace('\u00A0', ' ');                 
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^[\-\–\•\●]\s*", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
        return s.ToLowerInvariant();
    }

    private void RebuildNavForReview()
    {
        RebuildQuestionNavIfNeeded();

        // Rebind click của từng “chấm” để nhảy trong review
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

        // highlight lần đầu
        ReviewHighlightNavIndex(_idx);
    }
}