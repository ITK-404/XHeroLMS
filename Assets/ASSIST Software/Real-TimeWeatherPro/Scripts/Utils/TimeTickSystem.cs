//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System;
using UnityEngine;

namespace RealTimeWeather
{
    public class TimeTickSystem : MonoBehaviour
    {
        #region Public Events
        public event EventHandler<OnTickEventArgs> OnTick;
        #endregion

        #region Private Variables
        private float amountOfTimePerTick;
        private float tickTimer;
        private bool timerOn = false;
        private int currentTick;
        #endregion

        #region Public Properties
        #endregion

        #region Unity Methods
        private void Awake()
        {
            currentTick = 0;
        }

        private void Update()
        {
            if(timerOn == false)
            {
                return;
            }

            tickTimer += Time.deltaTime;
            if (tickTimer >= amountOfTimePerTick)
            {
                tickTimer -= amountOfTimePerTick;
                currentTick++;
                
                // Send event on tick
                if (null != OnTick)
                {
                    OnTick(this, new OnTickEventArgs {tick = currentTick});
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// The amount of time until a new tick is triggered
        /// </summary>
        /// <param name="amountOfTimePerTick">The amount of time per tick</param>
        public void StartTimer(float amountOfTimePerTick)
        {
            this.amountOfTimePerTick = amountOfTimePerTick;
            timerOn = true;
        }

        /// <summary>
        /// Stops the timer and resets the timer tick
        /// </summary>
        public void StopTimer()
        {
            tickTimer = 0.0f;
            timerOn = false;
        }
        #endregion
    }

    public class OnTickEventArgs : EventArgs
    {
        public int tick;
    }
}
