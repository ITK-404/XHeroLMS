using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MailListLoaderUI : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "";
    [SerializeField] private int skip = 0;
    [SerializeField] private int limit = 10;
    [SerializeField] private int timeoutSeconds = 20;

    [Header("Buttons")]
    [SerializeField] private Button btnSystem;
    [SerializeField] private Button btnPersonal;

    [Header("UI Spawn")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private MailElementVisualUI mailPrefab;
    [SerializeField] private bool clearOldItems = true;

    [Header("Configs")]
    [SerializeField] private MailTextConfig unreadConfig;
    [SerializeField] private MailTextConfig readConfig;

    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private bool autoLoadOnEnable = true;
    [SerializeField] private bool debugLog = true;

    private readonly List<MailElementVisualUI> spawnedItems = new();
    private Coroutine loadRoutine;
    private string currentTab = "system"; // mặc định hệ thống

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (LmsStore.Instance != null && !string.IsNullOrWhiteSpace(LmsStore.Instance.baseUrl))
                baseUrl = LmsStore.Instance.baseUrl;
        }

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
        if (btnSystem != null)
            btnSystem.onClick.AddListener(OnClickSystem);

        if (btnPersonal != null)
            btnPersonal.onClick.AddListener(OnClickPersonal);
    }

    private void OnDestroy()
    {
        if (btnSystem != null)
            btnSystem.onClick.RemoveListener(OnClickSystem);

        if (btnPersonal != null)
            btnPersonal.onClick.RemoveListener(OnClickPersonal);
    }

    private void OnEnable()
    {
        if (autoLoadOnEnable)
        {
            currentTab = "system"; // mặc định
            LoadCurrentTab();
        }
    }

    public void OnClickSystem()
    {
        currentTab = "system";
        LoadCurrentTab();
    }

    public void OnClickPersonal()
    {
        currentTab = "personal";
        LoadCurrentTab();
    }

    public void LoadCurrentTab()
    {
        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        loadRoutine = StartCoroutine(CoLoadMails(currentTab));
    }

    private IEnumerator CoLoadMails(string tab)
    {
        if (clearOldItems)
            ClearItems();

        SetEmpty(false, "Đang tải...");

        if (string.IsNullOrEmpty(TokenStore.AccessToken))
            TokenStore.TryRestoreFromDisk();

        string token = TokenStore.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[MailListLoaderUI] Không tìm thấy token.");
            SetEmpty(true, "Không có token đăng nhập.");
            yield break;
        }

        // string url = $"{baseUrl}/notifications{tab}?tab={UnityWebRequest.EscapeURL(tab)}&skip={skip}&limit={limit}";
        string url = $"{baseUrl}/notifications?tab={UnityWebRequest.EscapeURL(tab)}&skip={skip}&limit={limit}";
        if (debugLog) Debug.Log("[MailListLoaderUI] Request: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = timeoutSeconds;
            req.SetRequestHeader("authorization", "Bearer " + token);

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool failed = req.result != UnityWebRequest.Result.Success;
#else
            bool failed = req.isNetworkError || req.isHttpError;
#endif

            if (failed)
            {
                Debug.LogError("[MailListLoaderUI] API lỗi: " + req.error + "\n" + req.downloadHandler.text);
                SetEmpty(true, "Không tải được danh sách thông báo.");
                yield break;
            }

            string json = req.downloadHandler.text;
            if (debugLog) Debug.Log("[MailListLoaderUI] Response: " + json);

            MailListResponse root = null;
            try
            {
                root = JsonUtility.FromJson<MailListResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[MailListLoaderUI] Parse JSON lỗi: " + e.Message);
                SetEmpty(true, "Dữ liệu trả về không hợp lệ.");
                yield break;
            }

            if (root == null || !root.status || root.data == null || root.data.data == null || root.data.data.Count == 0)
            {
                SetEmpty(true, "Không có thông báo.");
                yield break;
            }

            SetEmpty(false, "");

            foreach (var item in root.data.data)
            {
                SpawnItem(item);
            }
        }

        loadRoutine = null;
    }

    private void SpawnItem(MailItemData data)
    {
        if (mailPrefab == null || contentParent == null || data == null)
            return;

        MailElementVisualUI ui = Instantiate(mailPrefab, contentParent);

        MailTextConfig config = data.isRead ? readConfig : unreadConfig;
        if (config != null)
            ui.SetConfig(config);

        string timeText = BuildTimeText(data.time);
        string readState = data.isRead ? "Đã đọc" : "Chưa đọc";

        ui.BindData(
            title: data.title,
            description: data.text,
            timeText: timeText,
            readStateText: readState
        );

        spawnedItems.Add(ui);
    }

    private string BuildTimeText(MailTimeData t)
    {
        if (t == null) return "";

        // API đang trả time.time = "10/03/2026"
        if (!string.IsNullOrEmpty(t.time))
            return t.time;

        if (!string.IsNullOrEmpty(t.day))
            return t.day;

        return "";
    }

    private void ClearItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }
        spawnedItems.Clear();

        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void SetEmpty(bool show, string msg)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(show);
            emptyText.text = msg;
        }
    }

    [Serializable]
    public class MailListResponse
    {
        public bool status;
        public MailListDataWrap data;
    }

    [Serializable]
    public class MailListDataWrap
    {
        public MailUnreadData totalUnread;
        public int total;
        public List<MailItemData> data;
    }

    [Serializable]
    public class MailUnreadData
    {
        public string all;
        public string personal;
        public string system;
        public string merchant;
    }

    [Serializable]
    public class MailItemData
    {
        public string _id;
        public string to;
        public string label;
        public string title;
        public string text;
        public string link;
        public string iconKey;
        public bool isRead;
        public MailTimeData time;
        public string icon;
    }

    [Serializable]
    public class MailTimeData
    {
        public string day;
        public string time;
        public string key;
    }
}