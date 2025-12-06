using UnityEngine;
using UnityEngine.UI;

public class PlayVideoHandleUI : MonoBehaviour
{
    [SerializeField] PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] private CourseReviewUI courseReviewUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    public Button skipButton;
    [SerializeField] RawImage rawImage;
    [SerializeField] private GameObject container;
    public Toggle autoSkipToggle;
    private void Awake()
    {
        container.gameObject.SetActive(false);
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}