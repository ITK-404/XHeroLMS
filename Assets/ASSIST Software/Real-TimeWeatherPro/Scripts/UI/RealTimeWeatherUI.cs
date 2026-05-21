//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
// 

using UnityEngine;
using UnityEngine.UI;
using RealTimeWeather.Classes;
using RealTimeWeather.Enums;
using RealTimeWeather.Managers;
using System.Collections.Generic;
using RealTimeWeather.Data;

namespace RealTimeWeather.UI
{
    public enum ForecastType
    {
        Current,
        Hourly,
        Daily
    }

    /// <summary>
    /// Class used to display the complete weather/maritime data information in the UI.
    /// </summary>
    public class RealTimeWeatherUI : MonoBehaviour
    {
        #region Const Members
        private const float kUIDistanceBetween = 48.0f;
        private const float kUIFactorDimensionDailyPanel = 1.1133f;
        #endregion

        #region Private Variables
        private enum UIButtonStates
        {
            Ok,
            Warning,
            Error
        }

        [Header("Weather & Maritime Data UI Classes")]
        [SerializeField] private WeatherDataUI weatherDataUIClass;
        [SerializeField] private MaritimeDataUI maritimeDataUIClass;

        [Header("Toggle")]
        [SerializeField] private Toggle displayInfo;

        [Header("Toggle Checkmark")]
        [SerializeField] private Image displayInfoCheckmark;

        [Header("UI Classes")]
        [SerializeField] private RectTransform weatherDataUI;
        [SerializeField] private RectTransform maritimeDataUI;

        [Header("Reserve Locations")]
        [SerializeField] private GameObject reserveLocationPanel;
        [SerializeField] private Text reserveLocationText;

        // Alternate toggles
        [Header("Toggle Background Sprites")]
        [SerializeField] private Sprite okStateBackground;
        [SerializeField] private Sprite warningStateBackground;
        [SerializeField] private Sprite errorStateBackground;

        [Header("Toggle Checkmark Sprites")]
        [SerializeField] private Sprite okStatePressed;
        [SerializeField] private Sprite warningStatePressed;
        [SerializeField] private Sprite errorStatePressed;

        private bool errorOccured;
        #endregion

        #region Public Properties
        public bool WeatherDataOn { get; set; }
        public bool MaritimeDataOn { get; set; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdateUI += OnCurrentWeatherDataUpdate;
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate += OnCurrentMaritimeDataUpdate;
                ForecastModule.OnForecastModuleTick += OnForecastWeatherUpdate;
            }

            displayInfo.onValueChanged.AddListener(delegate { DisplayInfoToggleChanged(); });
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdateUI -= OnCurrentWeatherDataUpdate;
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate -= OnCurrentMaritimeDataUpdate;
                ForecastModule.OnForecastModuleTick -= OnForecastWeatherUpdate;
            }

            displayInfo.onValueChanged.RemoveAllListeners();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets teh data error in the according UI (weather data errors or maritime data errors)
        /// </summary>
        /// <param name="message">The message that will be displayed</param>
        /// <param name="weatherDataError">Specifies if the error specific to weather data</param>
        /// <param name="exception">The exception type</param>
        public void SetDataErrorMessage(string message, bool weatherDataError, ExceptionType exception = ExceptionType.InvalidInputData)
        {
            SetButtonState(exception == ExceptionType.InvalidInputData ? UIButtonStates.Warning : UIButtonStates.Error);
            errorOccured = true;

            if(weatherDataError)
            {
                weatherDataUIClass.WeatherStateText.text = message;
            }
            else
            {
                maritimeDataUIClass.WeatherStateText.text = message;
            }  
        }

        /// <summary>
        /// Sets the forecast type, before displaying any UI data
        /// </summary>
        /// <param name="forecastType">The forecast type</param>
        public void SetWeatherForecastType(ForecastType forecastType)
        {
            weatherDataUIClass.SetForecastType(forecastType);
        }

        /// <summary>
        /// The daily forecast data necessary before displaying any data (for daily forecast)
        /// </summary>
        /// <param name="dailyWeatherData">The daily forecast data</param>
        public void OnDailyWeatherDataReceived(List<WeatherData> dailyWeatherData)
        {
            weatherDataUIClass.OnDailyWeatherDataReceived(dailyWeatherData);
        }

        public void SetReserveLocationMessage(string message, bool state)
        {
            reserveLocationPanel.SetActive(state);
            reserveLocationText.text = message;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Triggered on the current weather data update (one update per set update-frequency)
        /// </summary>
        /// <param name="weatherData">The weather data information</param>
        private void OnCurrentWeatherDataUpdate(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                SetButtonState(UIButtonStates.Ok);
                weatherDataUIClass.OnCurrentWeatherUpdate(weatherData);
            }
        }

        /// <summary>
        /// Triggered at any forecast simulation tick (one update per tick, based on simulation speed)
        /// </summary>
        /// <param name="weatherData">The weather data information</param>
        private void OnForecastWeatherUpdate(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                SetButtonState(UIButtonStates.Ok);
                weatherDataUIClass.OnForecastWeatherUpdate(weatherData);
            }
        }

        /// <summary>
        /// Triggered on the current maritime data update (one update per set update-frequency)
        /// </summary>
        /// <param name="waterData">The weather data information</param>
        private void OnCurrentMaritimeDataUpdate(WaterData waterData)
        {
            if (waterData != null)
            {
                maritimeDataUIClass.OnCurrentMaritimeDataUpdate(waterData);
            }
        }

        /// <summary>
        /// Updates the display info button based on the: informations ok, errors, warnings
        /// </summary>
        /// <param name="state">The new state of the display info button</param>
        private void SetButtonState(UIButtonStates state)
        {
            switch (state)
            {
                case UIButtonStates.Ok:
                {
                    displayInfo.image.sprite = okStateBackground;
                    displayInfoCheckmark.sprite = okStatePressed;
                    break;
                }
                case UIButtonStates.Warning:
                {
                    displayInfo.image.sprite = warningStateBackground;
                    displayInfoCheckmark.sprite = warningStatePressed;

                    break;
                }
                case UIButtonStates.Error:
                {
                    displayInfo.image.sprite = errorStateBackground;
                    displayInfoCheckmark.sprite = errorStatePressed;

                    break;
                }
            }
        }

        /// <summary>
        /// Function shows/hides info panel, triggered at toggle change value.
        /// </summary>
        private void DisplayInfoToggleChanged()
        {
            weatherDataUI.gameObject.SetActive(displayInfo.isOn && WeatherDataOn);
            maritimeDataUI.gameObject.SetActive(displayInfo.isOn && MaritimeDataOn);

            if (RealTimeWeatherManager.instance.IsForecastComponentEnabled() && errorOccured == false)
            {
                weatherDataUIClass.PauseResumeButton.gameObject.SetActive(true);
            }

            UpdateMaritimeDataUIPosition();
        }

        /// <summary>
        /// Updates the position of the maritime data UI to be accordingly separated from weather data UI
        /// </summary>
        private void UpdateMaritimeDataUIPosition()
        {
            float widthDimension = weatherDataUIClass.ForecastType == ForecastType.Daily ? weatherDataUI.rect.width * kUIFactorDimensionDailyPanel : weatherDataUI.rect.width;
            float xPos = WeatherDataOn ? widthDimension + kUIDistanceBetween - 15.0f : kUIDistanceBetween;
            maritimeDataUI.anchoredPosition = new Vector3(xPos, maritimeDataUI.anchoredPosition.y);

            maritimeDataUIClass.OnBackgroundSpriteChange(WeatherDataOn == false);
        }
        #endregion
    }
}
