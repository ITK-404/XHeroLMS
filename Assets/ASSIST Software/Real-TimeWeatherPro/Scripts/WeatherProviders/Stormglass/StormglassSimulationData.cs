//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.WeatherProvider.Stormglass;
using System;
using UnityEngine;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class StormglassSimulationData : ProviderSimulationData
    {
        [SerializeField]
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [SerializeField]
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [SerializeField]
        private bool _requestWeatherData = true;
        [SerializeField]
        private bool _requestBioData;
        [SerializeField]
        private bool _requestTideData;
        [SerializeField]
        private bool _saveDataToFile;
        [SerializeField]
        private bool _saveDataToSO;
        [SerializeField]
        private string _fileName = "StormglassData";
        [SerializeField]
        private StormglassData _stormglassData;

        public float Latitude { get => _latitude; set => _latitude = value; }
        public float Longitude { get => _longitude; set => _longitude = value; }
        public bool RequestWeatherData { get => _requestWeatherData; set => _requestWeatherData = value; }
        public bool RequestBioData { get => _requestBioData; set => _requestBioData = value; }
        public bool RequestTideData { get => _requestTideData; set => _requestTideData = value; }
        public bool SaveDataToFile { get => _saveDataToFile; set => _saveDataToFile = value; }
        public bool SaveDataToSO { get => _saveDataToSO; set => _saveDataToSO = value; }
        public string FileName { get => _fileName; set => _fileName = value; }
        public StormglassData StormglassData { get => _stormglassData; set => _stormglassData = value; }
    }
}