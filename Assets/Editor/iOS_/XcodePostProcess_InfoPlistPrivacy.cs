#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class XcodePostProcess_InfoPlistPrivacy
{
    private static readonly string[] QueriedSchemes =
    {
        "xhero"
        // thêm ở đây nếu cần
    };
    // Bạn sửa nội dung string này theo app của bạn
    private const string PhotoLibraryUsage =
        "Cho phép ứng dụng truy xuất và hiển thị các hình ảnh chứng chỉ đã tải, giúp trải nghiệm xem chứng chỉ trong game mượt mà và nhanh chóng hơn.";

    private const string PhotoLibraryAddUsage =
        "Cho phép lưu chứng chỉ bạn đã sở hữu vào thư viện ảnh để bạn có thể xem lại hoặc chia sẻ bất cứ lúc nào.";
    private const string NotificationUsage =
        "Cho phép ứng dụng gửi thông báo để cập nhật thông tin game và sự kiện mới nhất đến bạn.";
    
    [PostProcessBuild(900)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        UpdatePhotoPrivacy(target, pathToBuiltProject);
        UpdateSchema(target, pathToBuiltProject);
    }

    private static void UpdatePhotoPrivacy(BuildTarget target, string pathToBuiltProject)
    {
        // lấy file tên là Info.plist
        var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogError($"[InfoPlistPrivacy] Info.plist not found: {plistPath}");
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        var root = plist.root;

        // Photo Library
        root.SetString("NSPhotoLibraryUsageDescription", PhotoLibraryUsage);

        // Photo Library (Add Only)
        root.SetString("NSPhotoLibraryAddUsageDescription", PhotoLibraryAddUsage);
        
        root.SetString("NSUserNotificationUsageDescription", NotificationUsage);

        File.WriteAllText(plistPath, plist.WriteToString());
        UnityEngine.Debug.Log("[InfoPlistPrivacy] Wrote Photo Library usage descriptions to Info.plist");
    }

    private static void UpdateSchema(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogError($"[URLSchemes] Info.plist not found: {plistPath}");
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        var root = plist.root;

        // Lấy hoặc tạo mảng LSApplicationQueriesSchemes
        PlistElementArray schemesArray;
        if (root.values.ContainsKey("LSApplicationQueriesSchemes"))
        {
            schemesArray = root["LSApplicationQueriesSchemes"].AsArray();
        }
        else
        {
            schemesArray = root.CreateArray("LSApplicationQueriesSchemes");
        }

        // Để tránh add trùng, collect các scheme đã tồn tại
        var existing = new HashSet<string>();
        foreach (var el in schemesArray.values)
        {
            existing.Add(el.AsString());
        }

        // Add các scheme còn thiếu
        foreach (var scheme in QueriedSchemes)
        {
            if (!existing.Contains(scheme))
            {
                schemesArray.AddString(scheme);
            }
        }

        File.WriteAllText(plistPath, plist.WriteToString());
        UnityEngine.Debug.Log("[URLSchemes] Added LSApplicationQueriesSchemes to Info.plist");
    }
}
#endif