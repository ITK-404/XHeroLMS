// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Stormglass
{ 
    [CreateAssetMenu(fileName = "StormglassData", menuName = "Real-Time Weather/StormglassData", order = 1)]
    public class StormglassData : ScriptableObject
    {
        public StormglassWeatherData weatherData;
        public StormglassBioData bioData;
        public StormglassTideData tideData;
    }
}
