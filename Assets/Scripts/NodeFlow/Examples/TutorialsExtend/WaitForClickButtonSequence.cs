using System;
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

    [Serializable]
    public class ButtonCustom
    {
        public Button button;
        public string description;

    }

    [Header("References")] [SerializeField]
    private TutorialClickArea tutorialClickArea;

    [Header("Buttons")]
    // [SerializeField] private List<Button> buttons = new();
    [SerializeField]
    private List<ButtonCustom> customs = new();

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

        if (customs == null || customs.Count == 0)
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

        var currentButton = GetCurrentButton();

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

        if (currentButtonIndex >= customs.Count)
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
        var buttonCustom = GetCurrentButton();
        if (buttonCustom == null) return;

        var currentButton = buttonCustom.button;

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
        FocusHandManager.Instance.SetToTargetRect(buttonRect, buttonCustom.description);
    }

    private ButtonCustom GetCurrentButton()
    {
        if (customs == null)
        {
            return null;
        }

        if (currentButtonIndex < 0 ||
            currentButtonIndex >= customs.Count)
        {
            return null;
        }

        return customs[currentButtonIndex];
    }

    private void OnDestroy()
    {
        if (tutorialClickArea != null)
        {
            tutorialClickArea.Clicked -= OnTutorialAreaClicked;
        }
    }
}