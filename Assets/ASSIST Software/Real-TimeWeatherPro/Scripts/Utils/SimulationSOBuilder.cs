//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEditor;
using UnityEngine;
using RealTimeWeather.Managers;
using RealTimeWeather.Data;
using RealTimeWeather.Simulation;
#if UNITY_EDITOR
namespace RealTimeWeather
{
    public class SimulationSOBuilder
    {
        private const string kSimulationsPath = "/Resources/Forecast/Simulations";
        private const string kResourcesForecastPath = "/Resources/Forecast";
        private const string kProviderSimulationsPath = "/Resources/Forecast/ProviderSimulations";

        /// <summary>
        /// Creates a new Simulation data container in the Data/Simulations folder
        /// </summary>
        /// <returns>The simulation data profile instance</returns>
        public ForecastData CreateNewSimulation()
        {
            return CreateSimulationSO(kSimulationsPath, "Simulations");
        }

        /// <summary>
        /// Creates a new Simulation data container in the Data/ProviderSimulations folder
        /// </summary>
        /// <returns>The simulation data profile instance</returns>
        public ForecastData CreateNewProviderSimulation()
        {
           return CreateSimulationSO(kSimulationsPath, "Simulations");
        }

        public ForecastData DuplicateSimulationSO(string path)
        {
            var _realTimeWeatherManager = RealTimeWeatherManager.instance;

            var asset = (ForecastData)AssetDatabase.LoadAssetAtPath(_realTimeWeatherManager.RelativePath + path + ".asset", typeof(ForecastData));
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CopyAsset(assetPath, newAssetPath);

            var newAsset = AssetDatabase.LoadAssetAtPath(newAssetPath, typeof(ForecastData));
            var forecast = (ForecastData)AssetDatabase.LoadAssetAtPath(newAssetPath, typeof(ForecastData));
            forecast.SimulationName = newAsset.name;
            EditorUtility.SetDirty(forecast);
            AssetDatabase.SaveAssets();

            return forecast;
        }

        private ForecastData CreateSimulationSO(string path, string folder)
        {
            var _realTimeWeatherManager = RealTimeWeatherManager.instance;
            ForecastData newSimulation = ScriptableObject.CreateInstance<ForecastData>();
            newSimulation.SimulationName = "Simulation " + InspectorUtils.SimulationCount;

            if (!AssetDatabase.IsValidFolder(_realTimeWeatherManager.RelativePath + path))
            {
                AssetDatabase.CreateFolder(_realTimeWeatherManager.RelativePath + kResourcesForecastPath, folder);
            }

            while (AssetDatabase.LoadAssetAtPath($"{_realTimeWeatherManager.RelativePath + path}/{newSimulation.SimulationName}.asset", typeof(ForecastData)) != null)
            {
                InspectorUtils.SimulationCount++;
                newSimulation.SimulationName = "Simulation " + InspectorUtils.SimulationCount;
            }

            InspectorUtils.SimulationCount++;
            AssetDatabase.CreateAsset(newSimulation, $"{_realTimeWeatherManager.RelativePath + path}/{newSimulation.SimulationName}.asset");
            AssetDatabase.SaveAssets();

            return newSimulation;
        }
    }
}
#endif