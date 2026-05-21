//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;
using UnityEngine;
using RealTimeWeather.Data;

namespace RealTimeWeather.Simulation
{
    [CreateAssetMenu(fileName = "Weather Presets", menuName = "Real-Time Weather/Timelapse/Weather Presets", order = 0)]
    public class WeatherPresets : ScriptableObject
    {
        public string folderName;
        public List<ForecastWeatherData> forecastCustomPresets = new List<ForecastWeatherData>();

        public void AddWeatherDataToCustomPresets(ForecastWeatherData weatherData)
        {
            weatherData.forecastDateTime = new DateTime().ToString();
            forecastCustomPresets.Insert(0, weatherData);
        }

        public ForecastWeatherData GetWeatherDataFromCustomPresetsAtIndex(int index)
        {
            return forecastCustomPresets[index];
        }

        public void UpdateCustomPresetsAtIndex(ForecastWeatherData forecastData, int index)
        {
            forecastCustomPresets[index] = new ForecastWeatherData(forecastData);
        }
    }
}