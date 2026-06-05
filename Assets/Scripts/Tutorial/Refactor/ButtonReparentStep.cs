using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonReparentStep : TutorialStepReparentObject
{
    [SerializeField] private Button btn;

    private Action onComplete;
    
    private void OnValidate()
    {
        if (btn == null)
        {
            btn = GetComponent<Button>();
        }
    }

    protected override void OnCustomAwake()
    {
        base.OnCustomAwake();
        btn.onClick.AddListener(OnClickButton);
    }

    protected override void OnCustomDestroy()
    {
        base.OnCustomDestroy();
        btn.onClick.RemoveListener(OnClickButton);
    }

    private void OnClickButton()
    {
        Debug.Log($"Debug> OnClick Button");
        onComplete?.Invoke();
    }

    public override void StartListening(Action onComplete)
    {
        base.StartListening(onComplete);
        this.onComplete = onComplete;
    }

    public override void StopListening()
    {
        base.StopListening();
        this.onComplete = null;
    }
}