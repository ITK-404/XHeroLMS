using System.Collections.Generic;
using UnityEngine;

public class BookShelfManager : MonoBehaviour
{
    public CourseLessonTabID CourseID;
    [SerializeField] private BookShelfUI bookShelfUIPrefab;
    [SerializeField] private RectTransform content;
    private BookShelfUI[] bookShelfList;

    [ContextMenu("Load Data")]

    private void Start()
    {
        bookShelfList = GetComponentsInChildren<BookShelfUI>();
        OnLoad();
    }
    private List<BookHandler> books = new();
    private void OnLoad()
    {
        foreach (var item in bookShelfList)
        {
            books.AddRange(item.books);
        }
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            foreach(var book in books)
            {
                book.bookHandleUI.RefreshColor();
            }
        }
    }
}