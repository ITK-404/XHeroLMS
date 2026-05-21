// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    /// <summary>
    /// All the available stormglass data sources.
    /// </summary>
    [Serializable]
    public class StormglassProviders
    {
        public float icon; // Germany's National Meteorological Service, the Deutscher Wetterdienst
        public float noaa; // The National Oceanic and Atmospheric Administration
        public float meteo; // French National Meteorological service
        public float dwd; // Germany's National Meteorological Service, the Deutscher Wetterdienst
        public float meto; // United Kingdom's national weather service, The UK MetOffice
        public float fcoo; // Danish Defence Centre for Operational Oceanography
        public float fmi; // The Finnish Meteorological Institution
        public float yr; // Norwegian Meteorological Institute and NRK
        public float smhi; // Swedish Meteorological and Hydrological Institute
        public float sg; // Stormglass Ai

        public float mercator; // for bio attributes
    }
}