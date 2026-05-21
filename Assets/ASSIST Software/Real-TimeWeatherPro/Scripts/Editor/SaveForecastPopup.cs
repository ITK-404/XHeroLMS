//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.WeatherProvider;
using RealTimeWeather.WeatherProvider.OpenWeather;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    public class SaveForecastPopup : EditorWindow
    {
        private const string kWindowName = "Settings";
        private const int kWindowHeight = 370;
        private const int kWindowWidth = 450;
        private const int kLabelWidth = 50;
        private const int kLabelHeight = 25;
        private const int kCustomToggleStyleIndex = 6;

        private GUISkin _customGUIskin;
        private bool _isUSALocation;
        private string _startDate;
        private string _endDate;
        private string _apiKey;
        private string _cityName;
        private string _countryName;
        private float _latitude;
        private float _longitude;
        private ReverseGeocoding _reverseGeocoding = new ReverseGeocoding();
        private OpenWeatherToForecastPreset _openWeatherToForecastPreset = new OpenWeatherToForecastPreset();
        private bool _isRequestInProcess;
        private bool _useGeograficCoordinates;

        public static void InitializeForm()
        {
            var window = (SaveForecastPopup)GetWindow(typeof(SaveForecastPopup), false);
            window.titleContent.text = kWindowName;
            window.minSize = new Vector2(kWindowWidth, kWindowHeight);
            window.maxSize = new Vector2(kWindowWidth, kWindowHeight);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += ClosePopup;

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
            GUILayout.BeginHorizontal();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.BeginVertical();
            GUILayout.Space(InspectorUtils.BorderSpace);
            GUILayout.Label("Populate with Real Data", InspectorUtils.TitleInfoStyle);
            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.Label("Open weather Settings", InspectorUtils.SmallTitleInfoStyle);
            GUILayout.Space(InspectorUtils.BorderSpace);

            GUILayout.BeginHorizontal();
            GUILayout.Label("API Key", GUILayout.Width(kLabelWidth), GUILayout.Height(kLabelHeight));
            _apiKey = EditorGUILayout.TextField(_apiKey, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight));
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.BorderSpace);

            GUILayout.Label("Start Date");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Date (dd/mm/yyyy", GUILayout.Height(kLabelHeight));
            _startDate = EditorGUILayout.TextField(_startDate, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight));
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.MarginSpace);

            GUILayout.Label("End Date");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Date (dd/mm/yyyy", GUILayout.Height(kLabelHeight));
            _endDate = EditorGUILayout.TextField(_endDate, InspectorUtils.TextFieldInfoStyle, GUILayout.Height(kLabelHeight));
            GUILayout.EndHorizontal();

            GUILayout.Space(InspectorUtils.BorderSpace);

            GUILayout.Label("Global Location", InspectorUtils.LabelInfoStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Space(210);
            _useGeograficCoordinates = GUILayout.Toggle(_useGeograficCoordinates, "Use geographic coordonates", _customGUIskin.customStyles[kCustomToggleStyleIndex]);
            GUILayout.EndHorizontal();

            if (!_useGeograficCoordinates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(210);
                _isUSALocation = GUILayout.Toggle(_isUSALocation, "United States location", _customGUIskin.customStyles[kCustomToggleStyleIndex]);
                GUILayout.EndHorizontal();
                GUILayout.Space(InspectorUtils.MarginSpace);

                GUILayout.BeginHorizontal();
                GUILayout.Label("City", GUILayout.Height(kLabelHeight));
                GUILayout.Space(32);
                _cityName = EditorGUILayout.TextField("", _cityName, InspectorUtils.TextFieldInfoStyle, new GUILayoutOption[] { GUILayout.Width(140), GUILayout.Height(kLabelHeight) });
                GUILayout.FlexibleSpace();
                GUILayout.Label(_isUSALocation ? "State" : "Country", GUILayout.Height(kLabelHeight));
                GUILayout.Space(InspectorUtils.BorderSpace);
                _countryName = EditorGUILayout.TextField("", _countryName, InspectorUtils.TextFieldInfoStyle, new GUILayoutOption[] { GUILayout.Width(140), GUILayout.Height(kLabelHeight) });
                GUILayout.EndHorizontal();

                GUILayout.Space(InspectorUtils.MarginSpace);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Latitude", GUILayout.Height(kLabelHeight));
                GUILayout.Space(InspectorUtils.BorderSpace);
                _latitude = EditorGUILayout.FloatField(_latitude, InspectorUtils.TextFieldInfoStyle, new GUILayoutOption[] { GUILayout.Width(140), GUILayout.Height(kLabelHeight) });
                _latitude = Mathf.Clamp(_latitude, Utilities.kMinLatitudeValue, Utilities.kMaxLatitudeValue);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Longitude", GUILayout.Height(kLabelHeight));
                GUILayout.Space(InspectorUtils.BorderSpace);
                _longitude = EditorGUILayout.FloatField(_longitude, InspectorUtils.TextFieldInfoStyle, new GUILayoutOption[] { GUILayout.Width(140), GUILayout.Height(kLabelHeight) });
                _longitude = Mathf.Clamp(_longitude, Utilities.kMinLongitudeValue, Utilities.kMaxLongitudeValue);
                GUILayout.EndHorizontal();
            }


            GUILayout.Space(InspectorUtils.BorderSpace);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_isRequestInProcess)
            {
                EditorGUI.DrawRect(new Rect(0, 0, kWindowWidth, 380), new Color(1, 1, 1, 0.3f));
                GUILayout.Label("Don't close the window, request in process", InspectorUtils.LabelErrorStyle);
                GUILayout.Space(20);
            }

            if (GUILayout.Button("Populate", InspectorUtils.ColoredButtonStyle) && !_isRequestInProcess)
            {
                TryPopulateWithData();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(InspectorUtils.MarginSpace);
            GUILayout.EndHorizontal();
        }

        private async void TryPopulateWithData()
        {
            _isRequestInProcess = true;
            if (_openWeatherToForecastPreset == null)
            {
                _openWeatherToForecastPreset = new OpenWeatherToForecastPreset();
            }

            if (_cityName != string.Empty || _countryName != string.Empty)
            {
                var response = await _reverseGeocoding.RequestCoodinatesInformation(_cityName, _countryName);


                var lat = _latitude;
                var lng = _longitude;

                if (response != null && response.lon != string.Empty && response.lat != string.Empty)
                {
                    try
                    {
                        lat = float.Parse(response.lat);
                        lng = float.Parse(response.lon);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex);
                        _isRequestInProcess = false;
                        return;
                    }
                }

                await _openWeatherToForecastPreset.SaveDataSequence(_apiKey, lat, lng, _startDate, _endDate);
                _isRequestInProcess = false;

                Close();
            }
        }
    }
}
#endif