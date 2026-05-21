//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using RealTimeWeather.WeatherProvider.Stormglass;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace RealTimeWeather.Data
{
    public class StormglassWaterRequest
    {
        private const string kStormglassURL = "https://api.stormglass.io/v2";
        private const string kStormglassWeatherEndPoint = "/weather/point";
        private const string kStormglassBioEndPoint = "/bio/point";
        private const string kStormglassTideEndPoint = "/tide/extremes/point";
        private const string kLatStr = "?lat=";
        private const string kLonStr = "&lng=";
        private const string kParamsStr = "&params=";
        private const string kSeparatorStr = ",";
        private const string kAuthorizationHeaderNameStr = "Authorization";

        // Maritime constants
        private const string kSeaLevelParamStr = "seaLevel";
        private const string kSwellDirectionParamStr = "swellDirection";
        private const string kSecondarySwellDirectionParamStr = "secondarySwellDirection";
        private const string kSwellHeightParamStr = "swellHeight";
        private const string kSecondarySwellHeightParamStr = "secondarySwellHeight";
        private const string kSwellPeriodParamStr = "swellPeriod";
        private const string kSecondarySwellPeriodParamStr = "secondarySwellPeriod";
        private const string kWaterTemperatureParamStr = "waterTemperature";
        private const string kWaveDirectionParamStr = "waveDirection";
        private const string kWaveHeightParamStr = "waveHeight";
        private const string kWavePeriodParamStr = "wavePeriod";
        private const string kWindWaveDirectionParamStr = "windWaveDirection";
        private const string kWindWaveHeightParamStr = "windWaveHeight";
        private const string kWindWavePeriodParamStr = "windWavePeriod";
        private const string kWindDirectionParamStr = "windDirection";
        private const string kWindSpeedParamStr = "windSpeed";

        // Weather constants
        private const string kAirTemperatureStr = "airTemperature";
        private const string kPressureStr = "pressure";
        private const string kCloudCoverStr = "cloudCover";
        private const string kCurrentDirectionStr = "currentDirection";
        private const string kCurrentSpeedStr = "currentSpeed";
        private const string kGustStr = "gust";
        private const string kHumidityStr = "humidity";
        private const string kPrecipitationStr = "precipitation";
        private const string kSnowDepthStr = "snowDepth";
        private const string kVisibilityStr = "visibility";

        // Bio constants
        private const string kChlorophyllStr = "chlorophyll";
        private const string kIronStr = "iron";
        private const string kNitrateStr = "nitrate";
        private const string kPhytoStr = "phyto";
        private const string kOxygenStr = "oxygen";
        private const string kPhStr = "ph";
        private const string kPhytoplanktonStr = "phytoplankton";
        private const string kPhosphateStr = "phosphate";
        private const string kSilicateStr = "silicate";
        private const string kSalinityStr = "salinity";

        private const string kSoilMoistureStr = "soilMoisture";
        private const string kSoilTemperatureStr = "soilTemperature";

        public event Action<ExceptionType, string> RaiseException;

        private StormglassSimulationData _stormglassSimulationData;
        private StormglassAPIRequest _stormglassAPIRequest = new StormglassAPIRequest();
        private StormglassWeatherData _stormglassWeatherData;

        public StormglassWaterRequest(StormglassSimulationData stormglassSimulationData)
        {
            _stormglassSimulationData = stormglassSimulationData;
        }

        public async Task<StormglassWeatherData> SendStormglassAPIRequests()
        {
            if (string.IsNullOrWhiteSpace(_stormglassSimulationData.ApiKey))
            {
                RaiseException?.Invoke(ExceptionType.SystemException, "Stormglass service exception: Invalid API key");
                return null;
            }
            if (!_stormglassSimulationData.RequestWeatherData && !_stormglassSimulationData.RequestBioData && !_stormglassSimulationData.RequestTideData)
            {
                RaiseException?.Invoke(ExceptionType.SystemException, "Stormglass service exception: No data request type is selected");
                return null;
            }
            if (_stormglassSimulationData.RequestBioData)
            {
                await SendStormglassBioDataAPIRequest();
            }
            if (_stormglassSimulationData.RequestTideData)
            {
                await SendStormglassTideDataAPIRequest();
            }
            if (_stormglassSimulationData.RequestWeatherData)
            {
                var response = await SendStormglassWeatherDataAPIRequest();
                if (response != null)
                {
                    return response;
                }
            }

            return null;
        }

        private async Task<StormglassWeatherData> SendStormglassWeatherDataAPIRequest()
        {
            //Required
            string url = kStormglassURL + kStormglassWeatherEndPoint + kLatStr + _stormglassSimulationData.Latitude + kLonStr + _stormglassSimulationData.Longitude + kParamsStr;
            url += kSeaLevelParamStr + kSeparatorStr;
            url += kSwellDirectionParamStr + kSeparatorStr;
            url += kSecondarySwellDirectionParamStr + kSeparatorStr;
            url += kSwellHeightParamStr + kSeparatorStr;
            url += kSecondarySwellHeightParamStr + kSeparatorStr;
            url += kSwellPeriodParamStr + kSeparatorStr;
            url += kSecondarySwellPeriodParamStr + kSeparatorStr;
            url += kWaterTemperatureParamStr + kSeparatorStr;
            url += kWaveDirectionParamStr + kSeparatorStr;
            url += kWaveHeightParamStr + kSeparatorStr;
            url += kWavePeriodParamStr + kSeparatorStr;
            url += kWindWaveDirectionParamStr + kSeparatorStr;
            url += kWindWaveHeightParamStr + kSeparatorStr;
            url += kWindWavePeriodParamStr + kSeparatorStr;
            url += kWindDirectionParamStr + kSeparatorStr;
            url += kWindSpeedParamStr + kSeparatorStr;

            url += kAirTemperatureStr + kSeparatorStr;
            url += kPressureStr + kSeparatorStr;
            url += kCloudCoverStr + kSeparatorStr;
            url += kCurrentDirectionStr + kSeparatorStr;
            url += kCurrentSpeedStr + kSeparatorStr;
            url += kGustStr + kSeparatorStr;
            url += kHumidityStr + kSeparatorStr;
            url += kPrecipitationStr + kSeparatorStr;
            url += kSnowDepthStr + kSeparatorStr;
            url += kVisibilityStr;

            var response = await _stormglassAPIRequest.GetRequestAsync(url, kAuthorizationHeaderNameStr, _stormglassSimulationData.ApiKey, RequestDataType.Weather);
            return SaveWebRequest(response.Item1, response.Item2);
        }

        private async Task<StormglassWeatherData> SendStormglassBioDataAPIRequest()
        {
            //Optional
            string url = kStormglassURL + kStormglassBioEndPoint + kLatStr + _stormglassSimulationData.Latitude + kLonStr + _stormglassSimulationData.Longitude + kParamsStr;
            url += kChlorophyllStr + kSeparatorStr;
            url += kIronStr + kSeparatorStr;
            url += kNitrateStr + kSeparatorStr;
            url += kPhytoStr + kSeparatorStr;
            url += kOxygenStr + kSeparatorStr;
            url += kPhStr + kSeparatorStr;
            url += kPhytoplanktonStr + kSeparatorStr;
            url += kPhosphateStr + kSeparatorStr;
            url += kSilicateStr + kSeparatorStr;
            url += kSalinityStr + kSeparatorStr;
            url += kSoilMoistureStr + kSeparatorStr;
            url += kSoilTemperatureStr;

            var response = await _stormglassAPIRequest.GetRequestAsync(url, kAuthorizationHeaderNameStr, _stormglassSimulationData.ApiKey, RequestDataType.Bio);
            return SaveWebRequest(response.Item1, response.Item2);
        }

        private async Task<StormglassWeatherData> SendStormglassTideDataAPIRequest()
        {
            //Optional
            string url = kStormglassURL + kStormglassTideEndPoint + kLatStr + _stormglassSimulationData.Latitude + kLonStr + _stormglassSimulationData.Longitude;
            var response = await _stormglassAPIRequest.GetRequestAsync(url, kAuthorizationHeaderNameStr, _stormglassSimulationData.ApiKey, RequestDataType.Tide);
            return SaveWebRequest(response.Item1, response.Item2);
        }

        private StormglassWeatherData SaveWebRequest(string jsonData, RequestDataType dataType)
        {
            //Remove invalid file name characters
            _stormglassSimulationData.FileName = string.Join("", _stormglassSimulationData.FileName.Split(Path.GetInvalidFileNameChars()));
            if (_stormglassSimulationData.SaveDataToFile)
            {
                File.WriteAllText(Application.persistentDataPath + "/" + _stormglassSimulationData.FileName + "-" + dataType.ToString() + ".json", jsonData);
            }

            switch (dataType)
            {
                case RequestDataType.Weather:
                case RequestDataType.Maritime:
                    _stormglassWeatherData = JsonUtility.FromJson<StormglassWeatherData>(jsonData);
                    if (_stormglassSimulationData.StormglassData)
                    {
                        _stormglassSimulationData.StormglassData.weatherData = _stormglassWeatherData;
                    }
                    return _stormglassWeatherData;
                case RequestDataType.Bio:
                    if (_stormglassSimulationData.StormglassData)
                    {
                        _stormglassSimulationData.StormglassData.bioData = JsonUtility.FromJson<StormglassBioData>(jsonData);
                    }
                    break;
                case RequestDataType.Tide:
                    if (_stormglassSimulationData.StormglassData)
                    {
                        _stormglassSimulationData.StormglassData.tideData = JsonUtility.FromJson<StormglassTideData>(jsonData);
                    }
                    break;
            }
            return null;
        }
    }
}