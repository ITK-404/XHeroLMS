using UnityEngine;

public enum AppEnvironment
{
    Dev = 0,
    Prod = 1
}

public enum ApiEnvironment
{
    Dev = 0,
    Prod = 1
}

[CreateAssetMenu(fileName = "AppBuildEnv", menuName = "Build/App Build Env")]
public class AppBuildEnv : ScriptableObject
{
    [Header("Current Build Environment")]
    public AppEnvironment environment = AppEnvironment.Dev;

    [Header("API Environment")]
    public ApiEnvironment apiEnvironment = ApiEnvironment.Dev;

    [Header("Addressables / GCS")]
    public string gcsBucket;
    public string addressablesRootFolder;
    public string releasesFolder;
    public string platformName;
    public string remoteCatalogJsonUrl;
    public string remoteCatalogHashUrl;

    public bool IsDev => environment == AppEnvironment.Dev;
    public bool IsProd => environment == AppEnvironment.Prod;

    public bool IsApiDev => apiEnvironment == ApiEnvironment.Dev;
    public bool IsApiProd => apiEnvironment == ApiEnvironment.Prod;
}

public static class AppBuildEnvRuntime
{
    private static AppBuildEnv _config;

    public static AppBuildEnv Config
    {
        get
        {
            if (_config == null)
                _config = Resources.Load<AppBuildEnv>("AppBuildEnv");

            return _config;
        }
    }

    public static bool HasConfig => Config != null;

    public static AppEnvironment Environment =>
        Config != null ? Config.environment : AppEnvironment.Dev;

    public static ApiEnvironment ApiEnvironment =>
        Config != null ? Config.apiEnvironment : ApiEnvironment.Dev;

    public static bool IsDev => Environment == AppEnvironment.Dev;
    public static bool IsProd => Environment == AppEnvironment.Prod;

    public static bool IsApiDev => ApiEnvironment == ApiEnvironment.Dev;
    public static bool IsApiProd => ApiEnvironment == ApiEnvironment.Prod;

    public static string EnvironmentName => IsProd ? "PROD" : "DEV";
    public static string ApiEnvironmentName => IsApiProd ? "PROD" : "DEV";

    public static string RemoteCatalogJsonUrl =>
        Config != null ? Config.remoteCatalogJsonUrl : string.Empty;

    public static string RemoteCatalogHashUrl =>
        Config != null ? Config.remoteCatalogHashUrl : string.Empty;

    public static string ReleasesFolder =>
        Config != null ? Config.releasesFolder : string.Empty;

    public static string PlatformName =>
        Config != null ? Config.platformName : string.Empty;

    public static string GcsBucket =>
        Config != null ? Config.gcsBucket : string.Empty;

    public static string AddressablesRootFolder =>
        Config != null ? Config.addressablesRootFolder : string.Empty;
}