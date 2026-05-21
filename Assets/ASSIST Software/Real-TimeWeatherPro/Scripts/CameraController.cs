//
// Copyright(c) 2021 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather
{
    /// <summary>
    /// This class controls the camera movements: rotation and position.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Private Const Variables
        private const string kMouseXstr = "Mouse X";
        private const string kMouseYstr = "Mouse Y";

        private const int kMinAngle = -30;
        private const int kMaxAngle = 50;
        private const int kZeroValue = 0;
        #endregion

        #region Public Variables
        public float _cameraRotationSpeed = 5f;
        #endregion

        #region Private Variables
        [SerializeField] private float _normalMovementSpeed;
        [SerializeField] private float _speedMultiplier;

        private float _mouseX;
        private float _mouseY;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            var camera= GetComponent<Camera>();
            _mouseX= camera.transform.rotation.eulerAngles.y;
            _mouseY= camera.transform.rotation.eulerAngles.x;

            if(_mouseY > 180.0f)
            {
                _mouseY -= 360.0f;
            }
        }

        private void Update()
        {
            RotateCamera();
            MoveCamera();
        }

        #endregion

        #region Private Methods

        private void RotateCamera()
        {
            if (Input.GetMouseButton(1))
            {
                var mouseX = Input.GetAxis(kMouseXstr);
                var mouseY = Input.GetAxis(kMouseYstr);

                if (mouseX < -1.0f || mouseX > 1.0f || mouseY < -1.0f || mouseY > 1.0f)
                {
                    return;
                }

                _mouseX += mouseX * _cameraRotationSpeed;
                _mouseY -= mouseY * _cameraRotationSpeed;
                _mouseY = Mathf.Clamp(_mouseY, kMinAngle, kMaxAngle);
                transform.rotation = Quaternion.Euler(_mouseY, _mouseX, kZeroValue);
            }
        }

        private void MoveCamera()
        {
            var movementSpeed = Input.GetKey(KeyCode.RightShift) ? _normalMovementSpeed : _normalMovementSpeed * _speedMultiplier;
            var lateralMovement = 0;
            if(Input.GetKey(KeyCode.D))
            {
                lateralMovement += 1;
            }
            if(Input.GetKey(KeyCode.A))
            {
                lateralMovement -= 1;
            }

            var forwardMovement = 0;
            if(Input.GetKey(KeyCode.W)) 
            { 
                forwardMovement += 1; 
            }
            if(Input.GetKey(KeyCode.S)) 
            { 
                forwardMovement -= 1; 
            }

            var heightMovement = 0;
            if(Input.GetKey(KeyCode.Q))
            {
                heightMovement -= 1;
            }
            if(Input.GetKey(KeyCode.E))
            {
                heightMovement += 1;
            }

            var move = (forwardMovement * transform.forward + lateralMovement * transform.right) * movementSpeed * Time.unscaledDeltaTime;
            var cameraHeightTransition = heightMovement * movementSpeed * Time.unscaledDeltaTime;
            var transition = new Vector3(move.x, cameraHeightTransition + move.y, move.z);
            transform.Translate(transition, Space.World);
        }

        #endregion
    }
}