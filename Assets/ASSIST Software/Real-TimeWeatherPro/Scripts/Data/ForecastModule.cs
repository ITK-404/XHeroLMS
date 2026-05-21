//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;
using UnityEngine;
using RealTimeWeather.Classes;
using RealTimeWeather.UI;
using RRealTimeWeather.Data;

namespace RealTimeWeather.Data
{
    public class ForecastModule : MonoBehaviour
    {
        #region Const Members
        private const string kForecastSOAssetPathStr = "Forecast/ForecastData";
        private const string kForecastSimulationsPathStr = "Forecast/Simulations/";
        #endregion

        #region Events
        public static event Action<WeatherData, WeatherData, double> OnForecastProgressModuleTick;
        public static event Action<WeatherData> OnForecastModuleTick;
        public static Action OnPauseResumeForecast;
        #endregion

        #region Private Variables
        private TimeTickSystem timer;
        private TimelapseProgress timelapseProgress;
        #endregion

        #region Public Variables
        public ForecastData ForecastDataSO;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            timer = gameObject.AddComponent<TimeTickSystem>();
            OnForecastProgressModuleTick += UpdateForSimpleForecastModuleTick;
        }

        private void OnDestroy()
        {
            OnForecastProgressModuleTick -= UpdateForSimpleForecastModuleTick;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the current time progress and info about the current forecast simulation
        /// </summary>
        /// <returns>A serializable class that stores the current simulation name and progress</returns>
        public ForecastProgressData SaveForecastData()
        {
            return new ForecastProgressData(ForecastDataSO.SimulationName, timelapseProgress.TimelapseProgressDate, timelapseProgress.CurrentIntervalIndex);
        }

        /// <summary>
        /// Loads a user-given forecast simulation based on it's name and sets the timelapse progress data
        /// </summary>
        /// <param name="progressData">A serializable class that stores the simulation name and progress</param>
        public void LoadForecastData(ForecastProgressData progressData)
        {
            ForecastDataSO = Resources.Load<ForecastData>($"{kForecastSimulationsPathStr}{progressData.Name}");

            if (ForecastDataSO == null)
            {
                return;
            }

            if (timelapseProgress == null)
            {
                timelapseProgress = new TimelapseProgress(ForecastDataSO, timer, OnForecastProgressModuleTick);
                timelapseProgress.InitializeTimelapseSettings();
                OnPauseResumeForecast += timelapseProgress.ChangeSimulationTimerState;
            }

            timelapseProgress.TimelapseProgressDate = progressData.ProgressTime;
            timelapseProgress.CurrentIntervalIndex = progressData.IntervalIndex;

            if (!timelapseProgress.IsTimelapseActive)
            {
                timelapseProgress.ChangeSimulationTimerState();
            }

            WeatherDataUI.ChangeToPauseButtonText?.Invoke();
        }

        /// <summary>
        /// Receives forecast data from Weather Providers (e.g. Tomorrow.io, OpenWeatherMap) and initializes the forecast timelapse
        /// </summary>
        /// <param name="forecastData">The forecast data received as a List of WeatherData</param>
        /// <param name="hourlyForecast">Specifies if it's hourly forecast or daily forecast</param>
        /// <param name="simulationSpeed">The simulation speed (1h => time) that will be used</param>
        /// <param name="lerpStrength">The power of interpolation</param>
        /// <param name="loopForecast">Repeat simulation when the last forecast interval is over</param>
        public void OnProvidersDataReceived(List<WeatherData> forecastData, bool hourlyForecast, int simulationSpeed, IntervalLerpSpeed lerpStrength, bool loopForecast)
        {
            if (ForecastDataSO == null)
            {
                ForecastDataSO = Resources.Load<ForecastData>(kForecastSOAssetPathStr);
            }

            if (ForecastDataSO != null)
            {
                ForecastDataSO.SetForecastData(Utilities.ConvertWeatherDataToForecastData(forecastData));
                ForecastDataSO.ForecastType = hourlyForecast ? ForecastDataType.Hourly : ForecastDataType.Daily;
                ForecastDataSO.simulationSpeed = simulationSpeed;
                ForecastDataSO.LerpStrength = lerpStrength;
                ForecastDataSO.LoopSimulation = loopForecast;

                timelapseProgress = new TimelapseProgress(ForecastDataSO, timer, OnForecastProgressModuleTick, true);
                timelapseProgress.InitializeTimelapseSettings();
                OnPauseResumeForecast += timelapseProgress.ChangeSimulationTimerState;
            }
        }

        /// <summary>
        /// Receives user data and initializes the forecast timelapse
        /// </summary>
        /// <param name="forecastData">User-given forecast data</param>
        public void OnUserDataReceived(ForecastData forecastData)
        {
            ForecastDataSO = forecastData;

            if (ForecastDataSO != null)
            {
                timelapseProgress = new TimelapseProgress(ForecastDataSO, timer, OnForecastProgressModuleTick, true);
                timelapseProgress.InitializeTimelapseSettings();
                OnPauseResumeForecast += timelapseProgress.ChangeSimulationTimerState;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// After receiving a set of complete data of the forecast progress, this method triggersan event with only current weather data (suitable for UI)
        /// </summary>
        /// <param name="currentWeather">The current weather that is returned (interpolated one)</param>
        /// <param name="nextWeather">The next weather that should follow</param>
        /// <param name="progressOfWeather">The progress of the weather transition represented in percentages</param>
        private void UpdateForSimpleForecastModuleTick(WeatherData currentWeather, WeatherData nextWeather, double progressOfWeather)
        {
            OnForecastModuleTick?.Invoke(currentWeather);
        }
        #endregion
    }
}