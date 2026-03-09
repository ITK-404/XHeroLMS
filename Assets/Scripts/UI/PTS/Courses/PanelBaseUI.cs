using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PanelBaseUI : MonoBehaviour
{
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected ScrollRect scrollView;
    private void OnValidate()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (scrollView == null)
        {
            scrollView = GetComponent<ScrollRect>();
        }
    }

    private bool IsTranstion
    {
        get => isTranstion;
    }

    private bool isTranstion = false;
    
    public virtual void Show()
    {
        ResetScrollView();
        SetCanvasGroup(true);
    }

    private void ResetScrollView()
    {
        if (scrollView)
        {
            scrollView.horizontalNormalizedPosition = 1;
            scrollView.verticalNormalizedPosition = 1;
        }
    }

    public virtual void Hide()
    {
        SetCanvasGroup(false);
    }

    private void SetCanvasGroup(bool enable)
    {
        _canvasGroup.DOKill();
        _canvasGroup.gameObject.SetActive(enable);
        _canvasGroup.DOFade(enable ? 1 : 0, enable ? 0.25f : 0f);
    }
}