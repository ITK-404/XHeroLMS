//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections.Generic;
using RealTimeWeather.Classes;
using RealTimeWeather.Managers;
using RealTimeWeather.Enums;
using UnityEngine;
using RealTimeWeather.Data;
using RealTimeWeather.WeatherProvider.KWS;

#if (KWS_URP_PRESENT || KWS_HDRP_PRESENT) && UNITY_2020_3_OR_NEWER
using KWS;
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 

namespace RealTimeWeather.WeatherControllers
{
    public class KWSModuleController : MonoBehaviour
    {
        #region Private Constants
        private const string kOceanInstanceName = "KWSOcean";
        #endregion

        #region Private Variables
#if (KWS_URP_PRESENT || KWS_HDRP_PRESENT) && UNITY_2020_3_OR_NEWER
        [SerializeField] private List<KWSWaterBody> _waterBodies;
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 

        private Color waterTurbidityDefaultColor = new Color(0.19f, 0.13f, 0.06f, 1f);
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += UpdateRiversAndLakesWithWaterData;
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate += UpdateOceanWithWaterData;
            }
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= UpdateRiversAndLakesWithWaterData;
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate -= UpdateOceanWithWaterData;
            }
        }
        #endregion

#if (KWS_URP_PRESENT || KWS_HDRP_PRESENT) && UNITY_2020_3_OR_NEWER
        #region Public Methods
        /// <summary>
        /// This function creates the KWS manager and adds it as a child to the "RealTimeWeatherManager" manager.
        /// </summary>
        public void CreateKWSWaterSystemInstance()
        {
            _waterBodies = new List<KWSWaterBody>();
            WaterSystem[] waterSystems = FindObjectsOfType<WaterSystem>();
            if (waterSystems.Length == 0)
            {
                _waterBodies.Add(new KWSWaterBody());
                GameObject ocean = new GameObject();
                ocean.transform.SetParent(transform);
                ocean.name = kOceanInstanceName;
                _waterBodies[_waterBodies.Count - 1].waterSystem = ocean.AddComponent<WaterSystem>();
                _waterBodies[_waterBodies.Count - 1].waterTurbidityColor = waterTurbidityDefaultColor;
                _waterBodies[_waterBodies.Count - 1].waterType = WaterTypes.Ocean;
            }
            else
            {
                for (int i = 0; i < waterSystems.Length; i++)
                {
                    _waterBodies.Add(new KWSWaterBody());
                    _waterBodies[i].waterSystem = waterSystems[i];
                    _waterBodies[i].waterTurbidityColor = waterTurbidityDefaultColor;
                    _waterBodies[i].waterType = WaterTypes.None;
                }   
            }
        }
        #endregion
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 

        #region Private Methods
        private void UpdateRiversAndLakesWithWaterData(WeatherData weatherData)
        {
#if (KWS_URP_PRESENT || KWS_HDRP_PRESENT) && UNITY_2020_3_OR_NEWER
            for (int i = 0; i < _waterBodies.Count; i++)
            {
                if (_waterBodies[i].waterType == WaterTypes.Lake || _waterBodies[i].waterType == WaterTypes.River)
                {
                    _waterBodies[i].UpdateLakeAndRiverData(weatherData);
                }
                //This warning should be send to the Weather UI after it gets updated
                if(_waterBodies[i].waterType == WaterTypes.None)
                {
                    Debug.LogWarning("Water type not set for" + _waterBodies[i].waterSystem.gameObject.name);
                }
            }
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 
        }

        private void UpdateOceanWithWaterData(WaterData waterData)
        {
#if (KWS_URP_PRESENT || KWS_HDRP_PRESENT) && UNITY_2020_3_OR_NEWER
            for (int i = 0; i < _waterBodies.Count; i++)
            {
                if (_waterBodies[i].waterType == WaterTypes.Ocean)
                {
                    _waterBodies[i].UpdateOceanData(waterData);
                }
                //This warning should be send to the Weather UI after it gets updated
                if (_waterBodies[i].waterType == WaterTypes.None)
                {
                    Debug.LogWarning("Water type not set for" + _waterBodies[i].waterSystem.gameObject.name);
                }
            }
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT 
        }
        #endregion
    }
}
