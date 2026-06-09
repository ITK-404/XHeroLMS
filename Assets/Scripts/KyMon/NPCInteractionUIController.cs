using UnityEngine;

public class NPCInteractionUIController : MonoBehaviour
{
    [SerializeField] private NPCInteractionUIView interactionUIView;
    [SerializeField] private ActionChoiceViewUI actionChoiceViewUI;

    [SerializeField] private AdvanceKyMonCourseUIView advanceCourseUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    private void Awake()
    {
        actionChoiceViewUI.OnShowOptionOne += ActionChoiceViewUIOnOnShowOptionOne;
        actionChoiceViewUI.OnShowOptionTwo += ActionChoiceViewUIOnOnShowOptionTwo;
        
        tabItemManagerUI.OnClickReturnBtnEvent += TabItemManagerUIOnOnClickReturnBtnEvent;
        advanceCourseUI.OnClickReturnEvent += TabItemManagerUIOnOnClickReturnBtnEvent;
        
    }

    private void OnDestroy()
    {
        actionChoiceViewUI.OnShowOptionOne -= ActionChoiceViewUIOnOnShowOptionOne;
        actionChoiceViewUI.OnShowOptionTwo -= ActionChoiceViewUIOnOnShowOptionTwo;
        
        tabItemManagerUI.OnClickReturnBtnEvent -= TabItemManagerUIOnOnClickReturnBtnEvent;
        advanceCourseUI.OnClickReturnEvent -= TabItemManagerUIOnOnClickReturnBtnEvent;
    }

    private void TabItemManagerUIOnOnClickReturnBtnEvent()
    {
        interactionUIView.Show();
        actionChoiceViewUI.Show();
        
        tabItemManagerUI.gameObject.SetActive(false);
        advanceCourseUI.Hide();
    }

    private void ActionChoiceViewUIOnOnShowOptionTwo()
    {
        interactionUIView.Hide();
        actionChoiceViewUI.Hide();

        advanceCourseUI?.Show();
    }

    private void ActionChoiceViewUIOnOnShowOptionOne()
    {
        interactionUIView.Hide();
        actionChoiceViewUI.Hide();
        if (tabItemManagerUI)
        {
            tabItemManagerUI.gameObject.SetActive(true);
        }
    }
}