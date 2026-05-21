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
    /// This class maintains data about internal parameters.
    /// </summary>
    [Serializable]
    public class SystemData
    {
        #region Constructors
        public SystemData()
        {
            type = 0;
            id = 0;
            message = string.Empty;
            country = string.Empty;
            sunrise = 0;
            sunset = 0;
        }

        public SystemData(int type, int id, string message, string country, int sunrise, int sunset)
        {
            this.type = type;
            this.id = id;
            this.country = message;
            this.country = country;
            this.sunrise = sunrise;
            this.sunset = sunset;
        }
        #endregion

        #region Private Variables
        [SerializeField] private int type;
        [SerializeField] private int id;
        [SerializeField] private string message;
        [SerializeField] private string country;
        [SerializeField] private int sunrise;
        [SerializeField] private int sunset;
        #endregion

        #region Public Properties
        /// <summary>
        /// This is an internal parameter
        /// </summary>
        public int Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        /// <summary>
        /// This is an internal parameter
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
        /// Message representing additional details about the request (request failed, missing data)
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Country code (GB, JP etc.)
        /// </summary>
        public string Country
        {
            get
            {
                return country;
            }

            set
            {
                country = value;
            }
        }

        /// <summary>
        /// Sunrise time, unix, UTC
        /// </summary>
        public int Sunrise
        {
            get
            {
                return sunrise;
            }

            set
            {
                sunrise = value;
            }
        }

        /// <summary>
        /// Sunset time, unix, UTC
        /// </summary>
        public int Sunset
        {
            get
            {
                return sunset;
            }

            set
            {
                sunset = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"System=> type: {type + ", ID: " + id + ", country: " + country + ", sunrise: " + sunrise + ", sunset: " + sunset + ", message: " + message}";
        }
        #endregion
    }
}
