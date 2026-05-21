//
// Copyright(c) 2021 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.ComponentModel;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider.Tomorrow
{
    /// <summary>
    /// <para>
    /// PrecipitationType enum values are:
    /// <br>0: N/A</br>
    /// <br>1: Rain</br>
    /// <br>2: Snow</br>
    /// <br>3: Freezing Rain</br>
    /// <br>4: Ice Pellets</br>
    /// </para>
    /// </summary>
    public enum PrecipitationType
    {
        [Description("0: N/A")] NA,
        [Description("1: Rain")] Rain,
        [Description("2: Snow")] Snow,
        [Description("3: Freezing Rain")] FreezingRain,
        [Description("4: Ice Pellets")] IcePellets
    }

    /// <summary>
    /// <para>
    /// WeatherCode enum values are:
    /// <br>0: Unknown</br>
    /// <br>1000: Clear</br>
    /// <br>1001: Cloudy</br>
    /// <br>1100: Mostly Clear</br>
    /// <br>1101: Partly Cloudy</br>
    /// <br>1102: Mostly Cloudy</br>
    /// <br>2000: Fog</br>
    /// <br>2100: Light Fog</br>
    /// <br>3000: Light Wind</br>
    /// <br>3001: Wind</br>
    /// <br>3002: Strong Wind</br>
    /// <br>4000: Drizzle</br>
    /// <br>4001: Rain</br>
    /// <br>4200: Light Rain</br>
    /// <br>4201: Heavy Rain</br>
    /// <br>5000: Snow</br>
    /// <br>5001: Flurries</br>
    /// <br>5100: Light Snow</br>
    /// <br>5101: Heavy Snow</br>
    /// <br>6000: Freezing Drizzle</br>
    /// <br>6001: Freezing Rain</br>
    /// <br>6200: Light Freezing Rain</br>
    /// <br>6201: Heavy Freezing Rain</br>
    /// <br>7000: Ice Pellets</br>
    /// <br>7101: Heavy Ice Pellets</br>
    /// <br>7102: Light Ice Pellets</br>
    /// <br>8000: Thunderstorm</br>
    /// </para>
    /// </summary>
    public enum WeatherCode
    {
        [Description("0: Unknown")] Unknown = 0,
        [Description("1000: Clear")] Clear = 1000,
        [Description("1001: Cloudy")] Cloudy = 1001,
        [Description("1100: Mostly Clear")] MostlyClear = 1100,
        [Description("1101: Partly Cloudy")] PartlyCloudy = 1101,
        [Description("1102: Mostly Cloudy")] MostlyCloudy = 1102,
        [Description("2000: Fog")] Fog = 2000,
        [Description("2100: Light Fog")] LightFog = 2100,
        [Description("3000: Light Wind")] LightWind = 3000,
        [Description("3001: Wind")] Wind = 3001,
        [Description("3002: Strong Wind")] StrongWind = 3002,
        [Description("4000: Drizzle")] Drizzle = 4000,
        [Description("4001: Rain")] Rain = 4001,
        [Description("4200: Light Rain")] LightRain = 4200,
        [Description("4201: Heavy Rain")] HeavyRain = 4201,
        [Description("5000: Snow")] Snow = 5000,
        [Description("5001: Flurries")] Flurries = 5001,
        [Description("5100: Light Snow")] LightSnow = 5100,
        [Description("5101: Heavy Snow")] HeavySnow = 5101,
        [Description("6000: Freezing Drizzle")] FreezingDrizzle = 6000,
        [Description("6001: Freezing Rain")] FreezingRain = 6001,
        [Description("6200: Light Freezing Rain")] LightFreezingRain = 6200,
        [Description("6201: Heavy Freezing Rain")] HeavyFreezingRain = 6201,
        [Description("7000: Ice Pellets")] IcePellets = 7000,
        [Description("7101: Heavy Ice Pellets")] HeavyIcePellets = 7101,
        [Description("7102: Light Ice Pellets")] LightIcePellets = 7102,
        [Description("8000: Thunderstorm")] Thunderstorm = 8000
    }

    /// <summary>
    /// <para>
    /// Tomorrow Level is an enum that stores the levels that a pollen parameter can have.
    /// <br>Level enum values are:</br>
    /// <br>0: None</br>
    /// <br>1: Very low</br>
    /// <br>2: Low</br>
    /// <br>3: Medium</br>
    /// <br>4: High</br>
    /// <br>5: Very High</br>
    /// </para>
    /// </summary>
    public enum Level
    {
        [Description("0: None")] None,
        [Description("1: Very low")] Verylow,
        [Description("2: Low")] Low,
        [Description("3: Medium")] Medium,
        [Description("4: High")] High,
        [Description("5: Very High")] VeryHigh
    }

    /// <summary>
    /// <para>
    /// This class manages Tomorrow weather data.
    /// </para>
    /// </summary>
    [Serializable]
    public class TomorrowData
    {
        #region Constructors
        public TomorrowData()
        {
            temperature = 0f;
            temperatureApparent = 0f;
            dewPoint = 0f;
            humidity = 0f;
            windSpeed = 0f;
            windDirection = 0f;
            windGust = 0f;
            pressureSurfaceLevel = 0f;
            pressureSeaLevel = 0f;
            visibility = 0f;
            cloudCover = 0f;
            precipitationIntensity = 0f;
            precipitationProbability = 0f;
            particulateMatter25 = 0f;
            pollutantO3 = 0f;
            pollutantNO2 = 0f;
            pollutantCO = 0f;
            pollutantSO2 = 0f;
            precipitationType = PrecipitationType.NA;
            weatherCode = WeatherCode.Unknown;
            treeIndex = Level.None;
            grassIndex = Level.None;
            weedIndex = Level.None;

            waveSignificantHeight = -1000f;
            waveFromDirection = 0f;
            waveMeanPeriod = -1000f;
            seaSurfaceTemperature = -1000f;
            primarySwellWaveSignificantHeight = -1000f;
            primarySwellWaveMeanPeriod = -1000f;
            secondarySwellWaveSignificantHeight = -1000f;
            secondarySwellWaveMeanPeriod = -1000f;
            tertiarySwellWaveSignificantHeight = -1000f;
            tertiarySwellWaveMeanPeriod = -1000f;
            tides = -1000f;
        }
        #endregion

        #region Private Variables
        //Weather data
        [SerializeField]
        private float temperature;
        [SerializeField]
        private float temperatureApparent;
        [SerializeField]
        private float dewPoint;
        [SerializeField]
        private float humidity;
        [SerializeField]
        private float windSpeed;
        [SerializeField]
        private float windDirection;
        [SerializeField]
        private float windGust;
        [SerializeField]
        private float pressureSurfaceLevel;
        [SerializeField]
        private float pressureSeaLevel;
        [SerializeField]
        private float visibility;
        [SerializeField]
        private float cloudCover;
        [SerializeField]
        private float precipitationIntensity;
        [SerializeField]
        private float precipitationProbability;
        [SerializeField]
        private float particulateMatter25;
        [SerializeField]
        private float pollutantO3;
        [SerializeField]
        private float pollutantNO2;
        [SerializeField]
        private float pollutantCO;
        [SerializeField]
        private float pollutantSO2;
        [SerializeField]
        private PrecipitationType precipitationType;
        [SerializeField]
        private WeatherCode weatherCode;
        [SerializeField]
        private Level treeIndex;
        [SerializeField]
        private Level grassIndex;
        [SerializeField]
        private Level weedIndex;

        //Maritime data
        [SerializeField]
        private float waveSignificantHeight;
        [SerializeField]
        private float waveFromDirection;
        [SerializeField]
        private float waveMeanPeriod;
        [SerializeField]
        private float seaSurfaceTemperature;
        [SerializeField]
        private float primarySwellWaveSignificantHeight;
        [SerializeField]
        private float primarySwellWaveMeanPeriod;
        [SerializeField]
        private float secondarySwellWaveSignificantHeight;
        [SerializeField]
        private float secondarySwellWaveMeanPeriod;
        [SerializeField]
        private float tertiarySwellWaveSignificantHeight;
        [SerializeField]
        private float tertiarySwellWaveMeanPeriod;
        [SerializeField]
        private float tides;
        #endregion

        #region Public Properties
        /// <summary>
        /// Measured in Celsius.
        /// </summary>
        public float Temperature
        {
            get
            {
                return temperature;
            }

            set
            {
                temperature = value;
            }
        }

        /// <summary>
        /// <para>
        /// The temperature to which air must be cooled to become saturated with water vapor.
        /// </para>
        /// Measured in Celsius.
        /// <para>
        /// </para>
        /// </summary>
        public float DewPoint
        {
            get
            {
                return dewPoint;
            }

            set
            {
                dewPoint = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of water vapor present in the air.
        /// </para>
        /// <para>
        /// Measured in procentages (%).
        /// </para>
        /// </summary>
        public float Humidity
        {
            get
            {
                return humidity;
            }

            set
            {
                humidity = value;
            }
        }

        /// <summary>
        /// <para>
        /// The fundamental atmospheric quantity caused by air moving from high to low pressure, usually due to changes in temperature.
        /// </para>
        /// <para>
        /// Measured in m/s.
        /// </para>
        /// </summary>
        public float WindSpeed
        {
            get
            {
                return windSpeed;
            }

            set
            {
                windSpeed = value;
            }
        }

        /// <summary>
        /// <para>
        /// The direction from which it originates, measured in degrees counter-clockwise from due north.
        /// </para>
        /// </summary>
        public float WindDirection
        {
            get
            {
                return windDirection;
            }

            set
            {
                windDirection = value;
            }
        }

        /// <summary>
        /// <para>
        /// The maximum brief increase in the speed of the wind, usually less than 20 seconds.
        /// </para>
        /// <para>
        /// Measured in m/s.
        /// </para>
        /// </summary>
        public float WindGust
        {
            get
            {
                return windGust;
            }

            set
            {
                windGust = value;
            }
        }

        /// <summary>
        /// <para>
        /// The force exerted against a surface by the weight of the air above the surface (at the surface level)
        /// </para>
        /// <para>
        /// Measured in hPa.
        /// </para>
        /// </summary>
        public float PressureSurfaceLevel
        {
            get
            {
                return pressureSurfaceLevel;
            }

            set
            {
                pressureSurfaceLevel = value;
            }
        }

        /// <summary>
        /// <para>
        /// The force exerted against a surface by the weight of the air above the surface (at the mean sea level).
        /// </para>
        /// <para>
        /// Measured in hPa.
        /// </para>
        /// </summary>
        public float PressureSeaLevel
        {
            get
            {
                return pressureSeaLevel;
            }

            set
            {
                pressureSeaLevel = value;
            }
        }

        /// <summary>
        /// <para>
        /// The measure of the distance at which an object or light can be clearly discerned
        /// </para>
        /// <para>
        /// Measured in km.
        /// </para>
        /// </summary>
        public float Visibility
        {
            get
            {
                return visibility;
            }

            set
            {
                visibility = value;
            }
        }

        /// <summary>
        /// <para>
        /// The fraction of the sky obscured by clouds when observed from a particular location.
        /// </para>
        /// <para>
        /// Measured in procentages (%).
        /// </para>
        /// </summary>
        public float CloudCover
        {
            get
            {
                return cloudCover;
            }

            set
            {
                cloudCover = value;
            }
        }

        /// <summary>
        /// <para>
        /// The amount of precipitation that falls over time, covering the ground in a period of time.
        /// </para>
        /// <para>
        /// Measured in mm/hr.
        /// </para>
        /// </summary>
        public float PrecipitationIntensity
        {
            get
            {
                return precipitationIntensity;
            }

            set
            {
                precipitationIntensity = value;
            }
        }

        /// <summary>
        /// <para>
        /// The chance of precipitation that at least some minimum quantity of precipitation will occur within a specified forecast period and location.
        /// </para>
        /// <para>
        /// Measured in procentages (%).
        /// </para>
        /// </summary>
        public float PrecipitationProbability
        {
            get
            {
                return precipitationProbability;
            }

            set
            {
                precipitationProbability = value;
            }
        }

        /// <summary>
        /// <para>
        /// The PrecipitationType enum that specify the various types of precipitation which is falling to ground level.
        /// </para>
        /// </summary>
        public PrecipitationType PrecipitationType
        {
            get
            {
                return precipitationType;
            }

            set
            {
                precipitationType = value;
            }
        }

        /// <summary>
        /// <para>
        /// The WeatherCode enum that contains the most prominent weather condition.
        /// </para>
        /// </summary>
        public WeatherCode WeatherCode
        {
            get
            {
                return weatherCode;
            }

            set
            {
                weatherCode = value;
            }
        }

        /// <summary>
        /// <para>
        /// The temperature equivalent perceived by humans, caused by the combined effects of air temperature, relative humidity, and wind speed.
        /// </para>
        /// <para>
        /// Measured in Celsius.
        /// </para>
        /// </summary>
        public float TemperatureApparent
        {
            get
            {
                return temperatureApparent;
            }

            set
            {
                temperatureApparent = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of atmospheric particulate matter (PM) that have a diameter of fewer than 2.5 micrometers.
        /// </para>
        /// <para>
        /// Measured in μg/m^3 .
        /// </para>
        /// </summary>
        public float ParticulateMatter25
        {
            get
            {
                return particulateMatter25;
            }

            set
            {
                particulateMatter25 = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of surface Ozone (O3).
        /// </para>
        /// <para>
        /// Measured in ppb (parts per billion).
        /// </para>
        /// </summary>
        public float PollutantO3
        {
            get
            {
                return pollutantO3;
            }

            set
            {
                pollutantO3 = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of surface Nitrogen Dioxide (NO2).
        /// </para>
        /// <para>
        /// Measured in ppb (parts per billion).
        /// </para>
        /// </summary>
        public float PollutantNO2
        {
            get
            {
                return pollutantNO2;
            }

            set
            {
                pollutantNO2 = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of surface Carbon Monoxide (CO2).
        /// </para>
        /// <para>
        /// Measured in ppb (parts per billion).
        /// </para>
        /// </summary>
        public float PollutantCO
        {
            get
            {
                return pollutantCO;
            }

            set
            {
                pollutantCO = value;
            }
        }

        /// <summary>
        /// <para>
        /// The concentration of surface Sulfur Dioxide (SO2).
        /// </para>
        /// <para>
        /// Measured in ppb (parts per billion).
        /// </para>
        /// </summary>
        public float PollutantSO2
        {
            get
            {
                return pollutantSO2;
            }

            set
            {
                pollutantSO2 = value;
            }
        }

        /// <summary>
        /// <para>
        /// The Tomorrow index representing the extent of grains of overall tree pollen or mold spores in a cubic meter of the air.
        /// </para>
        /// </summary
        public Level TreeIndex
        {
            get
            {
                return treeIndex;
            }

            set
            {
                treeIndex = value;
            }
        }

        /// <summary>
        /// <para>
        /// The Tomorrow index representing the extent of grains of overall grass pollen or mold spores in a cubic meter of the air.
        /// </para>
        /// </summary
        public Level GrassIndex
        {
            get
            {
                return grassIndex;
            }

            set
            {
                grassIndex = value;
            }
        }

        /// <summary>
        /// <para>
        /// The Tomorrow index representing the extent of grains of overall weed pollen or mold spores in a cubic meter of the air.
        /// </para>
        /// </summary
        public Level WeedIndex
        {
            get
            {
                return weedIndex;
            }

            set
            {
                weedIndex = value;
            }
        }

        /// <summary>
        /// The height of the combined wind waves and swells.
        /// </summary>
        public float WaveSignificantHeight
        {
            get 
            { 
                return waveSignificantHeight;
            }
            set 
            { 
                waveSignificantHeight = value;
            }
        }

        /// <summary>
        /// The direction of the combined wind waves and swells are moving in.
        /// </summary>
        public float WaveFromDirection 
        {
            get 
            { 
                return waveFromDirection;
            }
            set 
            { 
                waveFromDirection = value; 
            }
        }

        /// <summary>
        /// The frequency of the combined wind waves and swells or, the space and time between each wave.
        /// </summary>
        public float WaveMeanPeriod 
        {
            get
            {
                return waveMeanPeriod;
            }
            set
            {
                waveMeanPeriod = value;
            }
        }

        /// <summary>
        /// The temperature of sea water near the surface (including the part under sea-ice, if any).
        /// </summary>
        public float SeaSurfaceTemperature
        {
            get
            {
                return seaSurfaceTemperature;
            }
            set
            {
                seaSurfaceTemperature = value;
            }
        }

        /// <summary>
        /// The height of the primary swell.
        /// </summary>
        public float PrimarySwellWaveSignificantHeight
        {
            get
            {
                return primarySwellWaveSignificantHeight;
            }
            set
            {
                primarySwellWaveSignificantHeight = value;
            }
        }

        /// <summary>
        /// The frequency of primary swells or, the space and time between each wave.
        /// </summary>
        public float PrimarySwellWaveMeanPeriod
        {
            get
            {
                return primarySwellWaveMeanPeriod;
            }
            set
            {
                primarySwellWaveMeanPeriod = value;
            }
        }

        /// <summary>
        /// The height of the secondary swells.
        /// </summary>
        public float SecondarySwellWaveSignificantHeight
        {
            get
            {
                return secondarySwellWaveSignificantHeight;
            }
            set
            {
                secondarySwellWaveSignificantHeight = value;
            }
        }

        /// <summary>
        /// The frequency of secondary swells or, the space and time between each wave.
        /// </summary>
        public float SecondarySwellWaveMeanPeriod
        {
            get
            {
                return secondarySwellWaveMeanPeriod;
            }
            set
            {
                secondarySwellWaveMeanPeriod = value;
            }
        }

        /// <summary>
        /// The height of the tertiary swells.
        /// </summary>
        public float TertiarySwellWaveSignificantHeight
        {
            get
            {
                return tertiarySwellWaveSignificantHeight;
            }
            set
            {
                tertiarySwellWaveSignificantHeight = value;
            }
        }

        /// <summary>
        /// The frequency of tertiary swells or, the space and time between each wave.
        /// </summary>
        public float TertiarySwellWaveMeanPeriod
        {
            get
            {
                return tertiarySwellWaveMeanPeriod;
            }
            set
            {
                tertiarySwellWaveMeanPeriod = value;
            }
        }

        /// <summary>
        /// Amplitude of ocean tide in meters or feet.
        /// </summary>
        public float Tides 
        {
            get
            {
                return tides;
            }
            set
            {
                tides = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Concatenates the attributes of the TomorrowWeatherData class into a single string.
        /// </summary>
        public override string ToString()
        {
            return ("\n[Tomorrow Weather Data]\n" +
                $"WeatherCode: {weatherCode}\n" +
                $"Temperature: {temperature} °C \n" +
                $"Apparent temperature: {temperatureApparent} °C \n" +
                $"Dewpoint: {dewPoint} °C \n" +
                $"Humidity: {humidity} % \n" +
                $"Visibility: {Visibility} km \n" +
                $"Cloud data: \n\tcloud cover={cloudCover} % \n" +
                $"Wind data: \n\twind speed={WindSpeed + " m/s \n\twind direction=" + windDirection + " degrees \n\twind gust=" + windGust} m/s \n" +
                $"Pressure data: \n\tpressure surface level={pressureSurfaceLevel + " hPa \n\tpressure sea level=" + pressureSeaLevel} hPa \n" +
                $"Precipitation data: \n\tprecipitation intensity={PrecipitationIntensity + " mm/hr \n\tprecipitation probability=" + PrecipitationProbability + " % \n\tprecipitation type=" + precipitationType}\n" +
                $"Pollen data: \n\ttreeIndex={treeIndex.ToString() + " \n\tgrassIndex=" + grassIndex.ToString() + " \n\tweedIndex=" + weedIndex.ToString()}\n" +
                $"Air quality data: \n\tparticulate matter (PM)={particulateMatter25 + "  μg/m^3 \n\tpollutantO3=" + pollutantO3 + " ppb \n\tpollutantNO2=" + pollutantNO2 + " ppb \n\tpollutantSO2=" + pollutantSO2 + " ppb \n\tpollutantCO=" + pollutantCO + ""} ppb \n" +
                $"Wave Height: {waveSignificantHeight} m\n" +
                $"Wave Direction: {waveFromDirection}° \n" +
                $"Wave Mean Period: {waveMeanPeriod} seconds \n" +
                $"Sea Surface Temperature: {seaSurfaceTemperature} °C \n" +
                $"Primary swell data: \n\tHeight={primarySwellWaveSignificantHeight} m \n\tMean Period={primarySwellWaveMeanPeriod} seconds \n" +
                $"Secondary swell data: \n\tHeight={secondarySwellWaveSignificantHeight} m \n\tMean Period={secondarySwellWaveMeanPeriod} seconds \n" +
                $"Tertiary swell data: \n\tHeight={tertiarySwellWaveSignificantHeight} m \n\tMean Period={tertiarySwellWaveMeanPeriod} seconds \n" +
                $"Tides: {tides} m"
                );
        }
        #endregion
    }
}