// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//
using System;
using System.Collections.Generic;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using RealTimeWeather.Data;
using static RealTimeWeather.Managers.RealTimeWeatherManager;
using UnityEngine;
using RealTimeWeather.Managers;

namespace RealTimeWeather.Simulation
{
    [Serializable]
    public class Timelapse
    {
        #region Private Constants
        private const float kDefaultTemperature = 25;
        private const float kDefaultPressure = 1013;
        private const float kDefaultPrecipitation = 0;
        private const float kDefaultHumidity = 50;
        private const float kDefaultDewpoint = 22;
        private const float kDefaultVisibility = 10;
        private const float kDefaultIndexUV = 1;
        private const WeatherState kDefaultWeatherState = WeatherState.PartlyCloudy;
        #endregion

        #region Public Variables
        public IntervalLerpSpeed lerpSpeed;
        public string timelapseName = "New Simulation";
        public string timelapseDate;
        public float timelapseLatitude;
        public float timelapseLongitude;
        public int simulationSpeed = 5;
        public bool loopSim;
        public string city;
        public string country;

        public ForecastWeatherData defaultIntervalData = RealTimeWeatherManager.instance.DefaultForecastData;

        public List<TimelapseDay> timelapseDays = new List<TimelapseDay>();
        public TomorrowSimulationData tomorrowSimulationData = new TomorrowSimulationData();
        public OpenWeatherData openWeatherSimulationData= new OpenWeatherData();
        public SimulationType WeatherSimulationType;
        public WeatherRequestMode LastChosenRequestMode;
        #endregion

        #region Public Constructors
        public Timelapse()
        {
            AddNewDefaultDay();
        }

        public Timelapse(Timelapse timelapse)
        {
            lerpSpeed = timelapse.lerpSpeed;
            timelapseName = timelapse.timelapseName;
            timelapseDate = timelapse.timelapseDate;
            timelapseLatitude = timelapse.timelapseLatitude;
            timelapseLongitude = timelapse.timelapseLongitude;
            simulationSpeed = timelapse.simulationSpeed;
            loopSim = timelapse.loopSim;
            defaultIntervalData =new ForecastWeatherData(RealTimeWeatherManager.instance.DefaultForecastData);
            timelapseDays = new List<TimelapseDay>();
            for (int i = 0; i < timelapse.timelapseDays.Count; i++)
            {
                timelapseDays.Add(new TimelapseDay(timelapse.timelapseDays[i]));
            }
            tomorrowSimulationData = timelapse.tomorrowSimulationData;
            LastChosenRequestMode = timelapse.LastChosenRequestMode;
            WeatherSimulationType = timelapse.WeatherSimulationType;
        }
        #endregion

        #region Public Methods
        public void AddNewDefaultDay()
        {
            timelapseDays.Add(new TimelapseDay(RealTimeWeatherManager.instance.DefaultForecastData));
        }

        public void RemoveDay(int index)
        {
            timelapseDays.RemoveAt(index);
        }

        public void DuplicateDay(int index)
        {
            timelapseDays.Insert(index + 1, new TimelapseDay(timelapseDays[index], RealTimeWeatherManager.instance.DefaultForecastData));
        }

        public void AddDay(TimelapseDay day)
        {
            timelapseDays.Add(day);
        }
        #endregion
    }
}
