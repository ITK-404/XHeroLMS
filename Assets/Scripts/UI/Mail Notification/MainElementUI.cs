using UnityEngine;
using UnityEngine.UI;

public class MainElementUI : MonoBehaviour
{
    [SerializeField] private MailTextConfig readConfig;
    [SerializeField] private MailTextConfig unreadConfig;
    [SerializeField] private bool isUnread = true;
    [SerializeField] private MailElementVisualUI visual;
    [SerializeField] private Button btn;

    private string notificationId;
    private NotificationMailItem currentData;

    private void Awake()
    {
        if (btn == null)
            btn = GetComponent<Button>();

        if (btn != null)
            btn.onClick.AddListener(OnClickItem);
    }

    private void OnDestroy()
    {
        if (btn != null)
            btn.onClick.RemoveListener(OnClickItem);
    }

    public void Bind(NotificationMailItem data)
    {
        currentData = data;
        notificationId = data != null ? data._id : "";
        isUnread = data != null ? !data.isRead : true;

        ApplyState();
    }

    private void OnClickItem()
    {
        Debug.Log($"[Notification] Click ID = {notificationId}");

        // click xong chuyển sang đã đọc
        if (isUnread)
        {
            isUnread = false;
            ApplyState();
        }
    }

    private void ApplyState()
    {
        if (visual == null)
            return;

        visual.SetConfig(isUnread ? unreadConfig : readConfig);
        visual.SetReadStateText(isUnread ? "Chưa đọc" : "Đã đọc");
    }

    private void OnValidate()
    {
        if (visual == null)
            visual = GetComponent<MailElementVisualUI>();

        if (btn == null)
            btn = GetComponent<Button>();
    }

    private void OnDrawGizmosSelected()
    {
        if (visual != null)
        {
            visual.SetConfig(isUnread ? unreadConfig : readConfig);
            visual.SetReadStateText(isUnread ? "Chưa đọc" : "Đã đọc");
        }
    }
}