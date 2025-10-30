using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Common base for chapter UI used by learning and review UIs.
// Provides shared references and toggle logic (open/close), and
// basic active/deactive group handling.
public class ChapterBaseUI : MonoBehaviour
{
    [Header("References (shared)")]
    public TextMeshProUGUI titleName;

    public GameObject scrollView;
    public GameObject lessonContainer;
    public Button toggleOpenBtn;
    public Button toggleOffBtn;
    public GameObject activeGroup;
    public GameObject deActiveGroup;

    protected bool isOpen;

    protected virtual void Awake()
    {
        if (toggleOpenBtn != null)
            toggleOpenBtn.onClick.AddListener(ToggleOn);
        if (toggleOffBtn != null)
            toggleOffBtn.onClick.AddListener(ToggleOff);

        // default closed
        ToggleOff();
    }

    protected virtual void OnDestroy()
    {
        if (toggleOpenBtn != null)
            toggleOpenBtn.onClick.RemoveListener(ToggleOn);
        if (toggleOffBtn != null)
            toggleOffBtn.onClick.RemoveListener(ToggleOff);
    }

    // Open chapter content
    public virtual void ToggleOn()
    {
        Debug.Log("Toggle On", gameObject);
        isOpen = true;
        if (scrollView != null)
            scrollView.SetActive(isOpen);
        if (toggleOpenBtn != null)
            toggleOpenBtn.gameObject.SetActive(false);
        if (toggleOffBtn != null)
            toggleOffBtn.gameObject.SetActive(true);
    }

    // Close chapter content
    public virtual void ToggleOff()
    {
        Debug.Log("Toggle Off", gameObject);
        isOpen = false;
        if (scrollView != null)
            scrollView.SetActive(isOpen);
        if (toggleOpenBtn != null)
            toggleOpenBtn.gameObject.SetActive(true);
        if (toggleOffBtn != null)
            toggleOffBtn.gameObject.SetActive(false);
    }


    // visual active/deactive group handling (used by some implementations)
    public virtual void ShowActiveUI(bool active)
    {
        if (activeGroup != null)
            activeGroup.SetActive(active);
        if (deActiveGroup != null)
            deActiveGroup.SetActive(!active);
    }
}