using System.Collections;
using UnityEngine;

public class WaitForTimeTutorialStep : TutorialStepBehaviour
{
    [Min(1)]
    [SerializeField] private int waitTime = 3;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private bool isCountingDone;
    private int remainingSeconds;

    public override void Enter()
    {
        base.Enter();

        StopAllCoroutines();
        StartCoroutine(WaitForTime());
    }

    private IEnumerator WaitForTime()
    {
        isCountingDone = false;
        remainingSeconds = waitTime;

        while (remainingSeconds > 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[{name}] Tutorial countdown: {remainingSeconds}s");
            }

            yield return new WaitForSecondsRealtime(1f);
            remainingSeconds--;
        }

        isCountingDone = true;

        if (enableDebugLog)
        {
            Debug.Log($"[{name}] Tutorial countdown completed.");
        }
    }

    public override bool IsCompleted()
    {
        return isCountingDone;
    }
}