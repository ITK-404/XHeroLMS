using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI titleName;
    public GameObject lessonContainer;
    public Button toggleOpenBtn;
    public Button toggleOffBtn;
    [Header("Setting")]
    [SerializeField] private bool isOpen = false;

    private void Awake()
    {
        toggleOpenBtn.onClick.AddListener(ToggleOn);
        toggleOffBtn.onClick.AddListener(ToggleOff);
        if (isOpen)
        {
            ToggleOn();
        }
        else
        {
            ToggleOff();
        }
    }

    private void OnDestroy()
    {
        toggleOpenBtn.onClick.RemoveListener(ToggleOn);
        toggleOffBtn.onClick.RemoveListener(ToggleOff);
    }

    private void ToggleOn()
    {
        isOpen = true;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(false);
        toggleOffBtn.gameObject.SetActive(true);
    }

    private void ToggleOff()
    {
        isOpen = false;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(true);
        toggleOffBtn.gameObject.SetActive(false);
    }


}

public class LessonUI : MonoBehaviour
{

}

public class QuestionUI : MonoBehaviour
{

}
