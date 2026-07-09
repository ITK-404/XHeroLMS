using System;
using UnityEngine;

public enum RoomPlayerState
{
    Global,
    Focus
}
public class RoomController : MonoBehaviour
{
    private FocusStateMachine focusStateMachine;
    public void ChangeState(string stateName)
    {
    }
}

public class FocusStateMachine : MonoBehaviour
{
    [SerializeField] private FocusMode globalMode;
    [SerializeField] private FocusMode[] focusModes;

    private FocusMode currentFocusMode;

    public void ChangeMode(FocusMode focusMode)
    {
        if (currentFocusMode != focusMode)
        {
            currentFocusMode.Exit();
        }

        currentFocusMode = focusMode;
        currentFocusMode.Enter();
    }
}

public class FocusMode : MonoBehaviour
{
    public virtual void Enter(){}
    public virtual void Exit(){}
}

public class ExploreMode : FocusMode
{
    
}