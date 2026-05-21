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
    public class TomorrowWaterSimulationData : ProviderSimulationData
    {
        [SerializeField]
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [SerializeField]
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [SerializeField]
        private bool _includeExtraPackage;

        public float Latitude { get => _latitude; set => _latitude = value; }
        public float Longitude { get => _longitude; set => _longitude = value; }
        public bool IncludeExtraPackage { get => _includeExtraPackage; set => _includeExtraPackage = value; }
    }
}