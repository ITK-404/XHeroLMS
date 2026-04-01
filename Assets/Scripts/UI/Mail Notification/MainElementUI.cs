using UnityEngine;
using UnityEngine.UI;

public class MainElementUI : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private MailTextConfig readConfig;
    [SerializeField] private MailTextConfig unreadConfig;

    [Header("Refs")]
    [SerializeField] private MailElementVisualUI visual;
    [SerializeField] private Button btn;
    [SerializeField] private NotificationsDetailLoader detailLoader;

    private string notificationId;
    private string courseId;
    private NotificationMailItem currentData;

    private void Awake()
    {
        if (btn == null)
            btn = GetComponent<Button>();

        if (visual == null)
            visual = GetComponent<MailElementVisualUI>();

        if (detailLoader == null)
            detailLoader = FindFirstObjectByType<NotificationsDetailLoader>();

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
        courseId = GetCourseId(data);

        ApplyState();

        Debug.Log($"[MainElementUI] Bind item: title={data?.title}, notificationId={notificationId}, courseId={courseId}");
    }

    public void SetDetailLoader(NotificationsDetailLoader loader)
    {
        detailLoader = loader;
    }

    private void OnClickItem()
    {
        Debug.Log($"[MainElementUI] Click notificationId={notificationId}, courseId={courseId}");

        if (currentData == null)
        {
            Debug.LogWarning("[MainElementUI] currentData đang null.");
            return;
        }

        if (!currentData.isRead)
        {
            currentData.isRead = true;
            ApplyState();
        }

        if (detailLoader == null)
            detailLoader = FindFirstObjectByType<NotificationsDetailLoader>();

        if (detailLoader == null)
        {
            Debug.LogWarning("[MainElementUI] detailLoader đang null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(notificationId))
        {
            Debug.LogWarning("[MainElementUI] notificationId đang rỗng.");
            return;
        }

        string token = TokenStore.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[MainElementUI] TokenStore.AccessToken đang rỗng.");
            return;
        }

        Debug.Log($"[MainElementUI] Load notification detail with notificationId={notificationId}");

        // Truyền notificationId đúng mục đích cho NotificationsDetailLoader
        detailLoader.LoadById(notificationId, token);
    }

    private string GetCourseId(NotificationMailItem data)
    {
        if (data == null)
            return "";

        if (data.additionalData == null)
            return "";

        return string.IsNullOrWhiteSpace(data.additionalData.courseId)
            ? ""
            : data.additionalData.courseId.Trim();
    }

    private void ApplyState()
    {
        if (visual == null || currentData == null)
            return;

        bool isUnread = !currentData.isRead;

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
        if (visual == null)
            return;

        bool isUnreadPreview = true;
        visual.SetConfig(isUnreadPreview ? unreadConfig : readConfig);
        visual.SetReadStateText(isUnreadPreview ? "Chưa đọc" : "Đã đọc");
    }
}