//
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Tomorrow
{
    /// <summary>
    /// <para>
    /// This class manages Tomorrow.io API core data.
    /// </para>
    /// </summary>
    [Serializable]
    public class CoreData
    {
        #region Constructors
        public CoreData()
        {
            timelines = new List<TimeData>();
            warnings = new List<TomorrowWarning>();
        }

        public CoreData(List<TimeData> timelines)
        {
            this.timelines = timelines;
        }
        
        public CoreData(List<TimeData> timelines, List<TomorrowWarning> warnings)
        {
            this.timelines = timelines;
            this.warnings = warnings;
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private List<TimeData> timelines;
        [SerializeField]
        private List<TomorrowWarning> warnings;
        #endregion

        #region Public Properties
        /// <summary>
        /// <para>
        /// A list of TimeData instances.
        /// </para>
        /// </summary>
        public List<TimeData> TimelinesList
        {
            get
            {
                return timelines;
            }

            set
            {
                timelines = value;
            }
        }

        /// <summary>
        /// <para>
        /// A list of TomorrowWarning instances that notify the user about errors in the JSON data.
        /// </para>
        /// </summary>
        public List<TomorrowWarning> Warnings
        {
            get 
            { 
                return warnings;
            }
            set 
            { 
                warnings = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the CoreData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ($"{String.Join<TimeData>("\n", timelines)}\n" +
                $"{String.Join<TomorrowWarning>("\n", warnings)}\n");
        }
        #endregion
    }
}