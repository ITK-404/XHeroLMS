//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using RealTimeWeather.Data;
using RealTimeWeather.Enums;
using RealTimeWeather.Simulation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// Get and convert OpenWeather data to forecast data
    /// </summary>
    public class OpenWeatherToForecastPreset
    {
        private const string kServer = "https://api.openweathermap.org/data/3.0/onecall/timemachine?";
        private const string kInputLatitudeIdentifier = "lat=";
        private const string kInputLongitudeIdentifier = "&lon=";
        private const string kDateIndentifier = "&dt=";
        private const string kInputUnitsIdentifier = "&units=metric";
        private const string kInputAppIDIdentifier = "&appid=";
        private const string kDateFormat = "dd/MM/yyyy";

        private WeatherAPIRequest _weatherWebRequest;
        private ForecastToTimelapseConvertor _forecastToTimelapseConvertor = new ForecastToTimelapseConvertor();

        public OpenWeatherToForecastPreset()
        {
            _weatherWebRequest = new WeatherAPIRequest();
            _weatherWebRequest.onErrorRaised += OnServerError;
        }

        public async Task SaveDataSequence(string ApiKei, float latitude, float longitude, string startDate, string endDate)
        {
            if (_weatherWebRequest == null)
            {
                _weatherWebRequest = new WeatherAPIRequest();
            }

            if (_forecastToTimelapseConvertor == null)
            {
                _forecastToTimelapseConvertor = new ForecastToTimelapseConvertor();
            }

            DateTime dateTimeStart = DateTime.Now;
            DateTime dateTimeEnd = DateTime.Now;

            try
            {
                dateTimeStart = DateTime.ParseExact(startDate, kDateFormat, CultureInfo.InvariantCulture);
                dateTimeEnd = DateTime.ParseExact(endDate, kDateFormat, CultureInfo.InvariantCulture);
                var maxTimeEnd = DateTime.Now.AddDays(4);

                if (dateTimeEnd < dateTimeStart)
                {
                    Debug.LogWarning("Start date could not be be before the end date");
                    return;
                }

                if(dateTimeEnd > maxTimeEnd)
                {
                    Debug.LogWarning("We could only provide data only for 4 days in future");
                    return;
                }

                var daysNr = dateTimeEnd.Subtract(dateTimeStart).Days;

                if (daysNr > 5)
                {
                    Debug.Log("5 days is the limit");
                    return;
                }

                dateTimeEnd = dateTimeEnd.AddDays(1);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Invalid input for start or end date");
                Debug.LogWarning(ex);
                return;
            }

            if(ApiKei == string.Empty)
            {
                Debug.LogWarning("Please provide api key");
                return;
            }

            OpenWeatherHistoryCallApiMapData resultedData2 = null;
            while (dateTimeStart < dateTimeEnd)
            {
                var localZone = TimeZoneInfo.Local; // Get the local time zone
                var offset = localZone.GetUtcOffset(dateTimeStart); // Get the UTC offset for the current time in the local time zone
                var unixOffset = (long)offset.TotalSeconds; // Convert the offset to the number of seconds since the Unix epoch
                var unixTime = ((DateTimeOffset)dateTimeStart).ToUnixTimeSeconds() + unixOffset;
                var url = GetUrl(unixTime, latitude, longitude, ApiKei);

                try
                {
                    var response = await _weatherWebRequest.GetRequestAsync(url);
                    if (response != string.Empty)
                    {
                        string resultData = response;
                        OpenWeatherHistoryCallApiMapData resultedData = JsonUtility.FromJson<OpenWeatherHistoryCallApiMapData>(resultData);

                        if (resultedData2 == null)
                        {
                            resultedData2 = resultedData;
                        }
                        else
                        {
                            resultedData2.WeatherData.Add(resultedData.WeatherData[0]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }
                dateTimeStart = dateTimeStart.AddHours(1);
            }

            if (resultedData2 == null) return;

            List<WeatherData> forecastData = new List<WeatherData>();
            var converter = new OpenWeatherOneCallAPIMapConverter(resultedData2);
            forecastData = converter.ConvertHistoryWeatherDataToRealTimeManagerWeatherListData();

            _forecastToTimelapseConvertor.CreateTimelapseData(forecastData, ForecastDataType.Hourly, latitude, longitude, true, IntervalLerpSpeed.Slow, 5, startDate);
        }

        public string GetUrl(long date, float latitude, float longitude, string key)
        {
            return kServer + kInputLatitudeIdentifier + latitude + kInputLongitudeIdentifier + longitude + kDateIndentifier + date + kInputUnitsIdentifier + kInputAppIDIdentifier + key;
        }

        private void OnServerError(ExceptionType exceptionType, string errorMessage)
        {
            LogFile.Write("OpenWeather service exception:" + exceptionType.ToString() + " => " + errorMessage);
            Debug.Log("OpenWeather service exception:" + exceptionType.ToString() + " => " + errorMessage);
        }
    }
}
#endif