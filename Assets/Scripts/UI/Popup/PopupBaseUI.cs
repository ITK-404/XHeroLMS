using UnityEngine;

public class PopupBaseUI : MonoBehaviour
{
    public GameObject container;
    private void OnValidate()
    {
        if(container == null)
        {
            container = transform.Find("Container").gameObject;
        }
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        InputBlocker.SuppressGameplayInput();
    }
}
