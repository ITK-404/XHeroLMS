//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.WeatherStation
{
    public class WeatherStationData
    {
        #region Private Variables
        private string _ICAO;
        private string _name;
        private int _elevation;
        private string _country;
        private string _region;
        private double _lat;
        private double _lon;
        #endregion

        #region Public Properties
        public string ICAO { get { return _ICAO; } }
        public string Name { get { return _name; } }
        public string Country { get { return _country; } }
        public string Region { get { return _region; } }
        public double Lat { get { return _lat; } }
        public double Lon { get { return _lon; } }
        public int Elevation { get { return _elevation; } }
        #endregion

        #region Public Constructors
        public WeatherStationData()
        {
            _ICAO = "";
            _name = "";
            _elevation = 0;
            _country = "";
            _region = "";
            _lat = 0;
            _lon = 0;
        }

        public WeatherStationData(string iCAO, string name, int elevation, string country, string region, double lat, double lon)
        {
            _ICAO = iCAO;
            _name = name;
            _elevation = elevation;
            _country = country;
            _region = region;
            _lat = lat;
            _lon = lon;
        }
        #endregion
    }
}
