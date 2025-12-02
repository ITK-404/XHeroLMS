using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class TutorialStep
{
    public string step_ID;
    public bool isShowingUI = false;
    public Transform popupUI;
    
    public bool CanShowUI()
    {
        if (isShowingUI)
        {
            if(popupUI == null)
            {
                Debug.Log("Popup đang rỗng");
            }
            return popupUI != null;
        }
        return false;
    }
    
}public class TutorialHandler : MonoBehaviour
{
    public List<TutorialStep> tutorialSteps = new();
    private int index = 0;
    private void Awake()
    {
        EventHub.OnSendTutorialStep += TryValidStep;
    }

    private void TryValidStep(string step_ID)
    {
        if (index < 0 || index >= tutorialSteps.Count)
        {
            Debug.LogWarning($"Current index out of range: {index}");
            return;
        }
        var currentStep = tutorialSteps[index];
        if (!string.Equals(currentStep.step_ID, step_ID, StringComparison.Ordinal))
            return;

        // đi tới step tiếp theo
        // nếu có UI thì hiện ra

        if (currentStep.CanShowUI())
        {
            currentStep.popupUI.gameObject.SetActive(false);
        }

        var nextStep = tutorialSteps[index + 1];

        if (nextStep.CanShowUI())
        {
            currentStep.popupUI.gameObject.SetActive(true);
        }
    }

}
public class TutorialManager : MonoBehaviour
{
    public GameObject tutorialPrefab;
    private GameObject currentTutorial;
    public static TutorialManager Instance;
    public List<TutorialButton> tutorialButtons = new();
    public int currentIndex = 0;
    private bool isPlayed = false;
    // Assign the Canvas that contains the UI (set in inspector)
    public Canvas uiCanvas;
    private string tutorialKey = "Tutorial_1";
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (PlayerPrefs.HasKey(tutorialKey))
        {
            isPlayed = PlayerPrefs.GetInt(tutorialKey) == 1;
        }
        if (isPlayed)
        {
            return;
        }
        StartTutorial();
    }

    [ContextMenu("Delete Key")]
    private void DeleteKey()
    {
        if (PlayerPrefs.HasKey(tutorialKey))
        {
            PlayerPrefs.DeleteKey(tutorialKey);
        }
    }

    public void StartTutorial()
    {
        if (tutorialButtons == null || tutorialButtons.Count == 0)
            return;
        currentIndex = 0;
        UpdateCurrentTutorialAtButton(tutorialButtons[currentIndex]);
    }

    public void GoNextTutorial()
    {
        if (tutorialButtons == null || tutorialButtons.Count == 0)
            return;

        currentIndex++;
        if (currentIndex >= tutorialButtons.Count)
        {
            isPlayed = true;
            PlayerPrefs.SetInt(tutorialKey, 1);
            if (currentTutorial != null)
                Destroy(currentTutorial);
            return;
        }
        Debug.Log("GoNextTutorial: " + currentIndex);
        UpdateCurrentTutorialAtButton(tutorialButtons[currentIndex]);
    }

    public void ShowTutorial(TutorialButton button)
    {
        if (button == null || tutorialButtons == null)
            return;

        var index = tutorialButtons.IndexOf(button);
        if (index < 0)
            return;

        currentIndex = index;
        UpdateCurrentTutorialAtButton(button);
    }


    private void UpdateCurrentTutorialAtButton(TutorialButton button)
    {
        if (uiCanvas == null || tutorialPrefab == null)
            return;

        var canvasRect = uiCanvas.GetComponent<RectTransform>();
        var buttonRect = button.GetComponent<RectTransform>();
        if (canvasRect == null || buttonRect == null)
            return;

        if (currentTutorial != null)
            Destroy(currentTutorial);

        currentTutorial = Instantiate(tutorialPrefab, uiCanvas.transform, false);

        var currentRect = currentTutorial.GetComponent<RectTransform>();
        if (currentRect == null)
            return;

        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, buttonRect.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out localPoint);

        currentRect.anchoredPosition = localPoint;
    }

}