using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MailTabHandleView : MonoBehaviour
{
    [SerializeField] private MailViewUI[] views;
    [SerializeField] private Button[] buttons;
    [SerializeField] private MailList mailList;
    [SerializeField] private Transform emptyMailState;

    private void Awake()
    {
        Binding();
        ShowTab(0);
    }

    private void OnDestroy()
    {
        UnBinding();
    }

    private MailContentView GetMailContentView()
    {
        if (MailContentView.Instance != null)
            return MailContentView.Instance;

        return FindFirstObjectByType<MailContentView>(FindObjectsInactive.Include);
    }

    private void HandleButtonVisual(int index)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn != null && btn.image != null)
                btn.image.DOFade(index == i ? 1f : 0f, 0f);
        }
    }

    private void Binding()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(() => ShowTab(index));
        }
    }

    private void UnBinding()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].onClick.RemoveAllListeners();
        }
    }

    private void ShowTab(int index)
    {
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null) continue;

            bool isShow = index == i;
            if (isShow) view.Show();
            else view.Hide();
        }

        var contentView = GetMailContentView();
        if (contentView != null)
            contentView.ResetView();

        NotificationsDetailStaticStore.Reset();

        if (mailList != null && index >= 0 && index < views.Length && views[index] != null)
        {
            mailList.SetRenderTarget(views[index].ContentParent);

            if (index == 0)
                mailList.SetFilter(MailList.MailFilter.System);
            else if (index == 1)
                mailList.SetFilter(MailList.MailFilter.Personal);

            mailList.ForceResetToFirstItem();
        }

        HandleButtonVisual(index);
    }
}