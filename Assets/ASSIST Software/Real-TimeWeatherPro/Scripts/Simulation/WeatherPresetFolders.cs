//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Managers;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealTimeWeather.Simulation
{
    [CreateAssetMenu(fileName = "Weather Presets Folders", menuName = "Real-Time Weather/Timelapse/Weather Presets Folders", order = 0)]
    public class WeatherPresetFolders : ScriptableObject
    {
        private const string kSimulationsPath = "/Resources/Forecast/Forecast Presets";

        public WeatherPresets DefaultPreset;
        public List<WeatherPresets> CustomPresets;
        [FormerlySerializedAs("forecastImages")]
        public List<Texture2D> ForecastImages = new List<Texture2D>();

#if UNITY_EDITOR
        public void CreateNewCustomPresetsFolder(string name)
        {
            if (name == string.Empty || name  == null) return;

            var newWeatherPresets = ScriptableObject.CreateInstance<WeatherPresets>();
            newWeatherPresets.folderName = name;

            if (AssetDatabase.LoadAssetAtPath($"{RealTimeWeatherManager.instance.RelativePath}/{kSimulationsPath}/{name}.asset", typeof(WeatherPresets)) != null)
            {
                Debug.LogWarning("A folder with the same name already exists");
                return;
            }

            AssetDatabase.CreateAsset(newWeatherPresets, $"{RealTimeWeatherManager.instance.RelativePath}/{kSimulationsPath}/{name}.asset");
            AssetDatabase.SaveAssets();
            CustomPresets.Add(newWeatherPresets);
        }

        public void DeletePresetsFolder(string name)
        {
            if (AssetDatabase.LoadAssetAtPath($"{RealTimeWeatherManager.instance.RelativePath}/{kSimulationsPath}/{name}.asset", typeof(WeatherPresets)) != null)
            {
                AssetDatabase.DeleteAsset($"{RealTimeWeatherManager.instance.RelativePath}/{kSimulationsPath}/{name}.asset");
            }
        }
#endif
    }
}