using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LessonUI : MonoBehaviour
{
    [Header("References")]
    public ChapterUI chapterUI;
    public GameObject frameHighlight;
    
    [Header("UI")]
    public Button btn;
    public TextMeshProUGUI titleTMP;
    public Color textNormalColor;
    public Color textSelectColor;
    public Image iconImg;
    public Sprite onActiveIcon;
    public Sprite onDeActiveIcon;

    [Header("Data")]
    public string lessonID;
    public string linkVideo2;
    public string type;
    public bool isSelect = false;
    public string percent;
    public Action<string> OnClickPlayVideo;
    [Header("Learning progress")]
    public float duration;
    public float progressTime;

    public Action<LessonUI> OnSelected;

    private void Awake()
    {
        btn.onClick.AddListener(OnClickBtn);
        // SetActive(false);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickBtn);
    }

    private void OnClickBtn()
    {
        chapterUI.SelectLesson(this);
        OnSelected?.Invoke(this);
        OnClickPlayVideo?.Invoke(linkVideo2);
    }

    public void SetActive(bool active)
    {
        Debug.Log("Set active lesson: " + active, gameObject);
        isSelect = active;
        titleTMP.color = active ? textSelectColor : textNormalColor;
        frameHighlight.gameObject.SetActive(active);
        
        iconImg.sprite = active ? onActiveIcon : onDeActiveIcon;
    }

    public void SetHover(bool hover) 
    {
        titleTMP.color = hover ? textSelectColor : textNormalColor;
        frameHighlight.gameObject.SetActive(hover);
    }

    public void TryUpdateProgress(float newProgressTime)
    {
        if(IsLessonDone())
        {
            progressTime = duration;
            return;
        }
        progressTime = Mathf.Clamp(newProgressTime + 1, progressTime, duration);
    }

    public bool IsLessonDone()
    {
        return progressTime >= duration - 60;
    }
}

