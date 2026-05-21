//
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.WeatherProvider.Tomorrow;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.Data
{
    /// <summary>
    /// This class manages Tomorrow time data.
    /// </summary>
    [Serializable]
    public class TimeData
    {
        #region Constructors
        public TimeData()
        {
            timestep = kCurrentStr;
            startTime = string.Empty;
            endTime = string.Empty;
            intervals = new List<IntervalData>();
        }

        public TimeData(string timestep, string startTime, string endTime, List<IntervalData> intervals)
        {
            this.timestep = timestep;
            this.startTime = startTime;
            this.endTime = endTime;
            this.intervals = intervals;
        }
        #endregion

        #region Const Proprieties
        public const string kCurrentStr = "current";
        #endregion

        #region Private Variables
        [SerializeField]
        private string timestep;
        [SerializeField]
        private string startTime;
        [SerializeField]
        private string endTime;
        [SerializeField]
        private List<IntervalData> intervals;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// Timestep is a string instance that can have this values: "1m", "5m", "15m", "30m", "1h", "1d" or "current".
        /// <br>Default is set to "current".</br>
        /// </para>
        /// </summary>
        public string Timestep
        {
            get
            {
                return timestep;
            }

            set
            {
                timestep = value;
            }
        }

        /// <summary>
        /// <para>
        /// Start time is a string value that comes from Tomorrow API in ISO 8601 format "2019-03-20T14:09:50Z".
        /// <br>Default is set to DateTime.Now.</br>
        /// </para>
        /// </summary>
        public string StartTime
        {
            get
            {
                return startTime;
            }

            set
            {
                startTime = value;
            }
        }

        /// <summary>
        /// <para>
        /// End time is a string value that comes from Tomorrow API in ISO 8601 format "2019-03-20T14:09:50Z".
        /// </para>
        /// </summary>
        public string EndTime
        {
            get
            {
                return endTime;
            }

            set
            {
                endTime = value;
            }
        }

        /// <summary>
        /// <para>
        /// A list of IntervalData instances.
        /// </para>
        /// </summary>
        public List<IntervalData> Intervals
        {
            get
            {
                return intervals;
            }

            set
            {
                intervals = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the TimeData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n[Tomorrow Time Data]\n" +
                   $"Timestep: {timestep}\n" +
                   $"Start time: {startTime}\n" +
                   $"End time: {endTime}\n" +
                   $"{String.Join<IntervalData>("\n", intervals)}\n"
                   );
        }
        #endregion
    }
}