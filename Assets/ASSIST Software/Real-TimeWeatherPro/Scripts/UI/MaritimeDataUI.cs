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
using RealTimeWeather.Data;

namespace RealTimeWeather.UI
{
    /// <summary>
    /// Class used to display maritime data information in the UI.
    /// </summary>
    public class MaritimeDataUI : MonoBehaviour
    {
        #region Private Const Variables
        private const string kTwoDecimalsFloatFormat = "F2";
        private const string kWaveHeightStr = "Wave Height: ";
        private const string kWavePeriodPeakStr = "Wave Period Peak: ";
        private const string kWaveDirectionStr = "Wave Direction: ";
        private const string kRadiationFluxLongwaveStr = "Radiation Flux Longwave: ";
        private const string kRadiationFluxShortwaveStr = "Radiation Flux Shortwave: ";
        private const string kSeaSurfaceTemperatureStr = "Sea Surface Temperature: ";
        private const string kMetersStr = " m";
        private const string kSecondsStr = " sec";
        private const string kWPerSquareMeterStr = " W/m^2";
        private const string kDegreesCelsiusStr = "°C";
        private const string kNotAvailableStr = "Not available";
        #endregion

        #region Private Variables
        [Header("Text Properties")]
        [SerializeField] private Text localizationText;
        [SerializeField] private Text dateText;
        [SerializeField] private Text geoCoordinatesText;
        [SerializeField] private Text maritimeStateText;
        [SerializeField] private Text windStatusText;
        [SerializeField] private Text humidityText;
        [SerializeField] private Text dewpointText;
        [SerializeField] private Text precipitationRateText;
        [SerializeField] private Text airPressureAtSeaText;
        [SerializeField] private Text waveHeightText;
        [SerializeField] private Text wavePeriodPeakText;
        [SerializeField] private Text waveDirectionText;
        [SerializeField] private Text radiationFluxLongwaveText;
        [SerializeField] private Text radiationFluxShortwaveText;
        [SerializeField] private Text seaSurfaceTemperatureText;
        [SerializeField] private Text visibilityText;
        [SerializeField] private Text offsetUTCText;

        [Header("Background Image")]
        [SerializeField] private Image backgroundImage;

        [Header("Background Sprites")]
        [SerializeField] private Sprite firstPanelSprite;
        [SerializeField] private Sprite secondPanelSprite;
        #endregion

        #region Public Properties
        public Text WeatherStateText { get => maritimeStateText; }
        #endregion

        #region Public Methods
        public void OnCurrentMaritimeDataUpdate(WaterData waterData)
        {
            maritimeStateText.text = UIUtilities.ReturnWeatherStateInfo(Utilities.ReturnWeatherStateBasedCloudCover(waterData.CloudCover.ToString(), waterData.Precipitation.ToString()), waterData.Temperature, waterData.DateTime);
            dateText.text = UIUtilities.ReturnDateTimeInfo(waterData.DateTime);
            localizationText.text = UIUtilities.ReturnLocalizationInfo(waterData.Localization.City, waterData.Localization.Country);
            geoCoordinatesText.text = UIUtilities.ReturnGeoCoordinatesInfo(waterData.Localization);
            precipitationRateText.text = UIUtilities.ReturnPrecipitationInfo(waterData.Precipitation);
            airPressureAtSeaText.text = UIUtilities.ReturnPressureInfo(waterData.AirPressureAtSea);
            humidityText.text = UIUtilities.ReturnHumidityInfo(waterData.Humidity);
            dewpointText.text = UIUtilities.ReturnDewpointInfo(Utilities.CalculateDewpoint(waterData.Temperature, waterData.Humidity));
            windStatusText.text = UIUtilities.ReturnWindInfo(waterData.Wind);
            visibilityText.text = UIUtilities.ReturnVisibilityInfo((float)waterData.Visibility);

            offsetUTCText.text = UIUtilities.ReturnUTCOffsetInfo(waterData.TimeZone, waterData.UtcOffset);
            waveHeightText.text = SetWaveHeightInfo(waterData.Wave.Height);
            wavePeriodPeakText.text = SetWavePeriodPeakInfo(waterData.Wave.PeriodPeak);
            waveDirectionText.text = SetWaveDirectionInfo(waterData.Wave.Direction);
            radiationFluxLongwaveText.text = SetRadiationFluxLongwave(waterData.FluxLongwave);
            radiationFluxShortwaveText.text = SetRadiationFluxShortwave(waterData.FluxShortwave);
            seaSurfaceTemperatureText.text = SetSeaSurfaceTemperature(waterData.SeaSurfaceTemperature);
        }

        /// <summary>
        /// Updates the background image of the panel, based on the configuration
        /// </summary>
        /// <param name="firstPanel">Specifies if the maritime UI is placed first in the configuration</param>
        public void OnBackgroundSpriteChange(bool firstPanel)
        {
            backgroundImage.sprite = firstPanel ? firstPanelSprite : secondPanelSprite;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Return the wave height information.
        /// </summary>
        /// <param name="waveHeight">A float value that represents the wave height in meters.</param>
        private string SetWaveHeightInfo(float waveHeight)
        {
            return kWaveHeightStr + (waveHeight != -1000f ? waveHeight.ToString(kTwoDecimalsFloatFormat) + kMetersStr : kNotAvailableStr);
        }

        /// <summary>
        /// Return the wave period peak information.
        /// </summary>
        /// <param name="wavePeriodPeak">A float value that represents the wave period peak in meters.</param>
        private string SetWavePeriodPeakInfo(float wavePeriodPeak)
        {
            return kWavePeriodPeakStr + (wavePeriodPeak != -1000f ? wavePeriodPeak.ToString(kTwoDecimalsFloatFormat) + kSecondsStr : kNotAvailableStr);
        }

        /// <summary>
        /// Return the wave direction information.
        /// </summary>
        /// <param name="waveDirection">A Vector2 value that represents the wave direction translated from degrees.</param>
        private string SetWaveDirectionInfo(Vector2 waveDirection)
        {
            return kWaveDirectionStr + waveDirection.ToString(kTwoDecimalsFloatFormat);
        }

        /// <summary>
        /// Return the radiation flux longwave information.
        /// </summary>
        /// <param name="fluxLongwave">A float value that represents the radiation flux longwave in W/m^2.</param>
        private string SetRadiationFluxLongwave(float fluxLongwave)
        {
            return kRadiationFluxLongwaveStr + (fluxLongwave != -1000f ? fluxLongwave.ToString(kTwoDecimalsFloatFormat) + kWPerSquareMeterStr : kNotAvailableStr);
        }

        /// <summary>
        /// Return the radiation flux shortwave information.
        /// </summary>
        /// <param name="fluxShortwave">A float value that represents the radiation flux shortwave in W/m^2.</param>
        private string SetRadiationFluxShortwave(float fluxShortwave)
        {
            return kRadiationFluxShortwaveStr + (fluxShortwave != -1000f ? fluxShortwave.ToString(kTwoDecimalsFloatFormat) + kWPerSquareMeterStr : kNotAvailableStr);
        }

        private string SetSeaSurfaceTemperature(float seaSurfaceTemperature)
        {
            return kSeaSurfaceTemperatureStr + (seaSurfaceTemperature != -1000f ? seaSurfaceTemperature.ToString(kTwoDecimalsFloatFormat) + kDegreesCelsiusStr : kNotAvailableStr);
        }
        #endregion
    }
}
