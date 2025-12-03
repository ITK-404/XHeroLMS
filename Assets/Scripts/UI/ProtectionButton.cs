using UnityEngine;
using UnityEngine.UI;

public class ProtectionButton : MonoBehaviour
{
    private Button btn;
    private float timer;

    private static float defaultTime = 1;
    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0)
            {
                btn.interactable = true;
            }
        }

    }

    private void OnEnable()
    {
        timer = defaultTime;
    }
}
