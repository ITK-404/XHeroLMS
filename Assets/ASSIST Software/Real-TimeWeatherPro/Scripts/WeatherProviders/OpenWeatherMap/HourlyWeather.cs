//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;
using System;
using System.Collections.Generic;

namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class maintains weather data for a period of an hour
    /// </summary>
    [Serializable]
    public class HourlyWeather
    {
        #region Constructors
        public HourlyWeather()
        {
            weather = new List<WeatherDetails>();
        }
        #endregion

        #region Private Variables
        [SerializeField] private int dt;
        [SerializeField] private double temp;
        [SerializeField] private double feels_like;
        [SerializeField] private int pressure;
        [SerializeField] private int humidity;
        [SerializeField] private double dew_point;
        [SerializeField] private double uvi;
        [SerializeField] private int clouds;
        [SerializeField] private int visibility;
        [SerializeField] private float wind_speed;
        [SerializeField] private int wind_deg;
        [SerializeField] private float wind_gust;
        [SerializeField] private double sunrise;
        [SerializeField] private double sunset;
        [SerializeField] private List<WeatherDetails> weather;
        #endregion

        #region Public Properties
        /// <summary>
        /// Data receiving time, unix, UTC
        /// </summary>
        public int UnixTimestamp
        {
            get { return dt; }
            set { dt = value; }
        }
        /// <summary>
        /// CurrentWeather temperature.
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double Temperature
        {
            get { return temp; }
            set { temp = value; }
        }
        /// <summary>
        /// This temperature parameter accounts for the human perception of weather.
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double FeelsLike
        {
            get { return feels_like; }
            set { feels_like = value; }
        }
        /// <summary>
        /// Atmospheric pressure (on the sea level, if there is no sea_level or grnd_level data), hPa
        /// </summary>
        public int Pressure
        {
            get { return pressure; }
            set { pressure = value; }
        }
        /// <summary>
        /// Humidity measured in procentages (%)
        /// </summary>
        public int Humidity
        {
            get { return humidity; }
            set { humidity = value; }
        }
        /// <summary>
        /// Dew Point - Atmospheric temperature below which water droplets begin to condense and dew can form
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double DewPoint
        {
            get { return dew_point; }
            set { dew_point = value; }
        }
        /// <summary>
        /// CurrentWeather UV index
        /// </summary>
        public double UVI
        {
            get { return uvi; }
            set { uvi = value; }
        }
        /// <summary>
        /// Cloudiness, represented by percentages
        /// </summary>
        public int Clouds
        {
            get { return clouds; }
            set { clouds = value; }
        }
        /// <summary>
        /// Average visibility, metres
        /// </summary>
        public int Visibility
        {
            get { return visibility; }
            set { visibility = value; }
        }
        /// <summary>
        /// The actual speed of the wind.
        /// <para>Unit Default: meter/sec</para>
        /// <para>Metric: meter/sec</para>
        /// <para>Imperial: miles/hour</para>
        /// </summary>
        public float WindSpeed
        {
            get { return wind_speed; }
            set { wind_speed = value; }
        }
        /// <summary>
        /// Wind direction, degrees (meteorological)
        /// </summary>
        public int WindDeg
        {
            get { return wind_deg; }
            set { wind_deg = value; }
        }
        /// <summary>
        /// Wind gust
        /// <para>Unit Default: meter/sec</para>
        /// <para>Metric: meter/sec</para>
        /// <para>Imperial: miles/hour</para>
        /// </summary>
        public float WindGust
        {
            get { return wind_gust; }
            set { wind_gust = value; }
        }

        /// <summary>
        /// A list with weather data type (Sunny, Clear, Cloudy, etc.)
        /// </summary>
        public List<WeatherDetails> WeatherDetailsList
        {
            get { return weather; }
            set { weather = value; }
        }

        public double Sunrise { get => sunrise; set => sunrise = value; }
        public double Sunset { get => sunset; set => sunset = value; }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            string weatherProp = string.Empty;

            foreach (var t in weather)
            {
                weatherProp += "Weather=> Weather code ID: " + t.ID + " | state: " + t.Main + " | description: " + t.Description + " | icon: " + t.Icon + "\n";
            }


            return
                $"Unix timestamp: {dt}\n" +
                $"Temperature: {Temperature}\n" +
                $"Feels like: {FeelsLike}\n" +
                $"Pressure: {Pressure}\n" +
                $"Humidity: {Humidity}\n" +
                $"Dew Point: {DewPoint}\n" +
                $"UVI: {UVI}\n" +
                $"Cloudiness: {clouds}\n" +
                $"Visibility: {visibility}\n" +
                $"Wind=> Speed: {WindSpeed}\n" +
                $"Degrees: {WindDeg}\n" +
                $" | Gust: {WindGust}\n" +
                weatherProp;
        }
        #endregion
    }
}
