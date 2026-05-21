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
    /// This class maintains data about the clouds properties (a single one at the moment, cloudiness)
    /// </summary>
    [Serializable]
    public class Clouds
    {
        #region Constructors
        public Clouds()
        {
            all = 0;
        }

        public Clouds(int all)
        {
            this.all = all;
        }
        #endregion

        #region Private Variables
        [SerializeField] private int all;
        #endregion

        #region Public Properties
        /// <summary>
        /// Cloudiness property measured in percentages (%)
        /// </summary>
        public int Cloudiness
        {
            get
            {
                return all;
            }

            set
            {
                all = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Clouds=> cloudiness: {all}";
        }
        #endregion
    }
}
