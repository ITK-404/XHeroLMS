// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using RealTimeWeather.Data;
using RealTimeWeather.Managers;
using UnityEngine;

namespace RealTimeWeather.Simulation
{
    public class TimelapseInterval
    {
        #region Public Variables
        public ForecastWeatherData weatherData;
        public string weatherDataProfileStr = "Weather Data: Default Interval";
        public IntervalState intervalState;
        public string presetName;
        public Color presetColor;
        #endregion

        #region Constructors
        public TimelapseInterval()
        {
            weatherData = RealTimeWeatherManager.instance.DefaultForecastData;
            intervalState = IntervalState.DefaultInterval;
            presetName = "Clear";
            presetColor = RealTimeWeatherManager.instance.DefaultForecastData.color;
        }

        public TimelapseInterval(WeatherData defaultIntervalData)
        {
            weatherData = new ForecastWeatherData(defaultIntervalData);
        }

        public TimelapseInterval(ForecastWeatherData forecastWeatherData)
        {
            weatherData = forecastWeatherData;
            presetColor = forecastWeatherData.color;
            presetName = forecastWeatherData.name;
        }

        public TimelapseInterval(TimelapseInterval intervalData)
        {
            weatherData = new ForecastWeatherData(intervalData.weatherData);
            weatherDataProfileStr = intervalData.weatherDataProfileStr;
            intervalState = intervalData.intervalState;
            presetName = intervalData.presetName;
            presetColor = intervalData.presetColor;
        }
        #endregion
    }
}
