#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;

public class AndroidDeepLinkPostProcess : IPostGenerateGradleAndroidProject
{
    // Thứ tự chạy. Số lớn để chạy sau các bước khác.
    public int callbackOrder => 1000;

    // ====== CONFIG ======
    private const string DeepLinkScheme = "lms";
    private const string DeepLinkHost   = "lms.deeplink";

    // Unity activities:
    private const string UnityPlayerActivity     = "com.unity3d.player.UnityPlayerActivity";
    private const string UnityPlayerGameActivity = "com.unity3d.player.UnityPlayerGameActivity";

    // Chọn activity để gắn deeplink:
    // - Nếu project bạn dùng GameActivity (Unity mới), để true.
    // - Nếu dùng Activity cũ, để false.
    private const bool UseGameActivity = true;
    // =====================

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Với Unity, AndroidManifest.xml thường ở:
        // <gradleProject>/src/main/AndroidManifest.xml
        var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            UnityEngine.Debug.LogError($"[AndroidDeepLink] Not found: {manifestPath}");
            return;
        }

        var doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.Load(manifestPath);

        var nsAndroid = "http://schemas.android.com/apk/res/android";
        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("android", nsAndroid);

        var manifest = doc.SelectSingleNode("/manifest");
        var application = doc.SelectSingleNode("/manifest/application");
        if (manifest == null || application == null)
        {
            UnityEngine.Debug.LogError("[AndroidDeepLink] Invalid manifest structure.");
            return;
        }

        // Tìm activity mục tiêu
        string targetActivityName = UseGameActivity ? UnityPlayerGameActivity : UnityPlayerActivity;

        XmlNode targetActivity = FindActivity(application, nsmgr, targetActivityName);
        if (targetActivity == null)
        {
            // Nếu không tìm thấy, thử fallback sang activity còn lại
            string fallback = UseGameActivity ? UnityPlayerActivity : UnityPlayerGameActivity;
            targetActivity = FindActivity(application, nsmgr, fallback);

            if (targetActivity == null)
            {
                UnityEngine.Debug.LogError($"[AndroidDeepLink] Cannot find activity '{targetActivityName}' (or fallback) in manifest.");
                return;
            }
        }

        // Đảm bảo exported=true cho Android 12+ (activity có intent-filter VIEW nên nên set)
        EnsureAttribute(targetActivity, nsAndroid, "exported", "true");

        // Nếu đã có deeplink intent-filter (scheme+host) thì thôi
        if (HasDeepLinkIntentFilter(targetActivity, nsmgr, DeepLinkScheme, DeepLinkHost))
        {
            UnityEngine.Debug.Log("[AndroidDeepLink] Deep link intent-filter already exists. Skip.");
            return;
        }

        // Tạo intent-filter deeplink
        var intentFilter = doc.CreateElement("intent-filter");
        AppendElementWithAttr(doc, intentFilter, "action", nsAndroid, "name", "android.intent.action.VIEW");

        var catDefault = doc.CreateElement("category");
        catDefault.SetAttribute("name", nsAndroid, "android.intent.category.DEFAULT");
        intentFilter.AppendChild(catDefault);

        var catBrowsable = doc.CreateElement("category");
        catBrowsable.SetAttribute("name", nsAndroid, "android.intent.category.BROWSABLE");
        intentFilter.AppendChild(catBrowsable);

        var data = doc.CreateElement("data");
        data.SetAttribute("scheme", nsAndroid, DeepLinkScheme);
        data.SetAttribute("host", nsAndroid, DeepLinkHost);
        intentFilter.AppendChild(data);

        targetActivity.AppendChild(intentFilter);

        doc.Save(manifestPath);
        UnityEngine.Debug.Log($"[AndroidDeepLink] Added deeplink: {DeepLinkScheme}://{DeepLinkHost} to {GetAttr(targetActivity, nsAndroid, "name")}");
    }

    private static XmlNode FindActivity(XmlNode application, XmlNamespaceManager nsmgr, string activityName)
    {
        var nodes = application.SelectNodes("activity", nsmgr);
        if (nodes == null) return null;

        foreach (XmlNode act in nodes)
        {
            var name = GetAttr(act, "http://schemas.android.com/apk/res/android", "name");
            if (name == activityName) return act;
        }
        return null;
    }

    private static bool HasDeepLinkIntentFilter(XmlNode activity, XmlNamespaceManager nsmgr, string scheme, string host)
    {
        var dataNodes = activity.SelectNodes("intent-filter/data", nsmgr);
        if (dataNodes == null) return false;

        foreach (XmlNode data in dataNodes)
        {
            var s = GetAttr(data, "http://schemas.android.com/apk/res/android", "scheme");
            var h = GetAttr(data, "http://schemas.android.com/apk/res/android", "host");
            if (s == scheme && h == host) return true;
        }
        return false;
    }

    private static void EnsureAttribute(XmlNode node, string nsAndroid, string attrName, string value)
    {
        var attr = node.Attributes?[attrName, nsAndroid];
        if (attr == null)
        {
            attr = node.OwnerDocument.CreateAttribute("android", attrName, nsAndroid);
            attr.Value = value;
            node.Attributes.Append(attr);
        }
        else
        {
            attr.Value = value;
        }
    }

    private static void AppendElementWithAttr(XmlDocument doc, XmlElement parent, string elementName, string nsAndroid, string attrName, string attrValue)
    {
        var el = doc.CreateElement(elementName);
        el.SetAttribute(attrName, nsAndroid, attrValue);
        parent.AppendChild(el);
    }

    private static string GetAttr(XmlNode node, string nsAndroid, string attrName)
        => node.Attributes?[attrName, nsAndroid]?.Value ?? "";
}
#endif
