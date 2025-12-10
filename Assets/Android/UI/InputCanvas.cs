using UnityEngine;

public class InputCanvas : MonoBehaviour
{
    public GameObject container;
    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}
