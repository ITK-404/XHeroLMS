using UnityEngine;
using UnityEngine.UI;
public class TutorialButton : TutorialBase
{
    [SerializeField] private Button btn;

    private void Awake()
    {
        if(btn == null)
        {
            btn = GetComponent<Button>();
        }
        if (btn != null)
        {
            btn.onClick.AddListener(OnDoneTutorial);
        }
    }

    private void OnDestroy()
    {
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnDoneTutorial);
        }
    }
 
}
public class TutorialBase : MonoBehaviour
{
    public string step_ID;
    public bool isUI;

    private bool isClick = false;

    public void OnDoneTutorial()
    {
        if (isClick) return;
        isClick = true;

        gameObject.SetActive(false);
        TutorialManager.Instance.Clear();
    }

    public void ShowTutorial()
    {
        TutorialManager.Instance.ShowTutorial(this);
    }
}