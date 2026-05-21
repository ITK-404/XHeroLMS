//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class TomorrowSimulationData : ProviderSimulationData
    {
        [SerializeField]
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [SerializeField]
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [SerializeField]
        private string _city;
        [SerializeField]
        private string _country;
        [SerializeField]
        private bool _includeExtraPackage;
        [SerializeField]
        private IntervalLerpSpeed _intervalLerpSpeed;
        [SerializeField]
        private int _simulationSpeed;
        [SerializeField]
        private bool _isUsingCoordonates;

        public float Latitude { get => _latitude; set => _latitude = value; }
        public float Longitude { get => _longitude; set => _longitude = value; }
        public string City { get => _city; set => _city = value; }
        public string Country { get => _country; set => _country = value; }
        public bool IncludeExtraPackage { get => _includeExtraPackage; set => _includeExtraPackage = value; }
        public IntervalLerpSpeed IntervalLerpSpeed { get => _intervalLerpSpeed; set => _intervalLerpSpeed = value; }
        public int SimulationSpeed { get => _simulationSpeed; set => _simulationSpeed = value; }
        public bool IsUsingCoordonates { get => _isUsingCoordonates; set => _isUsingCoordonates = value; }
    }
}