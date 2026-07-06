using UnityEngine;

public class GameplayLockActive : MonoBehaviour
{
    private UIView uiView;
    private bool ownsGameplayLock;

    private void Awake()
    {
        uiView = GetComponent<UIView>();
        if (uiView)
        {
            uiView.OnViewOpened += OnViewOpened;
            uiView.OnViewClosed += OnViewClosed;
        }
        
    }

    private void OnViewClosed()
    {
       SetGameplayLock(false);
    }

    private void OnViewOpened()
    {
        SetGameplayLock(true);
    }

    private void OnDisable()
    {
        SetGameplayLock(false);

        if (uiView)
        {
            uiView.OnViewOpened -= OnViewOpened;
            uiView.OnViewClosed -= OnViewClosed;
        }
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
