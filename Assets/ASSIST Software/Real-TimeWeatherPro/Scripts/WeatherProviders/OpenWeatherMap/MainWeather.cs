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
    /// This class maintains the main data about the weather (temperature, pressure, humidity)
    /// </summary>
    [Serializable]
    public class MainWeather
    {
        #region Constructors
        public MainWeather()
        {
            temp = 0.0d;
            feels_like = 0.0d;
            temp_min = 0.0d;
            temp_max = 0.0d;
            pressure = 0;
            humidity = 0;
            sea_level = 0;
            sea_grnd = 0;
        }

        public MainWeather(double temp, double feels_like, double temp_min, double temp_max, int pressure, int humidity, int sea_level = 0, int sea_grnd = 0)
        {
            this.temp = temp;
            this.feels_like = feels_like;
            this.temp_min = temp_min;
            this.temp_max = temp_max;
            this.pressure = pressure;
            this.humidity = humidity;
            this.sea_level = sea_level;
            this.sea_grnd = sea_grnd;
        }
        #endregion

        #region Private Variables
        [SerializeField] private double temp;
        [SerializeField] private double feels_like;
        [SerializeField] private double temp_min;
        [SerializeField] private double temp_max;
        [SerializeField] private int pressure;
        [SerializeField] private int humidity;
        [SerializeField] private int sea_level;
        [SerializeField] private int sea_grnd;
        #endregion

        #region Public Properties
        /// <summary>
        /// Current temperature. 
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double Temperature
        {
            get
            {
                return temp;
            }

            set
            {
                temp = value;
            }
        }

        /// <summary>
        /// This temperature parameter accounts for the human perception of weather. 
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double FeelsLike
        {
            get
            {
                return feels_like;
            }

            set
            {
                feels_like = value;
            }
        }

        /// <summary>
        /// This is minimal currently observed temperature (within large megalopolises and urban areas).
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double MinimumTemperature
        {
            get
            {
                return temp_min;
            }

            set
            {
                temp_min = value;
            }
        }

        /// <summary>
        /// This is maximal currently observed temperature (within large megalopolises and urban areas).
        /// <para>Unit Default: Kelvin</para>
        /// <para>Metric: Celsius</para>
        /// <para>Imperial: Fahrenheit</para>
        /// </summary>
        public double MaximalTemperature
        {
            get
            {
                return temp_max;
            }

            set
            {
                temp_max = value;
            }
        }

        /// <summary>
        /// Atmospheric pressure (on the sea level, if there is no sea_level or grnd_level data), hPa
        /// </summary>
        public int Pressure
        {
            get
            {
                return pressure;
            }

            set
            {
                pressure = value;
            }
        }

        /// <summary>
        /// Humidity measured in procentages (%)
        /// </summary>
        public int Humidity
        {
            get
            {
                return humidity;
            }

            set
            {
                humidity = value;
            }
        }

        /// <summary>
        /// Atmospheric pressure on the sea level, hPa
        /// </summary>
        public int PressureAtSeaLevel
        {
            get
            {
                return sea_level;
            }

            set
            {
                sea_level = value;
            }
        }

        /// <summary>
        /// Atmospheric pressure on the ground level, hPa
        /// </summary>
        public int PressureAtGroundLevel
        {
            get
            {
                return sea_grnd;
            }

            set
            {
                sea_grnd = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Main=> temperature: {temp + ", feels like: " + feels_like + ", min: " + temp_min + ", max: " + temp_max + ", pressure: " + pressure + ", humidity: " + humidity + ", pressure (at sea): " + sea_level + ", pressure (at sea): " + sea_grnd}";
        }
        #endregion
    }
}
