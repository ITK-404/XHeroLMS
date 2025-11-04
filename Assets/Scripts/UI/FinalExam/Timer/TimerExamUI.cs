using TMPro;
using UnityEngine;

public class TimerExamUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Color startColor;
    [SerializeField] private Color finalColor;

    public void UpdateText(string timer,float lerp)
    {
        timerTMP.text = timer;
        timerTMP.color = Color.Lerp(startColor, finalColor, lerp);
    }
}

