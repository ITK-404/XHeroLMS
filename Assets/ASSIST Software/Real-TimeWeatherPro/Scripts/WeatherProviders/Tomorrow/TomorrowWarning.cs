//
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Tomorrow
{
    /// <summary>
    /// <para>
    /// This class handles the Tomorrow errors and warnings.
    /// </para>
    /// </summary>
    [Serializable]
    public class TomorrowWarning
    {
        #region Constructors
        public TomorrowWarning()
        {
            code = 0;
            type = string.Empty;
            message = string.Empty;
            meta = new WarningMeta();
        }

        public TomorrowWarning(int code, string type, string message, WarningMeta meta)
        {
            this.code = code;
            this.type = type;
            this.message = message;
            this.meta = meta;
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private int code;
        [SerializeField]
        private string type;
        [SerializeField]
        private string message;
        [SerializeField]
        private WarningMeta meta;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// It is an int that indicates the error's code.
        /// <br>Codes in the 2xx range indicate success,
        /// 4xx category indicates errors in the provided information and
        /// 5xx codes imply there's an error with servers.</br>
        /// </para>
        /// </summary>
        public int Code
        {
            get { return code; }
            set { code = value; }
        }

        /// <summary>
        /// <para>
        /// It is a string that indicates the error's type.
        /// <br>E.g: Invalid Query Parameters.</br>
        /// </para>
        /// </summary>
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// <para>
        /// It is a string that indicates the error's description.
        /// </para>
        /// </summary
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        /// <summary>
        /// <para>
        /// It is a WarningMeta instance that indicates the error's metadata: field, start date, and end date.
        /// </para>
        /// </summary
        public WarningMeta Meta
        {
            get { return meta; }
            set { meta = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the TomorrowWarning class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n============================> Tomorrow Warning <============================\n" +
                $"Error code: {code}\n" +
                $"Error type: {type}\n" +
                $"Error description: {message}\n" +
                $"Meta: {meta.ToString()}\n");
        }
        #endregion
    }
}