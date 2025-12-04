using System;
using UnityEngine;

public static class LmsSecurityHeader
{
    private const string AesGcmKeyBase64 = "x7qfE7pG1Yc7YtX1XK9v3QO3Yv0xv5q3O1Iu6yGvV0Y=";
    private const string AesPlatform     = "lms-3d";

    // Đổi true/false để test BE dùng seconds hay milliseconds
    private const bool USE_MILLISECONDS = false;

    [Serializable]
    private class SecurityPayload
    {
        public long   timestamp;
        public string platform;
    }

    public static string BuildXDataHeader()
    {
        long ts = USE_MILLISECONDS
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var payload = new SecurityPayload
        {
            timestamp = ts,
            platform  = AesPlatform
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"[LmsSecurityHeader] payload = {json}");

        // MÃ HÓA ĐÚNG FORMAT NODE
        string xData = AesGcmStartup.EncryptForXData(json, AesGcmKeyBase64);
        Debug.Log($"[LmsSecurityHeader] x-data = {xData}");

        return xData;
    }
}
