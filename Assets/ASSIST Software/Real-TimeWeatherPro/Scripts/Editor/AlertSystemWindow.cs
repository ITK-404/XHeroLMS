//
// Copyright(c) 2020 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.UI;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace RealTimeWeather.Editors
{
    /// <summary>
    /// This class it use for alerts that are send from weather system.
    /// </summary>
    public class AlertSystemWindow : EditorWindow
    {
        #region Private Constants
        private const string kSendButtonText = "Send";
        #endregion

        #region Private Variables
        private string _emailFrom = "";
        private string _details = "Enter text...";
        private Vector2 _scroll;
        private int _selected = 1;
        private string[] _options = new string[3] { EditorConstants.kBug, EditorConstants.kServicesDown, EditorConstants.kAskQuestion };
        #endregion

        #region Public Methods
        /// <summary>
        /// This method Activates the Weather System Bug Reporter window.
        /// </summary>
        [MenuItem(EditorConstants.kBugReportOptionName, false, 100)]
        public static void Init()
        {
            AlertSystemWindow window = (AlertSystemWindow)GetWindow(typeof(AlertSystemWindow), false);
            window.titleContent.text = EditorConstants.kBugReportOptionDescription;
        }

        /// <summary>
        /// This method opens the Display Dialog when the email format is invalid.
        /// </summary>
        public static void InvalidEmailDialog()
        {
            EditorUtility.DisplayDialog("Invalid Email", "Please use a correct email address format!", "OK!");
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _selected = EditorGUILayout.Popup("Select the problem", _selected, _options);
            EditorGUILayout.LabelField("Details");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUIStyle style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            _details = EditorGUILayout.TextArea(_details, style, GUILayout.Height(position.height));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.TextField("Attached files:", LogFile.Path);
            _emailFrom = EditorGUILayout.TextField("Your Email Address:", _emailFrom);

            // Check if the send button is pressed
            if (GUILayout.Button(kSendButtonText))
            {
                if (AlertSystemModuleHelper.IsValidEmail(_emailFrom))
                {
                    AlertSystemModuleHelper.SendEmail(_emailFrom, EditorConstants.kEmailTo, _options[_selected], _details + "\n" + "Unity Version: " + Application.unityVersion + "\n" + "C# version: " + Environment.Version +  "\n" + "Platform: " + Application.platform + "\n\n" + LogFile.GetLogText() + "\n\n");
                }
                else
                {
                    InvalidEmailDialog();
                }
            }
        }
        #endregion
    }
}
#endif