using System;
using System.Collections.Generic;
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
        //string key = $"{TokenStore.UserID}" + keyPlayedBefore;
        //if(!PlayerPrefs.HasKey(key))
        //{
        //    PlayerPrefs.SetInt(key, 1);
        //    return;
        //}

        worldTutorialStep.gameObject.SetActive(false);
        standupUI.gameObject.SetActive(false);
    }
    
    private void Load()
    {
        isPlayedBefore = PlayerPrefs.GetInt($"{TokenStore.UserID}" + keyPlayedBefore, 0) == 1;
    }

    private void Awake()
    {
        Instance = this;
        ShowStep(0);
        Load();
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
        return;
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
                break;
            case 2:
                // Show world tutorial
                standupUI.gameObject.SetActive(true);
                worldTutorialStep.gameObject.SetActive(false);
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
