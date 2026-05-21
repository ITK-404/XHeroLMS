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
    /// This class maintains data about wind properties (speed, degrees, gust)
    /// </summary>
    [Serializable]
    public class Wind
    {
        #region Constructors
        public Wind()
        {
            speed = 0.0f;
            deg = 0;
            gust = 0.0f;
        }

        public Wind(float speed, int deg, float gust = 0.0f)
        {
            this.speed = speed;
            this.deg = deg;
            this.gust = gust;
        }
        #endregion

        #region Private Variables
        [SerializeField] private float speed;
        [SerializeField] private int deg;
        [SerializeField] private float gust;
        #endregion

        #region Public Properties
        /// <summary>
        /// The actual speed of the wind.
        /// <para>Unit Default: meter/sec</para>
        /// <para>Metric: meter/sec</para>
        /// <para>Imperial: miles/hour</para>
        /// </summary>
        public float Speed
        {
            get
            {
                return speed;
            }

            set
            {
                speed = value;
            }
        }

        /// <summary>
        /// Wind direction, degrees (meteorological)
        /// </summary>
        public int Deg
        {
            get
            {
                return deg;
            }

            set
            {
                deg = value;
            }
        }

        /// <summary>
        /// The actual gust of the wind.
        /// <para>Unit Default: meter/sec</para>
        /// <para>Metric: meter/sec</para>
        /// <para>Imperial: miles/hour</para>
        /// </summary>
        public float Gust
        {
            get
            {
                return gust;
            }

            set
            {
                gust = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Wind data=> Speed: {speed + ", degrees: " + deg  + ", gust: " + gust}";
        }
        #endregion
    }
}
