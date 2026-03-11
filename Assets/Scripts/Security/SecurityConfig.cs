using System;
using System.Text;
using UnityEngine;

public static class SecurityConfig
{
    private const string XOR_KEY = "client_side_key_for_obfuscation";

    private const string ENCODED_BASE_URL = "CxgdFR1OcFwIFAwsRgEcKUgXGjodDQcFA00CGwQ="; // api-dev
    // private const string ENCODED_BASE_URL = "CxgdFR1OcFwIFAwsRgkULEgXGjodDQcFA00CGwQ="; // api-prod

    private static string _cachedBaseUrl;

    public static string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(_cachedBaseUrl))
            return _cachedBaseUrl;

        _cachedBaseUrl = Decode(ENCODED_BASE_URL);
        return _cachedBaseUrl;
    }

    // === clear cache để lần sau decode lại (dùng khi switch env runtime) ===
    public static void ClearCache()
    {
        _cachedBaseUrl = null;
    }

    // === force decode ngay lập tức ===
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
