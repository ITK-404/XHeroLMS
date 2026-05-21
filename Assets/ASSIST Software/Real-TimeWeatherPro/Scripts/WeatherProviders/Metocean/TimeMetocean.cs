using System.Collections.Generic;
using System;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    [System.Serializable]
    public class TimeMetocean
    {
        #region Constructor
        public TimeMetocean()
        {
            type = string.Empty;
            units = string.Empty;
            data = new List<string>();
        }
        #endregion

        #region Private Variables
        [SerializeField] 
        private string type;
        [SerializeField] 
        private string units;
        [SerializeField] 
        private List<string> data;
        #endregion

        #region Public Properties
        public string Type 
        {
            get { return type; }
            set { type = value; }
        }
        public string Units 
        {
            get { return units; }
            set { units = value; }
        }
        public List<string> Data 
        {
            get { return data; }
            set { data = value; }
        }
        #endregion
    }
}
