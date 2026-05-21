//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using RealTimeWeather.Data;
using RealTimeWeather.Enums;
using RealTimeWeather.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RealTimeWeather.Simulation
{
    /// <summary>
    /// Convert daily or hourly data to a simulation timelapse
    /// </summary>
    public class ForecastToTimelapseConvertor
    {
        private const string kSliderTexturesStr = "SliderTextures";
        private const string kAssetExtension = ".asset";
        private const string kDateFormat = "dd/MM/yyyy";

        private TimelapsePopupTextures popupTextures = RealTimeWeatherManager.instance.PopupTextures;
#if UNITY_EDITOR
        /// <summary>
        /// Creates and saves the timelapse
        /// </summary>
        public void CreateTimelapseData(List<WeatherData> forecastData, ForecastDataType type, float latitude, float longitude, bool loop, IntervalLerpSpeed intervalLerpSpeed, int simulationSpeed, string startDate = "")
        {
            if (forecastData == null || forecastData.Count == 0) return;

            Timelapse timelapse;

            if (type == ForecastDataType.Hourly)
            {
                timelapse = CreateTimelapseFromHourlyData(forecastData);
            }
            else
            {
                timelapse = CreateTimelapseFromDailyData(forecastData);
            }

            timelapse.timelapseLongitude = longitude;
            timelapse.timelapseLatitude = latitude;
            timelapse.timelapseName = RealTimeWeatherManager.instance.CurrentSimulationSelected.name;
            timelapse.loopSim = loop;
            timelapse.lerpSpeed = intervalLerpSpeed;
            timelapse.simulationSpeed = simulationSpeed;
            timelapse.timelapseDate = forecastData[0].DateTime.ToString();
            try
            {
                var date = DateTime.ParseExact(startDate, kDateFormat, CultureInfo.InvariantCulture);
                RealTimeWeatherManager.instance.SaveTimelapse(timelapse, date, true);
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
        private Timelapse CreateTimelapseFromHourlyData(List<WeatherData> forecastData)
        {
            var timelapse = new Timelapse();
            timelapse.timelapseDays.Clear();
            var defaultTimelapseDay = new TimelapseDay();
            defaultTimelapseDay.timelapseIntervals.Clear();
            var defaultInterval = new TimelapseInterval(timelapse.defaultIntervalData);
            defaultTimelapseDay.delimitersPos = new bool[25];

            for (int i = 0; i < defaultTimelapseDay.delimitersPos.Length; i++)
            {
                defaultTimelapseDay.delimitersPos[i] = true;
            }
            defaultTimelapseDay.delimitersPos[0] = false;

            for (int i = 0; i < 24; i++)
            {
                defaultTimelapseDay.AddInterval(i, defaultInterval);
                if (i != 0)
                {
                    defaultTimelapseDay.startedInside.Add(false);
                    defaultTimelapseDay.sliderRelPosX.Add((float)i / InspectorUtils.kLabelDivisions);
                }
            }

            var startIndex = forecastData[0].DateTime.Hour;
            var timelapseDay = new TimelapseDay(defaultTimelapseDay);

            for (int i = 0; i < forecastData.Count; i++)
            {
                if ((i + startIndex) % 24 == 0 && i + startIndex > 0)
                {
                    timelapse.AddDay(new TimelapseDay(timelapseDay));
                    timelapseDay = new TimelapseDay(defaultTimelapseDay);
                }

                timelapseDay.RemoveInterval(forecastData[i].DateTime.Hour);
                var timelapseInterval = new TimelapseInterval(forecastData[i]);
                timelapseInterval.weatherDataProfileStr = "Weather Data: Custom Interval";
                timelapseInterval.weatherData.color = GetDefaultPresetColor(forecastData[i].WeatherState);
                timelapseInterval.weatherData.texture = GetWeatherStateTexture(forecastData[i].WeatherState);
                timelapseDay.AddInterval(forecastData[i].DateTime.Hour, timelapseInterval);
            }

            timelapse.AddDay(new TimelapseDay(timelapseDay));

            return timelapse;
        }
#endif

        private Timelapse CreateTimelapseFromDailyData(List<WeatherData> forecastData)
        {
            var timelapse = new Timelapse();
            timelapse.timelapseDays.Clear();
            for (int i = 0; i < forecastData.Count; i++)
            {
                var timelapseDay = new TimelapseDay();
                timelapseDay.timelapseIntervals.Clear();
                var timelapseInterval = new TimelapseInterval(forecastData[i]);
                timelapseInterval.weatherDataProfileStr = "Weather Data: Custom Interval";
                timelapseDay.AddInterval(0, timelapseInterval);
                timelapse.AddDay(timelapseDay);
            }

            return timelapse;
        }

        private Color GetDefaultPresetColor(WeatherState weatherState)
        {
            switch (weatherState)
            {
                case WeatherState.Clear:
                    return popupTextures.ClearDefaultColor;
                case WeatherState.PartlyClear:
                    return popupTextures.PartlyClearDefaultColor;
                case WeatherState.Cloudy:
                    return popupTextures.CloudyDefaultColor;
                case WeatherState.Mist:
                    return popupTextures.MistDefaultColor;
                case WeatherState.Thunderstorms:
                    return popupTextures.ThunderstormsDefaultColor;
                case WeatherState.RainSnowPrecipitation:
                    return popupTextures.RainSnowPrecipitationDefaultColor;
                case WeatherState.RainPrecipitation:
                    return popupTextures.RainPrecipitationDefaultColor;
                case WeatherState.SnowPrecipitation:
                    return popupTextures.SnowPrecipitatioDefaultColor;
                case WeatherState.Windy:
                    return popupTextures.WindyDefaultColor;
                case WeatherState.Sunny:
                    return popupTextures.SunnyDefaultColor;
            }
            return popupTextures.SunnyDefaultColor;
        }

        /// <summary>
        /// Gets a texture from a WeatherState
        /// </summary>
        /// <param name="weatherState">The WeatherState</param>
        /// <returns>The texture</returns>
        private Texture2D GetWeatherStateTexture(WeatherState weatherState)
        {
            switch (weatherState)
            {
                case WeatherState.Clear:
                    return popupTextures.intervalTexturesClear;
                case WeatherState.PartlyClear:
                    return popupTextures.intervalTexturesPartlyClear;
                case WeatherState.Cloudy:
                    return popupTextures.intervalTexturesCloudy;
                case WeatherState.Mist:
                    return popupTextures.intervalTexturesMist;
                case WeatherState.Thunderstorms:
                    return popupTextures.intervalTexturesThunderstorms;
                case WeatherState.RainSnowPrecipitation:
                    return popupTextures.intervalTexturesRainSnowPrecipitation;
                case WeatherState.RainPrecipitation:
                    return popupTextures.intervalTexturesRainPrecipitation;
                case WeatherState.SnowPrecipitation:
                    return popupTextures.intervalTexturesSnowPrecipitation;
                case WeatherState.Windy:
                    return popupTextures.intervalTexturesWindy;
                case WeatherState.Sunny:
                    return popupTextures.intervalTexturesSunny;
            }
            return popupTextures.intervalTexturesSunny;
        }
    }
}