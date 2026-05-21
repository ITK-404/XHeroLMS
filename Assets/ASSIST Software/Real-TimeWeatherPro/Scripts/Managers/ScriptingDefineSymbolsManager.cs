//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

using System;
using UnityEngine;

namespace RealTimeWeather.Managers
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public class ScriptingDefineSymbolsManager
    {
        #region Private Const Variables
        private const int kAssetFolderNameCharacterCount = 6;
        //Render Pipeline Assemblies
        private const string kUniversalRenderPipelineStr = "Unity.RenderPipelines.Universal.Runtime";
        private const string kHighDefinitionRenderPipelineStr = "Unity.RenderPipelines.HighDefinition.Runtime";
        private const string kCommaStr = ";";
        // Enviro module constants
        private const string kEnviroAssetStr = "Enviro - Sky and Weather";
        private const string kEnviroSymbolStr = "ENVIRO_PRESENT";
        private const string kEnviro3SymbolStr = "ENVIRO_3";
        private const string kEnviroHDSymbolStr = "ENVIRO_HD";
        private const string kEnviroLWSymbolStr = "ENVIRO_LW";
        // Tenkoku module constants
        private const string kTenkokuAssetStr = "TENKOKU - DYNAMIC SKY";
        private const string kTenkokuSymbolStr = "TENKOKU_PRESENT";
        // Atmos module constants
        private const string kAtmosAssetStr = "MassiveCloudsAtmos";
        private const string kAtmosSymbolStr = "ATMOS_PRESENT";
        private const string kAtmosPadPackagePath = "/External Weather Engine Data/Atmos/AtmosPadPackage.unitypackage";
        private const string kAtmosPadAssetPath = "/Prefabs/Atmos Prefabs/Atmos Pad Package Prefabs";
        // Expanse module constants
        private const string kExpanseAssetStr = "Expanse";
        private const string kExpanseSymbolStr = "EXPANSE_PRESENT";
        //Easy Sky module constants
        private const string kEasySkyAssetStr = "EasySkyWeatherManager";
        private const string kEasySkySymbolStr = "EASYSKY_PRESENT";
        // Crest module constants
        private const string kCrestHdrpAssetStr = "UnderwaterMaskHDRP";
        private const string kCrestUrpAssetStr = "Crest URP";
        private const string kCrestHdrpSymbolStr = "CREST_HDRP_PRESENT";
        private const string kCrestUrpSymbolStr = "CREST_URP_PRESENT";
        //HDRP constants
        private const string kHDRenderPipelineSymbolStr = "UNITY_PIPELINE_HDRP";
        private const string kHDRenderPipelineAssetNameStr = "HDRenderPipelineAsset";
        //URP constants
        private const string kUniversalRenderPipelineSymbolStr = "UNITY_PIPELINE_URP";
        private const string kUniversalRenderPipelineAssetNameStr = "UniversalRenderPipelineAsset";

        //KWS constants
        private const string kKWSAssembly = "KWS";
        #endregion

        #region Private Variables
        private static BuildTargetGroup[] _buildTargetPlatforms = { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS };
        #endregion

        #region Constructor
        static ScriptingDefineSymbolsManager()
        {
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += UpdateDefines;
#else
            EditorApplication.update += UpdateDefines;
#endif
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the scripting define symbols.
        /// </summary>
        private static void UpdateDefines()
        {
            ValidateEnviroDefines();
            ValidateTenkokuDefines();
            ValidateAtmosDefines();
            ValidateExpanseDefines();
            ValidateEasySkyDefines();
            ValidateHDRenderPipelineDefines();
            ValidateUniversalRenderPipelineDefines();
            ValidateKWSDefinesURP();
            ValidateKWSDefinesHDRP();
            ValidateCrestHDRPDefines();
            ValidateCrestURPDefines();
        }

        /// <summary>
        /// Attempts to add a new #define constant to the Player Settings
        /// </summary>
        /// <param name="newDefineSymbol">A string value that represents the symbol to define.</param>
        /// <param name="targetPlatforms">A BuildTargetGroup array that specifies the target platforms.</param>
        private static void AddDefine(string newDefineSymbol, BuildTargetGroup[] targetPlatforms = null)
        {
            if (targetPlatforms == null)
            {
                return;
            }

            foreach (BuildTargetGroup target in targetPlatforms)
            {
                if (target == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                string targetDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

                if (!targetDefines.Contains(newDefineSymbol))
                {
                    if (targetDefines.Length > 0)
                    {
                        targetDefines += kCommaStr;
                    }

                    targetDefines += newDefineSymbol;
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, targetDefines);
                }
            }
        }

        /// <summary>
        /// Attempts to remove a #define constant from the Player Settings
        /// </summary>
        /// <param name="defineSymbol">A string value that represents the symbol to remove.</param>
        /// <param name="targetPlatforms">A BuildTargetGroup array that specifies the target platforms.</param>
        private static void RemoveDefine(string defineSymbol, BuildTargetGroup[] targetPlatforms = null)
        {
            if (targetPlatforms == null)
            {
                return;
            }

            foreach (BuildTargetGroup target in targetPlatforms)
            {
                string targetDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
                int symbolIndex = targetDefines.IndexOf(defineSymbol);

                if (symbolIndex < 0)
                {
                    continue;
                }
                else if (symbolIndex > 0)
                {
                    symbolIndex -= 1;
                }

                int length = Math.Min(defineSymbol.Length + 1, targetDefines.Length - symbolIndex);
                targetDefines = targetDefines.Remove(symbolIndex, length);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, targetDefines);
            }
        }

        /// <summary>
        /// Validates the presence of Enviro in the project and adds/removes the #define constant "ENVIRO_PRESENT".
        /// We can't use the assembly definitions to detect Enviro presence as no assembly file is located in the Enviro asset folder.
        /// </summary>
        private static void ValidateEnviroDefines()
        {
            string[] enviroAssets = AssetDatabase.FindAssets(kEnviroAssetStr, null);

            if (enviroAssets.Length != 0)
            {
#if !ENVIRO_PRESENT
                AddDefine(kEnviroSymbolStr, _buildTargetPlatforms);
#endif
#if UNITY_ANDROID || UNITY_IOS
#if !ENVIRO_HD
                AddDefine(kEnviroHDSymbolStr, _buildTargetPlatforms);
#endif

#if !ENVIRO_LW
                AddDefine(kEnviroLWSymbolStr, _buildTargetPlatforms);
#endif
#endif

            }
            else
            {
#if ENVIRO_PRESENT
                RemoveDefine(kEnviroSymbolStr, _buildTargetPlatforms);
#endif

#if ENVIRO_3
                RemoveDefine(kEnviro3SymbolStr, _buildTargetPlatforms);
#endif

#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
#if ENVIRO_HD
                RemoveDefine(kEnviroHDSymbolStr, _buildTargetPlatforms);
#endif

#if ENVIRO_LW
                RemoveDefine(kEnviroLWSymbolStr, _buildTargetPlatforms);
#endif
#endif
            }
        }

        /// <summary>
        /// Validates the presence of Tenkoku in the project and adds/removes the #define constant "TENKOKU_PRESENT".
        /// We can't use the assembly definitions to detect TENKOKU presence as no assembly file is located in the TENKOKU asset folder.
        /// </summary>
        private static void ValidateTenkokuDefines()
        {
            string[] tentokuAssets = AssetDatabase.FindAssets(kTenkokuAssetStr, null);

            if (tentokuAssets.Length != 0)
            {
#if !TENKOKU_PRESENT
                AddDefine(kTenkokuSymbolStr, _buildTargetPlatforms);
#endif
            }
            else
            {
#if TENKOKU_PRESENT
                RealTimeWeatherManager.instance.DeactivateTenkokuSimulation();
                RemoveDefine(kTenkokuSymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        /// <summary>
        /// Validates the presence of Massive Clouds Atmos in the project and adds/removes the #define constant "ATMOS_PRESENT".
        /// We can't use the assembly definitions to detect Atmos presence as no assembly file is located in the Massive Atmos Clouds asset folder.
        /// </summary>
        private static void ValidateAtmosDefines()
        {
            string[] atmosAssets = AssetDatabase.FindAssets(kAtmosAssetStr, null);

            if (atmosAssets.Length != 0)
            {
#if !ATMOS_PRESENT
                AddDefine(kAtmosSymbolStr, _buildTargetPlatforms);
                string appPathStr = Application.dataPath;
                appPathStr = appPathStr.Remove(appPathStr.Length - kAssetFolderNameCharacterCount);
                PackageManager.ImportPackage(appPathStr + RealTimeWeatherManager.instance.RelativePath + kAtmosPadPackagePath, false);
#endif
            }
            else
            {
#if ATMOS_PRESENT
                string appPathStr = Application.dataPath;
                appPathStr = appPathStr.Remove(appPathStr.Length - kAssetFolderNameCharacterCount);
                PackageManager.DeleteDirectory(appPathStr + RealTimeWeatherManager.instance.RelativePath + kAtmosPadAssetPath);
                RealTimeWeatherManager.instance.DeactivateAtmosSimulation();
                RemoveDefine(kAtmosSymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        /// <summary>
        /// Validates the presence of HD Render Pipeline setup in the project and adds/removes the #define constant "UNITY_PIPELINE_HDRP".
        /// </summary>
        private static void ValidateHDRenderPipelineDefines()
        {
#if UNITY_6000_0_OR_NEWER
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
#else
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
#endif

            if (renderPipelineAsset != null && renderPipelineAsset.GetType().ToString().Contains(kHDRenderPipelineAssetNameStr))
            {
                AddDefine(kHDRenderPipelineSymbolStr, _buildTargetPlatforms);
            }
            else
            {
                RemoveDefine(kHDRenderPipelineSymbolStr, _buildTargetPlatforms);
            }
        }

        /// <summary>
        /// Validates the presence of Universal Render Pipeline setup in the project and adds/removes the #define constant "UNITY_PIPELINE_URP".
        /// </summary>
        private static void ValidateUniversalRenderPipelineDefines()
        {
#if UNITY_6000_0_OR_NEWER
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
#else
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
#endif
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().ToString().Contains(kUniversalRenderPipelineAssetNameStr))
            {
                AddDefine(kUniversalRenderPipelineSymbolStr, _buildTargetPlatforms);
            }
            else
            {
                RemoveDefine(kUniversalRenderPipelineSymbolStr, _buildTargetPlatforms);
            }
        }

        /// <summary>
        /// Validates the presence of Expanse in the project and adds/removes the #define constant "EXPANSE_PRESENT".
        /// </summary>
        private static void ValidateExpanseDefines()
        {
            if (ValidateAssetDefines(kExpanseAssetStr, kHighDefinitionRenderPipelineStr))
            {
#if !EXPANSE_PRESENT
                AddDefine(kExpanseSymbolStr, _buildTargetPlatforms);
#endif
            }
            else
            {
#if EXPANSE_PRESENT
                RealTimeWeatherManager.instance.DeactivateExpanseSimulation();
                RemoveDefine(kExpanseSymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        /// <summary>
        /// Validates the presence of Easy Sky in the project and adds/removes the #define constant "EASYSKY_PRESENT".
        /// </summary>
        private static void ValidateEasySkyDefines()
        {
            string[] easySkyAssets = AssetDatabase.FindAssets(kEasySkyAssetStr, null);

            if (easySkyAssets.Length != 0)
            {
#if !EASYSKY_PRESENT
                AddDefine(kEasySkySymbolStr, _buildTargetPlatforms);
#endif
            }
            else
            {
#if EASYSKY_PRESENT
                RealTimeWeatherManager.instance.DeactivateEasySkySimulation();
                RemoveDefine(kEasySkySymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        /// <summary>
        /// Validates the presence of Crest HDRP in the project and adds/removes the #define constant "CREST_HDRP_PRESENT".
        /// </summary>
        private static void ValidateCrestHDRPDefines()
        {
            string[] crestAssets = AssetDatabase.FindAssets(kCrestHdrpAssetStr, null);
            if (crestAssets.Length != 0)
            {
#if !CREST_HDRP_PRESENT
                AddDefine(kCrestHdrpSymbolStr, _buildTargetPlatforms);
#endif
            }
            else
            {
#if CREST_HDRP_PRESENT
                RemoveDefine(kCrestHdrpSymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        private static void ValidateCrestURPDefines()
        {
            string[] crestAssets = AssetDatabase.FindAssets(kCrestUrpAssetStr, null);
            if (crestAssets.Length != 0)
            {
#if !CREST_URP_PRESENT
                AddDefine(kCrestUrpSymbolStr, _buildTargetPlatforms);
#endif
            }
            else
            {
#if CREST_URP_PRESENT
                RemoveDefine(kCrestUrpSymbolStr, _buildTargetPlatforms);
#endif
            }
        }

        private static void ValidateKWSDefinesHDRP()
        {
            if (ValidateAssetDefines(kKWSAssembly, kHighDefinitionRenderPipelineStr))
            {
#if !KWS_HDRP_PRESENT
                AddDefine("KWS_HDRP_PRESENT", _buildTargetPlatforms);
#endif
            }
            else
            {
#if KWS_HDRP_PRESENT
                RemoveDefine("KWS_HDRP_PRESENT", _buildTargetPlatforms);
#endif
            }
        }

        private static void ValidateKWSDefinesURP()
        {
            if (ValidateAssetDefines(kKWSAssembly, kUniversalRenderPipelineStr))
            {
#if !KWS_URP_PRESENT
                AddDefine("KWS_URP_PRESENT", _buildTargetPlatforms);
#endif
            }
            else
            {
#if KWS_URP_PRESENT
                RemoveDefine("KWS_URP_PRESENT", _buildTargetPlatforms);
#endif
            }
        }

        /// <summary>
        /// Search for the asset assembly and its render pipelines
        /// </summary>
        /// <param name="assetAssembly">Assembly file of the asset</param>
        /// <param name="rpAssembly">The renderer pipeline present in the assembly</param>
        private static bool ValidateAssetDefines(string assetAssembly, string rpAssembly)
        {
            Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            Assembly foundAssembly = null;
            foreach (var assembly in playerAssemblies)
            {
                if (assembly.name.Equals(assetAssembly))
                {
                    foundAssembly = assembly;
                }
            }

            Assembly renderPipeline = null;
            if (foundAssembly != null)
            {
                foreach (var definition in foundAssembly.assemblyReferences)
                {
                    if (definition.name.Equals(rpAssembly))
                    {
                        renderPipeline = definition;
                    }
                }
            }
            return foundAssembly != null && renderPipeline != null;
        }
#endregion
    }
#endif //UNITY_EDITOR
        }