using UnityEngine;

public class PCInput : BaseInput
{
    public bool isMobile = false;
    private void Update()
    {
        if (InputHandler == null) return;
        MoveHandle();

    }

    private void OnDisable()
    {
        MoveVector = Vector2.zero;
        IsClicked = false;
    }

    private void MoveHandle()
    {
        MoveVector = InputHandler.Player.Move.ReadValue<Vector2>();

        IsClicked = InputBlocker.IsBlocked() ? false : InputHandler.Player.Attack.WasPressedThisFrame();
    }
}
