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
    /// Data about maritime weather
    /// </summary>
    [Serializable]
    public class StormglassWeatherData
    {
        public List<StormglassWeatherParams> hours;
        public StormglassRequestData meta;
    }

    [Serializable]
    public class StormglassWeatherParams
    {
        public string time; // Timestamp in UTC

        public StormglassProviders airTemperature; // Air temperature in degrees celsius
        public StormglassProviders pressure; // Air pressure in hPa
        public StormglassProviders cloudCover; // Total cloud coverage in percent
        public StormglassProviders currentDirection; // Direction of current. 0° indicates current coming from north
        public StormglassProviders currentSpeed; // Speed of current in meters per second
        public StormglassProviders gust; // Wind gust in meters per second
        public StormglassProviders humidity; // Relative humidity in percent
        public StormglassProviders precipitation; // Mean precipitation in kg/m²/h = mm/h
        public StormglassProviders snowDepth; // Depth of snow in meters
        public StormglassProviders visibility; // Horizontal visibility in km

        public StormglassProviders iceCover; // Proportion, 0-1
        public StormglassProviders seaLevel; // Sea level relative to MSL
        public StormglassProviders swellDirection; // Direction of swell waves. 0° indicates swell coming from north
        public StormglassProviders secondarySwellDirection; // Direction of secondary swell waves. 0° indicates swell coming from north
        public StormglassProviders swellHeight; // Height of swell waves in meters
        public StormglassProviders secondarySwellHeight; // Height of secondary swell waves in meters
        public StormglassProviders swellPeriod; // Period of swell waves in seconds
        public StormglassProviders secondarySwellPeriod; // Period of secondary swell waves in seconds
        public StormglassProviders waterTemperature; // Water temperature in degrees celsius
        public StormglassProviders waveDirection; // Direction of combined wind and swell waves. 0° indicates waves coming from north
        public StormglassProviders waveHeight; // Significant Height of combined wind and swell waves in meters
        public StormglassProviders wavePeriod; // Period of combined wind and swell waves in seconds
        public StormglassProviders windWaveDirection; // Direction of wind waves. 0° indicates waves coming from north
        public StormglassProviders windWaveHeight; // Height of wind waves in meters
        public StormglassProviders windWavePeriod; // Period of wind waves in seconds
        public StormglassProviders windDirection; // Direction of wind at 10m above sea level. 0° indicates wind coming from north
        public StormglassProviders windSpeed; // Speed of wind at 10m above sea level in meters per second.
    }
}