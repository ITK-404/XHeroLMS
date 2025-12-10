using UnityEngine;

public class PCInput : BaseInput
{
    public bool isMobile = false;
    public float xInput, yInput;

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
        Vector2 MoveVector = InputHandler.Player.Move.ReadValue<Vector2>();
        xInput = MoveVector.x;
        yInput = MoveVector.y;

        IsClicked = InputBlocker.IsBlocked() ? false : InputHandler.Player.Attack.WasPressedThisFrame();
    }
}
