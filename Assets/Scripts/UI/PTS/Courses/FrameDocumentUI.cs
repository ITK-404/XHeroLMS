using System;
using UnityEngine;

public class FrameDocumentUI : PanelBaseUI
{
    [Header("References")]
    [SerializeField] private UniWebView uniWebViewPrefab;
    [SerializeField] private Transform emptyUI;

    [Header("PDF Viewer")]
    [SerializeField] private bool useGoogleViewerForPdf = true;

    private UniWebView page;
    private string currentLoadedUrl;

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += HandleCourseDetailChanged;
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= HandleCourseDetailChanged;
    }

    public override void Show()
    {
        base.Show();

        string docUrl = GetFirstDocAttachUrl();
        ShowDocument(docUrl);
    }

    public override void Hide()
    {
        base.Hide();

        if (page != null)
            page.Hide();
    }

    private void HandleCourseDetailChanged()
    {
        if (!gameObject.activeInHierarchy)
            return;

        string docUrl = GetFirstDocAttachUrl();
        ShowDocument(docUrl);
    }

public void ShowDocument(string pageUrl)
{
    base.Show();

    bool hasUrl = !string.IsNullOrWhiteSpace(pageUrl);

    if (emptyUI != null)
        emptyUI.gameObject.SetActive(!hasUrl);

    if (!hasUrl)
    {
        if (page != null)
            page.Hide();

        currentLoadedUrl = null;
        return;
    }

    string finalUrl = ConvertToViewableUrl(pageUrl);

    if (page == null)
    {
        page = Instantiate(uniWebViewPrefab, transform);
        page.gameObject.SetActive(true);

        page.OnPageErrorReceived += (view, errorCode, errorMessage) =>
        {
            Debug.LogWarning($"[FrameDocumentUI] WebView load error: {errorCode} - {errorMessage}");
        };

        page.OnLoadingErrorReceived += (view, errorCode, errorMessage, payload) =>
        {
            Debug.LogWarning($"[FrameDocumentUI] WebView loading error: {errorCode} - {errorMessage}");
        };
    }

    if (currentLoadedUrl != finalUrl)
    {
        currentLoadedUrl = finalUrl;
        Debug.Log($"[FrameDocumentUI] Load document: {finalUrl}");
        page.Load(finalUrl);
    }

    page.Show();
}

    private string ConvertToViewableUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        // Android WebView thường không tự render PDF như Chrome,
        // nên bọc PDF/IPFS file qua Google Docs Viewer để xem trong WebView.
        if (useGoogleViewerForPdf && ShouldUseGoogleViewer(rawUrl))
        {
            string encodedUrl = Uri.EscapeDataString(rawUrl);
            return $"https://docs.google.com/gview?embedded=1&url={encodedUrl}";
        }

        return rawUrl;
    }

    private bool ShouldUseGoogleViewer(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        string lower = url.ToLowerInvariant();

        // Filebase/IPFS trả PDF nhưng URL không có .pdf,
        // nên check domain ipfs.filebase.io luôn.
        if (lower.Contains("ipfs.filebase.io/ipfs/"))
            return true;

        if (lower.EndsWith(".pdf"))
            return true;

        if (lower.Contains(".pdf?"))
            return true;

        return false;
    }

    private string GetFirstDocAttachUrl()
    {
        var detail = CourseDetailStaticStore.CurrentDetail;
        if (detail == null || detail.chapters == null || detail.chapters.Count == 0)
            return null;

        for (int c = 0; c < detail.chapters.Count; c++)
        {
            var chapter = detail.chapters[c];
            if (chapter == null || chapter.lessons == null || chapter.lessons.Count == 0)
                continue;

            for (int l = 0; l < chapter.lessons.Count; l++)
            {
                var lesson = chapter.lessons[l];
                if (lesson == null || lesson.docAttach == null || lesson.docAttach.Count == 0)
                    continue;

                var firstDoc = lesson.docAttach[0];
                if (firstDoc == null || string.IsNullOrWhiteSpace(firstDoc.uri))
                    continue;

                return firstDoc.uri;
            }
        }

        return null;
    }
}