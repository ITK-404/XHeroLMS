//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using RealTimeWeather.WeatherProvider;
using RealTimeWeather.WeatherProvider.Metocean;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RealTimeWeather.Data
{
    public class MetoceanWaterDataRequest
    {
        private const string kAcceptHeader = "accept";
        private const string kAcceptValue = "application/json";
        private const string kApiKeyHeader = "x-api-key";
        private const string kMetoceanURL = "https://forecast-v2.metoceanapi.com/point/time";
        private const string kLocationString = "?lat=";
        private const string kMetoceanSeparator = "&";
        private const string kComma = ",";
        private const string kVariablesString = "&variables=";
        private const string kDateFrom = "&from=";
        private const string kInterval = "&interval=";
        private const string kRepeat = "&repeat=";
        private const string kCycleLock = "&cycleLock=true";
        private const string kJoinModels = "&joinModels=true";
        private const string kHours = "h";
        private const string kPrecipitationRate = "precipitation.rate";
        private const string kAirTemperature = "air.temperature.at-2m";
        private const string kAirPressureAtSea = "air.pressure.at-sea-level";
        private const string kWindDirection = "wind.direction.at-10m";
        private const string kAirHumidity = "air.humidity.at-2m";
        private const string kAirVisibility = "air.visibility";
        private const string kWindSpeed = "wind.speed.at-10m";
        private const string kWaveHeight = "wave.height";
        private const string kWavePeriodPeak = "wave.period.peak";
        private const string kWaveDirection = "wave.direction.peak";
        private const string kCloudCover = "cloud.cover";
        private const string kRadiationLongwave = "radiation.flux.downward.longwave";
        private const string kRadiationShortwave = "radiation.flux.downward.shortwave";
        private const string kSeaSurfaceTemperature = "sea.temperature.at-surface";
        private const string kMetoceanStr = "Metocean service exception: ";
        private const string kApiKeyError = " (make sure you have a valid API Key in the MetoceanModule)";
        private const string kForecastNotFound = " (forecast model not found)";
        private const string kUnexpectedError = " (unexpected server error)";
        private const int kUnauthorized = 401;
        private const int kBadRequest = 404;

        public event Action<ExceptionType, string> RaiseException;

        private MetoceanWaterRequestData _metoceanWaterSimulationData;

        public MetoceanWaterDataRequest(MetoceanWaterRequestData metoceanWaterSimulationData)
        {
            this._metoceanWaterSimulationData = metoceanWaterSimulationData;
        }

        public async Task<MetoceanData> RequestMetoceanData()
        {
            string url = GenerateTheURL();
            var response = await GetRequest(url);

            if (response == string.Empty) return null;

            response = Utilities.ConvertStrongFormatToSimpleCase(response, new System.Text.RegularExpressions.Regex("(-)|(\\.)"));
            MetoceanData metoceanData = JsonUtility.FromJson<MetoceanData>(response);
            metoceanData.Latitude = _metoceanWaterSimulationData.Latitude;
            metoceanData.Longitude = _metoceanWaterSimulationData.Longitude;

            var data = await GetGeocodingInformation(metoceanData);
            MetoceanLog.LogApiErrors(data);

            return data;
        }

        private async Task<MetoceanData> GetGeocodingInformation(MetoceanData metoceanData)
        {
            var reverseGeocoding = new ReverseGeocoding();
            var reverseGeoData = await reverseGeocoding.RequestGeocodingInformationAsync(metoceanData.Latitude, metoceanData.Longitude);

            if (reverseGeoData != null)
            {
                string locality = string.Empty;
                if (!string.IsNullOrEmpty(reverseGeoData.Address.District))
                {
                    locality = reverseGeoData.Address.District;
                }
                if (!string.IsNullOrEmpty(reverseGeoData.Address.County))
                {
                    locality = reverseGeoData.Address.County;
                }
                if (!string.IsNullOrEmpty(reverseGeoData.Address.Municipality))
                {
                    locality = reverseGeoData.Address.Municipality;
                }
                if (!string.IsNullOrEmpty(reverseGeoData.Address.Village))
                {
                    locality = reverseGeoData.Address.Village;
                }
                if (!string.IsNullOrEmpty(reverseGeoData.Address.Town))
                {
                    locality = reverseGeoData.Address.Town;
                }
                if (!string.IsNullOrEmpty(reverseGeoData.Address.City))
                {
                    locality = reverseGeoData.Address.City;
                }

                metoceanData.Location = locality;
                metoceanData.Country = reverseGeoData.Address.Country;
            }

            return metoceanData;
        }

        private string GenerateTheURL()
        {
            string url = kMetoceanURL;
            url += AddLocation();
            url += AddVariables();
            url += AddDateAndTime();
            url += AddInterval();
            url += AddRepeat();
            url += AddCycleLock();
            return url;
        }

        private string AddLocation()
        {
            string locationString = string.Empty;
            string latitude = _metoceanWaterSimulationData.Latitude.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            string longitude = "lon=" + _metoceanWaterSimulationData.Longitude.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            locationString += kLocationString + latitude + kMetoceanSeparator + longitude;
            return locationString;
        }

        private string AddVariables()
        {
            string variables = kVariablesString;
            variables += kPrecipitationRate + kComma;
            variables += kAirTemperature + kComma;
            variables += kAirPressureAtSea + kComma;
            variables += kWindDirection + kComma;
            variables += kAirHumidity + kComma;
            variables += kAirVisibility + kComma;
            variables += kWindSpeed + kComma;
            variables += kWaveHeight + kComma;
            variables += kWavePeriodPeak + kComma;
            variables += kWaveDirection + kComma;
            variables += kCloudCover + kComma;
            variables += kRadiationLongwave + kComma;
            variables += kRadiationShortwave + kComma;
            variables += kSeaSurfaceTemperature + kComma;

            variables = variables.Remove(variables.Length - 1);
            return variables;
        }

        private string AddDateAndTime()
        {
            string dateAndTime = kDateFrom;
            DateTime date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            dateAndTime += date.ToString("yyyy-MM-ddTHH:mm:ssZ");

            return dateAndTime;
        }

        private string AddInterval()
        {
            string interval = kInterval;
            interval += _metoceanWaterSimulationData.IntervalInHours.ToString() + kHours;
            return interval;
        }

        private string AddRepeat()
        {
            if (!_metoceanWaterSimulationData.MoreIntervals)
            {
                _metoceanWaterSimulationData.NumberOfIntervals = 0;
            }
            string repeat = kRepeat;
            repeat += _metoceanWaterSimulationData.NumberOfIntervals.ToString();
            return repeat;
        }

        private string AddCycleLock()
        {
            return kCycleLock + kJoinModels;
        }

        private async Task<string> GetRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                webRequest.SetRequestHeader(kAcceptHeader, kAcceptValue);
                webRequest.SetRequestHeader(kApiKeyHeader, _metoceanWaterSimulationData.ApiKey);
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
                        case kUnauthorized:
                            RaiseException?.Invoke(exception, kMetoceanStr + webRequest.error + kApiKeyError);
                            break;
                        case kBadRequest:
                            RaiseException?.Invoke(exception, kMetoceanStr + webRequest.error + kForecastNotFound);
                            break;
                        default:
                            RaiseException?.Invoke(exception, kMetoceanStr + webRequest.error + kUnexpectedError);
                            break;
                    }
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }

                return string.Empty;
            }
        }

    }
}