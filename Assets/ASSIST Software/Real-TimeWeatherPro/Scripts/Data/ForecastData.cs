//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using System.Collections.Generic;
using UnityEngine;
using static RealTimeWeather.Managers.RealTimeWeatherManager;

namespace RealTimeWeather.Data
{
    [CreateAssetMenu(fileName = "Forecast Data", menuName = "Real-Time Weather/Forecast/Data", order = 0)]
    public class ForecastData : ScriptableObject
    {
        public bool LoopSimulation;
        public string SimulationName;
        public List<IntervalState> IntervalStates;
        public List<string> TimelapseDayNames;
        public List<string> IntervalPresetName;
        public List<string> IntervalName;
        public IntervalLerpSpeed LerpStrength;
        public ForecastWeatherData DefaultWeatherData;
        public List<ForecastWeatherData> forecastData;
        public ForecastDataType ForecastType;
        public int simulationSpeed;
        public TomorrowSimulationData TomorrowSimulationData;
        public OpenWeatherData OpenWeatherSimulationData;
        public RtwSimulationData RtwSimulationData;
        public TomorrowWaterSimulationData TomorrowWaterSimulationData;
        public StormglassSimulationData StormglassSimulationData;
        public MetoceanWaterRequestData MetoceanWaterSimulationData;
        public int DaysOfSimulation = 0;
        public SimulationType WeatherSimulationType;
        public WeatherRequestMode WeatherRequestMode;
        public WaterRequestMode WaterRequestMode;
        public bool IsWeatherSimulationActive;
        public bool IsWaterSimulationActive;
        public bool IsUsaLocation;

        public void SetDefaultWeatherData(ForecastWeatherData weatherData)
        {
            DefaultWeatherData = new ForecastWeatherData(weatherData);
        }

        public void SetForecastData(List<ForecastWeatherData> weatherData)
        {
            forecastData = new List<ForecastWeatherData>();
            for (int i = 0; i < weatherData.Count; i++)
            {
                forecastData.Add(new ForecastWeatherData());
                forecastData[i] = new ForecastWeatherData(weatherData[i]);
                forecastData[i].forecastTimeZone = Utilities.GetTimeZone(forecastData[i].forecastLocation.forecastLatitude, forecastData[i].forecastLocation.forecastLongitude);
            }
        }

        public ForecastWeatherData GetDefaultWeatherData()
        {
            return DefaultWeatherData;
        }

        public List<WeatherData> GetForecastData(bool useCustomDateTime = false)
        {
            List<WeatherData> weatherData = new List<WeatherData>();
            for (int i = 0; i < forecastData.Count; i++)
            {
                weatherData.Add(Utilities.ForecastDataToWeatherData(forecastData[i], useCustomDateTime));
            }
            return weatherData;
        }
    }

    public enum ForecastDataType
    {
        Hourly,
        Daily
    }

    public enum IntervalLerpSpeed
    {
        Slow = 0,
        Medium = 50,
        Fast = 75
    }
}