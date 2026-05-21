// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;

namespace RealTimeWeather.UI
{
    public class UIUtilities
    {
        #region Const Members
        private const string kTwoDecimalsFloatFormat = "F2";
        private const string kLatStr = "lat ";
        private const string kLongStr = " / long ";
        private const string kDirectionStr = "direction ";
        private const string kSeparatorStr = ", ";
        private const string kCelsiusDegreeStr = " °C";
        private const string kPercentStr = " %";
        private const string kMbarStr = " mbar";
        private const string kMmStr = " mm";
        private const string kKmStr = " km";
        private const string kKmPerHourStr = " km/h";
        private const string kWindDataStr = "Wind Data: ";
        private const string kHumidityStr = "Humidity: ";
        private const string kDewpointStr = "Dewpoint: ";
        private const string kPressureStr = "Pressure: ";
        private const string kPrecipitationStr = "Precipitation: ";
        private const string kVisibilityStr = "Visibility: ";
        private const string kUVIndexStr = "UV index: ";
        private const string kUTCOffsetStr = "UTC offset: ";
        private const string kNotAvailable = "Not available";
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Return the exact date time when the request has been made.
        /// </summary>
        /// <param name="dateTime">A DateTime value that represents the time when the request has been made.</param>
        public static string ReturnDateTimeInfo(DateTime dateTime)
        {
            return dateTime.Date.ToLongDateString();
        }

        /// <summary>
        /// Return the localization information, city, and country.
        /// </summary>
        /// <param name="location">A string value that represents the city.</param>
        /// <param name="country">A string value that represents the country.</param>
        public static string ReturnLocalizationInfo(string location, string country)
        {
            if (!string.IsNullOrEmpty(country))
            {
                return location.ToUpper() + kSeparatorStr + country.ToUpper();
            }

            return null;
        }

        /// <summary>
        /// Return geographic coordinates information, latitude and longitude.
        /// </summary>
        /// <param name="localization">A Localization class instance that represents the localization data.</param>
        public static string ReturnGeoCoordinatesInfo(Localization localization)
        {
            return kLatStr
                        + localization.Latitude.ToString()
                        + kLongStr
                        + localization.Longitude.ToString();
        }

        /// <summary>
        /// Return the precipitation information.
        /// </summary>
        /// <param name="precipitation">A float value that represents the precipitation in mm.</param>
        public static string ReturnPrecipitationInfo(float precipitation)
        {
            return kPrecipitationStr + precipitation.ToString(kTwoDecimalsFloatFormat) + kMmStr;
        }

        /// <summary>
        /// Return the pressure information.
        /// </summary>
        /// <param name="pressure">A float value that represents the pressure in mbar.</param>
        public static string ReturnPressureInfo(float pressure)
        {
            return kPressureStr + pressure.ToString(kTwoDecimalsFloatFormat) + kMbarStr;
        }

        /// <summary>
        /// Return the humidity information.
        /// </summary>
        /// <param name="humidity">A float value that represents the humidity in percent.</param>
        public static string ReturnHumidityInfo(float humidity)
        {
            return kHumidityStr + humidity.ToString(kTwoDecimalsFloatFormat) + kPercentStr;
        }

        /// <summary>
        /// Return the wind information.
        /// </summary>
        /// <param name="wind">A Wind class instance that represents wind data.</param>
        public static string ReturnWindInfo(Wind wind)
        {
            return kWindDataStr + wind.Speed.ToString(kTwoDecimalsFloatFormat) + kKmPerHourStr + kSeparatorStr +
                kDirectionStr + "(" + wind.Direction.x.ToString(kTwoDecimalsFloatFormat) + kSeparatorStr + wind.Direction.y.ToString(kTwoDecimalsFloatFormat) + ")";
        }

        /// <summary>
        /// Return the visibility information.
        /// </summary>
        /// <param name="visibility">A float value that represents the visibility in km.</param>
        public static string ReturnVisibilityInfo(float visibility)
        {
            return kVisibilityStr + visibility.ToString(kTwoDecimalsFloatFormat) + kKmStr;
        }

        /// <summary>
        /// Return the weather state information.
        /// </summary>
        /// <param name="temperature">The temperature data.</param>
        /// <param name="dateTime">The current date time.</param>
        public static string ReturnWeatherStateInfo(WeatherState weatherState, float temperature, DateTime dateTime)
        {
            return weatherState.ToString()
                        + kSeparatorStr
                        + temperature.ToString(kTwoDecimalsFloatFormat)
                        + kCelsiusDegreeStr
                        + kSeparatorStr
                        + dateTime.ToLongTimeString();
        }

        /// <summary>
        /// Set the dewpoint information.
        /// </summary>
        /// <param name="dewpoint">A float value that represents the dewpoint in °C.</param>
        public static string ReturnDewpointInfo(float dewpoint)
        {
            return kDewpointStr + dewpoint.ToString(kTwoDecimalsFloatFormat) + kCelsiusDegreeStr;
        }

        /// <summary>
        /// Set the UTC offset information.
        /// </summary>
        /// <param name="timeZone">The time zone specification of the location.</param>
        /// <param name="utcOffset">An TimeSpan value that represents the UTC.</param>
        public static string ReturnUTCOffsetInfo(string timeZone, TimeSpan utcOffset)
        {
            return kUTCOffsetStr + utcOffset.Hours + " (" + timeZone + ")";
        }

        /// <summary>
        /// Set the UV index information.
        /// </summary>
        /// <param name="indexUV">A float value that represents the UV index.</param>
        public static string ReturnUVIndexInfo(float indexUV)
        {
            if (indexUV == -1f)
            {
                return kUVIndexStr + kNotAvailable;
            }

            return kUVIndexStr + indexUV;
        }
        #endregion
    }
}
