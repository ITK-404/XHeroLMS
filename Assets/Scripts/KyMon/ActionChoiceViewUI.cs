using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionChoiceViewUI : UIView
{
    [SerializeField] private Button optionOneBtn;
    [SerializeField] private Button optionTwoBtn;

   
    public event Action OnShowOptionOne;
    public event Action OnShowOptionTwo;

    protected override void Awake()
    {
        base.Awake();
        optionOneBtn.onClick.AddListener(OnClickOptionOne);
        optionTwoBtn.onClick.AddListener(OnClickOptionOne);
    }

    private void OnDestroy()
    {
        optionOneBtn.onClick.RemoveListener(OnClickOptionOne);
        optionTwoBtn.onClick.RemoveListener(OnClickOptionOne);
    }

    private void OnClickOptionOne()
    {
        OnShowOptionOne?.Invoke();
    }
   
    private void OnClickOptionTwo()
    {
        OnShowOptionTwo?.Invoke();
    }
}