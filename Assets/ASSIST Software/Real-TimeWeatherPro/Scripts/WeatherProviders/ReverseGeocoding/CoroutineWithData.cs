//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using System.Collections;
using UnityEngine;

namespace RealTimeWeather.WeatherProvider
{
    /// <summary>
    /// This class performs the functionality of a coroutine that keeps the result in a private variable.
    /// </summary>
    public class CoroutineWithData
    {
        #region Constructors
        public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
        {
            _target = target;
            Coroutine = owner.StartCoroutine(RunCoroutine());
        }
        #endregion

        #region Private Variables
        private object _result;
        private IEnumerator _target;
        #endregion

        #region Public Properties
        public Coroutine Coroutine
        {
            get;
            private set;
        }

        public object Result
        {
            get
            {
                return _result;
            }

            set
            {
                _result = value;
            }
        }
        #endregion

        #region Private Methods
        private IEnumerator RunCoroutine()
        {
            while (_target.MoveNext())
            {
                _result = _target.Current;
                yield return _result;
            }
        }
        #endregion
    }
}