//
// Copyright(c) 2023 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace RealTimeWeather.WeatherProvider
{
    /// <summary>
    /// <para>
    /// This class handle the HTTP communication with web server.
    /// </para>
    /// </summary>
    public class WeatherAPIRequest
    {
        #region Public Delegates
        public delegate void OnSendResponse(string requestedData);
        public delegate void OnErrorRaised(ExceptionType exceptionType, string errorMessage);

        public OnSendResponse onSendResponse;
        public OnErrorRaised onErrorRaised;
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a UnityWebRequest for HTTP GET.
        /// </summary>
        public async Task<string> GetRequestAsync(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                webRequest.SendWebRequest();
                while(!webRequest.isDone)
                {
                    await Task.Yield();
                }

#if UNITY_2020_2_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.responseCode != (long)System.Net.HttpStatusCode.OK || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                if (webRequest.isNetworkError || webRequest.responseCode != (long)System.Net.HttpStatusCode.OK || webRequest.isHttpError)
#endif
                {
                    var errorDescription = webRequest.error + " => " + webRequest.downloadHandler.text;
                    onErrorRaised?.Invoke(ExceptionType.HTTPException, errorDescription);
                    return string.Empty;
                }
                else
                {
                    return webRequest.downloadHandler.text;

                }
            }
        }
#endregion
    }
}