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
    /// This class represents the structure for the temperature. Data obtained from OpenWeatherMap One Call API server.
    /// </summary>
    [Serializable]
    public class Temperature
    {
        #region Private Variables
        [SerializeField] private double morning;
        [SerializeField] private double day;
        [SerializeField] private double eve;
        [SerializeField] private double night;
        [SerializeField] private double min;
        [SerializeField] private double max;
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

        /// <summary>
        /// Min Daily Temperature
        /// </summary>
        public double MinDailyTemperature
        {
            get { return min; }
            set { min = value; }
        }

        /// <summary>
        /// Max Daily Temperature
        /// </summary>
        public double MaxDailyTemperature
        {
            get { return max; }
            set { max = value; }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return //"\n====================> Temperature Data <============================\n" +
                $"Morning temperature: {MorningTemperature}" + $" | Day temperature: {DayTemperature}" + $" | Evening temperature: {EveningTemperature}" + $" | Night temperature: {NightTemperature}" + $" | Min daily temperature: {MinDailyTemperature}" + $" | Max daily temperature: {MaxDailyTemperature}";
        }
        #endregion
    }
}
