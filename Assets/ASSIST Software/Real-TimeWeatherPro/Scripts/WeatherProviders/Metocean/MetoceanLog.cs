
namespace RealTimeWeather.WeatherProvider.Metocean
{
    public class MetoceanLog
    {
        #region Constants
        private const string kMetoceanStr = "Metocean service exception: ";
        private const string kAirTemperature = "Air Temperature";
        private const string kPrecipitationRate = "Precipitation Rate";
        private const string kAirPressureAtSea = "Air Pressure at Sea";
        private const string kWindDirection = "Wind Direction";
        private const string kAirHumidity = "Air Humidity";
        private const string kAirVisibility = "Air Visibility";
        private const string kWindSpeed = "Wind Speed";
        private const string kWaveHeight = "Wave Height";
        private const string kWavePeriodPeak = "Wave Period Peak";
        private const string kCloudCover = "Cloud Cover";
        private const string kRadiationLongwave = "Radiation Flux Downward Longwave";
        private const string kRadiationShortwave = "Radiation Flux Downward Shortwave";
        private const string kErrorStr = " data could not be retrieved or it's a placeholder value.";
        private const string kLandErrorStr = " data could not be retrieved because it cannot be found on ice or land.";
        private const string kInvalidStr = " data was found but it was outside the representable range of the requested variable's numeric domain.";
        #endregion

        #region Public Methods
        /// <summary>
        /// Logs every API request error
        /// </summary>
        /// <param name="metoceanData">Weather data from the last Metocean API Request</param>
        public static void LogApiErrors(MetoceanData metoceanData)
        {
            foreach (int error in metoceanData.Variables.AirTemperature.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kAirTemperature + kErrorStr);
                }
                if(error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kAirTemperature + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.PrecipitationRate.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kPrecipitationRate + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kPrecipitationRate + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.AirPressureAtSea.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kAirPressureAtSea + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kAirPressureAtSea + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.AirHumidity.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kAirHumidity + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kAirHumidity + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.AirVisibility.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kAirVisibility + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kAirVisibility + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.WindDirection.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kWindDirection + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kWindDirection + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.WindSpeed.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kWindSpeed + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kWindSpeed + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.WaveHeight.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kWaveHeight + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.Land || error == metoceanData.NoDataReasons.Ice)
                {
                    LogFile.Write(kMetoceanStr + kWaveHeight + kLandErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kWaveHeight + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.WavePeriodPeak.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kWavePeriodPeak + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.Land || error == metoceanData.NoDataReasons.Ice)
                {
                    LogFile.Write(kMetoceanStr + kWavePeriodPeak + kLandErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kWavePeriodPeak + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.CloudCover.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kCloudCover + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kCloudCover + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.RadiationFluxLongwave.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kRadiationLongwave + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kRadiationLongwave + kInvalidStr);
                }
            }

            foreach (int error in metoceanData.Variables.RadiationFluxShortwave.NoData)
            {
                if (error == metoceanData.NoDataReasons.Fill || error == metoceanData.NoDataReasons.Gap || error == metoceanData.NoDataReasons.ErrorInternal)
                {
                    LogFile.Write(kMetoceanStr + kRadiationShortwave + kErrorStr);
                }
                if (error == metoceanData.NoDataReasons.InvalidHigh || error == metoceanData.NoDataReasons.InvalidLow)
                {
                    LogFile.Write(kMetoceanStr + kRadiationShortwave + kInvalidStr);
                }
            }
        }
        #endregion
    }
}
