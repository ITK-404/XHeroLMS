using System;
using UnityEngine;

public static class TokenStore
{
    // ================== RUNTIME CACHE ==================
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

    // ================== PERSIST KEYS ==================
    private const string PREF_HAS_SESSION = "AUTH_HAS_SESSION";
    private const string PREF_TOKEN       = "AUTH_TOKEN";
    private const string PREF_USER_JSON   = "AUTH_USER_JSON";
    private const string PREF_UNREAD_JSON = "AUTH_UNREAD_JSON";
    private const string PREF_SAVED_AT    = "AUTH_SAVED_AT";

    [Serializable] private class PersistUser
    {
        public string id, username, fullName, gender, role, email, status, avatar, referralCode, jit;
    }

    [Serializable] private class PersistUnread
    {
        public string all, personal, system;
    }
 
    public static void SetData(AuthResponseRoot auth)
    {
        if (auth == null || auth.data == null)
        {
            Debug.LogWarning("[TokenStore] Không có dữ liệu hợp lệ để lưu.");
            return;
        }

        AccessToken = auth.data.token;

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
        else
        {
            UserID = Username = FullName = Gender = Role = Email = Status = Avatar = ReferralCode = Jit = null;
        }

        if (auth.data.totalUnread != null)
        {
            UnreadAll = auth.data.totalUnread.all;
            UnreadPersonal = auth.data.totalUnread.personal;
            UnreadSystem = auth.data.totalUnread.system;
        }
        else
        {
            UnreadAll = UnreadPersonal = UnreadSystem = null;
        }

        SaveToDisk();
        Debug.Log($"[TokenStore] Saved token + user: {FullName} ({Username})");
    }

    public static void SaveToDisk()
    {
        bool hasToken = !string.IsNullOrEmpty(AccessToken);

        PlayerPrefs.SetInt(PREF_HAS_SESSION, hasToken ? 1 : 0);
        PlayerPrefs.SetString(PREF_TOKEN, AccessToken ?? "");

        if (!string.IsNullOrEmpty(UserID) || !string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(FullName))
        {
            var pu = new PersistUser
            {
                id = UserID,
                username = Username,
                fullName = FullName,
                gender = Gender,
                role = Role,
                email = Email,
                status = Status,
                avatar = Avatar,
                referralCode = ReferralCode,
                jit = Jit
            };
            PlayerPrefs.SetString(PREF_USER_JSON, JsonUtility.ToJson(pu));
        }
        else PlayerPrefs.DeleteKey(PREF_USER_JSON);

        if (!string.IsNullOrEmpty(UnreadAll) || !string.IsNullOrEmpty(UnreadPersonal) || !string.IsNullOrEmpty(UnreadSystem))
        {
            var pr = new PersistUnread { all = UnreadAll, personal = UnreadPersonal, system = UnreadSystem };
            PlayerPrefs.SetString(PREF_UNREAD_JSON, JsonUtility.ToJson(pr));
        }
        else PlayerPrefs.DeleteKey(PREF_UNREAD_JSON);

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(PREF_SAVED_AT, nowUnix.ToString());

        PlayerPrefs.Save();
    }

    public static bool TryRestoreFromDisk()
    {
        int has = PlayerPrefs.GetInt(PREF_HAS_SESSION, 0);
        if (has != 1) return false;

        string token = PlayerPrefs.GetString(PREF_TOKEN, "");
        if (string.IsNullOrEmpty(token))
        {
            ClearDiskOnly();
            return false;
        }

        AccessToken = token;

        string userJson = PlayerPrefs.GetString(PREF_USER_JSON, "");
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var pu = JsonUtility.FromJson<PersistUser>(userJson);
                if (pu != null)
                {
                    UserID = pu.id;
                    Username = pu.username;
                    FullName = pu.fullName;
                    Gender = pu.gender;
                    Role = pu.role;
                    Email = pu.email;
                    Status = pu.status;
                    Avatar = pu.avatar;
                    ReferralCode = pu.referralCode;
                    Jit = pu.jit;
                }
            }
            catch { /* ignore */ }
        }

        string unreadJson = PlayerPrefs.GetString(PREF_UNREAD_JSON, "");
        if (!string.IsNullOrEmpty(unreadJson))
        {
            try
            {
                var pr = JsonUtility.FromJson<PersistUnread>(unreadJson);
                if (pr != null)
                {
                    UnreadAll = pr.all;
                    UnreadPersonal = pr.personal;
                    UnreadSystem = pr.system;
                }
            }
            catch { /* ignore */ }
        }

        Debug.Log("[TokenStore] Restore session OK.");
        return true;
    }

    public static void Clear()
    {
        AccessToken = null;
        UserID = Username = FullName = Gender = Role = Email = Status = Avatar = ReferralCode = Jit = null;
        UnreadAll = UnreadPersonal = UnreadSystem = null;

        ClearDiskOnly();
        Debug.Log("[TokenStore] Cleared runtime + disk.");
    }

    public static void ClearDiskOnly()
    {
        PlayerPrefs.DeleteKey(PREF_HAS_SESSION);
        PlayerPrefs.DeleteKey(PREF_TOKEN);
        PlayerPrefs.DeleteKey(PREF_USER_JSON);
        PlayerPrefs.DeleteKey(PREF_UNREAD_JSON);
        PlayerPrefs.DeleteKey(PREF_SAVED_AT);
        PlayerPrefs.Save();
    }
}
