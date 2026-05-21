//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
// 

using System;
using System.Collections.Generic;
using RealTimeWeather.Classes;
using RealTimeWeather.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace RealTimeWeather.UI
{
    public class CellContents : MonoBehaviour
    {
        #region Private Constants
        private const string kDegreesCelsius = "°C";
        #endregion

        #region Private Members
        private Dictionary<WeatherState, Sprite> weatherStateIcons;
        #endregion

        #region Public Variables
        public Image weatherStatus;
        public Image highlight;
        public Text dayOfWeek;
        public Text temperature;

        [Header("Weather Status Icon Resources")]
        public Sprite NoData;
        public Sprite clearDay;
        public Sprite partlyClearDay;
        public Sprite cloudy;
        public Sprite mist;
        public Sprite snowPrecipitation;
        public Sprite rainPrecipitation;
        public Sprite thunderstorm;
        public Sprite windy;
        #endregion

        #region Public Member Functions
        public void SetDefaultContents()
        {
            dayOfWeek.text = "N/A";
            temperature.text = "";
            weatherStatus.sprite = NoData;
        }

        // Sets the weather parameters for the current day (day of week, temperature, weather icon)
        public void SetCellContents(WeatherData weatherData)
        {
            dayOfWeek.text = weatherData.DateTime.Date.DayOfWeek.ToString();
            temperature.text = Math.Round(weatherData.Temperature) + kDegreesCelsius;

            SetWeatherStateIcon(weatherStatus, weatherData);
        }

        // Highlight current day in simulation
        public void SetHighlight(bool toggle)
        {
            highlight.gameObject.SetActive(toggle);
        }
        #endregion

        #region Private Methods
        // Sets the appropriate weather icon based on weather state
        private void SetWeatherStateIcon(Image targetImage, WeatherData weatherData)
        {
            if (weatherStateIcons == null)
                CreateWeatherIconStateDictionary();

            targetImage.sprite = weatherStateIcons[weatherData.WeatherState];
        }

        private void CreateWeatherIconStateDictionary()
        {
            weatherStateIcons = new Dictionary<WeatherState, Sprite>();

            weatherStateIcons.Add(WeatherState.Sunny, clearDay);
            weatherStateIcons.Add(WeatherState.Clear, clearDay);
            weatherStateIcons.Add(WeatherState.Fair, clearDay);

            weatherStateIcons.Add(WeatherState.Cloudy, cloudy);

            weatherStateIcons.Add(WeatherState.PartlyClear, partlyClearDay);
            weatherStateIcons.Add(WeatherState.PartlySunny, partlyClearDay);
            weatherStateIcons.Add(WeatherState.PartlyCloudy, partlyClearDay);

            weatherStateIcons.Add(WeatherState.Mist, mist);

            weatherStateIcons.Add(WeatherState.Thunderstorms, thunderstorm);

            weatherStateIcons.Add(WeatherState.Windy, windy);

            weatherStateIcons.Add(WeatherState.RainPrecipitation, rainPrecipitation);

            weatherStateIcons.Add(WeatherState.SnowPrecipitation, snowPrecipitation);
            weatherStateIcons.Add(WeatherState.RainSnowPrecipitation, snowPrecipitation);
        }
        #endregion
    }
}