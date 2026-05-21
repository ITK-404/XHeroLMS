//
// Copyright(c) 2020 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;


namespace RealTimeWeather.UI
{
    /// <summary>
    /// This class allows to drag UI Elements
    /// </summary>
    public class MovableUI : MonoBehaviour
    {
        private float offsetX, offsetY;

        /// <summary>
        /// This method stores screen postion.
        /// </summary>
        public void BeginDrag()
        {
            offsetX = transform.position.x - Input.mousePosition.x;
            offsetY = transform.position.y - Input.mousePosition.y;
        }

        /// <summary>
        /// This function moves the current element depending on how much the mouse has moved.
        /// </summary>
        public void OnDrag()
        {
            transform.position = new Vector3(offsetX + Input.mousePosition.x, offsetY + Input.mousePosition.y);
        }

    }
}
