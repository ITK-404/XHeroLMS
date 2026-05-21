//
// Copyright(c) 2023 EasySky ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;
using RealTimeWeather.Data;
using RealTimeWeather.Classes;
using RealTimeWeather.Managers;

#if EASYSKY_PRESENT
using EasySky.WeatherArea;
using EasySky;
#endif

namespace RealTimeWeather.WeatherControllers
{
    public class EasySkyModuleController : MonoBehaviour
    {
#if EASYSKY_PRESENT
        [SerializeField] private EasySkyWeatherManager _easySkyWeatherManager;

        public void CreateEasySkyManagerInstance(GameObject easySkyManager)
        {
            if (easySkyManager == null)
            {
                return;
            }

            var easySkyManagerObject = Instantiate(easySkyManager);
            easySkyManagerObject.transform.SetParent(transform);

            var easySkyWeatherManager = easySkyManagerObject.GetComponent<EasySkyWeatherManager>();
            if (easySkyWeatherManager == null)
            {
                return;
            }

            _easySkyWeatherManager = easySkyWeatherManager;
        }

        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick += OnForecastWeatherUpdate;

            }
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick -= OnForecastWeatherUpdate;
            }
        }

        private void OnForecastWeatherUpdate(WeatherData currentWeatherData, WeatherData nextWeatherData, double weatherProgress)
        {
            if (currentWeatherData == null || nextWeatherData == null)
            {
                return;
            }

            var curentWeatherPreset = GetWeatherPreset(currentWeatherData);
            var nextWeatherPreset = GetWeatherPreset(nextWeatherData);
            _easySkyWeatherManager.WeatherEffectsController.SetInterpolateData(curentWeatherPreset, nextWeatherPreset, (float)weatherProgress);
            _easySkyWeatherManager.CloudsController.LerpVolCloudPreset(curentWeatherPreset.VolumetricCloudPresetData, nextWeatherPreset.VolumetricCloudPresetData, (float)weatherProgress);
            _easySkyWeatherManager.CloudsController.LerpLayerCloudPreset(curentWeatherPreset.LayerCloudPresetData, nextWeatherPreset.LayerCloudPresetData, (float)weatherProgress);
            SetLocalization(currentWeatherData.Localization);
            SetTime(currentWeatherData.DateTime);
            SetWind(currentWeatherData.Wind);
        }

        private void OnCurrentWeatherUpdate(WeatherData weatherData)
        {
            if (weatherData == null)
            {
                return;
            }

            SetLocalization(weatherData.Localization);
            SetTime(weatherData.DateTime);
            SetWeatherState(weatherData);
            SetWind(weatherData.Wind);
            SetVisibility(weatherData.Visibility);
        }

        private void SetVisibility(float visibility)
        {
            _easySkyWeatherManager.WeatherEffectsController.FogController.ChangeFogState(true);
            _easySkyWeatherManager.WeatherEffectsController.FogController.ChangeVisibility(visibility * 1000);
        }

        private void SetWind(Wind wind)
        {
            _easySkyWeatherManager.GlobalData.windDirection = Vector2.Angle(wind.Direction, Vector2.right);
            _easySkyWeatherManager.GlobalData.windSpeed = wind.Speed;
            _easySkyWeatherManager.FireUpdateWind();
        }

        private void SetWeatherState(WeatherData weatherData)
        {
            _easySkyWeatherManager.SetWeatherPreset(GetWeatherPreset(weatherData));
        }

        private void SetTime(DateTime dateTime)
        {
            _easySkyWeatherManager.GlobalData.globalTime = dateTime;
            _easySkyWeatherManager.GlobalData.minutes = dateTime.Minute;
            _easySkyWeatherManager.GlobalData.hours = dateTime.Hour;
            _easySkyWeatherManager.GlobalData.days = dateTime.Day;
            _easySkyWeatherManager.GlobalData.months = dateTime.Month;
            _easySkyWeatherManager.GlobalData.years = dateTime.Year;
            _easySkyWeatherManager.CelestialObjectsController.UpdateStarsPositions();
            _easySkyWeatherManager.CelestialObjectsController.UpdatePlanetPosition();
        }

        private void SetLocalization(Localization localization)
        {
            _easySkyWeatherManager.GlobalData.latitude = localization.Latitude;
            _easySkyWeatherManager.GlobalData.longitude = localization.Longitude;
            _easySkyWeatherManager.CelestialObjectsController.UpdateStarsPositions();
            _easySkyWeatherManager.CelestialObjectsController.UpdatePlanetPosition();
        }

        private WeatherPresetData GetWeatherPreset(WeatherData weatherData)
        {
            foreach (var item in _easySkyWeatherManager.DefaultWeatherPresets.WeatherPresetsList)
            {
                if (item.WeatherType.ToString() == weatherData.WeatherState.ToString())
                {
                    return item;
                }
            }

            return _easySkyWeatherManager.DefaultWeatherPresets.WeatherPresetsList[0];
        }
#endif
    }
}