//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Managers;
using System.Collections.Generic;
using UnityEngine;
#if ENVIRO_3
using Enviro;
#endif

namespace RealTimeWeather.Managers
{
    /// <summary>
    /// This class manages Enviro's weather presets.
    /// </summary>
    public class EnviroModulePresetsManager : MonoBehaviour
    {
        #region Private Const Variables
        private const string kRainAndSnowPresetName = "Rain And Snow";
        private const string kEnviroLightRainPresetName = "Light Rain";
        private const string kEnviroLightSnowPrefabName = "Light Snow HQ";
        #endregion

        #region Private Variables
#if ENVIRO_PRESENT && !ENVIRO_3
        private List<EnviroWeatherPreset> _presets;
        private EnviroWeatherPreset _rainAndSnowPreset;
#elif ENVIRO_3
        private List<EnviroWeatherType> _weatherTypes;
#endif
        #endregion

        #region Public Properties
#if ENVIRO_PRESENT && !ENVIRO_3
        public List<EnviroWeatherPreset> EnviroPresetsList
        {
            get { return _presets; }
            set { _presets = value; }
        }
#endif

#if ENVIRO_3
        public List<EnviroWeatherType> WeatherTypes
        {
            get { return _weatherTypes; }
            set { _weatherTypes = value; }
        }
#endif  
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes Enviro weather presets.
        /// </summary>
        public void InitializeEnviroPresets()
        {
#if ENVIRO_PRESENT && !ENVIRO_3
            if (EnviroSkyMgr.instance == null)
            {
                return;
            }

            EnviroZone activeZone = EnviroSkyMgr.instance.GetCurrentActiveZone();
            _presets = activeZone.zoneWeatherPresets;

            EnviroWeatherEffects snowEffect = new EnviroWeatherEffects();
            snowEffect.prefab = RealTimeWeatherManager.GetPrefab(kEnviroLightSnowPrefabName);
            EnviroWeatherPreset rainPreset = GetEnviroPreset(kEnviroLightRainPresetName);

            if (rainPreset && GetEnviroPreset(kRainAndSnowPresetName) == null)
            {
                _rainAndSnowPreset = Instantiate(rainPreset);
                _rainAndSnowPreset.Name = kRainAndSnowPresetName;
                _rainAndSnowPreset.name = kRainAndSnowPresetName;
                _rainAndSnowPreset.winter = true;
                _rainAndSnowPreset.effectSystems.Add(snowEffect);

                Vector3 effectPositionOffset = new Vector3(0f, 25f, 0f);
                Vector3 effectRotationOffset = new Vector3(-90f, 0f, -180f);
                foreach(var effect in _rainAndSnowPreset.effectSystems)
                {
                    effect.localPositionOffset = effectPositionOffset;
                    effect.localRotationOffset = effectRotationOffset;
                }

                activeZone.zoneWeatherPresets.Add(_rainAndSnowPreset);
            }
#elif ENVIRO_3
            WeatherTypes = EnviroManager.instance.configuration.Weather.Settings.weatherTypes;
#endif
        }

#if ENVIRO_PRESENT && !ENVIRO_3
        /// <summary>
        /// Finds and returns an Enviro weather preset by name. (Works only in editor!)
        /// </summary>
        /// <param name="presetName">A string value that represents the preset name.</param>
        public EnviroWeatherPreset GetEnviroPreset(string presetName)
        {
            EnviroWeatherPreset preset = ScriptableObject.CreateInstance<EnviroWeatherPreset>();

            if (_presets.Count > 0)
            {
                preset = _presets.Find(i => i.Name == presetName);
            }

            return preset;
        }
#elif ENVIRO_3
        /// <summary>
        /// Returns the Enviro 3 weather type (preset) based on it's name
        /// </summary>
        public EnviroWeatherType GetEnviroWeatherType(string presetName)
        {
            EnviroWeatherType weatherType = ScriptableObject.CreateInstance<EnviroWeatherType>();

            if (WeatherTypes.Count > 0)
            {
                weatherType = WeatherTypes.Find(i => i.name == presetName);
            }

            return weatherType;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new Enviro 3 weather preset
        /// </summary>
        public EnviroWeatherType CreateEnviroWeatherType(string presetName)
        {
            // Return if there already is a preset with the given name
            if (GetEnviroWeatherType(presetName) != null)
            {
                return null;
            }

            // Create the new weather preset and instantiate the scriptable object asset
            EnviroManager.instance.configuration.Weather.CreateNewWeatherType();
            var weatherType = WeatherTypes[WeatherTypes.Count - 1];
            weatherType.name = presetName;

            // Make sure there are no duplicate weather preset assets
            var assetGuid = UnityEditor.AssetDatabase.FindAssets($"t: EnviroWeatherType New Weather Type");
            if(assetGuid.Length != 0)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid[0]);
                UnityEditor.AssetDatabase.RenameAsset(assetPath, presetName);
            }

            return weatherType;
        }
#endif

        /// <summary>
        /// The EnviroWeatherType object will copy all the values from the duplicated preset
        /// </summary>
        /// <param name="weatherType">The weather type that will be given the copied values</param>
        /// <param name="duplicatedPresetName">The name of the preset that will be duplicated</param>
        /// <returns></returns>
        public EnviroWeatherType DuplicateEnviroWeatherType(EnviroWeatherType weatherType, string duplicatedPresetName)
        {
            if (weatherType == null)
            {
                return null;
            }

            foreach (var item in EnviroManager.instance.configuration.Weather.Settings.weatherTypes)
            {
                if (item.name == duplicatedPresetName)
                {
                    var rainPreset = ScriptableObject.Instantiate(GetEnviroWeatherType(duplicatedPresetName));
                    weatherType.audioOverride = rainPreset.audioOverride;
                    weatherType.auroraOverride = rainPreset.auroraOverride;
                    weatherType.cloudsOverride = rainPreset.cloudsOverride;
                    weatherType.effectsOverride = rainPreset.effectsOverride;
                    weatherType.environmentOverride = rainPreset.environmentOverride;
                    weatherType.flatCloudsOverride = rainPreset.flatCloudsOverride;
                    weatherType.fogOverride = rainPreset.fogOverride;
                    weatherType.lightingOverride = rainPreset.lightingOverride;
                    weatherType.lightningOverride = rainPreset.lightningOverride;

                    break;
                }
            }

            return weatherType;
        }
#endif
        #endregion
    }
}