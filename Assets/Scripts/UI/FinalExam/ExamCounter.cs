using System.Collections;
using UnityEngine;

public class ExamCounter : MonoBehaviour
{
    public TimerExamUI timerExamUI;
    public int duration;
    public int timer;
    private WaitForSecondsRealtime yieldWaitOneSecond;

    private void Awake()
    {
        yieldWaitOneSecond = new WaitForSecondsRealtime(1);
    }

    public void SetData(ExamData examData)
    {
        int.TryParse(examData.duration, out var result);
        duration = result;
        timer = duration;
   
    }

    public void StartCounter()
    {
        StopCoroutine(Counter());
        StartCoroutine(Counter());
    }

    private IEnumerator Counter()
    {
        while (timer > 0)
        {
            float lerp = timer / duration;
            timerExamUI.UpdateText(timer, lerp);
            yield return yieldWaitOneSecond;
            timer--;
        }
    }
}