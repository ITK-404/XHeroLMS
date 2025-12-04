using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClickExit);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickExit);
    }

    private void OnClickExit()
    {
        Application.Quit();
    }
}
