// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    /// <summary>
    /// Information about the API request.
    /// </summary>
    [Serializable]
    public class StormglassRequestData
    {
        public int cost;
        public int dailyQuota;
        public DateTime end;
        public float lat;
        public float lng;
        public int requestCount;
        public DateTime start;
    }
}
