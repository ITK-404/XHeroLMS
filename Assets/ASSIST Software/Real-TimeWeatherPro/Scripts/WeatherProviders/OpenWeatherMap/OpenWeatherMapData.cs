//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
 
namespace RealTimeWeather.WeatherProvider.OpenWeather
{
    /// <summary>
    /// This class represents the structure for the weather data obtained from OpenWeatherMap server.
    /// </summary>
    [Serializable]
    public class OpenWeatherMapData
    {
        #region Constructors
        public OpenWeatherMapData()
        {
            coord = new GeographicCoordinates();
            weather = new List<WeatherDetails>();

            main = new MainWeather();
            wind = new Wind();
            clouds = new Clouds();

            sys = new SystemData();
        }
        #endregion

        #region Private Variables
        [SerializeField] private GeographicCoordinates coord;
        [SerializeField] private List<WeatherDetails> weather;
        [SerializeField] private string @base;

        [SerializeField] private MainWeather main;
        [SerializeField] private int visibility;

        [SerializeField] private Wind wind;
        [SerializeField] private Clouds clouds;

        [SerializeField] private int dt;
        [SerializeField] private SystemData sys;

        [SerializeField] private int timezone;
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private int cod;

        //This field is not a part of the structure, but is useful to know in which type of units is the data
        private Units units;
        #endregion

        #region Public Properties
        public GeographicCoordinates GeographicCoordinates
        {
            get
            {
                return coord;
            }

            set
            {
                coord = value;
            }
        }

        public List<WeatherDetails> WeatherDetailsList
        {
            get
            {
                return weather;
            }

            set
            {
                weather = value;
            }
        }

        public MainWeather MainWeather
        {
            get
            {
                return main;
            }

            set
            {
                main = value;
            }
        }

        public Wind Wind
        {
            get
            {
                return wind;
            }

            set
            {
                wind = value;
            }
        }

        public Clouds Clouds
        {
            get
            {
                return clouds;
            }

            set
            {
                clouds = value;
            }
        }

        public SystemData System
        {
            get
            {
                return sys;
            }

            set
            {
                sys = value;
            }
        }

        /// <summary>
        /// This specify which type of units are used to describe the weather parameters.
        /// </summary>
        public Units Units
        {
            get
            {
                return units;
            }

            set
            {
                units = value;
            }
        }

        /// <summary>
        /// This is an internal parameter. Specify which source was used for obtaining the data.
        /// </summary>
        public string Base
        {
            get
            {
                return @base;
            }

            set
            {
                @base = value;
            }
        }

        /// <summary>
        /// Measured distance at which an object or light can be clearly discerned
        /// </summary>
        public int Visibility
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
        /// Data receiving time, unix, UTC
        /// </summary>
        public int UnixTimestamp
        {
            get
            {
                return dt;
            }

            set
            {
                dt = value;
            }
        }

        /// <summary>
        /// Shift in seconds from UTC
        /// </summary>
        public int TimeZone
        {
            get
            {
                return timezone;
            }

            set
            {
                timezone = value;
            }
        }

        /// <summary>
        /// Check JSON file with all cities IDs from OpenWeatherMap website. ("city.list.json.gz" => http://bulk.openweathermap.org/sample/)
        /// </summary>
        public int CityID
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        /// <summary>
        /// This is HTTP response code of the request
        /// </summary>
        public int HTTPCode
        {
            get
            {
                return cod;
            }

            set
            {
                cod = value;
            }
        }

        /// <summary>
        /// The name of the city interrogated.
        /// </summary>
        public string CityName
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            string weatherProp = string.Empty;

            for (int i = 0; i < weather.Count; i++)
            {
                weatherProp += "Weather=> Weather code ID: " + weather[i].ID + " | state: " + weather[i].Main + " | description: " + weather[i].Description + " | icon: " + weather[i].Icon + "\n";
            }


            return "\n====================> OpenWeatherMap Data <============================\n" +
                $"Location: {name + ", " + sys.Country + ": latitude->" + coord.Latitude + ", longitude->" + coord.Longitude}\n" +
                weatherProp +
                $"Main=> Temperature: {main.Temperature}" + $" | Feels like: {main.FeelsLike}" + $" | Min: {main.MinimumTemperature}" + $" | Max: {main.MaximalTemperature}" +
                $" | Pressure: {main.Pressure}" + $" | Humidity: {main.Humidity}\n" +
                $"Wind=> Speed: {wind.Speed}" + $" | Degrees: {wind.Deg}\n" +
                $"System=> Type: {sys.Type}" + $" | ID: {sys.ID}" + $" | Sunrise: {sys.Sunrise}" + $" | Sunset: {sys.Sunset}\n" +
                $"Unix timestamp: {dt}\n" +
                $"Cloudiness: {clouds.Cloudiness}\n" +
                $"Visibility: {visibility}\n" +
                $"Base: {@base}\n" +
                $"Timezone: {timezone}\n" +
                $"City ID: {id}\n" +
                $"HTTP Code: {cod}\n";
        }
        #endregion
    }

    public enum RequestMode
    {
        CityName,
        CityID,
        GeographicCoordinates,
        ZipCode
    }

    public enum Units
    {
        Standard,
        Metric,
        Imperial
    }

    public enum Language
    {
        [Description("en")] English,
        [Description("af")] Afrikaans,
        [Description("al")] Albanian,
        [Description("ar")] Arabic,
        [Description("az")] Azerbaijani,
        [Description("bg")] Bulgarian,
        [Description("ca")] Catalan,
        [Description("cz")] Czech,
        [Description("da")] Danish,
        [Description("de")] German,
        [Description("el")] Greek,
        [Description("eu")] Basque,
        [Description("fa")] Persian,
        [Description("fi")] Finnish,
        [Description("fr")] French,
        [Description("gl")] Galician,
        [Description("he")] Hebrew,
        [Description("hi")] Hindi,
        [Description("hr")] Croatian,
        [Description("hu")] Hungarian,
        [Description("id")] Indonesian,
        [Description("it")] Italian,
        [Description("ja")] Japanese,
        [Description("kr")] Korean,
        [Description("la")] Latvian,
        [Description("lt")] Lithuanian,
        [Description("mk")] Macedonian,
        [Description("no")] Norwegian,
        [Description("nl")] Dutch,
        [Description("pl")] Polish,
        [Description("pt")] Portuguese,
        [Description("pt_br")] Português_Brasil,
        [Description("ro")] Romanian,
        [Description("ru")] Russian,
        [Description("se")] Swedish,
        [Description("sk")] Slovak,
        [Description("sl")] Slovenian,
        [Description("es")] Spanish,
        [Description("sr")] Serbian,
        [Description("th")] Thai,
        [Description("tr")] Turkish,
        [Description("ua")] Ukrainian,
        [Description("vi")] Vietnamese,
        [Description("zh_cn")] Chinese_Simplified,
        [Description("zh_tw")] Chinese_Traditional,
        [Description("zu")] Zulu,
    }

    public enum WeatherState
    {
        //Thunderstorm
        ThunderstormWithLightRain = 200,
        ThunderstormWithRain = 201,
        ThunderstormWithHeavyRain = 202,
        LightThunderstorm = 210,
        Thunderstorm = 211,
        HeavyThunderstorm = 212,
        RaggedThunderstorm = 221,
        ThunderstormWithLightDrizzle = 230,
        ThunderstormWithDrizzle = 231,
        ThunderstormWithHeavyDrizzle = 232,

        //Drizzle
        LightIntensityDrizzle = 300,
        Drizzle = 301,
        HeavyIntensityDrizzle = 302,
        LightIntensityDrizzleRain = 310,
        DrizzleRain = 311,
        HeavyIntensityDrizzleRain = 312,
        ShowerRainAndDrizzle = 313,
        HeavyShowerRainAndDrizzle = 314,
        ShowerDrizzle = 321,

        //Rain
        LightRain = 500,
        ModerateRain = 501,
        HeavyIntensityRain = 502,
        VeryHeavyRain = 503,
        ExtremeRain = 504,
        FreezingRain = 511,
        LightIntensityShowerRain = 520,
        ShowerRain = 521,
        HeavyIntensityShowerRain = 522,
        RaggedShowerRain = 531,

        //Snow
        LightSnow = 600,
        Snow = 601,
        HeavySnow = 602,
        Sleet = 611,
        LightShowerSleet = 612,
        ShowerSleet = 613,
        LightRainAndSnow = 615,
        RainAndSnow = 616,
        LightShowerSnow = 620,
        ShowerSnow = 621,
        HeavyShowerSnow = 622,

        //Atmosphere
        Mist = 701,
        Smoke = 711,
        Haze = 721,
        Sand_DustWhirls = 731,
        Fog = 741,
        Sand = 751,
        Dust = 761,
        VolcanicAsh = 762,
        Squalls = 771,
        Tornado = 781,

        //Clear
        Clear = 800,

        //Clouds
        FewClouds = 801,
        ScaterredClouds = 802,
        BrokenClouds = 803,
        OvercastClouds = 804
    }
}
