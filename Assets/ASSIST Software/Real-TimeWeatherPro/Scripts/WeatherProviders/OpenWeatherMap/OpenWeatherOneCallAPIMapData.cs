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

namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class represents the structure for the weather data obtained from OpenWeatherMap One Call API server.
    /// </summary>
    [Serializable]
    public class OpenWeatherOneCallAPIMapData
    {
        #region Constructors
        public OpenWeatherOneCallAPIMapData()
        {
            hourly = new List<HourlyWeather>();
            daily = new List<DailyWeather>();
        }
        #endregion

        #region Private Variables
        [SerializeField] private double lon;
        [SerializeField] private double lat;
        [SerializeField] private string timezone;
        [SerializeField] private int timezone_offset;
        [SerializeField] private List<HourlyWeather> hourly;
        [SerializeField] private List<DailyWeather> daily;

        //This field is not a part of the structure, but is useful to know in which type of units is the data
        private Units units;
        #endregion

        #region Public Properties
        /// <summary>
        /// City geo location, longitude, values between [-180, 180]
        /// </summary>
        public double Longitude
        {
            get { return lon; }
            set { lon = value; }
        }

        /// <summary>
        /// City geo location, latitude, values between [-90, 90]
        /// </summary>
        public double Latitude
        {
            get { return lat; }
            set { lat = value; }
        }

        /// <summary>
        /// Timezone name for the requested location
        /// </summary>
        public string TimeZone
        {
            get { return timezone; }
            set { timezone = value; }
        }

        /// <summary>
        /// Shift in seconds from UTC
        /// </summary>
        public int TimezoneOffset
        {
            get { return timezone_offset; }
            set { timezone_offset = value; }
        }

        public List<HourlyWeather> HourlyWeather
        {
            get { return hourly; }
            set { hourly = value; }
        }
        public List<DailyWeather> DailyWeather
        {
            get { return daily; }
            set { daily = value; }
        }

        /// <summary>
        /// This specify which type of units are used to describe the weather parameters.
        /// </summary>
        public Units Units
        {
            get { return units; }
            set { units = value; }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            string hourlyWeather = string.Empty;
            string dailyWeather = string.Empty;

            foreach (var t in HourlyWeather)
            {
                hourlyWeather += " {" + t + " }";
            }
            foreach (var t in DailyWeather)
            {
                dailyWeather += " {" + t + " }";
            }

            return "\n====================> OpenWeatherMap Data <============================\n" +
                $"Location: {"Timezone: " + TimeZone + "| Timezone Offset: " + TimezoneOffset + " | latitude:" + Latitude + " | longitude:" + Longitude}\n" +
                $"Hourly Weather=>\n {hourlyWeather}\n" +
                $"Daily Weather=>\n {dailyWeather}\n";
        }
        #endregion
    }
}
