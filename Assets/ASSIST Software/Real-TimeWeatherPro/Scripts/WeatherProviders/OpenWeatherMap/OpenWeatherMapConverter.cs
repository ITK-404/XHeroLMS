//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using System;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class is used as a converter of OpenWeatherData structure to Real-Time Weather data structure
    /// </summary>

    public class OpenWeatherMapConverter
    {
        #region Const Members
        private const int kOneHundred = 100;
        private const int kRainIdentifier = 5;
        private const int kIndexOfHundredThunderstorm = 2;
        private const int kIndexOfHundredDrizzle = 3;
        private const int kIndexOfHundredRain = 5;
        private const int kIndexOfHundredSnow = 6;
        private const int kIndexOfHundredAtmosphere = 7;
        #endregion

        #region Private Variables
        private DateTime epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private OpenWeatherMapData weatherData;
        #endregion

        #region Constructors
        public OpenWeatherMapConverter(OpenWeatherMapData weatherData)
        {
            this.weatherData = weatherData;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Main functionality for converting OpenWeatherData structure to Real-Time Weather data structure
        /// </summary>
        /// <returns>An instance of WeatherData class with all data converted</returns>
        public WeatherData ConvertToRealTimeManagerWeatherData()
        {
            WeatherData RTWData = new WeatherData();

            RTWData.Localization = ReturnLocalizationParameter();
            RTWData.DateTime = ReturnDateTimeParameter();
            RTWData.Wind = ReturnWindParameter();
            RTWData.WeatherState = ReturnWeatherStateParameter();
            RTWData.UTCOffset = ReturnUTCOffsetParameter();
            RTWData.TimeZone = ReturnTimeZoneParameter();
            RTWData.Temperature = ReturnTemperatureParameter();
            RTWData.Pressure = ReturnPressureParameter();
            RTWData.Precipitation = ReturnPrecipitationParameter();
            RTWData.Humidity = ReturnHumidityParameter();
            RTWData.Dewpoint = ReturnDewpointParameter();
            RTWData.Visibility = ReturnVisibilityParameter();

            return RTWData;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates a new Localization instance with data about the country, city, latitude and longitude
        /// </summary>
        /// <returns>An instance of Localization class</returns>
        private Localization ReturnLocalizationParameter()
        {
            return new Localization(weatherData.System.Country, weatherData.CityName, (float)weatherData.GeographicCoordinates.Latitude, (float)weatherData.GeographicCoordinates.Longitude);
        }

        /// <summary>
        /// Creates a new DateTime structure with data about the date time
        /// </summary>
        /// <returns>An instance of a DateTime structure</returns>
        private DateTime ReturnDateTimeParameter()
        {
            var localTime = epochDateTime.AddSeconds(weatherData.UnixTimestamp);

            return localTime + new TimeSpan(0, 0, weatherData.TimeZone);
        }

        /// <summary>
        /// Creates a new Wind instance with data about the speed and direction of the wind
        /// </summary>
        /// <returns>An instance of Wind class</returns>
        private RealTimeWeather.Classes.Wind ReturnWindParameter()
        {
            Vector2 direction = Utilities.DegreeToVector2(weatherData.Wind.Deg);
            float speed = 0.0f;

            switch(weatherData.Units)
            {
                case Units.Standard:
                case Units.Metric:
                    speed = (float)Math.Round(Utilities.ConvertMeterPerSecondToKMPerHour(weatherData.Wind.Speed));
                    break;
                case Units.Imperial:
                    speed = (float)Math.Round(Utilities.ConvertMilePerHourToKMPerHour(weatherData.Wind.Speed));
                    break;
            }

            return new RealTimeWeather.Classes.Wind(direction, speed);
        }

        /// <summary>
        /// Creates a new WeatherState enum value representing the weather state
        /// </summary>
        /// <returns>A WeatherState enum value</returns>
        private Enums.WeatherState ReturnWeatherStateParameter()
        {
            if (weatherData.WeatherDetailsList.Count > 0)
            {
                //We choose first WeatherDetails from the list, even if can be more than one
                int weatherID = weatherData.WeatherDetailsList[0].ID;

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
        /// Creates a new Timespan structure representing the UTC offset of an geographic location
        /// </summary>
        /// <returns>An instance of a TimeSpan structure</returns>
        private TimeSpan ReturnUTCOffsetParameter()
        {
            return new TimeSpan(0, 0, weatherData.TimeZone);
        }

        /// <summary>
        /// Creates a new string representing the time zone(IANA) of a geographic location
        /// </summary>
        /// <returns>A time zone string value</returns>
        private string ReturnTimeZoneParameter()
        {
            return Utilities.GetTimeZone((float)weatherData.GeographicCoordinates.Latitude, (float)weatherData.GeographicCoordinates.Longitude);
        }

        /// <summary>
        /// Creates a new float value representing the current temperature
        /// </summary>
        /// <returns>A temperature float value</returns>
        private float ReturnTemperatureParameter()
        {
            switch (weatherData.Units)
            {
                case Units.Standard:
                    return (float)Math.Round(Utilities.ConvertKelvinToDegrees(weatherData.MainWeather.Temperature));
                case Units.Imperial:
                    return (float)Math.Round(Utilities.ConvertFahrenheitToDegrees(weatherData.MainWeather.Temperature));
                default:
                    return (float)Math.Round(weatherData.MainWeather.Temperature);
            }
        }

        /// <summary>
        /// Creates a new float value representing the current pressure of the air
        /// </summary>
        /// <returns>A pressure float value</returns>
        private float ReturnPressureParameter()
        {
            return weatherData.MainWeather.Pressure;
        }

        /// <summary>
        /// Create a float value representing an estimative precipitation value
        /// </summary>
        /// <returns>A precipitation float value</returns>
        private float ReturnPrecipitationParameter()
        {
            int ID = 0;
            for (int i = 0; i < weatherData.WeatherDetailsList.Count; i++)
            {
                if(weatherData.WeatherDetailsList[i].ID / kOneHundred == kRainIdentifier || weatherData.WeatherDetailsList[i].ID / kOneHundred == kIndexOfHundredSnow)
                {
                    ID = weatherData.WeatherDetailsList[i].ID;
                    break;
                }
            }

            if(ID != 0)
            {
                switch(ID)
                {
                    case (int)WeatherState.LightRain:                   return 4;
                    case (int)WeatherState.ModerateRain:                return 8;
                    case (int)WeatherState.HeavyIntensityRain:          return 12;
                    case (int)WeatherState.VeryHeavyRain:               return 16;
                    case (int)WeatherState.ExtremeRain:                 return 20;
                    case (int)WeatherState.FreezingRain:                return 4;
                    case (int)WeatherState.LightIntensityShowerRain:    return 6;
                    case (int)WeatherState.ShowerRain:                  return 9;
                    case (int)WeatherState.HeavyIntensityShowerRain:    return 15;
                    case (int)WeatherState.RaggedShowerRain:            return 10;
                    case (int)WeatherState.LightSnow:                   return 4;
                    case (int)WeatherState.Snow:                        return 8;
                    case (int)WeatherState.HeavySnow:                   return 12;
                    case (int)WeatherState.Sleet:                       return 6;
                    case (int)WeatherState.LightShowerSleet:            return 4;
                    case (int)WeatherState.ShowerSleet:                 return 6;
                    case (int)WeatherState.LightRainAndSnow:            return 4;
                    case (int)WeatherState.RainAndSnow:                 return 6;
                    case (int)WeatherState.LightShowerSnow:             return 4;
                    case (int)WeatherState.ShowerSnow:                  return 6;
                    case (int)WeatherState.HeavyShowerSnow:             return 12;
                }
            }

            return 0.0f;
        }

        /// <summary>
        /// Creates a float value representing the current humidity in the air
        /// </summary>
        /// <returns>A humidity float value</returns>
        private float ReturnHumidityParameter()
        {
            return weatherData.MainWeather.Humidity;
        }

        /// <summary>
        /// Creates a float value representing calculated dewpoint based on temperature and humidity
        /// </summary>
        /// <returns>A dewpoint float value</returns>
        private float ReturnDewpointParameter()
        {
            float temperature = ReturnTemperatureParameter();

            return (float)Utilities.CalculateDewpoint(temperature, weatherData.MainWeather.Humidity);
        }

        /// <summary>
        /// Creates a float value representing the visibility at which can be clearly discerned
        /// </summary>
        /// <returns>A visibility float value</returns>
        private float ReturnVisibilityParameter()
        {
            return (float)Math.Round(Utilities.ConvertMetersToKilometers(weatherData.Visibility));
        }
#endregion
    }
}