using TMPro;
using UnityEngine;

public class MailContentView : MonoBehaviour
{
    public static MailContentView Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleTmp;
    [SerializeField] private TextMeshProUGUI contentTmp;
    [SerializeField] private TextMeshProUGUI courseNameTmp;

    [Header("View Root")]
    [SerializeField] private GameObject viewRoot;

    private bool isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MailContentView] Đã có instance khác, huỷ object mới.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        NotificationsDetailStaticStore.OnChanged += RefreshView;

        InitializeOnce();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        NotificationsDetailStaticStore.OnChanged -= RefreshView;
    }

    private void InitializeOnce()
    {
        if (isInitialized) return;
        isInitialized = true;
        ResetView();
    }

    public void Show()
    {
        if (viewRoot != null)
            viewRoot.SetActive(true);
    }

    public void Hide()
    {
        if (viewRoot != null)
            viewRoot.SetActive(false);
    }

    public void Clear()
    {
        if (titleTmp != null)
            titleTmp.text = "";

        if (contentTmp != null)
            contentTmp.text = "";

        if (courseNameTmp != null)
            courseNameTmp.text = "";
    }

    public void ResetView()
    {
        Clear();
        Hide();
    }

    private void Bind(NotificationMailItem data)
    {
        if (data == null)
        {
            ResetView();
            return;
        }

        if (titleTmp != null)
            titleTmp.text = data.title ?? "";

        if (contentTmp != null)
            contentTmp.text = data.text ?? "";

        if (courseNameTmp != null)
            courseNameTmp.text = data.text ?? "";
    }

    private void RefreshView()
    {
        Debug.Log("[MailContentView] RefreshView called");

        if (NotificationsDetailStaticStore.IsLoading)
            return;

        if (!string.IsNullOrWhiteSpace(NotificationsDetailStaticStore.LastError))
        {
            Debug.LogWarning("[MailContentView] Load detail lỗi: " + NotificationsDetailStaticStore.LastError);
            ResetView();
            return;
        }

        var data = NotificationsDetailStaticStore.CurrentDetail;
        if (data == null)
        {
            ResetView();
            return;
        }

        Bind(data);
        Show();
    }
}