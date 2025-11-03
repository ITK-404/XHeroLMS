public static class AuthFlowSession
{
    // Registration context
    public static string LastRegUsername84 = ""; // normalized 84… (or email)
    public static string LastRegOtpBy = ""; // "phone" | "email"


    // OTP context (shared by Register/Forgot)
    public static string LastOtpIdentifier = ""; // email hoặc 84…
    public static string LastOtpBy = ""; // "email" | "phone"
    public static string LastOtpPurpose = ""; // "forgot-password" | "register"


    // Reset context
    public static string LastResetUsername = ""; // convenience for ResetPasswordController
}