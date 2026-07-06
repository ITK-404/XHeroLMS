using UnityEngine;

public class PCInput : BaseInput
{
    public bool isMobile = false;
    public Vector2 delta;
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
        MoveVector = GameplayLock.IsLocked(GameplayLockTarget.Movement)
            ? Vector2.zero
            : InputHandler.Player.Move.ReadValue<Vector2>();

        IsClicked = !GameplayLock.IsLocked(GameplayLockTarget.Interact)
                    && InputHandler.Player.Attack.WasPressedThisFrame();
    }
}
