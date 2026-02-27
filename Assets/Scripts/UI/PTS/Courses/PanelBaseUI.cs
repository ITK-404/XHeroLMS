using DG.Tweening;
using UnityEngine;

public class PanelBaseUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    
    private void OnValidate()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private bool IsTranstion
    {
        get => isTranstion;
    }

    private bool isTranstion = false;
    
    public void Show()
    {
        SetCanvasGroup(true);
    }

    public void Hide()
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