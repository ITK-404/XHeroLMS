using System.Collections.Generic;
using UnityEngine;

public class BookShelfManager : MonoBehaviour
{
    [SerializeField] private BookShelfUI bookShelfUIPrefab;

    [SerializeField] private BookShelfUI[] bookShelfList;

    [ContextMenu("Load Data")]

    private void Start()
    {
        bookShelfList = GetComponentsInChildren<BookShelfUI>();
        OnLoad();
    }

    private void OnLoad()
    {
        var books = new List<BookHandler>();
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
                book.bookHandle.ActiveGrayScale();
            }
            else
            {
                book.bookHandle.DeActiveGrayScale();
            }
            book.bookHandleUI.priceText.text = "1.000.000đ";
            book.bookHandleUI.RefreshColor();

        }
    }
}