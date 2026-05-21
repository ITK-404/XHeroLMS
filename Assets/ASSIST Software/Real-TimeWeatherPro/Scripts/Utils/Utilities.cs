// 
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;
using NodaTime;
using NodaTime.TimeZones;
using GeoTimeZone;
using TimeZoneConverter;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using System.Text.RegularExpressions;
using RealTimeWeather.Data;
using System.Collections.Generic;
using System.Globalization;

namespace RealTimeWeather
{
    public class Utilities
    {
        #region Const Members
        private const string kStandardTime = "GMT Standard Time";
        private const double kAbsoluteKelvinTemperature = 273.15d;
        private const double kDifferenceFahrToCelsius = 32.0d;
        private const double kFahrToCelsius = 5.0d / 9.0d;
        private const double kKilometerPerHourFromMS = 3.6d;
        private const double kKilometerPerHourFromMH = 1.60934d;
        private const double kOneKMToMeters = 1000.0f;
        private const float kOneHundred = 100.0f;
        private const float kMagnusCoeff = 17.625f;
        private const float kMagnusCoeff2 = 243.04f;
        public const float kMaxLatitudeValue = 90.0f;
        public const float kMinLatitudeValue = -90.0f;
        public const float kMaxLongitudeValue = 180.0f;
        public const float kMinLongitudeValue = -180.0f;
        public const float kMinSimulationSpeedValue = 1.0f;
        public const float kMaxSimulationSpeedValue = 600.0f;
        public const string kMinutesStr = ":00";
        public const int kMaxNameLength = 100;
        private static string[] _dateFormats = { "dd/M/yyyy hh:mm:ss tt", "dd/M/yyyy h:mm:ss tt", "dd/M/yyyy h:mm:ss tt", "dd/M/yyyy h:mm:ss tt", "d/M/yyyy hh:mm:ss tt", "d/M/yyyy h:mm:ss tt", "d/M/yyyy h:mm:ss tt", "d/M/yyyy h:mm:ss tt" };
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Converts a degree to Vector2.
        /// </summary>
        /// <param name="degree">A float value that represents the degree.</param>
        public static Vector2 DegreeToVector2(float degree)
        {
            return RadianToVector2(degree * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Converts a Vector2 to a degree
        /// </summary>
        /// <param name="vector">A Vector2 value that is converted to a float representing degrees</param>
        /// <returns>The vector converted to degrees</returns>
        public static float Vector2ToDegree(Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * 180 / Mathf.PI;
        }

        /// <summary>
        /// Converts Hectopascals to Pascals
        /// </summary>
        /// <param name="hPa">A float value that is converted to SI value</param>
        public static float ConvertHectoPascalsToPascals(float hPa)
        {
            return hPa * kOneHundred;
        }

        public static float ConvertHectoPascalsToMiliBars(float hPa)
        {
            return hPa / kOneHundred;
        }

        /// <summary>
        /// This method calculates the dewpoint temperature based on temperature and humidity
        /// </summary>
        /// <param name="airTemperature">The temperature in degrees units</param>
        /// <param name="airHumidity">The humidity represented in percentages</param>
        /// <returns>Dewpoint represented in celsius degrees</returns>
        public static float CalculateDewpoint(float airTemperature, float airHumidity)
        {
            return ((airTemperature * kMagnusCoeff) / (airTemperature + kMagnusCoeff2) + Mathf.Log(airHumidity / 100f))
                * kMagnusCoeff2 / (kMagnusCoeff - ((airTemperature * kMagnusCoeff) / (airTemperature + kMagnusCoeff2) + (Mathf.Log(airHumidity) / 100f)));
        }

        /// <summary>
        /// Converts a radian to Vector2.
        /// </summary>
        /// <param name="radian">A float value that represents the radian.</param>
        public static Vector2 RadianToVector2(float radian)
        {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }

        /// <summary>
        /// Calculates the direction of vector 2 (used for calculating the wind direction)
        /// </summary>
        /// <param name="vector2">The Vector2 represented as X/Y</param>
        /// <returns>The direction represented in degrees</returns>
        public static float Vector2ToDegrees(Vector2 vector2)
        {
            float radians = (float)Math.Atan2(vector2.x, vector2.y);

            float degreesDirection = radians * (float)(180 / Math.PI);
            if (degreesDirection < 0)
            {
                degreesDirection += 360;
            }
            return degreesDirection;
        }

        /// <summary>
        /// This method obtains the time zone data from longitude and latitude.
        /// </summary>
        /// <param name="locationLatitude">Latitude data calculated</param>
        /// <param name="locationLongitude">Longitude data calculated</param>
        /// <returns>The current time zone in format "Continent/Region"</returns>
        public static string GetTimeZone(float locationLatitude, float locationLongitude)
        {
            string ianaTimeZoneResult = TimeZoneLookup.GetTimeZone(locationLatitude, locationLongitude).Result;
            string resultedTimeZone = string.Empty;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            try
            {
                resultedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneResult).DisplayName;
            }
            catch (Exception)
            {
                resultedTimeZone = kStandardTime;
            }
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID || UNITY_IOS
            try
            {
                resultedTimeZone = TZConvert.IanaToWindows(ianaTimeZoneResult);
            }
            catch (Exception)
            {
                resultedTimeZone = kStandardTime;
            }
#endif
            return resultedTimeZone;
        }

        /// <summary>
        /// This method parses the UTC offset data.
        /// </summary>
        /// <param name="timeZone">Time zone data</param>
        /// <returns>The current utc offset in hours</returns>
        public static TimeSpan GetUTCOffsetData(string timeZone)
        {
            TimeZoneInfo timeZoneInfo;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID || UNITY_IOS
            timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
#endif

            return timeZoneInfo.GetUtcOffset(DateTime.Now);
        }

        /// <summary>
        /// This method calculates the utc offset data.
        /// </summary>
        /// <param name="timeZoneWindowsId">Time zone data windows ID. For example 'GTB Standard Time'.</param>
        /// <returns>The current utc offset in hours:minutes</returns>
        public static TimeSpan GetUTCOffsetOnAndroid(string timeZoneWindowsId)
        {
            Offset offset = new Offset();
            string tzdbId;

            if (!TzdbDateTimeZoneSource.Default.WindowsMapping.PrimaryMapping.TryGetValue(timeZoneWindowsId, out tzdbId))
            {
                return offset.ToTimeSpan();
            }

            DateTimeZone timeZoneDate = TzdbDateTimeZoneSource.Default.ForId(tzdbId);
            IClock systemClock = SystemClock.Instance;
            offset = timeZoneDate.GetUtcOffset(systemClock.GetCurrentInstant());

            return offset.ToTimeSpan();
        }

        /// <summary>
        /// This method converts Kelvin units in Degrees units
        /// </summary>
        /// <param name="kelvinValue">The value in Kelvin units</param>
        /// <returns>A new value in Degrees units</returns>
        public static double ConvertKelvinToDegrees(double kelvinValue)
        {
            return kelvinValue - kAbsoluteKelvinTemperature;
        }

        /// <summary>
        /// This method converts Fahrenheit units in Degrees units
        /// </summary>
        /// <param name="fahrenheitValue">The value in Fahrenheit units</param>
        /// <returns>A new value in Degrees units</returns>
        public static double ConvertFahrenheitToDegrees(double fahrenheitValue)
        {
            return (fahrenheitValue - kDifferenceFahrToCelsius) * kFahrToCelsius;
        }

        /// <summary>
        /// This method converts {meters per second} units in {kilometers per hour} units
        /// </summary>
        /// <param name="metersPerSecond">The value in {meters per second} units</param>
        /// <returns>A new value in {kilometers per hour} units</returns>
        public static double ConvertMeterPerSecondToKMPerHour(double metersPerSecond)
        {
            return metersPerSecond * kKilometerPerHourFromMS;
        }

        /// <summary>
        /// This method converts {miles per hour} units in {kilometers per hour} units
        /// </summary>
        /// <param name="milesPerHour">The value in {miles per hour} units</param>
        /// <returns>A new value in {kilometers per hour} units</returns>
        public static double ConvertMilePerHourToKMPerHour(double milesPerHour)
        {
            return milesPerHour * kKilometerPerHourFromMH;
        }

        /// <summary>
        /// This method converts meters units in kilometers units
        /// </summary>
        /// <param name="meters">The value in meters units</param>
        /// <returns>A new value in kilometers units</returns>
        public static double ConvertMetersToKilometers(double meters)
        {
            return meters / kOneKMToMeters;
        }

        /// <summary>
        /// This method converts kilometers units in meters units
        /// </summary>
        /// <param name="kilometers">The value in kilometers units</param>
        /// <returns>A new value in meters units</returns>
        public static double ConvertKilometersToMeters(double kilometers)
        {
            return kilometers * kOneKMToMeters;
        }

        /// <summary>
        /// Returns a weather state based on the cloud cover percentage
        /// </summary>
        public static WeatherState ReturnWeatherStateBasedCloudCover(string cloudCover, string precipitation)
        {
            float cloudCoverFloat;
            float precipitationRate;
            try
            {
                cloudCoverFloat = float.Parse(cloudCover);
                precipitationRate = float.Parse(precipitation);
            }
            catch (Exception)
            {
                return WeatherState.Clear;
            }

            if (cloudCoverFloat < 5)
            {
                return WeatherState.Clear;
            }
            if (cloudCoverFloat < 10)
            {
                return WeatherState.Fair;
            }
            if (cloudCoverFloat < 40)
            {
                return WeatherState.PartlyCloudy;
            }
            if (cloudCoverFloat < 70)
            {
                return WeatherState.PartlyClear;
            }
            if (cloudCoverFloat <= 101 && precipitationRate > 1.5f)
            {
                return WeatherState.RainPrecipitation;
            }
            if (cloudCoverFloat <= 101)
            {
                return WeatherState.Cloudy;
            }

            return WeatherState.Clear;
        }

        /// <summary>
        /// Obtains date time based on latitude and longitude
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <returns>The date time of the interrogated latitude and longitude</returns>
        public static DateTime GetDateTimeBasedOnGeocoordinates(float latitude, float longitude)
        {
            var timeZone = Utilities.GetTimeZone(latitude, longitude);
#if UNITY_STANDALONE || UNITY_IOS
            var UTCOffset = Utilities.GetUTCOffsetData(timeZone);
#endif
#if UNITY_ANDROID
            var UTCOffset = Utilities.GetUTCOffsetOnAndroid(timeZone);
#endif

            TimeZoneInfo localZone = TimeZoneInfo.Local;
            DateTime localTime = DateTime.Now;
            TimeSpan localOffset = localZone.GetUtcOffset(localTime);

            DateTime currentLocationTime = DateTime.Now;
            currentLocationTime = currentLocationTime.Add(UTCOffset - localOffset);

            return currentLocationTime;
        }

        /// <summary>
        /// Rounds the value to two decimals
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The rounded value</returns>
        public static float ToTwoDecimals(float value)
        {
            return Mathf.Round(value * 100) / 100;
        }

        /// <summary>
        /// Creates a new WeatherData instance
        /// </summary>
        /// <param name="weatherData">WeatherData instance to be duplicated</param>
        /// <returns>The new WeatherData instance</returns>
        public static WeatherData NewWeatherDataInstance(WeatherData weatherData)
        {
            WeatherData newWeatherData = new WeatherData();
            newWeatherData.Localization = new Localization(weatherData.Localization.Country, weatherData.Localization.City, weatherData.Localization.Latitude, weatherData.Localization.Longitude);
            newWeatherData.DateTime = new DateTime(weatherData.DateTime.Ticks);
            newWeatherData.Wind = new Wind(weatherData.Wind.Direction, weatherData.Wind.Speed);
            newWeatherData.UTCOffset = new TimeSpan(weatherData.UTCOffset.Ticks);
            newWeatherData.Precipitation = weatherData.Precipitation;
            newWeatherData.WeatherState = weatherData.WeatherState;
            newWeatherData.Temperature = weatherData.Temperature;
            newWeatherData.Visibility = weatherData.Visibility;
            newWeatherData.Pressure = weatherData.Pressure;
            newWeatherData.Humidity = weatherData.Humidity;
            newWeatherData.Dewpoint = weatherData.Dewpoint;
            newWeatherData.IndexUV = weatherData.IndexUV;
            return newWeatherData;
        }

        /// <summary>
        /// Calculates the sunrise/sunset angle which is used to determine the sunrise/sunset times by adding the returned angle to the solar noon
        /// </summary>
        public static double CalculateSunriseSunsetAngle(float latitude, int dayOfYear)
        {
            double declinationAngle = 23.45 * Math.Sin((284f + dayOfYear) * 0.9863014f * Mathf.Deg2Rad);
            double sunriseSunsetAngle = Math.Acos(-Math.Tan(latitude * Mathf.Deg2Rad) * Math.Tan(declinationAngle * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

            return sunriseSunsetAngle;
        }

        /// <summary>
        /// Calculates the sunrise time based on the latitude and the given day of the year (as a number)
        /// </summary>
        public static TimeSpan CalculateSunriseTime(float latitude, int dayOfYear)
        {
            double sunriseAngle = CalculateSunriseSunsetAngle(latitude, dayOfYear);
            double sunrise = 12f - sunriseAngle / 15f;
            TimeSpan sunriseTime = new TimeSpan((int)Math.Floor(sunrise), (int)((sunrise - Math.Floor(sunrise)) * 60), 0);

            return sunriseTime;
        }

        /// <summary>
        /// Calculates the sunset time based on the latitude and the given day of the year (as a number)
        /// </summary>
        public static TimeSpan CalculateSunsetTime(float latitude, int dayOfYear)
        {
            double sunsetAngle = CalculateSunriseSunsetAngle(latitude, dayOfYear);
            double sunset = 12f + sunsetAngle / 15f;
            TimeSpan sunsetTime = new TimeSpan((int)Math.Floor(sunset), (int)((sunset - Math.Floor(sunset)) * 60), 0);

            return sunsetTime;
        }

        /// <summary>
        /// Calculates the total daylight hours based on the latitude and the given day of the year (as a number)
        /// </summary>
        public static TimeSpan CalculateDaylightHours(float latitude, int dayOfYear)
        {
            double sunriseSunsetAngle = CalculateSunriseSunsetAngle(latitude, dayOfYear);
            double daylightTime = 2f / 15f * sunriseSunsetAngle;
            TimeSpan daylightHours = new TimeSpan((int)Math.Floor(daylightTime), (int)((daylightTime - Math.Floor(daylightTime)) * 60), 0);

            return daylightHours;
        }

        /// <summary>
        /// Returns true if the given latitude does not specify any polar surface
        /// </summary>
        public static bool IsOutsidePolarRegion(float latitude)
        {
            if (latitude >= -67.5f && latitude <= 67.5f)
            {
                return true;
            }

            return false;
        }

        public static WeatherData ForecastDataToWeatherData(ForecastWeatherData forecastData, bool useCustomDateTime = false)
        {

            WeatherData weatherData = new WeatherData();
            weatherData.Localization = new Localization(forecastData.forecastLocation.forecastCountry,
                                                        forecastData.forecastLocation.forecastCity,
                                                        forecastData.forecastLocation.forecastLatitude,
                                                        forecastData.forecastLocation.forecastLongitude);
            DateTime dateTime;

            if (!useCustomDateTime)
            {
                if (DateTime.TryParseExact(forecastData.forecastDateTime, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                {
                    weatherData.DateTime = dateTime;
                }
            }
            else if (DateTime.TryParse(forecastData.forecastDateTime, out dateTime))
            {
                weatherData.DateTime = dateTime;
            }


            weatherData.Wind = new Wind(forecastData.forecastWind.forecastDirection, forecastData.forecastWind.forecastSpeed);
            weatherData.WeatherState = forecastData.forecastWeatherState;
            weatherData.Temperature = forecastData.forecastTemperature;
            weatherData.Pressure = forecastData.forecastPressure;
            weatherData.Precipitation = forecastData.foreacastPrecipitation;
            weatherData.Humidity = forecastData.forecastHumidity;
            weatherData.Dewpoint = forecastData.forecastDewpoint;
            weatherData.Visibility = forecastData.forecastVisibility;
            weatherData.IndexUV = forecastData.forecastIndexUV;
            weatherData.TimeZone = forecastData.forecastTimeZone;
            weatherData.UTCOffset = TimeSpan.Parse(forecastData.UTCOffset);
            return weatherData;
        }

        public static List<ForecastWeatherData> ConvertWeatherDataToForecastData(List<WeatherData> weatherDatas)
        {
            var list = new List<ForecastWeatherData>();
            foreach (var data in weatherDatas)
            {
                list.Add(new ForecastWeatherData(data));
            }

            return list;
        }

        /// <summary>
        /// Removes the elements from the regular expression and sets the next character to uppercase 
        /// </summary>
        /// <param name="phrase">The string to be modified</param>
        /// <param name="regex">The characters that will be removed from the string (as long as they are not part of a number) and followed by an uppercase letter</param>
        /// <returns>The converted string</returns>
        public static string ConvertStrongFormatToSimpleCase(string phrase, Regex regex)
        {
            // Split the string whenever a character from the regex is found
            string[] splitPhrase = regex.Split(phrase);
            var sb = new System.Text.StringBuilder();
            // Adds the first split string to the string builder
            sb.Append(splitPhrase[0]);
            splitPhrase[0] = string.Empty;
            //Get every split phrase
            foreach (string s in splitPhrase)
            {
                char[] splitPhraseChars = s.ToCharArray();
                if (splitPhraseChars.Length > 0)
                {
                    // Any letter character that follows a character from the regex will be uppercased
                    splitPhraseChars[0] = ((new string(splitPhraseChars[0], 1)).ToUpper().ToCharArray())[0];

                    // If the split string is a character from the regex and it's part of a number, or it's simply a phrase that follows a character from the regex, it will be added to the string builder
                    // If the split string is not a character from the regex and it's not part of a number, it won't be added to the final string
                    if (sb.Length != 0 && Char.IsLetter(sb[sb.Length - 1]) && !Char.IsDigit(splitPhraseChars[0]) && !Char.IsLetter(splitPhraseChars[0]) && splitPhraseChars.Length <= 1)
                    {
                        continue;
                    }
                }
                sb.Append(new string(splitPhraseChars));
            }
            return sb.ToString();
        }
        #endregion
    }
}