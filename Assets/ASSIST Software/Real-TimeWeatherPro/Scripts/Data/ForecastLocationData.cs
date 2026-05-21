//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using RealTimeWeather.Classes;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class ForecastLocationData
    {
        public string forecastCity;
        public string forecastCountry;
        public float forecastLatitude;
        public float forecastLongitude;

        public ForecastLocationData() { }

        public ForecastLocationData(Localization location)
        {
            forecastCity = location.City;
            forecastCountry = location.Country;
            forecastLatitude = location.Latitude;
            forecastLongitude = location.Longitude;
        }
    }
}
