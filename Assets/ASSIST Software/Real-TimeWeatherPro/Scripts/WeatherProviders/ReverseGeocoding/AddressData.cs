// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather.WeatherProvider
{
    /// <summary>
    /// This class contains every bit of address information retrieved from the Nominatim API
    /// </summary>
    [System.Serializable]
    public class AddressData
    {
        #region Constructors
        public AddressData()
        {
            neighbourhood = string.Empty;
            municipality = string.Empty;
            village = string.Empty;
            town = string.Empty;
            city = string.Empty;
            county = string.Empty;
            state = string.Empty;
            district = string.Empty;
            postcode = string.Empty;
            country = string.Empty;
            country_code = string.Empty;
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private string neighbourhood;
        [SerializeField]
        private string suburb;
        [SerializeField]
        private string municipality;
        [SerializeField]
        private string village;
        [SerializeField]
        private string town;
        [SerializeField]
        private string city;
        [SerializeField]
        private string county;
        [SerializeField]
        private string state;
        [SerializeField]
        private string district;
        [SerializeField]
        private string postcode;
        [SerializeField]
        private string country;
        [SerializeField]
        private string country_code;
        #endregion

        #region Public Properties
        /// <summary>
        /// A string value that represents the requested neighbourhood name
        /// </summary>
        public string Neighbourhood
        {
            get
            {
                return neighbourhood;
            }
            set
            {
                neighbourhood = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested suburb name
        /// </summary>
        public string Suburb
        {
            get
            {
                return suburb;
            }
            set
            {
                suburb = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested municipality name
        /// </summary>
        public string Municipality
        {
            get
            {
                return municipality;
            }
            set
            {
                municipality = value;
            }
        } 

        /// <summary>
        /// A string value that represents the requested village name
        /// </summary>
        public string Village
        {
            get
            {
                return village;
            }
            set
            {
                village = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested town name
        /// </summary>
        public string Town
        {
            get
            {
                return town;
            }
            set
            {
                town = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested city name
        /// </summary>
        public string City
        {
            get
            {
                return city;
            }

            set
            {
                city = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested county name
        /// </summary>
        public string County
        {
            get
            {
                return county;
            }
            set
            {
                county = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested state name
        /// </summary>
        public string State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested district name
        /// </summary>
        public string District
        {
            get
            {
                return district;
            }
            set
            {
                district = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested locality postal code
        /// </summary>
        public string Postcode 
        {
            get
            {
                return postcode;
            }
            set
            {
                postcode = value;
            }
        }

        /// <summary>
        /// A string value that represents the requested country name
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
        /// A string value that represents the country code as defined by ISO 3166-1 standard.
        /// </summary>
        public string CountryCode
        {
            get
            {
                return country_code;
            }

            set
            {
                country_code = value;
            }
        }
        #endregion

        #region Public Methods       
        /// <summary>
        /// Concatenates the attributes of the AddressData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Address Data <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n" +
                   $"Neighbourhood name: {neighbourhood}\n" +
                   $"Suburb name: {suburb}\n" +
                   $"Municipality name: {municipality}\n" +
                   $"Village name: {village}\n" +
                   $"Town name: {town}\n" +
                   $"City name: {city}\n" +
                   $"County name: {county}\n" +
                   $"State name: {state}\n" +
                   $"District name: {district}\n" +
                   $"Postal code: {postcode}\n" +
                   $"Country name: {country}\n" +
                   $"Country code: {CountryCode}\n"
                   );
        }
        #endregion
    }
}
