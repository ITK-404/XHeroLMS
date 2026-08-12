using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// NOTE: Quick using
public class TutorialContext : MonoBehaviour
{
    private const string Prefix = "tutorial_";
    private const int PlayedValue = 1;
    private const int NotPlayedValue = 0;

    [SerializeField] private string tutorialName;
    private static HashSet<string> playedTutorialIds = new();
    public bool IsPlayed { get; private set; }

    private void Start()
    {
        CheckIsTutorialPlayed();
    }

    public void CheckIsTutorialPlayed()
    {
        if (!IsSaveIdValid())
        {
            return;
        }
        var tutorialId = GetTutorialId();
        IsPlayed = playedTutorialIds.Contains(tutorialId);
        Debug.Log($"TutorialContext trang thai is played {tutorialId} {IsPlayed}");
    }

    public static void ClearPlayedTutorialIds()
    {
        playedTutorialIds.Clear();
    }
    
    public static void Load(List<string> saveTutorialIds)
    {
        if (saveTutorialIds == null || saveTutorialIds.Count == 0)
        {
            return;
        }

        foreach (var item in saveTutorialIds)
        {
            Debug.Log($"[TutorialContext] tutorialID: {item}");
            playedTutorialIds.Add(item);
        }
    }

    public static List<string> GetSaveList()
    {
        if (playedTutorialIds == null || playedTutorialIds.Count == 0)
            return new List<string>();
        
        List<string> saveTutorialIds = new();
        foreach (var item in playedTutorialIds)
        {
            saveTutorialIds.Add(item);
        }
        return saveTutorialIds;
    }

    public void MarkAsPlayed()
    {
        if (IsPlayed)
        {
            return;
        }

        IsPlayed = true;
        Save();
    }

    public bool ShouldShow()
    {
        return !IsPlayed;
    }

    [ContextMenu("ResetTutorial")]
    public void ResetTutorial()
    {
    }

    private void Save()
    {
        if (IsSaveIdValid() == false) return;

        var tutorialId = GetTutorialId();
        Debug.Log($"TutorialContext save {tutorialId}");
        playedTutorialIds.Add(tutorialId);
        if(TokenStore.IsAuthenticated == false) return;
        GameInitializer.Instance.GameSessionHandler.SaveSession().Forget();
    }

    [ContextMenu("DebugTest")]

    private bool IsSaveIdValid()
    {
        if (string.IsNullOrEmpty(tutorialName)) return false;
        
        return true;
    }
    
    private string GetTutorialId()
    {
        return Prefix + tutorialName;
    }
}