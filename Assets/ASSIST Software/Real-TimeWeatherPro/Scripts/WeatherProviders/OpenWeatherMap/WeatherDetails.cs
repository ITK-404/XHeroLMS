//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class maintains some specific data about the current weather data (Weather condition codes)
    /// </summary>
    [Serializable]
    public class WeatherDetails
    {
        #region Constructors
        public WeatherDetails()
        {
            id = 0;
            main = string.Empty;
            description = string.Empty;
            icon = string.Empty;
        }

        public WeatherDetails(int id, string main, string description, string icon)
        {
            this.id = id;
            this.main = main;
            this.description = description;
            this.icon = icon;
        }
        #endregion

        #region Private Variables
        [SerializeField] private int id;
        [SerializeField] private string main;
        [SerializeField] private string description;
        [SerializeField] private string icon;
        #endregion

        #region Public Properties
        /// <summary>
        /// Weather condition ID. Check https://openweathermap.org/weather-conditions#Weather-Condition-Codes-2
        /// </summary>
        public int ID
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        /// <summary>
        /// Group of weather parameters (Rain, Snow, Extreme etc.)
        /// </summary>
        public string Main
        {
            get
            {
                return main;
            }

            set
            {
                main = value;
            }
        }

        /// <summary>
        /// Weather condition withing group.
        /// You can get the output in your language.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        /// <summary>
        /// Weather icon id. Check OpenWeatherMap website to see which images are coresponding to every icon id.
        /// Link: https://openweathermap.org/weather-conditions#Icon-list
        /// </summary>
        public string Icon
        {
            get
            {
                return icon;
            }

            set
            {
                icon = value;
            }
        }
        #endregion

        #region Public Methods

        public WeatherState GetWeatherState()
        {
            return (WeatherState)id;
        }

        public override string ToString()
        {
            return $"Weather details=> Weather code ID: {id + ", main: " + main + ", description: " + description + ", icon: " + icon}";
        }
        #endregion
    }
}
