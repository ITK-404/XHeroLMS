// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using RealTimeWeather.Data;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    public class StormglassDataConverter
    {
        #region Public Delegates
        public delegate void OnStormglassExceptionRaised(ExceptionType exception, string exceptionMessage);
        public OnStormglassExceptionRaised onStormglassExceptionRaised;
        #endregion

        #region Private Constants
        private const string kStormglassTimeFormat = "yyyy-MM-ddTHH:mm:ss+00:00";
        #endregion

        #region Public Methods
        public WeatherData ConvertCurrentStormglassDataToWeatherData(StormglassWeatherData stormglassData)
        {
            WeatherData _weatherData = new WeatherData();
            _weatherData.TimeZone = Utilities.GetTimeZone(stormglassData.meta.lat, stormglassData.meta.lng);
#if UNITY_STANDALONE || UNITY_IOS
            _weatherData.UTCOffset = Utilities.GetUTCOffsetData(_weatherData.TimeZone);
#endif
#if UNITY_ANDROID
            _weatherData.UTCOffset = Utilities.GetUTCOffsetOnAndroid(_weatherData.TimeZone);
#endif
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            DateTime localTime = DateTime.Now;
            TimeSpan localOffset = localZone.GetUtcOffset( localTime );
            
            DateTime currentLocationTime = DateTime.Now;
            currentLocationTime = currentLocationTime.Add(_weatherData.UTCOffset - localOffset);
            
            int currentHour = currentLocationTime.Hour;
            _weatherData.Precipitation = stormglassData.hours[currentHour].precipitation.sg;
            _weatherData.Pressure = stormglassData.hours[currentHour].pressure.sg;
            _weatherData.Temperature = stormglassData.hours[currentHour].airTemperature.sg;
            _weatherData.Humidity = stormglassData.hours[currentHour].humidity.sg;
            _weatherData.Visibility = stormglassData.hours[currentHour].visibility.sg;
            _weatherData.WeatherState = ConvertCloudCoverIntoWeatherState(stormglassData.hours[currentHour].cloudCover.sg);
            _weatherData.DateTime = Utilities.GetDateTimeBasedOnGeocoordinates(stormglassData.meta.lat, stormglassData.meta.lng);
            _weatherData.Wind = ConvertWind(stormglassData.hours[currentHour].windDirection.sg, stormglassData.hours[currentHour].windSpeed.sg);
            _weatherData.Localization = ConvertLocation(stormglassData.meta.lat, stormglassData.meta.lng);

            return _weatherData;
        }

        public List<WeatherData> ConvertHourlyStormglassDataToWeatherData(StormglassWeatherData stormglassData)
        {
            List<WeatherData> _weatherData = new List<WeatherData>();
            for (int i = 0; i < stormglassData.hours.Count; i++)
            {
                _weatherData.Add(new WeatherData());
                _weatherData[i].Precipitation = stormglassData.hours[i].precipitation.sg;
                _weatherData[i].Pressure = stormglassData.hours[i].pressure.sg;
                _weatherData[i].Temperature = stormglassData.hours[i].airTemperature.sg;
                _weatherData[i].Humidity = stormglassData.hours[i].humidity.sg;
                _weatherData[i].Visibility = stormglassData.hours[i].visibility.sg;
                _weatherData[i].TimeZone = Utilities.GetTimeZone(stormglassData.meta.lat, stormglassData.meta.lng);
#if UNITY_STANDALONE || UNITY_IOS
                _weatherData[i].UTCOffset = Utilities.GetUTCOffsetData(_weatherData[i].TimeZone);
#endif
#if UNITY_ANDROID
                _weatherData[i].UTCOffset = Utilities.GetUTCOffsetOnAndroid(_weatherData[i].TimeZone);
#endif
                _weatherData[i].WeatherState = ConvertCloudCoverIntoWeatherState(stormglassData.hours[i].cloudCover.sg);
                _weatherData[i].DateTime = Utilities.GetDateTimeBasedOnGeocoordinates(stormglassData.meta.lat, stormglassData.meta.lng);
                _weatherData[i].Wind = ConvertWind(stormglassData.hours[i].windDirection.sg, stormglassData.hours[i].windSpeed.sg);
                _weatherData[i].Localization = ConvertLocation(stormglassData.meta.lat, stormglassData.meta.lng);
            }
            return _weatherData;
        }

        public WaterData ConvertCurrentStormglassDataToWaterData(StormglassWeatherData stormglassData)
        {
            WaterData _maritimeData = new WaterData();
            int currentHour = DateTime.Now.Hour;
            _maritimeData.Precipitation = stormglassData.hours[currentHour].precipitation.sg;
            _maritimeData.Temperature = stormglassData.hours[currentHour].waterTemperature.sg;
            _maritimeData.AirPressureAtSea = stormglassData.hours[currentHour].pressure.sg;
            _maritimeData.Humidity = stormglassData.hours[currentHour].humidity.sg;
            _maritimeData.Visibility = stormglassData.hours[currentHour].visibility.sg;
            _maritimeData.CloudCover = stormglassData.hours[currentHour].cloudCover.sg;
            _maritimeData.DateTime = Utilities.GetDateTimeBasedOnGeocoordinates(stormglassData.meta.lat, stormglassData.meta.lng);
            _maritimeData.Wind = ConvertWind(stormglassData.hours[currentHour].windDirection.sg, stormglassData.hours[currentHour].windSpeed.sg);
            _maritimeData.Wave = ConvertWave(stormglassData.hours[currentHour].waveHeight.sg, stormglassData.hours[currentHour].wavePeriod.sg, stormglassData.hours[currentHour].waveDirection.sg);
            _maritimeData.Localization = ConvertLocation(stormglassData.meta.lat, stormglassData.meta.lng);

            return _maritimeData;
        }

        public List<WaterData> ConvertHourlyStormglassDataToWaterData(StormglassWeatherData stormglassData)
        {
            List<WaterData> _maritimeData = new List<WaterData>();
            for (int i = 0; i < stormglassData.hours.Count; i++)
            {
                _maritimeData.Add(new WaterData());
                _maritimeData[i].Precipitation = stormglassData.hours[i].precipitation.sg;
                _maritimeData[i].Temperature = stormglassData.hours[i].waterTemperature.sg;
                _maritimeData[i].AirPressureAtSea = stormglassData.hours[i].pressure.sg;
                _maritimeData[i].Humidity = stormglassData.hours[i].humidity.sg;
                _maritimeData[i].Visibility = stormglassData.hours[i].visibility.sg;
                _maritimeData[i].CloudCover = stormglassData.hours[i].cloudCover.sg;
                _maritimeData[i].DateTime = Utilities.GetDateTimeBasedOnGeocoordinates(stormglassData.meta.lat, stormglassData.meta.lng);
                _maritimeData[i].Wind = ConvertWind(stormglassData.hours[i].windDirection.sg, stormglassData.hours[i].windSpeed.sg);
                _maritimeData[i].Wave = ConvertWave(stormglassData.hours[i].waveHeight.sg, stormglassData.hours[i].wavePeriod.sg, stormglassData.hours[i].waveDirection.sg);
                _maritimeData[i].Localization = ConvertLocation(stormglassData.meta.lat, stormglassData.meta.lng);
            }
            return _maritimeData;
        }
        #endregion

        #region Private Methods
        private Wind ConvertWind(float stormglassDirection, float stormglassSpeed)
        {
            Wind waterDataWind = new Wind();
            waterDataWind.Direction = Utilities.DegreeToVector2(stormglassDirection);
            waterDataWind.Speed = stormglassSpeed;
            return waterDataWind;
        }

        private Wave ConvertWave(float stormglassHeight, float stormglassPeriodPeak, float stormglassDirection)
        {
            Wave waterDataWave = new Wave();
            waterDataWave.Direction = Utilities.DegreeToVector2(stormglassDirection);
            waterDataWave.Height = (stormglassHeight != 0.0f ? stormglassHeight : -1000f);
            waterDataWave.PeriodPeak = (stormglassPeriodPeak != 0.0f ? stormglassPeriodPeak : -1000f);
            return waterDataWave;
        }

        private Localization ConvertLocation(float stormglassLatitude, float stormglassLongitude)
        {
            Localization waterDataLocation = new Localization();
            waterDataLocation.Latitude = stormglassLatitude;
            waterDataLocation.Longitude = stormglassLongitude;
            return waterDataLocation;
        }

        private WeatherState ConvertCloudCoverIntoWeatherState(float cloudCover)
        {
            if(cloudCover < 5)
            {
                return WeatherState.Clear;
            }
            if(cloudCover < 10)
            {
                return WeatherState.Fair;
            }
            if(cloudCover < 40)
            {
                return WeatherState.PartlyCloudy;
            }
            if(cloudCover < 70)
            {
                return WeatherState.PartlyClear;
            }
            if(cloudCover <= 100)
            {
                return WeatherState.Cloudy;
            }

            return WeatherState.Clear;
        }
        #endregion
    }
}