using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    /// <summary>
    /// <para>
    /// Data from the Metocean API used for error handling
    /// </para>
    /// </summary>
    [System.Serializable]
    public class NoDataReasons
    {
        #region Constructor
        public NoDataReasons()
        {
            ERROR_INTERNAL = 0;
            FILL = 0;
            GAP = 0;
            GOOD = 0;
            MASK_LAND = 0;
            MASK_ICE = 0;
            INVALID_HIGH = 0;
            INVALID_LOW = 0;
        }
        #endregion

        #region Private Variables
        [SerializeField] private int ERROR_INTERNAL;
        [SerializeField] private int FILL;
        [SerializeField] private int GAP;
        [SerializeField] private int GOOD;
        [SerializeField] private int MASK_LAND;
        [SerializeField] private int MASK_ICE;
        [SerializeField] private int INVALID_HIGH;
        [SerializeField] private int INVALID_LOW;
        #endregion

        #region Public Properties
        /// <summary>
        /// Metocean internal error
        /// </summary>
        public int ErrorInternal 
        {
            get { return ERROR_INTERNAL; }
            set { ERROR_INTERNAL = value; }
        }

        /// <summary>
        /// Some or all data for the requested variable contains placeholder "fill" values. Often found near boundaries or because of threshold/partition conditions.
        /// </summary>
        public int Fill 
        {
            get { return FILL; }
            set { FILL = value; }
        }

        /// <summary>
        /// No accessible models in the API contain the variable requested at that point and time.
        /// </summary>
        public int Gap 
        {
            get { return GAP; }
            set { GAP = value; }
        }

        /// <summary>
        /// Data returned succesfully.
        /// </summary>
        public int Good 
        {
            get { return GOOD; }
            set { GOOD = value; }
        }

        /// <summary>
        /// No valid data was available because the location requested in the model decided upon is on land and the variable in question is not available on land.
        /// </summary>
        public int Land 
        {
            get { return MASK_LAND; }
            set { MASK_LAND = value; }
        }

        /// <summary>
        /// Similar to LAND but for some polar regions where ice cover interferes.
        /// </summary>
        public int Ice 
        {
            get { return MASK_ICE; }
            set { MASK_ICE = value; }
        }

        /// <summary>
        /// Data was found but it was outside the representable range of the requested variable's numeric domain.
        /// </summary>
        public int InvalidHigh 
        {
            get { return INVALID_HIGH; }
            set { INVALID_HIGH = value; }
        }

        /// <summary>
        /// Data was found but it was outside the representable range of the requested variable's numeric domain.
        /// </summary>
        public int InvalidLow 
        {
            get { return INVALID_LOW; }
            set { INVALID_LOW = value; }
        }
        #endregion
    }
}