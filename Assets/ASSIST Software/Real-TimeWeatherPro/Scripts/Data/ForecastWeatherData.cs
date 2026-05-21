//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using UnityEngine;
using RealTimeWeather.UI;
using RealTimeWeather.Managers;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class ForecastWeatherData
    {
        public ForecastLocationData forecastLocation;
        public string forecastDateTime;
        public ForecastWindData forecastWind;
        public WeatherState forecastWeatherState;
        public float forecastTemperature;
        public float forecastPressure;
        public float foreacastPrecipitation;
        public float forecastHumidity;
        public float forecastDewpoint;
        public float forecastVisibility;
        public float forecastIndexUV;
        public string forecastTimeZone;
        public string UTCOffset;
        public Color color = new Color(1,1,1,1);
        public Texture2D texture;
        public string name;

        public ForecastWeatherData() { }

        public ForecastWeatherData(WeatherData weatherData)
        {
            forecastLocation = new ForecastLocationData(weatherData.Localization);
            forecastDateTime = weatherData.DateTime.ToString();
            forecastWind = new ForecastWindData(weatherData.Wind);
            forecastWeatherState = weatherData.WeatherState;
            forecastTemperature = weatherData.Temperature;
            forecastPressure = weatherData.Pressure;
            foreacastPrecipitation = weatherData.Precipitation;
            forecastHumidity = weatherData.Humidity;
            forecastDewpoint = weatherData.Dewpoint;
            forecastVisibility = weatherData.Visibility;
            forecastIndexUV = weatherData.IndexUV;
            forecastTimeZone = weatherData.TimeZone;
            UTCOffset = weatherData.UTCOffset.ToString();
        }

        public ForecastWeatherData(ForecastWeatherData forecastWeatherData)
        {
            forecastLocation = forecastWeatherData.forecastLocation;
            forecastDateTime = forecastWeatherData.forecastDateTime;
            forecastWind = forecastWeatherData.forecastWind;
            forecastWeatherState = forecastWeatherData.forecastWeatherState;
            forecastTemperature = forecastWeatherData.forecastTemperature;
            forecastPressure = forecastWeatherData.forecastPressure;
            foreacastPrecipitation = forecastWeatherData.foreacastPrecipitation;
            forecastHumidity = forecastWeatherData.forecastHumidity;
            forecastDewpoint = forecastWeatherData.forecastDewpoint;
            forecastVisibility = forecastWeatherData.forecastVisibility;
            forecastIndexUV = forecastWeatherData.forecastIndexUV;
            forecastTimeZone = forecastWeatherData.forecastTimeZone;
            UTCOffset = forecastWeatherData.UTCOffset;
            color = forecastWeatherData.color;
            texture = forecastWeatherData.texture;
            name = forecastWeatherData.name;
        }
    }
}
