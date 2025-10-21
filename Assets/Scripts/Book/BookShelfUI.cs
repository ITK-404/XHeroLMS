using UnityEngine;
public class BookShelfUI : MonoBehaviour
{
    [HideInInspector] public BookHandler[] books ;
    private void Awake()
    {
        books = GetComponentsInChildren<BookHandler>();
    }
}
