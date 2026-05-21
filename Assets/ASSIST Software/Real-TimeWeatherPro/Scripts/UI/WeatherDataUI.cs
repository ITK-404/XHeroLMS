//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
// 

using UnityEngine;
using UnityEngine.UI;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using System.Collections.Generic;
using System;
using RealTimeWeather.Data;

namespace RealTimeWeather.UI
{
    /// <summary>
    /// Class used to display weather information in the UI.
    /// </summary>
    public class WeatherDataUI : MonoBehaviour
    {
        #region Private Const Variables
        private const string kTwoDecimalsFloatFormat = "F2";
        private const string kResumeButtonStr = "Resume";
        private const string kPauseButtonStr = "Pause";
        private const string kCelsiusDegreeStr = "°C";
        #endregion

        #region Public Events
        public static Action ChangeToPauseButtonText;
        #endregion

        #region Private Variables
        [Header("Pause/Resume Button")]
        [SerializeField] private Button pauseResumeButton;

        [Header("Text Properties")]
        [SerializeField] private Text localizationText;

        [SerializeField] private Text geoCoordinatesText;
        [SerializeField] private Text weatherStateText;
        [SerializeField] private Text windText;
        [SerializeField] private Text humidityText;
        [SerializeField] private Text dewpointText;
        [SerializeField] private Text pressureText;
        [SerializeField] private Text precipitationText;
        [SerializeField] private Text visibilityText;
        [SerializeField] private Text indexUVText;
        [SerializeField] private Text offsetUTCText;
        [SerializeField] private Text currentDateText;

        [Header("Hourly Forecast")]
        [SerializeField] private GameObject timeOfDaySliderPanel;
        [SerializeField] private Text timeOfDaySliderPanelText;
        [SerializeField] private Slider timeOfDaySlider;
        [SerializeField] private Text timeOfDaySliderTemperature;
        [SerializeField] private Text timeOfDaySliderHour;
        
        [Header("Daily Forecast")]
        [SerializeField] private GameObject dailyForecastPanel;

        [Header("Weather State Icon")]
        [SerializeField] private Image weatherStateIcon;

        [Header("Weather Status Icon Resources")]
        [SerializeField] private Sprite clearDay;
        [SerializeField] private Sprite clearNight;
        [SerializeField] private Sprite partlyClearDay;
        [SerializeField] private Sprite partlyClearNight;
        [SerializeField] private Sprite cloudy;
        [SerializeField] private Sprite mist;
        [SerializeField] private Sprite rainSnowPrecipitation;
        [SerializeField] private Sprite rainPrecipitation;
        [SerializeField] private Sprite thunderstorm;
        [SerializeField] private Sprite windy;

        // Keep track of type of forecast displayed
        private ForecastType _setForecastType = ForecastType.Current;
        private WeatherData _previousWeatherData;
        private bool _isForecastPlaying;

        private TimeSpan _sunriseTime;
        private TimeSpan _sunsetTime;
        #endregion

        #region Public Properties
        public Text WeatherStateText { get => weatherStateText; }
        public ForecastType ForecastType { get => _setForecastType; }
        public Button PauseResumeButton { get => pauseResumeButton; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            pauseResumeButton.onClick.AddListener(delegate { PauseResumeButtonPressed(); });
            ChangeToPauseButtonText += SetPauseButtonText;
        }

        private void OnDestroy()
        {
            pauseResumeButton.onClick.RemoveAllListeners();
            ChangeToPauseButtonText -= SetPauseButtonText;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Handles the current weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received current weather data.</param>
        public void OnCurrentWeatherUpdate(WeatherData weatherData)
        {
            if (_setForecastType == ForecastType.Current)
            {
                if (Utilities.IsOutsidePolarRegion(weatherData.Localization.Latitude))
                {
                    _sunriseTime = Utilities.CalculateSunriseTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                    _sunsetTime = Utilities.CalculateSunsetTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                }
            }

            if (_setForecastType == ForecastType.Current || _setForecastType == ForecastType.Hourly)
            {
                SetTimeOfDaySliderValues(weatherData);
            }

            SetPanelWeatherIcon(weatherData);
            weatherStateText.text = UIUtilities.ReturnWeatherStateInfo(weatherData.WeatherState, weatherData.Temperature, weatherData.DateTime);
            currentDateText.text = UIUtilities.ReturnDateTimeInfo(weatherData.DateTime);
            localizationText.text = UIUtilities.ReturnLocalizationInfo(weatherData.Localization.City, weatherData.Localization.Country);
            geoCoordinatesText.text = UIUtilities.ReturnGeoCoordinatesInfo(weatherData.Localization);
            precipitationText.text = UIUtilities.ReturnPrecipitationInfo(weatherData.Precipitation);
            pressureText.text = UIUtilities.ReturnPressureInfo(weatherData.Pressure);
            humidityText.text = UIUtilities.ReturnHumidityInfo(weatherData.Humidity);
            windText.text = UIUtilities.ReturnWindInfo(weatherData.Wind);
            visibilityText.text = UIUtilities.ReturnVisibilityInfo(weatherData.Visibility);
            dewpointText.text = UIUtilities.ReturnDewpointInfo(weatherData.Dewpoint);
            offsetUTCText.text = UIUtilities.ReturnUTCOffsetInfo(weatherData.TimeZone, weatherData.UTCOffset);
            indexUVText.text = UIUtilities.ReturnUVIndexInfo(weatherData.IndexUV);
        }

        /// <summary>
        /// Handles the forecast (hourly or daily) weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance list that represents the received hourly weather data.</param>
        public void OnForecastWeatherUpdate(WeatherData weatherData)
        {
            //Modifications for UI are required here (with the functionalities for the new UI)
            //Regarding of the type of forecast, I see two options:
            //1. You at the start the type of forecast and store the type in this class, so this function has only one parameter (the WeatherData) -> preferred
            //2. Add to this function the type parameter, and you send the type from the ForecastModule.cs every time
            OnCurrentWeatherUpdate(weatherData);

            if(_previousWeatherData == null)
            {
                _previousWeatherData = weatherData;
                if(Utilities.IsOutsidePolarRegion(weatherData.Localization.Latitude))
                {
                    _sunriseTime = Utilities.CalculateSunriseTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                    _sunsetTime = Utilities.CalculateSunsetTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                }
                return;
            }

            if (_previousWeatherData.DateTime.Date < weatherData.DateTime.Date)
            {
                if (Utilities.IsOutsidePolarRegion(weatherData.Localization.Latitude))
                {
                    _sunriseTime = Utilities.CalculateSunriseTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                    _sunsetTime = Utilities.CalculateSunsetTime(weatherData.Localization.Latitude, weatherData.DateTime.DayOfYear);
                }
                // New day
                if(_setForecastType == ForecastType.Daily)
                {
                    dailyForecastPanel.GetComponent<DailyForecastManager>().OnForecastDayPassed();
                }
                _previousWeatherData = weatherData;
            }
        }

        /// <summary>
        /// This prepares the user-interface for weather data with daily structure
        /// </summary>
        /// <param name="dailyWeatherData">A list with the daily weather forecast</param>
        public void OnDailyWeatherDataReceived(List<WeatherData> dailyWeatherData)
        {
            dailyForecastPanel.gameObject.SetActive(true);
            dailyForecastPanel.GetComponent<DailyForecastManager>().SetForecastData(dailyWeatherData);
        }

        /// <summary>
        /// Specifies if the forecast is hourly or daily, before displaying any data
        /// </summary>
        /// <param name="forecastType">The type of the forecast</param>
        public void SetForecastType(ForecastType forecastType)
        {
            _setForecastType = forecastType;
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Changes the Pause/Resume button text when the forecast resumes
        /// </summary>
        private void SetPauseButtonText()
        {
            pauseResumeButton.GetComponentInChildren<Text>().text = kPauseButtonStr;
            _isForecastPlaying = false;
        }

        /// <summary>
        /// Changes the Pause/Resume button text when the forecast is paused
        /// </summary>
        private void SetResumeButtonText()
        {
            pauseResumeButton.GetComponentInChildren<Text>().text = kResumeButtonStr;
            _isForecastPlaying = true;
        }

        /// <summary>
        /// Trigger event when the Pause/Resume button is pressed
        /// </summary>
        private void PauseResumeButtonPressed()
        {
            if (_isForecastPlaying)
            {
                SetPauseButtonText();
            }
            else
            {
                SetResumeButtonText();
            }
            ForecastModule.OnPauseResumeForecast?.Invoke();
        }

        /// <summary>
        /// Updates the weather state icon from the weather data UI accordingly
        /// </summary>
        /// <param name="weatherData">The weather data information</param>
        private void SetPanelWeatherIcon(WeatherData weatherData)
        {
            weatherStateIcon.gameObject.SetActive(true);

            bool isDaytime;
            if(Utilities.IsOutsidePolarRegion(weatherData.Localization.Latitude))
            {
                isDaytime = (weatherData.DateTime.TimeOfDay >= _sunriseTime && weatherData.DateTime.TimeOfDay <= _sunsetTime);
            }
            else
            {
                if(weatherData.Localization.Latitude > 0f)
                {
                    isDaytime = weatherData.DateTime.Month > (int)Months.February && weatherData.DateTime.Month < (int)Months.September ? true : false;
                }
                else
                {
                    isDaytime = weatherData.DateTime.Month >= (int)Months.September || weatherData.DateTime.Month <= (int)Months.February ? true : false;
                }
            }

            switch (weatherData.WeatherState)
            {
                case WeatherState.Sunny:
                case WeatherState.Clear:
                case WeatherState.Fair:
                    if (isDaytime)
                    {
                        weatherStateIcon.sprite = clearDay;
                        break;
                    }
                    weatherStateIcon.sprite = clearNight;
                    break;
                
                case WeatherState.Cloudy:
                    weatherStateIcon.sprite = cloudy;
                    break;
                
                case WeatherState.PartlyClear:
                case WeatherState.PartlySunny:
                case WeatherState.PartlyCloudy:
                    if (isDaytime)
                    {
                        weatherStateIcon.sprite = partlyClearDay;
                        break;
                    }
                    weatherStateIcon.sprite = partlyClearNight;
                    break;

                case WeatherState.Mist:
                    weatherStateIcon.sprite = mist;
                    break;
                
                case WeatherState.Thunderstorms:
                    weatherStateIcon.sprite = thunderstorm;
                    break;
                
                case WeatherState.Windy:
                    weatherStateIcon.sprite = windy;
                    break;
                
                case WeatherState.RainPrecipitation:
                    weatherStateIcon.sprite = rainPrecipitation;
                    break;
                
                case WeatherState.SnowPrecipitation:
                case WeatherState.RainSnowPrecipitation:
                    weatherStateIcon.sprite = rainSnowPrecipitation;
                    break;
            }
        }

        /// <summary>
        /// Updates the time of the day on the hour progress slider
        /// </summary>
        /// <param name="weatherData">The weather data information</param>
        private void SetTimeOfDaySliderValues(WeatherData weatherData)
        {
            timeOfDaySliderPanel.gameObject.SetActive(true);

            if (_setForecastType == ForecastType.Hourly) // This is displayed only in this scenario
            {
                timeOfDaySliderPanelText.gameObject.SetActive(true);
            }

            // Handle position and info
            timeOfDaySlider.value = weatherData.DateTime.Hour + (1.0f * (weatherData.DateTime.Minute / 60.0f));
            timeOfDaySliderHour.text = weatherData.DateTime.Hour.ToString();
            timeOfDaySliderTemperature.text = weatherData.Temperature.ToString(kTwoDecimalsFloatFormat) + kCelsiusDegreeStr;
        }
        #endregion
    }
}