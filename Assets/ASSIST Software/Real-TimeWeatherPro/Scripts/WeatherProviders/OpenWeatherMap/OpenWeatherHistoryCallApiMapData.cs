//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.WeatherProvider.OpenWeather;
using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.Data
{
    public class OpenWeatherHistoryCallApiMapData
    {
        public OpenWeatherHistoryCallApiMapData()
        {
            data = new List<HourlyWeather>();
        }

        [SerializeField] private double lon;
        [SerializeField] private double lat;
        [SerializeField] private string timezone;
        [SerializeField] private int timezone_offset;
        [SerializeField] private List<HourlyWeather> data;

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

        public List<HourlyWeather> WeatherData
        {
            get { return data; }
            set { data = value; }
        }
    }
}