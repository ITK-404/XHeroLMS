//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Managers;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.WeatherStation
{
    public class WeatherStations
    {
        #region Private Constants
        private const int kNumberOfDist = 5;
        private const float kPlanetDiameter = 12745.6f;
        private const string kStationDataPath = "/Resources/WeatherStationData.txt";
        #endregion

        #region Private Variables
        private Dictionary<string, WeatherStationData> _weatherStations;

        private double _lat;
        private double _lon;
        #endregion

        #region Public Methods
        /// <summary>
        /// Finds the closest weather station from the specified coordinates 
        /// </summary>
        /// <param name="targetLat">The latitude of the target position</param>
        /// <param name="targetLon">The longitude of the target position</param>
        /// <returns>A list with the closest weather stations</returns>
        public void ClosestWeatherStations(double targetLat, double targetLon, out WeatherStationData[] stations, out double[] distances)
        {
            if (_weatherStations == null)
            {
                ReadWeatherStationList();
            }
            distances = new double[kNumberOfDist];
            stations = new WeatherStationData[kNumberOfDist];
            _lat = targetLat;
            _lon = targetLon;

            double[] min = new double[kNumberOfDist];
            for (int i = 0; i < kNumberOfDist; i++)
            {
                min[i] = double.MaxValue;
                stations[i] = new WeatherStationData();
            }

            foreach (KeyValuePair<string, WeatherStationData> entry in _weatherStations)
            {
                double dist = DistanceTo(entry.Value.Lat, entry.Value.Lon);
                for (int i = 0; i < kNumberOfDist; i++)
                {
                    if (dist < min[i])
                    {
                        stations[i] = entry.Value;
                        distances[i] = dist;
                        min[i] = dist;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Load the weather stations from a file
        /// </summary>
        public void ReadWeatherStationList()
        {
            _weatherStations = new Dictionary<string, WeatherStationData>();
            StreamReader reader = new StreamReader(RealTimeWeatherManager.instance.RelativePath + kStationDataPath);
            string line = reader.ReadLine();

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                string[] str = line.Split(' ');
                WeatherStationData stationData = new WeatherStationData(str[0], str[1], int.Parse(str[2]), str[3], str[4], double.Parse(str[5]), double.Parse(str[6]));
                _weatherStations.Add(str[0], stationData);
            }
            reader.Close();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Calculates the distance between two coordinates on a sphere
        /// </summary>
        /// <param name="lat">The latitude of a weather station</param>
        /// <param name="lon">The longitude of a weather station</param>
        /// <returns>The distance between two coordinates on a sphere</returns>
        private double DistanceTo(double lat, double lon)
        {
            return kPlanetDiameter * Mathf.Asin(Mathf.Sqrt((float)(SinSQ(lat - _lat) + Mathf.Cos((float)(Mathf.Deg2Rad * _lat)) * Mathf.Cos((float)(Mathf.Deg2Rad * lat)) * SinSQ(lon - _lon))));
        }

        /// <summary>
        /// Calculates the sin²(diff / 2)
        /// </summary>
        /// <param name="diff"></param>
        /// <returns></returns>
        private float SinSQ(double diff)
        {
            return Mathf.Pow(Mathf.Sin((float)(Mathf.Deg2Rad * diff / 2d)), 2);
        }
        #endregion
    }
}
