using UnityEngine;

public class PopupBaseUI : MonoBehaviour
{
    public GameObject container;
    private bool ownsGameplayLock;

    private void OnValidate()
    {
        if (container == null)
        {
            container = transform.Find("Container").gameObject;
        }
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
        SetGameplayLock(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        SetGameplayLock(false);
    }

    private void OnDisable()
    {
        SetGameplayLock(false);
    }

    private void SetGameplayLock(bool locked)
    {
        if (ownsGameplayLock == locked)
            return;

        if (locked)
        {
            GameplayLock.Lock(GameplayLockReason.UI, GameplayLockTarget.All);
        }
        else
        {
            GameplayLock.Unlock(GameplayLockReason.UI);
        }

        ownsGameplayLock = locked;
    }
}