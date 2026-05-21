//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RealTimeWeather.Managers;
using static RealTimeWeather.Managers.RealTimeWeatherManager;
using RealTimeWeather.Data;
using RealTimeWeather.Simulation;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor.PackageManager.UI;
#endif

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    /// <summary>
    /// This class create a custom editor for RealTimeWeatherManager component.
    /// </summary>
    [CustomEditor(typeof(RealTimeWeatherManager))]
    public class RealTimeWeatherEditor : UnityEditor.Editor
    {
        #region Private Const Variables
        // Title constants
        private const string kTitleStr = "Real-Time Weather Manager";
        // Info
        private const string kInfoVersionStr = "Version";
        private const string kInfoCurrentVersion = "1.3.3";
        private const string _connectToWeatherSystem = "Connect to weather system";
        private const string _connectToWaterSystem = "Connect to water system";
        private const string _globalSettings = "Global Settings";
        // General settings constants
        private const string kDontDestroyStr = "Don't destroy on load";
        private const string kVisibleUIStr = "Show Debug Widget";
        // Simulation settings constants
        private const string kNewSimulationButtonStr = "New Simulation";
        private const string kSimulationProfileType = "t: ForecastData";
        private const string kSimulationsPath = "/Resources/Forecast/Simulations";
        private const string kProviderSimulationsPath = "/Resources/Forecast/ProviderSimulations";

        private const string kNotCompatibleStr = "Only the Universal and High Definition render pipelines are supported!";
        // Enviro constants
        private const string kEnviroStr = "Enviro";
        private const string kEnviroUrlStr = "https://assetstore.unity.com/packages/tools/particles-effects/enviro-sky-and-weather-33963";
        private const string kEnviroNotFoundStr = "Please add Enviro in your project!";
        private const string kEnviroDeactivatedStr = "Enviro simulation is deactivated!\nAfter activation, complete render pipeline setup in EnviroSkyManager!";
        private const string kEnviroDeactivateBtnStr = "Deactivate Enviro Simulation";
        private const string kEnviroActivateBtnStr = "Activate Enviro Simulation";
        private const string kEnviroActivatedStr = "Enviro simulation is activated!";
        // Tenkoku constants
        private const string kTenkokuStr = "Tenkoku";
        private const string kTenkokuUrlStr = "https://assetstore.unity.com/packages/tools/particles-effects/tenkoku-dynamic-sky-34435";
        private const string kTenkokuNotFoundStr = "Please add Tenkoku in your project!";
        private const string kTenkokuDeactivatedStr = "Tenkoku simulation is deactivated!";
        private const string kTenkokuDeactivateBtnStr = "Deactivate Tenkoku Simulation";
        private const string kTenkokuActivateBtnStr = "Activate Tenkoku Simulation";
        private const string kTenkokuActivatedStr = "Tenkoku simulation is activated!";
        private const string kTenkokuNotCompatibleStr = "Tenkoku is only compatible with SRP projects!";
        // Massive Clouds Atmos constants
        private const string kAtmosStr = "Atmos";
        private const string kAtmosUrlStr = "https://assetstore.unity.com/packages/tools/particles-effects/massive-clouds-atmos-volumetric-skybox-173160";
        private const string kAtmosNotFoundStr = "Please add Atmos in your project!";
        private const string kAtmosDeactivatedStr = "Atmos simulation is deactivated!";
        private const string kAtmosDeactivateBtnStr = "Deactivate Atmos Simulation";
        private const string kAtmosActivateBtnStr = "Activate Atmos Simulation";
        private const string kAtmosActivatedStr = "Atmos simulation is activated!";
        // Expanse constants
        private const string kExpanseStr = "Expanse";
        private const string kExpanseUrlStr = "https://assetstore.unity.com/packages/tools/particles-effects/expanse-volumetric-skies-clouds-and-atmospheres-in-hdrp-192456";
        private const string kExpanseNotFoundStr = "Please add Expanse in your project!";
        private const string kExpanseDeactivatedStr = "Expanse simulation is deactivated!";
        private const string kExpanseDeactivateBtnStr = "Deactivate Expanse Simulation";
        private const string kExpanseActivateBtnStr = "Activate Expanse Simulation";
        private const string kExpanseActivatedStr = "Expanse simulation is activated!";
        private const string kExpanseCompatibilityStr = "Expanse is only compatible with the High Definition Render Pipeline";
        // Crest Constants
        private const string kCrestStr = "Crest";
        private const string kCrestUrlStr = "https://assetstore.unity.com/publishers/41652";
        private const string kCrestNotFoundStr = "Please add Crest in your project!";
        private const string kCrestNotCompatibleStr = "Crest is only compatible with URP/HDRP projects!";
        private const string kCrestDeactivatedStr = "Crest simulation is deactivated!";
        private const string kCrestDeactivateBtnStr = "Deactivate Crest Simulation";
        private const string kCrestActivateBtnStr = "Activate Crest Simulation";
        private const string kCrestActivatedStr = "Crest simulation is activated!";
        private const string kCrestPipelineIncompatibleStr = "Your version of Crest is incompatible with the current Render Pipeline!";
        // KWS constants
        private const string kKWSStr = "KWS";
        private const string kKwsUrlStr = "https://assetstore.unity.com/packages/tools/particles-effects/kws-water-system-hdrp-rendering-205007";
        private const string kKWSDeactivatedStr = "KWS simulation is deactivated!";
        private const string kKWSDeactivateBtnStr = "Deactivate KWS Simulation";
        private const string kKWSActivateBtnStr = "Activate KWS Simulation";
        private const string kKWSActivatedStr = "KWS simulation is activated!";
        private const string kKWSNotFoundHDRPStr = "Please add KWS HDRP in your project!";
        private const string kKWSNotFoundURPStr = "Please add KWS URP in your project!";
        // EasySky constants
        private const string kEasySkyActivatedStr = "EasySky simulation is activated";
        private const string kEasySkyNotCompatibleStr = "EasySky is only compatible with HDRP projects on version 2022.3.3 or newer!";
        private const string kEasySkyNotFoundStr = "Please add EasySky in your project!";

        // Properties constants
        private const string kDontDestroyPropStr = "_dontDestroy";
        private const string kVisibleUIPropStr = "_visibleUI";
        private const string kTypeOfSimulationPropStr = "_typeOfSimulation";
        private const string kAutoUpdateRatePropStr = "_autoUpdateRate";
        private const string kIsAutoWeatherUpdateEnabledPropStr = "_isAutoWeatherUpdateEnabled";

        // Automatic weather update constants
        private const string kAutoWeatherUpdateStr = "Auto weather update";
        private const string kAutoUpdateRateStr = "Update frequency";
        private const string kAutoWeatherStr = "Weather Update";
        private const string kInfoAutoUpdateStr = "Weather data will be updated with the set frequency (in minutes).";
        private const string kInfoAutoUpdateTooltipStr = "Update frequency in minutes";

        private const int kHeaderSpace = 20;
        private const int kLogoBottomSpace = 60;
        private const int kLogoInfoPaddingSpace = 40;
        private const int kLogoYOffset = 25;
        private const int kLogoXOffset = 7;
        private const float kLogoRectWidth = 307.5f;
        private const float kLogoRectHeight = 112.5f;
        private const int kInfoMargin = 15;
        private const int kInfoSpace = 110;
        private const int kMenuTabsColumns = 2;
        private const int kToggleSpace = 175;
        private const int kToggleStyleIndex = 13;

        private const int kMinFrequencyValue = 1;
        private const int kMaxFrequencyValue = 120;
        private Vector2 scrollPos;
        #endregion

        #region Private Variables
        private RealTimeWeatherManager _scriptTarget;

        private Color _backgroundColor1;
        private Color _backgroundColor2;
        private Color _backgroundColor3;
        private Color _defaultColor;

        private Rect _logoRect;

        private Texture2D _logoTexture;
        private Texture2D _tabButtonTexture;
        private Texture2D _tabButtonActiveTexture;
        private Texture2D _userDataButtonTexture;
        public Vector2 scrollPosition = Vector2.zero;

        private string[] _simulationTypeButtons = { "My simulations", "Examples" };

        private string[] _weatherSystems = { "None", "Enviro", "Tenkoku", "Massive Clouds Atmos", "Expanse", "Easy Sky" };
        private string[] _waterSystems = { "None", "KWS", "Crest" };

        private ReorderableList list;
        private int currentIndex;

        private SimulationSOBuilder simulationSOBuilder = new SimulationSOBuilder();
        private bool _isWeatherSystemOpen = true;
        private bool _isWatherSystemOpen = true;
        private bool _isGlobalSettingOpen = true;
        private int _selectedSimulation = -1;
        public GUISkin customGUIskin;
        public Texture2D UserWater;
        public Texture2D UserWeather;
        public Texture2D ProviderWater;
        public Texture2D ProviderWeather;
        #endregion

        #region Private Properties
        private SerializedProperty DontDestroyProperty;
        private SerializedProperty VisibleUIProperty;
        private SerializedProperty TypeOfSimulationProperty;
        private SerializedProperty AutoWeatherDataUpdateProperty;
        private SerializedProperty AutoUpdateRateProperty;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            ShowWelcomeTab();
            _scriptTarget = (RealTimeWeatherManager)target;
            DontDestroyProperty = serializedObject.FindProperty(kDontDestroyPropStr);
            VisibleUIProperty = serializedObject.FindProperty(kVisibleUIPropStr);
            TypeOfSimulationProperty = serializedObject.FindProperty(kTypeOfSimulationPropStr);
            AutoWeatherDataUpdateProperty = serializedObject.FindProperty(kIsAutoWeatherUpdateEnabledPropStr);
            AutoUpdateRateProperty = serializedObject.FindProperty(kAutoUpdateRatePropStr);

            _scriptTarget.LastChosenRequestMode = WeatherRequestMode.None;
            _scriptTarget.LastChosenWaterRequestMode = WaterRequestMode.None;

            list = new ReorderableList(_scriptTarget.DataWeatherProviders, typeof(List<string>), true, false, false, false);
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), new GUIContent(_scriptTarget.DataWeatherProviders[index], _scriptTarget.DataWeatherProvidersAddress[_scriptTarget.RTWDataWeatherProvidersIndexes[index]]));
            };

            list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Weather Providers");

#if UNITY_2019_1_OR_NEWER
            list.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                var listTemp = _scriptTarget.RTWDataWeatherProvidersIndexes;
                var moveItem = listTemp[oldIndex];
                listTemp.RemoveAt(oldIndex);
                listTemp.Insert(newIndex, moveItem);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            };
#else
            list.onSelectCallback = (ReorderableList list) =>
            {
                currentIndex = list.index;
            };

            list.onReorderCallback = (ReorderableList list) =>
            {
                var listTemp = _scriptTarget.RTWDataWeatherProvidersIndexes;
                var moveItem = listTemp[currentIndex];
                listTemp.RemoveAt(currentIndex);
                listTemp.Insert(list.index, moveItem);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            };
#endif
            list.footerHeight = 0;
            _scriptTarget.selectedSimulation = 0;

            _scriptTarget.popupOpen = false;
            UpdateSimulations();
            _scriptTarget.UpdateSimulationList();

            if (RealTimeWeatherManager.instance.RelativePath == null)
            {
                RealTimeWeatherRelativePath.UpdatePath();
            }
        }

        public override void OnInspectorGUI()
        {
            SetEditorElementsStyle();
            // Title
            GUI.backgroundColor = _backgroundColor1;
            GUILayout.BeginVertical(string.Empty, InspectorUtils.OuterBoxStyle, GUILayout.MinWidth(500), GUILayout.MaxWidth(1000));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("", customGUIskin.customStyles[18]))
            {
                WelcomePopup.InitializeForm();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(kHeaderSpace);
            // Logo
            GUI.DrawTexture(new Rect(_logoRect.x + kLogoXOffset, _logoRect.y + kLogoYOffset, kLogoRectWidth, kLogoRectHeight), _logoTexture, ScaleMode.ScaleToFit, true);
            GUILayout.Space(kLogoBottomSpace);
            GUILayout.Space(kLogoInfoPaddingSpace);

            GUI.backgroundColor = _backgroundColor2;
            GUILayout.BeginVertical(string.Empty, InspectorUtils.OuterBoxStyle);
            GUILayout.BeginVertical(string.Empty, InspectorUtils.InnerBoxStyle);
            _isWeatherSystemOpen = GUILayout.Toggle(_isWeatherSystemOpen, _connectToWeatherSystem, InspectorUtils.HeaderStyle);

            SelectWeatherProvider();

            GUILayout.EndVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUI.backgroundColor = _backgroundColor2;
            GUILayout.BeginVertical(string.Empty, InspectorUtils.InnerBoxStyle);
            _isWatherSystemOpen = GUILayout.Toggle(_isWatherSystemOpen, _connectToWaterSystem, InspectorUtils.HeaderStyle);

            SelectWaterProvider();

            GUILayout.EndVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);

            OpenGlobalSettings();

            GUILayout.EndVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Simulations", InspectorUtils.BigLabelInfoStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(kNewSimulationButtonStr, InspectorUtils.ColoredButtonStyleFixWidth))
            {
                TypeOfSimulationProperty.intValue = 0;
                AddNewSimulation();
            }
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginVertical();

            var type = GUILayout.Toolbar(TypeOfSimulationProperty.intValue, _simulationTypeButtons, InspectorUtils.ButtonStyle);

            if (type != TypeOfSimulationProperty.intValue)
            {
                _scriptTarget.CurrentSimulationSelected = null;
                _scriptTarget.LoadedSimulation = null;
                _scriptTarget.selectedSimulation = -1;
                _selectedSimulation = -1;
                TypeOfSimulationProperty.intValue = type;
            }

            GUILayout.Space(InspectorUtils.BorderSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Name", InspectorUtils.SmallLabelInfoStyle, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Days", InspectorUtils.SmallLabelInfoStyle, GUILayout.Height(30));
            GUILayout.Space(21);
            GUILayout.Label("Speed", InspectorUtils.SmallLabelInfoStyle, GUILayout.Height(30));
            GUILayout.Space(30);
            GUILayout.Label("Type", InspectorUtils.SmallLabelInfoStyle, GUILayout.Height(30));
            GUILayout.Space(45);
            GUILayout.EndHorizontal();

            if (TypeOfSimulationProperty.intValue == 0)
            {
                FillSimulationScrolview();
            }
            else
            {
                FillDemoSimulationScrolview();
            }

            GUILayout.EndVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = _defaultColor;
            if (GUILayout.Button("Load", InspectorUtils.ButtonStyle))
            {
                _scriptTarget.LoadedSimulation = _scriptTarget.CurrentSimulationSelected;
                EditorUtility.SetDirty(_scriptTarget.gameObject);
                UpdateSimulations();
            }

            GUILayout.Space(InspectorUtils.MarginSpace);

            if (GUILayout.Button("Edit", InspectorUtils.ButtonStyle) && !EditorApplication.isPlaying)
            {
                EditSelectedSimulaton();
            }

            if (TypeOfSimulationProperty.intValue == 0)
            {
                GUILayout.Space(InspectorUtils.MarginSpace);

                if (GUILayout.Button("Duplicate", InspectorUtils.ButtonStyle) && _scriptTarget.CurrentSimulationSelected != null)
                {
                    simulationSOBuilder.DuplicateSimulationSO(kSimulationsPath + "/" + _scriptTarget.CurrentSimulationSelected.name);
                    UpdateSimulations();
                }

                GUILayout.Space(InspectorUtils.MarginSpace);

                if (GUILayout.Button("Delete", InspectorUtils.ButtonStyle) && !EditorApplication.isPlaying)
                {
                    RemoveSelectedSimulation();
                }
            }
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.MarginSpace);
            if (_scriptTarget.LoadedSimulation != null)
            {
                EditorGUILayout.LabelField("Loaded Simulation: " + _scriptTarget.LoadedSimulation.SimulationName);
            }
            else
            {
                EditorGUILayout.LabelField("Loaded Simulation: None");
            }
            EditorGUILayout.LabelField(kInfoVersionStr + " " + kInfoCurrentVersion, InspectorUtils.LabelInfoStyle);
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void SelectWeatherProvider()
        {
            if (_isWeatherSystemOpen)
            {
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUI.backgroundColor = _backgroundColor1;
                var curentsystem = (WeatherSystems)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.SelectedWeatherSystem, _weatherSystems);

                if (curentsystem != _scriptTarget.SelectedWeatherSystem)
                {
                    _scriptTarget.SelectedWeatherSystem = curentsystem;
                    _scriptTarget.DeactivateAllWeather();
                }

#if !EXPANSE_PRESENT && !ENVIRO_PRESENT && !ATMOS_PRESENT && !TENKOKU_PRESENT && !EASYSKY_PRESENT
                EditorGUILayout.LabelField("Please add a Weather Simulation Module", InspectorUtils.LabelWarningStyle);
#else


                switch (_scriptTarget.SelectedWeatherSystem)
                {
                    case WeatherSystems.Expanse:
#if !EXPANSE_PRESENT
#if !UNITY_PIPELINE_HDRP
                        EditorGUILayout.LabelField(kExpanseCompatibilityStr, InspectorUtils.LabelWarningStyle);
#else
                        EditorGUILayout.LabelField(kExpanseNotFoundStr, InspectorUtils.LabelWarningStyle);
#endif
#else
                        if (!_scriptTarget.IsExpanseEnabled)
                        {
                            _scriptTarget.ActivateExpanseSimulation();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(kExpanseActivatedStr, InspectorUtils.LabelGreenStyle);
                        }
#endif
                        break;
                    case WeatherSystems.Enviro:
#if !ENVIRO_PRESENT
                        EditorGUILayout.LabelField(kEnviroNotFoundStr, InspectorUtils.LabelWarningStyle);
#else
                        if (!_scriptTarget.IsEnviroEnabled)
                        {
                            _scriptTarget.ActivateEnviroSimulation();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(kEnviroActivatedStr, InspectorUtils.LabelGreenStyle);
                        }
#endif
                        break;
                    case WeatherSystems.Atmos:
#if !ATMOS_PRESENT
                        EditorGUILayout.LabelField(kAtmosNotFoundStr, InspectorUtils.LabelWarningStyle);
#else
                        if (!_scriptTarget.IsAtmosEnabled)
                        {
                            _scriptTarget.ActivateAtmosSimulation();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(kAtmosActivatedStr, InspectorUtils.LabelGreenStyle);
                        }
#endif
                        break;
                    case WeatherSystems.Tenkoku:
#if !TENKOKU_PRESENT
#if (UNITY_PIPELINE_HDRP || UNITY_PIPELINE_URP)
                        EditorGUILayout.LabelField(kTenkokuNotCompatibleStr, InspectorUtils.LabelWarningStyle);
#else
                        EditorGUILayout.LabelField(kTenkokuNotFoundStr, InspectorUtils.LabelWarningStyle);
#endif
#else
                        if (!_scriptTarget.IsTenkokuEnabled)
                        {
                            _scriptTarget.ActivateTenkokuSimulation();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(kTenkokuActivatedStr, InspectorUtils.LabelGreenStyle);
                        }
#endif
                        break;
                    case WeatherSystems.EasySky:
#if !EASYSKY_PRESENT
                        EditorGUILayout.LabelField(kEasySkyNotFoundStr, InspectorUtils.LabelWarningStyle);
#else
#if (!UNITY_PIPELINE_HDRP || !UNITY_2022_3_OR_NEWER)
                        EditorGUILayout.LabelField(kEasySkyNotCompatibleStr, InspectorUtils.LabelWarningStyle);
#else
                        if (!_scriptTarget.IsEasySkyEnabled)
                        {
                            _scriptTarget.ActivateEasySkySimulation();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(kEasySkyActivatedStr, InspectorUtils.LabelGreenStyle);
                        }
#endif
#endif
                        break;
                    default:
                        break;
                }
#endif
                GUILayout.Space(InspectorUtils.MarginSpace);
            }
        }

        private void SelectWaterProvider()
        {
            if (_isWatherSystemOpen)
            {
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUI.backgroundColor = _backgroundColor1;

                var curentWaterSystem = (WaterSystems)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.SelectedWaterSystem, _waterSystems);

                if (curentWaterSystem != _scriptTarget.SelectedWaterSystem)
                {
                    _scriptTarget.SelectedWaterSystem = curentWaterSystem;
                    _scriptTarget.DeactivateAllWater();
                }

#if !CREST_HDRP_PRESENT && !CREST_URP_PRESENT && !KWS_URP_PRESENT && !KWS_HDRP_PRESENT
                EditorGUILayout.LabelField("Please add a Water Simulation Module", InspectorUtils.LabelWarningStyle);
#else

                switch (_scriptTarget.SelectedWaterSystem)
                {
                    case WaterSystems.Crest:

#if !(CREST_HDRP_PRESENT && UNITY_PIPELINE_HDRP) && !(CREST_URP_PRESENT && UNITY_PIPELINE_URP) || !UNITY_2020_3_OR_NEWER
#if !UNITY_PIPELINE_HDRP && !UNITY_PIPELINE_URP
                        EditorGUILayout.LabelField(kNotCompatibleStr, InspectorUtils.LabelWarningStyle);
#else
#if !CREST_HDRP_PRESENT && !CREST_URP_PRESENT
            EditorGUILayout.LabelField(kCrestNotFoundStr, InspectorUtils.LabelWarningStyle);
#else
            if (IsCrestEnabledProperty.boolValue)
            {
                _scriptTarget.DeactivateCrestSimulation();
                IsCrestEnabledProperty.boolValue = false;
            }
#endif
#endif
#else
            if (!_scriptTarget.IsCrestEnabled)
            {
                _scriptTarget.ActivateCrestSimulation();
            }
            else
            {
                EditorGUILayout.LabelField(kCrestActivatedStr, InspectorUtils.LabelGreenStyle);
            }
#endif
                        break;
                    case WaterSystems.KWS:
#if !KWS_URP_PRESENT
#if !KWS_HDRP_PRESENT && UNITY_PIPELINE_HDRP
                        EditorGUILayout.LabelField(kKWSNotFoundHDRPStr, InspectorUtils.LabelWarningStyle);
#endif
#if !KWS_URP_PRESENT && UNITY_PIPELINE_URP
                        EditorGUILayout.LabelField(kKWSNotFoundURPStr, InspectorUtils.LabelWarningStyle);
#endif
#if !UNITY_PIPELINE_URP && !UNITY_PIPELINE_HDRP
                        EditorGUILayout.LabelField(kNotCompatibleStr, InspectorUtils.LabelWarningStyle);
#endif
#endif
#if KWS_URP_PRESENT && UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP && KWS_HDRP_PRESENT
            if (!_scriptTarget.isKWSEnabled)
            {
                _scriptTarget.ActivateKWSSimulation();
            }
            else
            {
                EditorGUILayout.LabelField(kKWSActivatedStr, InspectorUtils.LabelGreenStyle);
            }
#endif
                        break;
                    default:
                        break;
                }
#endif
                GUILayout.Space(InspectorUtils.MarginSpace);
            }
        }

        private void OpenGlobalSettings()
        {
            GUI.backgroundColor = _backgroundColor2;
            GUILayout.BeginVertical(string.Empty, InspectorUtils.InnerBoxStyle);
            _isGlobalSettingOpen = GUILayout.Toggle(_isGlobalSettingOpen, _globalSettings, InspectorUtils.HeaderStyle, GUILayout.Height(15));

            if (_isGlobalSettingOpen)
            {
                GUILayout.Space(InspectorUtils.BorderSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Space(kToggleSpace);
                VisibleUIProperty.boolValue = GUILayout.Toggle(VisibleUIProperty.boolValue, kVisibleUIStr, customGUIskin.customStyles[3]);
                GUILayout.EndHorizontal();

                GUILayout.Space(InspectorUtils.BorderSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Space(kToggleSpace);
                DontDestroyProperty.boolValue = GUILayout.Toggle(DontDestroyProperty.boolValue, kDontDestroyStr, customGUIskin.customStyles[11]);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.BorderSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Space(kToggleSpace);
                AutoWeatherDataUpdateProperty.boolValue = GUILayout.Toggle(AutoWeatherDataUpdateProperty.boolValue, new GUIContent(kAutoWeatherUpdateStr, kInfoAutoUpdateStr), customGUIskin.customStyles[12]);

                if (!AutoWeatherDataUpdateProperty.boolValue)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Settings", InspectorUtils.SmallerButtonStyle))
                    {
                        if (!_scriptTarget.popupOpen)
                        {
                            SettingsPopup.InitializeForm(_scriptTarget);
                            UpdateSimulations();
                        }
                    }
                }

                GUILayout.EndHorizontal();

                _scriptTarget.OnAutoWeatherStateChanged();
                if (AutoWeatherDataUpdateProperty.boolValue)
                {
                    GUILayout.Space(InspectorUtils.ElementsSpace);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                    GUILayout.Label(kAutoUpdateRateStr, customGUIskin.customStyles[kToggleStyleIndex]);
                    GUILayout.Space(67);
                    AutoUpdateRateProperty.intValue = ClampAutoUpdateFrequencyValue(
                        EditorGUILayout.IntField(new GUIContent(string.Empty, kInfoAutoUpdateTooltipStr),
                            AutoUpdateRateProperty.intValue, GUILayout.Width(50)));
                    GUILayout.Space(5);
                    GUILayout.Label("min", customGUIskin.customStyles[13]);

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Settings", InspectorUtils.SmallerButtonStyle))
                    {
                        if (!_scriptTarget.popupOpen)
                        {
                            SettingsPopup.InitializeForm(_scriptTarget);
                            UpdateSimulations();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(InspectorUtils.MarginSpace);
                }
                else
                {
                    GUILayout.Space(InspectorUtils.MarginSpace);
                }

                GUI.backgroundColor = _backgroundColor1;
            }
            GUI.backgroundColor = _backgroundColor2;

            GUILayout.EndVertical();
        }

        private void FillSimulationScrolview()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.MaxHeight(300));

            for (int i = 0; i < _scriptTarget.SimulationsDataProfiles.Count; i++)
            {
                bool x = i == _selectedSimulation;
                if (i == _selectedSimulation)
                {
                    GUI.color = _backgroundColor3;
                }
                else
                {
                    GUI.color = _defaultColor;
                }

                GUILayout.BeginHorizontal(string.Empty, InspectorUtils.InnerBoxStyle);
                GUILayout.BeginVertical();
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.BeginHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);
                bool v = GUILayout.Toggle(x, _scriptTarget.SimulationsDataProfiles[i].name, customGUIskin.customStyles[5], GUILayout.Height(20));
                GUILayout.FlexibleSpace();

                if (_scriptTarget.SimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                {
                    GUILayout.Label("1");
                }
                else
                {
                    GUILayout.Label(_scriptTarget.SimulationsDataProfiles[i].DaysOfSimulation.ToString());
                }

                GUILayout.Space(38);

                var speed = 1f;
                if (_scriptTarget.SimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                {
                    switch (_scriptTarget.SimulationsDataProfiles[i].WeatherRequestMode)
                    {
                        case WeatherRequestMode.OpenWeatherMapMode:
                            if (_scriptTarget.SimulationsDataProfiles[i].OpenWeatherSimulationData.IsForecastModeEnabled)
                            {
                                speed = 3600f / _scriptTarget.SimulationsDataProfiles[i].OpenWeatherSimulationData.SimulationSpeed;
                            }
                            break;
                        case WeatherRequestMode.TomorrowMode:
                            if (_scriptTarget.SimulationsDataProfiles[i].TomorrowSimulationData.IsForecastModeEnabled)
                            {
                                speed = 3600f / _scriptTarget.SimulationsDataProfiles[i].TomorrowSimulationData.SimulationSpeed;
                            }
                            break;
                    }
                }

                if (_scriptTarget.SimulationsDataProfiles[i].WeatherSimulationType == SimulationType.UserData)
                {
                    speed = 3600f / _scriptTarget.SimulationsDataProfiles[i].simulationSpeed;
                }

                if (!_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    speed = 1f;
                }

                GUILayout.Label(speed.ToString("0.0") + "x", GUILayout.Width(45));
                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-5);
                }

                if (!_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive ||
                    _scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(4);
                }

                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-5);
                }

                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(4);
                }


                GUILayout.Space(23);
                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive)
                {
                    GUILayout.Label(ProviderWater);
                }
                GUILayout.Space(-InspectorUtils.MarginSpace);

                if (_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    if (_scriptTarget.SimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                    {
                        GUILayout.Label(ProviderWeather);
                    }
                    else
                    {
                        GUILayout.Label(UserWeather);
                    }
                }

                if (!_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive ||
                    _scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(20);
                }

                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(5);
                }

                if (!_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(50);
                }

                if (_scriptTarget.SimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.SimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-3);
                }

                GUILayout.Space(InspectorUtils.BorderSpace);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (x != v)
                {
                    _selectedSimulation = v ? i : -1;
                    _scriptTarget.selectedSimulation = _selectedSimulation;

                    _scriptTarget.CurrentSimulationSelected = _selectedSimulation < 0 ? null : _scriptTarget.SimulationsDataProfiles[_scriptTarget.selectedSimulation];
                }
            }
            GUI.color = _defaultColor;
            GUILayout.EndScrollView();
        }

        private void FillDemoSimulationScrolview()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.MaxHeight(300));

            for (int i = 0; i < _scriptTarget.ProviderSimulationsDataProfiles.Count; i++)
            {
                bool x = i == _selectedSimulation;
                if (i == _selectedSimulation)
                {
                    GUI.color = _backgroundColor3;
                }
                else
                {
                    GUI.color = _defaultColor;
                }

                GUILayout.BeginHorizontal(string.Empty, InspectorUtils.InnerBoxStyle);
                GUILayout.BeginVertical();
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.BeginHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);
                bool v = GUILayout.Toggle(x, _scriptTarget.ProviderSimulationsDataProfiles[i].name, customGUIskin.customStyles[5], GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                if (_scriptTarget.ProviderSimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                {
                    GUILayout.Label("1");
                }
                else
                {
                    GUILayout.Label(_scriptTarget.ProviderSimulationsDataProfiles[i].DaysOfSimulation.ToString());
                }
                GUILayout.Space(40);

                var speed = 1f;
                if (_scriptTarget.ProviderSimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                {
                    switch (_scriptTarget.ProviderSimulationsDataProfiles[i].WeatherRequestMode)
                    {
                        case WeatherRequestMode.OpenWeatherMapMode:
                            if (_scriptTarget.ProviderSimulationsDataProfiles[i].OpenWeatherSimulationData.IsForecastModeEnabled)
                            {
                                speed = 3600f / _scriptTarget.ProviderSimulationsDataProfiles[i].OpenWeatherSimulationData.SimulationSpeed;
                            }
                            break;
                        case WeatherRequestMode.TomorrowMode:
                            if (_scriptTarget.ProviderSimulationsDataProfiles[i].TomorrowSimulationData.IsForecastModeEnabled)
                            {
                                speed = 3600f / _scriptTarget.ProviderSimulationsDataProfiles[i].TomorrowSimulationData.SimulationSpeed;
                            }
                            break;
                    }
                }

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].WeatherSimulationType == SimulationType.UserData)
                {
                    speed = 3600f / _scriptTarget.ProviderSimulationsDataProfiles[i].simulationSpeed;
                }

                if (!_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    speed = 1f;
                }

                GUILayout.Label(speed.ToString("0.0") + "x", GUILayout.Width(45));
                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-5);
                }

                if (!_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive ||
                    _scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(4);
                }

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-5);
                }

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(4);
                }

                GUILayout.Space(23);
                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive)
                {
                    GUILayout.Label(ProviderWater);
                }
                GUILayout.Space(-InspectorUtils.MarginSpace);

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    if (_scriptTarget.ProviderSimulationsDataProfiles[i].WeatherSimulationType == SimulationType.DataProviders)
                    {
                        GUILayout.Label(ProviderWeather);
                    }
                    else
                    {
                        GUILayout.Label(UserWeather);
                    }
                }

                if (!_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive ||
                    _scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(20);
                }

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(5);
                }

                if (!_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && !_scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(50);
                }

                if (_scriptTarget.ProviderSimulationsDataProfiles[i].IsWaterSimulationActive && _scriptTarget.ProviderSimulationsDataProfiles[i].IsWeatherSimulationActive)
                {
                    GUILayout.Space(-3);
                }

                GUILayout.Space(InspectorUtils.BorderSpace);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (x != v)
                {
                    _selectedSimulation = v ? i : -1;
                    _scriptTarget.selectedSimulation = _selectedSimulation;

                    _scriptTarget.CurrentSimulationSelected = _selectedSimulation < 0 ? null : _scriptTarget.ProviderSimulationsDataProfiles[_scriptTarget.selectedSimulation];
                }
            }
            GUI.color = _defaultColor;
            GUILayout.EndScrollView();
        }
        #endregion

        #region Private Methods
        private void ShowWelcomeTab()
        {
            if (PlayerPrefs.GetInt("WasWelcomeShowed") != 1)
            {
                WelcomePopup.InitializeForm();
                PlayerPrefs.SetInt("WasWelcomeShowed", 1);
            }
        }

        /// <summary>
        /// Defines custom styles for the editor elements.
        /// </summary>
        private void SetEditorElementsStyle()
        {
            _backgroundColor1 = new Color(0.50f, 0.50f, 0.50f, 1f);
            _backgroundColor2 = Color.white;
            _backgroundColor3 = new Color(0.5165f, 0.8666f, 0.7614f, 1);
            _defaultColor = GUI.color;

            if (_logoTexture == null)
            {
                _logoTexture = Resources.Load("textures/Logo Pro") as Texture2D;

                if (EditorGUIUtility.isProSkin == true)
                {
                    _logoTexture = Resources.Load("textures/Logo Pro") as Texture2D;
                }
            }

            if (_tabButtonTexture == null)
            {
                _tabButtonTexture = Resources.Load("textures/tab_button") as Texture2D;
            }

            if (_tabButtonActiveTexture == null)
            {
                _tabButtonActiveTexture = Resources.Load("textures/tab_button_active") as Texture2D;
            }

            if (_userDataButtonTexture == null)
            {
                _userDataButtonTexture = Resources.Load("textures/user_data_button") as Texture2D;
            }

            if (customGUIskin == null)
            {
                customGUIskin = Resources.Load("CustomGUI") as GUISkin;
            }

            if (UserWater == null)
            {
                UserWater = Resources.Load("textures/UserWater") as Texture2D;
            }

            if (UserWeather == null)
            {
                UserWeather = Resources.Load("textures/UserWeather") as Texture2D;
            }

            if (ProviderWater == null)
            {
                ProviderWater = Resources.Load("textures/ProviderWater") as Texture2D;
            }

            if (ProviderWeather == null)
            {
                ProviderWeather = Resources.Load("textures/ProviderWeather") as Texture2D;
            }

            if (_scriptTarget.WeatherPresets != null && _scriptTarget.WeatherPresets.DefaultPreset.forecastCustomPresets.Count > 0 && _scriptTarget.DefaultForecastData.name == null)
            {
                _scriptTarget.DefaultForecastData = new ForecastWeatherData(_scriptTarget.WeatherPresets.DefaultPreset.forecastCustomPresets[0]);
            }

            GUIContent buttonText = new GUIContent(string.Empty);
            GUIStyle buttonStyle = GUIStyle.none;
            _logoRect = GUILayoutUtility.GetRect(buttonText, buttonStyle);
        }

        private void UpdateSimulations()
        {
#if UNITY_EDITOR
            List<ForecastData> foundAssets = FindSimulationProfiles();

            if (foundAssets.Count != _scriptTarget.SimulationsDataProfiles.Count)
            {
                _scriptTarget.SimulationsDataProfiles = foundAssets;
                _scriptTarget.UpdateSimulationList();
            }

            List<ForecastData> foundProviderAssets = FindProviderSimulationProfiles();

            if (foundProviderAssets.Count != _scriptTarget.ProviderSimulationsDataProfiles.Count)
            {
                _scriptTarget.ProviderSimulationsDataProfiles = foundProviderAssets;
                _scriptTarget.UpdateSimulationList();
            }

            if (foundProviderAssets.Count == 0 && foundAssets.Count == 0)
            {
                InspectorUtils.SimulationCount = 0;
            }
#endif
        }

        /// <summary>
        /// Adds a new simulation profile instance, a button for editing purposes and a new Scriptable Object asset in the Data/Simulations folder
        /// </summary>
        private void AddNewSimulation()
        {
            if (_scriptTarget.popupOpen)
            {
                return;
            }

            var newSimulation = simulationSOBuilder.CreateNewSimulation();
            if (newSimulation != null)
            {
                _scriptTarget.SimulationsDataProfiles.Add(newSimulation);
                _scriptTarget.Simulations.Add(newSimulation.SimulationName);
                _scriptTarget.selectedSimulation = _scriptTarget.Simulations.Count - 1;
                _scriptTarget.CurrentSimulationSelected = _scriptTarget.SimulationsDataProfiles[_scriptTarget.selectedSimulation];
                EditSelectedSimulaton();
            }

            UpdateSimulations();
        }

        /// <summary>
        /// Removes the selected simulation profile instance, the scrollview button and the Scriptable Object asset of the selected simulation from the Data/Simulations folder
        /// </summary>
        private void RemoveSelectedSimulation()
        {
            if (_scriptTarget.popupOpen)
            {
                return;
            }

            if (TypeOfSimulationProperty.intValue == 0)
            {
                if (_scriptTarget.Simulations.Count == 0 || _scriptTarget.CurrentSimulationSelected == null)
                {
                    return;
                }
                _scriptTarget.Simulations.Remove(_scriptTarget.Simulations[_scriptTarget.selectedSimulation]);
                _scriptTarget.SimulationsDataProfiles.Remove(_scriptTarget.CurrentSimulationSelected);
                AssetDatabase.DeleteAsset($"{_scriptTarget.RelativePath + kSimulationsPath}/{_scriptTarget.CurrentSimulationSelected.SimulationName}.asset");
                _selectedSimulation = -1;
            }

            if (TypeOfSimulationProperty.intValue == 1)
            {
                if (_scriptTarget.ProvidersSimulations.Count == 0 || _scriptTarget.CurrentSimulationSelected == null)
                {
                    return;
                }
                _scriptTarget.ProvidersSimulations.Remove(_scriptTarget.ProvidersSimulations[_scriptTarget.selectedSimulation]);
                _scriptTarget.ProviderSimulationsDataProfiles.Remove(_scriptTarget.CurrentSimulationSelected);
                AssetDatabase.DeleteAsset($"{_scriptTarget.RelativePath + kProviderSimulationsPath}/{_scriptTarget.CurrentSimulationSelected.SimulationName}.asset");
                _selectedSimulation = -1;
            }

            UpdateSimulations();
        }

        /// <summary>
        /// Open a popup dialog used for modifying simulation data
        /// </summary>
        private void EditSelectedSimulaton()
        {
            if (!_scriptTarget.popupOpen && _scriptTarget.CurrentSimulationSelected != null)
            {
                Timelapse timelapse = new Timelapse();
                _scriptTarget.LoadTimelapse(timelapse);
                TimelapsePopup.InitializeForm(_scriptTarget, timelapse);
                UpdateSimulations();
            }
        }

        /// <summary>
        /// This function limits the update frequency value between 1 and 120 (minutes)
        /// </summary>
        private int ClampAutoUpdateFrequencyValue(int value)
        {
            if (value >= kMaxFrequencyValue) return kMaxFrequencyValue;
            if (value <= kMinFrequencyValue) return kMinFrequencyValue;
            return value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Search for simulation data profiles in project
        /// </summary>
        private List<ForecastData> FindSimulationProfiles()
        {
            if (!UnityEngine.Windows.Directory.Exists(_scriptTarget.RelativePath + kSimulationsPath))
            {
                return new List<ForecastData>();
            }
            string[] assets = AssetDatabase.FindAssets(kSimulationProfileType, new[] { _scriptTarget.RelativePath + kSimulationsPath });
            List<ForecastData> simulationProfiles = new List<ForecastData>();

            foreach (string asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                ForecastData profile = (ForecastData)AssetDatabase.LoadAssetAtPath(path, typeof(ForecastData));
                simulationProfiles.Add(profile);
            }

            return simulationProfiles;
        }

        private List<ForecastData> FindProviderSimulationProfiles()
        {
            if (!UnityEngine.Windows.Directory.Exists(_scriptTarget.RelativePath + kProviderSimulationsPath))
            {
                return new List<ForecastData>();
            }
            string[] assets = AssetDatabase.FindAssets(kSimulationProfileType, new[] { _scriptTarget.RelativePath + kProviderSimulationsPath });
            List<ForecastData> simulationProfiles = new List<ForecastData>();

            foreach (string asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                ForecastData profile = (ForecastData)AssetDatabase.LoadAssetAtPath(path, typeof(ForecastData));
                simulationProfiles.Add(profile);
            }

            return simulationProfiles;
        }
#endif

        #endregion
    }
}
#endif