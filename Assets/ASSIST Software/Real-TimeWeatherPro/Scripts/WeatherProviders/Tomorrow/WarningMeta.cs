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
    /// This class handles the warning meta data.
    /// </para>
    /// </summary>
    [Serializable]
    public class WarningMeta
    {
        #region Constructors
        public WarningMeta()
        {
            field = String.Empty;
            from = DateTime.Now;
            to = DateTime.Now;
        }

        public WarningMeta(string field, DateTime from, DateTime to)
        {
            this.field = field;
            this.from = from;
            this.to = to;
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private string field;
        [SerializeField]
        private DateTime from;
        [SerializeField]
        private DateTime to;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// WarningField is a string value that comes from the Tomorrow API and specifies the data filed for which the error was raised.
        /// </para>
        /// </summary>
        public string WarningField
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }

        /// <summary>
        /// <para>
        /// StartDate is a DateTime value that comes from Tomorrow API in ISO 8601 format "2019-03-20T14:09:50Z".
        /// </para>
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return from;
            }

            set
            {
                from = value;
            }
        }

        /// <summary>
        /// <para>
        /// EndDate is a DateTime value that comes from Tomorrow API in ISO 8601 format "2019-03-20T14:09:50Z".
        /// </para>
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return to;
            }

            set
            {
                to = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the WarningMeta class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n============================> Tomorrow Warning Meta <============================\n" +
                $"Warning field: {field}\n" +
                $"Start date: {from.ToString()}\n" +
                $"End date: {to.ToString()}\n");
        }
        #endregion
    }
}