using TMPro;
using UnityEngine;

public class TimerExamUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Color startColor;
    [SerializeField] private Color finalColor;

    public void UpdateText(int second, float lerp)
    {
        if (second < 0) second = 0;
        int minutes = second / 60;
        int secs = second % 60;
        timerTMP.text = $"{minutes}:{secs:00}";
        timerTMP.color = Color.Lerp(startColor, finalColor, lerp);
    }
}

