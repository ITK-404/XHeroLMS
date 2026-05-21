//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Threading.Tasks;
using System.Collections.Generic;
using RealTimeWeather.Classes;
using RealTimeWeather.Managers;
using RealTimeWeather.Enums;
using RealTimeWeather;
using UnityEngine;
using UnityEngine.Rendering;
using RealTimeWeather.Data;
#if EXPANSE_PRESENT
using Expanse;
#endif
#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RealTimeWeather.WeatherControllers
{
    /// <summary>
    /// Class used to simulate weather using Expanse plug-in.
    /// </summary>
    public class ExpanseModuleController : MonoBehaviour
    {
#if EXPANSE_PRESENT
        #region Private Const Variables
        private const string kExpanseSunnySkyPath = "Gatos Optimized";
        private const string kExpansePartlySunnySkyPath = "Gatos Optimized";
        private const string kExpanseFairSkyPath = "Gatos Optimized";
        private const string kExpanseClearSkyPath = "Gatos Optimized";

        private const string kExpansePartlyClearSkyPath = "Gatos Optimized";
        private const string kExpansePartlyCloudySkyPath = "Minerva Optimized";
        private const string kExpanseMistySkyPath = "Minerva Optimized";
        private const string kExpanseWindySkyPath = "Minerva Optimized";
        private const string kExpanseCloudySkyPath = "Brewing Optimized";
        private const string kExpanseRainySkyPath = "Rain Cover Optimized";
        private const string kExpanseThunderstormSkyPath = "Rain Cover Optimized";
        private const string kExpanseRainSnowSkyPath = "Rain Cover Optimized";
        private const string kExpanseSnowSkyPath = "Snow Cover Optimized";

        private const string kExpanseProfileTransitionsPath = "/Resources/Expanse Profile Transitions";
        private const string kExpanseManagerName = "Expanse Sky (Interpolable)";
        private const float kSunnySkyCloudCoverage = 0f;
        private const float kPartlySunnySkyCloudCoverage = 0.25f;
        private const float kClearSkyCloudCoverage = 0f;
        private const float kPartlyClearSkyCloudCoverage = 0.26f;
        private const float kMistySkyCloudCoverage = 0.3f;
        private const float kFairSkyCloudCoverage = 0.19f;
        private const float kWindySkyCloudCoverage = 0.19f;
        private const float kPartlyCloudySkyCloudCoverage = 0.21f;
        private const float kCloudySkyCloudCoverage = 0.23f;
        private const float kRainySkyCloudCoverage = 0.35f;
        private const float kThunderstormSkyCloudCoverage = 0.6f;
        private const float kRainSnowSkyCloudCoverage = 0.35f;
        private const float kSnowSkyCloudCoverage = 0.35f;

        private const float kExpanseAutoModeInterpolationTime = 50;
        #endregion

        #region Private Variables
        [Header("Expanse References")]
        [SerializeField] private DateTimeController _expanseDateTime;
        [SerializeField] private CloudLayerInterpolator _expanseCloudInterpolator;
        [SerializeField] private ProceduralCloudVolume _expanseCloudVolume;
        [SerializeField] private CreativeFog _expanseFog;
        [SerializeField] private CreativeSun _expanseSun;
        [SerializeField] private CreativeCloudVolume _expanseCreativeCloudVolume;

        [Header("Light Color")]
        [SerializeField] private float _temperatureColorChangeThreshold = 10;
        [SerializeField] private Color _expanseHotTempTint = new Color(0.81f, 0.83f, 0.7f);
        [SerializeField] private Color _expanseColdTempTint = new Color(0.67f, 0.85f, 1f);

        [Header("Cloud Presets")]
        [SerializeField] private SerializedDictionary<WeatherState, UniversalCloudLayer> _cloudLayerTransitions = new SerializedDictionary<WeatherState, UniversalCloudLayer>();
        [SerializeField] private SerializedDictionary<WeatherState, UniversalCloudLayer> _cloudLayerTransitionsAndCoverage = new SerializedDictionary<WeatherState, UniversalCloudLayer>();
        #endregion

        #region Public Variables
        public SerializedDictionary<WeatherState, string> expansePresetsPaths = new SerializedDictionary<WeatherState, string>();
        public SerializedDictionary<WeatherState, string> expansePresetsPathsAndCoverage = new SerializedDictionary<WeatherState, string>();
        public SerializedDictionary<WeatherState, float> expanseCloudCoverage = new SerializedDictionary<WeatherState, float>();
        
        [Header("Volume Profile")]
        public VolumeProfile expanseVolumeProfile;
        #endregion

        #region Public Methods
        /// <summary>
        /// This function creates the expanse manager and adds it as a child to the "RealTimeWeatherManager" manager.
        /// </summary>
        public void CreateExpanseManagerInstance()
        {
#if UNITY_EDITOR
            PopulateWithDefaultData();
            _cloudLayerTransitions = new SerializedDictionary<WeatherState, UniversalCloudLayer>();

            _expanseCloudInterpolator = FindObjectOfType<CloudLayerInterpolator>();
            _expanseCreativeCloudVolume = FindObjectOfType<CreativeCloudVolume>();
            _expanseCloudVolume = FindObjectOfType<ProceduralCloudVolume>();
            _expanseDateTime = FindObjectOfType<DateTimeController>();
            _expanseFog = FindObjectOfType<CreativeFog>();
            _expanseSun = FindObjectOfType<CreativeSun>();

            if (_expanseDateTime == null || _expanseCloudVolume == null || _expanseFog == null || _expanseSun == null)
            {
                GameObject expansePrefab = RealTimeWeatherManager.GetPrefab(kExpanseManagerName);
                GameObject expanseInstance = Instantiate(expansePrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
                expanseInstance.name = kExpanseManagerName;

                _expanseDateTime = gameObject.GetComponentInChildren<DateTimeController>();
                _expanseCloudInterpolator = gameObject.GetComponentInChildren<CloudLayerInterpolator>();
                _expanseCloudVolume = gameObject.GetComponentInChildren<ProceduralCloudVolume>();
                _expanseFog = gameObject.GetComponentInChildren<CreativeFog>();
                _expanseSun = gameObject.GetComponentInChildren<CreativeSun>();
                GlobalSettings expanseQualitySettings = gameObject.GetComponentInChildren<GlobalSettings>();
                Volume expanseSkyAndFogVolume = gameObject.GetComponentInChildren<Volume>();

                expanseQualitySettings.m_cloudShadowMapQuality = Datatypes.Quality.High;
                expanseQualitySettings.m_atmosphereTextureQuality = Datatypes.Quality.High;
                expanseQualitySettings.m_fogQuality = Datatypes.Quality.High;

                UpdateExpanseVolume();
                _expanseCloudInterpolator.m_transitionTime = kExpanseAutoModeInterpolationTime;
            }

            AddExpanseCloudCoverageValues();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            CloudPresetMapper _presetMapper;
            _presetMapper = gameObject.AddComponent<CloudPresetMapper>();
            if (_presetMapper)
            {
                AddExpansePresetTransitions(_presetMapper);
            }
            else
            {
                Debug.LogError("Could not create the Expanse preset transitions");
            }
#endif
        }

        public void RemoveProfileTransitions()
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(RealTimeWeatherManager.instance.RelativePath + kExpanseProfileTransitionsPath);
#endif
        }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += OnWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick += OnForecastWeatherUpdate;
                if (_expanseCloudInterpolator)
                {
                    if (_cloudLayerTransitions.ContainsKey(WeatherState.Cloudy))
                    {
                        _expanseCloudInterpolator.LoadPreset(_cloudLayerTransitions[WeatherState.Cloudy]);
                    }
                    else
                    {
                        _expanseCloudInterpolator.LoadPreset(_cloudLayerTransitionsAndCoverage[WeatherState.Cloudy]);
                    }
                   
                }
            }
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= OnWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick -= OnForecastWeatherUpdate;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles the weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void OnWeatherUpdate(WeatherData weatherData)
        {
            if(weatherData == null)
            {
                return;
            }

            SetTimeZone(weatherData);
            SetDateTime(weatherData);
            SetVisibility(weatherData);
            SetWindSpeed(weatherData);

            UniversalCloudLayer cloudLayer;
            if(_cloudLayerTransitions.ContainsKey(weatherData.WeatherState))
            {
                cloudLayer = _cloudLayerTransitions[weatherData.WeatherState];
            }
            else
            {
                cloudLayer = _cloudLayerTransitionsAndCoverage[weatherData.WeatherState];
            }

            if (_expanseCloudInterpolator)
            {
                _expanseCloudInterpolator.m_autoMode = true;
                _expanseCloudInterpolator.LoadPreset(cloudLayer);
            }
            else
            {
                _expanseCloudVolume.FromUniversal(cloudLayer);
            }
            SetTemperature(weatherData);
        }

        /// <summary>
        /// Handles the forecast data update event.
        /// </summary>
        /// <param name="currentWeatherData">The current forecast weather data</param>
        /// <param name="nextWeatherData">The next forecast weather data</param>
        /// <param name="weatherProgress">The current transition progress from the current weather data to the next one</param>
        private void OnForecastWeatherUpdate(WeatherData currentWeatherData, WeatherData nextWeatherData, double weatherProgress)
        {
            if(currentWeatherData == null || nextWeatherData == null)
            {
                return;
            }

            SetTimeZone(currentWeatherData);
            SetDateTime(currentWeatherData);
            SetVisibility(currentWeatherData);
            SetWindSpeed(currentWeatherData);

            UniversalCloudLayer currentCloudLayer;
            if (_cloudLayerTransitions.ContainsKey(currentWeatherData.WeatherState))
            {
                currentCloudLayer = _cloudLayerTransitions[currentWeatherData.WeatherState];
            }
            else
            {
                currentCloudLayer = _cloudLayerTransitionsAndCoverage[currentWeatherData.WeatherState];
            }

            UniversalCloudLayer nextCloudLayer;
            if (_cloudLayerTransitions.ContainsKey(nextWeatherData.WeatherState))
            {
                nextCloudLayer = _cloudLayerTransitions[nextWeatherData.WeatherState];
            }
            else
            {
                nextCloudLayer = _cloudLayerTransitionsAndCoverage[nextWeatherData.WeatherState];
            }

            if (_expanseCloudInterpolator)
            {
                _expanseCloudInterpolator.m_autoMode = false;
                _expanseCloudInterpolator.m_currentPreset = currentCloudLayer; 
                _expanseCloudInterpolator.m_targetPreset = nextCloudLayer;
                _expanseCloudInterpolator.m_interpolationAmount = (float)weatherProgress;
            }
            else
            {
                _expanseCloudVolume.FromUniversal(nextCloudLayer);
                _expanseCreativeCloudVolume.m_coverage = Mathf.Lerp(expanseCloudCoverage[currentWeatherData.WeatherState], expanseCloudCoverage[nextWeatherData.WeatherState], (float)weatherProgress);
            }
            SetTemperature(currentWeatherData);
        }

        /// <summary>
        /// Adds the default cloud coverage values to the dictionary
        /// </summary>
        private void AddExpanseCloudCoverageValues()
        {
#if UNITY_EDITOR && EXPANSE_PRESENT
            expanseCloudCoverage = new SerializedDictionary<WeatherState, float>();
            expanseCloudCoverage.Add(WeatherState.Sunny, kSunnySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.PartlySunny, kPartlySunnySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Clear, kClearSkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.PartlyClear, kPartlyClearSkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Mist, kMistySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Cloudy, kCloudySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Windy, kWindySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Fair, kFairSkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.PartlyCloudy, kPartlyCloudySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.RainPrecipitation, kRainySkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.Thunderstorms, kThunderstormSkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.RainSnowPrecipitation, kRainSnowSkyCloudCoverage);
            expanseCloudCoverage.Add(WeatherState.SnowPrecipitation, kSnowSkyCloudCoverage);
#endif
        }

        /// <summary>
        /// Sets the date and time in Expanse.
        /// </summary>
        /// <param name="weatherData"></param>
        private void SetDateTime(WeatherData weatherData)
        {
            if(!_expanseDateTime)
            {
                return;
            }

            _expanseDateTime.m_timeUTCOffset = weatherData.UTCOffset.Hours;
            _expanseDateTime.m_timeLocal.year = weatherData.DateTime.Year;
            _expanseDateTime.m_timeLocal.month = weatherData.DateTime.Month;
            _expanseDateTime.m_timeLocal.day = weatherData.DateTime.Day;
            _expanseDateTime.m_timeLocal.hour = weatherData.DateTime.Hour;
            _expanseDateTime.m_timeLocal.minute = weatherData.DateTime.Minute;
            _expanseDateTime.m_timeLocal.second = weatherData.DateTime.Second;
            _expanseDateTime.m_timeLocal.millisecond = weatherData.DateTime.Millisecond;
        }

        /// <summary>
        /// Sets the time zone in Expanse.
        /// </summary>
        /// <param name="weatherData"></param>
        private void SetTimeZone(WeatherData weatherData)
        {
            if(!_expanseDateTime)
            {
                return;
            }
            _expanseDateTime.m_longitude = weatherData.Localization.Longitude;
            _expanseDateTime.m_latitude = weatherData.Localization.Latitude;
        }

        /// <summary>
        /// Sets the wind speed in Expanse.
        /// </summary>
        /// <param name="weatherData"></param>
        private void SetWindSpeed(WeatherData weatherData)
        {
            if(!_expanseCloudVolume)
            {
                return;
            }
            _expanseCloudVolume.m_coverageVelocity = weatherData.Wind.Direction * Mathf.Lerp(0f, 0.007f, weatherData.Wind.Speed / 55);
            _expanseCloudVolume.m_baseVelocity = weatherData.Wind.Direction * Mathf.Lerp(0f, 0.05f, weatherData.Wind.Speed / 55);
        }

        /// <summary>
        /// Sets the fog visibility distance in expanse.
        /// </summary>
        /// <param name="weatherData"></param>
        private void SetVisibility(WeatherData weatherData)
        {
            if(!_expanseFog)
            {
                return;
            }

            _expanseFog.m_visibilityDistance = (float)Utilities.ConvertKilometersToMeters(weatherData.Visibility);
        }

        /// <summary>
        /// Sets the sun light tint based on temperature in expanse.
        /// </summary>
        /// <param name="weatherData"></param>
        private void SetTemperature(WeatherData weatherData)
        {
            if (!_expanseSun)
            {
                return;
            }

            _expanseSun.m_lightTint = weatherData.Temperature > _temperatureColorChangeThreshold ? _expanseHotTempTint : _expanseColdTempTint;
        }

        private void UpdateExpanseVolume()
        {
            Volume expanseVolume = gameObject.GetComponentInChildren<Volume>();
            expanseVolumeProfile = expanseVolume.profile;
            if (expanseVolumeProfile)
            {
                expanseVolumeProfile.Add(typeof(HDShadowSettings));
                HDShadowSettings shadowSettings = (HDShadowSettings)expanseVolumeProfile.components[expanseVolumeProfile.components.Count - 1];
                shadowSettings.maxShadowDistance.value = 50.0f;
                shadowSettings.maxShadowDistance.overrideState = true;

                expanseVolumeProfile.Add(typeof(Vignette));
                Vignette vignetteSettings = (Vignette)expanseVolumeProfile.components[expanseVolumeProfile.components.Count - 1];
                vignetteSettings.intensity.value = 0.3f;
                vignetteSettings.intensity.overrideState = true;

                expanseVolumeProfile.Add(typeof(Bloom));
                Bloom bloomSettings = (Bloom)expanseVolumeProfile.components[expanseVolumeProfile.components.Count - 1];
                bloomSettings.threshold.value = 3;
                bloomSettings.threshold.overrideState = true;
                bloomSettings.intensity.value = 0.2f;
                bloomSettings.intensity.overrideState = true;
            }
        }

        /// <summary>
        /// Creates all the possible transition between the loaded profiles
        /// </summary>
        /// <param name="presetMapper">Used for creating the transition between the cloud layers</param>
#if UNITY_EDITOR
        private void AddExpansePresetTransitions(CloudPresetMapper presetMapper)
        {
            if (!AssetDatabase.IsValidFolder(RealTimeWeatherManager.instance.RelativePath + kExpanseProfileTransitionsPath))
            {
                AssetDatabase.CreateFolder(RealTimeWeatherManager.instance.RelativePath + "/Resources", "Expanse Profile Transitions");
            }

            foreach (KeyValuePair<WeatherState, string> profile in expansePresetsPaths)
            {
                string profileStr = RealTimeWeatherManager.instance.RelativePath + kExpanseProfileTransitionsPath + "/" + profile.Key + " mapped to Cloudy.asset";
                presetMapper.LoadSourcePreset(profile.Value);
                presetMapper.LoadTargetPreset(expansePresetsPaths[WeatherState.Cloudy]);
                presetMapper.Map(profileStr);
                _cloudLayerTransitions.Add(profile.Key, AssetDatabase.LoadAssetAtPath<UniversalCloudLayer>(profileStr));
            }

            foreach (KeyValuePair<WeatherState, string> profile in expansePresetsPathsAndCoverage)
            {
                string profileStr = RealTimeWeatherManager.instance.RelativePath + kExpanseProfileTransitionsPath + "/" + profile.Key + " mapped to Sunny.asset";
                presetMapper.LoadSourcePreset(profile.Value);
                presetMapper.LoadTargetPreset(expansePresetsPaths[WeatherState.Cloudy]);
                presetMapper.Map(profileStr);
                UniversalCloudLayer cloudLayer = AssetDatabase.LoadAssetAtPath<UniversalCloudLayer>(profileStr);
                cloudLayer.renderSettings.coverageIntensity = expanseCloudCoverage[profile.Key];
                _cloudLayerTransitionsAndCoverage.Add(profile.Key, cloudLayer);
            }
        }
#endif

        /// <summary>
        /// Populates the cloud presets from the expanse module.
        /// </summary>
        private void PopulateWithDefaultData()
        {
#if UNITY_EDITOR && EXPANSE_PRESENT
            AddExpansePresetDefaultValues();

            foreach (KeyValuePair<WeatherState, string> item in expansePresetsPaths)
            {
                if (item.Value == null)
                {
                    Debug.LogError("Could not populate all preset fields");
                    break;
                }
            }
            AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Adds the default presets to the dictionary
        /// </summary>
        private void AddExpansePresetDefaultValues()
        {
#if UNITY_EDITOR && EXPANSE_PRESENT
            expansePresetsPaths = new SerializedDictionary<WeatherState, string>();
            expansePresetsPathsAndCoverage = new SerializedDictionary<WeatherState, string>();

            expansePresetsPathsAndCoverage.Add(WeatherState.PartlySunny, GetExpansePreset(kExpansePartlySunnySkyPath));
            expansePresetsPathsAndCoverage.Add(WeatherState.Sunny, GetExpansePreset(kExpanseSunnySkyPath));
            expansePresetsPathsAndCoverage.Add(WeatherState.Clear, GetExpansePreset(kExpanseClearSkyPath));
            expansePresetsPathsAndCoverage.Add(WeatherState.Fair, GetExpansePreset(kExpanseFairSkyPath));

            expansePresetsPaths.Add(WeatherState.PartlyClear, GetExpansePreset(kExpansePartlyClearSkyPath));
            expansePresetsPaths.Add(WeatherState.Cloudy, GetExpansePreset(kExpanseCloudySkyPath));
            expansePresetsPaths.Add(WeatherState.PartlyCloudy, GetExpansePreset(kExpansePartlyCloudySkyPath));
            expansePresetsPaths.Add(WeatherState.Mist, GetExpansePreset(kExpanseMistySkyPath));
            expansePresetsPaths.Add(WeatherState.Thunderstorms, GetExpansePreset(kExpanseThunderstormSkyPath));
            expansePresetsPaths.Add(WeatherState.RainSnowPrecipitation, GetExpansePreset(kExpanseRainSnowSkyPath));
            expansePresetsPaths.Add(WeatherState.RainPrecipitation, GetExpansePreset(kExpanseRainySkyPath));
            expansePresetsPaths.Add(WeatherState.SnowPrecipitation, GetExpansePreset(kExpanseSnowSkyPath));
            expansePresetsPaths.Add(WeatherState.Windy, GetExpansePreset(kExpanseWindySkyPath));
#endif
        }

        /// <summary>
        /// Finds and returns an Expanse preset specified by name.
        /// </summary>
        /// <param name="presetName">A string value that represents the preset name.</param>
#if UNITY_EDITOR && EXPANSE_PRESENT
        private string GetExpansePreset(string presetName)
        {
            string[] assets = AssetDatabase.FindAssets(presetName);
            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[idx]);
                if (path.Contains(".asset"))
                {
                    return path;
                }
            }
            return null;
        }
#endif
        #endregion
#endif
    }
}
