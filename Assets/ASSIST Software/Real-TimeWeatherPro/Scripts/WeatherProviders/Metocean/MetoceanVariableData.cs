// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections.Generic;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    [System.Serializable]
    public class MetoceanVariableData
    {
        #region Constructor
        public MetoceanVariableData()
        {
            units = string.Empty;
            siUnits = string.Empty;
            data = new List<string>();
            noData = new List<float>();
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private string units;
        [SerializeField]
        private string siUnits;
        public List<string> data;
        [SerializeField]
        private List<float> noData;
        #endregion
    
        #region Public Properties
        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Units 
        {
            get { return units; }
            set { units = value; }
        }

        /// <summary>
        /// Unit of measurement in The International System of Units.
        /// </summary>
        public string SiUnits 
        {
            get { return siUnits; }
            set { siUnits = value; }
        }

        /// <summary>
        /// The resulting data in millimeters per hour
        /// </summary>
        public List<string> Data 
        {
            get { return data; }
            set { data = value; } 
        }

        /// <summary>
        /// If it's different than 0, there's an error retrieving data (check MetoceanData.NoDataReasons for more info)
        /// </summary>
        public List<float> NoData 
        {
            get { return noData; }
            set { noData = value; }
        }
        #endregion
    }
}
