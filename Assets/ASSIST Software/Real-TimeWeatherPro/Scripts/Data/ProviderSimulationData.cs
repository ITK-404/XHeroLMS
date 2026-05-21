//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;
using static RealTimeWeather.Managers.RealTimeWeatherManager;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class ProviderSimulationData
    {
        [SerializeField]
        private WeatherRequestMode _weatherRequestMode;
        [SerializeField]
        private string apiKey;
        [SerializeField]
        private bool _isForecastModeEnabled;

        public WeatherRequestMode WeatherRequestMode { get => _weatherRequestMode; set => _weatherRequestMode = value; }
        public string ApiKey { get => apiKey; set => apiKey = value; }
        public bool IsForecastModeEnabled { get => _isForecastModeEnabled; set => _isForecastModeEnabled = value; }
    }
}