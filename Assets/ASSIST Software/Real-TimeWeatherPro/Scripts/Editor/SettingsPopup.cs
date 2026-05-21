//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Data;
using RealTimeWeather.Enums;
using RealTimeWeather.Managers;
using RealTimeWeather.Simulation;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static RealTimeWeather.Managers.RealTimeWeatherManager;

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    public class SettingsPopup : EditorWindow
    {
        private const string kWindowName = "Settings";
        private const string kPrecipitationStr = "Precipitation";
        private const string kTemperatureStr = "Temperature";
        private const string kVisibilityStr = "Visibility";
        private const string kDirectionStr = "Wind Direction";
        private const string kPressureStr = "Pressure";
        private const string kHumidityStr = "Humidity";
        private const string kDewpointStr = "Dewpoint";
        private const string kIndexUVStr = "IndexUV";
        private const string kSpeedStr = "Wind Speed";

        private const string kPrecipitationUnitStr = "mm";
        private const string kVisibilityUnitStr = "km";
        private const string kPressureUnitStr = "mbar";
        private const string kDirectionUnitStr = "°";
        private const string kSpeedUnitStr = "km/h";
        private const string kCelsiusUnitStr = "°C";
        private const string kHumidityUnitStr = "%";

        private const int kMinTemperature = -273;
        private const int kMinPrecipitation = 0;
        private const int kMinDewPoint = -273;
        private const int kMinVisibility = 0;
        private const int kMinWindSpeed = 0;
        private const int kMinPressure = 0;
        private const int kMinHumidity = 0;
        private const int kMinIndexUV = 1;

        private const int kMaxPrecipitation = 50000;
        private const int kMaxVisibility = 10000;
        private const int kMaxTemperature = 100;
        private const int kMaxWindSpeed = 1000;
        private const int kMaxPressure = 1400;
        private const int kMaxHumidity = 100;
        private const int kMaxDewPoint = 100;
        private const int kMaxIndexUV = 11;

        private const int kLabelSpace = 30;
        private const int kImageOffset = -26;
        private const int kHorizontalImageOffset = 23;
        private const int kTextOffset = -68;
        private const int kSelectePresetImageOffset = -82;
        private const int kPresetBorder = 6;
        private const int kImageSize = 40;
        private const int kPresetImageSize = 80;

        private static RealTimeWeatherManager _scriptTarget;

        private Vector2 _scrollPos = Vector2.zero;
        private int _selectedPreset;
        private GUISkin testGUIskin;
        private Color _defaultColor;
        private int _selectedIndex = 0;
        private int _selectedFolderIndex;
        private string[] _weatherStates = { "Clear", "Partialy Clear", "Cloudy", "Partialy Cloudy", "Mist", "Thunderstorm", "Sleet", "Rain", "Snow", "Windy", "Partialy Sunny", "Sunny", "Fair" };
        private string _newFolderName;
        private static WeatherPresets _selectedWeatherPresets;
        private static List<string> _presetFoldersName = new List<string>();

        public static void InitializeForm(RealTimeWeatherManager scripTarget)
        {
            _scriptTarget = scripTarget;

            var window = (SettingsPopup)GetWindow(typeof(SettingsPopup), false);
            window.titleContent.text = kWindowName;
            window.minSize = new Vector2(780, 524);
            window.maxSize = new Vector2(780, 524);
            _presetFoldersName.Clear();
            _presetFoldersName.Add(_scriptTarget.WeatherPresets.DefaultPreset.folderName);
            foreach (var preset in _scriptTarget.WeatherPresets.CustomPresets)
            {
                _presetFoldersName.Add(preset.folderName);
            }
            _selectedWeatherPresets = _scriptTarget.WeatherPresets.DefaultPreset;
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += ClosePopup;

            if (testGUIskin == null)
            {
                testGUIskin = Resources.Load("CustomGUI") as GUISkin;
            }
        }

        private void ClosePopup(PlayModeStateChange state)
        {
            this.Close();
        }
        private void OnGUI()
        {
            _defaultColor = GUI.color;
            GUILayout.BeginHorizontal(GUILayout.MinWidth(300));
            GUILayout.Space(10);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, InspectorUtils.OuterBoxStyle, GUILayout.Width(205));
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            GUILayout.Label("Weather Presets", InspectorUtils.BoldTitleInfoStyle);

            GUILayout.Space(15);

            GUILayout.BeginVertical(InspectorUtils.InnerBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Folder:");
            var x = EditorGUILayout.Popup(string.Empty, _selectedFolderIndex, _presetFoldersName.ToArray(), GUILayout.Width(130));
            if (x != _selectedFolderIndex)
            {
                _selectedFolderIndex = x;

                if (_selectedFolderIndex == 0)
                {
                    _selectedWeatherPresets = _scriptTarget.WeatherPresets.DefaultPreset;
                    _selectedIndex = 0;
                }
                else
                {
                    _selectedWeatherPresets = _scriptTarget.WeatherPresets.CustomPresets[_selectedFolderIndex - 1];
                    _selectedIndex = 0;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (_selectedFolderIndex != 0 && GUILayout.Button("Delete selected folder"))
            {
                _scriptTarget.WeatherPresets.DeletePresetsFolder(_selectedWeatherPresets.folderName);
                _scriptTarget.WeatherPresets.CustomPresets.RemoveAt(_selectedFolderIndex - 1);
                _selectedFolderIndex = 0;
                _selectedWeatherPresets = _scriptTarget.WeatherPresets.DefaultPreset;
                _selectedIndex = 0;
                _presetFoldersName.Clear();
                _presetFoldersName.Add(_scriptTarget.WeatherPresets.DefaultPreset.folderName);
                foreach (var preset in _scriptTarget.WeatherPresets.CustomPresets)
                {
                    _presetFoldersName.Add(preset.folderName);
                }
            }
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("New folder name:");
            _newFolderName = EditorGUILayout.TextField(_newFolderName);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Create new folder", GUILayout.Height(25)))
            {
                _scriptTarget.WeatherPresets.CreateNewCustomPresetsFolder(_newFolderName);
                _presetFoldersName.Clear();
                _presetFoldersName.Add(_scriptTarget.WeatherPresets.DefaultPreset.folderName);
                foreach (var preset in _scriptTarget.WeatherPresets.CustomPresets)
                {
                    _presetFoldersName.Add(preset.folderName);
                }
                _selectedFolderIndex = _presetFoldersName.Count - 1;
                _selectedWeatherPresets = _scriptTarget.WeatherPresets.CustomPresets[_scriptTarget.WeatherPresets.CustomPresets.Count - 1];
                _newFolderName = string.Empty;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("Presets", InspectorUtils.BoldTitleInfoStyle);
            if (_selectedFolderIndex > 0 && GUILayout.Button("Create new preset", GUILayout.Height(25)))
            {
                _scriptTarget.WeatherPresets.CustomPresets[_selectedFolderIndex - 1].AddWeatherDataToCustomPresets(new ForecastWeatherData(_scriptTarget.DefaultForecastData));
            }
            GUILayout.Space(10);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            if (_selectedFolderIndex == 0)
            {
                AddPresetList(_scriptTarget.WeatherPresets.DefaultPreset);
            }
            else
            {
                AddPresetList(_scriptTarget.WeatherPresets.CustomPresets[_selectedFolderIndex - 1]);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.Space(InspectorUtils.MarginSpace);

            AddPresetEditor();

            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
        }

        private void AddPresetEditor()
        {
            if (_selectedWeatherPresets.forecastCustomPresets.Count <= 0) return;
            var weatherData = _selectedWeatherPresets.forecastCustomPresets[_selectedIndex];

            EditorGUIUtility.labelWidth = 100;
            GUILayout.BeginHorizontal(InspectorUtils.OuterBoxStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.BorderSpace);
            EditorGUILayout.LabelField("Global Settings", InspectorUtils.BoldTitleInfoStyle);
            EditorGUILayout.LabelField("Manage Weather Interval Presets");
            GUILayout.Space(InspectorUtils.BorderSpace);
            EditorGUILayout.LabelField("Preset Name");
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 250;
            _selectedWeatherPresets.forecastCustomPresets[_selectedIndex].name = EditorGUILayout.TextField(_selectedWeatherPresets.forecastCustomPresets[_selectedIndex].name);
            EditorGUIUtility.labelWidth = 100;
            GUILayout.Space(80);

            if (GUILayout.Button("Mark as default"))
            {
                _scriptTarget.DefaultForecastData = _selectedWeatherPresets.forecastCustomPresets[_selectedIndex];
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color", GUILayout.Width(50));
            EditorGUILayout.Space(InspectorUtils.BorderSpace);
            EditorGUILayout.LabelField("Choose icon");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            _selectedWeatherPresets.forecastCustomPresets[_selectedIndex].color = EditorGUILayout.ColorField(_selectedWeatherPresets.forecastCustomPresets[_selectedIndex].color, GUILayout.Width(40), GUILayout.Height(25));

            EditorGUILayout.Space(25);

            for (int i = 0; i < _scriptTarget.WeatherPresets.ForecastImages.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(kLabelSpace));
                GUILayout.BeginHorizontal();
                bool x = i == _selectedPreset;
                bool v = GUILayout.Toggle(x, "", testGUIskin.customStyles[9]);

                if (x != v)
                {
                    _selectedPreset = i;
                    _selectedWeatherPresets.forecastCustomPresets[_selectedIndex].texture = _scriptTarget.WeatherPresets.ForecastImages[i];
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(-32);
                GUILayout.BeginHorizontal();
                GUILayout.Space(0);
                GUILayout.Label(_scriptTarget.WeatherPresets.ForecastImages[i], new GUILayoutOption[] { GUILayout.Width(kLabelSpace), GUILayout.Height(kLabelSpace) });
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);
            float angle = Utilities.Vector2ToDegree(weatherData.forecastWind.forecastDirection);
            angle = AddFloatFieldWithMeasuringUnit(angle, kDirectionStr, kDirectionUnitStr);
            weatherData.forecastWind.forecastDirection = Utilities.DegreeToVector2(angle);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastDewpoint = AddFloatFieldWithMeasuringUnit(weatherData.forecastDewpoint, kDewpointStr, kCelsiusUnitStr, kMinDewPoint, kMaxDewPoint);
            GUILayout.Space(kLabelSpace);
            weatherData.foreacastPrecipitation = AddFloatFieldWithMeasuringUnit(weatherData.foreacastPrecipitation, kPrecipitationStr, kPrecipitationUnitStr, kMinPrecipitation, kMaxPrecipitation);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastTemperature = AddFloatFieldWithMeasuringUnit(weatherData.forecastTemperature, kTemperatureStr, kCelsiusUnitStr, kMinTemperature, kMaxTemperature);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastIndexUV = AddIntFieldWithMeasuringUnit((int)weatherData.forecastIndexUV, kIndexUVStr, "", kMinIndexUV, kMaxIndexUV);
            GUILayout.EndVertical();

            GUILayout.Space(40);
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);
            weatherData.forecastWind.forecastSpeed = AddFloatFieldWithMeasuringUnit(weatherData.forecastWind.forecastSpeed, kSpeedStr, kSpeedUnitStr, kMinWindSpeed, kMaxWindSpeed);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastPressure = AddFloatFieldWithMeasuringUnit(weatherData.forecastPressure, kPressureStr, kPressureUnitStr, kMinPressure, kMaxPressure);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastVisibility = AddFloatFieldWithMeasuringUnit(weatherData.forecastVisibility, kVisibilityStr, kVisibilityUnitStr, kMinVisibility, kMaxVisibility);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastHumidity = AddFloatFieldWithMeasuringUnit(weatherData.forecastHumidity, kHumidityStr, kHumidityUnitStr, kMinHumidity, kMaxHumidity);
            GUILayout.Space(kLabelSpace);
            weatherData.forecastWeatherState = (WeatherState)EditorGUILayout.Popup("Weather state", (int)weatherData.forecastWeatherState, _weatherStates, GUILayout.Width(220));
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            if (_selectedFolderIndex != 0 && GUILayout.Button("Delete", GUILayout.Width(100), GUILayout.Height(25)))
            {
                _scriptTarget.WeatherPresets.CustomPresets[_selectedFolderIndex - 1].forecastCustomPresets.RemoveAt(_selectedIndex);
                _selectedIndex = 0;
            }
            GUILayout.Space(25);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void AddPresetList(WeatherPresets weatherPresets)
        {
            GUILayout.BeginVertical();

            var count = weatherPresets.forecastCustomPresets.Count;

            for (int i = 0; count % 2 == 0 ? i < count / 2 : i <= count / 2; i++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(50));
                GUILayout.BeginHorizontal();
                GUILayout.Space(kPresetBorder);
                bool x = i == _selectedIndex;
                bool v = GUILayout.Toggle(x, "", testGUIskin.customStyles[8]);

                if (x != v)
                {
                    _selectedIndex = i;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(kImageOffset);
                GUI.color = weatherPresets.forecastCustomPresets[i].color;
                GUILayout.Label(_scriptTarget.PopupTextures.PresetForeground);
                GUI.color = _defaultColor;
                GUILayout.Space(kTextOffset);
                GUILayout.BeginHorizontal();
                GUILayout.Space(kHorizontalImageOffset);
                GUILayout.Label(weatherPresets.forecastCustomPresets[i].texture, new GUILayoutOption[] { GUILayout.Width(kImageSize), GUILayout.Height(kImageSize) });
                GUILayout.EndHorizontal();
                GUILayout.Label(weatherPresets.forecastCustomPresets[i].name, InspectorUtils.SmallBlackLabelInfoStyle, GUILayout.Height(20), GUILayout.Width(kPresetImageSize));
                GUILayout.Space(kPresetBorder);
                if (x)
                {
                    GUILayout.Space(kSelectePresetImageOffset);
                    GUILayout.Label(_scriptTarget.PopupTextures.PresetSelected, GUILayout.Width(kPresetImageSize), GUILayout.Height(kPresetImageSize));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(InspectorUtils.BorderSpace);
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            for (int i = count % 2 == 0 ? count / 2 : 1 + count / 2; i < count; i++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(50));
                GUILayout.BeginHorizontal();
                GUILayout.Space(kPresetBorder);
                bool x = i == _selectedIndex;
                bool v = GUILayout.Toggle(x, "", testGUIskin.customStyles[8]);

                if (x != v)
                {
                    _selectedIndex = i;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(kImageOffset);
                GUI.color = weatherPresets.forecastCustomPresets[i].color;
                GUILayout.Label(_scriptTarget.PopupTextures.PresetForeground);
                GUI.color = _defaultColor;
                GUILayout.Space(kTextOffset);
                GUILayout.BeginHorizontal();
                GUILayout.Space(kHorizontalImageOffset);
                GUILayout.Label(weatherPresets.forecastCustomPresets[i].texture, new GUILayoutOption[] { GUILayout.Width(kImageSize), GUILayout.Height(kImageSize) });
                GUILayout.EndHorizontal();
                GUILayout.Label(weatherPresets.forecastCustomPresets[i].name, InspectorUtils.SmallBlackLabelInfoStyle, GUILayout.Height(20), GUILayout.Width(kPresetImageSize));
                GUILayout.Space(kPresetBorder);
                if (x)
                {
                    GUILayout.Space(kSelectePresetImageOffset);
                    GUILayout.Label(_scriptTarget.PopupTextures.PresetSelected, GUILayout.Width(kPresetImageSize), GUILayout.Height(kPresetImageSize));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(InspectorUtils.BorderSpace);
            }

            GUILayout.EndVertical();
        }

        private float AddFloatFieldWithMeasuringUnit(float value, string label, string unit, float clampMin = float.MinValue, float clampMax = float.MaxValue)
        {
            float val;
            EditorGUILayout.BeginHorizontal();
            val = EditorGUILayout.FloatField(label, value, InspectorUtils.InputLabelStyle, GUILayout.Width(200), GUILayout.Height(25));
            val = Mathf.Clamp(val, clampMin, clampMax);
            EditorGUILayout.LabelField(unit, GUILayout.MaxWidth(40));
            EditorGUILayout.EndHorizontal();
            return val;
        }

        private int AddIntFieldWithMeasuringUnit(int value, string label, string unit, float clampMin = float.MinValue, float clampMax = float.MaxValue)
        {
            int val;
            EditorGUILayout.BeginHorizontal();
            val = EditorGUILayout.IntField(label, value, InspectorUtils.InputLabelStyle, GUILayout.Width(200), GUILayout.Height(25));
            val = (int)Mathf.Clamp(val, clampMin, clampMax);
            EditorGUILayout.LabelField(unit, GUILayout.MaxWidth(40));
            EditorGUILayout.EndHorizontal();
            return val;
        }
    }
}
#endif