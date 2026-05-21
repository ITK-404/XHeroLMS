// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//
using UnityEngine;
using System.Collections.Generic;
using RealTimeWeather.Data;
using RealTimeWeather.Classes;

namespace RealTimeWeather.Simulation
{
    public class TimelapseDay
    {
        #region Const Members
        private const int kDefaultIntervalMinValue = 0;
        private const int kDefaultIntervalMaxValue = 1;
        private const int kDefaultIntervalStartHour = 0;
        private const int kDefaultIntervalEndHour = 1;
        #endregion

        #region Public Variables
        public List<TimelapseInterval> timelapseIntervals = new List<TimelapseInterval>();

        #region Editor Variables
        public bool showData;
        public bool[] delimitersPos;

        public string dayName;
        public int selectedSlider;
        public int lastDelimiterPos;
        public Vector2 presetWindowScrollPosition = Vector2.zero;
        public List<bool> startedInside = new List<bool>();
        public List<float> sliderRelPosX = new List<float>();
        #endregion
        #endregion

        #region Public Constructors
        public TimelapseDay()
        {
            InitializeVariables();
            timelapseIntervals.Add(new TimelapseInterval());
        }

        public TimelapseDay(ForecastWeatherData defaultIntervalData)
        {
            InitializeVariables();
            TimelapseInterval interval = new TimelapseInterval();
            interval.weatherData = defaultIntervalData;
            interval.presetName = defaultIntervalData.name;
            interval.intervalState = IntervalState.DefaultInterval;
            interval.presetColor = defaultIntervalData.color;
            timelapseIntervals.Add(interval);
        }

        public TimelapseDay(TimelapseDay timelapseDay)
        {
            showData = timelapseDay.showData;
            dayName = timelapseDay.dayName;
            selectedSlider = timelapseDay.selectedSlider;
            lastDelimiterPos = timelapseDay.lastDelimiterPos;
            presetWindowScrollPosition = timelapseDay.presetWindowScrollPosition;
            delimitersPos = new bool[timelapseDay.delimitersPos.Length];
            for (int i = 0; i < timelapseDay.delimitersPos.Length; i++)
            {
                delimitersPos[i] = timelapseDay.delimitersPos[i];
            }
            startedInside = new List<bool>();
            for (int i = 0; i < timelapseDay.startedInside.Count; i++)
            {
                startedInside.Add(timelapseDay.startedInside[i]);
            }
            sliderRelPosX = new List<float>();
            for (int i = 0; i < timelapseDay.sliderRelPosX.Count; i++)
            {
                sliderRelPosX.Add(timelapseDay.sliderRelPosX[i]);
            }
            timelapseIntervals = new List<TimelapseInterval>();
            for (int i = 0; i < timelapseDay.timelapseIntervals.Count; i++)
            {
                timelapseIntervals.Add(new TimelapseInterval(timelapseDay.timelapseIntervals[i]));
            }
        }

        public TimelapseDay(TimelapseDay timelapseDay, ForecastWeatherData defaultIntervalData)
        {
            dayName = timelapseDay.dayName;

            timelapseIntervals = new List<TimelapseInterval>();
            startedInside = new List<bool>();
            sliderRelPosX = new List<float>();
            delimitersPos = new bool[25];

            TimelapseInterval intervalData = new TimelapseInterval();
            for (int i = 0; i < timelapseDay.timelapseIntervals.Count; i++)
            {
                intervalData = new TimelapseInterval(timelapseDay.timelapseIntervals[i].weatherData);
                if (timelapseDay.timelapseIntervals[i].intervalState == IntervalState.DefaultInterval)
                {
                    intervalData.weatherData = defaultIntervalData;
                    intervalData.intervalState = IntervalState.DefaultInterval;
                }
                
                timelapseIntervals.Add(intervalData);

            }
            for (int i = 0; i < timelapseDay.sliderRelPosX.Count; i++)
            {
                sliderRelPosX.Add(timelapseDay.sliderRelPosX[i]);
                startedInside.Add(timelapseDay.startedInside[i]);
            }
            for (int i = 0; i < delimitersPos.Length; i++)
            {
                delimitersPos[i] = timelapseDay.delimitersPos[i];
            }
        }
        #endregion

        #region Public Methods
        public void AddInterval(int index, TimelapseInterval timelapseInterval)
        {
            timelapseIntervals.Insert(index, timelapseInterval);
        }

        public void RemoveInterval(int index)
        {
            timelapseIntervals.RemoveAt(index);
        }
        #endregion

        #region Private Methods
        private void InitializeVariables()
        {
            selectedSlider = -1;
            startedInside = new List<bool>();
            sliderRelPosX = new List<float>();
            delimitersPos = new bool[25];
            lastDelimiterPos = -1;
            dayName = null;
        }
        #endregion
    }
}