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
using RealTimeWeather.WeatherProvider;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace RealTimeWeather.WeatherProvider.Tomorrow
{
    /// <summary>
    /// Get and convert Tomorrow.io data to forecast data
    /// </summary>
    public class TomorrowToForecastPreset
    {
        private WeatherAPIRequest _weatherWebRequest;
        private ForecastToTimelapseConvertor _forecastToTimelapseConvertor = new ForecastToTimelapseConvertor();

        public TomorrowToForecastPreset()
        {
            _weatherWebRequest = new WeatherAPIRequest();
            _weatherWebRequest.onErrorRaised += OnServerError;
        }

        public async void SaveDataSequence(string url, float latitude, float longitude, ForecastDataType type, int hourlyForecastLength, int dailyForecastLength, bool loop, IntervalLerpSpeed intervalLerpSpeed, int simulationSpeed)
        {
            try
            {
                var response = await _weatherWebRequest.GetRequestAsync(url);
                if (response != string.Empty)
                {
                    TomorrowCoreData tomorrowData = JsonUtility.FromJson<TomorrowCoreData>(response);
                    tomorrowData.Latitude = latitude;
                    tomorrowData.Longitude = longitude;

                    TomorrowDataConverter tomorrowDataConverter = new TomorrowDataConverter(tomorrowData);

                    List<WeatherData> forecastData = new List<WeatherData>();
                    if (type == ForecastDataType.Hourly)
                    {
                        forecastData = tomorrowDataConverter.ConvertHourlyTomorrowDataToRtwData();
                        forecastData.RemoveRange(hourlyForecastLength + 1, forecastData.Count - hourlyForecastLength - 1);
                    }
                    else
                    {
                        forecastData = tomorrowDataConverter.ConvertDailyTomorrowDataToRtwData();
                        forecastData.RemoveRange(dailyForecastLength + 1, forecastData.Count - dailyForecastLength - 1);
                    }

                    _forecastToTimelapseConvertor.CreateTimelapseData(forecastData, type, latitude, longitude, loop, intervalLerpSpeed, simulationSpeed);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }

        private void OnServerError(ExceptionType exceptionType, string errorMessage)
        {
            LogFile.Write("Tomorrow.io service exception:" + exceptionType.ToString() + " => " + errorMessage);
            Debug.Log("Tomorrow.io service exception:" + exceptionType.ToString() + " => " + errorMessage);
        }
    }
}
#endif