//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using RealTimeWeather.Enums;
using RealTimeWeather.WeatherProvider;
using RealTimeWeather.WeatherProvider.OpenWeather;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RealTimeWeather.Data
{
    public class OpenWeatherRequest
    {
        private const string kServer = "https://api.openweathermap.org/data/2.5/weather?";
        private const string kOneCallServer = "https://api.openweathermap.org/data/2.5/onecall?";
        private const string kInputCityNameIdentifier = "q=";
        private const string kInputLatitudeIdentifier = "lat=";
        private const string kInputLongitudeIdentifier = "&lon=";
        private const string kInputExcludeIdentifier = "&exclude=current,minutely,alerts";
        private const string kInputAppIDIdentifier = "&appid=";
        private const string kInputUnitsIdentifier = "&units=";
        private const string kInputLanguageIdentifier = "&lang=";
        private const string kEnglishLanguage = "en";

        public OpenWeatherData OpenWeatherSimulationData;
        public event Action<ExceptionType, string> RaiseException;

        private string _requestLink = string.Empty;
        private string _requestOneCallAPILink = string.Empty;
        private WeatherAPIRequest _weatherAPIRequest = new WeatherAPIRequest();

        public OpenWeatherRequest(OpenWeatherData openWeatherSimulationData)
        {
            OpenWeatherSimulationData = openWeatherSimulationData;
        }

        public async Task<OpenWeatherMapData> RequestOpenWeatherData()
        {
            ConstructRequestLink();

            _weatherAPIRequest.onErrorRaised += SendError;
            var response = await _weatherAPIRequest.GetRequestAsync(_requestLink);

            if (response == string.Empty) return null;

            OpenWeatherMapData resultedData = JsonUtility.FromJson<OpenWeatherMapData>(response);
            resultedData.Units = Units.Metric;
            _weatherAPIRequest.onErrorRaised -= SendError;

            return resultedData;
        }

        public async Task<OpenWeatherOneCallAPIMapData> RequestOpenWeatherPeriodData()
        {
            ConstructRequestLinkWithWeatherPeriodParameters();
            _weatherAPIRequest.onErrorRaised += SendError;
            var response = await _weatherAPIRequest.GetRequestAsync(_requestOneCallAPILink);

            if (response == string.Empty) { return null; }

            OpenWeatherOneCallAPIMapData resultedData = JsonUtility.FromJson<OpenWeatherOneCallAPIMapData>(response);
            resultedData.Units = Units.Metric;
            _weatherAPIRequest.onErrorRaised -= SendError;
            return resultedData;
        }

        private void ConstructRequestLinkWithWeatherPeriodParameters()
        {
            _requestOneCallAPILink = kOneCallServer + kInputLatitudeIdentifier + OpenWeatherSimulationData.Latitude + kInputLongitudeIdentifier + OpenWeatherSimulationData.Longitude + kInputExcludeIdentifier;
            _requestOneCallAPILink += ",daily";

            _requestOneCallAPILink += kInputAppIDIdentifier + OpenWeatherSimulationData.ApiKey;
            _requestOneCallAPILink += kInputUnitsIdentifier + Units.Metric.ToString().ToLower();
            _requestOneCallAPILink += kInputLanguageIdentifier + kEnglishLanguage;
        }

        private void SendError(ExceptionType exceptionType, string errorMessage)
        {
            RaiseException?.Invoke(exceptionType, errorMessage);
        }

        private void ConstructRequestLink()
        {
            if (OpenWeatherSimulationData.CityName != string.Empty || OpenWeatherSimulationData.CountryCode != string.Empty)
            {
                _requestLink = CityNameRequest();
            }
            else
            {
                _requestLink = GeographicCoordinatesRequest();
            }

            CompleteRequestLinkWithParameters();
        }

        private string CityNameRequest()
        {
            string cityNamePart = OpenWeatherSimulationData.CityName != string.Empty ? OpenWeatherSimulationData.CityName : string.Empty;
            string countryCodePart = OpenWeatherSimulationData.CountryCode != string.Empty ? "," + OpenWeatherSimulationData.CountryCode : string.Empty;

            return kServer + kInputCityNameIdentifier + cityNamePart + countryCodePart;
        }

        private string GeographicCoordinatesRequest()
        {
            return kServer + kInputLatitudeIdentifier + OpenWeatherSimulationData.Latitude + kInputLongitudeIdentifier + OpenWeatherSimulationData.Longitude;
        }

        private void CompleteRequestLinkWithParameters()
        {
            _requestLink += kInputAppIDIdentifier + OpenWeatherSimulationData.ApiKey;
            _requestLink += kInputUnitsIdentifier + Units.Metric.ToString().ToLower();
            _requestLink += kInputLanguageIdentifier + kEnglishLanguage;
        }
    }
}