using System;
using UnityEngine;
public class BookShelfUI : MonoBehaviour
{
    [HideInInspector] public BookHandler[] books ;
    private RectTransform boundRect;
    private void Awake()
    {
        books = GetComponentsInChildren<BookHandler>();
    }

    private void Start()
    {
        foreach (var bookHandler in books)
        {
            var bookModel = bookHandler.bookModel;
            bookModel.CanPlayerClick = CanPlayerClickBook;
        }
    }

    public void SetClickBounds(RectTransform bounds) => boundRect = bounds;

    private bool CanPlayerClickBook() => TryClick(Input.mousePosition);

    private bool TryClick(Vector2 screenPosition)
    {
        if (boundRect == null)
            return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            boundRect,
            screenPosition,
            Camera.main// hoặc Camera.main nếu Canvas là Screen Space - Camera
        );
    }
}
