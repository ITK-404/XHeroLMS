using UnityEngine;

public class MailViewUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentParent;
    public Transform ContentParent => contentParent;

    public void Show()
    {
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }
}