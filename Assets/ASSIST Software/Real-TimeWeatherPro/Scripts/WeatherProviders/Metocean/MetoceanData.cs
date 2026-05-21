using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    /// <summary>
    /// This class manages Metocean data.
    /// </summary>
    [System.Serializable]
    public class MetoceanData
    {
        #region Constructor
        public MetoceanData()
        {
            dimensions = new Dimensions();
            noDataReasons = new NoDataReasons();
            variables = new Variables();
            _latitude = 0f;
            _longitude = 0f;
            _location = string.Empty;
        }
        #endregion

        #region Private Variables
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [SerializeField]
        private string _location;
        [SerializeField]
        private string _country;
        [SerializeField]
        private Dimensions dimensions;
        [SerializeField]
        private NoDataReasons noDataReasons;
        [SerializeField]
        private Variables variables;
        #endregion

        #region Public Properties
        /// <summary>
        /// It's a float value that represets a geographic coordinate that specifies the north–south position of a point on the Earth's surface.
        /// <para>
        /// Latitude must be set according to ISO 6709.
        /// </para>
        /// </summary>
        public float Latitude 
        {
            get { return _latitude; }
            set { _latitude = value; }
        }
        
        /// <summary>
        /// It's a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.
        /// <para>
        /// Longitude must be set according to ISO 6709.
        /// </para>
        /// </summary>
        public float Longitude 
        {
            get { return _longitude; }
            set { _longitude = value; } 
        }
        
        public Dimensions Dimensions 
        {
            get { return dimensions; }
            set { dimensions = value; }
        }

        /// <summary>
        /// Reasons why data couldn't be returned
        /// </summary>
        public NoDataReasons NoDataReasons 
        {
            get { return noDataReasons; }
            set { noDataReasons = value; }
        }

        /// <summary>
        /// The core weather data from the Metocean API 
        /// </summary>
        public Variables Variables 
        {
            get { return variables; }
            set { variables = value; }
        }

        /// <summary>
        /// The location retrieved based on the given coordinates
        /// </summary>
        public string Location 
        {
            get 
            { 
                return _location;
            }
            set 
            {
                _location = value;
            }
        }

        /// <summary>
        /// The country retrieved based on the given coordinates
        /// </summary>
        public string Country 
        {
            get
            {
                return _country;
            }
            set
            {
                _country = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the MetoceanData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Metocean Data <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n" +
                   "[Variables]\n" +
                   $"Location: {_location}, Country: {_country}\n" +
                   $"Air temperature: {variables.AirTemperature.Data[0]} °C, No Data Reason: {variables.AirTemperature.NoData[0]}\n" +
                   $"Precipitation rate: {variables.PrecipitationRate.Data[0]} mm/s, No Data Reason: {variables.PrecipitationRate.NoData[0]}\n" +
                   $"Air pressure at sea level: {variables.AirPressureAtSea.Data[0]} Pa, No Data Reason: {variables.AirPressureAtSea.NoData[0]}\n" +
                   $"Wind direction: {variables.WindDirection.Data[0]}°, No Data Reason: {variables.WindDirection.NoData[0]}\n" +
                   $"Wind speed: {variables.WindSpeed.Data[0]} m/s, No Data Reason: {variables.WindSpeed.NoData[0]}\n" +
                   $"Air humidity: {variables.AirHumidity.Data[0]}%, No Data Reason: {variables.AirHumidity.NoData[0]}\n" +
                   $"Air visibility: {variables.AirVisibility.Data[0]} km, No Data Reason: {variables.AirVisibility.NoData[0]}\n" +
                   $"Wave height: {variables.WaveHeight.Data[0]} m, No Data Reason: {variables.WaveHeight.NoData[0]}\n" +
                   $"Wave period peak: {variables.WavePeriodPeak.Data[0]} s, No Data Reason: {variables.WavePeriodPeak.NoData[0]}\n" +
                   $"Wave direction: {variables.WaveDirection.Data[0]}°, No Data Reason: {variables.WaveDirection.NoData[0]}\n" +
                   $"Cloud cover: {variables.CloudCover.Data[0]}%, No Data Reason: {variables.CloudCover.NoData[0]}\n" +
                   $"Radiation flux longwave: {variables.RadiationFluxLongwave.Data[0]} w/m^2, No Data Reason: {variables.RadiationFluxLongwave.NoData[0]}\n" +
                   $"Radiation flux shortwave: {variables.RadiationFluxShortwave.Data[0]} w/m^2, No Data Reason: {variables.RadiationFluxShortwave.NoData[0]}\n" +
                   $"Sea surface temperature: {variables.SeaSurfaceTemperature.Data[0]} °C, No Data Reason: {variables.SeaSurfaceTemperature.NoData[0]}\n" +
                   "\n[No Data Reasons]\n" +
                   $"ERROR_INTERNAL: {noDataReasons.ErrorInternal}\n" +
                   $"FILL: {noDataReasons.Fill}\n" +
                   $"GAP: {noDataReasons.Gap}\n" +
                   $"GOOD: {noDataReasons.Good}\n" +
                   $"LAND: {noDataReasons.Land}\n" +
                   $"ICE: {noDataReasons.Ice}\n" +
                   $"INVALID_HIGH: {noDataReasons.InvalidHigh}\n" +
                   $"INVALID_LOW: {noDataReasons.InvalidLow}\n");
        }
        #endregion
    }
}   