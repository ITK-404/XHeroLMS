//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Managers;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace RealTimeWeather.Data
{
    public class RandomForecastPopupData
    {
        public List<PresetData> Presets;
        public int ForecastLength;
        public int MinPresetLength;
        public int MaxPresetLength;
        public bool IsRandomProbabilityOn;

        public RandomForecastPopupData(RealTimeWeatherManager realTimeWeatherManager)
        {
            Presets = new List<PresetData>
        {
            new PresetData(realTimeWeatherManager.WeatherPresets.DefaultPreset.forecastCustomPresets[0], 100)
        };
            ForecastLength = 1;
            MinPresetLength = 1;
            MaxPresetLength = 24;
            IsRandomProbabilityOn = false;
        }
    }

    public struct PresetData
    {
        public ForecastWeatherData ForecastWeatherData;
        public int ForecastProbability;

        public PresetData(ForecastWeatherData forecastWeatherData, int forecastProbability)
        {
            ForecastWeatherData = forecastWeatherData;
            ForecastProbability = forecastProbability;
        }
    }
}
#endif