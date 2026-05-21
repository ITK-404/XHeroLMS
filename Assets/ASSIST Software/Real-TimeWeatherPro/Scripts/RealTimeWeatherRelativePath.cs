//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RealTimeWeather.Managers;

namespace RealTimeWeather
{
    [InitializeOnLoad]
    public class RealTimeWeatherRelativePath
    {
        private const int kPathDiscardCharacterCount = 39;

        static RealTimeWeatherRelativePath()
        {
            EditorApplication.projectChanged += UpdatePath;
        }

        /// <summary>
        /// Updates the path of the RTW folder every time the project has been changed
        /// </summary>
        public static void UpdatePath()
        {
            string[] assets = AssetDatabase.FindAssets("RealTimeWeatherRelativePath t:Script");
            if (assets.Length == 1)
            {
                string str = AssetDatabase.GUIDToAssetPath(assets[0]);
                str = str.Remove(str.Length - kPathDiscardCharacterCount);
                if (RealTimeWeatherManager.instance)
                {
                    RealTimeWeatherManager.instance.RelativePath = str;
                }
            }
            else
            {
                Debug.LogError("Could not determine Real-Time Weather folder path");
            }
        }
    }

}
#endif
