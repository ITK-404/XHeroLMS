using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamInfoElement : MonoBehaviour
{
    [SerializeField] private Image answeredImg;
    [SerializeField] private Image unAsweredImg;
    [SerializeField] private Image selectAnsweredImg;
    [SerializeField] private TextMeshProUGUI coloredTmp;
    [SerializeField] private TextMeshProUGUI grayTmp;
    [SerializeField] private TextMeshProUGUI gradientTmp;

    private void Awake()
    {
        SetUnansweredButton();
    }

    public void SetAnsweredButton()
    {
        coloredTmp.gameObject.SetActive(true);
        grayTmp.gameObject.SetActive(false);
        gradientTmp.gameObject.SetActive(false);
    }

    public void SetUnansweredButton()
    {
        coloredTmp.gameObject.SetActive(false);
        grayTmp.gameObject.SetActive(true);
        gradientTmp.gameObject.SetActive(false);
    }

    public void ShowSelectedAnswerButton()
    {
        coloredTmp.gameObject.SetActive(false);
        grayTmp.gameObject.SetActive(false);
        gradientTmp.gameObject.SetActive(true);
    }

    public void SetQuestionIndexText(int index)
    {
        string formatIndex = $"Câu\n{index}";
        coloredTmp.text = formatIndex;
        grayTmp.text = formatIndex;
        gradientTmp.text = formatIndex;
    }
   
}