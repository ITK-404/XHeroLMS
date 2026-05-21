//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
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
    /// This class represents the structure for the human perception of weather. Data obtained from OpenWeatherMap One Call API server.
    /// </summary>
    [Serializable]
    public class FeelsLikeWeather
    {
        #region Private Variables
        [SerializeField] private double morning;
        [SerializeField] private double day;
        [SerializeField] private double eve;
        [SerializeField] private double night;
        #endregion

        #region Public Properties
        /// <summary>
        /// Morning Temperature
        /// </summary>
        public double MorningTemperature
        {
            get { return morning; }
            set { morning = value; }
        }

        /// <summary>
        /// Day Temperature
        /// </summary>
        public double DayTemperature
        {
            get { return day; }
            set { day = value; }
        }

        /// <summary>
        /// Evening Temperature
        /// </summary>
        public double EveningTemperature
        {
            get { return eve; }
            set { eve = value; }
        }

        /// <summary>
        /// Night Temperature
        /// </summary>
        public double NightTemperature
        {
            get { return night; }
            set { night = value; }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return //"\n====================> Feels Like Weather Data <============================\n" +
                $"Morning temperature: {MorningTemperature}" + $" | Day temperature: {DayTemperature}" + $" | Evening temperature: {EveningTemperature}" + $" | Night temperature: {NightTemperature}";
        }
        #endregion
    }
}
