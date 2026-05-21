using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    [System.Serializable]
    public class Dimensions
    {
        #region Constructor
        public Dimensions()
        {
            time = new TimeMetocean();
        }
        #endregion

        #region Private Variables
        [SerializeField] private TimeMetocean time;
        #endregion

        #region Public Properties
        /// <summary>
        /// Date and time of the requested data
        /// </summary>
        public TimeMetocean Time 
        {
            get { return time; }
            set { time = value; }
        }
        #endregion
    }
}
