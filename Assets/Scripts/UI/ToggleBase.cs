using UnityEngine;
using UnityEngine.UI;

public class ToggleBase : MonoBehaviour
{
    [SerializeField] protected Toggle toggle;

    protected virtual void Awake()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    protected virtual void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    protected virtual void OnValueChanged(bool value)
    {

    }
}
