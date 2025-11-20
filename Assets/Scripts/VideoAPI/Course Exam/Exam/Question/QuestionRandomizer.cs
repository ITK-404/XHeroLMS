using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý random / cắt câu hỏi cho 1 ExamPaper.
/// Không phải MonoBehaviour – được gọi bởi ExamQuestionManager.
/// </summary>
public class QuestionRandomizer
{
    private readonly bool _randomizeQuestions;
    private readonly int  _numberOfQuestions;

    private bool _randomApplied = false;
    private List<ExamQuestion> _fullQuestionBank;

    public QuestionRandomizer(bool randomizeQuestions, int numberOfQuestions)
    {
        _randomizeQuestions  = randomizeQuestions;
        _numberOfQuestions   = numberOfQuestions;
    }

    /// <summary>
    /// Reset lại random cho attempt mới + restore full bank nếu đã lưu.
    /// </summary>
    public void ResetForNewAttempt(ExamPaper paper)
    {
        _randomApplied = false;

        if (paper != null &&
            _fullQuestionBank != null &&
            _fullQuestionBank.Count > 0)
        {
            paper.questions = new List<ExamQuestion>(_fullQuestionBank);
        }
    }

    /// <summary>
    /// Áp dụng random/cắt câu hỏi, chỉ chạy 1 lần cho mỗi attempt.
    /// </summary>
    public void ApplyRandomQuestionFilterIfNeeded(ExamPaper paper)
    {
        if (_randomApplied) return;
        if (!_randomizeQuestions) { _randomApplied = true; return; }
        if (paper == null || paper.questions == null || paper.questions.Count == 0)
        {
            _randomApplied = true;
            return;
        }

        var qs = paper.questions;

        // lần đầu, lưu full bank
        if (_fullQuestionBank == null || _fullQuestionBank.Count == 0)
        {
            _fullQuestionBank = new List<ExamQuestion>(qs);
        }

        int total = _fullQuestionBank.Count;

        int need = _numberOfQuestions <= 0 ? total : Mathf.Clamp(_numberOfQuestions, 1, total);
        if (need >= total)
        {
            // random thứ tự toàn bộ, không cắt bớt
            List<ExamQuestion> tempAll = new List<ExamQuestion>(_fullQuestionBank);
            for (int i = tempAll.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (tempAll[i], tempAll[j]) = (tempAll[j], tempAll[i]);
            }
            paper.questions = tempAll;
            _randomApplied  = true;
            Debug.Log($"[QuestionRandomizer] Random {total}/{total} câu (không cắt).");
            return;
        }

        // copy từ full bank và shuffle
        List<ExamQuestion> temp = new List<ExamQuestion>(_fullQuestionBank);
        for (int i = temp.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (temp[i], temp[j]) = (temp[j], temp[i]);
        }

        paper.questions = temp.GetRange(0, need);
        _randomApplied  = true;

        Debug.Log($"[QuestionRandomizer] Random {need}/{total} câu.");
    }
}
