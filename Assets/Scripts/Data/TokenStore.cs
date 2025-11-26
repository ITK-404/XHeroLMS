using UnityEngine;

public static class TokenStore
{
    // ==== TOKEN ====
    public static string AccessToken { get; private set; }
    public static bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    // ==== USER INFO ====
    public static string UserID { get; private set; }
    public static string Username { get; private set; }
    public static string FullName { get; private set; }
    public static string Gender { get; private set; }
    public static string Role { get; private set; }
    public static string Email { get; private set; }
    public static string Status { get; private set; }
    public static string Avatar { get; private set; }
    public static string ReferralCode { get; private set; }
    public static string Jit { get; private set; }

    // ==== UNREAD COUNTS ====
    public static string UnreadAll { get; private set; }
    public static string UnreadPersonal { get; private set; }
    public static string UnreadSystem { get; private set; }

    /// <summary>
    /// Lưu toàn bộ dữ liệu sau khi đăng nhập thành công.
    /// </summary>
    public static void SetData(AuthResponseRoot auth)
    {
        if (auth == null || auth.data == null)
        {
            Debug.LogWarning("[TokenStore] Không có dữ liệu hợp lệ để lưu.");
            return;
        }

        // === Token ===
        AccessToken = auth.data.token;

        // === User Info ===
        if (auth.data.user != null)
        {
            var u = auth.data.user;
            UserID = u.id;
            Username = u.username;
            FullName = u.fullName;
            Gender = u.gender;
            Role = u.role;
            Email = u.email;
            Status = u.status;
            Avatar = u.avatar;
            ReferralCode = u.referralCode;
            Jit = u.jit;
        }

        // === Unread Info ===
        if (auth.data.totalUnread != null)
        {
            UnreadAll = auth.data.totalUnread.all;
            UnreadPersonal = auth.data.totalUnread.personal;
            UnreadSystem = auth.data.totalUnread.system;
        }

        Debug.Log($"[TokenStore] Đã lưu token và thông tin user: {FullName} ({Username})");
    }

    /// <summary>
    /// Xóa toàn bộ dữ liệu khi đăng xuất hoặc khởi tạo lại.
    /// </summary>
    public static void Clear()
    {
        AccessToken = null;
        UserID = Username = FullName = Gender = Role = Email = Status = Avatar = ReferralCode = Jit = null;
        UnreadAll = UnreadPersonal = UnreadSystem = null;

        Debug.Log("[TokenStore] Dữ liệu TokenStore đã được xóa.");
    }
    /// <summary>
    /// cách dùng:
    /*
        if (TokenStore.IsAuthenticated)
        {
            Debug.Log("Token: " + TokenStore.AccessToken);
            Debug.Log("Tên đầy đủ: " + TokenStore.FullName);
            Debug.Log("Email: " + TokenStore.Email);
            Debug.Log("Role: " + TokenStore.Role);
        }
        else
        {
            Debug.LogWarning("Chưa đăng nhập!");
        }
    */
    /// </summary>
}
