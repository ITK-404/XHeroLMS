// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.IO;
using System.Collections.Generic;
using RealTimeWeather.Enums;
using RealTimeWeather.Managers;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
using RealTimeWeather.Simulation;
using RealTimeWeather.Data;
using static RealTimeWeather.Managers.RealTimeWeatherManager;
using RealTimeWeather.WeatherProvider.Stormglass;

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    /// <summary>
    /// A popup window for the user input data of the simulation.
    /// </summary>
    public class TimelapsePopup : EditorWindow
    {
        #region Private Constants
        //Popup window name
        private const string kWindowName = "Forecast Timelapse";

        //Labels
        private const string kLerpSpeedStr = "Interval Lerp Speed";
        private const string kWeatherDataLabelStr = "Weather Data: ";
        private const string kSimulationNameStr = "Simulation Name";
        private const string kSimSpeedStr = "Simulation Speed(1h)";
        private const string kCustomPresetStr = "Custom Preset";
        private const string kDayOrderHeaderStr = "Day Order";
        private const string kNewPresetNameStr = "New Preset";
        private const string kLongitudeStr = "Longitude";
        private const string kLatitudeStr = "Latitude";
        private const string kUsaLocationStr = "United States location";
        private const string kApiKeyStr = "API Key";
        private const string kGlobalLocationStr = "Global Location";
        private const string kCityStr = "City";
        private const string kExpanseCompatibilityStr = "Expanse is only compatible with the High Definition Render Pipeline";
        private const string kEnviroNotFoundStr = "Please add Enviro in your project!";
        private const string kAtmosNotFoundStr = "Please add Atmos in your project!";
        private const string kTenkokuNotFoundStr = "Please add Tenkoku in your project!";
        private const string kEasySkyNotFoundStr = "Please add Easy Sky in your project";
        private const string kEasySkyNotCompatibleStr = "Easy Sky is only compatible with HDRP";
        private const string kNotCompatibleStr = "Only the Universal and High Definition render pipelines are supported!";
        private const string kCrestNotFoundStr = "Please add Crest in your project!";
        private const string kKWSNotFoundHDRPStr = "Please add KWS HDRP in your project!";
        private const string kKWSNotFoundURPStr = "Please add KWS URP in your project!";
        private const string kTenkokuNotCompatibleStr = "Tenkoku is only compatible with SRP projects!";
        private const string kEnviroActivatedStr = "Enviro simulation is activated!";
        private const string kTenkokuActivatedStr = "Tenkoku simulation is activated!";
        private const string kAtmosActivatedStr = "Atmos simulation is activated!";
        private const string kExpanseActivatedStr = "Expanse simulation is activated!";
        private const string kEasySkyActivatedStr = "Easy Sky simulation is activated!";
        private const string kCrestActivatedStr = "Crest simulation is activated!";
        private const string kKWSActivatedStr = "KWS simulation is activated!";
        private const string kExpanseNotFoundStr = "Please add Expanse in your project!";

        //WeatherData labels
        private const string kPrecipitationStr = "Precipitation";
        private const string kTemperatureStr = "Temperature";
        private const string kVisibilityStr = "Visibility";
        private const string kDirectionStr = "Wind Direction";
        private const string kPressureStr = "Pressure";
        private const string kHumidityStr = "Humidity";
        private const string kDewpointStr = "Dewpoint";
        private const string kIndexUVStr = "IndexUV";
        private const string kSpeedStr = "Wind Speed";

        //Measuring units
        private const string kPrecipitationUnitStr = "mm";
        private const string kVisibilityUnitStr = "km";
        private const string kPressureUnitStr = "mbar";
        private const string kDirectionUnitStr = "°";
        private const string kSpeedUnitStr = "km/h";
        private const string kCelsiusUnitStr = "°C";
        private const string kHumidityUnitStr = "%";

        //Buttons
        private const string kAddNewDayButtonStr = "Add New Day";

        //Warnings
        private const string kNoIntervalSelectedStr = "No Interval Selected";
        private const string kInvalidCharacterInName = "Invalid Character In Name";
        private const string kMaxNameLenghtExceeded = "Max preset name length exceeded!";


        //Clamp values
        private const int kMinSimulationSpeed = 1;
        private const int kMinTemperature = -273;
        private const int kMinPrecipitation = 0;
        private const int kMinDewPoint = -273;
        private const int kMinVisibility = 0;
        private const int kMinWindSpeed = 0;
        private const int kMinPressure = 0;
        private const int kMinHumidity = 0;
        private const int kMinIndexUV = 1;

        private const int kMaxPrecipitation = 50000;
        private const int kMaxSimulationSpeed = 600;
        private const int kMaxVisibility = 10000;
        private const int kMaxTemperature = 100;
        private const int kMaxWindSpeed = 1000;
        private const int kMaxPressure = 1400;
        private const int kMaxHumidity = 100;
        private const int kMaxDewPoint = 100;
        private const int kMaxIndexUV = 11;

        private const int kMinutesInAHour = 60;
        private const int kSecondsInAHour = 3600;

        //Offsets
        private const int ScrollViewHeightOffset = 6;

        private const int kCustomToggleStyleIndex = 6;
        private const int kFieldWidth = 140;
        #endregion

        #region Private Variables
        //Dropdown bools
        private bool _showPresetData;
        private bool _showPresetEditor;
        private bool _showDefaultInterval;
        private bool _showWaterSettings;

        //Popup variables
        private string[] _popupErrors;
        private char[] _invalidChars;
        private int _focusedDay;
        private int _currentDay;
        private int _mouseButtonIndex;
        private Vector2 _dayScrollPosition;
        private Vector2 _presetScrollPosition;
        private Vector2 _dayOrderScrollPosition;
        private Vector2 _forecastFieldsScrollPosition;
        private Vector2 _minimumSize = new Vector2(1405, 490);
        private static DateTime _date;
        private Color _defaultColor;

        //Slider variables
        private Color _defaultSkinColour = new Color(0.64f, 0.64f, 0.64f, 1f);
        private Rect _backgroundRect = new Rect(100, 100, 100, 100);
        private Rect _clippingRect = new Rect(100, 100, 100, 100);
        private Color _proSkinColour = new Color(0.2f, 0.2f, 0.2f, 1f);
        private Rect _labelRect = new Rect(100, 100, 100, 100);
        private Vector2 _mousePos = Vector2.zero;
        private Vector2 _localPos = Vector2.zero;
        private float _textureScale = 1;
        private bool _hasReleasedSlider;
        private float _lastSnapped;
        private bool _isOverSlider;
        private ForecastWeatherData _defaultIntervalData = new ForecastWeatherData();

        //Preset variables
        private static int _selectedInterval;
        private static int _selectedDay;
        private static ReorderableList _dayOrder;
        private string[] _weatherSystems = { "None", "Enviro", "Tenkoku", "Massive Clouds Atmos", "Expanse", "Easy Sky" };
        private string[] _waterSystems = { "None", "KWS", "Crest" };
        private string[] _simulationTypes = { "Custom Simulation", "Live weather data stream" };
        private string[] _weatherDataRequestMode = new[] { "Assist Scraper", "Tomorrow.io", "OpenWeather" };
        private string[] _waterDataRequestMode = new[] { "Metocean", "Stormglass", "Tomorrow.io" };
        private string[] _timeUnits = new[] { "sec", "min", "h" };
        private GUISkin testGUIskin;
        private ReorderableList _list;
        private Vector2 _presetsScrollPos = Vector2.zero;
        private int _slectedPreset;
        private int _selectedTimeUnit;
        private static float _timeSpeed;
        private static List<string> _presetsFoldersNames = new List<string>();
        private int _selectedFolderIndex;
        private static WeatherPresets _weatherPresets;
        private Vector2 _weatherDataScrollPos = Vector2.zero;
        private WeatherSystems _currentsystem;
        #endregion

        #region Static Variables
        private static RealTimeWeatherManager _scriptTarget;
        private static Timelapse _targetTimelapse;
        public static Timelapse TargetTimelapse { get => _targetTimelapse; }
        #endregion

        #region Static Methods
        public static void InitializeForm(RealTimeWeatherManager scripTarget, Timelapse targetTimelapse)
        {
            _scriptTarget = scripTarget;
            _targetTimelapse = targetTimelapse;
            _scriptTarget.UpdateTimelapse += OnTimelapseUpdated;
            TimelapsePopup window = (TimelapsePopup)GetWindow(typeof(TimelapsePopup), false);
            _timeSpeed = _targetTimelapse.simulationSpeed;
            window.titleContent.text = kWindowName;
            _selectedInterval = 0;
            _presetsFoldersNames.Clear();
            _presetsFoldersNames.Add(_scriptTarget.WeatherPresets.DefaultPreset.folderName);
            foreach (var preset in _scriptTarget.WeatherPresets.CustomPresets)
            {
                _presetsFoldersNames.Add(preset.folderName);
            }
            _weatherPresets = _scriptTarget.WeatherPresets.DefaultPreset;
            _scriptTarget.RandomForecastPopupData = new RandomForecastPopupData(_scriptTarget);
        }

        private static void OnTimelapseUpdated(Timelapse timelapse, DateTime dateTime)
        {
            _targetTimelapse = timelapse;
            _selectedDay = 0;
            _selectedInterval = 0;
            _date = dateTime;
            InitializeDayOrderList();
        }
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            InitializeDayOrderList();

            if (testGUIskin == null)
            {
                testGUIskin = Resources.Load("CustomGUI") as GUISkin;
            }

            _selectedDay = 0;
            _defaultIntervalData = new ForecastWeatherData(_targetTimelapse.defaultIntervalData);
            _hasReleasedSlider = true;
            _scriptTarget.popupOpen = true;
            wantsMouseEnterLeaveWindow = true;
            _currentsystem = _scriptTarget.SelectedWeatherSystem;
            EditorApplication.playModeStateChanged += ClosePopup;

            minSize = _minimumSize;
            _dayScrollPosition = Vector2.zero;
            _defaultColor = GUI.backgroundColor;

            if (String.IsNullOrEmpty(_targetTimelapse.timelapseDate))
            {
                _targetTimelapse.timelapseDate += DateTime.Now.ToString("dd");
                _targetTimelapse.timelapseDate += "/";
                _targetTimelapse.timelapseDate += DateTime.Now.ToString("MM");
                _targetTimelapse.timelapseDate += "/";
                _targetTimelapse.timelapseDate += DateTime.Now.Year;
            }
            _invalidChars = Path.GetInvalidFileNameChars();
            _popupErrors = new string[4];

            _list = new ReorderableList(_scriptTarget.DataWeatherProviders, typeof(List<string>), true, false, false, false);
            _list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), new GUIContent(_scriptTarget.DataWeatherProviders[index], _scriptTarget.DataWeatherProvidersAddress[_scriptTarget.RTWDataWeatherProvidersIndexes[index]]));
            };

            _list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Weather Providers");

#if UNITY_2019_1_OR_NEWER
            _list.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
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
            _list.footerHeight = 0;
        }

        private void OnGUI()
        {
            ResetOnMouseUp();
            EditorGUILayout.BeginVertical(InspectorUtils.OuterBoxStyle);
            EditorGUILayout.BeginHorizontal();
            AddTimelapseFields();
            AddTimelapseDays();
            EditorGUILayout.EndHorizontal();
            AddBottomClipBox();
            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            _scriptTarget.popupOpen = false;
            _scriptTarget.UpdateTimelapse -= OnTimelapseUpdated;
            _scriptTarget.SaveTimelapse(new Timelapse(_targetTimelapse), _date);
        }
        #endregion

        #region Private Methods
        private void ClosePopup(PlayModeStateChange state)
        {
            if (!_scriptTarget.popupOpen) return;

            _scriptTarget.popupOpen = false;
            _scriptTarget.SaveTimelapse(new Timelapse(_targetTimelapse), _date);
            _scriptTarget.UpdateTimelapse -= OnTimelapseUpdated;
            _scriptTarget.RandomForecastPopupData = new RandomForecastPopupData(_scriptTarget);
            this.Close();
        }

        private static void InitializeDayOrderList()
        {
            _dayOrder = new ReorderableList(_targetTimelapse.timelapseDays, typeof(TimelapseDay), draggable: true, displayHeader: true, displayAddButton: false, displayRemoveButton: false);
            _dayOrder.elementHeight = InspectorUtils.FieldHeight;
            _dayOrder.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, kDayOrderHeaderStr);
            };
            _dayOrder.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.DrawRect(rect, InspectorUtils.timelapsePopupDayOrderColours[index % 2]);
                EditorGUI.LabelField(rect, _targetTimelapse.timelapseDays[index].dayName + " Day: " + (index + 1));
            };
            _dayOrder.onReorderCallback = (ReorderableList list) =>
            {
                if (_selectedInterval >= _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals.Count)
                {
                    _selectedInterval = _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals.Count - 1;
                }
            };
        }

        /// <summary>
        /// Adds all the global fields of the timelapse preset
        /// </summary>
        private void AddTimelapseFields()
        {
            EditorGUILayout.Space(InspectorUtils.MarginSpace);
            EditorGUILayout.BeginVertical(InspectorUtils.OuterBoxStyle, GUILayout.MinWidth(InspectorUtils.kGlobalFieldTabWidth));
            _forecastFieldsScrollPosition = EditorGUILayout.BeginScrollView(_forecastFieldsScrollPosition, GUILayout.MinWidth(InspectorUtils.kLeftTabWidth));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(InspectorUtils.MarginSpace);
            AddNameField();
            EditorGUILayout.Space(20);
            AddWeatherSystemSettings();
            EditorGUILayout.Space(InspectorUtils.InnerSpace);
            AddWaterSystemSettings();
            EditorGUILayout.Space(InspectorUtils.InnerSpace);
            EditorGUILayout.EndVertical();
            if (_scriptTarget.CurrentSimulationSelected.WeatherSimulationType == SimulationType.UserData)
            {
                _dayOrderScrollPosition = EditorGUILayout.BeginScrollView(_dayOrderScrollPosition, GUILayout.MinHeight(100), GUILayout.MaxHeight(500));
                GUI.backgroundColor = InspectorUtils.backgroundColorLightGray;
                _dayOrder.DoLayoutList();
                GUI.backgroundColor = _defaultColor;
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndScrollView();
            DrawPopupErrors();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(InspectorUtils.MarginSpace);
        }

        /// <summary>
        /// Adds the name field of the timelapse preset
        /// </summary>
        private void AddNameField()
        {
            GUILayout.Label(kSimulationNameStr);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _targetTimelapse.timelapseName = EditorGUILayout.TextField("", _targetTimelapse.timelapseName, InspectorUtils.InputLabelStyle, new GUILayoutOption[] { GUILayout.Height(25) });
            if (EditorGUI.EndChangeCheck())
            {
                _popupErrors[3] = null;
                if (_targetTimelapse.timelapseName.Length > Utilities.kMaxNameLength)
                {
                    _popupErrors[3] = "Simulation Name: " + kMaxNameLenghtExceeded;
                }
                if (_targetTimelapse.timelapseName.IndexOfAny(_invalidChars) != -1)
                {
                    _popupErrors[3] = "Simulation Name: " + kInvalidCharacterInName;
                }
            }
            if (GUILayout.Button("Save", InspectorUtils.SmallerColoredButtonStyle))
            {
                _scriptTarget.SaveTimelapse(new Timelapse(_targetTimelapse), _date);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adds the new day button of the timelapse preset
        /// </summary>
        private void AddNewDayButton()
        {
            float buttonXCoord = _clippingRect.x + _clippingRect.width - InspectorUtils.kNewDayButtonWidth;
            if (GUI.Button(new Rect(_clippingRect.x, _clippingRect.y, InspectorUtils.kNewDayButtonWidth, InspectorUtils.kNewDayButtonHeight), "Populate with Real Data"))
            {
                SaveForecastPopup.InitializeForm();
            }

            if (GUI.Button(new Rect(_clippingRect.x + InspectorUtils.kNewDayButtonWidth, _clippingRect.y, InspectorUtils.kNewDayButtonWidth, InspectorUtils.kNewDayButtonHeight), "Populate with Procedural Data"))
            {
                RandomForecastPopup.InitializeForm(_scriptTarget);
            }

            if (GUI.Button(new Rect(buttonXCoord, _clippingRect.y, InspectorUtils.kNewDayButtonWidth, InspectorUtils.kNewDayButtonHeight), kAddNewDayButtonStr))
            {
                _targetTimelapse.AddNewDefaultDay();
                _targetTimelapse.timelapseDays[_targetTimelapse.timelapseDays.Count - 1].delimitersPos = new bool[InspectorUtils.kLabelDivisions + 1];
            }
        }

        /// <summary>
        /// Adds the fields for the default interval data
        /// </summary>
        private void AddWeatherSystemSettings()
        {
            EditorGUILayout.BeginVertical(InspectorUtils.OuterBoxStyle);
            GUILayout.BeginHorizontal();
            _scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive, "", testGUIskin.customStyles[7]);
            GUILayout.Label("Weather Simulation", InspectorUtils.SmallTitleInfoStyle);
            _showDefaultInterval = GUILayout.Toggle(_showDefaultInterval, "", InspectorUtils.HeaderStyle2);
            GUILayout.EndHorizontal();
            if (_showDefaultInterval)
            {
                GUILayout.Label("Weather System", InspectorUtils.LabelInfoStyle);
                _scriptTarget.SelectedWeatherSystem = (WeatherSystems)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.SelectedWeatherSystem, _weatherSystems);

                if (_currentsystem != _scriptTarget.SelectedWeatherSystem)
                {
                    _scriptTarget.DeactivateAllWeather();
                }

#if !EXPANSE_PRESENT && !ENVIRO_PRESENT && !ATMOS_PRESENT && !TENKOKU_PRESENT && !EASYSKY_PRESENT
                EditorGUILayout.LabelField("No Weather System detected", InspectorUtils.LabelErrorStyle);
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

                GUILayout.Space(InspectorUtils.BorderSpace);
                GUILayout.Label("Simulation Type", InspectorUtils.LabelInfoStyle);
                _scriptTarget.CurrentSimulationSelected.WeatherSimulationType = (SimulationType)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.CurrentSimulationSelected.WeatherSimulationType, _simulationTypes);

                if (_scriptTarget.CurrentSimulationSelected.WeatherSimulationType == SimulationType.DataProviders)
                {
                    GUILayout.Space(InspectorUtils.BorderSpace);
                    GUILayout.Label("Weather Data Provider", InspectorUtils.LabelInfoStyle);
                    _scriptTarget.CurrentSimulationSelected.WeatherRequestMode = (WeatherRequestMode)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.CurrentSimulationSelected.WeatherRequestMode, _weatherDataRequestMode);

                    switch (_scriptTarget.CurrentSimulationSelected.WeatherRequestMode)
                    {
                        case WeatherRequestMode.TomorrowMode:
                            FillTomorrowInput();
                            break;
                        case WeatherRequestMode.OpenWeatherMapMode:
                            FillOpenWeatherInput();
                            break;
                        case WeatherRequestMode.RtwMode:
                            FillRtwInput();
                            break;
                        default: break;
                    }
                }

                if (_scriptTarget.CurrentSimulationSelected.WeatherSimulationType == SimulationType.UserData)
                {
                    FillUserDataInput();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void FillTomorrowInput()
        {
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Tomorrow.io Settings", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label(kApiKeyStr);
            _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.ApiKey = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.ApiKey, new GUILayoutOption[] { GUILayout.Width(380) });
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsForecastModeEnabled = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsForecastModeEnabled, "Forecast mode", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            if (_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsForecastModeEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(kLerpSpeedStr, GUILayout.MaxWidth(200));
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IntervalLerpSpeed = (IntervalLerpSpeed)EditorGUILayout.EnumPopup(string.Empty, _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IntervalLerpSpeed, GUILayout.MaxWidth(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _timeSpeed = EditorGUILayout.FloatField(kSimSpeedStr, _timeSpeed, GUILayout.MaxWidth(200));
                GUILayout.Space(1);
                _selectedTimeUnit = EditorGUILayout.Popup(string.Empty, _selectedTimeUnit, _timeUnits, GUILayout.MaxWidth(100));

                switch (_selectedTimeUnit)
                {
                    case 0:
                        _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.SimulationSpeed = (int)Mathf.Clamp(_timeSpeed, 5, kSecondsInAHour);
                        _timeSpeed = _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.SimulationSpeed;
                        break;
                    case 1:
                        _timeSpeed = Mathf.Clamp(_timeSpeed, kMinSimulationSpeed, kMinutesInAHour);
                        _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.SimulationSpeed = (int)(kMinutesInAHour * _timeSpeed);
                        break;
                    case 2:
                        _timeSpeed = Mathf.Clamp(_timeSpeed, 0.1f, kMinSimulationSpeed);
                        _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.SimulationSpeed = (int)(kSecondsInAHour * _timeSpeed);
                        break;
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.BorderSpace);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IncludeExtraPackage = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IncludeExtraPackage, "Include Extra Data Package", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Space(208);
            _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsUsingCoordonates = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsUsingCoordonates, "Use geographic coordonates", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            if (!_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.IsUsingCoordonates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(208);
                _scriptTarget.CurrentSimulationSelected.IsUsaLocation = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.IsUsaLocation, kUsaLocationStr, testGUIskin.customStyles[kCustomToggleStyleIndex]);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Space(2);
                GUILayout.Label(kCityStr);
                GUILayout.Space(32);
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.City = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.City, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.FlexibleSpace();
                GUILayout.Label(_scriptTarget.CurrentSimulationSelected.IsUsaLocation ? "State" : "Country");
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Country = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Country, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }
            else
            {
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.City = string.Empty;
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Country = string.Empty;
                GUILayout.BeginHorizontal();
                GUILayout.Space(2);
                GUILayout.Label(kLatitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
                GUILayout.FlexibleSpace();
                GUILayout.Label(kLongitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.TomorrowSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }
        }

        private void FillOpenWeatherInput()
        {
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Open Weather Settings", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label(kApiKeyStr);
            _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.ApiKey = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.ApiKey, new GUILayoutOption[] { GUILayout.Width(380) });
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsForecastModeEnabled = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsForecastModeEnabled, "Forecast mode", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            if (_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsForecastModeEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(kLerpSpeedStr, GUILayout.MaxWidth(200));
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IntervalLerpSpeed = (IntervalLerpSpeed)EditorGUILayout.EnumPopup(string.Empty, _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IntervalLerpSpeed, GUILayout.MaxWidth(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _timeSpeed = EditorGUILayout.FloatField(kSimSpeedStr, _timeSpeed, GUILayout.MaxWidth(200));
                GUILayout.Space(1);
                _selectedTimeUnit = EditorGUILayout.Popup(string.Empty, _selectedTimeUnit, _timeUnits, GUILayout.MaxWidth(100));

                switch (_selectedTimeUnit)
                {
                    case 0:
                        _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.SimulationSpeed = (int)Mathf.Clamp(_timeSpeed, kMinSimulationSpeed, kSecondsInAHour);
                        _timeSpeed = _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.SimulationSpeed;
                        break;
                    case 1:
                        _timeSpeed = Mathf.Clamp(_timeSpeed, kMinSimulationSpeed, kMinutesInAHour);
                        _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.SimulationSpeed = (int)(kMinutesInAHour * _timeSpeed);
                        break;
                    case 2:
                        _timeSpeed = Mathf.Clamp(_timeSpeed, 0.1f, kMinSimulationSpeed);
                        _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.SimulationSpeed = (int)(kSecondsInAHour * _timeSpeed);
                        break;
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Space(208);
            _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsUsingCoordonates = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsUsingCoordonates, "Use geographic coordonates", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            if (!_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.IsUsingCoordonates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(208);
                _scriptTarget.CurrentSimulationSelected.IsUsaLocation = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.IsUsaLocation, kUsaLocationStr, testGUIskin.customStyles[kCustomToggleStyleIndex]);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Space(2);
                GUILayout.Label(kCityStr);
                GUILayout.Space(32);
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CityName = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CityName, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.FlexibleSpace();
                GUILayout.Label(_scriptTarget.CurrentSimulationSelected.IsUsaLocation ? "State" : "Country");
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CountryCode = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CountryCode, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }
            else
            {
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CityName = string.Empty;
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.CountryCode = string.Empty;
                GUILayout.BeginHorizontal();
                GUILayout.Space(2);
                GUILayout.Label(kLatitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
                GUILayout.FlexibleSpace();
                GUILayout.Label(kLongitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.OpenWeatherSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }
        }

        private void FillRtwInput()
        {
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("RTW Settings", InspectorUtils.BoldLabelInfoStyle);

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Space(208);
            _scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUsingCoordonates = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUsingCoordonates, "Use geographic coordonates", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            if (!_scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUsingCoordonates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(208);
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUSALocation = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUSALocation, kUsaLocationStr, testGUIskin.customStyles[kCustomToggleStyleIndex]);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Label(kCityStr);
                GUILayout.Space(32);
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCity = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCity, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.FlexibleSpace();
                GUILayout.Label(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.IsUSALocation ? "State" : "Country");
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCountry = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCountry, new GUILayoutOption[] { GUILayout.Width(140) });
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }
            else
            {
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCity = string.Empty;
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.RequestedCountry = string.Empty;
                GUILayout.BeginHorizontal();
                GUILayout.Label(kLatitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
                _targetTimelapse.timelapseLatitude = _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Latitude;
                GUILayout.FlexibleSpace();
                GUILayout.Label(kLongitudeStr);
                GUILayout.Space(InspectorUtils.BorderSpace);
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
                _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.RtwSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
                _targetTimelapse.timelapseLongitude = _scriptTarget.CurrentSimulationSelected.RtwSimulationData.Longitude;
                GUILayout.Space(InspectorUtils.MarginSpace);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Website Scraping Priority", InspectorUtils.LabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            _list.DoLayoutList();
        }

        private void FillUserDataInput()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Simulation Settings", InspectorUtils.BoldLabelInfoStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(kLerpSpeedStr, GUILayout.MaxWidth(200));
            _targetTimelapse.lerpSpeed = (IntervalLerpSpeed)EditorGUILayout.EnumPopup(string.Empty, _targetTimelapse.lerpSpeed, GUILayout.MaxWidth(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);
            _timeSpeed = EditorGUILayout.FloatField(kSimSpeedStr, _timeSpeed, GUILayout.MaxWidth(200));
            GUILayout.Space(1);
            _selectedTimeUnit = EditorGUILayout.Popup(string.Empty, _selectedTimeUnit, _timeUnits, GUILayout.MaxWidth(100));

            switch (_selectedTimeUnit)
            {
                case 0:
                    _targetTimelapse.simulationSpeed = (int)Mathf.Clamp(_timeSpeed, kMinSimulationSpeed, kSecondsInAHour);
                    _timeSpeed = _targetTimelapse.simulationSpeed;
                    break;
                case 1:
                    _timeSpeed = Mathf.Clamp(_timeSpeed, kMinSimulationSpeed, kMinutesInAHour);
                    _targetTimelapse.simulationSpeed = (int)(kMinutesInAHour * _timeSpeed);
                    break;
                case 2:
                    _timeSpeed = Mathf.Clamp(_timeSpeed, 0.1f, kMinSimulationSpeed);
                    _targetTimelapse.simulationSpeed = (int)(kSecondsInAHour * _timeSpeed);
                    break;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _targetTimelapse.loopSim = GUILayout.Toggle(_targetTimelapse.loopSim, "Loop Simulation", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(kLatitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _targetTimelapse.timelapseLatitude = EditorGUILayout.FloatField(_targetTimelapse.timelapseLatitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _targetTimelapse.timelapseLatitude = Mathf.Clamp(_targetTimelapse.timelapseLatitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
            GUILayout.FlexibleSpace();
            GUILayout.Label(kLongitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _targetTimelapse.timelapseLongitude = EditorGUILayout.FloatField(_targetTimelapse.timelapseLongitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _targetTimelapse.timelapseLongitude = Mathf.Clamp(_targetTimelapse.timelapseLongitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void AddWaterSystemSettings()
        {
            EditorGUILayout.BeginVertical(InspectorUtils.OuterBoxStyle);

            GUILayout.BeginHorizontal();
            _scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive, "", testGUIskin.customStyles[7]);
            GUILayout.Label("Water Simulation", InspectorUtils.SmallTitleInfoStyle);
            _showWaterSettings = GUILayout.Toggle(_showWaterSettings, "", InspectorUtils.HeaderStyle2);
            GUILayout.EndHorizontal();

            if (_showWaterSettings)
            {
                GUILayout.Label("Water System", InspectorUtils.LabelInfoStyle);
                _scriptTarget.SelectedWaterSystem = (WaterSystems)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.SelectedWaterSystem, _waterSystems);
#if !CREST_HDRP_PRESENT && !CREST_URP_PRESENT && !KWS_URP_PRESENT && !KWS_HDRP_PRESENT
                EditorGUILayout.LabelField("No Water System detected", InspectorUtils.LabelErrorStyle);
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
                GUILayout.Space(InspectorUtils.BorderSpace);
                GUILayout.Label("Weather Data Provider", InspectorUtils.LabelInfoStyle);
                _scriptTarget.CurrentSimulationSelected.WaterRequestMode = (WaterRequestMode)EditorGUILayout.Popup(string.Empty, (int)_scriptTarget.CurrentSimulationSelected.WaterRequestMode, _waterDataRequestMode);

                switch (_scriptTarget.CurrentSimulationSelected.WaterRequestMode)
                {
                    case WaterRequestMode.None:
                        break;
                    case WaterRequestMode.StormglassMode:
                        FillStormglassInput();
                        break;
                    case WaterRequestMode.TomorrowMode:
                        FillTomorrowWaterInput();
                        break;
                    case WaterRequestMode.MetoceanMode:
                        FillMetoceanSettings();
                        break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void FillMetoceanSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label("Metocean Settings", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label(kApiKeyStr);
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.ApiKey = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.ApiKey, new GUILayoutOption[] { GUILayout.Width(380) });
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Data Retrived Interval");
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.IntervalInHours = EditorGUILayout.IntField(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.IntervalInHours, new GUILayoutOption[] { GUILayout.Width(50) });
            GUILayout.Label("h");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(208);
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.MoreIntervals = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.MoreIntervals, "More intervals", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            if (_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.MoreIntervals)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Number of intervals");
                GUILayout.Space(14);
                _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.NumberOfIntervals = EditorGUILayout.IntField(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.NumberOfIntervals, new GUILayoutOption[] { GUILayout.Width(50) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(kLatitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
            GUILayout.FlexibleSpace();
            GUILayout.Label(kLongitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.MetoceanWaterSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
        }

        private void FillTomorrowWaterInput()
        {
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Tomorrow.io Settings", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label(kApiKeyStr);
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.ApiKey = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.ApiKey, new GUILayoutOption[] { GUILayout.Width(380) });
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Space(208);
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.IncludeExtraPackage = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.IncludeExtraPackage, "Include Extra Data Package", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(kLatitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
            GUILayout.FlexibleSpace();
            GUILayout.Label(kLongitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.TomorrowWaterSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
        }

        private void FillStormglassInput()
        {
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Stormglass Settings", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label(kApiKeyStr);
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.ApiKey = EditorGUILayout.TextField("", _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.ApiKey, new GUILayoutOption[] { GUILayout.Width(380) });
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            EditorGUILayout.LabelField("Choose if the data will be saved to a file or not", InspectorUtils.LabelInfoStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToFile = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToFile, "Save Data To File", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();

            if (_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToFile)
            {
                _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName = EditorGUILayout.TextField("File Name", _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName);
                if (_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName.Length > 50)
                {
                    _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName = _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName.Remove(49, _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.FileName.Length - 50);
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToSO = GUILayout.Toggle(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToSO, "Save Data To Scriptable Object", testGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();

            if (_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.SaveDataToSO)
            {
                _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.StormglassData = (StormglassData)EditorGUILayout.ObjectField("Scriptable Object", _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.StormglassData, typeof(StormglassData), true);
            }

            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label(kGlobalLocationStr, InspectorUtils.BoldLabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(kLatitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Latitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Latitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Latitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
            GUILayout.FlexibleSpace();
            GUILayout.Label(kLongitudeStr);
            GUILayout.Space(InspectorUtils.BorderSpace);
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Longitude = EditorGUILayout.FloatField(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Longitude, new GUILayoutOption[] { GUILayout.Width(140) });
            _scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Longitude = Mathf.Clamp(_scriptTarget.CurrentSimulationSelected.StormglassSimulationData.Longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adds all the days of the timelapse preset
        /// </summary>
        private void AddTimelapseDays()
        {
            EditorGUILayout.BeginVertical();

            if (!_scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive && !_scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            _clippingRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kCustomSliderHeight);
            _dayScrollPosition = EditorGUILayout.BeginScrollView(_dayScrollPosition);
            if (_scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive)
            {
                if (_scriptTarget.CurrentSimulationSelected.WeatherSimulationType == SimulationType.UserData)
                {
                    for (int i = 0; i < _targetTimelapse.timelapseDays.Count; i++)
                    {
                        GUI.backgroundColor = InspectorUtils.timelapsePopupBoxColours[i % 2];
                        EditorGUILayout.BeginVertical();
                        GUI.backgroundColor = _defaultColor;
                        AddDaysButtons(i);
                        if (i < _targetTimelapse.timelapseDays.Count)
                        {
                            //Day tab
                            if (_targetTimelapse.timelapseDays[i].showData)
                            {
                                if (_targetTimelapse.timelapseDays[i].timelapseIntervals.Count != 0)
                                {
                                    CustomSlider(i);
                                }

                                if (_scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive)
                                {
                                    GUI.backgroundColor = InspectorUtils.timelapsePopupBoxColours[0];
                                    EditorGUILayout.BeginVertical();

                                    LiveWaterDataSlider();

                                    GUI.backgroundColor = _defaultColor;
                                    EditorGUILayout.EndVertical();

                                }
                            }
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
                else
                {
                    GUI.backgroundColor = InspectorUtils.timelapsePopupBoxColours[0];
                    EditorGUILayout.BeginVertical();
                    GUI.backgroundColor = _defaultColor;
                    ProviderSliderButtons();

                    _currentDay = 0;
                    _labelRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kSliderLabelHeight);
                    DrawLabelValues();
                    _backgroundRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kProviderSliderHeight);
                    _backgroundRect.x += InspectorUtils.MarginSpace;
                    _backgroundRect.width -= 10;

                    if (_targetTimelapse.timelapseDays[0].timelapseIntervals.Count != 0)
                    {
                        LiveWeatherDataSlider();
                    }

                    if (_scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive)
                    {
                        GUI.backgroundColor = Color.white;

                        LiveWaterDataSlider();

                        GUI.backgroundColor = _defaultColor;
                    }

                    EditorGUILayout.EndVertical();
                }
            }


            if (!_scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive && _scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive)
            {
                GUI.backgroundColor = InspectorUtils.timelapsePopupBoxColours[0];
                EditorGUILayout.BeginVertical();
                GUI.backgroundColor = _defaultColor;
                ProviderSliderButtons();
                //Day tab
                _currentDay = 0;
                _labelRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kSliderLabelHeight);
                DrawLabelValues();
                if (_scriptTarget.CurrentSimulationSelected.IsWaterSimulationActive)
                {
                    GUI.backgroundColor = InspectorUtils.timelapsePopupBoxColours[0];
                    EditorGUILayout.BeginVertical();
                    GUI.backgroundColor = _defaultColor;

                    LiveWaterDataSlider();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            if (_scriptTarget.CurrentSimulationSelected.WeatherSimulationType == SimulationType.UserData && _scriptTarget.CurrentSimulationSelected.IsWeatherSimulationActive)
            {
                Rect presetAndWeatherDataClipping = GUILayoutUtility.GetLastRect();
                Color presetAndWeatherDataColor = EditorGUIUtility.isProSkin ? _proSkinColour : _defaultSkinColour;
                EditorGUI.DrawRect(new Rect(presetAndWeatherDataClipping.x, presetAndWeatherDataClipping.y + presetAndWeatherDataClipping.height, presetAndWeatherDataClipping.width, 1000), presetAndWeatherDataColor);
                if (_targetTimelapse.timelapseDays[_selectedDay].showData)
                {
                    AddIntervalDetails();
                    GUILayout.Space(-5);
                    AddDayPresetList();
                    AddSimulationFields(_targetTimelapse.timelapseDays[_selectedDay]);
                }
                else
                {
                    EditorGUILayout.HelpBox(kNoIntervalSelectedStr, MessageType.Info);
                }
                EditorGUILayout.Space(10);
                AddTopClipBox();
                AddNewDayButton();
            }

            EditorGUILayout.EndVertical();
        }

        private void AddIntervalDetails()
        {
            GUILayout.BeginHorizontal(InspectorUtils.OuterBoxStyle, GUILayout.Height(20));
            GUILayout.Label("Day " + _selectedDay);
            GUILayout.Space(-5);
            GUILayout.Label(", interval " + _selectedInterval);
            var intervalStart = GetIntervalStart();
            GUILayout.Label("(" + intervalStart + ":00 - " + GetIntervalEnd(intervalStart) + ":00)");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private int GetIntervalStart()
        {
            var curentInterval = 0;
            for (var i = 0; i < _targetTimelapse.timelapseDays[_selectedDay].delimitersPos.Length; i++)
            {
                if (_targetTimelapse.timelapseDays[_selectedDay].delimitersPos[i])
                {
                    curentInterval++;
                }

                if (curentInterval == _selectedInterval)
                {
                    return i;
                }
            }

            return 0;
        }

        private int GetIntervalEnd(int start)
        {
            for (var i = start + 1; i < _targetTimelapse.timelapseDays[_selectedDay].delimitersPos.Length; i++)
            {
                bool interval = _targetTimelapse.timelapseDays[_selectedDay].delimitersPos[i];
                if (_targetTimelapse.timelapseDays[_selectedDay].delimitersPos[i])
                {
                    return i;
                }
            }

            return 24;
        }

        /// <summary>
        /// The delete and duplicate buttons for each day.
        /// </summary>
        /// <param name="index"></param>
        private void AddDaysButtons(int index)
        {
            EditorGUILayout.BeginHorizontal(InspectorUtils.InnerBoxStyle);
            _targetTimelapse.timelapseDays[index].showData = GUILayout.Toggle(_targetTimelapse.timelapseDays[index].showData, _scriptTarget.CurrentSimulationSelected.name + " | Day: " + (index), InspectorUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_scriptTarget.PopupTextures.duplicateButtonTexture, EditorStyles.toolbarButton))
            {
                _targetTimelapse.DuplicateDay(index);
            }
            if (GUILayout.Button(_scriptTarget.PopupTextures.deleteButtonTexture, EditorStyles.toolbarButton))
            {
                if (_targetTimelapse.timelapseDays.Count != 1)
                {
                    _targetTimelapse.RemoveDay(index);
                    if (_selectedDay >= _targetTimelapse.timelapseDays.Count)
                    {
                        _selectedDay = _targetTimelapse.timelapseDays.Count - 1;
                    }
                }
                else
                {
                    _targetTimelapse.timelapseDays[0] = new TimelapseDay(_targetTimelapse.defaultIntervalData);
                }
                _selectedInterval = 0;
            }
            GUI.backgroundColor = _defaultColor;
            EditorGUILayout.EndHorizontal();
        }

        private void ProviderSliderButtons()
        {
            EditorGUILayout.BeginHorizontal();
            _targetTimelapse.timelapseDays[0].showData = GUILayout.Toggle(_targetTimelapse.timelapseDays[0].showData, _targetTimelapse.timelapseDays[0].dayName + " Day: " + (0 + 1), InspectorUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adds the data fields for the selected interval.
        /// </summary>
        /// <param name="timelapseDay">The preset day data</param>
        private void AddSimulationFields(TimelapseDay timelapseDay)
        {
            if (_selectedInterval >= 0)
            {
                TimelapseInterval interval = timelapseDay.timelapseIntervals[_selectedInterval];
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(InspectorUtils.InnerBoxStyle, GUILayout.Height(140), GUILayout.MaxWidth(200));
                EditorGUILayout.LabelField("Profile Name");
                _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].presetName = EditorGUILayout.TextField(_targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].presetName);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.BorderSpace);
                if (GUILayout.Button("Set as default preset", GUILayout.Width(150)))
                {
                    _scriptTarget.DefaultForecastData = _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherData;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(InspectorUtils.InnerBoxStyle);
                EditorGUI.BeginChangeCheck();
                AddWeatherFields(interval.weatherData);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].intervalState != IntervalState.CustomInterval)
                    {
                        _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherDataProfileStr = kWeatherDataLabelStr + kCustomPresetStr;
                        _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherData = new ForecastWeatherData(_targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherData);
                        if (_targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].intervalState == IntervalState.DefaultInterval)
                        {
                            _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].intervalState = IntervalState.CustomInterval;
                            ForecastWeatherData oldWeatherData = new ForecastWeatherData(_defaultIntervalData);
                            for (int i = 0; i < _targetTimelapse.timelapseDays.Count; i++)
                            {
                                for (int j = 0; j < _targetTimelapse.timelapseDays[i].timelapseIntervals.Count; j++)
                                {
                                    if (_targetTimelapse.timelapseDays[i].timelapseIntervals[j].intervalState == IntervalState.DefaultInterval)
                                    {
                                        _targetTimelapse.timelapseDays[i].timelapseIntervals[j].weatherData = oldWeatherData;
                                    }
                                }
                            }
                            _targetTimelapse.defaultIntervalData = oldWeatherData;
                        }
                        _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].intervalState = IntervalState.CustomInterval;
                    }
                }
                EditorGUILayout.Space(InspectorUtils.InnerSpace);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Adds the fields for all the data types of the WeatherData class
        /// </summary>
        /// <param name="weatherData">The data that will be printed</param>
        private void AddWeatherFields(ForecastWeatherData weatherData)
        {
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.LabelField("Settings");
            GUILayout.Space(InspectorUtils.MarginSpace);

            _weatherDataScrollPos = EditorGUILayout.BeginScrollView(_weatherDataScrollPos, new GUILayoutOption[] { GUILayout.Width(1000), GUILayout.MinWidth(300), GUILayout.Height(100) });
            EditorGUILayout.BeginHorizontal();
            float angle = Utilities.Vector2ToDegree(weatherData.forecastWind.forecastDirection);
            angle = AddFloatFieldWithMeasuringUnit(angle, kDirectionStr, kDirectionUnitStr);
            weatherData.forecastWind.forecastDirection = Utilities.DegreeToVector2(angle);
            GUILayout.FlexibleSpace();
            weatherData.forecastWind.forecastSpeed = AddFloatFieldWithMeasuringUnit(weatherData.forecastWind.forecastSpeed, kSpeedStr, kSpeedUnitStr, kMinWindSpeed, kMaxWindSpeed);
            GUILayout.FlexibleSpace();

            weatherData.forecastDewpoint = AddFloatFieldWithMeasuringUnit(weatherData.forecastDewpoint, kDewpointStr, kCelsiusUnitStr, kMinDewPoint, kMaxDewPoint);
            GUILayout.FlexibleSpace();

            weatherData.forecastPressure = AddFloatFieldWithMeasuringUnit(weatherData.forecastPressure, kPressureStr, kPressureUnitStr, kMinPressure, kMaxPressure);
            GUILayout.FlexibleSpace();

            EditorGUILayout.Space(InspectorUtils.InnerSpace);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            weatherData.foreacastPrecipitation = AddFloatFieldWithMeasuringUnit(weatherData.foreacastPrecipitation, kPrecipitationStr, kPrecipitationUnitStr, kMinPrecipitation, kMaxPrecipitation);
            GUILayout.FlexibleSpace();

            weatherData.forecastVisibility = AddFloatFieldWithMeasuringUnit(weatherData.forecastVisibility, kVisibilityStr, kVisibilityUnitStr, kMinVisibility, kMaxVisibility);
            GUILayout.FlexibleSpace();

            weatherData.forecastTemperature = AddFloatFieldWithMeasuringUnit(weatherData.forecastTemperature, kTemperatureStr, kCelsiusUnitStr, kMinTemperature, kMaxTemperature);
            GUILayout.FlexibleSpace();

            weatherData.forecastHumidity = AddFloatFieldWithMeasuringUnit(weatherData.forecastHumidity, kHumidityStr, kHumidityUnitStr, kMinHumidity, kMaxHumidity);
            EditorGUILayout.Space(InspectorUtils.InnerSpace);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.BorderSpace);
            EditorGUILayout.BeginHorizontal();
            weatherData.forecastIndexUV = AddIntFieldWithMeasuringUnit((int)weatherData.forecastIndexUV, kIndexUVStr, "", kMinIndexUV, kMaxIndexUV);
            GUILayout.FlexibleSpace();
            GUILayout.Space(100);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = 0;
        }

        /// <summary>
        /// Adds a float field with a label, measuring unit and optionally clamp parameters
        /// </summary>
        /// <param name="value">The value that will pe updated</param>
        /// <param name="label">The label of the value</param>
        /// <param name="unit">The measuring unit of the value</param>
        /// <param name="clampMin">Minimum clamp value</param>
        /// <param name="clampMax">Maximum clamp value</param>
        /// <returns>The updated value</returns>
        private float AddFloatFieldWithMeasuringUnit(float value, string label, string unit, float clampMin = float.MinValue, float clampMax = float.MaxValue)
        {
            float val;
            EditorGUILayout.BeginHorizontal();
            val = EditorGUILayout.FloatField(label, value, GUILayout.Width(200));
            val = Mathf.Clamp(val, clampMin, clampMax);
            EditorGUILayout.LabelField(unit, GUILayout.MaxWidth(40));
            EditorGUILayout.EndHorizontal();
            return val;
        }

        /// <summary>
        /// Adds an int field with a label, measuring unit and optionally clamp parameters
        /// </summary>
        /// <param name="value">The value that will pe printed</param>
        /// <param name="label">The label of the value</param>
        /// <param name="unit">The measuring unit of the value</param>
        /// <param name="clampMin">Minimum clamp value</param>
        /// <param name="clampMax">Maximum clamp value</param>
        /// <returns>The updated value</returns>
        private int AddIntFieldWithMeasuringUnit(int value, string label, string unit, float clampMin = float.MinValue, float clampMax = float.MaxValue)
        {
            int val;
            EditorGUILayout.BeginHorizontal();
            val = EditorGUILayout.IntField(label, value, GUILayout.Width(200));
            val = (int)Mathf.Clamp(val, clampMin, clampMax);
            EditorGUILayout.LabelField(unit, GUILayout.MaxWidth(40));
            EditorGUILayout.EndHorizontal();
            return val;
        }

        /// <summary>
        /// Adds a clipping box at the top of the popup. Used because ScrollView doesn't clip DrawPreviewTexture
        /// </summary>
        private void AddTopClipBox()
        {
            Color clipBoxColour = EditorGUIUtility.isProSkin ? _proSkinColour : _defaultSkinColour;
            EditorGUI.DrawRect(new Rect(_clippingRect.x, _clippingRect.y, _clippingRect.width, _clippingRect.height + 3), clipBoxColour);
        }

        /// <summary>
        /// Adds a clipping box at the bottom of the popup. Used because ScrollView doesn't clip DrawPreviewTexture
        /// </summary>
        private void AddBottomClipBox()
        {
            Color clipBoxColour = EditorGUIUtility.isProSkin ? _proSkinColour : _defaultSkinColour;
            _clippingRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kBottomClippingRectHeight);
            EditorGUI.DrawRect(new Rect(_clippingRect.x - 3, _clippingRect.y - 10, _clippingRect.width + 6, _clippingRect.height + 20), clipBoxColour);
        }

        /// <summary>
        /// Adds the preset list for the day intervals
        /// </summary>
        private void AddDayPresetList()
        {
            EditorGUILayout.BeginVertical(InspectorUtils.InnerBoxStyle);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Weather Presets ", InspectorUtils.BoldLabelInfoStyle);

            GUILayout.Label("Folder:");
            var dropdownIndex = EditorGUILayout.Popup(string.Empty, _selectedFolderIndex, _presetsFoldersNames.ToArray(), GUILayout.Width(130));
            if (dropdownIndex != _selectedFolderIndex)
            {
                _selectedFolderIndex = dropdownIndex;
                _weatherPresets = _selectedFolderIndex == 0
                    ? _scriptTarget.WeatherPresets.DefaultPreset
                    : _scriptTarget.WeatherPresets.CustomPresets[_selectedFolderIndex - 1];
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            _presetsScrollPos = EditorGUILayout.BeginScrollView(_presetsScrollPos, GUILayout.Height(80));
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _weatherPresets.forecastCustomPresets.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                GUILayout.BeginHorizontal();
                GUILayout.Space(6);
                bool x = i == _slectedPreset;
                bool v = GUILayout.Toggle(x, "", testGUIskin.customStyles[15]);

                if (x != v)
                {
                    _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].intervalState = IntervalState.Other;
                    _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherData = _scriptTarget.WeatherPresets.DefaultPreset.GetWeatherDataFromCustomPresetsAtIndex(i);
                    _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherDataProfileStr = kWeatherDataLabelStr + _weatherPresets.forecastCustomPresets[i].name;
                    _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].presetName = _weatherPresets.forecastCustomPresets[i].name;
                    _targetTimelapse.timelapseDays[_selectedDay].timelapseIntervals[_selectedInterval].weatherData.color = _weatherPresets.forecastCustomPresets[i].color;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(-19);
                GUI.color = _weatherPresets.forecastCustomPresets[i].color;
                GUILayout.Label(_scriptTarget.PopupTextures.TimelapsePresetForeground);
                GUI.color = _defaultColor;
                GUILayout.Space(-63);
                GUILayout.BeginHorizontal();
                GUILayout.Space(18);
                GUILayout.Label(_weatherPresets.forecastCustomPresets[i].texture, new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(40) });
                GUILayout.EndHorizontal();
                GUILayout.Label(_weatherPresets.forecastCustomPresets[i].name, InspectorUtils.SmallBlackLabelInfoStyle, GUILayout.Height(20), GUILayout.Width(70));
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(InspectorUtils.MarginSpace);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Adds a custom made slider
        /// </summary>
        /// <param name="index">The index of the current day that is being drawn</param>
        private void CustomSlider(int index)
        {
            _currentDay = index;
            _labelRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kSliderLabelHeight);
            DrawLabelValues();
            _backgroundRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kCustomSliderHeight);
            _backgroundRect.x += InspectorUtils.MarginSpace;
            _backgroundRect.width -= 10;
            if (_backgroundRect.y != 0)
            {
                if (IsInsideBox(_scriptTarget.mouseRealTimePosition, _backgroundRect) && _hasReleasedSlider)
                {
                    _focusedDay = index;
                    if (Event.current.mousePosition.y >= _backgroundRect.y && Event.current.mousePosition.y <= _backgroundRect.y + _backgroundRect.height && Event.current.mousePosition != Vector2.zero
                        && Event.current.type == EventType.MouseDown && _mouseButtonIndex == 0)
                    {
                        if (_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count == 0)
                        {
                            _selectedDay = _focusedDay;
                            _selectedInterval = 0;
                        }
                    }
                }

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                EditorGUI.LabelField(_backgroundRect, "");
                GUI.color = _defaultColor;

                RunEvents();

                _scriptTarget.mousePos = _mousePos;
                _scriptTarget.mouseLocalPos = _localPos;

                DrawIntervals();
                DrawLabelDivisions();
                DrawFirstAndLastIntervalDelimiters();

                bool foundInterval = false;
                _isOverSlider = false;

                for (int i = 0; i < _targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count; i++)
                {
                    float currentSliderPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i], _backgroundRect.width);
                    if (Event.current.mousePosition.y >= _backgroundRect.y && Event.current.mousePosition.y <= _backgroundRect.y + _backgroundRect.height && Event.current.mousePosition != Vector2.zero)
                    {
                        if (Event.current.type == EventType.MouseDown && _mouseButtonIndex == 0)
                        {
                            _selectedDay = _focusedDay;
                            if (_localPos.x >= currentSliderPosX - InspectorUtils.kColliderThickness / 2 &&
                                _localPos.x <= currentSliderPosX + InspectorUtils.kColliderThickness / 2)
                            {
                                _targetTimelapse.timelapseDays[_currentDay].startedInside[i] = true;
                                if (_targetTimelapse.timelapseDays[_currentDay].selectedSlider == -1)
                                {
                                    _targetTimelapse.timelapseDays[_currentDay].selectedSlider = i;
                                    _selectedInterval = i;
                                    _targetTimelapse.timelapseDays[_currentDay].lastDelimiterPos = -1;
                                    _hasReleasedSlider = false;
                                }
                            }
                            else
                            {
                                if (!foundInterval && _targetTimelapse.timelapseDays[_currentDay].selectedSlider == -1)
                                {
                                    if (_localPos.x <= currentSliderPosX - InspectorUtils.kColliderThickness / 2)
                                    {
                                        _selectedInterval = i;
                                        foundInterval = true;
                                    }
                                    else
                                    {
                                        _selectedInterval = i + 1;
                                    }

                                }
                            }
                        }
                        if (_localPos.x >= currentSliderPosX - InspectorUtils.kColliderThickness / 2 && _localPos.x <= currentSliderPosX + InspectorUtils.kColliderThickness / 2)
                        {
                            _isOverSlider = true;
                        }
                    }
                    DelimiterCollisionsCheck(i);
                    DrawIntervalDelimiters(i);
                }
                if (Event.current.type == EventType.MouseDown && _mouseButtonIndex == 1 && IsInsideBox(_scriptTarget.mouseRealTimePosition, _backgroundRect))
                {
                    CustomSliderOptionsMenu(_isOverSlider);
                }
                Repaint();
            }
        }

        private void LiveWeatherDataSlider()
        {
            if (_backgroundRect.y != 0)
            {
                if (IsInsideBox(_scriptTarget.mouseRealTimePosition, _backgroundRect) && _hasReleasedSlider)
                {
                    _focusedDay = 0;
                    if (Event.current.mousePosition.y >= _backgroundRect.y && Event.current.mousePosition.y <= _backgroundRect.y + _backgroundRect.height && Event.current.mousePosition != Vector2.zero
                        && Event.current.type == EventType.MouseDown && _mouseButtonIndex == 0)
                    {
                        if (_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count == 0)
                        {
                            _selectedDay = _focusedDay;
                            _selectedInterval = 0;
                        }
                    }
                }

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                EditorGUI.LabelField(_backgroundRect, "");
                GUI.color = _defaultColor;

                _scriptTarget.mousePos = _mousePos;
                _scriptTarget.mouseLocalPos = _localPos;

                DrawWeatherProviderInterval();
            }
        }

        private void LiveWaterDataSlider()
        {
            _currentDay = 0;
            _backgroundRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kProviderSliderHeight);
            _backgroundRect.x += 5;
            _backgroundRect.width -= 10;

            if (_backgroundRect.y != 0)
            {
                if (IsInsideBox(_scriptTarget.mouseRealTimePosition, _backgroundRect) && _hasReleasedSlider)
                {
                    _focusedDay = 0;
                    if (Event.current.mousePosition.y >= _backgroundRect.y && Event.current.mousePosition.y <= _backgroundRect.y + _backgroundRect.height && Event.current.mousePosition != Vector2.zero
                        && Event.current.type == EventType.MouseDown && _mouseButtonIndex == 0)
                    {
                        if (_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count == 0)
                        {
                            _selectedDay = _focusedDay;
                            _selectedInterval = 0;
                        }
                    }
                }

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                EditorGUI.LabelField(_backgroundRect, "");
                GUI.color = _defaultColor;

                _scriptTarget.mousePos = _mousePos;
                _scriptTarget.mouseLocalPos = _localPos;

                DrawWaterProviderInterval();
            }
        }

        /// <summary>
        /// Draws the divisions in which the interval is split
        /// </summary>
        private void DrawLabelDivisions()
        {
            for (int i = 0; i <= InspectorUtils.kLabelDivisions; i++)
            {
                Vector2 sliderLabelPosition = new Vector3((_backgroundRect.x + i * _backgroundRect.width / InspectorUtils.kLabelDivisions) - InspectorUtils.kLabelLineThickness / 2, _backgroundRect.y);
                Vector2 sliderLabelSize = new Vector2(InspectorUtils.kLabelLineThickness, _backgroundRect.height);
                Rect sliderLabelRect = new Rect(sliderLabelPosition, sliderLabelSize);

                EditorGUI.DrawRect(sliderLabelRect, new Color(0.1f, 0.1f, 0.1f, 1f));
            }
        }

        /// <summary>
        /// Draws the values for each of the interval split
        /// </summary>
        private void DrawLabelValues()
        {
            _labelRect.x += InspectorUtils.MarginSpace;
            _labelRect.width -= 10;
            for (int i = 0; i <= InspectorUtils.kLabelDivisions; i++)
            {
                int charNum = i == 0 ? 1 : 0;
                int aux = i;
                while (aux != 0)
                {
                    aux /= 10;
                    charNum++;
                }

                Vector2 sliderLabelPosition = new Vector3(_labelRect.x + (i * _labelRect.width / InspectorUtils.kLabelDivisions) - (charNum * InspectorUtils.kCharacterWidth / 2), _labelRect.y);
                Vector2 sliderLabelSize = new Vector2(charNum * 10, _labelRect.height);
                Rect sliderLabelRect = new Rect(sliderLabelPosition, sliderLabelSize);

                EditorGUI.LabelField(sliderLabelRect, i.ToString(), EditorStyles.boldLabel);
            }
        }

        /// <summary>
        /// Draws all the hour intervals
        /// </summary>
        private void DrawIntervals()
        {

            WeatherState intervalWeatherState = WeatherState.Clear;
            Color intervalBackgroundColour = Color.white;
            Color intervalHighlightColour = Color.white;
            Vector2 textureOffset = Vector2.zero;

            if (_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count == 0)
            {
                intervalWeatherState = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.forecastWeatherState;
                intervalBackgroundColour = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.color;
                intervalHighlightColour = GetIntervalHighlightColour(intervalWeatherState);

                DrawIntervalBackground(_backgroundRect.x, _backgroundRect.width, intervalBackgroundColour);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(15, _backgroundRect.width, _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.texture);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * (_backgroundRect.width / _backgroundRect.height), _textureScale, 0, 0));
                DrawIntervalHighlight(_backgroundRect.x, _backgroundRect.width, 0, intervalHighlightColour);
            }
            else
            {
                float posCurent = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[0], _backgroundRect.width);
                float posNext;
                intervalWeatherState = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.forecastWeatherState;
                intervalBackgroundColour = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.color;
                intervalHighlightColour = GetIntervalHighlightColour(intervalWeatherState);

                DrawIntervalBackground(_backgroundRect.x, posCurent, intervalBackgroundColour);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(_backgroundRect.x + 5, posCurent, _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.texture);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * (FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[0], _backgroundRect.width) / _backgroundRect.height), _textureScale, 0, 0));
                DrawIntervalHighlight(_backgroundRect.x, posCurent, 0, intervalHighlightColour);

                for (int i = 0; i < _targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count - 1; i++)
                {
                    posCurent = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i], _backgroundRect.width);
                    posNext = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i + 1], _backgroundRect.width);
                    float intervalPosX = Mathf.Min(posCurent, posNext) + _backgroundRect.x;
                    float intervalWidth = Mathf.Abs(posCurent - posNext);
                    intervalWeatherState = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[i + 1].weatherData.forecastWeatherState;
                    intervalBackgroundColour = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[i + 1].weatherData.color;
                    intervalHighlightColour = GetIntervalHighlightColour(intervalWeatherState);

                    DrawIntervalBackground(intervalPosX, intervalWidth, intervalBackgroundColour);
                    textureOffset.x = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i], _backgroundRect.width) / _backgroundRect.height;
                    _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                    _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
                    DrawIntervalTexture(intervalPosX + 5, intervalWidth, _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[i + 1].weatherData.texture);
                    _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * ((FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i + 1], _backgroundRect.width) - FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[i], _backgroundRect.width)) / _backgroundRect.height), _textureScale, 0, 0));
                    DrawIntervalHighlight(intervalPosX, intervalWidth, i + 1, intervalHighlightColour);
                }

                posNext = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count - 1], _backgroundRect.width);
                intervalWeatherState = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1].weatherData.forecastWeatherState;
                intervalBackgroundColour = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1].weatherData.color;
                intervalHighlightColour = GetIntervalHighlightColour(intervalWeatherState);

                DrawIntervalBackground(posNext + _backgroundRect.x, _backgroundRect.width - posNext, intervalBackgroundColour);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
                textureOffset.x = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count - 1], _backgroundRect.width) / _backgroundRect.height;
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(posNext + _backgroundRect.x + 5, _backgroundRect.width - posNext, _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1].weatherData.texture);
                _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * (_backgroundRect.width - FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count - 1], _backgroundRect.width)) / _backgroundRect.height, _textureScale, 0, 0));
                DrawIntervalHighlight(posNext + _backgroundRect.x, _backgroundRect.width - posNext, _targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count, intervalHighlightColour);
            }
        }

        private void DrawWeatherProviderInterval()
        {
            DrawIntervalBackground(_backgroundRect.x + InspectorUtils.MarginSpace, _backgroundRect.width, _scriptTarget.PopupTextures.ProviderBackgroundColor);
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * (_backgroundRect.width / _backgroundRect.height), _textureScale, 0, 0));
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
            DrawProviderDefaultTexture(_backgroundRect.x + InspectorUtils.MarginSpace, _backgroundRect.width - 5, 0, _scriptTarget.PopupTextures.weatherProviderIntervalTexture);
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
            DrawProviderTexture(InspectorUtils.MarginSpace, true, _scriptTarget.PopupTextures.weatherProviderTexture);
        }

        private void DrawWaterProviderInterval()
        {
            DrawIntervalBackground(_backgroundRect.x + InspectorUtils.MarginSpace, _backgroundRect.width, _scriptTarget.PopupTextures.WaterProviderBackgroundColor);
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(_textureScale * (_backgroundRect.width / _backgroundRect.height), _textureScale, 0, 0));
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
            DrawProviderDefaultTexture(_backgroundRect.x + InspectorUtils.MarginSpace, _backgroundRect.width, 0, _scriptTarget.PopupTextures.waterProviderIntervalTexture);
            _scriptTarget.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, _textureScale, 0, 0));
            DrawProviderTexture(InspectorUtils.MarginSpace, false, _scriptTarget.PopupTextures.waterProviderTexture);
        }

        /// <summary>
        /// Draws the hour interval backround
        /// </summary>
        /// <param name="posX">Starting position of the hour interval</param>
        /// <param name="width">The width of the hour interval</param>
        /// <param name="intervalColour">The colour of the hour interval</param>
        private void DrawIntervalBackground(float posX, float width, Color intervalColour)
        {
            EditorGUI.DrawRect(new Rect(posX, _backgroundRect.y, width, _backgroundRect.height), intervalColour);
        }

        /// <summary>
        /// Draws the hour interval highlight if it is selected
        /// </summary>
        /// <param name="posX">Starting position of the hour interval</param>
        /// <param name="width">The width of the hour interval</param>
        /// <param name="interval">The index of the current hour interval</param>
        /// <param name="intervalColour">The colour of the hour interval higlight</param>
        private void DrawIntervalHighlight(float posX, float width, int interval, Color intervalColour)
        {
            if (_selectedInterval == interval && _selectedDay == _currentDay)
            {
                EditorGUI.DrawRect(new Rect(posX, _backgroundRect.y, width, _backgroundRect.height), intervalColour);
            }
        }

        /// <summary>
        /// Draws the hour interval texture overlay
        /// </summary>
        /// <param name="posX">Starting position of the hour interval</param>
        /// <param name="width">The width of the hour interval</param>
        /// <param name="texture">The texture of the hour interval</param>
        private void DrawIntervalTexture(float posX, float width, Texture2D texture)
        {
            if (_backgroundRect.y > _dayScrollPosition.y - _backgroundRect.height)
            {
                EditorGUI.DrawPreviewTexture(new Rect(posX, _backgroundRect.y + 13, 25, 25), texture, new Material(_scriptTarget.PopupTextures.intervalMaterial), ScaleMode.StretchToFill);
            }
        }

        private void DrawProviderTexture(float posX, bool isWeather, Texture2D texture)
        {
            if (_backgroundRect.y > _dayScrollPosition.y - _backgroundRect.height)
            {
                EditorGUI.DrawPreviewTexture(new Rect(posX, _backgroundRect.y, 200, 25), texture, new Material(_scriptTarget.PopupTextures.intervalMaterial), ScaleMode.StretchToFill);
                EditorGUI.LabelField(new Rect(posX + 25, _backgroundRect.y, 200, 25), "Live data from " + (isWeather ? _weatherDataRequestMode[(int)_scriptTarget.CurrentSimulationSelected.WeatherRequestMode] : _waterDataRequestMode[(int)_scriptTarget.CurrentSimulationSelected.WaterRequestMode]));
            }
        }

        private void DrawProviderDefaultTexture(float posX, float width, int index, Texture2D texture)
        {
            if (_backgroundRect.y > _dayScrollPosition.y - _backgroundRect.height)
            {
                EditorGUI.DrawPreviewTexture(new Rect(posX, _backgroundRect.y, width, _backgroundRect.height), texture, new Material(_scriptTarget.PopupTextures.intervalMaterial), ScaleMode.StretchToFill);
            }
        }

        /// <summary>
        /// Checks to see if the weather state represent a clear or cloudy day
        /// </summary>
        /// <param name="intervalWeatherState">The WeatherState of the current interval</param>
        /// <param name="index">The index of the current interval</param>
        /// <returns>A highlight colour based on the input WeatherState</returns>
        private Color GetIntervalHighlightColour(WeatherState intervalWeatherState)
        {
            Color intervalColour = Color.white;
            if (intervalWeatherState == WeatherState.Clear || intervalWeatherState == WeatherState.PartlyCloudy || intervalWeatherState == WeatherState.Sunny || intervalWeatherState == WeatherState.Fair)
            {
                intervalColour = _scriptTarget.PopupTextures.selectedIntervalSunnyHighlight;
            }
            else
            {
                intervalColour = _scriptTarget.PopupTextures.selectedIntervalCloudyHighlight;
            }
            return intervalColour;
        }

        /// <summary>
        /// Checks to see if the weather state represent a clear or cloudy day
        /// </summary>
        /// <param name="intervalWeatherState">The WeatherState of the current interval</param>
        /// <param name="index">The index of the current interval</param>
        /// <returns>A delimiter colour based on the input WeatherState</returns>
        private Color GetDelimiterColor(WeatherState intervalWeatherState)
        {
            Color intervalColour = Color.white;
            if (intervalWeatherState == WeatherState.Clear || intervalWeatherState == WeatherState.PartlyCloudy || intervalWeatherState == WeatherState.Sunny || intervalWeatherState == WeatherState.Fair)
            {
                intervalColour = _scriptTarget.PopupTextures.delimiterColSunny;
            }
            else
            {
                intervalColour = _scriptTarget.PopupTextures.delimiterColCloudy;
            }
            return intervalColour;
        }

        /// <summary>
        /// Draws the interval delimiters and checks if the cursor is inside the delimiter
        /// </summary>
        /// <param name="index">The index of the current delimiter</param>
        private void DrawIntervalDelimiters(int index)
        {
            float currentSliderX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index], _backgroundRect.width);
            Vector2 sliderPosition = new Vector3(currentSliderX - InspectorUtils.kLineThickness / 2 + _backgroundRect.x, _backgroundRect.y);
            Vector2 sliderSize = new Vector2(InspectorUtils.kLineThickness, _backgroundRect.height);
            Rect sliderRect = new Rect(sliderPosition, sliderSize);
            Color delimiterColour = GetDelimiterColor(_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[index].weatherData.forecastWeatherState);

            EditorGUI.DrawRect(sliderRect, delimiterColour);
            if (index == _targetTimelapse.timelapseDays[_currentDay].selectedSlider)
            {
                EditorGUI.DrawRect(sliderRect, _scriptTarget.PopupTextures.selectedIntervalCloudyHighlight);
            }


            if (IsInsideBox(_scriptTarget.mouseRealTimePosition, new Rect(new Vector3(currentSliderX - InspectorUtils.kColliderThickness / 2 + _backgroundRect.x, _backgroundRect.y), new Vector2(InspectorUtils.kColliderThickness, _backgroundRect.height))) || _targetTimelapse.timelapseDays[_currentDay].selectedSlider != -1)
            {
                EditorGUIUtility.AddCursorRect(new Rect(_scriptTarget.mouseRealTimePosition - new Vector2(6, 6), new Vector2(12, 12)), MouseCursor.ResizeHorizontal);
            }
        }

        /// <summary>
        /// Draws the first and last delimiters
        /// </summary>
        private void DrawFirstAndLastIntervalDelimiters()
        {
            Color delimiterColour = GetDelimiterColor(_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[0].weatherData.forecastWeatherState);
            EditorGUI.DrawRect(new Rect(_backgroundRect.x - InspectorUtils.kLineThickness / 2, _backgroundRect.y, InspectorUtils.kLineThickness, _backgroundRect.height), delimiterColour);
            delimiterColour = GetDelimiterColor(_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1].weatherData.forecastWeatherState);
            EditorGUI.DrawRect(new Rect(_backgroundRect.x + _backgroundRect.width - InspectorUtils.kLineThickness / 2, _backgroundRect.y, InspectorUtils.kLineThickness, _backgroundRect.height), delimiterColour);
        }

        /// <summary>
        /// Opens an option menu for the custom slider
        /// </summary>
        /// <param name="isOverDelimiter">If true the mouse cursor is above an interval delimiter</param>
        private void CustomSliderOptionsMenu(bool isOverDelimiter)
        {
            GenericMenu menu = new GenericMenu();
            int closestIndex = IndexOfClosest(_scriptTarget.mouseRealTimePosition);
            if (closestIndex == -1)
            {
                closestIndex = _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1;
            }

            if (!isOverDelimiter)
            {
                AddMenuItem(menu, "Split interval", 1);
                AddMenuItem(menu, "Add new interval", 2);

                if (_targetTimelapse.timelapseDays[_currentDay].timelapseIntervals[closestIndex].intervalState == IntervalState.DefaultInterval)
                {
                    AddDisabledMenuItem(menu, "Mark as default interval");
                }
                else
                {
                    AddMenuItem(menu, "Mark as default interval", 7);
                }
                menu.AddSeparator("");
                if (closestIndex != 0 && _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count > 1)
                {
                    AddMenuItem(menu, "Delete Interval/Keep Left", 5);
                }
                else
                {
                    AddDisabledMenuItem(menu, "Delete Interval/Keep Left");
                }

                if (closestIndex != _targetTimelapse.timelapseDays[_currentDay].timelapseIntervals.Count - 1 && _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count > 1)
                {
                    AddMenuItem(menu, "Delete Interval/Keep Right", 6);
                }
                else
                {
                    AddDisabledMenuItem(menu, "Delete Interval/Keep Right");
                }
            }
            else
            {
                AddDisabledMenuItem(menu, "Split interval");
                AddDisabledMenuItem(menu, "Add new interval");
                menu.AddSeparator("");
                AddMenuItem(menu, "Merge Intervals/Keep Left", 3);
                AddMenuItem(menu, "Merge Intervals/Keep Right", 4);
            }
            menu.ShowAsContext();
        }

        /// <summary>
        /// Adds an item for the options menu
        /// </summary>
        /// <param name="menu">The options menu</param>
        /// <param name="menuPath">The path of the option</param>
        /// <param name="index">An identifying index for the option</param>
        private void AddMenuItem(GenericMenu menu, string menuPath, int index)
        {
            menu.AddItem(new GUIContent(menuPath), false, OnMenuItemClick, index);
        }

        /// <summary>
        /// Adds a disabled item for the options menu
        /// </summary>
        /// <param name="menu">The options menu</param>
        /// <param name="menuPath">The path of the option</param>
        private void AddDisabledMenuItem(GenericMenu menu, string menuPath)
        {
            menu.AddDisabledItem(new GUIContent(menuPath), false);
        }

        /// <summary>
        /// Called when a menu option is clicked
        /// </summary>
        /// <param name="index">The option identifier</param>
        private void OnMenuItemClick(object index)
        {
            switch (index)
            {
                case 1:
                    AddNewDelimiterAtIndex(PointClosestToDelimiter(_scriptTarget.mouseRealTimePosition));
                    break;
                case 2:
                    int closestIndex = -1;
                    if (CanAddInterval())
                    {
                        int mousePosOnSlider = (int)(ToLocalPos(_scriptTarget.mouseRealTimePosition, _backgroundRect).x / (_backgroundRect.width / InspectorUtils.kLabelDivisions));
                        _scriptTarget.mouseRealTimePosition.x = mousePosOnSlider * _backgroundRect.width / InspectorUtils.kLabelDivisions;
                        AddNewDelimiterAtIndex(IndexOfClosest(_scriptTarget.mouseRealTimePosition));
                        _scriptTarget.mouseRealTimePosition.x += _backgroundRect.width / InspectorUtils.kLabelDivisions;
                        AddNewDelimiterAtIndex(IndexOfClosest(_scriptTarget.mouseRealTimePosition));
                        closestIndex = IndexOfClosest(_scriptTarget.mouseRealTimePosition);
                    }
                    else
                    {
                        AddNewDelimiterAtIndex(IndexOfClosest(_scriptTarget.mouseRealTimePosition));
                        closestIndex = IndexOfClosest(_scriptTarget.mouseRealTimePosition);
                    }

                    if (closestIndex >= 0)
                    {
                        _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[closestIndex] = new TimelapseInterval(_targetTimelapse.defaultIntervalData);
                    }
                    else if (closestIndex == -1)
                    {
                        _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 1] = new TimelapseInterval(_targetTimelapse.defaultIntervalData);
                    }
                    break;
                case 3:
                    MergeIntervals(_scriptTarget.mouseRealTimePosition, true);
                    break;
                case 4:
                    MergeIntervals(_scriptTarget.mouseRealTimePosition, false);
                    break;
                case 5:
                    DeleteSelectedInterval(_scriptTarget.mouseRealTimePosition, true);
                    break;
                case 6:
                    DeleteSelectedInterval(_scriptTarget.mouseRealTimePosition, false);
                    break;
                case 7:
                    ChangeIntervalStatus(_scriptTarget.mouseRealTimePosition);
                    break;
            }
            Repaint();
        }

        /// <summary>
        /// Merges two intervals
        /// </summary>
        /// <param name="point">A point on the slider</param>
        /// <param name="keepLeft">If true the left interval data will be kept after the merge</param>
        private void MergeIntervals(Vector2 point, bool keepLeft)
        {
            int index = -1;
            Vector2 mousePosition = ToLocalPos(point, _backgroundRect);
            TimelapseInterval savedIntervalData = new TimelapseInterval();

            for (int i = 0; i < _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count; i++)
            {
                float sliderPos = FromRelativePosition(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[i], _backgroundRect.width);
                if (mousePosition.x >= sliderPos - InspectorUtils.kColliderThickness / 2 && mousePosition.x <= sliderPos + InspectorUtils.kColliderThickness / 2)
                {
                    index = i;
                    break;
                }
            }

            if (keepLeft)
            {
                savedIntervalData = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index];
            }
            else
            {
                savedIntervalData = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index + 1];
            }

            if (index != -1)
            {
                RemoveDelimiterAtIndex(index, savedIntervalData);
            }
        }

        /// <summary>
        /// Deletes an interval
        /// </summary>
        /// <param name="point">A point on the slider</param>
        /// <param name="keepLeft">If true the left interval data will be kept after the merge</param>
        private void DeleteSelectedInterval(Vector2 point, bool keepLeft)
        {
            int index = IndexOfClosest(point);
            TimelapseInterval savedWeatherData = new TimelapseInterval();

            if (keepLeft)
            {
                if (index != -1)
                {
                    savedWeatherData = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index - 1];
                }
                else
                {
                    savedWeatherData = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 2];
                }
            }
            else
            {
                savedWeatherData = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index + 1];
            }

            if (index != -1 && !keepLeft)
            {
                RemoveDelimiterAtIndex(index, savedWeatherData);
            }

            if (index != 0 && keepLeft)
            {
                if (index != -1)
                {
                    RemoveDelimiterAtIndex(index - 1, savedWeatherData);
                }
                else
                {
                    RemoveDelimiterAtIndex(_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 2, savedWeatherData);
                }
            }
        }

        private void ChangeIntervalStatus(Vector2 point)
        {
            int index = IndexOfClosest(point);
            if (index == -1)
            {
                index = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 1;
            }
            _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index].weatherDataProfileStr = kWeatherDataLabelStr + "Default Interval";
            _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index].weatherData = _scriptTarget.DefaultForecastData;
            _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index].intervalState = IntervalState.DefaultInterval;
            _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index].presetName = _scriptTarget.DefaultForecastData.name;
        }

        /// <summary>
        /// Removes the delimiter at the given index
        /// </summary>
        /// <param name="index">The index of the delimiter</param>
        /// <param name="intervalDataData">The weather data that the new interval will have</param>
        private void RemoveDelimiterAtIndex(int index, TimelapseInterval intervalDataData)
        {
            _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[RelPosToPosValue(index, _focusedDay)] = false;
            _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.RemoveAt(index);
            _targetTimelapse.timelapseDays[_focusedDay].RemoveInterval(index);
            _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index] = new TimelapseInterval(intervalDataData);

            if (_selectedInterval >= _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count)
            {
                _selectedInterval = _targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 1;
            }
        }

        /// <summary>
        /// Finds the first valid index for the delimiter
        /// </summary>
        /// <param name="point">A point on the slider</param>
        /// <returns>A delimiter index</returns>
        private int IndexOfClosest(Vector2 point)
        {
            int index = -1;
            Vector2 mousePosition = ToLocalPos(point, _backgroundRect);
            for (int i = 0; i < _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count; i++)
            {
                float sliderPos = FromRelativePosition(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[i], _backgroundRect.width);
                if (mousePosition.x < sliderPos)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// Finds the index of the closest delimiter to the point
        /// </summary>
        /// <param name="point">A point on the slider</param>
        /// <returns>A delimiter index</returns>
        private int PointClosestToDelimiter(Vector2 point)
        {
            int index = IndexOfClosest(point);
            Vector2 mousePosition = ToLocalPos(point, _backgroundRect);

            if (index > 0 &&
               Mathf.Abs(mousePosition.x - FromRelativePosition(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[index], _backgroundRect.width)) >
               Mathf.Abs(mousePosition.x - FromRelativePosition(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[index - 1], _backgroundRect.width)))
            {
                return index - 1;
            }
            return index;
        }

        /// <summary>
        /// Adds a new delimiter
        /// </summary>
        /// <param name="index">Where the new delimiter will be added</param>
        /// <returns>-1 if it could not place a delimiter at the index otherwise it returns the index</returns>
        private int AddNewDelimiterAtIndex(int index)
        {
            float mousePosRelative = 0;
            if (index != -1)
            {
                mousePosRelative = _scriptTarget.mouseRealTimePosition.x - FromRelativePosition(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[index], _backgroundRect.width) > 0 ? 1 : -1;
            }

            float divisionWidth = Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions);
            float value = _scriptTarget.mouseRealTimePosition.x / divisionWidth;
            int roundedValue = Mathf.RoundToInt(value);
            float currentDividerPosX = 0;
            int currentPosition = Mathf.RoundToInt((divisionWidth * roundedValue) / _backgroundRect.width * InspectorUtils.kLabelDivisions);

            if (_targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition] &&
               (index > 0 && (_targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition - 1]) &&
                index < _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count - 1 && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition + 1] ||
               (index > 0 && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition - 1] && mousePosRelative < 0) ||
               (index < _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count - 1 && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition + 1] && mousePosRelative > 0)))
            {
                return -1;
            }

            currentDividerPosX = CheckForAdjacentSpaces(ref index, mousePosRelative, divisionWidth, currentPosition, roundedValue);

            if (index == -1)
            {
                AddDelimiterAtTheEnd(divisionWidth, roundedValue, currentPosition);
            }
            else
            {
                AddDelimiter(index, currentDividerPosX);
            }
            return index;
        }

        /// <summary>
        /// Checks to see if there are valid adjacent spaces
        /// </summary>
        /// <param name="index">The index where we want to place the delimiter</param>
        /// <param name="mousePosRelative">The mouse position relative to the slider</param>
        /// <param name="divisionWidth">The width of one slider division</param>
        /// <param name="currentPosition">The closest division to the mouse position</param>
        /// /// <param name="roundedValue">The rounded value of the closest available space</param>
        /// <returns>A valid position for the interval divider if it exists</returns>
        private float CheckForAdjacentSpaces(ref int index, float mousePosRelative, float divisionWidth, int currentPosition, int roundedValue)
        {
            float currentDividerPosX = 0;
            if (index != -1 && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition])
            {
                if (mousePosRelative > 0)
                {
                    //There is a valid space on the right
                    currentDividerPosX = divisionWidth * (roundedValue + 1);
                    _lastSnapped = currentDividerPosX;
                }
                else if (mousePosRelative <= 0)
                {
                    //There is a valid space on the left
                    currentDividerPosX = divisionWidth * (roundedValue - 1);
                    _lastSnapped = currentDividerPosX;
                }
            }
            else
            {
                //Current space is valid
                currentDividerPosX = divisionWidth * roundedValue;
                _lastSnapped = currentDividerPosX;
            }

            //Checks to see if the divider should be placed on the right
            if (mousePosRelative > 0 && index != -1)
            {
                index++;
            }
            return currentDividerPosX;
        }

        /// <summary>
        /// Adds an interval delimiter
        /// </summary>
        /// <param name="index">The delimiter index</param>
        /// <param name="currentDividerPosX">The delimiter position</param>
        private void AddDelimiter(int index, float currentDividerPosX)
        {
            if (Mathf.RoundToInt(currentDividerPosX / _backgroundRect.width * InspectorUtils.kLabelDivisions) != 0)
            {
                _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Insert(index, currentDividerPosX / _backgroundRect.width);
                _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[RelPosToPosValue(index, _focusedDay)] = true;
                TimelapseInterval interval = new TimelapseInterval(_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index]);
                if (_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[index].intervalState == IntervalState.DefaultInterval)
                {
                    interval.weatherData = _targetTimelapse.defaultIntervalData;
                    interval.intervalState = IntervalState.DefaultInterval;
                }
                _targetTimelapse.timelapseDays[_focusedDay].AddInterval(index, interval);
                _targetTimelapse.timelapseDays[_focusedDay].startedInside.Add(false);
            }
        }

        /// <summary>
        /// Adds an interval delimiter at the end of the list
        /// </summary>
        /// <param name="divisionWidth">The width of one slider division</param>
        /// <param name="currentPosition">The closest division to the mouse position</param>
        /// <param name="roundedValue">The rounded value of the closest available space</param>
        private void AddDelimiterAtTheEnd(float divisionWidth, int roundedValue, int currentPosition)
        {
            float relPosition = -1;
            if (_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count > 0 && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition] &&
                Mathf.RoundToInt((divisionWidth * (roundedValue + 1)) / _backgroundRect.width * InspectorUtils.kLabelDivisions) != 24)
            {
                //There is a valid space on the right
                relPosition = (divisionWidth * (roundedValue + 1)) / _backgroundRect.width;
            }
            else if (!_targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition] && Mathf.RoundToInt((divisionWidth * roundedValue) / _backgroundRect.width * InspectorUtils.kLabelDivisions) != 24 &&
                     Mathf.RoundToInt((divisionWidth * roundedValue) / _backgroundRect.width * InspectorUtils.kLabelDivisions) != 0)
            {
                //Current space is valid
                relPosition = (divisionWidth * roundedValue) / _backgroundRect.width;
            }

            if (relPosition != -1)
            {
                _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Add(relPosition);
                _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[RelPosToPosValue(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count - 1, _focusedDay)] = true;
                TimelapseInterval interval = new TimelapseInterval(_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 1]);
                if (_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals[_targetTimelapse.timelapseDays[_focusedDay].timelapseIntervals.Count - 1].intervalState == IntervalState.DefaultInterval)
                {
                    interval.weatherData = _targetTimelapse.defaultIntervalData;
                    interval.intervalState = IntervalState.DefaultInterval;
                }
                _targetTimelapse.timelapseDays[_focusedDay].AddInterval(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count - 1, interval);
                _targetTimelapse.timelapseDays[_focusedDay].startedInside.Add(false);
            }
        }

        /// <summary>
        /// Checks for collision between Delimiters
        /// </summary>
        /// <param name="index">The index of the current delimiter</param>
        private void DelimiterCollisionsCheck(int index)
        {
            float currentDividerPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index], _backgroundRect.width);
            if (index == _targetTimelapse.timelapseDays[_currentDay].selectedSlider && _targetTimelapse.timelapseDays[_currentDay].startedInside[index])
            {
                currentDividerPosX = _localPos.x;

                float nextSliderPosX;
                float lastSliderPosX;

                if (index > 0 && index < _targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count - 1)
                {
                    nextSliderPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index + 1], _backgroundRect.width);
                    lastSliderPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index - 1], _backgroundRect.width);
                    if (currentDividerPosX > nextSliderPosX - InspectorUtils.kColliderThickness)
                    {
                        currentDividerPosX = nextSliderPosX - InspectorUtils.kColliderThickness;
                    }
                    if (currentDividerPosX < lastSliderPosX + InspectorUtils.kColliderThickness)
                    {
                        currentDividerPosX = lastSliderPosX + InspectorUtils.kColliderThickness;
                    }
                }
                else
                {
                    if (_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX.Count != 1)
                    {
                        if (index == 0)
                        {
                            nextSliderPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index + 1], _backgroundRect.width);
                            if (currentDividerPosX > nextSliderPosX - InspectorUtils.kColliderThickness)
                            {
                                currentDividerPosX = nextSliderPosX - InspectorUtils.kColliderThickness;
                            }
                        }
                        else
                        {
                            lastSliderPosX = FromRelativePosition(_targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index - 1], _backgroundRect.width);
                            if (currentDividerPosX < lastSliderPosX + InspectorUtils.kColliderThickness)
                            {
                                currentDividerPosX = lastSliderPosX + InspectorUtils.kColliderThickness;
                            }
                        }
                    }
                    StartAndEndCollisions(ref currentDividerPosX);
                }

                SnapToDivisions(ref currentDividerPosX);
                MinIntervalCheck(ref currentDividerPosX, index);
                _targetTimelapse.timelapseDays[_currentDay].sliderRelPosX[index] = currentDividerPosX / _backgroundRect.width;

                if (_targetTimelapse.timelapseDays[_currentDay].lastDelimiterPos != -1)
                {
                    _targetTimelapse.timelapseDays[_currentDay].delimitersPos[_targetTimelapse.timelapseDays[_currentDay].lastDelimiterPos] = false;
                }
                _targetTimelapse.timelapseDays[_currentDay].delimitersPos[RelPosToPosValue(index, _currentDay)] = true;
                _targetTimelapse.timelapseDays[_currentDay].lastDelimiterPos = RelPosToPosValue(index, _currentDay);
            }
        }

        /// <summary>
        /// Check for collisions with the first and last Delimiter
        /// </summary>
        /// <param name="currentDividerPosX">The position of the current delimiter</param>
        private void StartAndEndCollisions(ref float currentDividerPosX)
        {
            float lastSliderPosX = FromRelativePosition(0.01f, _backgroundRect.width);
            if (currentDividerPosX <= lastSliderPosX + InspectorUtils.kColliderThickness)
            {
                currentDividerPosX = _backgroundRect.width / InspectorUtils.kLabelDivisions;
            }
            float nextSliderPosX = FromRelativePosition(0.99f, _backgroundRect.width);
            if (currentDividerPosX > nextSliderPosX - InspectorUtils.kColliderThickness)
            {
                currentDividerPosX = _backgroundRect.width / InspectorUtils.kLabelDivisions * (InspectorUtils.kLabelDivisions - 1);
            }
        }

        /// <summary>
        /// Snaps the delimiter to the interval divisions
        /// </summary>
        /// <param name="currentSliderPosX">The position of the current delimiter</param>
        private void SnapToDivisions(ref float currentSliderPosX)
        {
            float value = currentSliderPosX / Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions);
            if (value - Mathf.Round(value) > -(float)InspectorUtils.kSliderSnapThreshold / 100 && value - Mathf.Round(value) < (float)InspectorUtils.kSliderSnapThreshold / 100)
            {
                currentSliderPosX = Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions) * Mathf.Round(value);
                if (_lastSnapped != currentSliderPosX)
                {
                    _lastSnapped = currentSliderPosX;
                }
            }
            else
            {
                currentSliderPosX = _lastSnapped;
            }
        }

        /// <summary>
        /// Compensates for the floating point error
        /// </summary>
        /// <param name="currentSliderPosX">The position of the current delimiter</param>
        /// <param name="index">The index of the current delimiter</param>
        private void MinIntervalCheck(ref float currentSliderPosX, int index)
        {
            float value = currentSliderPosX / Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions);
            if (index != _targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX.Count - 1 && (int)(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[index + 1] * 1000) == (int)(currentSliderPosX / _backgroundRect.width * 1000))
            {
                currentSliderPosX = Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions) * (Mathf.Round(value) - 1);
            }
            else if (index != 0 && (int)(_targetTimelapse.timelapseDays[_focusedDay].sliderRelPosX[index - 1] * 1000) == (int)(currentSliderPosX / _backgroundRect.width * 1000))
            {
                currentSliderPosX = Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions) * (Mathf.Round(value) + 1);
            }
        }

        /// <summary>
        /// Clamps the mouse position to the current slider
        /// </summary>
        /// <param name="pos">Global mouse position</param>
        /// <param name="marginOffsetX">Margin offset for the X axis</param>
        /// <param name="marginOffsetY">Margin offset for the Y axis</param>
        private void GetPosition(Vector2 pos, float marginOffsetX = 0, float marginOffsetY = 0)
        {
            if (pos.y > _backgroundRect.y + marginOffsetY && pos.y < _backgroundRect.y + _backgroundRect.height - marginOffsetY)
            {
                _mousePos.y = pos.y;
            }
            else
            {
                if (pos.y <= _backgroundRect.y + marginOffsetY)
                {
                    _mousePos.y = _backgroundRect.y + marginOffsetY;
                }
                if (pos.y >= _backgroundRect.y + _backgroundRect.height - marginOffsetY)
                {
                    _mousePos.y = _backgroundRect.y + _backgroundRect.height - marginOffsetY;
                }
            }
            if (pos.x > _backgroundRect.x + marginOffsetX && pos.x < _backgroundRect.x + _backgroundRect.width - marginOffsetX)
            {
                _mousePos.x = pos.x;
            }
            else
            {
                if (pos.x <= _backgroundRect.x + marginOffsetX)
                {
                    _mousePos.x = _backgroundRect.x + marginOffsetX;
                }
                if (pos.x >= _backgroundRect.x + _backgroundRect.width - marginOffsetX)
                {
                    _mousePos.x = _backgroundRect.x + _backgroundRect.width - marginOffsetX;
                }
            }
            Repaint();
        }

        /// <summary>
        /// Runs the mouse events
        /// </summary>
        private void RunEvents()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            _scriptTarget.mouseRealTimePosition = Event.current.mousePosition;
            Event evnt = Event.current;
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    _mouseButtonIndex = evnt.button;
                    GetPosition(evnt.mousePosition, InspectorUtils.kLineThickness / 2);
                    _localPos = ToLocalPos(_mousePos, _backgroundRect);
                    _mousePos = evnt.mousePosition;
                    break;
                case EventType.MouseUp:
                case EventType.MouseLeaveWindow:
                    if (_focusedDay < _targetTimelapse.timelapseDays.Count)
                    {
                        for (int i = 0; i < _targetTimelapse.timelapseDays[_focusedDay].startedInside.Count; i++)
                        {
                            _targetTimelapse.timelapseDays[_focusedDay].startedInside[i] = false;
                        }
                        _targetTimelapse.timelapseDays[_focusedDay].selectedSlider = -1;
                    }
                    _mousePos = Vector2.zero;
                    _localPos = Vector2.zero;
                    _hasReleasedSlider = true;
                    break;
            }
        }

        /// <summary>
        /// Resets to the default values the the left click is up
        /// </summary>
        private void ResetOnMouseUp()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                for (int i = 0; i < _targetTimelapse.timelapseDays.Count; i++)
                {
                    _targetTimelapse.timelapseDays[i].selectedSlider = -1;
                }
                _hasReleasedSlider = true;
            }
        }

        /// <summary>
        /// Checks to see if a new interval can be added
        /// </summary>
        /// <returns>True if it can add a new interval, false otherwise</returns>
        private bool CanAddInterval()
        {
            float divisionWidth = Utilities.ToTwoDecimals(_backgroundRect.width / InspectorUtils.kLabelDivisions);
            float value = _scriptTarget.mouseRealTimePosition.x / divisionWidth;
            int currentPosition = Mathf.RoundToInt((divisionWidth * (int)value) / _backgroundRect.width * InspectorUtils.kLabelDivisions);
            int nextPosition = Mathf.RoundToInt((divisionWidth * ((int)value + 1)) / _backgroundRect.width * InspectorUtils.kLabelDivisions);

            if (_targetTimelapse.timelapseDays[_focusedDay].delimitersPos[currentPosition] || (nextPosition < InspectorUtils.kLabelDivisions && _targetTimelapse.timelapseDays[_focusedDay].delimitersPos[nextPosition]))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks to see if the posiotion is inside the box
        /// </summary>
        /// <param name="pos">The position</param>
        /// <param name="box">The clamping box</param>
        /// <returns>True if the position is inside the box, false otherwise</returns>
        private bool IsInsideBox(Vector2 pos, Rect box)
        {
            return pos.x >= box.x && pos.x <= box.x + box.width && pos.y >= box.y && pos.y <= box.y + box.height ? true : false;
        }

        /// <summary>
        /// Transforms the relative position to the real position
        /// </summary>
        /// <param name="relPos">The relative position</param>
        /// <param name="intervalSize">The size of an interval</param>
        /// <returns>The real position</returns>
        private float FromRelativePosition(float relPos, float intervalSize)
        {
            return relPos * intervalSize;
        }

        /// <summary>
        /// Transfotrms a global position to a local one
        /// </summary>
        /// <param name="pos">The position</param>
        /// <param name="box">The clamping box</param>
        /// <returns>The local position</returns>
        private Vector2 ToLocalPos(Vector2 pos, Rect box)
        {
            return new Vector2(pos.x - box.x, pos.y - box.y);
        }

        /// <summary>
        /// Transforms a relative position to a division value
        /// </summary>
        /// <param name="index">The index of the relative position</param>
        /// <param name="day">The interval day</param>
        /// <returns>The division value</returns>
        private int RelPosToPosValue(int index, int day)
        {
            return Mathf.RoundToInt(_targetTimelapse.timelapseDays[day].sliderRelPosX[index] * InspectorUtils.kLabelDivisions);
        }

        /// <summary>
        /// List all the field related errors for the Forecast Popup
        /// </summary>
        private void DrawPopupErrors()
        {
            for (int i = 0; i < _popupErrors.Length; i++)
            {
                if (!string.IsNullOrEmpty(_popupErrors[i]))
                {
                    EditorGUILayout.HelpBox(_popupErrors[i], MessageType.Error);
                }
            }
        }
        #endregion
    }
}
#endif