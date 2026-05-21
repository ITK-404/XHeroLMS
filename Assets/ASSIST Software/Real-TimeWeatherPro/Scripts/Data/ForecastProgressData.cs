//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;

namespace RealTimeWeather.Data
{
    [Serializable]
    public class ForecastProgressData
    {
        public string Name;
        public DateTime ProgressTime;
        public int IntervalIndex;

        public ForecastProgressData(string simulationName, DateTime timelapseProgressTime, int currentIntervalIndex)
        {
            Name = simulationName;
            ProgressTime = timelapseProgressTime;
            IntervalIndex = currentIntervalIndex;
        }
    }
}
