using System;
using System.Collections;
using UnityEngine;

public class TutorialStepWaiting : TutorialStepObject
{
    private Action onComplete;

    public override void StartListening(Action onComplete)
    {
        base.StartListening(onComplete);
        this.onComplete = onComplete;

        StartCoroutine(StartWaiting());
    }

    public override void StopListening()
    {
        base.StopListening();
        onComplete = null;
    }

    private IEnumerator StartWaiting()
    {
        yield return new WaitForSeconds(5f);
        this.onComplete?.Invoke();
    }
}