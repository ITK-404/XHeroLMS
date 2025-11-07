using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamResultUI : MonoBehaviour
{
    public Button continueBtn;
    public Button checkAnswerBtn;

    public TMP_Text textCorrectAnswer;
    public TMP_Text textInCorrectAnswer;
    public TMP_Text skipAnswer;
    public TMP_Text textTotalPass;
    public GameObject successGroup;
    public GameObject unSuccessGroup;
    public GameObject container;
    
    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);
    
    public void ShowSuccess()
    {
        successGroup.gameObject.SetActive(true);
        unSuccessGroup.gameObject.SetActive(false);
    }
    
    public void ShowUnSuccess()
    {
        successGroup.gameObject.SetActive(false);
        unSuccessGroup.gameObject.SetActive(true);
    }

    public void SetTotalAnswerPass(int score,int maxScore)
    {
        string color = "#F4A42B";
        textTotalPass.text =
            $"BÀI THI CỦA BẠN: <color={color}>{score}/{maxScore}</color>\n" +
            (score >= 24
                ? "CHÚC MỪNG! BẠN ĐÃ HOÀN THÀNH XUẤT SẮC!"
                : "HÃY ÔN LẠI KIẾN THỨC VÀ THỬ LẠI NHÉ!");
    }
}