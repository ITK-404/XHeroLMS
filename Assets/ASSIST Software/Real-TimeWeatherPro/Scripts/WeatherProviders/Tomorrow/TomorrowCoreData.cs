//
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Tomorrow
{
    /// <summary>
    /// <para>
    /// This class manages Tomorrow data.
    /// </para>
    /// </summary>
    [Serializable]
    public class TomorrowCoreData
    {
        #region Constructors
        public TomorrowCoreData()
        {
            data = new CoreData();
            latitude = 0f;
            longitude = 0f;
            country = string.Empty;
            city = string.Empty;
        }

        public TomorrowCoreData(CoreData data, float latitude, float longitude, string country, string city)
        {
            this.data = data;
            this.latitude = latitude;
            this.longitude = longitude;
            this.country = country;
            this.city = city;
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private string country;
        [SerializeField]
        private string city;
        [SerializeField]
        [Range(-90.0f, 90.0f)]
        private float latitude;
        [SerializeField]
        [Range(-180.0f, 180.0f)]
        private float longitude;
        [SerializeField]
        private CoreData data;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// A CoreData instance that represents the Tomorrow API data.
        /// </para>
        /// </summary>
        public CoreData Data
        {
            get { return data; }
            set { data = value; }
        }

        /// <summary>
        /// <para>
        /// Is a float value that represents a geographic coordinate that specifies the north–south position of a point on the Earth's surface.
        /// </para>
        /// <para>
        /// Latitude must be set according to ISO 6709.
        /// </para>
        /// </summary>
        public float Latitude
        {
            get { return latitude; }
            set { latitude = value; }
        }

        /// <summary>
        /// <para>
        /// Is a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.
        /// </para>
        /// <para>
        ///  Longitude must be set according to ISO 6709.
        /// </para>
        /// </summary>
        public float Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }

        /// <summary>
        /// <para>
        /// Is a string value that represents city name.
        /// </para>
        /// </summary>
        public string City
        {
            get { return city; }
            set { city = value; }
        }

        /// <summary>
        /// <para>
        /// Is a string value that represents country name.
        /// </para>
        /// </summary>
        public string Country
        {
            get { return country; }
            set { country = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the TomorrowData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Tomorrow Data <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n" +
                   $"{data.ToString()}"
                   );
        }
        #endregion
    }
}