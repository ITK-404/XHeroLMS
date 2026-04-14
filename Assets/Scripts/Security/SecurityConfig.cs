using System;
using System.Text;
using UnityEngine;

public static class SecurityConfig
{
    private const string XOR_KEY = "client_side_key_for_obfuscation";

    // DEV
    private const string ENCODED_BASE_URL_DEV =
        "CxgdFR1OcFwIFAwsRgEcKUgXGjodDQcFA00CGwQ=";

    // PROD
    private const string ENCODED_BASE_URL_PROD =
        "CxgdFR1OcFwIFAwsRgkULEgXGjodDQcFA00CGwQ=";

    private static string _cachedBaseUrl;

    public static string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(_cachedBaseUrl))
            return _cachedBaseUrl;

        string encoded = AppBuildEnvRuntime.IsApiProd
            ? ENCODED_BASE_URL_PROD
            : ENCODED_BASE_URL_DEV;

        _cachedBaseUrl = Decode(encoded);
        return _cachedBaseUrl;
    }

    public static string UrlWeb =>
        AppBuildEnvRuntime.IsApiProd
            ? "https://daotao.phongthuydainam.vn"
            : "https://lms.xheroapp.com";

    public static void ClearCache()
    {
        _cachedBaseUrl = null;
    }

    public static string ForceRefreshAndGet()
    {
        ClearCache();
        return GetBaseUrl();
    }

#if UNITY_EDITOR
    public static string EncodeForCode(string plain)
    {
        byte[] data = Encoding.UTF8.GetBytes(plain);
        byte[] key  = Encoding.UTF8.GetBytes(XOR_KEY);

        for (int i = 0; i < data.Length; i++)
            data[i] ^= key[i % key.Length];

        string base64 = Convert.ToBase64String(data);
        Debug.Log("[Encoded] " + base64);
        return base64;
    }
#endif

    private static string Decode(string encoded)
    {
        try
        {
            byte[] data = Convert.FromBase64String(encoded);
            byte[] key  = Encoding.UTF8.GetBytes(XOR_KEY);

            for (int i = 0; i < data.Length; i++)
                data[i] ^= key[i % key.Length];

            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }
}