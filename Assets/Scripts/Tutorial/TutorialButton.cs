using UnityEngine;
using UnityEngine.UI;
public class TutorialButton : MonoBehaviour
{
    public string tutorialName;
    private Button btn;
    private bool isClick = false;
    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
        }
    }
    public void OnClick()
    {
        if (isClick) return;
        isClick = true;
        TutorialManager.Instance.GoNextTutorial();
    }
}
