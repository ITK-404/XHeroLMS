using UnityEngine;
using UnityEngine.UI;

public class LessonToggleVisual : MonoBehaviour
{
    [SerializeField] private Button activeBtn;
    [SerializeField] private Button deActiveBtn;
    [SerializeField] private ToggleBaseUI toggleBaseUI;

    private void Awake()
    {
        activeBtn.onClick.AddListener(ActiveToggle);
        deActiveBtn.onClick.AddListener(DeActiveToggle);

        toggleBaseUI.OnValueChange += HandleStateChange;
    }

    private void HandleStateChange(ToggleBaseUI.State state)
    {
        if (state == ToggleBaseUI.State.Active)
        {
            activeBtn.gameObject.SetActive(false);
            deActiveBtn.gameObject.SetActive(true);
        }
        else
        {
            activeBtn.gameObject.SetActive(true);
            deActiveBtn.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        activeBtn.onClick.RemoveListener(ActiveToggle);
        deActiveBtn.onClick.RemoveListener(DeActiveToggle);

        toggleBaseUI.OnValueChange -= HandleStateChange;
    }

    private void ActiveToggle()
    {
        toggleBaseUI.ChangeState(ToggleBaseUI.State.Active);
    }

    private void DeActiveToggle()
    {
        toggleBaseUI.ChangeState(ToggleBaseUI.State.DeActive);
    }
}