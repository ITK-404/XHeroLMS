//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using RealTimeWeather.WeatherProvider;
using RealTimeWeather.WeatherProvider.Tomorrow;
using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace RealTimeWeather.Data
{
    public class TomorrowWeatherDataRequest
    {
        private const string kTimestepHourlyStr = "1h";
        private const string kPressureSurfaceLevelStr = "pressureSurfaceLevel";
        private const string kWindGustStr = "windGust";
        private const string kPressureSeaLevelStr = "pressureSeaLevel";
        private const string kPrecipitationProbabilityStr = "precipitationProbability";
        private const string kCloudCoverStr = "cloudCover";
        private const string kTemperatureApparentStr = "temperatureApparent";
        private const string kParticulateMatter25Str = "particulateMatter25";
        private const string kPollutantO3Str = "pollutantO3";
        private const string kPollutantNO2Str = "pollutantNO2";
        private const string kPollutantCOStr = "pollutantCO";
        private const string kPollutantSO2Str = "pollutantSO2";
        private const string kTreeIndexStr = "treeIndex";
        private const string kGrassIndexStr = "grassIndex";
        private const string kWeedIndexStr = "weedIndex";
        private const string kTomorrowURL = "https://api.tomorrow.io/v4/timelines";
        private const string kApiKeyUrlStr = "?apikey=";
        private const string kLocationUrlStr = "&location=";
        private const string kFieldsUrlStr = "&fields=";
        private const string kSeparatorStr = ",";
        private const string kTemperatureStr = "temperature";
        private const string kDewPointStr = "dewPoint";
        private const string kHumidityStr = "humidity";
        private const string kWindSpeedStr = "windSpeed";
        private const string kWindDirectionStr = "windDirection";
        private const string kVisibilityStr = "visibility";
        private const string kPrecipitationIntensityStr = "precipitationIntensity";
        private const string kWeatherCodeStr = "weatherCode";
        private const string kTimestepsStr = "&timesteps=";
        private const string kTimestepCurrentStr = "current";
        private const string kEndTimeStr = "&endTime=";

        //Warnings
        private const string kApiWarningMessageStr = "Invalid API key.";
        private const string kLocationWarningMessageStr = "Invalid location(latitude, longitude).";

        public event Action<ExceptionType, string> RaiseException;

        private TomorrowSimulationData _tomorrowSimulationData;
        private WeatherAPIRequest _weatherAPIRequest = new WeatherAPIRequest();
        private ReverseGeocoding _reverseGeocoding = new ReverseGeocoding();

        public TomorrowWeatherDataRequest(TomorrowSimulationData tomorrowSimulationData)
        {
            _tomorrowSimulationData = tomorrowSimulationData;
        }

        private async Task<string> GenerateTheUrl()
        {
            string url = kTomorrowURL;
            url += AddApiKeyStr();
            url += await AddLocationStr();
            url += AddFieldsStr();
            url += AddTimestepsStr();
            url += AddEndTimeStr();

            return url;
        }

        private string AddApiKeyStr()
        {
            string apiKeyStr = string.Empty;

            if (_tomorrowSimulationData.ApiKey.Equals(string.Empty))
            {
                RaiseException?.Invoke(ExceptionType.InvalidInputData, kApiWarningMessageStr);
            }
            else
            {
                apiKeyStr = kApiKeyUrlStr + _tomorrowSimulationData.ApiKey;
            }

            return apiKeyStr;
        }

        private async Task<string> AddLocationStr()
        {
            string location = string.Empty;

            if (string.IsNullOrEmpty(_tomorrowSimulationData.Latitude.ToString(CultureInfo.InvariantCulture)) || string.IsNullOrEmpty(_tomorrowSimulationData.Longitude.ToString(CultureInfo.InvariantCulture)))
            {
                RaiseException?.Invoke(ExceptionType.InvalidInputData, kLocationWarningMessageStr);
            }
            else
            {
                var lat = _tomorrowSimulationData.Latitude;
                var lng = _tomorrowSimulationData.Longitude;
                if (_tomorrowSimulationData.City != string.Empty || _tomorrowSimulationData.Country != string.Empty)
                {
                    var response = await _reverseGeocoding.RequestCoodinatesInformation(_tomorrowSimulationData.City, _tomorrowSimulationData.Country);



                    if (response != null && response.lon != string.Empty && response.lat != string.Empty)
                    {
                        try
                        {
                            lat = float.Parse(response.lat);
                            lng = float.Parse(response.lon);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(ex);
                        }
                    }
                }

                _tomorrowSimulationData.Latitude = lat;
                _tomorrowSimulationData.Longitude = lng;
                string latitude = lat.ToString("0.00000", CultureInfo.InvariantCulture);
                string longitude = lng.ToString("0.00000", CultureInfo.InvariantCulture);
                location = kLocationUrlStr + latitude + kSeparatorStr + longitude;
            }

            return location;
        }

        private string AddFieldsStr()
        {
            string fields = kFieldsUrlStr;
            // Core data
            fields += kTemperatureStr + kSeparatorStr;
            fields += kDewPointStr + kSeparatorStr;
            fields += kHumidityStr + kSeparatorStr;
            fields += kWindSpeedStr + kSeparatorStr;
            fields += kWindDirectionStr + kSeparatorStr;
            fields += kPressureSurfaceLevelStr + kSeparatorStr;
            fields += kVisibilityStr + kSeparatorStr;
            fields += kPrecipitationIntensityStr + kSeparatorStr;
            fields += kWeatherCodeStr + kSeparatorStr;

            //Extra data
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPressureSeaLevelStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPrecipitationProbabilityStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kTemperatureApparentStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kCloudCoverStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kParticulateMatter25Str + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPollutantCOStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPollutantNO2Str + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPollutantO3Str + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kPollutantSO2Str + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kGrassIndexStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kTreeIndexStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kWeedIndexStr + kSeparatorStr : string.Empty;
            fields += _tomorrowSimulationData.IncludeExtraPackage ? kWindGustStr : string.Empty;
            return fields;
        }

        private string AddTimestepsStr()
        {
            string timesteps = kTimestepsStr;
            timesteps += kTimestepCurrentStr + kSeparatorStr;
            if (_tomorrowSimulationData.IsForecastModeEnabled)
            {
                timesteps += kTimestepHourlyStr + kSeparatorStr;
            }

            return timesteps;
        }

        private string AddEndTimeStr()
        {
            string fields = string.Empty;
            fields += (_tomorrowSimulationData.IsForecastModeEnabled) ? kEndTimeStr : string.Empty;

            fields += (_tomorrowSimulationData.IsForecastModeEnabled) ? DateTime.UtcNow.AddHours(108).AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssK") : string.Empty;
            return fields;
        }

        public async Task<TomorrowCoreData> GetData()
        {
            _weatherAPIRequest.onErrorRaised += SendError;
            var url = await GenerateTheUrl();
            var response = await _weatherAPIRequest.GetRequestAsync(url);

            if (response == string.Empty) return null;

            var tomorrowData = JsonUtility.FromJson<TomorrowCoreData>(response);
            tomorrowData.Latitude = _tomorrowSimulationData.Latitude;
            tomorrowData.Longitude = _tomorrowSimulationData.Longitude;

            var reverseGeocoding = new ReverseGeocoding();

            if (_tomorrowSimulationData.City == string.Empty && _tomorrowSimulationData.Country == string.Empty)
            {
                var reverseGeoData = await reverseGeocoding.RequestGeocodingInformationAsync(tomorrowData.Latitude, tomorrowData.Longitude);
                ValidateGeocodingLocation(reverseGeoData, tomorrowData);
            }
            else
            {
                tomorrowData.City = _tomorrowSimulationData.City;
                tomorrowData.Country= _tomorrowSimulationData.Country;
            }

            _weatherAPIRequest.onErrorRaised -= SendError;
            return tomorrowData;
        }

        private void ValidateGeocodingLocation(GeocodingData reverseGeoData, TomorrowCoreData tomorrowData)
        {
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

                tomorrowData.City = locality;
                tomorrowData.Country = reverseGeoData.Address.Country;
            }
        }

        private void SendError(ExceptionType exceptionType, string errorMessage)
        {
            RaiseException?.Invoke(exceptionType, errorMessage);
        }
    }
}