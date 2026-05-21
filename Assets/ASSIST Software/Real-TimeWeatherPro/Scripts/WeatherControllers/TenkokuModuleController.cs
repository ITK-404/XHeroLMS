//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;
using RealTimeWeather.Classes;
using RealTimeWeather.Managers;
using RealTimeWeather.Data;
#if TENKOKU_PRESENT
using Tenkoku.Core;
#endif

namespace RealTimeWeather.WeatherControllers
{
    /// <summary>
    /// Class used to simulate weather using Tenkoku plug-in.
    /// </summary>
    ///
    public class TenkokuModuleController : MonoBehaviour
    {
        #region Private Const Variables
        private const string kTenkokuManagerHierachyName = "Tenkoku Dynamic Sky";
        #endregion

        #region private variable
        private const float kPrecipitationQualityDefault = 1.5f;
        private const float kCloudQualityDefault = 1.3f;
        private const float kMinPrecipitation = 0.02f;
        private const float kHighWindKmValue = 55.0f;
        private const float kOneHundred = 100.0f;
        private const int kHumidityDivide = 50;

#if TENKOKU_PRESENT
        private TenkokuModule _tenkokuModule;
#endif
        [SerializeField] private WeatherDataProfile _clearWeatherDataProfile;
        [SerializeField] private WeatherDataProfile _fairWeatherDataProfile;
        [SerializeField] private WeatherDataProfile _cloudyWeatherDataProfile;
        [SerializeField] private WeatherDataProfile _partlyCloudyWeatherDataProfile;
        [SerializeField] private WeatherDataProfile _rainWeatherDataProfile;
        [SerializeField] private WeatherDataProfile _stormWeatherDataProfile;
        #endregion

        #region Public Methods
        /// <summary>
        /// This function creates the TenkokuManager" manager and adds it as a child to the "RealTimeWeatherManager" manager.
        /// </summary>
        public void CreateTenkokuManagerInstance(GameObject tenkokuManagerPrefab)
        {
#if TENKOKU_PRESENT && UNITY_EDITOR
            if (tenkokuManagerPrefab != null)
            {
                GameObject tenkokuModuleInstance = Instantiate(tenkokuManagerPrefab, Vector3.zero, Quaternion.identity);
                tenkokuModuleInstance.name = kTenkokuManagerHierachyName;

                //Tenkoku module will be extracted here and any settings applied to it will also be applied in the scene
                //But after "Play" button is pressed, the reference will be lost, so the reference needs to be searched again
                _tenkokuModule = tenkokuModuleInstance.GetComponent<TenkokuModule>();
                _tenkokuModule.cloudQuality = kCloudQualityDefault;
                _tenkokuModule.precipQuality = kPrecipitationQualityDefault;
                _tenkokuModule.autoTime = false;
                _tenkokuModule.enabled = true;
                _tenkokuModule.enableDST = true;
                tenkokuModuleInstance.transform.SetParent(transform);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif
        }
        #endregion

#if TENKOKU_PRESENT
        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick += OnWeatherForecastUpdate;
            }
            var tenkokuModules = FindObjectsOfType<TenkokuModule>();
            if (tenkokuModules.Length == 1)
            {
                _tenkokuModule = tenkokuModules[0];
            }
            else if (tenkokuModules.Length > 1)
            {
                Debug.LogError("Multiple Tenkoku modules: " + tenkokuModules.Length);
            }
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick -= OnWeatherForecastUpdate;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles the weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void OnCurrentWeatherUpdate(WeatherData weatherData)
        {
            if (weatherData == null)
            {
                return;
            }

            var currentWeatherProfile = ReturnWeatherDataProfile(weatherData.WeatherState);
            SetWindsData(weatherData);

            SetWeatherProfile(currentWeatherProfile);
            // SetTimeZone(weatherData);
            // SetDate(weatherData);
            SetVisibility(weatherData);
            SetTemperature(weatherData);
            SetPrecipitation(weatherData);
        }

        /// <summary>
        /// Handles the forecast weather data update event.
        /// </summary>
        /// <param name="currentWeather">A WeatherData class instance that represents the current weather data.</param>
        /// <param name="nextWeather">A WeatherData class instance that represents the next weather data.</param>
        /// <param name="weatherProgress">A double value percentage that represents the progress from start weather (not current weather) and next weather.</param>
        private void OnWeatherForecastUpdate(WeatherData currentWeather, WeatherData nextWeather, double weatherProgress)
        {
            if (currentWeather == null)
            {
                return;
            }

            if(nextWeather == null)
            {
                OnCurrentWeatherUpdate(nextWeather);
                return;
            }

            var _forecastWeatherDataProfile = ReturnForecastWeatherState(currentWeather, nextWeather, weatherProgress);
            SetWindsData(currentWeather);

            SetWeatherProfile(_forecastWeatherDataProfile);
            // SetTimeZone(currentWeather);
            // SetDate(currentWeather);
            SetVisibility(currentWeather);
            SetTemperature(currentWeather);
            SetPrecipitation(currentWeather);
        }

        /// <summary>
        /// Sets the current weather profile in tenkoku.
        /// </summary>
        private void SetWeatherProfile(WeatherDataProfile weatherDataProfile)
        {
            _tenkokuModule.weather_setAuto = weatherDataProfile.Parameter.setAuto;
            _tenkokuModule.weather_cloudAltAmt = weatherDataProfile.Parameter.cloudAltAmt;
            _tenkokuModule.weather_cloudAltoStratusAmt = weatherDataProfile.Parameter.cloudAltoStratusAmt;
            _tenkokuModule.weather_cloudCirrusAmt = weatherDataProfile.Parameter.cloudCirrusAmt;
            _tenkokuModule.weather_cloudCumulusAmt = weatherDataProfile.Parameter.cloudCumulusAmt;
            _tenkokuModule.weather_cloudScale = weatherDataProfile.Parameter.cloudScale;
            _tenkokuModule.weather_cloudSpeed = weatherDataProfile.Parameter.cloudSpeed;
            _tenkokuModule.weather_OvercastAmt = weatherDataProfile.Parameter.overcastAmt;
            _tenkokuModule.weather_OvercastDarkeningAmt = weatherDataProfile.Parameter.overcastDarkeningAmt;
            _tenkokuModule.weather_lightning = weatherDataProfile.Parameter.lightning;
            _tenkokuModule.weather_rainbow = weatherDataProfile.Parameter.rainbow;
            _tenkokuModule.weather_lightningDir = weatherDataProfile.Parameter.lightningDir;
            _tenkokuModule.weather_lightningRange = weatherDataProfile.Parameter.lightningRange;
            _tenkokuModule.autoFog = weatherDataProfile.Parameter.setFogAuto;
        }

        /// <summary>
        /// Update the clouds properties based on the current weather state and next weather state, applying some interpolation
        /// </summary>
        /// <param name="currentWeather">The current weather</param>
        /// <param name="nextWeather">The next weather</param>
        /// <param name="weatherProgress">The progress from start weather to next weather</param>
        private WeatherDataProfile ReturnForecastWeatherState(WeatherData currentWeather, WeatherData nextWeather, double weatherProgress)
        {
            var currentWeatherDataProfile = ReturnWeatherDataProfile(currentWeather.WeatherState);
            var nextWeatherDataProfile = ReturnWeatherDataProfile(nextWeather.WeatherState);

            var interpolatedWeatherData = new WeatherDataProfile();
            interpolatedWeatherData.Parameter.setAuto = currentWeatherDataProfile.Parameter.setAuto;
            interpolatedWeatherData.Parameter.cloudAltAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudAltAmt, nextWeatherDataProfile.Parameter.cloudAltAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.cloudAltoStratusAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudAltoStratusAmt, nextWeatherDataProfile.Parameter.cloudAltoStratusAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.cloudCirrusAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudCirrusAmt, nextWeatherDataProfile.Parameter.cloudCirrusAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.cloudCumulusAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudCumulusAmt, nextWeatherDataProfile.Parameter.cloudCumulusAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.cloudScale = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudScale, nextWeatherDataProfile.Parameter.cloudScale, (float)weatherProgress);
            interpolatedWeatherData.Parameter.cloudSpeed = Mathf.Lerp(currentWeatherDataProfile.Parameter.cloudSpeed, nextWeatherDataProfile.Parameter.cloudSpeed, (float)weatherProgress);
            interpolatedWeatherData.Parameter.overcastAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.overcastAmt, nextWeatherDataProfile.Parameter.overcastAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.overcastDarkeningAmt = Mathf.Lerp(currentWeatherDataProfile.Parameter.overcastDarkeningAmt, nextWeatherDataProfile.Parameter.overcastDarkeningAmt, (float)weatherProgress);
            interpolatedWeatherData.Parameter.lightning = currentWeatherDataProfile.Parameter.lightning;
            interpolatedWeatherData.Parameter.rainbow = currentWeatherDataProfile.Parameter.rainbow;
            interpolatedWeatherData.Parameter.lightningDir = Mathf.Lerp(currentWeatherDataProfile.Parameter.lightningDir, nextWeatherDataProfile.Parameter.lightningDir, (float)weatherProgress);
            interpolatedWeatherData.Parameter.lightningDir = Mathf.Lerp(currentWeatherDataProfile.Parameter.lightningDir, nextWeatherDataProfile.Parameter.lightningRange, (float)weatherProgress);
            interpolatedWeatherData.Parameter.setFogAuto = currentWeatherDataProfile.Parameter.setFogAuto;
            return interpolatedWeatherData;
        }

        /// <summary>
        /// Returns the weather data profile based on the weather state
        /// </summary>
        /// <param name="weatherState">The weather state interrogated</param>
        /// <returns>A weather data profile specific to the given weather state</returns>
        private WeatherDataProfile ReturnWeatherDataProfile(Enums.WeatherState weatherState)
        {
            switch (weatherState)
            {
                case Enums.WeatherState.Clear:
                case Enums.WeatherState.Sunny:
                    return _clearWeatherDataProfile;
                case Enums.WeatherState.Fair:
                case Enums.WeatherState.PartlyClear:
                case Enums.WeatherState.PartlySunny:
                    return _fairWeatherDataProfile;
                case Enums.WeatherState.Cloudy:
                    return _cloudyWeatherDataProfile;
                case Enums.WeatherState.PartlyCloudy:
                case Enums.WeatherState.Mist:
                case Enums.WeatherState.Windy:
                    return _partlyCloudyWeatherDataProfile;
                case Enums.WeatherState.Thunderstorms:
                    return _stormWeatherDataProfile;
                case Enums.WeatherState.RainSnowPrecipitation:
                case Enums.WeatherState.RainPrecipitation:
                case Enums.WeatherState.SnowPrecipitation:
                    return _rainWeatherDataProfile;
                default:
                    return _clearWeatherDataProfile;
            }
        }

        /// <summary>
        /// Sets precipitation in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetPrecipitation(WeatherData weatherData)
        {
            _tenkokuModule.weather_RainAmt = 0;
            _tenkokuModule.weather_SnowAmt = 0;

            float precipitationFromHumidity = (weatherData.Humidity > kHumidityDivide) ? (weatherData.Humidity - kHumidityDivide) / kHumidityDivide : kMinPrecipitation;
            float precipitation = (weatherData.Precipitation > 0.0f) ? weatherData.Precipitation : precipitationFromHumidity;
            switch (weatherData.WeatherState)
            {
                case Enums.WeatherState.RainPrecipitation:
                    _tenkokuModule.weather_RainAmt = precipitation;
                    break;
                case Enums.WeatherState.SnowPrecipitation:
                    _tenkokuModule.weather_SnowAmt = precipitation;
                    break;
                case Enums.WeatherState.RainSnowPrecipitation:
                    _tenkokuModule.weather_RainAmt = precipitation;
                    _tenkokuModule.weather_SnowAmt = precipitation;
                    break;
                default:
                    if (weatherData.Temperature > 0)
                    {
                        _tenkokuModule.weather_RainAmt = weatherData.Precipitation;
                    }
                    else
                    {
                        _tenkokuModule.weather_SnowAmt = weatherData.Precipitation;
                    }
                    break;
            }
        }

        /// <summary>
        /// Sets the date in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetDate(WeatherData weatherData)
        {
            _tenkokuModule.autoDateSync = false;
            _tenkokuModule.currentYear = weatherData.DateTime.Year;
            _tenkokuModule.currentMonth = weatherData.DateTime.Month;
            _tenkokuModule.currentDay = weatherData.DateTime.Day;
            _tenkokuModule.currentHour = weatherData.DateTime.Hour;
            _tenkokuModule.currentMinute = weatherData.DateTime.Minute;
            _tenkokuModule.currentSecond = weatherData.DateTime.Second;
            _tenkokuModule.setTZOffset = weatherData.UTCOffset.Hours;
        }

        /// <summary>
        /// Sets wind in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetWindsData(WeatherData weatherData)
        {
            _tenkokuModule.weather_WindDir = Utilities.Vector2ToDegrees(weatherData.Wind.Direction);

            var tenkokuWindSpeed = weatherData.Wind.Speed / kHighWindKmValue;
            _tenkokuModule.weather_WindAmt = Mathf.Clamp(tenkokuWindSpeed, 0.0f, 1.0f);
        }

        /// <summary>
        /// Sets time zone in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetTimeZone(WeatherData weatherData)
        {
            _tenkokuModule.setLatitude = weatherData.Localization.Latitude;
            _tenkokuModule.setLongitude = weatherData.Localization.Longitude;
        }

        /// <summary>
        /// Sets the temperature in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetTemperature(WeatherData weatherData)
        {
            _tenkokuModule.weather_temperature = weatherData.Temperature;
        }

        /// <summary>
        /// Sets humidity in tenkoku.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetVisibility(WeatherData weatherData)
        {
            _tenkokuModule.weather_humidity = weatherData.Humidity / kOneHundred;
        }
        #endregion
#endif
    }
}