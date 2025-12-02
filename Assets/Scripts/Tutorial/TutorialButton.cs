using UnityEngine;
using UnityEngine.UI;
public class TutorialButton : TutorialBase
{
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
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

public class TutorialChair : MonoBehaviour
{

}
public class TutorialBase : MonoBehaviour
{
    private bool isClick = false;
    public void OnDoneTutorial()
    {
        if (isClick) return;
        isClick = true;
        TutorialManager.Instance.GoNextTutorial();
    }
}