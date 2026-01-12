using UnityEngine;

public class AreaScrollViewUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private AreaDisplayManager areaDisplayManager;
    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}