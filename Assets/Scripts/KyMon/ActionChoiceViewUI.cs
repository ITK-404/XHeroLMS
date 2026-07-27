using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionChoiceViewUI : UIView
{
    [SerializeField] private Button optionOneBtn;
    [SerializeField] private Button optionTwoBtn;
    [SerializeField] private Button returnBtn;

    public event Action OnClickReturnBtn;
   
    public event Action OnShowOptionOne;
    public event Action OnShowOptionTwo;

    protected override void Awake()
    {
        base.Awake();
        optionOneBtn.onClick.AddListener(OnClickOptionOne);
        optionTwoBtn.onClick.AddListener(OnClickOptionTwo);
        returnBtn.onClick.AddListener(ClickReturnBtn);
        
    }

    private void OnDestroy()
    {
        optionOneBtn.onClick.RemoveListener(OnClickOptionOne);
        optionTwoBtn.onClick.RemoveListener(OnClickOptionTwo);
        returnBtn.onClick.RemoveListener(ClickReturnBtn);
    }

    private void OnClickOptionOne()
    {
        OnShowOptionOne?.Invoke();
    }
    public void ClickReturnBtn() => OnClickReturnBtn?.Invoke();
    
   
    private void OnClickOptionTwo()
    {
        OnShowOptionTwo?.Invoke();
    }
}

