// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections;
using UnityEngine.Networking;
using RealTimeWeather.Enums;
using System.Threading.Tasks;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    public class StormglassAPIRequest
    {
        #region Public Delegates  
        public delegate void OnSendResponse(string requestedData, RequestDataType dataType);
        public delegate void OnErrorRaised(ExceptionType exception, string exceptionMessage);

        public OnSendResponse onSendResponse;
        public OnErrorRaised onErrorRaised;
        #endregion

        #region Private Constants
        private const string kStromglassExceptionStr = "Stormglass service exception: ";
        #endregion

        #region Public Methods
        public IEnumerator GetRequest(string url, string headerName, string headerValue, RequestDataType dataType)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                if (headerName != null)
                {
                    webRequest.SetRequestHeader(headerName, headerValue);
                }
                yield return webRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                if (webRequest.isNetworkError || webRequest.isHttpError)
#endif
                {
#if UNITY_2020_2_OR_NEWER
                    ExceptionType exception = webRequest.result == UnityWebRequest.Result.ProtocolError ? ExceptionType.HTTPException : ExceptionType.SystemException;
#else
                ExceptionType exception = webRequest.isHttpError ? ExceptionType.HTTPException : ExceptionType.SystemException;
#endif
                    switch (webRequest.responseCode)
                    {
                        case 400:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Bad Request – Your request is invalid.");
                            break;
                        case 401:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Unauthorized – Your API key is invalid.");
                            break;
                        case 429:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Too Many Requests – You’ve reached your daily limit.");
                            break;
                        case 500:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Internal Server Error – We had a problem with our server. Try again later.");
                            break;
                        case 503:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Service Unavailable – We’re temporarily offline for maintenance. Please try again later.");
                            break;
                        default:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error);
                            break;
                    }
                }
                else
                {
                    onSendResponse?.Invoke(webRequest.downloadHandler.text, dataType);
                }
            }
        }

        public async Task<(string, RequestDataType)> GetRequestAsync(string url, string headerName, string headerValue, RequestDataType dataType)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                if (headerName != null)
                {
                    webRequest.SetRequestHeader(headerName, headerValue);
                }

                webRequest.SendWebRequest();
                while (!webRequest.isDone)
                {
                    await Task.Yield();
                }

#if UNITY_2020_2_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                if (webRequest.isNetworkError || webRequest.isHttpError)
#endif
                {
#if UNITY_2020_2_OR_NEWER
                    ExceptionType exception = webRequest.result == UnityWebRequest.Result.ProtocolError ? ExceptionType.HTTPException : ExceptionType.SystemException;
#else
                    ExceptionType exception = webRequest.isHttpError ? ExceptionType.HTTPException : ExceptionType.SystemException;
#endif
                    switch (webRequest.responseCode)
                    {
                        case 400:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Bad Request – Your request is invalid.");
                            break;
                        case 401:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Unauthorized – Your API key is invalid.");
                            break;
                        case 429:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Too Many Requests – You’ve reached your daily limit.");
                            break;
                        case 500:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Internal Server Error – We had a problem with our server. Try again later.");
                            break;
                        case 503:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error + " Service Unavailable – We’re temporarily offline for maintenance. Please try again later.");
                            break;
                        default:
                            onErrorRaised?.Invoke(exception, kStromglassExceptionStr + webRequest.error);
                            break;
                    }
                }
                else
                {
                    return(webRequest.downloadHandler.text, dataType);
                }

                return (string.Empty, RequestDataType.Weather);
            }
        }
        #endregion
    }
}