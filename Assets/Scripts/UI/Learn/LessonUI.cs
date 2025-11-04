using System;
using TMPro;
using UnityEngine;
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
    public string courseID;
    public string linkVideo2;
    public string type;
    public bool isSelect = false;
    public string percent;
    public Action<string> OnClickPlayVideo;
    private void Awake()
    {
        btn.onClick.AddListener(OnClickBtn);
        SetActive(false);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickBtn);
    }

    private void OnClickBtn()
    {
        chapterUI.SelectLesson(this);
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
}