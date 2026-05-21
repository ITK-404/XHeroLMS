//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using RealTimeWeather.Classes;
using UnityEngine;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class ForecastWindData
    {
        public Vector2 forecastDirection;
        public float forecastSpeed;

        public ForecastWindData() { }

        public ForecastWindData(Wind wind)
        {
            forecastDirection = wind.Direction;
            forecastSpeed = wind.Speed;
        }
    }
}
