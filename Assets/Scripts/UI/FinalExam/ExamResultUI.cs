using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueBtn;
    public Button checkAnswerBtn;

    [Header("Texts")]
    public TMP_Text textCorrectAnswer;
    public TMP_Text textInCorrectAnswer;
    public TMP_Text skipAnswer;
    public TMP_Text textTotalPass;

    [Header("Views")]
    public GameObject successGroup;
    public GameObject unSuccessGroup;
    public GameObject container;

    private Action _onCheckExternal;

    void Awake()
    {
        // mặc định: bấm "Xem kết quả" -> ẩn panel hiện tại
        if (checkAnswerBtn)
        {
            checkAnswerBtn.onClick.RemoveAllListeners();
            checkAnswerBtn.onClick.AddListener(() =>
            {
                Hide();
                _onCheckExternal?.Invoke(); 
            });
        }
        
        if (continueBtn)
        {
            continueBtn.onClick.RemoveAllListeners();
        }
    }
    
    public void SetupOnCheck(Action onCheck)
    {
        _onCheckExternal = onCheck;
    }

    public void Show() => container?.SetActive(true);
    public void Hide() => container?.SetActive(false);

    public void ShowSuccess()
    {
        if (successGroup) successGroup.SetActive(true);
        if (unSuccessGroup) unSuccessGroup.SetActive(false);
    }

    public void ShowUnSuccess()
    {
        if (successGroup) successGroup.SetActive(false);
        if (unSuccessGroup) unSuccessGroup.SetActive(true);
    }

    public void SetTotalAnswerPass(int score, int maxScore, int passPercent = 80)
    {
        int required = Mathf.CeilToInt(maxScore * Mathf.Clamp(passPercent, 0, 100) / 100f);
        string color = "#F4A42B";
        textTotalPass.text =
            $"BÀI THI CỦA BẠN: <color={color}>{score}/{maxScore}</color>\n" +
            (score >= required
                ? "CHÚC MỪNG! BẠN ĐÃ HOÀN THÀNH XUẤT SẮC!"
                : $"Bài thi cần đạt tối thiểu {required}/{maxScore}. Hãy ôn lại và thử lại nhé!");
    }
}
