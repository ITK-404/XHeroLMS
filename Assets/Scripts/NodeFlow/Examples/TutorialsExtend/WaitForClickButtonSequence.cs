using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitForClickButtonSequence : TutorialStepBehaviour
{
    private enum State
    {
        None,
        WaitingForClick,
        Completed
    }

    [Header("References")]
    [SerializeField] private TutorialClickArea tutorialClickArea;

    [Header("Buttons")]
    [SerializeField] private List<Button> buttons = new();

    private int currentButtonIndex;
    private State currentState = State.None;

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);

        currentButtonIndex = 0;
        currentState = State.None;

        if (!ValidateData())
        {
            currentState = State.Completed;
            return;
        }

        tutorialClickArea = ClassTutorialFlow.Instance.blockingArea;
        
        tutorialClickArea.Clicked += OnTutorialAreaClicked;
        tutorialClickArea.Active();

        ChangeState(State.WaitingForClick);
    }

    public override void Exit(CutsceneContext context = null)
    {
        tutorialClickArea.Clicked -= OnTutorialAreaClicked;
        tutorialClickArea.DeActive();

        currentState = State.None;

        base.Exit(context);
    }

    public override bool IsCompleted()
    {
        return currentState == State.Completed;
    }

    private bool ValidateData()
    {
        if (tutorialClickArea == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForClickButtonSequence)}] TutorialClickArea is missing.",
                this
            );

            return false;
        }

        if (buttons == null || buttons.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(WaitForClickButtonSequence)}] Button list is empty.",
                this
            );

            return false;
        }

        return true;
    }

    private void OnTutorialAreaClicked()
    {
        if (currentState != State.WaitingForClick)
        {
            return;
        }

        Button currentButton = GetCurrentButton();

        if (currentButton == null)
        {
            MoveToNextButton();
            return;
        }

        // Bật dòng này nếu click vùng tutorial cũng phải thực thi button thật.
        // currentButton.onClick.Invoke();

        MoveToNextButton();
    }

    private void MoveToNextButton()
    {
        currentButtonIndex++;

        if (currentButtonIndex >= buttons.Count)
        {
            ChangeState(State.Completed);
            return;
        }

        FocusCurrentButton();
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        switch (currentState)
        {
            case State.WaitingForClick:
                FocusCurrentButton();
                break;

            case State.Completed:
                tutorialClickArea.DeActive();
                break;
        }
    }

    private void FocusCurrentButton()
    {
        Button currentButton = GetCurrentButton();

        if (currentButton == null)
        {
            Debug.LogWarning(
                $"[{nameof(WaitForClickButtonSequence)}] " +
                $"Button at index {currentButtonIndex} is null.",
                this
            );

            MoveToNextButton();
            return;
        }

        RectTransform buttonRect =
            currentButton.transform as RectTransform;

        if (buttonRect == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForClickButtonSequence)}] " +
                $"Button at index {currentButtonIndex} has no RectTransform.",
                currentButton
            );

            MoveToNextButton();
            return;
        }

        ClassTutorialFlow.Instance.SetInteractZone(buttonRect);
    }

    private Button GetCurrentButton()
    {
        if (buttons == null)
        {
            return null;
        }

        if (currentButtonIndex < 0 ||
            currentButtonIndex >= buttons.Count)
        {
            return null;
        }

        return buttons[currentButtonIndex];
    }

    private void OnDestroy()
    {
        if (tutorialClickArea != null)
        {
            tutorialClickArea.Clicked -= OnTutorialAreaClicked;
        }
    }
}