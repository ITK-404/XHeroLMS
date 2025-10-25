using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LessonUI : MonoBehaviour
{
    public TextMeshProUGUI titleTMP;
    public Button btn;
    public Color normalColor;
    public Color selectColor;
    public bool isSelect = false;

    public ChapterUI chapterUI;

    public Action<string> OnClickPlayVideo;
    [Header("Data")]

    public string linkVideo2;
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
        titleTMP.color = active ? selectColor : normalColor;
    }
}
