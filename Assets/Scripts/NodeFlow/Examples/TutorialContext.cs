using UnityEngine;
// NOTE: Quick using
public class TutorialContext : MonoBehaviour
{
    private const string Prefix = "tutorial_";
    private const int PlayedValue = 1;
    private const int NotPlayedValue = 0;

    [SerializeField] private string tutorialName;

    public bool IsPlayed { get; private set; }

    private void Awake()
    {
        Load();
    }
  
    public void Load()
    {
        if (string.IsNullOrWhiteSpace(tutorialName))
        {
            Debug.LogWarning($"[TutorialContext] tutorialName is empty on '{gameObject.name}'");
            return;
        }

        var tutorialId = GetTutorialId();
        IsPlayed = PlayerPrefs.GetInt(tutorialId, NotPlayedValue) == PlayedValue;
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
        IsPlayed = false;
        PlayerPrefs.DeleteKey(GetTutorialId());
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(tutorialName))
        {
            return;
        }

        var tutorialId = GetTutorialId();
        PlayerPrefs.SetInt(tutorialId, PlayedValue);
        PlayerPrefs.Save();
    }

    private string GetTutorialId()
    {
        return Prefix + tutorialName;
    }
}