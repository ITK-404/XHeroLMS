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

    public enum ClickMode
    {
        /// <summary>
        /// Nhận callback từ vùng TutorialClickArea.
        /// </summary>
        TutorialClickArea,

        /// <summary>
        /// Nhận callback trực tiếp từ Button.onClick.
        /// </summary>
        ButtonCallback
    }

    [Serializable]
    public class ButtonCustom
    {
        public Button button;

        [TextArea]
        public string description;
    }

    [Header("Input Mode")]
    [SerializeField]
    private ClickMode clickMode = ClickMode.TutorialClickArea;

    [Tooltip(
        "Khi dùng TutorialClickArea, click vào vùng tutorial " +
        "có gọi luôn onClick của Button thật hay không."
    )]
    [SerializeField]
    private bool invokeButtonWhenAreaClicked;

    [Header("References")]
    [SerializeField]
    private TutorialClickArea tutorialClickArea;

    [Header("Buttons")]
    [SerializeField]
    private List<ButtonCustom> customs = new();

    private int currentButtonIndex;
    private State currentState = State.None;

    /// <summary>
    /// Button đang được đăng ký callback.
    /// Chỉ dùng trong ButtonCallback mode.
    /// </summary>
    private Button subscribedButton;

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);

        currentButtonIndex = 0;
        currentState = State.None;

        ResolveReferences();

        if (!ValidateData())
        {
            ChangeState(State.Completed);
            return;
        }

        RegisterInput();

        ChangeState(State.WaitingForClick);
    }

    public override void Exit(CutsceneContext context = null)
    {
        UnregisterInput();

        if (tutorialClickArea != null)
        {
            tutorialClickArea.DeActive();
        }

        currentState = State.None;

        base.Exit(context);
    }

    public override bool IsCompleted()
    {
        return currentState == State.Completed;
    }

    private void ResolveReferences()
    {
        if (tutorialClickArea != null)
        {
            return;
        }

        if (ClassTutorialFlow.Instance == null)
        {
            return;
        }

        tutorialClickArea = ClassTutorialFlow.Instance.blockingArea;
    }

    private bool ValidateData()
    {
        if (customs == null || customs.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(WaitForClickButtonSequence)}] Button list is empty.",
                this
            );

            return false;
        }

        if (clickMode == ClickMode.TutorialClickArea &&
            tutorialClickArea == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForClickButtonSequence)}] " +
                $"TutorialClickArea is required when using " +
                $"{nameof(ClickMode.TutorialClickArea)} mode.",
                this
            );

            return false;
        }

        return true;
    }

    private void RegisterInput()
    {
        switch (clickMode)
        {
            case ClickMode.TutorialClickArea:
                tutorialClickArea.Clicked -= OnTutorialAreaClicked;
                tutorialClickArea.Clicked += OnTutorialAreaClicked;
                break;

            case ClickMode.ButtonCallback:
                // Callback button sẽ được đăng ký trong FocusCurrentButton().
                break;
        }
    }

    private void UnregisterInput()
    {
        if (tutorialClickArea != null)
        {
            tutorialClickArea.Clicked -= OnTutorialAreaClicked;
        }

        UnsubscribeCurrentButton();
    }

    private void OnTutorialAreaClicked()
    {
        if (currentState != State.WaitingForClick)
        {
            return;
        }

        ButtonCustom buttonCustom = GetCurrentButton();

        if (buttonCustom == null || buttonCustom.button == null)
        {
            MoveToNextButton();
            return;
        }

        /*
         * Trong TutorialClickArea mode, click đang bị vùng block nhận.
         * Vì vậy Button thật thường không nhận được click.
         *
         * Có thể gọi onClick thủ công nếu tutorial cần thực thi logic Button.
         */
        if (invokeButtonWhenAreaClicked)
        {
            buttonCustom.button.onClick.Invoke();
        }

        MoveToNextButton();
    }

    private void OnCurrentButtonClicked()
    {
        if (currentState != State.WaitingForClick)
        {
            return;
        }

        /*
         * Button.onClick đã tự thực thi logic của Button.
         * Ở đây chỉ cần báo tutorial chuyển sang bước kế tiếp.
         */
        MoveToNextButton();
    }

    private void MoveToNextButton()
    {
        /*
         * Gỡ callback của Button hiện tại trước khi đổi index.
         * Chỉ có tác dụng trong ButtonCallback mode.
         */
        UnsubscribeCurrentButton();

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
                UnregisterInput();

                if (tutorialClickArea != null)
                {
                    tutorialClickArea.DeActive();
                }

                break;
        }
    }

    private void FocusCurrentButton()
    {
        ButtonCustom buttonCustom = GetCurrentButton();

        if (buttonCustom == null)
        {
            MoveToNextButton();
            return;
        }

        Button currentButton = buttonCustom.button;

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

        if (ClassTutorialFlow.Instance != null)
        {
            ClassTutorialFlow.Instance.SetInteractZone(buttonRect);
        }

        if (FocusHandManager.Instance != null)
        {
            FocusHandManager.Instance.SetToTargetRect(
                buttonRect,
                buttonCustom.description
            );
        }

        SetupInputForCurrentButton(currentButton);
    }

    private void SetupInputForCurrentButton(Button currentButton)
    {
        switch (clickMode)
        {
            case ClickMode.TutorialClickArea:
                if (tutorialClickArea != null)
                {
                    tutorialClickArea.Active();
                }

                break;

            case ClickMode.ButtonCallback:
                SubscribeCurrentButton(currentButton);
                break;
        }
    }

    private void SubscribeCurrentButton(Button button)
    {
        UnsubscribeCurrentButton();

        if (button == null)
        {
            return;
        }

        subscribedButton = button;
        subscribedButton.onClick.AddListener(OnCurrentButtonClicked);
    }

    private void UnsubscribeCurrentButton()
    {
        if (subscribedButton == null)
        {
            return;
        }

        subscribedButton.onClick.RemoveListener(OnCurrentButtonClicked);
        subscribedButton = null;
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
        UnregisterInput();
    }
}