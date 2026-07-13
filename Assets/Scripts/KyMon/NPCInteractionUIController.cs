using UnityEngine;

public class NPCInteractionUIController : MonoBehaviour
{
    [SerializeField] private NPCInteractionUIView interactionUIView;
    [SerializeField] private ActionChoiceViewUI actionChoiceViewUI;

    [SerializeField] private AdvanceKyMonCourseUIView advanceCourseUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    private void Awake()
    {
        actionChoiceViewUI.OnShowOptionOne += HandleShowOptionOne;
        actionChoiceViewUI.OnShowOptionTwo += HandleShowOptionTwo;

        tabItemManagerUI.OnClickReturnBtnEvent += HandleReturnButtonClicked;
        advanceCourseUI.OnClickReturnEvent += HandleReturnButtonClicked;
    }

    private void OnDestroy()
    {
        actionChoiceViewUI.OnShowOptionOne -= HandleShowOptionOne;
        actionChoiceViewUI.OnShowOptionTwo -= HandleShowOptionTwo;

        tabItemManagerUI.OnClickReturnBtnEvent -= HandleReturnButtonClicked;
        advanceCourseUI.OnClickReturnEvent -= HandleReturnButtonClicked;
    }

    private void HandleReturnButtonClicked()
    {
        interactionUIView.Show();
        actionChoiceViewUI.Show();
        interactionUIView.ShowSupportChatBox();

        tabItemManagerUI.gameObject.SetActive(false);
        advanceCourseUI.Hide();
    }

    private void HandleShowOptionTwo()
    {
        interactionUIView.Hide();
        actionChoiceViewUI.Hide();

        advanceCourseUI.Show();
    }

    private void HandleShowOptionOne()
    {
        interactionUIView.Hide();
        actionChoiceViewUI.Hide();

        if (tabItemManagerUI)
        {
            tabItemManagerUI.gameObject.SetActive(true);
        }
    }
}