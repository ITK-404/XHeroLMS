//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    public class WelcomePopup : EditorWindow
    {
        private const string kWindowName = "Welcome";
        private const int kBorderSpace = 20;

        private Texture2D _logoTexture;
        private Rect _logoRect;
        private const int kLogoYOffset = 25;
        private const int kLogoXOffset = 198;
        private const float kLogoRectWidth = 152f;
        private const float kLogoRectHeight = 36f;

        public static void InitializeForm()
        {
            var window = (WelcomePopup)GetWindow(typeof(WelcomePopup), false);
            window.titleContent.text = kWindowName;
            window.minSize = new Vector2(586, 259);
            window.maxSize = new Vector2(586, 259);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            if (_logoTexture == null)
            {
                _logoTexture = Resources.Load("textures/Logo Pro") as Texture2D;

                if (EditorGUIUtility.isProSkin == true)
                {
                    _logoTexture = Resources.Load("textures/Logo Pro") as Texture2D;
                }
            }

            GUI.DrawTexture(new Rect(_logoRect.x + kLogoXOffset, _logoRect.y + kLogoYOffset, kLogoRectWidth, kLogoRectHeight), _logoTexture, ScaleMode.ScaleToFit, true);

            GUILayout.Space(75);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Welcome to Real-Time Weather Pro", InspectorUtils.HugeTitleInfoStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("With");
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("Real-Time Weather Pro", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("you can create and manage complex weather patterns visually using a");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("timeline approach. With an");
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("intuitive User Interface", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label(", the");
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("Real-Time Editor Pro plugin", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("simplifies the");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("management of your weather engines (");
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label("Enviro, Tenkoku, Atmos, Expanse", InspectorUtils.BoldLabelInfoStyle);
            GUILayout.Space(-InspectorUtils.MarginSpace);
            GUILayout.Label(")");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(kBorderSpace);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View Tutorial", InspectorUtils.WhiteColoredButtonStyle))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLkbJAZKnBqf7De_LRpPjXZ2Pc-QRH_km_");
            }

            GUILayout.Space(kBorderSpace);

            if (GUILayout.Button("View Documentation", InspectorUtils.SmallerColoredButtonStyle))
            {
                Application.OpenURL("https://assist-software.net/sites/default/files/RTW_Documentation_Pro.pdf");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
#endif