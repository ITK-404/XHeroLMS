// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using System;
using System.Globalization;
using System.Collections.Generic;
using RealTimeWeather.Data;
using RealTimeWeather.WeatherProvider.Metocean;

namespace RealTimeWeather.UI
{
    public class MetoceanDataConverter
    {
        #region Public Delegates
        public delegate void OnMetoceanExceptionRaised(ExceptionType exception, string exceptionMessage);
        public OnMetoceanExceptionRaised onMetoceanExceptionRaised;
        #endregion

        #region Constants
        private const int kCurrentTimeIndex = 0;
        private const float noDataFloat = -1000f;
        private const string kMetoceanTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        #endregion

        #region Private Variables
        private MetoceanData _metoceanData;
        private int _repeat;
        #endregion

        #region Constructor
        public MetoceanDataConverter(MetoceanData waterData, int repeat)
        {
            _metoceanData = waterData;
            _repeat = repeat;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Main functionality for converting a list of MetoceanData structures to a list of Real-Time Weather WaterData structures
        /// </summary>
        /// <returns>A list of instances of WaterData class with all data converted</returns>
        public List<WaterData> ConvertIntervalMetoceanDataToWaterData()
        {
            List<WaterData> intervalDataList = new List<WaterData>();

            for (int i = kCurrentTimeIndex; i <= _repeat; i++)
            {
                WaterData waterData = ReturnConvertedData(i);
                intervalDataList.Add(waterData);
            }

            return intervalDataList;
        }

        public List<WeatherData> ConvertIntervalMetoceanDataToWeatherData()
        {
            List<WeatherData> intervalDataList = new List<WeatherData>();

            for (int i = kCurrentTimeIndex; i <= _repeat; i++)
            {
                WeatherData weatherData = ReturnWeatherConvertedData(i);
                intervalDataList.Add(weatherData);
            }

            return intervalDataList;
        }

        /// <summary>
        /// Returns the MetoceanData converted into Real-Time Weather WaterData
        /// </summary>
        /// <returns>A WaterData structure</returns>
        public WaterData ConvertCurrentMetoceanDataToWaterData()
        {
            WaterData RTWData = ReturnConvertedData(kCurrentTimeIndex);
            return RTWData;
        }

        public WeatherData ConvertCurrentMetoceanDataToWeatherData()
        {
            WeatherData weatherData = ReturnWeatherConvertedData(kCurrentTimeIndex);
            return weatherData;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Main functionality for converting a MetoceanData structure to Real-Time Weather WaterData structure
        /// </summary>
        /// <returns>An instance of WaterData class with all data converted</returns>
        private WaterData ReturnConvertedData(int dataRequestIndex)
        {
            WaterData RTWData = new WaterData();
            RTWData.Localization = ReturnLocalizationParameter();
            RTWData.Precipitation = ReturnPrecipitationParameter(dataRequestIndex);
            RTWData.Temperature = ReturnTemperatureParameter(dataRequestIndex);
            RTWData.AirPressureAtSea = ReturnAirPressureParameter(dataRequestIndex);
            RTWData.Wind = ReturnWindParameter(dataRequestIndex);
            RTWData.Humidity = ReturnHumidityParameter(dataRequestIndex);
            RTWData.Visibility = ReturnVisibilityParameter(dataRequestIndex);
            RTWData.Wave = ReturnWaveParameter(dataRequestIndex);
            RTWData.CloudCover = ReturnCloudCoverParameter(dataRequestIndex);
            RTWData.FluxLongwave = ReturnFluxLongwaveParameter(dataRequestIndex);
            RTWData.FluxShortwave = ReturnFluxShortwaveParameter(dataRequestIndex);
            RTWData.SeaSurfaceTemperature = ReturnSeaSurfaceTemperatureParameter(dataRequestIndex);
            RTWData.TimeZone = Utilities.GetTimeZone(RTWData.Localization.Latitude, RTWData.Localization.Longitude);
            RTWData.UtcOffset = ConvertUTCOffset(RTWData.TimeZone);
            RTWData.DateTime = ConvertDateTime(RTWData.UtcOffset);
            return RTWData;
        }

        private WeatherData ReturnWeatherConvertedData(int dataRequestIndex)
        {
            WeatherData weatherData = new WeatherData();
            weatherData.DateTime = ConvertDateTime(weatherData.UTCOffset);
            weatherData.Localization = ReturnLocalizationParameter();
            weatherData.Precipitation = ReturnPrecipitationParameter(dataRequestIndex);
            weatherData.Temperature = ReturnTemperatureParameter(dataRequestIndex);
            weatherData.Pressure = ReturnAirPressureParameter(dataRequestIndex);
            weatherData.Wind = ReturnWindParameter(dataRequestIndex);
            weatherData.Visibility = ReturnVisibilityParameter(dataRequestIndex);
            weatherData.WeatherState = Utilities.ReturnWeatherStateBasedCloudCover(_metoceanData.Variables.CloudCover.Data[dataRequestIndex],
                                                                                    _metoceanData.Variables.PrecipitationRate.Data[dataRequestIndex]);
            weatherData.Humidity = ReturnHumidityParameter(dataRequestIndex);
            weatherData.Dewpoint = Utilities.CalculateDewpoint(weatherData.Temperature, weatherData.Humidity);
            weatherData.TimeZone = Utilities.GetTimeZone(weatherData.Localization.Latitude, weatherData.Localization.Longitude);
            weatherData.UTCOffset = ConvertUTCOffset(weatherData.TimeZone);
            return weatherData;
        }

        /// <summary>
        /// Creates a new Localization instance with data about latitude and longitude
        /// </summary>
        /// <returns>An instance of Localization class</returns>
        private Localization ReturnLocalizationParameter()
        {
            Localization localization = new Localization();
            localization.Latitude = _metoceanData.Latitude;
            localization.Longitude = _metoceanData.Longitude;
            localization.City = _metoceanData.Location;
            localization.Country = _metoceanData.Country;

            return localization;
        }

        /// <summary>
        /// Create a DateTime instance representing the converted water data's time
        /// </summary>
        /// <returns>A DateTime value of data</returns>
        private string ReturnDateTimeParameter(int index)
        {
            return _metoceanData.Dimensions.Time.Data[index];
        }

        /// <summary>
        /// Create a float value representing an estimative precipitation value
        /// </summary>
        /// <returns>A precipitation float value</returns>
        private float ReturnPrecipitationParameter(int index)
        {
            var precipitationRateData = _metoceanData.Variables.PrecipitationRate.Data[index];
            return precipitationRateData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.PrecipitationRate.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Creates a new float value representing the current temperature
        /// </summary>
        /// <returns>A temperature float value</returns>
        private float ReturnTemperatureParameter(int index)
        {
            var airTemperatureData = _metoceanData.Variables.AirTemperature.Data[index];
            return airTemperatureData == string.Empty ? noDataFloat : (float)Utilities.ConvertKelvinToDegrees(float.Parse(_metoceanData.Variables.AirTemperature.Data[index], CultureInfo.InvariantCulture.NumberFormat));
        }

        /// <summary>
        /// Creates a new float value representing the current pressure of the air
        /// </summary>
        /// <returns>A pressure float value</returns>
        private float ReturnAirPressureParameter(int index)
        {
            var airPressureData = _metoceanData.Variables.AirPressureAtSea.Data[index];
            return airPressureData == string.Empty ? noDataFloat : Utilities.ConvertHectoPascalsToMiliBars(float.Parse(_metoceanData.Variables.AirPressureAtSea.Data[index], CultureInfo.InvariantCulture.NumberFormat));
        }

        /// <summary>
        /// Creates a new Wind instance with data about the speed and direction of the wind
        /// </summary>
        /// <returns>An instance of Wind class</returns>
        private Wind ReturnWindParameter(int index)
        {
            var windDirectionData = _metoceanData.Variables.WindDirection.Data[index];
            var windSpeedData = _metoceanData.Variables.WindSpeed.Data[index];

            var windDirection = windDirectionData == string.Empty ? Vector2.zero : Utilities.DegreeToVector2(float.Parse(_metoceanData.Variables.WindDirection.Data[index]));
            var windSpeed = windSpeedData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.WindSpeed.Data[index]);
            return new Wind(windDirection, windSpeed);
        }

        /// <summary>
        /// Creates a float value representing the current humidity in the air
        /// </summary>
        /// <returns>A humidity float value</returns>
        private float ReturnHumidityParameter(int index)
        {
            var airHumidityData = _metoceanData.Variables.AirHumidity.Data[index];
            return airHumidityData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.AirHumidity.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Creates a float value representing the visibility at which can be clearly discerned
        /// </summary>
        /// <returns>A visibility float value</returns>
        private float ReturnVisibilityParameter(int index)
        {
            var airVisibilityData = _metoceanData.Variables.AirVisibility.Data[index];
            return airVisibilityData == string.Empty ? noDataFloat : (float)Utilities.ConvertMetersToKilometers(float.Parse(_metoceanData.Variables.AirVisibility.Data[index], CultureInfo.InvariantCulture.NumberFormat));
        }

        /// <summary>
        /// Creates a new Wave instance with data about the height, period and direction of a wave
        /// </summary>
        /// <returns>An instance of a Wave class</returns>
        private Wave ReturnWaveParameter(int index)
        {
            var waveHeightData = _metoceanData.Variables.WaveHeight.Data[index];
            var wavePeriodPeakData = _metoceanData.Variables.WavePeriodPeak.Data[index];
            var waveDirectionData = _metoceanData.Variables.WaveDirection.Data[index];

            var waveHeight = waveHeightData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.WaveHeight.Data[index], CultureInfo.InvariantCulture.NumberFormat);
            var wavePeriodPeak = wavePeriodPeakData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.WavePeriodPeak.Data[index], CultureInfo.InvariantCulture.NumberFormat); ;
            var waveDirection = waveDirectionData == string.Empty ? Vector2.zero : Utilities.DegreeToVector2(float.Parse(_metoceanData.Variables.WaveDirection.Data[index], CultureInfo.InvariantCulture.NumberFormat));
            return new Wave(waveHeight, wavePeriodPeak, waveDirection);
        }

        /// <summary>
        /// A float value representing the fraction of the sky obscured by clouds
        /// </summary>
        /// <returns>A cloud cover float value</returns>
        private float ReturnCloudCoverParameter(int index)
        {
            var cloudCoverData = _metoceanData.Variables.CloudCover.Data[index];
            return cloudCoverData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.CloudCover.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// A float value representing the product of both downwelling infrared energy as well as emission by the underlying surface
        /// </summary>
        /// <returns>A flux longwave float value</returns>
        private float ReturnFluxLongwaveParameter(int index)
        {
            var fluxLongwaveData = _metoceanData.Variables.RadiationFluxLongwave.Data[index];
            return fluxLongwaveData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.RadiationFluxLongwave.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// A float value representing the result of specular and diffuse reflection of incident shortwave radiation by the underlying surface
        /// </summary>
        /// <returns>A flux shortwave float value</returns>
        private float ReturnFluxShortwaveParameter(int index)
        {
            var fluxShortwaveData = _metoceanData.Variables.RadiationFluxShortwave.Data[index];
            return fluxShortwaveData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.RadiationFluxShortwave.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// A float value representing the temperature of sea water near the surface
        /// </summary>
        /// <returns>A sea surface temperature float value</returns>
        private float ReturnSeaSurfaceTemperatureParameter(int index)
        {
            var seaSurfaceTemperatureData = _metoceanData.Variables.SeaSurfaceTemperature.Data[index];
            return seaSurfaceTemperatureData == string.Empty ? noDataFloat : float.Parse(_metoceanData.Variables.SeaSurfaceTemperature.Data[index], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Gets the requested information's date and time and adds the UTC offset
        /// </summary>
        /// <returns>The date and time of the location</returns>
        private DateTime ConvertDateTime(TimeSpan UTCOffset)
        {
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            DateTime localTime = DateTime.Now;
            TimeSpan localOffset = localZone.GetUtcOffset(localTime);
            
            DateTime currentLocationTime = DateTime.Now;
            currentLocationTime = currentLocationTime.Add(UTCOffset - localOffset);

            return currentLocationTime;
        }

        /// <summary>
        /// Gets the requested information's UTC Offset based on it's time zone
        /// </summary>
        private TimeSpan ConvertUTCOffset(string timeZone)
        {
#if UNITY_STANDALONE || UNITY_IOS
            return Utilities.GetUTCOffsetData(timeZone);
#endif
#if UNITY_ANDROID
            return Utilities.GetUTCOffsetOnAndroid(timeZone);
#endif
        }
        #endregion
    }
}
