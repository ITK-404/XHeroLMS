using System;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Metocean
{
    /// <summary>
    /// <para>
    /// This class manages Metocean weather data.
    /// </para>
    /// </summary>
    [Serializable]
    public class Variables
    {
        #region Constructor
        public Variables()
        {
            airTemperatureAt2m = new MetoceanVariableData();
            precipitationRate = new MetoceanVariableData();
            airPressureAtSeaLevel = new MetoceanVariableData();
            windDirectionAt10m = new MetoceanVariableData();
            windSpeedAt10m = new MetoceanVariableData();
            airHumidityAt2m = new MetoceanVariableData();
            airVisibility = new MetoceanVariableData();
            waveHeight = new MetoceanVariableData();
            wavePeriodPeak = new MetoceanVariableData();
            waveDirectionPeak = new MetoceanVariableData();
            cloudCover = new MetoceanVariableData();
            seaTemperatureAtSurface = new MetoceanVariableData();
            radiationFluxDownwardLongwave = new MetoceanVariableData();
            radiationFluxDownwardShortwave = new MetoceanVariableData();
        }
        #endregion

        #region Private Variables
        [SerializeField]
        private MetoceanVariableData airTemperatureAt2m;
        [SerializeField]
        private MetoceanVariableData precipitationRate;
        [SerializeField]
        private MetoceanVariableData airPressureAtSeaLevel;
        [SerializeField]
        private MetoceanVariableData windDirectionAt10m;
        [SerializeField]
        private MetoceanVariableData windSpeedAt10m;
        [SerializeField]
        private MetoceanVariableData airHumidityAt2m;
        [SerializeField]
        private MetoceanVariableData airVisibility;
        [SerializeField]
        private MetoceanVariableData waveHeight;
        [SerializeField]
        private MetoceanVariableData wavePeriodPeak;
        [SerializeField]
        private MetoceanVariableData waveDirectionPeak;
        [SerializeField]
        private MetoceanVariableData cloudCover;
        [SerializeField]
        private MetoceanVariableData radiationFluxDownwardLongwave;
        [SerializeField]
        private MetoceanVariableData radiationFluxDownwardShortwave;
        [SerializeField]
        private MetoceanVariableData seaTemperatureAtSurface;
        #endregion

        #region Public Properties
        /// <summary>
        /// Measured in Kelvin.
        /// </summary>
        public MetoceanVariableData AirTemperature 
        {
            get { return airTemperatureAt2m; }
            set { airTemperatureAt2m = value; }
        }

        /// <summary>
        /// The amount of precipitation that falls over time, covering the ground in a period of time.
        /// <para>
        /// Measured in mm/hr.
        /// </para>
        /// </summary>
        public MetoceanVariableData PrecipitationRate
        {
            get { return precipitationRate; }
            set { precipitationRate = value; }
        }

        /// <summary>
        /// The force exerted against a surface by the weight of the air above the surface (at the mean sea level).
        /// <para>
        /// Measured in Pa.
        /// </para>
        /// </summary>
        public MetoceanVariableData AirPressureAtSea 
        {
            get { return airPressureAtSeaLevel; }
            set { airPressureAtSeaLevel = value; }
        }

        /// <summary>
        /// The direction from which it originates, measured in degrees counter-clockwise from due north.
        /// </summary>
        public MetoceanVariableData WindDirection 
        {
            get { return windDirectionAt10m; }
            set { windDirectionAt10m = value; }
        }

        /// <summary>
        /// The fundamental atmospheric quantity caused by air moving from high to low pressure, usually due to changes in temperature.
        /// <para>
        /// Measured in m/s.
        /// </para>
        /// </summary>
        public MetoceanVariableData WindSpeed 
        {
            get { return windSpeedAt10m; }
            set { windSpeedAt10m = value; }
        }

        /// <summary>
        /// The concentration of water vapor present in the air.
        /// <para>
        /// Measured in procentages (%).
        /// </para>
        /// </summary>
        public MetoceanVariableData AirHumidity 
        {
            get { return airHumidityAt2m; }
            set { airHumidityAt2m = value; }
        }

        /// <summary>
        /// The measure of the distance at which an object or light can be clearly discerned
        /// <para>
        /// Measured in m.
        /// </para>
        /// </summary>
        public MetoceanVariableData AirVisibility 
        {
            get { return airVisibility; }
            set { airVisibility = value; }
        }

        /// <summary>
        /// The vertical distance between the crest(peak) and the trough of a wave.
        /// <para>
        /// Measured in m.
        /// </para>
        /// </summary>
        public MetoceanVariableData WaveHeight
        {
            get { return waveHeight; }
            set { waveHeight = value; }
        }

        /// <summary>
        /// The time it takes for two successive wave crests to reach a fixed point.
        /// <para>
        /// Measured in seconds.
        /// </para>
        /// </summary>
        public MetoceanVariableData WavePeriodPeak
        {
            get { return wavePeriodPeak; }
            set { wavePeriodPeak = value; }
        }

        /// <summary>
        /// The direction of waves in degrees, with 0 degrees being North
        /// </summary>
        public MetoceanVariableData WaveDirection
        {
            get { return waveDirectionPeak; }
            set { waveDirectionPeak = value; }
        }

        /// <summary>
        /// The fraction of the sky obscured by clouds when observed from a particular location.
        /// <para>
        /// Measured in procentages (%).
        /// </para>
        /// </summary>
        public MetoceanVariableData CloudCover
        {
            get { return cloudCover; }
            set { cloudCover = value; }
        }

        /// <summary>
        /// A product of both downwelling infrared energy as well as emission by the underlying surface.
        /// <para>
        /// Measured in W/m^2.
        /// </para>
        /// </summary>
        public MetoceanVariableData RadiationFluxLongwave 
        {
            get { return radiationFluxDownwardLongwave; }
            set { radiationFluxDownwardLongwave = value; }
        }

        /// <summary>
        /// A result of specular and diffuse reflection of incident shortwave radiation by the underlying surface.
        /// <para>
        /// Measured in W/m^2.
        /// </para>
        /// </summary>
        public MetoceanVariableData RadiationFluxShortwave 
        {
            get { return radiationFluxDownwardShortwave; }
            set { radiationFluxDownwardShortwave = value; }
        }

        /// <summary>
        /// The temperature of sea water near the surface (including the part under sea-ice, if any).
        /// </summary>
        public MetoceanVariableData SeaSurfaceTemperature 
        {
            get
            { 
                return seaTemperatureAtSurface;
            }
            set
            {
                seaTemperatureAtSurface = value;
            }
        }
        #endregion
    }
}