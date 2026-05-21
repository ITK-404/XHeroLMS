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
    /// This class contains all afferent data about the geographic coordinates (longitude, latitude)
    /// </summary>
    [Serializable]
    public class GeographicCoordinates
    {
        #region Constructors
        public GeographicCoordinates()
        {
            lon = 0.0d;
            lat = 0.0d;
        }

        public GeographicCoordinates(double lon, double lat)
        {
            this.lon = lon;
            this.lat = lat;
        }
        #endregion

        #region Private Variables
        [SerializeField] private double lon;
        [SerializeField] private double lat;
        #endregion

        #region Public Properties
        /// <summary>
        /// City geo location, longitude, values between [-180, 180]
        /// </summary>
        public double Longitude
        {
            get
            {
                return lon;
            }

            set
            {
                lon = value;
            }
        }

        /// <summary>
        /// City geo location, latitude, values between [-90, 90]
        /// </summary>
        public double Latitude
        {
            get
            {
                return lat;
            }

            set
            {
                lat = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Geographic Coordinates=> longitude: {lon + ", latitude: " + lat}";
        }
        #endregion
    }
}
