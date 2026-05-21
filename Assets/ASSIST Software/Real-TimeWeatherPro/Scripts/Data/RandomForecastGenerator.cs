// 
// Copyright(c) 2023 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;
using RealTimeWeather.Managers;
using RealTimeWeather.Simulation;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace RealTimeWeather.Data
{
    public class RandomForecastGenerator
    {
        public static void GenerateRandomForecast(string name, List<PresetData> presets, int days, int min, int max, bool isGeneratingProbabilityRequired)
        {
            var timelapse = new Timelapse();
            timelapse.timelapseDays.Clear();
            timelapse.timelapseName = name;
            var defaultTimelapseDay = new TimelapseDay();
            defaultTimelapseDay.timelapseIntervals.Clear();
            defaultTimelapseDay.delimitersPos = new bool[25];
            var random = new System.Random();
            var dayComplete = false;
            var startindex = 0;
            var intervalIndex = 0;
            var timelapseInterval2 = new TimelapseInterval();

            if (isGeneratingProbabilityRequired)
            {
                var prob = Mathf.RoundToInt(100f / presets.Count);
                for (int i = 0; i < presets.Count; i++)
                {
                    var itemProb = random.Next(1, prob);
                    if (i > 0)
                    {
                        itemProb += presets[i - 1].ForecastProbability;
                    }

                    presets[i] = new PresetData(presets[i].ForecastWeatherData, itemProb);
                }

                if (presets[presets.Count - 1].ForecastProbability != 100)
                {
                    prob = (100 - presets[presets.Count - 1].ForecastProbability) / presets.Count;

                    for (int i = 0; i < presets.Count; i++)
                    {
                        presets[i] = new PresetData(presets[i].ForecastWeatherData, presets[i].ForecastProbability + (i + 1) * prob);
                    }
                }

                presets[presets.Count - 1] = new PresetData(presets[presets.Count - 1].ForecastWeatherData, 100);
            }

            for (int i = 0; i < days; i++)
            {
                dayComplete = false;

                var timelapseDay = new TimelapseDay(defaultTimelapseDay);
                intervalIndex = 0;

                if (startindex != 0)
                {
                    timelapseDay.AddInterval(0, timelapseInterval2);
                    timelapseDay.delimitersPos[startindex] = true;
                    timelapseDay.sliderRelPosX.Add((float)startindex / InspectorUtils.kLabelDivisions);
                    intervalIndex++;
                }

                while (!dayComplete)
                {
                    var rand = random.Next(min, max + 1);
                    var timelapseInterval = new TimelapseInterval();

                    var randTimelapse = random.Next(1, 101);

                    for (int j = 0; j < presets.Count; j++)
                    {
                        if (randTimelapse <= presets[j].ForecastProbability)
                        {
                            timelapseInterval = new TimelapseInterval(presets[j].ForecastWeatherData);
                            timelapseInterval2 = new TimelapseInterval(timelapseInterval);
                            break;
                        }
                    }

                    timelapseDay.AddInterval(intervalIndex, timelapseInterval);
                    startindex += rand;

                    if (startindex >= 24)
                    {
                        startindex -= 24;
                        dayComplete = true;
                        timelapse.AddDay(new TimelapseDay(timelapseDay));
                    }
                    else
                    {
                        timelapseDay.delimitersPos[startindex] = true;
                        timelapseDay.sliderRelPosX.Add((float)startindex / InspectorUtils.kLabelDivisions);
                        intervalIndex++;
                    }
                }
            }
            RealTimeWeatherManager.instance.SaveTimelapse(timelapse, DateTime.Now, true);
        }
    }
}
#endif