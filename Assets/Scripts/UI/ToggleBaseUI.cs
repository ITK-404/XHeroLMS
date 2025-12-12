using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleBaseUI : MonoBehaviour
{
    public Button btn;
    public State currentState = State.DeActive;
    public Action<State> OnValueChange;
    public UnityEvent OnToggleOn;
    public UnityEvent OnToggleOff;
  
    private void OnValidate()
    {
        if(btn == null)
        {
            btn = GetComponent<Button>();
        }
    }

    protected virtual void Awake()
    {
        if(btn)
            btn.onClick.AddListener(OnClickButton);
    }

    private void OnDestroy()
    {
        if(btn)
            btn.onClick.RemoveListener(OnClickButton);
    }

    public enum State
    {
        Active,
        DeActive
    }

    public virtual void ChangeState(State newState)
    {
        Debug.Log("Thay đổi state hiện tại: " + newState);
        currentState = newState;
        OnValueChange?.Invoke(currentState);
        if(currentState == State.Active)
        {
            OnToggleOn?.Invoke();
        }
        else
        {
            OnToggleOff?.Invoke();
        }
    }

    public void Toggle()
    {
        if (currentState == State.Active)
        {
            currentState = State.DeActive;
            OnToggleOff?.Invoke();
        }
        else
        {
            currentState = State.Active;
            OnToggleOn?.Invoke();
        }

        ChangeState(currentState);
    }

    public virtual void OnClickButton()
    {
        Toggle();
    }
}
