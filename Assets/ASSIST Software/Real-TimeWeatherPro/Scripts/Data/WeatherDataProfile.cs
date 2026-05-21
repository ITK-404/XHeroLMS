//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;

namespace RealTimeWeather.Data
{
    [Serializable]
    public struct WeatherDataParameter
    {
        // Clouds
        public bool setAuto;
        [Range(0f, 1f)] public float cloudAltAmt;
        [Range(0f, 1f)] public float cloudAltoStratusAmt;
        [Range(0f, 1f)] public float cloudCirrusAmt;
        [Range(0f, 1f)] public float cloudCumulusAmt;
        [Range(0f, 20f)] public float cloudScale;
        [Range(0f, 1f)] public float cloudSpeed;
        [Range(0f, 1f)] public float overcastAmt;
        [Range(0f, 1f)] public float overcastDarkeningAmt;
        [Range(0f, 1f)] public float lightning;
        [Range(0f, 1f)] public float rainbow;
        [Range(0f, 359f)] public float lightningDir;
        [Range(0f, 180f)] public float lightningRange;
        public bool setFogAuto;

        public WeatherDataParameter(WeatherDataParameter other)
        {
            setAuto = other.setAuto;
            cloudAltAmt = other.cloudAltAmt;
            cloudAltoStratusAmt = other.cloudAltoStratusAmt;
            cloudCirrusAmt = other.cloudCirrusAmt;
            cloudCumulusAmt = other.cloudCumulusAmt;
            cloudScale = other.cloudScale;
            cloudSpeed = other.cloudSpeed;
            overcastAmt = other.overcastAmt;
            cloudScale = other.cloudScale;
            overcastDarkeningAmt = other.overcastDarkeningAmt;
            lightning = other.lightning;
            rainbow = other.rainbow;
            lightningDir = other.lightningDir;
            lightningRange = other.lightningRange;
            setFogAuto = other.setFogAuto;
        }
    }

    [CreateAssetMenu(fileName = "WeatherDataProfile",
        menuName = "Real-Time Weather/WeatherData/Profile", order = 1)]
    public class WeatherDataProfile : ScriptableObject
    {
        [SerializeField] public WeatherDataParameter Parameter;

        public void UpdateParameter(WeatherDataParameter other)
        {
            Parameter = new WeatherDataParameter(other);
        }

        public WeatherDataProfile()
        {
            Parameter = new WeatherDataParameter();
        }
    }
}