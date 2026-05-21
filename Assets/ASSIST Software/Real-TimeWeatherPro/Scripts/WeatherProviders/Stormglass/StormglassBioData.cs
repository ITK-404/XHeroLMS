// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using System.Collections.Generic;

namespace RealTimeWeather.WeatherProvider.Stormglass
{
    /// <summary>
    /// Data about water composition
    /// </summary>
    [Serializable]
    public class StormglassBioData
    {
        public List<StormglassBioParams> hours;
        public StormglassRequestData meta;
    }

    [Serializable]
    public class StormglassBioParams
    {
        public string time; // Timestamp in UTC

        public StormglassProviders chlorophyll; // Mass concentration of chlorophyll a in sea water
        public StormglassProviders iron; // Mole concentration of dissolved iron in sea water
        public StormglassProviders nitrate; // Mole concentration of nitrate in sea water
        public StormglassProviders phyto; // Net primary production of biomass expressed as carbon per unit volume in sea water
        public StormglassProviders oxygen; // Mole concentration of dissolved molecular oxygen in sea water
        public StormglassProviders ph; // Sea water ph reported on total scale
        public StormglassProviders phytoplankton; // Mole concentration of phytoplankton expressed as carbon in sea water
        public StormglassProviders phosphate; // Mole concentration of phosphate in sea water
        public StormglassProviders silicate; // Mole concentration of silicate in sea water
        public StormglassProviders salinity; // Sea water salinity given in per mille

        public StormglassProviders soilMoisture; // Volumetric soil moisture content at 0 to 10 cm below surface
        public StormglassProviders soilTemperature; // Soil temperature at 0 to 10 cm below surface
    }
}
