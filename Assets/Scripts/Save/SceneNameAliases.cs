using System;

public static class SceneNameAliases
{
    public const string IntroScene = "IntroScene";
    public const string LoadingScene = "LoadingScene";
    public const string NewSceneAddress = "New Scene";
    public const string NewSceneLateLabel = "new_scene_late";
    public const string GeneratedNewSceneName = "New Scene Addressable";
    public const string NewSceneLatePrefix = "New Scene Late ";

    public static string ToAddressableSceneKey(string sceneName)
    {
        if (IsNewSceneFamily(sceneName))
            return NewSceneAddress;

        return string.IsNullOrWhiteSpace(sceneName) ? sceneName : sceneName.Trim();
    }

    public static string ToSavedSceneName(string sceneName)
    {
        if (IsNewSceneFamily(sceneName))
            return NewSceneAddress;

        return string.IsNullOrWhiteSpace(sceneName) ? sceneName : sceneName.Trim();
    }

    public static bool CanUseSavedSceneForResume(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        if (EqualsNormalized(sceneName, IntroScene))
            return false;

        if (EqualsNormalized(sceneName, LoadingScene))
            return false;

        return true;
    }

    public static bool AreSameScene(string a, string b)
    {
        if (EqualsNormalized(a, b))
            return true;

        return IsNewSceneFamily(a) && IsNewSceneFamily(b);
    }

    public static bool IsNewSceneFamily(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        string trimmed = sceneName.Trim();

        return EqualsNormalized(trimmed, NewSceneAddress)
               || EqualsNormalized(trimmed, GeneratedNewSceneName)
               || trimmed.StartsWith(NewSceneLatePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualsNormalized(string a, string b)
    {
        return string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Trim()
            .ToLowerInvariant();
    }
}
