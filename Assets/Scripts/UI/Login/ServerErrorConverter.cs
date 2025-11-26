using System;
using UnityEngine;

public static class ServerErrorConverter
{
    [Serializable]
    private class MessageWrapper
    {
        public string message;
    }

    public static string Convert(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Đăng nhập thất bại. Vui lòng thử lại.";

        raw = raw.Trim();
        
        if (raw.StartsWith("{"))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<MessageWrapper>(raw);
                if (wrapper != null && !string.IsNullOrEmpty(wrapper.message))
                    raw = wrapper.message.Trim();
            }
            catch
            {
                // ignore, dùng raw
            }
        }

        // Lúc này raw là message “thô” -> xử lý code
        var lower = raw.ToLower();

        switch (lower)
        {
            case "wrong_username_or_password":
            case "wrong username or password":
                return "Sai tài khoản hoặc mật khẩu.";

            case "user_not_found":
                return "Tài khoản không tồn tại.";

            case "account_locked":
                return "Tài khoản đã bị khóa.";

            case "missing_username":
                return "Vui lòng nhập tài khoản.";

            case "missing_password":
                return "Vui lòng nhập mật khẩu.";

            default:
                return raw;
        }
    }
}
