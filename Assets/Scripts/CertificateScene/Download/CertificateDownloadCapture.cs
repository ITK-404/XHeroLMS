#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_STANDALONE || UNITY_EDITOR
using SFB;
#endif

public class CertificateDownloadCapture : MonoBehaviour
{
    [Header("Button tải về / share")]
    public Button downloadButton;

    [Header("Share Buttons (optional)")]
    public Button shareFacebookButton;
    public Button shareZaloButton;

    [Header("Các object cần ẩn khi chụp")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    public string fileNamePrefix = "certificate_";

    [Header("Nội dung share")]
    public string shareMessage = "Chúc mừng đã nhận bằng tốt nghiệp!";

    [Header("Trạng thái khung")]
    public Toggle toggleWithFrame;
    public Toggle toggleWithoutFrame;

    [Header("Android target packages")]
    [SerializeField] private string zaloPackage = "com.zing.zalo";
    [SerializeField] private string facebookPackage = "com.facebook.katana";

    [Header("Download/Save settings")]
    [Tooltip("Tên album trong Gallery (Android)")]
    public string androidAlbumName = "XHero Certificates";

    private enum MobileAction { Download, ShareToZalo, ShareToFacebook }

    private void Awake()
    {
        if (downloadButton) downloadButton.onClick.AddListener(() => StartCapture(MobileAction.Download));
        if (shareZaloButton) shareZaloButton.onClick.AddListener(() => StartCapture(MobileAction.ShareToZalo));
        if (shareFacebookButton) shareFacebookButton.onClick.AddListener(() => StartCapture(MobileAction.ShareToFacebook));
    }

    private void OnDestroy()
    {
        if (downloadButton) downloadButton.onClick.RemoveAllListeners();
        if (shareZaloButton) shareZaloButton.onClick.RemoveAllListeners();
        if (shareFacebookButton) shareFacebookButton.onClick.RemoveAllListeners();
    }

    private void StartCapture(MobileAction action)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CertificateDownloadCapture] GameObject chưa active, không thể chụp.");
            return;
        }
        StartCoroutine(CaptureAndHandleCoroutine(action));
    }

    private bool IsWithFrame()
    {
        if (toggleWithoutFrame != null && toggleWithoutFrame.isOn) return false;
        return true;
    }

    private string BuildShareMessage()
    {
        string msg = string.IsNullOrEmpty(shareMessage) ? "Chúc mừng đã nhận bằng tốt nghiệp!" : shareMessage;
        string modeLabel = IsWithFrame() ? "có khung" : "không khung";
        return $"{msg} ({modeLabel})";
    }

    private IEnumerator CaptureAndHandleCoroutine(MobileAction action)
    {
        var states = new List<bool>(objectsToHide.Count);
        foreach (var go in objectsToHide)
        {
            if (go == null) { states.Add(false); continue; }
            states.Add(go.activeSelf);
            go.SetActive(false);
        }

        try
        {
            yield return new WaitForEndOfFrame();

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0)
            {
                Debug.LogWarning("[CertificateDownloadCapture] Screen size invalid.");
                yield break;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            Destroy(tex);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string modeSuffix = IsWithFrame() ? "with-frame" : "no-frame";
            string fileName = $"{fileNamePrefix}{modeSuffix}_{timestamp}.png";

#if UNITY_ANDROID || UNITY_IOS
            // Local file để OpenWith/Share dùng FileProvider
            string dir = Path.Combine(Application.persistentDataPath, "ShareTemp");
            Directory.CreateDirectory(dir);

            string localPath = Path.Combine(dir, fileName);
            File.WriteAllBytes(localPath, pngBytes);

            Debug.Log($"[CertificateDownloadCapture] Saved local: {localPath}");

#if UNITY_ANDROID
            if (action == MobileAction.Download)
            {
                AndroidJavaObject savedUri;
                bool saved = SaveToGallery_Android(localPath, fileName, androidAlbumName, out savedUri);

                // Dù save fail vẫn OpenWith
                AndroidOpenWithImage_FileProvider(localPath);
            }
            else
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                {
                    AndroidJavaObject uri = GetContentUriFromFileProvider(localPath, context);
                    if (uri == null)
                    {
                        Debug.LogError("[Share] uri == null. Check FileProvider config.");
                        yield break;
                    }

                    string text = BuildShareMessage();

                    if (action == MobileAction.ShareToZalo)
                        AndroidShareImageToPackage(uri, "image/png", text, zaloPackage, chooserTitle: "Chia sẻ Zalo");
                    else if (action == MobileAction.ShareToFacebook)
                        AndroidShareImageToPackage(uri, "image/png", text, facebookPackage, chooserTitle: "Chia sẻ Facebook", fallbackChooserIfFail: true);
                }
            }
#else
            // iOS: NativeShare
            ShareSheetNativePreferTarget(localPath, BuildShareMessage(), null);
#endif

#else
            var extensions = new[] { new ExtensionFilter("PNG Image", "png") };
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string savePath = StandaloneFileBrowser.SaveFilePanel("Lưu chứng chỉ", desktopPath, fileName, extensions);
            if (string.IsNullOrEmpty(savePath)) yield break;

            File.WriteAllBytes(savePath, pngBytes);
            Debug.Log($"[CertificateDownloadCapture] Saved: {savePath}");
#endif
        }
        finally
        {
            RestoreObjects(states);
        }
    }

    private void RestoreObjects(List<bool> states)
    {
        for (int i = 0; i < objectsToHide.Count; i++)
        {
            if (i >= states.Count) break;
            if (objectsToHide[i] == null) continue;
            objectsToHide[i].SetActive(states[i]);
        }
    }

    // ===== iOS / fallback share (NativeShare) =====
    private void ShareSheetNativePreferTarget(string filePath, string text, string targetPackageOrNull)
    {
        try
        {
            var ns = new NativeShare()
                .AddFile(filePath, "image/png")
                .SetText(text)
                .SetSubject("Certificate");

#if UNITY_ANDROID
            if (!string.IsNullOrEmpty(targetPackageOrNull))
                TryCallNativeShareSetTarget(ns, targetPackageOrNull);
#endif
            ns.Share();
        }
        catch (Exception e)
        {
            Debug.LogError("[CertificateDownloadCapture] NativeShare.Share failed: " + e);
        }
    }

    private void TryCallNativeShareSetTarget(object nativeShareInstance, string pkg)
    {
        if (nativeShareInstance == null) return;
        try
        {
            MethodInfo mi = nativeShareInstance.GetType().GetMethod("SetTarget", new[] { typeof(string) });
            if (mi != null) mi.Invoke(nativeShareInstance, new object[] { pkg });
        }
        catch { }
    }

#if UNITY_ANDROID
    // ==========================
    //  ANDROID: SAVE TO GALLERY
    // ==========================
    private bool SaveToGallery_Android(string localPath, string displayName, string albumName, out AndroidJavaObject outUri)
    {
        outUri = null;

        Debug.Log($"[SaveToGallery] BEGIN localPath={localPath} displayName={displayName} album={albumName}");

        try
        {
            int sdkInt = GetAndroidSdkInt();
            Debug.Log($"[SaveToGallery] sdkInt={sdkInt}");

            if (sdkInt <= 28)
            {
                if (!HasAndroidPermission("android.permission.WRITE_EXTERNAL_STORAGE"))
                {
                    Debug.Log("[SaveToGallery] Request WRITE_EXTERNAL_STORAGE");
                    RequestAndroidPermission("android.permission.WRITE_EXTERNAL_STORAGE");
                }
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var cr = context.Call<AndroidJavaObject>("getContentResolver"))
            using (var mediaStoreImages = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
            using (var extContentUri = mediaStoreImages.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            using (var values = new AndroidJavaObject("android.content.ContentValues"))
            {
                long nowSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string safeAlbum = string.IsNullOrEmpty(albumName) ? "Certificates" : albumName;

                // put values (CHÚ Ý: Call(...) không generic)
                CV_PutString(values, "_display_name", displayName);
                CV_PutString(values, "mime_type", "image/png");
                CV_PutLong(values, "date_added", nowSec);
                CV_PutLong(values, "date_modified", nowSec);

                if (sdkInt >= 29)
                {
                    CV_PutString(values, "relative_path", $"Pictures/{safeAlbum}");
                    CV_PutInt(values, "is_pending", 1);
                }

                Debug.Log("[SaveToGallery] insert...");
                AndroidJavaObject uri = cr.Call<AndroidJavaObject>("insert", extContentUri, values);
                if (uri == null)
                {
                    Debug.LogError("[SaveToGallery] insert returned null uri");
                    return false;
                }

                Debug.Log("[SaveToGallery] openOutputStream...");
                using (var os = cr.Call<AndroidJavaObject>("openOutputStream", uri))
                {
                    if (os == null)
                    {
                        Debug.LogError("[SaveToGallery] openOutputStream returned null");
                        return false;
                    }

                    byte[] bytes = File.ReadAllBytes(localPath);
                    os.Call("write", bytes);
                    os.Call("flush");
                }

                if (sdkInt >= 29)
                {
                    values.Call("clear");
                    CV_PutInt(values, "is_pending", 0);
                    cr.Call<int>("update", uri, values, null, null);
                }

                TryMediaScannerScanUri(context, uri);

                outUri = uri;
                Debug.Log("[SaveToGallery] OK uri=" + uri.Call<string>("toString"));
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveToGallery] FAILED: " + e);
            return false;
        }
    }

    // Helper: ÉP ĐÚNG overload ContentValues.put (đừng generic)
    private void CV_PutString(AndroidJavaObject values, string key, string val)
    {
        values.Call("put", key, val);
    }

    private void CV_PutLong(AndroidJavaObject values, string key, long val)
    {
        using (var jLong = new AndroidJavaObject("java.lang.Long", val))
            values.Call("put", key, jLong);
    }

    private void CV_PutInt(AndroidJavaObject values, string key, int val)
    {
        using (var jInt = new AndroidJavaObject("java.lang.Integer", val))
            values.Call("put", key, jInt);
    }

    private void TryMediaScannerScanUri(AndroidJavaObject context, AndroidJavaObject uri)
    {
        try
        {
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.MEDIA_SCANNER_SCAN_FILE"))
            {
                intent.Call<AndroidJavaObject>("setData", uri);
                context.Call("sendBroadcast", intent);
            }
            Debug.Log("[SaveToGallery] Sent MEDIA_SCANNER_SCAN_FILE broadcast");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveToGallery] MediaScanner broadcast failed: " + e.Message);
        }
    }

    private int GetAndroidSdkInt()
    {
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                return version.GetStatic<int>("SDK_INT");
        }
        catch { return 0; }
    }

    private bool HasAndroidPermission(string perm)
    {
        try { return Permission.HasUserAuthorizedPermission(perm); }
        catch { return true; }
    }

    private void RequestAndroidPermission(string perm)
    {
        try { Permission.RequestUserPermission(perm); }
        catch { }
    }

    // ==========================
    //  ANDROID: OPENWITH + SHARE
    // ==========================
    private void AndroidOpenWithImage_FileProvider(string filePath)
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                AndroidJavaObject uri = GetContentUriFromFileProvider(filePath, context);
                if (uri == null)
                {
                    Debug.LogError("[OpenWith] uri == null. Check FileProvider config.");
                    return;
                }

                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setDataAndType", uri, "image/png");

                    int FLAG_GRANT_READ_URI_PERMISSION = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                    intent.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);

                    TrySetClipData(intent, uri);

                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Mở chứng chỉ bằng"))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }

                Debug.Log("[OpenWith] ACTION_VIEW launched OK");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[OpenWith] ACTION_VIEW failed: " + e);
        }
    }

    private void AndroidShareImageToPackage(
        AndroidJavaObject contentUri,
        string mimeType,
        string text,
        string targetPackage,
        string chooserTitle,
        bool fallbackChooserIfFail = false)
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            {
                intent.Call<AndroidJavaObject>("setType", mimeType);
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), contentUri);
                if (!string.IsNullOrEmpty(text))
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                int FLAG_GRANT_READ_URI_PERMISSION = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                intent.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);
                TrySetClipData(intent, contentUri);

                if (!string.IsNullOrEmpty(targetPackage))
                    intent.Call<AndroidJavaObject>("setPackage", targetPackage);

                bool canResolve = CanResolveIntent(activity, intent);
                if (!canResolve)
                {
                    Debug.LogWarning($"[Share] Cannot resolve package={targetPackage}.");
                    if (!fallbackChooserIfFail) return;
                    intent.Call<AndroidJavaObject>("setPackage", (string)null);
                }

                if (!string.IsNullOrEmpty(targetPackage) && canResolve)
                {
                    activity.Call("startActivity", intent);
                }
                else
                {
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle ?? "Chia sẻ"))
                        activity.Call("startActivity", chooser);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Share] Failed: " + e);
        }
    }

    private bool CanResolveIntent(AndroidJavaObject activity, AndroidJavaObject intent)
    {
        try
        {
            using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                AndroidJavaObject resolved = intent.Call<AndroidJavaObject>("resolveActivity", pm);
                return resolved != null;
            }
        }
        catch { return false; }
    }

    private AndroidJavaObject GetContentUriFromFileProvider(string filePath, AndroidJavaObject context)
    {
        string pkg = context.Call<string>("getPackageName");
        string authority = pkg + ".fileprovider";

        using (var fileObj = new AndroidJavaObject("java.io.File", filePath))
        using (var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
        {
            return fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", context, authority, fileObj);
        }
    }

    private void TrySetClipData(AndroidJavaObject intent, AndroidJavaObject uri)
    {
        try
        {
            using (var clipDataClass = new AndroidJavaClass("android.content.ClipData"))
            using (var clipData = clipDataClass.CallStatic<AndroidJavaObject>("newRawUri", "certificate", uri))
            {
                intent.Call<AndroidJavaObject>("setClipData", clipData);
            }
        }
        catch { }
    }
#endif
}
