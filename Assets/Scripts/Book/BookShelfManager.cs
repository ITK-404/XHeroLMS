using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookShelfManager : MonoBehaviour
{
    public CourseLessonTabID CourseID;
    [SerializeField] private BookShelfUI bookShelfUIPrefab;
    [SerializeField] private RectTransform content;

    [SerializeField] private ScrollRect scrollRect;
    private BookShelfUI[] bookShelfList;

    private List<BookHandler> books = new();

    private void Start()
    {
        bookShelfList = GetComponentsInChildren<BookShelfUI>();
        scrollRect = GetComponent<ScrollRect>();
        OnLoad();
        SetupClickBounds();
    }

    private void SetupClickBounds()
    {
        if (scrollRect == null)
        {
            return;
        }
        foreach (var bookShelf in bookShelfList)
        {
            bookShelf.SetClickBounds(scrollRect.viewport);
        }
    }
    
    private void OnLoad()
    {
        foreach (var item in bookShelfList)
        {
            books.AddRange(item.books);
        }

        SplitBookShelf();
    }

    private void SplitBookShelf()
    {
        int activeBook = books.Count / 3;
        for (int i = 0; i < books.Count; i++)
        {
            BookHandler book = books[i];
            if (i < activeBook)
            {
                book.bookModel.ActiveGrayScale();
            }
            else
            {
                book.bookModel.DeActiveGrayScale();
            }
            // book.bookHandleUI.priceText.text = "1.000.000đ";
            book.bookHandleUI.RefreshColor();
        }
    }

    public void ResetScrollContent()
    {
        content.anchoredPosition = new Vector2(0, 0);
    }
}