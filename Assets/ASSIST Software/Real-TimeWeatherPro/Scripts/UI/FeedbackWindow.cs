using UnityEditor;
using UnityEngine;

namespace RealTimeWeather.UI
{
#if UNITY_EDITOR
    public class FeedbackWindow : EditorWindow
    {
        #region Constants
        private const string kFeedbackFormTitle = "Real-Time Weather Feedback Form";
        private const string kEmailField = "Your Email Address";
        private const string kSubjectField = "Subject";
        private const string kFeedbackField = "Your Feedback";
        private const string kSendButton = "Send Mail";
        private const string kInvalidEmailTitle = "Invalid Email!";
        private const string kInvalidEmailMessage = "Please use a correct email address format!";
        private const string kInvalidEmailButtonText = "Ok";
        private const string kNewSolutionsStr = "What new weather solutions would you like integrated in Real-Time Weather?";
        private const string kNewFeaturesStr = "What new features would you like to be added in future releases?";
        private const string kEmailNewSolutions = "New weather solutions for Real-Time Weather";
        private const string kEmailNewFeatures = "New features for Real-Time Weather";
        #endregion

        #region Private Variables
        private string _emailFrom = string.Empty;
        private string _subject = string.Empty;
        private Vector2 _scroll;
        private string _details = string.Empty;
        private int _selected = 0;

        private string[] _options = new string[2] { kNewSolutionsStr, kNewFeaturesStr };
        private string[] _emailOptions = new string[2] { kEmailNewSolutions, kEmailNewFeatures };
        #endregion

        #region Public Static Methods
        /// <summary>
        /// This method Activates the Weather System Feedback Form window.
        /// </summary>
        public static void InitializeForm()
        {
            FeedbackWindow window = (FeedbackWindow)GetWindow(typeof(FeedbackWindow), false);
            window.titleContent.text = kFeedbackFormTitle;
        }

        /// <summary>
        /// This method opens the Display Dialog when the email format is invalid.
        /// </summary>
        public static void InvalidEmailDialog()
        {
            EditorUtility.DisplayDialog(kInvalidEmailTitle, kInvalidEmailMessage, kInvalidEmailButtonText);
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _emailFrom = EditorGUILayout.TextField(kEmailField, _emailFrom);
            _selected = EditorGUILayout.Popup(kSubjectField, _selected, _options);
            EditorGUILayout.LabelField(kFeedbackField);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUIStyle style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            _details = EditorGUILayout.TextArea(_details, style, GUILayout.Height(position.height));
            EditorGUILayout.EndScrollView();

            if(GUILayout.Button(kSendButton))
            {
                if(AlertSystemModuleHelper.IsValidEmail(_emailFrom))
                {
                    AlertSystemModuleHelper.SendEmail(_emailFrom, EditorConstants.kEmailTo, _emailOptions[_selected], _details);
                }
                else
                {
                    InvalidEmailDialog();
                }
            }
        }
        #endregion
    }
#endif
}