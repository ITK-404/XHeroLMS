using UnityEngine;

public class BaseInput : MonoBehaviour
{
    public InputHandler InputHandler;
    public bool IsClicked = false;
    public Vector2 MoveVector;

    private void Awake()
    {
        InputHandler = InputManager.Instance.InputHandler;
        InputHandler.Enable();
        InputHandler.Player.Enable();
    }
}
