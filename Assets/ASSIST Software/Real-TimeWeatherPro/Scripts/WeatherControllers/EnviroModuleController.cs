//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using RealTimeWeather.Enums;
using RealTimeWeather.Managers;
using System;
using UnityEngine;
using System.Collections.Generic;
using RealTimeWeather.Data;
using UnityEditor;
#if ENVIRO_3
using Enviro;
#endif

namespace RealTimeWeather.WeatherControllers
{
    /// <summary>
    /// Class used to simulate weather using Enviro plug-in.
    /// </summary>
    public class EnviroModuleController : MonoBehaviour
    {
        #region Private Const Variables
        private const string kEnviroManagerName = "Enviro Sky Manager";
        private const string kEnviroConfigurationName = "Default Enviro Configuration";

        private const float kZeroValue = 0f;
        private const float kCloudsUpwardsIntensity = 0.015f;
        private const float kOneValue = 1f;
        private const float kHundredValue = 100f;
        private const float kThousand = 1000f;
        private const float kPrecipitationMean = 8f;
        private const float kHumidityMean = 50f;
        private const float kPolarCircleNorth = 66.5f;
        private const float kPolarCircleSouth = -66.5f;
        private const float kLightshaftsBlurRadius = 2f;
        private const float kLightshaftsIntensity = 0.05f;
        private const float kAuroraBrightnessDefault = 75f;

        private const int kStateClearId = 0;
#if ENVIRO_PRESENT && !ENVIRO_3
        private const int kStatePartlyClearId = 1;
        private const int kStateSunnyId = 0;
        private const int kStatePartlySunnyId = 1;
        private const int kStateCloudyId = 2;
        private const int kStatePartlyCloudyId = 4;
        private const int kStateThunderstormsId = 10;
        private const int kStateHeavyPrecipitationId = 7;
        private const int kStateRainPrecipitationId = 6;
        private const int kStateRainSnowPrecipitationId = 11;
        private const int kStateHeavySnowPrecipitationId = 9;
        private const int kStateLightSnowPrecipitationId = 8;
        private const int kStateFairId = 1;
        private const int kStateMistId = 5;
        private const int kStateWindyId = 3;
#elif ENVIRO_3
        private const string kRainPreset = "Rain";
        private const string kSnowPreset = "Snow";

        private const int kStateFairId = 6; 
        private const int kStateCloudyId = 1;
        private const int kStatePartlyCloudyId = 2;
        private const int kStateThunderstormId = 8;
        private const int kStateLightRainId = 9;
        private const int kStateRainId = 4;
        private const int kStateSnowId = 5;
        private const int kStateFoggyId = 3;
        private const int kStateRainSnowId = 10;
#endif
        #endregion

        #region Public Variables
        [Header("Enviro Weather Presets Manager")]
        public EnviroModulePresetsManager _enviroPresetManager;
        #endregion

        #region Private Variables
#if ENVIRO_PRESENT && !ENVIRO_3
        private EnviroWeatherPreset _interpolatedWeatherData;
        private List<EnviroWeatherPreset> _weatherPresetsClones;
#elif ENVIRO_3
        private EnviroWeatherType _interpolatedWeatherData;
        private List<EnviroWeatherType> _weatherTypesClones;
#endif
        private Dictionary<string, EnviroPresetData> _enviroPresetData = new Dictionary<string, EnviroPresetData>();
        private int _currentIndex;
        #endregion

#region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick += OnForecastWeatherUpdate;
            }
        }

        private void Start()
        {
            FillPresetsClones();
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= OnCurrentWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick -= OnForecastWeatherUpdate;
            }
        }
#endregion

#region Public Methods
        /// <summary>
        /// This function creates the Enviro Manager instance and adds it as a child to the "RealTimeWeatherManager" manager.
        /// </summary>
        public void CreateEnviroManager()
        {
#if ENVIRO_PRESENT && UNITY_EDITOR && !ENVIRO_3
            if (EnviroSkyMgr.instance == null)
            {
                GameObject enviroManagerObj = new GameObject();
                enviroManagerObj.name = kEnviroManagerName;
                EnviroSkyMgr enviroManager = enviroManagerObj.AddComponent<EnviroSkyMgr>();
                enviroManagerObj.AddComponent<EnviroEvents>();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#elif ENVIRO_3 && UNITY_EDITOR
            if (EnviroManager.instance == null)
            {
                var enviroManagerObj = new GameObject();
                enviroManagerObj.transform.SetParent(transform);
                enviroManagerObj.name = "Enviro3 Manager";
                enviroManagerObj.SetActive(false);
                var enviroManager = enviroManagerObj.AddComponent<EnviroManager>();

                var assetGuid = AssetDatabase.FindAssets($"t: EnviroConfiguration");
                if (assetGuid.Length != 0)
                {
                    var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid[0]);
                    var configuration = AssetDatabase.LoadAssetAtPath<EnviroConfiguration>(assetPath);
                    enviroManager.configuration = configuration;
                }

                enviroManagerObj.SetActive(true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif
        }

        /// <summary>
        /// Initializes Enviro components.
        /// </summary>
        public void SetupEnviro()
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            if (EnviroSkyMgr.instance == null)
            {
                return;
            }

#if UNITY_STANDALONE
            if (!EnviroSkyMgr.instance.enviroHDInstance && !EnviroSkyMgr.instance.enviroLWInstance)
            {
                EnviroSkyMgr.instance.CreateEnviroHDInstance();
                EnviroSkyMgr.instance.ActivateHDInstance();
                EnviroSkyMgr.instance.AssignAndStart(Camera.main.gameObject, Camera.main);
                EnviroSkyMgr.instance.ReInit();
            }
#endif

#if (UNITY_ANDROID || UNITY_IOS) && (ENVIRO_HD || ENVIRO_LW)
            if (!EnviroSkyMgr.instance.enviroLWInstance && !EnviroSkyMgr.instance.enviroHDInstance)
            {
                EnviroSkyMgr.instance.CreateEnviroLWMobileInstance();
                EnviroSkyMgr.instance.ActivateLWInstance();
                EnviroSkyMgr.instance.AssignAndStart(Camera.main.gameObject, Camera.main);
                EnviroSkyMgr.instance.ReInit();
            }
#endif
            EnviroSkyMgr.instance.SetTimeProgress(EnviroTime.TimeProgressMode.None);
            EnviroSkyMgr.instance.SetAutoWeatherUpdates(false);

#if ENVIRO_HD || ENVIRO_LW
            EnviroSkyMgr.instance.Seasons.calcSeasons = false;
            EnviroSkyMgr.instance.WeatherSettings.wetnessDryingSpeed = kZeroValue;
            EnviroSkyMgr.instance.WeatherSettings.wetnessAccumulationSpeed = kZeroValue;
            EnviroSkyMgr.instance.CloudSettings.cloudsUpwardsWindIntensity = kCloudsUpwardsIntensity;
            EnviroSkyMgr.instance.CloudSettings.dualLayerParticleClouds = false;
#endif

            _enviroPresetManager.InitializeEnviroPresets();

#if ENVIRO_HD
            if (_enviroPresetManager.EnviroPresetsList.Count != 0)
            {
                foreach (var preset in _enviroPresetManager.EnviroPresetsList)
                {
                    preset.auroraIntensity = 1f;
                }
            }
#endif

            SetUseWindZoneDirection(false);
            SetVolumeClouds(true);
            SetFlatClouds(false);
            SetParticleClouds(true);
            SetFog(true);
            SetVolumeLighting(false);
            SetSunShafts(true);
            SetMoonShafts(false);
            SetDistanceBlur(true);
            SetLightShafts();
#endif

#if UNITY_EDITOR && ENVIRO_3
            EnviroManager.instance.configuration = RealTimeWeatherManager.instance.GetObject(kEnviroConfigurationName, "asset") as EnviroConfiguration;
#endif

#if ENVIRO_3
            _enviroPresetManager.InitializeEnviroPresets();
#if UNITY_EDITOR
            // Initialize Thunderstorm Preset
            var thunderstormPreset = _enviroPresetManager.CreateEnviroWeatherType("Thunderstorm");

            if (thunderstormPreset != null)
            {
                thunderstormPreset = _enviroPresetManager.DuplicateEnviroWeatherType(thunderstormPreset, kRainPreset);
                thunderstormPreset.lightningOverride.lightningStorm = true;
                thunderstormPreset.cloudsOverride.ligthAbsorbtion = 0.55f;
                EditorUtility.SetDirty(thunderstormPreset);
            }

            // Initialize Light Rain Preset
            var lightRainPreset = _enviroPresetManager.CreateEnviroWeatherType("Light Rain");

            if (lightRainPreset != null)
            {
                lightRainPreset = _enviroPresetManager.DuplicateEnviroWeatherType(lightRainPreset, kRainPreset);

                var rainEffect = lightRainPreset.effectsOverride.effectsOverride.Find(x => x.name.Equals(kRainPreset));
                if(rainEffect != null)
                {
                    rainEffect.emission = 0.3f;
                }

                // Add audio "Light Rain" effect to the preset
                var audioOverrideType = new EnviroAudioOverrideType();
                audioOverrideType.name = "Light Rain";
                audioOverrideType.volume = 1f;
                lightRainPreset.audioOverride.weatherOverride.Clear();
                lightRainPreset.audioOverride.weatherOverride.Add(audioOverrideType);
                lightRainPreset.cloudsOverride.coverage = 0.12f;
                EditorUtility.SetDirty(lightRainPreset);
            }

            // Initialize Rain&Snow preset
            var rainSnowPreset = _enviroPresetManager.CreateEnviroWeatherType("Rain&Snow");

            if (rainSnowPreset != null)
            {
                rainSnowPreset = _enviroPresetManager.DuplicateEnviroWeatherType(rainSnowPreset, kRainPreset);
                var rainEffect = rainSnowPreset.effectsOverride.effectsOverride.Find(x => x.name.Equals(kRainPreset));
                if (rainEffect != null)
                {
                    rainEffect.emission = 0.6f;
                }
                else
                {
                    var rainEffectAdd = new EnviroEffectsOverrideType();
                    rainEffectAdd.name = kRainPreset;
                    rainEffectAdd.showEditor = true;
                    rainEffectAdd.emission = 0.6f;
                    rainSnowPreset.effectsOverride.effectsOverride.Add(rainEffectAdd);
                }

                var snowEffect = new EnviroEffectsOverrideType();
                snowEffect.name = kSnowPreset;
                snowEffect.showEditor = true;
                snowEffect.emission = 0.6f;
                rainSnowPreset.effectsOverride.effectsOverride.Add(snowEffect);
                rainSnowPreset.cloudsOverride.coverage = 0.3f;

                var audioOverrideType = new EnviroAudioOverrideType();
                audioOverrideType.name = "Light Rain";
                audioOverrideType.volume = 0.4f;
                rainSnowPreset.audioOverride.weatherOverride.Clear();
                rainSnowPreset.audioOverride.weatherOverride.Add(audioOverrideType);
                EditorUtility.SetDirty(rainSnowPreset);
            }
#endif
#endif
        }
#endregion

#region Private Methods
        private void FillPresetsClones()
        {
#if ENVIRO_PRESENT && !ENVIRO_3
                if (EnviroSkyMgr.instance == null || !EnviroSkyMgr.instance.IsAvailable())
                {
                    this.enabled = false;
                    return;
                }

                EnviroZone enviroZone = EnviroSkyMgr.instance.GetCurrentActiveZone();

                if (RealTimeWeatherManager.instance.LoadedSimulation == null) return;

                if (RealTimeWeatherManager.instance.IsForecastModeEnabled() || RealTimeWeatherManager.instance.LoadedSimulation.WeatherSimulationType == SimulationType.UserData)
                {
                    _weatherPresetsClones = new List<EnviroWeatherPreset>();
                    foreach (var preset in enviroZone.zoneWeatherPresets)
                    {
                        var newPreset = ScriptableObject.Instantiate(preset);
                        _weatherPresetsClones.Add(newPreset);
                    }
                }
#elif ENVIRO_3
            if (EnviroManager.instance.Environment != null)
            {
                EnviroManager.instance.Environment.Settings.temperatureChangingSpeed = 0f;
                EnviroManager.instance.Environment.Settings.wetnessAccumulationSpeed = 0f;
                EnviroManager.instance.Environment.Settings.wetnessDrySpeed = 0f;
            }

            if (_enviroPresetManager.WeatherTypes == null || _enviroPresetManager.WeatherTypes.Count == 0)
            {
                SetupEnviro();
            }
            EnviroManager.instance.Weather.ChangeWeatherInstant(_enviroPresetManager.WeatherTypes[kStateClearId]);

            if (RealTimeWeatherManager.instance.LoadedSimulation == null) return;

            // Initialize Weather Types clones list, responsible for instantiating clones of the default presets
            // These clones will have their values interpolated to the next weather preset values during forecast and reset to the original default values when the interpolation is over
            if (RealTimeWeatherManager.instance.IsForecastModeEnabled() || RealTimeWeatherManager.instance.LoadedSimulation.WeatherSimulationType == SimulationType.UserData)
            {
                _weatherTypesClones = new List<EnviroWeatherType>();
                foreach (var weatherType in _enviroPresetManager.WeatherTypes)
                {
                    var newPreset = ScriptableObject.Instantiate(weatherType);
                    _weatherTypesClones.Add(newPreset);
                }
            }
#endif

        }

        /// <summary>
        /// Handles the weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void OnCurrentWeatherUpdate(WeatherData weatherData)
        {
#if ENVIRO_PRESENT || ENVIRO_3
            if (weatherData == null)
            {
                return;
            }

            SetLocalization(weatherData.Localization);
            SetTime(weatherData.DateTime);
            SetWeatherState(weatherData);
            SetSeason(weatherData.DateTime.Month, weatherData.Localization.Latitude);
            SetTemperature(weatherData.Temperature);
            SetHumidity(weatherData.Humidity);
            SetWind(weatherData.Wind);
            SetVisibility(weatherData.Visibility);
            SetUtcOffset(weatherData.UTCOffset);
            SetAurora(weatherData.Localization.Latitude);
#endif
        }

        private void OnForecastWeatherUpdate(WeatherData currentWeatherData, WeatherData nextWeatherData, double weatherProgress)
        {
#if ENVIRO_PRESENT || ENVIRO_3
            if (currentWeatherData == null || nextWeatherData == null)
            {
                return;
            }

            SetTime(currentWeatherData.DateTime);
            SetTemperature(currentWeatherData.Temperature);
            SetHumidity(currentWeatherData.Humidity);
            SetWind(currentWeatherData.Wind);
            SetVisibility(currentWeatherData.Visibility);
            SetLocalization(currentWeatherData.Localization);
            SetSeason(currentWeatherData.DateTime.Month, currentWeatherData.Localization.Latitude);
            SetAurora(currentWeatherData.Localization.Latitude);
            SetUtcOffset(currentWeatherData.UTCOffset);

            UpdateForecastWeatherState(currentWeatherData, nextWeatherData, weatherProgress);
            SetWeatherState(currentWeatherData);
#endif
        }

#if ENVIRO_PRESENT && !ENVIRO_3
        /// <summary>
        /// Resets the interpolated EnviroWeatherPreset data back to it's initial state 
        /// </summary>
        /// <param name="preset">The preset that interpolated it's values to the next preset's data from the forecast</param>
        /// <param name="presetName">Preset name identifier</param>
        private void ResetEnviroPresetData(EnviroWeatherPreset preset, string presetName)
        {
            if(preset == null)
            {
                return;
            }

            var enviroZone = EnviroSkyMgr.instance.GetCurrentActiveZone();

            for (var i = 0; i < _weatherPresetsClones.Count; i++)
            {
                if (preset == _weatherPresetsClones[i])
                {
                    _weatherPresetsClones[i] = ScriptableObject.Instantiate(enviroZone.zoneWeatherPresets[i]);
                    break;
                }
            }
        }
#elif ENVIRO_3
        /// <summary>
        /// Resets the Enviro presets data back to default after the forecast interpolation is over
        /// </summary>
        private void ResetEnviroTypeData(EnviroWeatherType weatherType, string presetName)
        {
            if (weatherType == null)
            {
                return;
            }

            for (var i = 0; i < _weatherTypesClones.Count; i++)
            {
                if (weatherType == _weatherTypesClones[i])
                {
                    _weatherTypesClones[i] = ScriptableObject.Instantiate(_enviroPresetManager.WeatherTypes[i]);
                    break;
                }
            }
        }
#endif

        /// <summary>
        /// Update the weather preset properties based on the current weather state and next weather state, applying some interpolation
        /// </summary>
        /// <param name="currentWeather">The current weather</param>
        /// <param name="nextWeather">The next weather</param>
        /// <param name="weatherProgress">The progress from start weather to next weather</param>
        private void UpdateForecastWeatherState(WeatherData currentWeather, WeatherData nextWeather, double weatherProgress)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroWeatherPreset currentWeatherPreset = GetForecastWeatherPreset(currentWeather);
            EnviroWeatherPreset nextWeatherPreset = GetForecastWeatherPreset(nextWeather);
            var enviroZone = EnviroSkyMgr.instance.GetCurrentActiveZone();

            if (_interpolatedWeatherData != currentWeatherPreset)
            {
                if(_interpolatedWeatherData != null)
                {
                    ResetEnviroPresetData(_interpolatedWeatherData, _interpolatedWeatherData.name);
                }
                _interpolatedWeatherData = currentWeatherPreset;

                // Get the current weather preset's index
                for (var i = 0; i < _weatherPresetsClones.Count; i++)
                {
                    if (_interpolatedWeatherData == _weatherPresetsClones[i])
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }

            // General Configs
            _interpolatedWeatherData.volumeLightIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].volumeLightIntensity, nextWeatherPreset.volumeLightIntensity, (float)weatherProgress);
            _interpolatedWeatherData.WindStrenght = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].WindStrenght, nextWeatherPreset.WindStrenght, (float)weatherProgress);
            _interpolatedWeatherData.wetnessLevel = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].wetnessLevel, nextWeatherPreset.wetnessLevel, (float)weatherProgress);
            _interpolatedWeatherData.snowLevel = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].snowLevel, nextWeatherPreset.snowLevel, (float)weatherProgress);

            // Fog Configs
            _interpolatedWeatherData.fogDensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].fogDensity, nextWeatherPreset.fogDensity, (float)weatherProgress);

            // Distance Blur Configs
            _interpolatedWeatherData.blurDistance = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].blurDistance, nextWeatherPreset.blurDistance, (float)weatherProgress);
            _interpolatedWeatherData.blurIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].blurIntensity, nextWeatherPreset.blurIntensity, (float)weatherProgress);
            _interpolatedWeatherData.blurSkyIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].blurSkyIntensity, nextWeatherPreset.blurSkyIntensity, (float)weatherProgress);

            // Volume clouds
            _interpolatedWeatherData.cloudsConfig.scatteringCoef = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.scatteringCoef, nextWeatherPreset.cloudsConfig.scatteringCoef, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.edgeDarkness = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.edgeDarkness, nextWeatherPreset.cloudsConfig.edgeDarkness, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.lightAbsorbtion = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.lightAbsorbtion, nextWeatherPreset.cloudsConfig.lightAbsorbtion, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.lightStepModifier = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.lightStepModifier, nextWeatherPreset.cloudsConfig.lightStepModifier, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.lightVariance = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.lightVariance, nextWeatherPreset.cloudsConfig.lightVariance, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.density = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.density, nextWeatherPreset.cloudsConfig.density, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.baseErosionIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.baseErosionIntensity, nextWeatherPreset.cloudsConfig.baseErosionIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.detailErosionIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.detailErosionIntensity, nextWeatherPreset.cloudsConfig.detailErosionIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.coverage = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.coverage, nextWeatherPreset.cloudsConfig.coverage, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.coverageType = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.coverageType, nextWeatherPreset.cloudsConfig.coverageType, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.cloudType = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.cloudType, nextWeatherPreset.cloudsConfig.cloudType, (float)weatherProgress);

            // Flat clouds
            _interpolatedWeatherData.cloudsConfig.flatCoverage = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCoverage, nextWeatherPreset.cloudsConfig.flatCoverage, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.flatCloudsDensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCloudsDensity, nextWeatherPreset.cloudsConfig.flatCloudsDensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.flatCloudsAbsorbtion = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCloudsAbsorbtion, nextWeatherPreset.cloudsConfig.flatCloudsAbsorbtion, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.flatCloudsDirectLightIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCloudsDirectLightIntensity, nextWeatherPreset.cloudsConfig.flatCloudsDirectLightIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.flatCloudsAmbientLightIntensity = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCloudsAmbientLightIntensity, nextWeatherPreset.cloudsConfig.flatCloudsAmbientLightIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.flatCloudsHGPhase = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.flatCloudsHGPhase, nextWeatherPreset.cloudsConfig.flatCloudsHGPhase, (float)weatherProgress);

            // Particle clouds
            _interpolatedWeatherData.cloudsConfig.particleLayer1Alpha = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer1Alpha, nextWeatherPreset.cloudsConfig.particleLayer1Alpha, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.particleLayer1Brightness = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer1Brightness, nextWeatherPreset.cloudsConfig.particleLayer1Brightness, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.particleLayer1ColorPow = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer1ColorPow, nextWeatherPreset.cloudsConfig.particleLayer1ColorPow, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.particleLayer2Alpha = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer2Alpha, nextWeatherPreset.cloudsConfig.particleLayer2Alpha, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.particleLayer2Brightness = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer2Brightness, nextWeatherPreset.cloudsConfig.particleLayer2Brightness, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.particleLayer2ColorPow = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.particleLayer2ColorPow, nextWeatherPreset.cloudsConfig.particleLayer2ColorPow, (float)weatherProgress);

            // Cirrus clouds
            _interpolatedWeatherData.cloudsConfig.cirrusAlpha = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.cirrusAlpha, nextWeatherPreset.cloudsConfig.cirrusAlpha, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.cirrusCoverage = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.cirrusCoverage, nextWeatherPreset.cloudsConfig.cirrusCoverage, (float)weatherProgress);
            _interpolatedWeatherData.cloudsConfig.cirrusColorPow = Mathf.Lerp(enviroZone.zoneWeatherPresets[_currentIndex].cloudsConfig.cirrusColorPow, nextWeatherPreset.cloudsConfig.cirrusColorPow, (float)weatherProgress);
#elif ENVIRO_3
            var currentWeatherType = GetForecastWeatherType(currentWeather);
            var nextWeatherType = GetForecastWeatherType(nextWeather);

            // If the simulation started or the weather state changed, the current weather will start interpolating it's values to the next weather preset
            if (_interpolatedWeatherData != currentWeatherType)
            {
                if (_interpolatedWeatherData != null)
                {
                    // Reset preset's values only when the weather state changed
                    ResetEnviroTypeData(_interpolatedWeatherData, _interpolatedWeatherData.name);
                }
                _interpolatedWeatherData = currentWeatherType;

                // Get the current weather preset's index
                for (var i = 0; i < _weatherTypesClones.Count; i++)
                {
                    if (_interpolatedWeatherData == _weatherTypesClones[i])
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }

            // Lighting
            _interpolatedWeatherData.lightingOverride.directLightIntensityModifier = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].lightingOverride.directLightIntensityModifier, nextWeatherType.lightingOverride.directLightIntensityModifier, (float)weatherProgress);
            _interpolatedWeatherData.lightingOverride.ambientIntensityModifier = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].lightingOverride.ambientIntensityModifier, nextWeatherType.lightingOverride.ambientIntensityModifier, (float)weatherProgress);

            // Volumetric Clouds
            _interpolatedWeatherData.cloudsOverride.coverage = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.coverage, nextWeatherType.cloudsOverride.coverage, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.dilateCoverage = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.dilateCoverage, nextWeatherType.cloudsOverride.dilateCoverage, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.dilateType = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.dilateType, nextWeatherType.cloudsOverride.dilateType, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.typeModifier = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.typeModifier, nextWeatherType.cloudsOverride.typeModifier, (float)weatherProgress);
            //_interpolatedWeatherData.cloudsOverride.anvilBiasLayer1 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.anvilBiasLayer1, nextWeatherType.cloudsOverride.anvilBiasLayer1, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.scatteringIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.scatteringIntensity, nextWeatherType.cloudsOverride.scatteringIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.multiScatterFalloff = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.multiScatterFalloff, nextWeatherType.cloudsOverride.multiScatterFalloff, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.multiScatterStrength = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.multiScatterStrength, nextWeatherType.cloudsOverride.multiScatterStrength, (float)weatherProgress);
            //_interpolatedWeatherData.cloudsOverride.multiScatteringCLayer1 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.multiScatteringCLayer1, nextWeatherType.cloudsOverride.multiScatteringCLayer1, (float)weatherProgress);
            //_interpolatedWeatherData.cloudsOverride.powderIntensityLayer1 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.powderIntensityLayer1, nextWeatherType.cloudsOverride.powderIntensityLayer1, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.silverLiningIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.silverLiningIntensity, nextWeatherType.cloudsOverride.silverLiningIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.silverLiningSpread = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.silverLiningSpread, nextWeatherType.cloudsOverride.silverLiningSpread, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.ligthAbsorbtion = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.ligthAbsorbtion, nextWeatherType.cloudsOverride.ligthAbsorbtion, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.density = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.density, nextWeatherType.cloudsOverride.density, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.baseErosionIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.baseErosionIntensity, nextWeatherType.cloudsOverride.baseErosionIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.detailErosionIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.detailErosionIntensity, nextWeatherType.cloudsOverride.detailErosionIntensity, (float)weatherProgress);
            _interpolatedWeatherData.cloudsOverride.curlIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].cloudsOverride.curlIntensity, nextWeatherType.cloudsOverride.curlIntensity, (float)weatherProgress);

            // Flat Clouds
            _interpolatedWeatherData.flatCloudsOverride.cirrusCloudsCoverage = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.cirrusCloudsCoverage, nextWeatherType.flatCloudsOverride.cirrusCloudsCoverage, (float)weatherProgress);
            _interpolatedWeatherData.flatCloudsOverride.cirrusCloudsAlpha = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.cirrusCloudsAlpha, nextWeatherType.flatCloudsOverride.cirrusCloudsAlpha, (float)weatherProgress);
            _interpolatedWeatherData.flatCloudsOverride.cirrusCloudsColorPower = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.cirrusCloudsColorPower, nextWeatherType.flatCloudsOverride.cirrusCloudsColorPower, (float)weatherProgress);
            _interpolatedWeatherData.flatCloudsOverride.flatCloudsCoverage = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.flatCloudsCoverage, nextWeatherType.flatCloudsOverride.flatCloudsCoverage, (float)weatherProgress);
            _interpolatedWeatherData.flatCloudsOverride.flatCloudsLightIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.flatCloudsLightIntensity, nextWeatherType.flatCloudsOverride.flatCloudsLightIntensity, (float)weatherProgress);
            _interpolatedWeatherData.flatCloudsOverride.flatCloudsAmbientIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.flatCloudsAmbientIntensity, nextWeatherType.flatCloudsOverride.flatCloudsAmbientIntensity, (float)weatherProgress);
            //_interpolatedWeatherData.flatCloudsOverride.flatCloudsAbsorbtion = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].flatCloudsOverride.flatCloudsAbsorbtion, nextWeatherType.flatCloudsOverride.flatCloudsAbsorbtion, (float)weatherProgress);

            // Fog
            _interpolatedWeatherData.fogOverride.fogDensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogDensity, nextWeatherType.fogOverride.fogDensity, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogHeightFalloff = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogHeightFalloff, nextWeatherType.fogOverride.fogHeightFalloff, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogHeight = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogHeight, nextWeatherType.fogOverride.fogHeight, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogDensity2 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogDensity2, nextWeatherType.fogOverride.fogDensity2, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogHeightFalloff2 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogHeightFalloff2, nextWeatherType.fogOverride.fogHeightFalloff2, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogHeight2 = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogHeight2, nextWeatherType.fogOverride.fogHeight2, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.fogColorBlend = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.fogColorBlend, nextWeatherType.fogOverride.fogColorBlend, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.scattering = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.scattering, nextWeatherType.fogOverride.scattering, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.extinction = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.extinction, nextWeatherType.fogOverride.extinction, (float)weatherProgress);
            _interpolatedWeatherData.fogOverride.anistropy = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].fogOverride.anistropy, nextWeatherType.fogOverride.anistropy, (float)weatherProgress);

            // Aurora
            _interpolatedWeatherData.auroraOverride.auroraIntensity = Mathf.Lerp(_enviroPresetManager.WeatherTypes[_currentIndex].auroraOverride.auroraIntensity, nextWeatherType.auroraOverride.auroraIntensity, (float)weatherProgress);

            // Effects
            if (_interpolatedWeatherData.effectsOverride.effectsOverride.Count == 0)
            {
                return;
            }

            var rainEffect = _interpolatedWeatherData.effectsOverride.effectsOverride.Find(x => x.name.Equals(kRainPreset));
            if (rainEffect != null)
            {
                var rainEffectEnviro = _enviroPresetManager.WeatherTypes[_currentIndex].effectsOverride.effectsOverride.Find(x => x.name.Equals(kRainPreset));
                var rainEffectEnviroEmission = rainEffectEnviro == null ? 0 : rainEffectEnviro.emission;
                var rainEffectNext = nextWeatherType.effectsOverride.effectsOverride.Find(x => x.name.Equals(kRainPreset));
                var rainEffectNextEmission = rainEffectNext == null ? 0 : rainEffectNext.emission;

                var lerpedValue = Mathf.Lerp(rainEffectEnviroEmission, rainEffectNextEmission, (float)weatherProgress);
                rainEffect.emission = lerpedValue;
            }

            var snowEffect = _interpolatedWeatherData.effectsOverride.effectsOverride.Find(x => x.name.Equals(kSnowPreset));
            if (snowEffect != null)
            {
                var snowEffectEnviro = _enviroPresetManager.WeatherTypes[_currentIndex].effectsOverride.effectsOverride.Find(x => x.name.Equals(kSnowPreset));
                var snowEffectEnviroEmission = snowEffectEnviro == null ? 0 : snowEffectEnviro.emission;
                var snowEffectNext = nextWeatherType.effectsOverride.effectsOverride.Find(x => x.name.Equals(kSnowPreset));
                var snowEffectNextEmission = snowEffectNext == null ? 0 : snowEffectNext.emission;

                var lerpedValue = Mathf.Lerp(snowEffectEnviroEmission, snowEffectNextEmission, (float)weatherProgress);
                snowEffect.emission = lerpedValue;
            }
#endif
        }

#if ENVIRO_3
        /// <summary>
        /// Returns the Enviro weather type (preset) based on the given weather state
        /// </summary>
        private EnviroWeatherType GetForecastWeatherType(WeatherData weatherData)
        {
            switch (weatherData.WeatherState)
            {
                case WeatherState.Clear:
                case WeatherState.Sunny:
                    return _weatherTypesClones[kStateClearId];
                case WeatherState.PartlyClear:
                case WeatherState.PartlySunny:
                case WeatherState.Fair:
                    return _weatherTypesClones[kStateFairId];
                case WeatherState.Cloudy:
                case WeatherState.Windy:
                    return _weatherTypesClones[kStateCloudyId];
                case WeatherState.PartlyCloudy:
                    return _weatherTypesClones[kStatePartlyCloudyId];
                case WeatherState.Thunderstorms:
                    return _weatherTypesClones[kStateThunderstormId];
                case WeatherState.RainPrecipitation:
                    if (weatherData.Precipitation < kPrecipitationMean)
                    {
                        return _weatherTypesClones[kStateLightRainId];
                    }
                    else
                    {
                        return _weatherTypesClones[kStateRainId];
                    }
                case WeatherState.SnowPrecipitation:
                    return _weatherTypesClones[kStateSnowId];
                case WeatherState.Mist:
                    return _weatherTypesClones[kStateFoggyId];
                case WeatherState.RainSnowPrecipitation:
                    return _weatherTypesClones[kStateRainSnowId];
                default:
                    return _weatherTypesClones[kStateClearId];
            }
        }
#endif

#if ENVIRO_PRESENT && !ENVIRO_3
        /// <summary>
        /// Return the coresponding EnviroWeatherPreset based on the RTW data
        /// </summary>
        /// <param name="weatherData">The RTW data</param>
        private EnviroWeatherPreset GetForecastWeatherPreset(WeatherData weatherData)
        {
            if(_weatherPresetsClones == null) FillPresetsClones();
            switch (weatherData.WeatherState)
            {
                case WeatherState.Clear:
                case WeatherState.Sunny:
                    return _weatherPresetsClones[kStateClearId];
                case WeatherState.PartlyClear:
                case WeatherState.PartlySunny:
                case WeatherState.Fair:
                    return _weatherPresetsClones[kStateFairId];
                case WeatherState.Cloudy:
                    return _weatherPresetsClones[kStateCloudyId];
                case WeatherState.PartlyCloudy:
                    return _weatherPresetsClones[kStatePartlyCloudyId];
                case WeatherState.Thunderstorms:
                    return _weatherPresetsClones[kStateThunderstormsId];
                case WeatherState.RainPrecipitation:
                    if (weatherData.Precipitation < kPrecipitationMean)
                    {
                        return _weatherPresetsClones[kStateRainPrecipitationId];
                    }
                    else
                    {
                        return _weatherPresetsClones[kStateHeavyPrecipitationId];
                    }
                case WeatherState.RainSnowPrecipitation:
                    return _weatherPresetsClones[kStateRainSnowPrecipitationId];
                case WeatherState.SnowPrecipitation:
                    if (weatherData.Precipitation < kPrecipitationMean)
                    {
                        return _weatherPresetsClones[kStateLightSnowPrecipitationId];
                    }
                    else
                    {
                        return _weatherPresetsClones[kStateHeavySnowPrecipitationId];
                    }
                case WeatherState.Mist:
                    return _weatherPresetsClones[kStateMistId];
                case WeatherState.Windy:
                    return _weatherPresetsClones[kStateWindyId];
                default:
                    return _weatherPresetsClones[kStateClearId];
            }
        }
#endif
#region Localization Methods
        /// <summary>
        /// Set the localization, latitude and longitude values.
        /// </summary>
        /// <param name="localization">A Localization class instance that represents the localization data.</param>
        public void SetLocalization(Localization localization)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.Time.Latitude = localization.Latitude;
            EnviroSkyMgr.instance.Time.Longitude = localization.Longitude;
#elif ENVIRO_3
            EnviroManager.instance.Time.Settings.longitude = localization.Longitude;
            EnviroManager.instance.Time.Settings.latitude = localization.Latitude;
#endif
        }
#endregion

#region Time Methods
        /// <summary>
        /// Set the exact date, by DateTime.
        /// </summary>
        /// <param name="dateTime">An DateTime value that represents the date.</param>
        public void SetTime(DateTime dateTime)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.SetTime(dateTime);
#elif ENVIRO_3
            EnviroManager.instance.Time.SetDateTime(dateTime.Second, dateTime.Minute, dateTime.Hour, dateTime.Day, dateTime.Month, dateTime.Year);
#endif
        }

        /// <summary>
        /// Set the UTC offset, by TimeSpan.
        /// </summary>
        /// <param name="utcOffset">An TimeSpan value that represents the UTC.</param>
        public void SetUtcOffset(TimeSpan utcOffset)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
#if ENVIRO_HD
            if (EnviroSkyMgr.instance.enviroHDInstance)
            {
                EnviroSkyMgr.instance.enviroHDInstance.GameTime.utcOffset = utcOffset.Hours;
            }
#endif
#if ENVIRO_LW
            if (EnviroSkyMgr.instance.enviroLWInstance)
            {
                EnviroSkyMgr.instance.enviroLWInstance.GameTime.utcOffset = utcOffset.Hours;
            }
#endif
#elif ENVIRO_3
            EnviroManager.instance.Time.Settings.utcOffset = utcOffset.Hours;
#endif
        }

        /// <summary>
        /// Set the season.
        /// </summary>
        /// <param name="month">An int value that represents the month.</param>
        public void SetSeason(int month, float latitude)
        {
#if ENVIRO_PRESENT
            switch (month)
            {
                case 1:
                case 2:
                case 12:
                    if(latitude > 0f)
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Winter);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Winter);
#endif
                    }
                    else
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Summer);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Summer);
#endif
                    }
                    break;
                case 3:
                case 4:
                case 5:
                    if(latitude > 0f)
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Spring);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Spring);
#endif
                    }
                    else
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Autumn);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Autumn);
#endif
                    }
                    break;
                case 6:
                case 7:
                case 8:
                    if(latitude > 0f)
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Summer);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Summer);
#endif
                    }
                    else
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Winter);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Winter);
#endif
                    }
                    break;
                case 9:
                case 10:
                case 11:
                    if(latitude > 0f)
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Autumn);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Autumn);
#endif
                    }
                    else
                    {
#if ENVIRO_3
                        EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Spring);
#else
                        EnviroSkyMgr.instance.ChangeSeason(EnviroSeasons.Seasons.Spring);
#endif
                    }
                    break;
            }
#endif
        }
#endregion

#region Weather Methods
        /// <summary>
        /// Set weather over id with instant transition. 
        /// </summary>
        /// <param name="id">An int value that represents the weather profile Id.</param>
        private void SetWeatherIDInstant(int id)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.ChangeWeatherInstant(id);
#endif
        }

        /// <summary>
        /// Set weather over id with smooth transtion.
        /// </summary>
        /// <param name="id">An int value that represents the weather profile Id.</param>
        private void SetWeatherID(int id)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            if(EnviroSkyMgr.instance.Weather.WeatherPrefabs.Count > 0)
            {
                EnviroSkyMgr.instance.ChangeWeather(id);
            }
#endif
        }

        /// <summary>
        /// Sets the Enviro 3 weather type based on it's index
        /// </summary>
        private void SetWeatherType(int index)
        {
#if ENVIRO_3
            if (RealTimeWeatherManager.instance.IsForecastModeEnabled())
            {
                EnviroManager.instance.Weather.ChangeWeather(_weatherTypesClones[index]);
            }
            else
            {
                EnviroManager.instance.Weather.ChangeWeather(_enviroPresetManager.WeatherTypes[index]);
            }
#endif
        }

        /// <summary>
        /// Set the current temperature value.
        /// </summary>
        /// <param name="temperature">A float value that represents the temperature in °C.</param>
        private void SetTemperature(float temperature)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.Weather.currentTemperature = temperature;
#elif ENVIRO_3
            EnviroManager.instance.Environment.Settings.temperature = temperature;
#endif
        }

        /// <summary>
        /// Set the current humidity value.
        /// </summary>
        /// <param name="humidity">A float value that represents the humidity in percent.</param>
        private void SetHumidity(float humidity)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.Weather.curWetness = Mathf.Clamp((humidity / kHundredValue), kZeroValue, kOneValue);
#elif ENVIRO_3
            EnviroManager.instance.Environment.Settings.wetness = Mathf.Clamp((humidity / kHundredValue), kZeroValue, kOneValue);
#endif
        }

        /// <summary>
        /// Set the current weather state.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void SetWeatherState(WeatherData weatherData)
        {
            switch (weatherData.WeatherState)
            {
                case WeatherState.Sunny:
                case WeatherState.Clear:
#if ENVIRO_3
                    SetWeatherType(kStateClearId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateClearId);
#endif
                    break;
                case WeatherState.PartlyClear:
                case WeatherState.PartlySunny:
                case WeatherState.Fair:
#if ENVIRO_3
                    SetWeatherType(kStateFairId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStatePartlyClearId);
#endif
                    break;
                case WeatherState.Cloudy:
#if ENVIRO_3
                    SetWeatherType(kStateCloudyId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateCloudyId);
#endif
                    break;
                case WeatherState.PartlyCloudy:
#if ENVIRO_3
                    SetWeatherType(kStatePartlyCloudyId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStatePartlyCloudyId);
#endif
                    break;
                case WeatherState.Thunderstorms:
#if ENVIRO_3
                    SetWeatherType(kStateThunderstormId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateThunderstormsId);
#endif
                    break;
                case WeatherState.RainPrecipitation:
                    if (weatherData.Precipitation < kPrecipitationMean)
                    {
#if ENVIRO_3
                        SetWeatherType(kStateLightRainId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                        SetWeatherID(kStateRainPrecipitationId);
#endif
                    }
                    else
                    {
#if ENVIRO_3
                        SetWeatherType(kStateRainId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                        SetWeatherID(kStateHeavyPrecipitationId);
#endif
                    }
                    break;
                case WeatherState.RainSnowPrecipitation:
#if ENVIRO_3
                    SetWeatherType(kStateRainSnowId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateRainSnowPrecipitationId);
#endif
                    break;
                case WeatherState.SnowPrecipitation:
#if ENVIRO_3
                    SetWeatherType(kStateSnowId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    if (weatherData.Precipitation < kPrecipitationMean)
                    {
                        SetWeatherID(kStateLightSnowPrecipitationId);
                    }
                    else
                    {
                        SetWeatherID(kStateHeavySnowPrecipitationId);
                    }
#endif
                    break;
                case WeatherState.Mist:
#if ENVIRO_3
                    SetWeatherType(kStateFoggyId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateMistId);
#endif
                    break;
                case WeatherState.Windy:
#if ENVIRO_3
                    SetWeatherType(kStatePartlyCloudyId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateWindyId);
#endif
                    break;
                default:
#if ENVIRO_3
                    SetWeatherType(kStateClearId);
#elif ENVIRO_PRESENT && !ENVIRO_3
                    SetWeatherID(kStateClearId);
#endif
                    break;
            }
        }
#endregion

#region Wind Methods
        /// <summary>
        /// Enable/Disable wind zone direction.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetUseWindZoneDirection(bool enable)
        {
#if ENVIRO_PRESENT && (ENVIRO_HD || ENVIRO_LW)
            EnviroSkyMgr.instance.CloudSettings.useWindZoneDirection = enable;
#endif
        }

        /// <summary>
        /// Set the wind speed and direction.
        /// </summary>
        /// <param name="wind">A Wind class instance that represents wind data.</param>
        private void SetWind(Wind wind)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.CloudSettings.cloudsWindIntensity = Mathf.Clamp((wind.Speed / kThousand), kZeroValue, kOneValue);
            EnviroSkyMgr.instance.CloudSettings.cloudsWindDirectionX = wind.Direction.x;
            EnviroSkyMgr.instance.CloudSettings.cloudsWindDirectionY = wind.Direction.y;
#endif
        }
#endregion

#region Fog Methods
        /// <summary>
        /// Enable/Disable fog.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetFog(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useFog = enable;
            EnviroSkyMgr.instance.UpdateFogIntensity = enable;
            EnviroSkyMgr.instance.CustomFogIntensity = 0.01f;
#endif
        }

        /// <summary>
        /// Set the visibility value.
        /// </summary>
        /// <param name="visibility">A float value that represents the visibility in km.</param>
        private void SetVisibility(float visibility)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            if (EnviroSkyMgr.instance.Weather.currentActiveWeatherPreset)
            {
                EnviroSkyMgr.instance.Weather.currentActiveWeatherPreset.fogDensity = kOneValue / (visibility * kThousand);
            }
#endif
        }
#endregion

#region Clouds Methods
        /// <summary>
        /// Enable/Disable volumn clouds.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetVolumeClouds(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useVolumeClouds = enable;
#endif
        }

        /// <summary>
        /// Enable/Disable flat clouds.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetFlatClouds(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useFlatClouds = enable;
#endif
        }

        /// <summary>
        /// Enable/Disable particle clouds
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetParticleClouds(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useParticleClouds = enable;
#endif
        }

        /// <summary>
        /// Enable/Disable Aurora effect
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetAurora(float latitude)
        {
#if ENVIRO_PRESENT
            if (latitude >= kPolarCircleNorth || latitude <= kPolarCircleSouth)
            {
#if ENVIRO_3
                EnviroManager.instance.Aurora.Settings.auroraBrightness = kAuroraBrightnessDefault;
#else
                EnviroSkyMgr.instance.useAurora = true;
#endif
            }
            else
            {
#if ENVIRO_3
                EnviroManager.instance.Aurora.Settings.auroraBrightness = 0f;
#else
                EnviroSkyMgr.instance.useAurora = false;
#endif
            }
#endif
        }
#endregion

#region Lights Methods
        /// <summary>
        /// Enable/Disable Volumn Lighting.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetVolumeLighting(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useVolumeLighting = enable;
#endif
        }

        /// <summary>
        /// Enable/Disable sun light shafts clouds.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetSunShafts(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useSunShafts = enable;
#endif
        }

        /// <summary>
        /// Enable/Disable moon light shafts clouds and lower lightshafts intensity.
        /// </summary>
        /// <param name="enable">A bool value that represents the enabled state.</param>
        private void SetMoonShafts(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useMoonShafts = enable;
            EnviroSky.instance.profile.lightshaftsSettings.intensity = 0.2f;
#endif
        }

        /// <summary>
        /// Set the permanent lightshafts intensity
        /// </summary>
        private void SetLightShafts()
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.LightShaftsSettings.blurRadius = kLightshaftsBlurRadius;
            EnviroSkyMgr.instance.LightShaftsSettings.intensity = kLightshaftsIntensity;
#endif
        }

        /// <summary>
        /// Enable/Disable the distance blur on Enviro
        /// </summary>
        private void SetDistanceBlur(bool enable)
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            EnviroSkyMgr.instance.useDistanceBlur = enable;
#endif
        }
#endregion

#endregion
    }
}