using System;
using UnityEngine;   // để dùng Debug.LogWarning / LogError

public static class ServerErrorConverter
{
    [Serializable]
    private class MessageWrapper
    {
        public string message;
    }

    public static string Convert(string raw)
    {
        // TH1: rỗng -> message chung chung
        if (string.IsNullOrEmpty(raw))
            return "Đăng nhập thất bại. Vui lòng thử lại.";

        raw = raw.Trim();
        string originalRaw = raw;   // lưu lại để log dev xem

        // TH2: backend trả JSON {"message":"..."}
        if (raw.StartsWith("{"))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<MessageWrapper>(raw);
                if (wrapper != null && !string.IsNullOrEmpty(wrapper.message))
                    raw = wrapper.message.Trim();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ServerErrorConverter] Parse JSON lỗi: " + e);
                // raw giữ nguyên
            }
        }

        // Lúc này raw là message thô từ server
        var lower = raw.ToLowerInvariant();

        switch (lower)
        {
            case "wrong_username_or_password":
            case "wrong username or password":
                return "Sai tài khoản hoặc mật khẩu.";

            case "user_not_found":
            case "username_not_found":
            case "username_is_not_existed":
                return "Tài khoản không tồn tại.";

            case "account_locked":
                return "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";

            case "missing_username":
                return "Vui lòng nhập tài khoản.";

            case "missing_password":
                return "Vui lòng nhập mật khẩu.";

            // Nếu backend sau này thêm code mới, chỉ cần thêm case ở đây
        }

        // TH3: không map được -> không show raw cho user, chỉ log cho dev
        Debug.LogWarning("[ServerErrorConverter] Unmapped server error message: " + originalRaw);

        return "Đăng nhập thất bại. Vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ.";
    }
}
