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
    public class MetoceanWaterRequestData : ProviderSimulationData
    {
        [SerializeField]
        [Tooltip("It's a float value that represents a geographic coordinate that specifies the north–south position of a point on the Earth's surface.\nLatitude must be set according to ISO 6709.")]
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [SerializeField]
        [Tooltip("It's a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.\nLongitude must be set according to ISO 6709.")]
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [SerializeField]
        [Tooltip("The interval at which the data is requested")]
        [Range(1, 12)]
        private int _intervalInHours = 1;
        [SerializeField]
        [Tooltip("How many times to repeat on requesting the data in one point during a period of time")]
        [Range(0, 56)]
        private int _numberOfIntervals;
        [SerializeField]
        private bool _moreIntervals = false;

        public float Latitude { get => _latitude; set => _latitude = value; }
        public float Longitude { get => _longitude; set => _longitude = value; }
        public int IntervalInHours { get => _intervalInHours; set => _intervalInHours = value; }
        public int NumberOfIntervals { get => _numberOfIntervals; set => _numberOfIntervals = value; }
        public bool MoreIntervals { get => _moreIntervals; set => _moreIntervals = value; }
    }
}