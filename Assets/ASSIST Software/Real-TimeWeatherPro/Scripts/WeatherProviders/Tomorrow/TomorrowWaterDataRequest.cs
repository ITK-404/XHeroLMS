//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using RealTimeWeather.WeatherProvider.Tomorrow;
using RealTimeWeather.WeatherProvider;
using System;
using System.Globalization;
using UnityEngine;
using System.Threading.Tasks;

namespace RealTimeWeather.Data
{
    public class TomorrowWaterDataRequest
    {
        //Core maritime data
        private const string kWaveSignificantHeightStr = "waveSignificantHeight";
        private const string kWaveDirectionStr = "waveFromDirection";
        private const string kWaveMeanPeriodStr = "waveMeanPeriod";
        private const string kPressureSeaLevelStr = "pressureSeaLevel";
        private const string kSeaSurfaceTemperatureStr = "seaSurfaceTemperature";
        private const string kSeaCurrentDirectionStr = "seaCurrentDirection";
        private const string kSeaCurrentSpeedStr = "seaCurrentSpeed";
        private const string kCloudCover = "cloudCover";

        //Optional maritime data
        private const string kPrimarySwellHeightStr = "primarySwellWaveSignificantHeight";
        private const string kPrimarySwellMeanPeriodStr = "primarySwellWaveMeanPeriod";
        private const string kSecondarySwellHeightStr = "secondarySwellWaveSignificantHeight";
        private const string kSecondarySwellMeanPeriodStr = "secondarySwellWaveMeanPeriod";
        private const string kTertiarySwellHeightStr = "tertiarySwellWaveSignificantHeight";
        private const string kTertiarySwellMeanPeriodStr = "tertiarySwellWaveMeanPeriod";
        private const string kTidesStr = "tides";

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
        private const int kHoursPerDay = 24;

        //Warnings
        private const string kApiWarningMessageStr = "Invalid API key.";
        private const string kLocationWarningMessageStr = "Invalid location(latitude, longitude).";

        public event Action<ExceptionType, string> RaiseException;

        private TomorrowWaterSimulationData _tomorrowWaterSimulationData;
        private WeatherAPIRequest _weatherAPIRequest = new WeatherAPIRequest();

        public TomorrowWaterDataRequest(TomorrowWaterSimulationData tomorrowWaterSimulationData)
        {
            _tomorrowWaterSimulationData = tomorrowWaterSimulationData;
        }

        public async Task<TomorrowCoreData> RequestTomorrowData()
        {
            _weatherAPIRequest.onErrorRaised += SendError;
            var url = GenerateTheUrl();
            var response = await _weatherAPIRequest.GetRequestAsync(url);

            if(response == string.Empty) return null;

            TomorrowCoreData tomorrowData = JsonUtility.FromJson<TomorrowCoreData>(response);
            tomorrowData.Latitude = _tomorrowWaterSimulationData.Latitude;
            tomorrowData.Longitude = _tomorrowWaterSimulationData.Longitude;

            var reverseGeocoding = new ReverseGeocoding();

            GeocodingData reverseGeoData = await reverseGeocoding.RequestGeocodingInformationAsync(tomorrowData.Latitude, tomorrowData.Longitude);

            ValidateGeocodingLocation(reverseGeoData,tomorrowData);

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

        protected string GenerateTheUrl()
        {
            string url = kTomorrowURL;
            url += AddApiKeyStr();
            url += AddLocationStr();
            url += AddFieldsStr();
            url += AddTimestepsStr();

            return url;
        }

        protected virtual string AddTimestepsStr()
        {
            string fields = kTimestepsStr;
            fields += kTimestepCurrentStr + kSeparatorStr;

            return fields;
        }

        private string AddLocationStr()
        {
            string location = string.Empty;

            if (string.IsNullOrEmpty(_tomorrowWaterSimulationData.Latitude.ToString(CultureInfo.InvariantCulture)) || string.IsNullOrEmpty(_tomorrowWaterSimulationData.Longitude.ToString(CultureInfo.InvariantCulture)))
            {
                RaiseException?.Invoke(ExceptionType.InvalidInputData, kLocationWarningMessageStr);
            }
            else
            {
                string latitude = _tomorrowWaterSimulationData.Latitude.ToString("0.00", CultureInfo.InvariantCulture);
                string longitude = _tomorrowWaterSimulationData.Longitude.ToString("0.00", CultureInfo.InvariantCulture);
                location = kLocationUrlStr + latitude + kSeparatorStr + longitude;
            }

            return location;
        }

        private string AddApiKeyStr()
        {
            string apiKeyStr = string.Empty;

            if (_tomorrowWaterSimulationData.ApiKey.Equals(string.Empty))
            {
                RaiseException?.Invoke(ExceptionType.InvalidInputData, kApiWarningMessageStr);
            }
            else
            {
                apiKeyStr = kApiKeyUrlStr + _tomorrowWaterSimulationData.ApiKey;
            }

            return apiKeyStr;
        }

        private string AddFieldsStr()
        {
            string fields = kFieldsUrlStr;
            //Core data
            fields += kTemperatureStr + kSeparatorStr;
            fields += kHumidityStr + kSeparatorStr;
            fields += kWindSpeedStr + kSeparatorStr;
            fields += kWindDirectionStr + kSeparatorStr;
            fields += kPressureSeaLevelStr + kSeparatorStr;
            fields += kVisibilityStr + kSeparatorStr;
            fields += kPrecipitationIntensityStr + kSeparatorStr;
            fields += kWeatherCodeStr + kSeparatorStr;
            fields += kCloudCover + kSeparatorStr;
            fields += kWaveSignificantHeightStr + kSeparatorStr;
            fields += kWaveDirectionStr + kSeparatorStr;
            fields += kWaveMeanPeriodStr + kSeparatorStr;
            fields += kSeaSurfaceTemperatureStr + kSeparatorStr;
            fields += kSeaCurrentDirectionStr + kSeparatorStr;
            fields += kSeaCurrentSpeedStr + kSeparatorStr;

            //Optional data
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kPrimarySwellHeightStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kPrimarySwellMeanPeriodStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kSecondarySwellHeightStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kSecondarySwellMeanPeriodStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kTertiarySwellHeightStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kTertiarySwellMeanPeriodStr + kSeparatorStr : string.Empty;
            fields += _tomorrowWaterSimulationData.IncludeExtraPackage ? kTidesStr + kSeparatorStr : string.Empty;
            return fields;
        }
    }
}