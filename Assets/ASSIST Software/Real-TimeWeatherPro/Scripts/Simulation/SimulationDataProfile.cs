using System;
using UnityEngine;
using System.Collections.Generic;
using RealTimeWeather.Classes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using RealTimeWeather;

namespace RealTimeWeather.Simulation
{
    [CreateAssetMenu(fileName = "SimulationsData", menuName = "Real-Time Weather/SimulationsData", order = 2)]
    public class SimulationDataProfile : ScriptableObject
    {
#if UNITY_EDITOR
        public string SimulationName;
        public float SimulationSpeed;
        public Localization Location;
        public DateTime DateTime;
        public List<int> SimulationDays;
#endif
    }
}
