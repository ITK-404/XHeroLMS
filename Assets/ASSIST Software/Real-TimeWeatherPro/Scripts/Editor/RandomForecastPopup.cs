// 
// Copyright(c) 2023 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Data;
using RealTimeWeather.Managers;
using RealTimeWeather.Simulation;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealTimeWeather.Editors
{
    public class RandomForecastPopup : EditorWindow
    {
        private const string kWindowName = "Random Forecast";
        private const int kWindowHeight = 370;
        private const int kWindowWidth = 1405;
        private const int kLabelWidth = 110;
        private const int kLabelHeight = 25;
        private const int kMinPixelWidthForImage = 41;
        private const int kHeaderSpace = 20;
        private const int kBoldToggleCustomStyleIndex = 17;
        private const int kImageOffset = 13;
        private const int kIndexOfRightClick = 0;
        private const int kMingPreseteProbability = 6;
        private const int kMaxSimulationLength = 500;

        private bool _hasReleasedSlider;
        private int _mouseButtonIndex;
        private int _slectedPreset;
        private int _selectedFolder;
        private int _selectedIndex;
        private Color _defaultColor;
        private Vector2 _mousePos = Vector2.zero;
        private Vector2 _presetsScrollPos;
        private Vector2 _localPos = Vector2.zero;
        private Rect _backgroundRect = new Rect(100, 100, 100, 100);
        private GUISkin _customGUIskin;
        private static RealTimeWeatherManager _realTimeWeatherManager;
        private static WeatherPresets _selectedWeatherPresets;
        private static List<string> _presetFoldersName = new List<string>();

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += ClosePopup;
            _defaultColor = GUI.color;

            if (_customGUIskin == null)
            {
                _customGUIskin = Resources.Load("CustomGUI") as GUISkin;
            }
        }

        private void ClosePopup(PlayModeStateChange state)
        {
            Close();
        }

        private void OnGUI()
        {
            ResetOnMouseUp();
            DrawInputFields();

            if (_realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn)
            {
                var value = Mathf.RoundToInt(100f / _realTimeWeatherManager.RandomForecastPopupData.Presets.Count());
                var sum = value;
                for (int i = 0; i < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count; i++)
                {
                    _realTimeWeatherManager.RandomForecastPopupData.Presets[i] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastWeatherData, sum);
                    sum += value;
                    if (i == _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1)
                    {
                        _realTimeWeatherManager.RandomForecastPopupData.Presets[i] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastWeatherData, 100);
                    }
                }
            }

            GUILayout.Space(kHeaderSpace);

            _backgroundRect = EditorGUILayout.GetControlRect(false, InspectorUtils.kCustomSliderHeight);
            _backgroundRect.width -= InspectorUtils.MarginSpace;

            if (_backgroundRect.y != 0)
            {
                GUI.color = Color.white;
                EditorGUI.LabelField(_backgroundRect, "");
                GUI.color = _defaultColor;

                DrawIntervals();

                if (Event.current.type == EventType.MouseDown && _mouseButtonIndex == 1 && IsInsideBox(_realTimeWeatherManager.mouseRealTimePosition, _backgroundRect))
                {
                    CustomSliderOptionsMenu();
                }
            }

            GUILayout.Space(kHeaderSpace);
            AddDayPresetList();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        public static void InitializeForm(RealTimeWeatherManager realTimeWeatherManager)
        {
            if (realTimeWeatherManager.RandomForecastPopupData == null)
            {
                realTimeWeatherManager.RandomForecastPopupData = new RandomForecastPopupData(realTimeWeatherManager);
            }
            var window = (RandomForecastPopup)GetWindow(typeof(RandomForecastPopup), false);
            window.titleContent.text = kWindowName;
            window.minSize = new Vector2(kWindowWidth, kWindowHeight);
            _realTimeWeatherManager = realTimeWeatherManager;
            _presetFoldersName.Clear();
            _presetFoldersName.Add(_realTimeWeatherManager.WeatherPresets.DefaultPreset.folderName);
            foreach (var preset in _realTimeWeatherManager.WeatherPresets.CustomPresets)
            {
                _presetFoldersName.Add(preset.folderName);
            }
            _selectedWeatherPresets = _realTimeWeatherManager.WeatherPresets.DefaultPreset;
        }

        private void DrawInputFields()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Populate with Procedural Data", InspectorUtils.TitleInfoStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(_realTimeWeatherManager.PopupTextures.InfoTexture, "This is a tab where you can generate random forecasts. The probability inside the slider represents the chance of that weather type appearing in the forecast. When the \"Random preset probability\" is selected, the values from the slider are overridden with random ones."), GUILayout.Width(30), GUILayout.Height(30));
            GUILayout.EndHorizontal();
            GUILayout.Space(kHeaderSpace);

            GUILayout.BeginVertical(InspectorUtils.InnerBoxStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Simulation length", InspectorUtils.BoldLabelInfoStyle, GUILayout.Width(kLabelWidth), GUILayout.Height(kLabelHeight));
            GUILayout.Space(InspectorUtils.BorderSpace);
            _realTimeWeatherManager.RandomForecastPopupData.ForecastLength = EditorGUILayout.IntField(_realTimeWeatherManager.RandomForecastPopupData.ForecastLength, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight), GUILayout.Width(50));
            GUILayout.Label("days", GUILayout.Width(50), GUILayout.Height(kLabelHeight));
            if(_realTimeWeatherManager.RandomForecastPopupData.ForecastLength > kMaxSimulationLength)
            {
                _realTimeWeatherManager.RandomForecastPopupData.ForecastLength = kMaxSimulationLength;
            }

            GUILayout.Space(30);
            GUILayout.Label("Preset length", InspectorUtils.BoldLabelInfoStyle, GUILayout.Width(90), GUILayout.Height(kLabelHeight));
            if (_realTimeWeatherManager.RandomForecastPopupData.ForecastLength <= 0) _realTimeWeatherManager.RandomForecastPopupData.ForecastLength = 1;

            GUILayout.Label("min", GUILayout.Width(30), GUILayout.Height(kLabelHeight));
            _realTimeWeatherManager.RandomForecastPopupData.MinPresetLength = EditorGUILayout.IntField(_realTimeWeatherManager.RandomForecastPopupData.MinPresetLength, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight), GUILayout.Width(30));
            if (_realTimeWeatherManager.RandomForecastPopupData.MinPresetLength < 1) _realTimeWeatherManager.RandomForecastPopupData.MinPresetLength = 1;

            GUILayout.Space(17);

            GUILayout.Label("max", GUILayout.Width(30), GUILayout.Height(kLabelHeight));
            _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength = EditorGUILayout.IntField(_realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight), GUILayout.Width(30));
            if (_realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength > 24) _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength = 24;
            if (_realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength < 1) _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength = 1;

            if (_realTimeWeatherManager.RandomForecastPopupData.MinPresetLength > _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength) _realTimeWeatherManager.RandomForecastPopupData.MinPresetLength = _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength;

            GUILayout.Label("hours", GUILayout.Width(kLabelWidth), GUILayout.Height(kLabelHeight));

            GUILayout.Space(200);
            _realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn = GUILayout.Toggle(_realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn, "Random Presets Probability", _customGUIskin.customStyles[kBoldToggleCustomStyleIndex]);

            GUILayout.Space(100);

            if (GUILayout.Button("Generate", GUILayout.Width(200), GUILayout.Height(kLabelHeight)))
            {
                RandomForecastGenerator.GenerateRandomForecast(TimelapsePopup.TargetTimelapse.timelapseName, _realTimeWeatherManager.RandomForecastPopupData.Presets, _realTimeWeatherManager.RandomForecastPopupData.ForecastLength, _realTimeWeatherManager.RandomForecastPopupData.MinPresetLength, _realTimeWeatherManager.RandomForecastPopupData.MaxPresetLength, _realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn);
                Close();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawIntervals()
        {
            if (_realTimeWeatherManager.RandomForecastPopupData.Presets.Count == 1)
            {
                DrawIntervalBackground(_backgroundRect.x, _backgroundRect.width, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastWeatherData.color, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastProbability.ToString());
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, 1, 0, 0));
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(55, kImageOffset, kLabelHeight, kLabelHeight, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastWeatherData.texture);
            }
            else
            {
                float posCurent = (_realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastProbability / 100f) * _backgroundRect.width;
                float posNext;

                DrawIntervalBackground(_backgroundRect.x, posCurent, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastWeatherData.color, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastProbability.ToString());
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(1, 1, 0, 0));
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(50, kImageOffset, kLabelHeight, kLabelHeight, _realTimeWeatherManager.RandomForecastPopupData.Presets[0].ForecastWeatherData.texture);

                for (int i = 0; i < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1; i++)
                {
                    posCurent = (_realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastProbability / 100f) * _backgroundRect.width;
                    posNext = (_realTimeWeatherManager.RandomForecastPopupData.Presets[i + 1].ForecastProbability / 100f) * _backgroundRect.width;
                    float intervalPosX = Mathf.Min(posCurent, posNext) + _backgroundRect.x;
                    float intervalWidth = Mathf.Abs(posCurent - posNext);

                    DrawIntervalBackground(intervalPosX, intervalWidth, _realTimeWeatherManager.RandomForecastPopupData.Presets[i + 1].ForecastWeatherData.color, (_realTimeWeatherManager.RandomForecastPopupData.Presets[i + 1].ForecastProbability - _realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastProbability).ToString());

                    if (_backgroundRect.width - intervalPosX >= kMinPixelWidthForImage)
                    {
                        DrawIntervalTexture(35 + intervalPosX, kImageOffset, kLabelHeight, kLabelHeight, _realTimeWeatherManager.RandomForecastPopupData.Presets[i + 1].ForecastWeatherData.texture);
                    }
                }
            }

            if (_realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn)
            {
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Tiling", new Vector4(kLabelHeight, 1, 0, 0));
                _realTimeWeatherManager.PopupTextures.intervalMaterial.SetVector("_Anim", Vector2.zero);
                DrawIntervalTexture(10, 0, _backgroundRect.width, 50, _realTimeWeatherManager.PopupTextures.Stripes);
            }

            Color delimiterColour = Color.grey;
            EditorGUI.DrawRect(new Rect(_backgroundRect.x - InspectorUtils.kLineThickness / 2, _backgroundRect.y, InspectorUtils.kLineThickness, _backgroundRect.height), delimiterColour);
            EditorGUI.DrawRect(new Rect(_backgroundRect.x + _backgroundRect.width - InspectorUtils.kLineThickness / 2, _backgroundRect.y, InspectorUtils.kLineThickness, _backgroundRect.height), delimiterColour);
            RunEvents();

            for (int i = 0; i < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1; i++)
            {
                float currentSliderPosX = _realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastProbability * _backgroundRect.width / 100f;

                if (Event.current.mousePosition.y >= _backgroundRect.y && Event.current.mousePosition.y <= _backgroundRect.y + _backgroundRect.height && Event.current.mousePosition != Vector2.zero)
                {
                    if (Event.current.type == EventType.MouseDown && _mouseButtonIndex == 0)
                    {
                        if (_localPos.x >= currentSliderPosX - InspectorUtils.kColliderThickness / 2 &&
                            _localPos.x <= currentSliderPosX + InspectorUtils.kColliderThickness / 2)
                        {
                            _hasReleasedSlider = true;
                            _selectedIndex = i;
                        }
                    }
                }

                DelimiterCollisionsCheck(i);
                DrawIntervalDelimiters(i);
            }
        }

        /// <summary>
        /// Opens an option menu for the custom slider
        /// </summary>
        private void CustomSliderOptionsMenu()
        {
            GenericMenu menu = new GenericMenu();

            AddMenuItem(menu, "Delete", 1);

            menu.ShowAsContext();
        }

        /// <summary>
        /// Finds the first valid index for the delimiter
        /// </summary>
        private int IndexOfClosest(Vector2 point)
        {
            int index = -1;
            Vector2 mousePosition = ToLocalPos(point, _backgroundRect);
            for (int i = 0; i < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count; i++)
            {
                float sliderPos = _realTimeWeatherManager.RandomForecastPopupData.Presets[i].ForecastProbability / 100f * _backgroundRect.width;
                if (mousePosition.x < sliderPos)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// Adds an item for the options menu
        /// </summary>
        private void AddMenuItem(GenericMenu menu, string menuPath, int index)
        {
            menu.AddItem(new GUIContent(menuPath), false, OnMenuItemClick, index);
        }

        /// <summary>
        /// Called when a menu option is clicked
        /// </summary>
        private void OnMenuItemClick(object index)
        {
            switch (index)
            {
                case 1:
                    var closestPreset = IndexOfClosest(_realTimeWeatherManager.mouseRealTimePosition);
                    if (_realTimeWeatherManager.RandomForecastPopupData.Presets.Count > 1)
                    {
                        if (closestPreset == _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1)
                        {
                            _realTimeWeatherManager.RandomForecastPopupData.Presets[closestPreset - 1] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[closestPreset - 1].ForecastWeatherData, 100);
                        }
                        _realTimeWeatherManager.RandomForecastPopupData.Presets.RemoveAt(closestPreset);
                    }
                    break;
            }
            Repaint();
        }

        /// <summary>
        /// Checks for collision between Delimiters
        /// </summary>
        private void DelimiterCollisionsCheck(int index)
        {
            if (!_hasReleasedSlider || index != _selectedIndex || _realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn)
            {
                return;
            }

            var currentDividerPosX = _localPos.x;

            float nextSliderPosX;
            float lastSliderPosX;

            if (index < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1)
            {
                nextSliderPosX = _realTimeWeatherManager.RandomForecastPopupData.Presets[index + 1].ForecastProbability / 100f * _backgroundRect.width;

                lastSliderPosX = index == 0 ? 0 : _realTimeWeatherManager.RandomForecastPopupData.Presets[index - 1].ForecastProbability / 100f * _backgroundRect.width;
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
                if (_realTimeWeatherManager.RandomForecastPopupData.Presets.Count != 1)
                {
                    if (index == 0)
                    {
                        nextSliderPosX = _realTimeWeatherManager.RandomForecastPopupData.Presets[index + 1].ForecastProbability / 100f * _backgroundRect.width;
                        if (currentDividerPosX > nextSliderPosX - InspectorUtils.kColliderThickness)
                        {
                            currentDividerPosX = nextSliderPosX - InspectorUtils.kColliderThickness;
                        }
                    }
                    else
                    {
                        lastSliderPosX = index == 0 ? 0 : _realTimeWeatherManager.RandomForecastPopupData.Presets[index - 1].ForecastProbability / 100f * _backgroundRect.width;
                        if (currentDividerPosX < lastSliderPosX + InspectorUtils.kColliderThickness)
                        {
                            currentDividerPosX = lastSliderPosX + InspectorUtils.kColliderThickness;
                        }
                    }
                }
            }

            var sliderPosition = Mathf.RoundToInt((currentDividerPosX / _backgroundRect.width) * 100);

            if (index > 0 && _realTimeWeatherManager.RandomForecastPopupData.Presets[index - 1].ForecastProbability < sliderPosition && index < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1 && _realTimeWeatherManager.RandomForecastPopupData.Presets[index + 1].ForecastProbability > sliderPosition)
            {
                _realTimeWeatherManager.RandomForecastPopupData.Presets[index] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[index].ForecastWeatherData, sliderPosition);
            }

            else if (index == 0 && sliderPosition > 0 && _realTimeWeatherManager.RandomForecastPopupData.Presets[index + 1].ForecastProbability > sliderPosition)
            {
                _realTimeWeatherManager.RandomForecastPopupData.Presets[index] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[index].ForecastWeatherData, sliderPosition);
            }
        }

        /// <summary>
        /// Resets to the default values the the left click is up
        /// </summary>
        private void ResetOnMouseUp()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == kIndexOfRightClick)
            {
                _hasReleasedSlider = false;
                _selectedIndex = -1;
                _localPos = Vector2.zero;
            }
        }

        /// <summary>
        /// Runs the mouse events
        /// </summary>
        private void RunEvents()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            _realTimeWeatherManager.mouseRealTimePosition = Event.current.mousePosition;
            Event evnt = Event.current;
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    _mouseButtonIndex = evnt.button;
                    ClampMousePosition(evnt.mousePosition, InspectorUtils.kLineThickness / 2);
                    _mousePos = evnt.mousePosition;
                    _localPos = ToLocalPos(_mousePos, _backgroundRect);
                    break;
                case EventType.MouseUp:
                case EventType.MouseLeaveWindow:
                    _mousePos = Vector2.zero;
                    _localPos = Vector2.zero;
                    _selectedIndex = -1;
                    _hasReleasedSlider = false;
                    break;
            }
        }

        /// <summary>
        /// Transfotrms a global position to a local one
        /// </summary>
        private Vector2 ToLocalPos(Vector2 pos, Rect box)
        {
            return new Vector2(pos.x - box.x, pos.y - box.y);
        }

        /// <summary>
        /// Clamps the mouse position to the current slider
        /// </summary>
        private void ClampMousePosition(Vector2 pos, float marginOffsetX = 0, float marginOffsetY = 0)
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
        /// Draws the hour interval backround
        /// </summary>
        private void DrawIntervalBackground(float posX, float width, Color intervalColour, string content)
        {
            EditorGUI.DrawRect(new Rect(posX, _backgroundRect.y, width, _backgroundRect.height), intervalColour);
            if (!_realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn)
            {
                var color = GUI.color;
                GUI.color = Color.black;
                EditorGUI.LabelField(new Rect(posX + 8, _backgroundRect.y, width, _backgroundRect.height), content + "%");
                GUI.color = color;
            }
        }

        /// <summary>
        /// Draws the hour interval texture overlay
        /// </summary>
        private void DrawIntervalTexture(float posX, float yOffset, float width, float heigth, Texture2D texture)
        {
            EditorGUI.DrawPreviewTexture(new Rect(posX, _backgroundRect.y + yOffset, width, heigth), texture, new Material(_realTimeWeatherManager.PopupTextures.intervalMaterial), ScaleMode.StretchToFill);
        }

        private void AddDayPresetList()
        {
            EditorGUILayout.BeginVertical(InspectorUtils.InnerBoxStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.Label("Weather Presets ", InspectorUtils.BoldLabelInfoStyle);

            GUILayout.Label("Folder:");
            var t = EditorGUILayout.Popup(string.Empty, _selectedFolder, _presetFoldersName.ToArray(), GUILayout.Width(130));
            if (t != _selectedFolder)
            {
                _selectedFolder = t;
                _selectedWeatherPresets = _selectedFolder == 0
                    ? _realTimeWeatherManager.WeatherPresets.DefaultPreset
                    : _realTimeWeatherManager.WeatherPresets.CustomPresets[_selectedFolder - 1];
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.MarginSpace);

            _presetsScrollPos = EditorGUILayout.BeginScrollView(_presetsScrollPos, GUILayout.Height(90));
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _selectedWeatherPresets.forecastCustomPresets.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                GUILayout.BeginHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);
                var wasThisToggleSelected = i == _slectedPreset;
                var currentToggleState = GUILayout.Toggle(wasThisToggleSelected, "", _customGUIskin.customStyles[15]);

                if (wasThisToggleSelected != currentToggleState)
                {
                    if (_realTimeWeatherManager.RandomForecastPopupData.Presets.FirstOrDefault(p => p.ForecastWeatherData == _selectedWeatherPresets.forecastCustomPresets[i]).ForecastWeatherData == null)
                    {
                        var index = -1;
                        for (int j = _realTimeWeatherManager.RandomForecastPopupData.Presets.Count - 1; j >= 0; j--)
                        {
                            if (j == 0 && _realTimeWeatherManager.RandomForecastPopupData.Presets[j].ForecastProbability >= kMingPreseteProbability)
                            {
                                index = j;
                                break;
                            }
                            if (_realTimeWeatherManager.RandomForecastPopupData.Presets[j].ForecastProbability - _realTimeWeatherManager.RandomForecastPopupData.Presets[j - 1].ForecastProbability >= kMingPreseteProbability)
                            {
                                index = j;
                                break;
                            }
                        }

                        if (index >= 0)
                        {
                            for (int j = index; j < _realTimeWeatherManager.RandomForecastPopupData.Presets.Count; j++)
                            {
                                _realTimeWeatherManager.RandomForecastPopupData.Presets[j] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[j].ForecastWeatherData, _realTimeWeatherManager.RandomForecastPopupData.Presets[j].ForecastProbability - 5);
                            }
                        }

                        _realTimeWeatherManager.RandomForecastPopupData.Presets.Add(new PresetData(_selectedWeatherPresets.forecastCustomPresets[i], 100));
                    }
                    else
                    {
                        if (_realTimeWeatherManager.RandomForecastPopupData.Presets.Count > 1)
                        {
                            var presetToBeRemoved = _realTimeWeatherManager.RandomForecastPopupData.Presets.FirstOrDefault(p => p.ForecastWeatherData == _selectedWeatherPresets.forecastCustomPresets[i]);
                            var presetIndex = _realTimeWeatherManager.RandomForecastPopupData.Presets.IndexOf(presetToBeRemoved);
                            _realTimeWeatherManager.RandomForecastPopupData.Presets.Remove(presetToBeRemoved);
                            if (presetIndex >= _realTimeWeatherManager.RandomForecastPopupData.Presets.Count)
                            {
                                _realTimeWeatherManager.RandomForecastPopupData.Presets[presetIndex - 1] = new PresetData(_realTimeWeatherManager.RandomForecastPopupData.Presets[presetIndex - 1].ForecastWeatherData, 100);
                            }
                        }
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(-19);
                GUILayout.BeginHorizontal();
                GUILayout.Space(3);
                GUI.color = _selectedWeatherPresets.forecastCustomPresets[i].color;
                GUILayout.Label(_realTimeWeatherManager.PopupTextures.TimelapsePresetForeground);
                GUI.color = _defaultColor;
                GUILayout.EndHorizontal();
                GUILayout.Space(-63);
                GUILayout.BeginHorizontal();
                GUILayout.Space(18);
                GUILayout.Label(_selectedWeatherPresets.forecastCustomPresets[i].texture, new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(40) });
                GUILayout.EndHorizontal();
                GUILayout.Label(_selectedWeatherPresets.forecastCustomPresets[i].name, InspectorUtils.SmallBlackLabelInfoStyle, GUILayout.Height(20), GUILayout.Width(70));

                if (_realTimeWeatherManager.RandomForecastPopupData.Presets.FirstOrDefault(p => p.ForecastWeatherData == _selectedWeatherPresets.forecastCustomPresets[i]).ForecastWeatherData != null)
                {
                    GUILayout.Space(-71);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(InspectorUtils.MarginSpace);
                    GUI.color = new Color(1, 1, 1, 0.8f);
                    GUILayout.Toggle(wasThisToggleSelected, "", _customGUIskin.customStyles[16]);
                    GUI.color = _defaultColor;
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                _realTimeWeatherManager.mousePos = _mousePos;
                _realTimeWeatherManager.mouseLocalPos = _localPos;
            }

            GUILayout.Space(InspectorUtils.MarginSpace);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the interval delimiters and checks if the cursor is inside the delimiter
        /// </summary>
        private void DrawIntervalDelimiters(int index)
        {
            var currentSliderX = (_realTimeWeatherManager.RandomForecastPopupData.Presets[index].ForecastProbability / 100f) * _backgroundRect.width;
            var sliderPosition = new Vector3(currentSliderX - InspectorUtils.kLineThickness / 2 + _backgroundRect.x, _backgroundRect.y);
            var sliderSize = new Vector2(InspectorUtils.kLineThickness, _backgroundRect.height);
            var sliderRect = new Rect(sliderPosition, sliderSize);
            var delimiterColour = Color.grey;

            EditorGUI.DrawRect(sliderRect, delimiterColour);

            if (IsInsideBox(_realTimeWeatherManager.mouseRealTimePosition, new Rect(new Vector3(currentSliderX - InspectorUtils.kColliderThickness / 2 + _backgroundRect.x, _backgroundRect.y), new Vector2(InspectorUtils.kColliderThickness, _backgroundRect.height))) && !_realTimeWeatherManager.RandomForecastPopupData.IsRandomProbabilityOn)
            {
                EditorGUIUtility.AddCursorRect(new Rect(_realTimeWeatherManager.mouseRealTimePosition - new Vector2(6, 6), new Vector2(12, 12)), MouseCursor.ResizeHorizontal);
            }
        }

        /// <summary>
        /// Checks to see if the posiotion is inside the box
        /// </summary>
        private bool IsInsideBox(Vector2 pos, Rect box)
        {
            return pos.x >= box.x && pos.x <= box.x + box.width && pos.y >= box.y && pos.y <= box.y + box.height ? true : false;
        }
    }
}