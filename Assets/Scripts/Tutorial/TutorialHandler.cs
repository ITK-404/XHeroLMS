using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    private int index = 0;
    public GameObject worldTutorialStep;
    public GameObject standupUI;
    public static TutorialHandler Instance;
    private const string keyPlayedBefore = "TutorialPlayedBefore";
    private bool isPlayedBefore = false;
    public void Save()
    {
        string key = $"{TokenStore.UserID}" + keyPlayedBefore;

        Debug.Log("Save key");
        PlayerPrefs.SetInt(key, 1);
    }

    private void Load()
    {
        if (PlayerPrefs.HasKey($"{TokenStore.UserID}" + keyPlayedBefore))
        {
            isPlayedBefore = false;
            return;
        }
        isPlayedBefore = PlayerPrefs.GetInt($"{TokenStore.UserID}" + keyPlayedBefore, 0) == 1;
    }

    private void Awake()
    {
        Instance = this;
        worldTutorialStep.gameObject.SetActive(false);
        standupUI.gameObject.SetActive(false);
        Load();
        ShowStep(0);
    }

    public void IncreaseStep()
    {
        ShowStep(index);
    }

    public bool IsStep(int index)
    {
        return this.index == index;
    }

    public void ShowStep(int index)
    {
        switch (index)
        {
            case 0:
                // Show world tutorial
                worldTutorialStep.gameObject.SetActive(true);
                standupUI.gameObject.SetActive(false);
                break;
            case 1:
                // Show world tutorial
                standupUI.gameObject.SetActive(true);
                worldTutorialStep.gameObject.SetActive(false);
                standupUI.GetComponentInChildren<TextMeshProUGUI>().text = "Click để ngồi";

                break;
            case 2:
                // Show world tutorial
                standupUI.gameObject.SetActive(true);
                standupUI.GetComponentInChildren<TextMeshProUGUI>().text = "Click để đứng";
                worldTutorialStep.gameObject.SetActive(false);

                Save();

                break;
            default:
                break;
        }
    }

    public bool IsPlayedBefore()
    {
        return false;
    }
}
