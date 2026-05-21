// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    /// <summary>
    /// Data about tides
    /// </summary>
    [Serializable]
    public class StormglassTideData
    {
        public List<StormglassTideParams> data;
        public StormglassRequestData meta;

    }

    [Serializable]
    public class StormglassTideParams
    {
        public string time; // Timestamp in UTC

        public float height; // Height in meters
        public string type; // Type of extreme. Either low or high
    }
}