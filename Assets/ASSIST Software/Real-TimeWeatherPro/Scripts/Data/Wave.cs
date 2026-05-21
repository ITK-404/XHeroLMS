// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather.Data
{
    public class Wave
    {
        #region Constructor
        public Wave()
        {
            _waveHeight = -1f;
            _wavePeriodPeak = -1f;
            _waveDirection = Vector2.zero;
        }
        
        public Wave(float waveHeight, float wavePeriodPeak, Vector2 waveDirection)
        {
            _waveHeight = waveHeight;
            _wavePeriodPeak = wavePeriodPeak;
            _waveDirection = waveDirection;
        }
        #endregion

        #region Private Variables
        private float _waveHeight;
        private float _wavePeriodPeak;
        private Vector2 _waveDirection;
        #endregion

        #region Public Properties

        /// <summary>
        /// The vertical distance between the crest(peak) and the through of a wave.
        /// Measured in meters
        /// </summary>
        public float Height 
        {
            get { return _waveHeight; }
            set { _waveHeight = value; }
        }

        /// <summary>
        /// The time it takes for two successive wave crests to reach a fixed point.
        /// Measured in seconds
        /// </summary>
        public float PeriodPeak 
        {
            get { return _wavePeriodPeak; }
            set { _wavePeriodPeak = value; }
        }

        /// <summary>
        /// The direction of waves in degrees, with 0 degrees being North
        /// </summary>
        public Vector2 Direction
        {
            get { return _waveDirection; }
            set { _waveDirection = value; }
        }
        #endregion
    }
}