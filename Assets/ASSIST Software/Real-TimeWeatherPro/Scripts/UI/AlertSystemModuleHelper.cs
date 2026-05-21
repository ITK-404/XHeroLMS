//
// Copyright(c) 2020 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace RealTimeWeather.UI
{
    /// <summary>
    /// This class it use for Alert system in editor and build
    /// </summary>
    public static class AlertSystemModuleHelper
    {
        #region Public Methods
        /// <summary>
        /// This method send e-mail using Open URL
        /// </summary>
        /// <param name="emailFrom">Sender e-mail </param>
        /// <param name="emailTo">Destinator e-mail</param>
        /// <param name="subject">Message of the e-mail</param>
        /// <param name="body">Containt of the the e-mail</param>
        public static void SendEmail(string emailFrom, string emailTo, string subject, string body)
        {
            subject = MyEscapeURL(subject);
            body = MyEscapeURL(body);
            Application.OpenURL("mailto:" + emailTo + "?subject=" + subject + "&body=" + body + " - " + emailFrom);
        }

        /// <summary>
        /// Escapes characters in a string to ensure they are URL-friendly.
        /// </summary>
        /// <param name="url">A string with characters to be escaped.</param>
        /// <returns>Escaped string</returns>
        public static string MyEscapeURL(string url)
        {
#if UNITY_2019_3_OR_NEWER
            return UnityWebRequest.EscapeURL(url).Replace("+", "%20");;
#else
            return WWW.EscapeURL(url).Replace("+", "%20"); ;
#endif
        }

        /// <summary>
        /// This function it determines whether its format is valid for an email address.
        /// </summary>
        /// <param name="email"> E-mail address that is validate/param>
        /// <returns>True if email is valid</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
            }
            catch (Exception exception)
            {
                LogFile.Write(exception.ToString());
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
        // Examines the domain part of the email and normalizes it.
        static string DomainMapper(Match match)
        {
            // Use IdnMapping class to convert Unicode domain names.
            var idn = new IdnMapping();

            // Pull out and process domain name (throws ArgumentException on invalid)
            var domainName = idn.GetAscii(match.Groups[2].Value);

            return match.Groups[1].Value + domainName;
        }
        #endregion
    }
}