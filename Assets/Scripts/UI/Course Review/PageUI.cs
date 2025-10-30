using UnityEngine;

public class PageUI : MonoBehaviour
{
    public Transform content;
    public int GetChildCount => content.childCount;
}