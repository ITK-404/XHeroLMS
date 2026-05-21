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
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using RealTimeWeather;
using RealTimeWeather.Data;

namespace RRealTimeWeather.Data
{
    public enum LerpLoopBounds
    {
        Low = 50,
        Medium = 75,
        High = 87
    }

    public class TimelapseProgress
    {
        #region Const Members
        private const float kOneMinute = 1.0f;
        private const float kThirtyFrames = 30;
        private const float kHundredPercent = 100.0f;
        #endregion

        #region Private Variables
        Dictionary<IntervalLerpSpeed, float> lerpStrengthFactor = new Dictionary<IntervalLerpSpeed, float>() {     
                                                                { IntervalLerpSpeed.Slow, 2.0f }, 
                                                                { IntervalLerpSpeed.Medium, 4.0f }, 
                                                                { IntervalLerpSpeed.Fast, 8.0f } 
        };

        Dictionary<IntervalLerpSpeed, LerpLoopBounds> lerpLoopBounds = new Dictionary<IntervalLerpSpeed, LerpLoopBounds>() {
                                                                { IntervalLerpSpeed.Slow, LerpLoopBounds.Low },
                                                                { IntervalLerpSpeed.Medium, LerpLoopBounds.Medium },
                                                                { IntervalLerpSpeed.Fast, LerpLoopBounds.High }
        };

        private Action<WeatherData, WeatherData, double> onForecastModuleTick;
        private List<WeatherData> forecastData;
        private ForecastData forecastDataContainer;
        private TimeTickSystem timeTickSystem;

        private DateTime timelapseStartDate;
        private DateTime timelapseProgressDate;
        private float timePerTick;
        private int currentIntervalIndex = 0;
        private int loopCount;
        private bool loop;
        #endregion

        #region Public Properties
        public int SimulationTotalTime { get; private set; }
        public int ForecastHoursTotalTime { get; private set; }
        public bool IsTimelapseActive { get; private set; } = false;
        public double ForecastPassedTime { get { return (timelapseProgressDate - timelapseStartDate).TotalHours; } }
        public double InGamePassedTime { get { return ForecastPassedTime * forecastDataContainer.simulationSpeed; } }
        public DateTime TimelapseProgressDate { get { return timelapseProgressDate; } set { timelapseProgressDate = value; } }
        public int CurrentIntervalIndex { get { return currentIntervalIndex; } set { currentIntervalIndex = value; } }
        #endregion

        #region Constructors
        public TimelapseProgress(ForecastData forecastDataContainer, TimeTickSystem timeTickSystem, Action<WeatherData, WeatherData, double> onForecastModuleTick,bool useCustomDateTime = false)
        {
            this.forecastDataContainer = forecastDataContainer;
            forecastData = forecastDataContainer.GetForecastData(useCustomDateTime);

            this.timeTickSystem = timeTickSystem;
            this.onForecastModuleTick = onForecastModuleTick;
            loop = forecastDataContainer.LoopSimulation;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize all necessary settings (start date time of forecast, total simulation time, tick system settings)
        /// </summary>
        public void InitializeTimelapseSettings()
        {
            if (forecastData.Count == 0)
            {
                return;
            }

            if(forecastData.Count == 1)
            {
                onForecastModuleTick?.Invoke(forecastData[0], null, 0.0f);
                return;
            }

            timelapseStartDate = forecastData[0].DateTime; //we store the start date time of this forecast
            timelapseProgressDate = forecastData[0].DateTime; //we will update a datetime according with the forecast
            if (forecastData.Count > 1)
            {
                //That is the entire time of a simulation (the time that passes in-game)
                CalculateSimulationTime(forecastData, forecastDataContainer.simulationSpeed);
            }

            timePerTick = (kOneMinute / forecastDataContainer.simulationSpeed) * 2;
            timeTickSystem.OnTick += OnTickUpdate;

            //Start the simulation with the first tick
            ChangeSimulationTimerState();
            WeatherData weatherData = ConstructClone(forecastData[currentIntervalIndex]);
            onForecastModuleTick?.Invoke(weatherData, ReturnTheNextIntervalWeather(), 0.0f);
        }

        /// <summary>
        /// Pause or resume the simulation depending on it's current state.
        /// Called whenever the player wants to pause/resume the timer or when the forecast is over.
        /// </summary>
        public void ChangeSimulationTimerState()
        {
            if (currentIntervalIndex >= forecastData.Count - 1)
            {
                if (IsTimelapseActive)
                {
                    timeTickSystem.StopTimer();
                    IsTimelapseActive = false;
                }

                return;
            }

            if (!IsTimelapseActive)
            {
                //Resume simulation
                var timeTickUpdate = kOneMinute / kThirtyFrames; //we segment 1hr time in small intervals, so we have more updates per interval, and also apply interpolation
                timeTickSystem.StartTimer(timeTickUpdate);
                IsTimelapseActive = true;
            }
            else
            {
                //Pause simulation
                timeTickSystem.StopTimer();
                IsTimelapseActive = false;
            }
        }
        #endregion

        #region Private Methods
        private void OnTickUpdate(object sender, OnTickEventArgs tickEvent)
        {
            if (tickEvent.tick == 0)
            {
                //we directly apply the start of the first interval
                onForecastModuleTick?.Invoke(forecastData[currentIntervalIndex], ReturnTheNextIntervalWeather(), 0.0f);
                return;
            }

            //We add some time to progress through forecast (based on how fast the tick is triggered)
            timelapseProgressDate = timelapseProgressDate.AddMinutes(timePerTick);

            //We increase the current interval index when our <timelapseProgressDate> passes the datetime from the next interval datetime
            if (timelapseProgressDate >= forecastData[currentIntervalIndex + 1].DateTime)
            {
                currentIntervalIndex++;
            }

            if (currentIntervalIndex == forecastData.Count - 1)
            {
                //That's the last day of the forecast. 
                //If we are here, it means that we can loop back again or stop updating the weather
                if (loop)
                {
                    loopCount++;
                    timelapseProgressDate = timelapseStartDate;
                    currentIntervalIndex = 0;
                }
                else
                {
                    onForecastModuleTick?.Invoke(forecastData[currentIntervalIndex], null, kHundredPercent);
                    ChangeSimulationTimerState();
                    return;
                }
            }

            WeatherData weatherData = ConstructClone(forecastData[currentIntervalIndex]);
            weatherData.DateTime = timelapseProgressDate;
            WeatherData nextWeatherData = ConstructClone(ReturnTheNextIntervalWeather());
            double progressInterpolation = CalculateAndApplyOptionalInterpolation(weatherData);
            progressInterpolation = LoopFunctionalityForWeatherData(weatherData, nextWeatherData, progressInterpolation);
            
            onForecastModuleTick?.Invoke(weatherData, nextWeatherData, progressInterpolation);
        }

        /// <summary>
        /// Check if the last two end-points of the last interval and the start-point of the first interval are different
        /// </summary>
        /// <param name="currentInterval">The start-point of the last interval</param>
        /// <param name="lastInterval">The end-point of the last interval</param>
        /// <param name="firstInterval">The start-point of the first interval</param>
        /// <returns>Specifies if all the ends are different between them</returns>
        private bool CheckLoopEdgeIntervals(WeatherState currentInterval, WeatherState lastInterval, WeatherState firstInterval)
        {
            bool lastIntervalsPass = currentInterval != lastInterval;
            bool firstAndLastPass = lastInterval != firstInterval;
            bool lastAndPenPass = firstInterval != currentInterval;

            return lastIntervalsPass && firstAndLastPass && lastAndPenPass;
        }

        private double LoopFunctionalityForWeatherData(WeatherData currentWeatherData, WeatherData nextWeatherData, double progressInterpolation)
        {
            if (loop)
            {
                //For a forecast of 6 hours, at every loop, a extra amount of 6 hours will be added to the datetime
                //The weather data will be the same, reused at every loop
                currentWeatherData.DateTime = currentWeatherData.DateTime.AddMinutes(loopCount * (ForecastHoursTotalTime * 60.0f));

                //We check if the next weather interval is the last interval
                bool nextIntervalIsTheLast = (currentIntervalIndex + 1) == forecastData.Count - 1;

                //We check how much time remained from the transition of the current interval to the next weather interval (the last one)
                TimeSpan timeLeftFromLastInterval = forecastData[currentIntervalIndex + 1].DateTime - timelapseProgressDate;

                //We check how long is the last interval (eg. 2 hours, 5 hours, max 24 hours, etc.)
                TimeSpan lastIntervalTime = new TimeSpan((forecastData[currentIntervalIndex + 1].DateTime - forecastData[currentIntervalIndex].DateTime).Ticks);

                //We calculate how much from the transition of the current interval to the last represents the lerp time
                //Ex. for Lerp strength set to high, which applies when the transition is at 75%, the last 25% is the lerp time, so it the total transition takes 2 hours, then the lerp time would be 30 mins
                var remainedPercentageForLoop = (100 - (int)forecastDataContainer.LerpStrength) / 100.0f;

                //We want to calculate half the lerp loop time, so if the lerp time is 30 mins, then half of it would be 15 mins
                TimeSpan lerpToFirstIntervalTransitionTime = new TimeSpan((long)(lastIntervalTime.Ticks / 2.0f * remainedPercentageForLoop));

                if (nextIntervalIsTheLast && timeLeftFromLastInterval < lerpToFirstIntervalTransitionTime)
                {
                    //We calculate the progress of the transition lerp from 0 to 100%
                    //Let's that the transition of two intervals is 1 hour, when have lerp strength set to High, it means lerp time is 15 mins, as the High Lerp applies from 75%
                    //So, from 1 hr, if passes 45 min, then the lerp should start and the progress to first interval is 0%
                    //In case that it passes 53.5 mins from 1 hr, then the progress to first interval is 50%
                    double progressToFirstInterval = (1.0d - (timeLeftFromLastInterval.TotalMilliseconds / lerpToFirstIntervalTransitionTime.TotalMilliseconds)) * 100.0d;

                    //We apply the interpolation between current interval and last interval during the lerp transition time
                    ApplyInterpolation(currentWeatherData, forecastData[0], (float)progressToFirstInterval);

                    if (forecastData[currentIntervalIndex].WeatherState == forecastData[currentIntervalIndex + 1].WeatherState)
                    {
                        //In case that the current interval is the same, as example, Clear and Clear, then it means that no visual effects will appear, as the states are the same, but the progress interpolation still increases
                        //So because the progress interpolation increases, and when the lerp strength is set to High, as example, it means that when the transition lerp time starts, the progress interpolation will be 75%
                        //We need to make a transition between last inteval and first interval, but we could no apply the 75% progress, so for the last 25%, every 1 procent will be 4 procents, so at 75%, will be 0%, at 76%, will be 4%
                        progressInterpolation = (progressInterpolation - (float)lerpLoopBounds[forecastDataContainer.LerpStrength] / 100.0f) * lerpStrengthFactor[forecastDataContainer.LerpStrength];
                    }
                    else if (forecastData[currentIntervalIndex].WeatherState == forecastData[0].WeatherState)
                    {
                        //If the above "if" statement doesn't apply, and the last two intervals are different, then it means that the clouds will suffer visual changes through the lerp time transition
                        //So, during the lerp time, we want that the transition to happen between last-end interval and the first one
                        currentWeatherData.WeatherState = forecastData[currentIntervalIndex + 1].WeatherState;
                    }
                    else if (CheckLoopEdgeIntervals(forecastData[currentIntervalIndex].WeatherState, forecastData[currentIntervalIndex + 1].WeatherState, forecastData[0].WeatherState))
                    {
                        if (progressToFirstInterval <= 50.0f)
                        {
                            //In case progress of the lerp transition time is less than 50%, then we rollback the current progress of the transition to the start
                            nextWeatherData.WeatherState = forecastData[currentIntervalIndex + 1].WeatherState;
                            currentWeatherData.WeatherState = forecastData[currentIntervalIndex].WeatherState;
                            progressInterpolation = (0.75f - progressInterpolation) * 2.0f;
                        }
                        else
                        {
                            //In the rest time of the transition lerp time, we do a very fast transition between the current interval and the first interval
                            progressInterpolation = (progressInterpolation - 0.75f) * 4.0f;
                            currentWeatherData.WeatherState = forecastData[currentIntervalIndex].WeatherState;
                        }
                    }
                }
            }

            return progressInterpolation;
        }


        /// <summary>
        /// Verifies if the weather data at that moment requires any interpolation computations
        /// </summary>
        /// <param name="weatherData">The weather data selected for display/simulate</param>
        private double CalculateAndApplyOptionalInterpolation(WeatherData weatherData)
        {
            //We calculate the percentage of every point of progress value. There are three methods of interpolation:
            //1. For "LOW": every point of progress represents 1% of the difference value, as the interpolation starts from 0%
            //2. For "MEDIUM": every point of progress represents 2% of the difference value, as the interpolation starts from 50% (2% * 50% -> total value)
            //3. For "HIGH": every point of progress represents 4% of the difference value, as the interpolation starts from 75% (4% * 25% -> total value)
            var interpolationValuePerPoint = kHundredPercent / (kHundredPercent - (int)forecastDataContainer.LerpStrength);

            TimeSpan currentTimeInterval = forecastData[currentIntervalIndex + 1].DateTime.Subtract(forecastData[currentIntervalIndex].DateTime);
            TimeSpan elapsedTimeInterval = timelapseProgressDate.Subtract(forecastData[currentIntervalIndex].DateTime);

            float progressThroughInterval = (elapsedTimeInterval.Ticks * kHundredPercent) / currentTimeInterval.Ticks;
            bool applyInterpolation = progressThroughInterval >= (int)forecastDataContainer.LerpStrength;

            double interpolationDifference = 0.0f;
            if (applyInterpolation)
            {
                if (forecastDataContainer.LerpStrength == IntervalLerpSpeed.Slow)
                {
                    interpolationDifference = progressThroughInterval * interpolationValuePerPoint;
                }
                else
                {
                    interpolationDifference = (progressThroughInterval - (int)forecastDataContainer.LerpStrength) * interpolationValuePerPoint;
                }

                ApplyInterpolation(weatherData, forecastData[currentIntervalIndex + 1], (float)interpolationDifference);
            }

            return (forecastDataContainer.LerpStrength == IntervalLerpSpeed.Slow ? progressThroughInterval : interpolationDifference) / kHundredPercent;
        }

        /// <summary>
        /// Applies interpolation on all weather data members, exception the ones that can't be changed, like UTC offset or Localization
        /// </summary>
        /// <param name="weatherData">The weather data on which the interpolation is applied</param>
        /// <param name="nextWeatherData">The weather data from where are extracted the data for interpolation</param>
        /// <param name="interpolationDifference">The value of interpolation applied on new values (relative to "nextWeatherData")</param>
        private void ApplyInterpolation(WeatherData weatherData, WeatherData nextWeatherData, float interpolationDifference)
        {
            weatherData.Temperature += UpdateValueWithInterpolation(weatherData.Temperature, nextWeatherData.Temperature, interpolationDifference);
            weatherData.Pressure += UpdateValueWithInterpolation(weatherData.Pressure, nextWeatherData.Pressure, interpolationDifference);
            weatherData.Precipitation += UpdateValueWithInterpolation(weatherData.Precipitation, nextWeatherData.Precipitation, interpolationDifference);
            weatherData.Humidity += UpdateValueWithInterpolation(weatherData.Humidity, nextWeatherData.Humidity, interpolationDifference);
            weatherData.Dewpoint += UpdateValueWithInterpolation(weatherData.Dewpoint, nextWeatherData.Dewpoint, interpolationDifference);
            weatherData.Visibility += UpdateValueWithInterpolation(weatherData.Visibility, nextWeatherData.Visibility, interpolationDifference);
            weatherData.IndexUV += (int)UpdateValueWithInterpolation(weatherData.IndexUV, nextWeatherData.IndexUV, interpolationDifference);
            weatherData.Wind.Speed += UpdateValueWithInterpolation(weatherData.Wind.Speed, nextWeatherData.Wind.Speed, interpolationDifference);

            var newXDirectionValue = UpdateValueWithInterpolation(weatherData.Wind.Direction.x, nextWeatherData.Wind.Direction.x, interpolationDifference);
            var newYDirectionValue = UpdateValueWithInterpolation(weatherData.Wind.Direction.y, nextWeatherData.Wind.Direction.y, interpolationDifference);
            weatherData.Wind.Direction += new Vector2(newXDirectionValue, newYDirectionValue);
        }

        /// <summary>
        /// Applies interpolation on a specific variable (float)
        /// </summary>
        /// <param name="currentValue">Start value used for interpolation</param>
        /// <param name="nextWeatherValue">The value from where the diffence is extracted for interpolation</param>
        /// <param name="interpolationDifference">The value of interpolation applied on the new value</param>
        /// <returns>The value interpolated</returns>
        private float UpdateValueWithInterpolation(float currentValue, float nextWeatherValue, float interpolationDifference)
        {
            return (nextWeatherValue - currentValue) * (interpolationDifference / kHundredPercent);
        }

        /// <summary>
        /// Calculates the complete time of the entire simulation (in seconds)
        /// </summary>
        /// <param name="forecastData">The list of the weather data</param>
        /// <param name="simulationSpeed">The simulation speed</param>
        private void CalculateSimulationTime(List<WeatherData> forecastData, int simulationSpeed)
        {
            ForecastHoursTotalTime = 0;
            DateTime dateTime = forecastData[0].DateTime;
            for (int i = 1; i < forecastData.Count; i++)
            {
                TimeSpan differenceDateTime = forecastData[i].DateTime.Subtract(dateTime);
                ForecastHoursTotalTime += (int)differenceDateTime.TotalHours;
                dateTime = dateTime.AddHours(differenceDateTime.TotalHours);
            }

            SimulationTotalTime = ForecastHoursTotalTime * simulationSpeed;
        }

        /// <summary>
        /// If the current interval is not the last, then a next interval of weather data is returned, otherwise "null" value is returned
        /// If loop option is activated, in case the current interval index is the last, then the first interval is returned
        /// </summary>
        /// <returns>The weather afferent to the request</returns>
        private WeatherData ReturnTheNextIntervalWeather()
        {
            //We check if the next weather interval is the last interval
            bool nextIntervalIsTheLast = (currentIntervalIndex + 1) == forecastData.Count - 1;

            //We check how much time remained from the transition of the current interval to the next weather interval (the last one)
            TimeSpan timeLeftFromLastInterval = forecastData[currentIntervalIndex + 1].DateTime - timelapseProgressDate;

            //We check how long is the last interval (eg. 2 hours, 5 hours, max 24 hours, etc.)
            TimeSpan lastIntervalTime = new TimeSpan((forecastData[currentIntervalIndex + 1].DateTime - forecastData[currentIntervalIndex].DateTime).Ticks);

            //We calculate how much from the transtion of the current interval to the last represents the lerp time
            //Ex. for Lerp strength set to high, which applies when the transition is at 75%, the last 25% is the lerp time, so it the total transition takes 2 hours, then the lerp time would be 30 mins
            var remainedPercentageForLoop = (100 - (int)forecastDataContainer.LerpStrength) / 100.0f;

            //We want to calculate half the lerp loop time, so if the lerp time is 30 mins, then half of it would be 15 mins
            TimeSpan lerpToFirstIntervalTransitionTime = new TimeSpan((long)(lastIntervalTime.Ticks / 2.0f * remainedPercentageForLoop));
            if (loop && nextIntervalIsTheLast && timeLeftFromLastInterval < lerpToFirstIntervalTransitionTime)
            {
                return forecastData[0];
            }

            return forecastData[currentIntervalIndex + 1];
        }
        #endregion

        //This could be added as constructor in WeatherData.cs class from the Real-Time Weather source solution, but dateTime will be different
        private WeatherData ConstructClone(WeatherData weatherData)
        {
            WeatherData weatherDataClone = new WeatherData();

            weatherDataClone.Localization = new Localization(weatherData.Localization.Country,
                weatherData.Localization.City, weatherData.Localization.Latitude,
                weatherData.Localization.Longitude);
            weatherDataClone.DateTime = timelapseProgressDate;
            weatherDataClone.Wind = new Wind(weatherData.Wind.Direction, weatherData.Wind.Speed);
            weatherDataClone.WeatherState = weatherData.WeatherState;
            weatherDataClone.Temperature = weatherData.Temperature;
            weatherDataClone.Pressure = weatherData.Pressure;
            weatherDataClone.Precipitation = weatherData.Precipitation;
            weatherDataClone.Humidity = weatherData.Humidity;
            weatherDataClone.Dewpoint = weatherData.Dewpoint;
            weatherDataClone.Visibility = weatherData.Visibility;
            weatherDataClone.IndexUV = weatherData.IndexUV;
            weatherDataClone.TimeZone = weatherData.TimeZone;
            weatherDataClone.UTCOffset = weatherData.UTCOffset;

            return weatherDataClone;
        }
    }
}
