using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationLoadCoursePopup : PopupBaseUI
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private TMP_Text descriptionText;

    private Action onAcceptEvent;
    private Action onDeclineEvent;

    private void Awake()
    {
        confirmButton.onClick.AddListener(ConfirmButtonClicked);
        declineButton.onClick.AddListener(DeclineButtonClicked);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(ConfirmButtonClicked);
        declineButton.onClick.RemoveListener(DeclineButtonClicked);
    }

    private void DeclineButtonClicked()
    {
        Hide();
    }

    private void ConfirmButtonClicked()
    {
        onAcceptEvent?.Invoke();
        onAcceptEvent = null;
    }


    public void Init(string courseName, Action onAccept)
    {
        onAcceptEvent = onAccept;
        descriptionText.text = $"{prefix}\n<b>{courseName}</b>";
    }

    private const string prefix = "Quý học viên muốn đi đến phòng học";
}