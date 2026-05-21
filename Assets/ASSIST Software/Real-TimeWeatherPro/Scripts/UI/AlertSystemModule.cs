//
// Copyright(c) 2020 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RealTimeWeather.UI
{
    /// <summary>
    /// This class it use for alerts that are send from weather system in unity builds.
    /// </summary
    public class AlertSystemModule : MonoBehaviour
    {
        #region Private Variables
        public string[] _options = new string[3] { EditorConstants.kBug, EditorConstants.kServicesDown, EditorConstants.kAskQuestion };
        public Dropdown dropdownList;
        public Button closeButton, sendButton;
        public Text emailFrom;
        public InputField details;
        public Text pathText;
        public GameObject content;
        public GameObject invalidEmail;
        #endregion

#if UNITY_2019_OR_NEWER
#region Unity Methods
        void Start()
        {
            dropdownList.AddOptions(_options.OfType<string>().ToList());
            closeButton.onClick.AddListener(delegate { CloseView(); });
            sendButton.onClick.AddListener(delegate { SendEmail(); });
            pathText.text = LogFile.Path;
        }

#endregion
#endif

#region Private Methods
        /// <summary>
        /// Open a email client so user can send a email
        /// </summary>
        void SendEmail()
        {
            if (AlertSystemModuleHelper.IsValidEmail(emailFrom.text))
            {
                AlertSystemModuleHelper.SendEmail(emailFrom.text, EditorConstants.kEmailTo, _options[dropdownList.value], details.text + "\n\n" + LogFile.GetLogText() + "\n\n");
                details.text = string.Empty;
                CloseView();
            }
            else
            {
                invalidEmail.SetActive(true);
                LogFile.Write("Invalid Email Dialog!");
            }
        }

        /// <summary>
        /// This function make containt visible in scene
        /// </summary>
        public void OpenView()
        {
            content.SetActive(true);
        }

        /// <summary>
        /// This function make containt invisible in scene
        /// </summary>
        private void CloseView()
        {
            content.SetActive(false);
        }
#endregion
    }
}