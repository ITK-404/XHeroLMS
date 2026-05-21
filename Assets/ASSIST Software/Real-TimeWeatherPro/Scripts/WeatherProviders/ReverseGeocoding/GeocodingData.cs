// 
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather.WeatherProvider
{
    /// <summary>
    /// <para>
    /// This class manages Nominatim API geocoding data.
    /// </para>
    /// </summary>
    [System.Serializable]
    public class GeocodingData
    {
        #region Constructors
        public GeocodingData()
        {
            lat = 0f;
            lon = 0f;
            address = new AddressData();
        }
        #endregion

        #region Private Variables
        [Range(-90.0f, 90.0f)]
        [SerializeField]
        private float lat;
        [SerializeField]
        [Range(-180.0f, 180.0f)]
        private float lon;
        [SerializeField]
        private AddressData address;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// It's a float value that represets a geographic coordinate that specifies the north–south position of a point on the Earth's surface.
        /// </para> 
        /// </summary>
        public float Latitude
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

        /// <summary>
        /// <para>
        /// It's a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.
        /// </para> 
        /// </summary>
        public float Longitude
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
        /// Contains every address information retrieved from the Nominatim API
        /// </summary>
        public AddressData Address 
        {
            get
            {
                return address;
            }
            set
            {
                address = value;
            }
        }
        #endregion

        #region Public Methods       
        /// <summary>
        /// Concatenates the attributes of the GeocodingData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Geocoding Data <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n" +
                   $"Latitude: {lat}\n" +
                   $"Longitude: {lon}\n" +
                   $"Neighbourhood name: {address.Neighbourhood}\n" +
                   $"Suburb name: {address.Suburb}\n" +
                   $"Municipality name: {address.Municipality}\n" +
                   $"Village name: {address.Village}\n" +
                   $"Town name: {address.Town}\n" +
                   $"City name: {address.City}\n" +
                   $"County name: {address.County}\n" +
                   $"State name: {address.State}\n" +
                   $"District name: {address.District}\n" +
                   $"Postal code: {address.Postcode}\n" +
                   $"Country name: {address.Country}\n" +
                   $"Country code: {address.CountryCode}\n"
                   );
        }
        #endregion
    }
}