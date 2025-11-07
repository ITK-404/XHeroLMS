using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamInfoElement : MonoBehaviour
{
    [SerializeField] private Sprite unansweredSprite;
    [SerializeField] private Sprite answeredSprite;
    [SerializeField] private Image answerImg;
    [SerializeField] private TextMeshProUGUI coloredTmp;
    [SerializeField] private TextMeshProUGUI grayTmp;
    
    public void SetAnsweredButton()
    {
        if (answerImg != null)
            answerImg.sprite = answeredSprite;
        coloredTmp.gameObject.SetActive(true);
        grayTmp.gameObject.SetActive(false);
    }

    public void SetUnansweredButton()
    {
        if (answerImg != null)
            answerImg.sprite = unansweredSprite;
        coloredTmp.gameObject.SetActive(false);
        grayTmp.gameObject.SetActive(true);
    }

    public void SetQuestionIndexText(string text)
    {
        coloredTmp.text = text;
        grayTmp.text = text;
    }
}