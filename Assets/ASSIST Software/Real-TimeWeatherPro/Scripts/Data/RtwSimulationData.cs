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
    public class RtwSimulationData : ProviderSimulationData
    {
        [SerializeField]
        private float _latitude;
        [SerializeField]
        private float _longitude;
        [SerializeField]
        private string _requestedCity;
        [SerializeField]
        private string _requestedState;
        [SerializeField]
        private string _requestedCountry;
        [SerializeField]
        private bool _isUSALocation;
        [SerializeField]
        private bool _isUsingCoordonates;

        public float Latitude { get => _latitude; set => _latitude = value; }
        public float Longitude { get => _longitude; set => _longitude = value; }
        public string RequestedCity { get => _requestedCity; set => _requestedCity = value; }
        public string RequestedState { get => _requestedState; set => _requestedState = value; }
        public string RequestedCountry { get => _requestedCountry; set => _requestedCountry = value; }
        public bool IsUSALocation { get => _isUSALocation; set => _isUSALocation = value; }
        public bool IsUsingCoordonates { get => _isUsingCoordonates; set => _isUsingCoordonates = value; }
    }
}