using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookPageCreator : MonoBehaviour
{
    public int itemsPerPage = 10;
    public PageUI pageLeftPrefab;
    public PageUI pageRightPrefab;
    public Transform container;

    public Button leftBtn;
    public Button rightBtn;
    private List<PageUI> pagesList = new();

    private int currentIndex = 0;

    private void Awake()
    {
        leftBtn.onClick.AddListener(TurnLeft);
        rightBtn.onClick.AddListener(TurnRight);
    }

    private void OnDestroy()
    {
        leftBtn.onClick.RemoveListener(TurnLeft);
        rightBtn.onClick.RemoveListener(TurnRight);
    }

    private void TurnLeft()
    {
        if (pagesList.Count == 0) return;
        currentIndex = Mathf.Max(0, currentIndex - 2);
        ShowPageByIndex(currentIndex);
    }

    private void TurnRight()
    {
        if (pagesList.Count == 0) return;
        currentIndex = Mathf.Min(currentIndex + 2, pagesList.Count - 1);
        ShowPageByIndex(currentIndex);
    }

    public void ClearExistPages()
    {
        foreach (var item in pagesList)
        {
            Destroy(item.gameObject);
        }
        pagesList.Clear();
    }

    public Transform TryGetOrCreatePageHolder()
    {
        foreach (var item in pagesList)
        {
            if (item.GetChildCount < itemsPerPage)
            {
                return item.content;
            }
        }

        var count = pagesList.Count;
        PageUI pageUI;
        if (count % 2 == 0)
        {
            pageUI = Instantiate(pageLeftPrefab, container);
        }
        else
        {
            pageUI = Instantiate(pageRightPrefab, container);
        }

        pagesList.Add(pageUI);
        return pageUI.content;
    }

    public void InitFirstPage()
    {
        currentIndex = 0;
        ShowPageByIndex(currentIndex);
    }

    private void ShowPageByIndex(int index)
    {
        if (pagesList.Count == 0) return;

        // clamp index
        index = Mathf.Clamp(index, 0, pagesList.Count - 1);
        currentIndex = index;

        for (int i = 0; i < pagesList.Count; i++)
        {
            bool shouldShow = (i == index) || (i == index + 1);
            pagesList[i].gameObject.SetActive(shouldShow);
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        leftBtn.interactable = currentIndex > 0;
        rightBtn.interactable = (currentIndex + 2) < pagesList.Count;
    }
}