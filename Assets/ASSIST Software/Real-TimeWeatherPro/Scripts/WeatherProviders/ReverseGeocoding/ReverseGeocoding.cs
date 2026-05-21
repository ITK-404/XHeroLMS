//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Data;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RealTimeWeather.WeatherProvider
{
    /// <summary>
    /// <para>
    /// This class is responsible for the reverse geocoding functionality and Nominatim API communication.
    /// </para>
    /// <para>
    /// Reverse geocoding is a process that converts latitude and longitude to readable locality properties.
    /// </para>
    /// <para>
    /// Nominatim API information is available at https://nominatim.org/release-docs/latest/
    /// </para>
    /// </summary>
    public class ReverseGeocoding
    {
        #region Private const Variables
        private const string kNominatimURL = "https://nominatim.openstreetmap.org/reverse?";
        private const string kSearchURL = "https://nominatim.openstreetmap.org/search?q=";
        private const string kSeparatorStr = "&";
        private const string kSearchFormatStr = "format=json&limit=0";
        private const string kFormatStr = "format=json";
        private const string kLatitudeStr = "lat=";
        private const string kLongitudeStr = "lon=";
        private const string kLanguageHeaderStr = "Accept-Language";
        private const string kLanguageStr = "en-US";
        #endregion

        #region Public Methods
        /// <summary>
        /// A coroutine that makes a web request to the Nominatim API in order to get the reverse geocoding data.
        /// </summary>
        /// <param name="latitude">It's a float value that represents the geographic coordinate that specifies the north–south position of a point on the Earth's surface.</param>
        /// <param name="longitude">It's a float value that represents the geographic coordinate that specifies the east-west position of a point on the Earth's surface.</param>
        public IEnumerator RequestGeocodingInformation(float latitude, float longitude)
        {
            string url = GenerateTheURL(latitude, longitude);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader(kLanguageHeaderStr, kLanguageStr);
                yield return webRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
#else
                if (webRequest.isNetworkError || webRequest.isHttpError)
#endif
                {
                    LogFile.Write(webRequest.error);
                    yield return null;
                }
                else
                {
                    GeocodingData geoData = JsonUtility.FromJson<GeocodingData>(webRequest.downloadHandler.text);
                    yield return geoData;
                }
            }
        }

        public async Task<GeocodingData> RequestGeocodingInformationAsync(float latitude, float longitude)
        {
            string url = GenerateTheURL(latitude, longitude);

            WeatherAPIRequest weatherAPI = new WeatherAPIRequest();

            var response = await weatherAPI.GetRequestAsync(url);
            GeocodingData geoData = JsonUtility.FromJson<GeocodingData>(response);

            return geoData;
        }

        public async Task<CoordinatesResponseData> RequestCoodinatesInformation(string city, string country)
        {
            string url = GenerateURLforCordsRequest(city, country);

            WeatherAPIRequest weatherAPI = new WeatherAPIRequest();

            var response = await weatherAPI.GetRequestAsync(url);

            response = EditCoordsData(response);
            CoordinatesResponseData geoData = JsonUtility.FromJson<CoordinatesResponseData>(response);

            return geoData;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Generates the URL for the Nominatim API call.
        /// </summary>
        /// <param name="latitude">It's a float value that represets the geographic coordinate that specifies the north–south position of a point on the Earth's surface.</param>
        /// <param name="longitude">It's a float value that represents the geographic coordinate that specifies the east-west position of a point on the Earth's surface.</param>
        /// <returns>
        /// A string value that represents the URL of the resource to retrieve via HTTP GET.</param>
        /// </returns>
        private string GenerateTheURL(float latitude, float longitude)
        {
            string url = kNominatimURL;
            url += kFormatStr + kSeparatorStr;
            url += kLatitudeStr + latitude + kSeparatorStr + kLongitudeStr + longitude;
            return url;
        }

        private string GenerateURLforCordsRequest(string city, string country)
        {
            return kSearchURL + city + "+" + country + kSeparatorStr + kSearchFormatStr;
        }

        private string EditCoordsData(string data)
        {
            data = data.Remove(0, 1);
            data = data.Remove(data.Length - 1, 1);

            return data;
        }
        #endregion
    }
}