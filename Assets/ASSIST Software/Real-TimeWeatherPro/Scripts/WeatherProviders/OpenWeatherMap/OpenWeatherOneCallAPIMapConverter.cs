//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using RealTimeWeather.Data;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class is used as a converter of OpenWeatherData structure to Real-Time Weather data structure
    /// </summary>

    public class OpenWeatherOneCallAPIMapConverter
    {
        #region Const Members
        private const int kOneHundred = 100;
        private const int kRainIdentifier = 5;
        private const int kIndexOfHundredThunderstorm = 2;
        private const int kIndexOfHundredDrizzle = 3;
        private const int kIndexOfHundredRain = 5;
        private const int kIndexOfHundredSnow = 6;
        private const int kIndexOfHundredAtmosphere = 7;
        private const int kMaxDefaultVisibility = 20000; // This API call type does not return a measured visibility so we need to assign a default value.
        #endregion

        #region Private Variables
        private DateTime epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private OpenWeatherOneCallAPIMapData weatherData;
        private OpenWeatherHistoryCallApiMapData historyWeatherData;
        #endregion

        #region Constructors
        public OpenWeatherOneCallAPIMapConverter(OpenWeatherOneCallAPIMapData weatherData)
        {
            this.weatherData = weatherData;
        }

        public OpenWeatherOneCallAPIMapConverter(OpenWeatherHistoryCallApiMapData openWeatherHistoryData)
        {
            
            historyWeatherData = openWeatherHistoryData;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Main functionality for converting OpenWeatherOneCallAPIMapData Hourly structure to Real-Time Weather data structure
        /// </summary>
        /// <returns>An array of WeatherData class with all data converted</returns>
        public List<WeatherData> ConvertHourlyWeatherDataToRealTimeManagerWeatherListData()
        {
            List<WeatherData> rtwDataList = new List<WeatherData>();

            foreach (var item in weatherData.HourlyWeather)
            {
                var rtwData = new WeatherData
                {
                    Localization = ReturnLocalizationParameter(),
                    DateTime = ReturnDateTimeParameter(item.UnixTimestamp, weatherData.TimezoneOffset),
                    Wind = ReturnWindParameter(item.WindSpeed, item.WindDeg),
                    WeatherState = ReturnWeatherStateParameter(item.WeatherDetailsList),
                    UTCOffset = ReturnUTCOffsetParameter(),
                    TimeZone = ReturnTimeZoneParameter(),
                    Temperature = ReturnTemperatureParameter(item.Temperature),
                    Pressure = ReturnPressureParameter(item.Pressure),
                    Precipitation = ReturnPrecipitationParameter(item.WeatherDetailsList),
                    Humidity = ReturnHumidityParameter(item.Humidity),
                    Dewpoint = ReturnDewpointParameter(item.DewPoint),
                    Visibility = ReturnVisibilityParameter(item.Visibility)
                };
                rtwDataList.Add(rtwData);
            }

            return rtwDataList;
        }

        /// <summary>
        /// Main functionality for converting OpenWeatherOneCallAPIMapData Daily structure to Real-Time Weather data structure
        /// </summary>
        /// <returns>An array of WeatherData class with all data converted</returns>
        public List<WeatherData> ConvertDailyWeatherDataToRealTimeManagerWeatherListData()
        {
            List<WeatherData> rtwDataList = new List<WeatherData>();

            foreach (var item in weatherData.DailyWeather)
            {
                var rtwData = new WeatherData
                {
                    Localization = ReturnLocalizationParameter(),
                    DateTime = ReturnDateTimeParameter(item.UnixTimestamp, weatherData.TimezoneOffset),
                    Wind = ReturnWindParameter(item.WindSpeed, item.WindDeg),
                    WeatherState = ReturnWeatherStateParameter(item.WeatherDetailsList),
                    UTCOffset = ReturnUTCOffsetParameter(),
                    TimeZone = ReturnTimeZoneParameter(),
                    Temperature = ReturnTemperatureParameter(item.Temperature.DayTemperature),
                    Pressure = ReturnPressureParameter(item.Pressure),
                    Precipitation = ReturnPrecipitationParameter(item.WeatherDetailsList),
                    Humidity = ReturnHumidityParameter(item.Humidity),
                    Dewpoint = ReturnDewpointParameter(item.DewPoint),
                    Visibility = ReturnVisibilityParameter(kMaxDefaultVisibility)
                };
                rtwDataList.Add(rtwData);
            }

            return rtwDataList;
        }

        public List<WeatherData> ConvertHistoryWeatherDataToRealTimeManagerWeatherListData()
        {
            List<WeatherData> rtwDataList = new List<WeatherData>();

            foreach (var item in historyWeatherData.WeatherData)
            {
                var rtwData = new WeatherData
                {
                    Localization = ReturnHistoryLocalizationParameter(),
                    DateTime = ReturnDateTimeParameter(item.UnixTimestamp, 0),
                    Wind = ReturnHistoryWindParameter(item.WindSpeed, item.WindDeg),
                    WeatherState = ReturnWeatherStateParameter(item.WeatherDetailsList),
                    UTCOffset = new TimeSpan(0, 0, 0),
                    TimeZone = Utilities.GetTimeZone((float)historyWeatherData.Latitude, (float)historyWeatherData.Longitude),
                    Temperature = (float)Math.Round(item.Temperature),
                    Pressure = ReturnPressureParameter(item.Pressure),
                    Precipitation = ReturnPrecipitationParameter(item.WeatherDetailsList),
                    Humidity = ReturnHumidityParameter(item.Humidity),
                    Dewpoint = ReturnDewpointParameter(item.DewPoint),
                    Visibility = ReturnVisibilityParameter(item.Visibility),
                };
                rtwDataList.Add(rtwData);
            }

            return rtwDataList;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates a new Localization instance with data about the country, city, latitude and longitude
        /// </summary>
        /// <returns>An instance of Localization class</returns>
        private Localization ReturnLocalizationParameter()
        {
            var location = weatherData.TimeZone.Split('/');
            return new Localization(location[0], location[1], (float)weatherData.Latitude, (float)weatherData.Longitude);
        }

        private Localization ReturnHistoryLocalizationParameter()
        {
            var location = historyWeatherData.TimeZone.Split('/');
            return new Localization(location[0], location[1], (float)historyWeatherData.Latitude, (float)historyWeatherData.Longitude);
        }

        /// <summary>
        /// Creates a new Timespan structure representing the UTC offset of an geographic location
        /// </summary>
        /// <returns>An instance of a TimeSpan structure</returns>
        private TimeSpan ReturnUTCOffsetParameter()
        {
            return new TimeSpan(0, 0, weatherData.TimezoneOffset);
        }

        /// <summary>
        /// Creates a new string representing the time zone(IANA) of a geographic location
        /// </summary>
        /// <returns>A time zone string value</returns>
        private string ReturnTimeZoneParameter()
        {
            return Utilities.GetTimeZone((float)weatherData.Latitude, (float)weatherData.Longitude);
        }

        /// <summary>
        /// Creates a new DateTime structure with data about the date time
        /// </summary>
        /// <returns>An instance of a DateTime structure</returns>
        private DateTime ReturnDateTimeParameter(int timestamp, int timezone_offset)
        {
            var localTime = epochDateTime.AddSeconds(timestamp);
            return localTime + new TimeSpan(0, 0, timezone_offset);
        }

        /// <summary>
        /// Creates a new Wind instance with data about the speed and direction of the wind
        /// </summary>
        /// <returns>An instance of Wind class</returns>
        private RealTimeWeather.Classes.Wind ReturnWindParameter(float wind_speed, int wind_deg)
        {
            Vector2 direction = Utilities.DegreeToVector2(wind_deg);
            float speed = 0.0f;

            switch (weatherData.Units)
            {
                case Units.Standard:
                case Units.Metric:
                    speed = (float)Math.Round(Utilities.ConvertMeterPerSecondToKMPerHour(wind_speed));
                    break;
                case Units.Imperial:
                    speed = (float)Math.Round(Utilities.ConvertMilePerHourToKMPerHour(wind_speed));
                    break;
            }

            return new RealTimeWeather.Classes.Wind(direction, speed);
        }

        private RealTimeWeather.Classes.Wind ReturnHistoryWindParameter(float wind_speed, int wind_deg)
        {
            Vector2 direction = Utilities.DegreeToVector2(wind_deg);
            float speed = 0.0f;
            speed = (float)Math.Round(Utilities.ConvertMeterPerSecondToKMPerHour(wind_speed));

            return new RealTimeWeather.Classes.Wind(direction, speed);
        }

    /// <summary>
    /// Creates a new WeatherState enum value representing the weather state
    /// </summary>
    /// <returns>A WeatherState enum value</returns>
    private Enums.WeatherState ReturnWeatherStateParameter(List<WeatherDetails> weather)
    {
        if (weather.Count > 0)
        {
            //We choose first WeatherDetails from the list, even if can be more than one
            int weatherID = weather[0].ID;

            if (weatherID == (int)WeatherState.Clear) return Enums.WeatherState.Clear;
            else if (weatherID == (int)WeatherState.FewClouds) return Enums.WeatherState.PartlyClear;
            else if (weatherID == (int)WeatherState.ScaterredClouds) return Enums.WeatherState.Fair;
            else if (weatherID == (int)WeatherState.BrokenClouds) return Enums.WeatherState.PartlyCloudy;
            else if (weatherID == (int)WeatherState.OvercastClouds) return Enums.WeatherState.Cloudy;

            int identifier = weatherID / kOneHundred;
            switch (identifier)
            {
                case kIndexOfHundredThunderstorm:
                    return Enums.WeatherState.Thunderstorms;
                case kIndexOfHundredDrizzle:
                case kIndexOfHundredRain:
                    return Enums.WeatherState.RainPrecipitation;
                case kIndexOfHundredSnow:
                    return Enums.WeatherState.SnowPrecipitation;
                case kIndexOfHundredAtmosphere:
                    return Enums.WeatherState.Mist;
                default:
                    return Enums.WeatherState.Clear;
            }
        }
        else
        {
            return Enums.WeatherState.Clear;
        }
    }


    /// <summary>
    /// Creates a new float value representing the current temperature
    /// </summary>
    /// <returns>A temperature float value</returns>
    private float ReturnTemperatureParameter(double temperature)
    {
        switch (weatherData.Units)
        {
            case Units.Standard:
                return (float)Math.Round(Utilities.ConvertKelvinToDegrees(temperature));
            case Units.Imperial:
                return (float)Math.Round(Utilities.ConvertFahrenheitToDegrees(temperature));
            default:
                return (float)Math.Round(temperature);
        }
    }

    /// <summary>
    /// Creates a new float value representing the current pressure of the air
    /// </summary>
    /// <returns>A pressure float value</returns>
    private float ReturnPressureParameter(int pressure)
    {
        return pressure;
    }

    /// <summary>
    /// Create a float value representing an estimative precipitation value
    /// </summary>
    /// <returns>A precipitation float value</returns>
    private float ReturnPrecipitationParameter(List<WeatherDetails> weather)
    {
        int ID = 0;
        for (int i = 0; i < weather.Count; i++)
        {
            if (weather[i].ID / kOneHundred == kRainIdentifier || weather[i].ID / kOneHundred == kIndexOfHundredSnow)
            {
                ID = weather[i].ID;
                break;
            }
        }

        if (ID != 0)
        {
            switch (ID)
            {
                case (int)WeatherState.LightRain: return 4;
                case (int)WeatherState.ModerateRain: return 8;
                case (int)WeatherState.HeavyIntensityRain: return 12;
                case (int)WeatherState.VeryHeavyRain: return 16;
                case (int)WeatherState.ExtremeRain: return 20;
                case (int)WeatherState.FreezingRain: return 4;
                case (int)WeatherState.LightIntensityShowerRain: return 6;
                case (int)WeatherState.ShowerRain: return 9;
                case (int)WeatherState.HeavyIntensityShowerRain: return 15;
                case (int)WeatherState.RaggedShowerRain: return 10;

                case (int)WeatherState.LightSnow: return 4;
                case (int)WeatherState.Snow: return 8;
                case (int)WeatherState.HeavySnow: return 12;
                case (int)WeatherState.Sleet: return 6;
                case (int)WeatherState.LightShowerSleet: return 4;
                case (int)WeatherState.ShowerSleet: return 6;
                case (int)WeatherState.LightRainAndSnow: return 4;
                case (int)WeatherState.RainAndSnow: return 6;
                case (int)WeatherState.LightShowerSnow: return 4;
                case (int)WeatherState.ShowerSnow: return 6;
                case (int)WeatherState.HeavyShowerSnow: return 12;
            }
        }

        return 0.0f;
    }

    /// <summary>
    /// Creates a float value representing the current humidity in the air
    /// </summary>
    /// <returns>A humidity float value</returns>
    private float ReturnHumidityParameter(int humidity)
    {
        return humidity;
    }

    /// <summary>
    /// Creates a float value representing calculated dewpoint based on temperature and humidity
    /// </summary>
    /// <returns>A dewpoint float value</returns>
    private float ReturnDewpointParameter(double dew_point)
    {
        return (float)dew_point;
    }

    /// <summary>
    /// Creates a float value representing the visibility at which can be clearly discerned
    /// </summary>
    /// <returns>A visibility float value</returns>
    private float ReturnVisibilityParameter(int visibility)
    {
        return (float)Math.Round(Utilities.ConvertMetersToKilometers(visibility));
    }
    #endregion
}
}