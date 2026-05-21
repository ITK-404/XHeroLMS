// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Classes;
using System;

namespace RealTimeWeather.Data
{
    /// <summary>
    /// A Real-Time Weather representation of water/ocean data 
    /// </summary>
    public class WaterData
    {
        #region Constructor
        public WaterData()
        {
            _localization = new Localization();
            _dateTime = DateTime.Now;
            _precipitation = 0;
            _temperature = 0;
            _airPressureAtSea = 0;
            _wind = new Wind();
            _humidity = 0;
            _visibility = 0;
            _wave = new Wave();
            _cloudCover = 0;
            _fluxLongwave = -1000f;
            _fluxShortwave = -1000f;
            _seaSurfaceTemperature = -1000f;
            _timeZone = string.Empty;
            _utcOffset = new TimeSpan();
        }
        #endregion

        #region Private Variables
        private Localization _localization;
        private DateTime _dateTime;
        private float _precipitation;
        private float _temperature;
        private float _airPressureAtSea;
        private Wind _wind;
        private float _humidity;
        private double _visibility;
        private Wave _wave;
        private float _cloudCover;
        private float _fluxLongwave;
        private float _fluxShortwave;
        private float _seaSurfaceTemperature;
        private string _timeZone;
        private TimeSpan _utcOffset;
        #endregion

        #region Public Properties
        /// <summary>
        /// Localization data represented in coordinates
        /// </summary>
        public Localization Localization 
        {
            get { return _localization; }
            set { _localization = value; }
        }

        /// <summary>
        /// Date and time of the water data
        /// </summary>
        public DateTime DateTime
        {
            get { return _dateTime; }
            set { _dateTime = value; }
        }

        /// <summary>
        /// The amount of precipitation that falls over time, covering the ground in a period of time.
        /// </summary>
        public float Precipitation 
        {
            get { return _precipitation; }
            set { _precipitation = value; }
        }

        /// <summary>
        /// Measure of hotness or coldness expressed in the area
        /// </summary>
        public float Temperature 
        {
            get { return _temperature; }
            set { _temperature = value; }
        }

        /// <summary>
        /// The force exerted against a surface by the weight of the air above the surface (at the mean sea level).
        /// </summary>
        public float AirPressureAtSea 
        {
            get { return _airPressureAtSea; }
            set { _airPressureAtSea = value; } 
        }

        /// <summary>
        /// The natural movement of air of any velocity
        /// </summary>
        public Wind Wind 
        {
            get { return _wind; }
            set { _wind = value; }
        }

        /// <summary>
        /// The concentration of water vapor present in the air.
        /// </summary>
        public float Humidity 
        {
            get { return _humidity; }
            set { _humidity = value; }
        }

        /// <summary>
        /// The measure of the distance at which an object or light can be clearly discerned
        /// </summary>
        public double Visibility 
        {
            get { return _visibility; }
            set { _visibility = value; } 
        }
        
        /// <summary>
        /// The moving ridge or swell on the ocean/sea surface
        /// </summary>
        public Wave Wave 
        {
            get { return _wave; }
            set { _wave = value; }
        }

        /// <summary>
        /// The fraction of the sky obscured by clouds when observed from a particular location.
        /// Represented in percentages
        /// </summary>
        public float CloudCover 
        {
            get { return _cloudCover; }
            set { _cloudCover = value; }
        }

        /// <summary>
        /// A product of both downwelling infrared energy as well as emission by the underlying surface.
        /// Measured in W/m^2
        /// </summary>
        public float FluxLongwave 
        {
            get { return _fluxLongwave; }
            set { _fluxLongwave = value; }
        }

        /// <summary>
        /// A result of specular and diffuse reflection of incident shortwave radiation by the underlying surface.
        /// Measured in W/m^2
        /// </summary>
        public float FluxShortwave 
        {
            get { return _fluxShortwave; }
            set { _fluxShortwave = value; }
        }

        /// <summary>
        /// The temperature of sea water near the surface (including the part under sea-ice, if any).
        /// Measured in Celsius
        /// </summary>
        public float SeaSurfaceTemperature 
        {
            get
            {
                return _seaSurfaceTemperature;
            }
            set
            {
                _seaSurfaceTemperature = value;
            }
        }

        /// <summary>
        /// The standard time in a specific zone
        /// </summary>
        public string TimeZone 
        {
            get
            {
                return _timeZone;
            }
            set
            {
                _timeZone = value;
            }
        }

        /// <summary>
        /// The UTC offset is the difference in hours and minutes between Coordinated Universal Time (UTC) and local solar time, at a particular place.
        /// </summary>
        public TimeSpan UtcOffset 
        {
            get 
            {
                return _utcOffset;
            }
            set 
            { 
                _utcOffset = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the RealTime Weather WaterData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return "[Water Data]\n" +
                $"Latitude: {_localization.Latitude}\n" +
                $"Longitude: {_localization.Longitude}\n" +
                $"Date: {_dateTime}\n" +
                $"Precipitation Rate: {_precipitation} mm/s\n" +
                $"Temperature: {_temperature} °C\n" +
                $"Air Pressure At Sea: {_airPressureAtSea} Pa\n" +
                $"Wind Direction: {_wind.Direction}\n" +
                $"Wind Speed: {_wind.Speed} m/s\n" +
                $"Humidity: {_humidity}%\n" +
                $"Visibility: {_visibility} m\n" +
                $"Wave Height: {_wave.Height} m\n" +
                $"Wave Period Peak: {_wave.PeriodPeak} s\n" +
                $"Wave Direction: {_wave.Direction}\n" +
                $"Cloud Cover: {_cloudCover}%\n" +
                $"Radiation Flux Longwave: {_fluxLongwave} w/m^2\n" +
                $"Radiation Flux Shortwave: {_fluxShortwave} w/m^2\n" + 
                $"Sea Surface Temperature: {_seaSurfaceTemperature} °C\n" +
                $"Time Zone: {_timeZone}\n" +
                $"UTC Offset: {_utcOffset}\n";
        }
        #endregion
    }
}