//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections;
using System;
using RealTimeWeather.Classes;
using RealTimeWeather.Enums;
using UnityEngine;
using RealTimeWeather.Data;

#if KWS_URP_PRESENT || KWS_HDRP_PRESENT
using KWS;
#endif // KWS_URP_PRESENT || KWS_HDRP_PRESENT 

namespace RealTimeWeather.WeatherProvider.KWS
{
    [Serializable]
    public class KWSWaterBody
    {
#if KWS_URP_PRESENT || KWS_HDRP_PRESENT
        #region Private constants
        private const int kWindConversionFactor = 7;

        private const float kLightRainStrength = 0.25f;
        private const float kModerateRainStrength = 0.5f;
        private const float kHeavyRainStrength = 0.75f;
        private const float kViolentRainStrength = 1f;
        #endregion

        #region Public Variables
        [Header("Water system parameters")]
        public WaterSystem waterSystem;
        public WaterTypes waterType;
        public Color waterTurbidityColor;
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the KWS ocean water bodies with the data from the weather providers
        /// </summary>
        /// <param name="waterData"></param>
        public void UpdateOceanData(WaterData waterData)
        {
            UpdateRainParameters(waterData.Precipitation);
            waterSystem.Settings.GlobalWindSpeed = waterData.Wave.Height > 1.5f ? waterData.Wave.Height : (waterData.Wind.Speed / kWindConversionFactor);
            waterSystem.Settings.GlobalWindRotation = Utilities.Vector2ToDegree(waterData.Wind.Direction) + 180;
            waterSystem.Settings.GlobalWindTurbulence = waterData.Wind.Speed;
        }

        /// <summary>
        /// Update the KWS lake and river water bodies with the data from the weather providers
        /// </summary>
        /// <param name="weatherData"></param>
        public void UpdateLakeAndRiverData(WeatherData weatherData)
        {
            UpdateRainParameters(weatherData.Precipitation);
            waterSystem.Settings.TurbidityColor = waterTurbidityColor;
            waterSystem.Settings.GlobalWindSpeed = weatherData.Wind.Speed / kWindConversionFactor;
            waterSystem.Settings.GlobalWindRotation = Utilities.Vector2ToDegree(weatherData.Wind.Direction) + 180;
        }

        public void UpdateRainParameters(float rainStrength)
        {
            float currentRainStrength = 0;

            waterSystem.Settings.UseDynamicWaves = rainStrength >= 1;
            waterSystem.Settings.UseDynamicWavesRainEffect = rainStrength >= 1;

            if (rainStrength >= 1 && rainStrength < 2.5f)
            {
                currentRainStrength = kLightRainStrength;
            }
            if (rainStrength >= 2.5f && rainStrength < 10)
            {
                currentRainStrength = kModerateRainStrength;
            }
            if (rainStrength >= 10 && rainStrength < 50)
            {
                currentRainStrength = kHeavyRainStrength;
            }
            if (rainStrength >= 50)
            {
                currentRainStrength = kViolentRainStrength;
            }

            waterSystem.Settings.DynamicWavesRainStrength = currentRainStrength;
            waterSystem.Settings.GlobalWindTurbulence = currentRainStrength;
        }
        #endregion
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 
    }
}
