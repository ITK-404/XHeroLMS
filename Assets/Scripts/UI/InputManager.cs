using MacacaGames;

public class InputManager : Singleton<InputManager>
{
    public InputHandler InputHandler;

    public InputManager()
    {
        InputHandler = new InputHandler();
    }
}
