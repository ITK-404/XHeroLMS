using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonControllerPro : MonoBehaviour
    {

        [Header("Movement")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float acceleration = 18f;
        public float rotationSpeed = 12f;
        public bool rotateOnlyWhenMoving = true;


        [Header("Jump / Gravity")]
        public float jumpHeight = 2.0f;
        public float gravity = -20f;
        public float groundSnap = 4f;
        public float coyoteTime = 0.1f;


        [Header("Camera")]
        public Camera sceneCamera;
        public bool holdRightMouseToOrbit = true;
        public float mouseSensitivity = 210f;
        public bool invertY = false;
        public float minPitch = -30f;
        public float maxPitch = 70f;

        [Header("Camera Boom")]
        public float cameraHeight = 1.6f;
        public float cameraDistance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 7.5f;
        public float zoomSpeed = 4f;
        public float camFollowDamp = 18f;

        [Header("Camera Collision")]
        public LayerMask cameraCollisionMask = ~0;
        public float cameraSphereRadius = 0.2f;
        public float cameraWallPadding = 0.1f;


        private CharacterController controller;
        private Transform camPivot;
        private float yaw, pitch;
        private Vector3 velocity;
        private float lastGroundedTime;
        private Vector3 planarVel;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (!sceneCamera) sceneCamera = Camera.main;
            if (!sceneCamera)
            {
                Debug.LogError("ThirdPersonController: No Camera found. Assign a Camera.");
                enabled = false; return;
            }


            camPivot = new GameObject("CameraPivot").transform;
            camPivot.SetPositionAndRotation(transform.position + new Vector3(0, cameraHeight, 0), Quaternion.identity);


            sceneCamera.transform.SetParent(camPivot, worldPositionStays: true);


            Vector3 toCam = (sceneCamera.transform.position - camPivot.position);
            if (toCam.sqrMagnitude < 0.001f)
            {
                yaw = transform.eulerAngles.y;
                pitch = 15f;
                sceneCamera.transform.position = camPivot.position - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * cameraDistance;
                sceneCamera.transform.rotation = Quaternion.LookRotation(camPivot.position - sceneCamera.transform.position, Vector3.up);
            }
            else
            {
                Quaternion look = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                var e = look.eulerAngles;
                yaw = e.y;
                pitch = NormalizeSignedAngle(e.x);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;


            HandleCameraInput(dt);


            HandleMovement(dt);


            HandleJumpAndGravity(dt);
        }

        void LateUpdate()
        {

            HandleCameraRig(Time.deltaTime);
        }


        void HandleCameraInput(float dt)
        {

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                cameraDistance -= scroll * zoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, minDistance, maxDistance);
            }

            bool orbiting = !holdRightMouseToOrbit || Input.GetMouseButton(1);
            if (!orbiting) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity * dt;
            pitch += (invertY ? 1f : -1f) * mouseY * mouseSensitivity * dt;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            yaw = WrapAngle360(yaw);
        }


        void HandleMovement(float dt)
        {

            Vector3 camFwd = YawToForward(yaw);
            Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);


            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v);
            input = Vector3.ClampMagnitude(input, 1f);


            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 targetPlanar = (camFwd * input.z + camRight * input.x) * targetSpeed;


            planarVel = Vector3.MoveTowards(planarVel, targetPlanar, acceleration * dt);


            controller.Move(planarVel * dt);


            if (planarVel.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(planarVel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);
            }
            else if (!rotateOnlyWhenMoving)
            {

                Quaternion toCam = Quaternion.Euler(0f, yaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, toCam, rotationSpeed * dt);
            }
        }


        void HandleJumpAndGravity(float dt)
        {
            bool grounded = controller.isGrounded;

            if (grounded)
            {
                lastGroundedTime = Time.time;


                if (velocity.y < 0f) velocity.y = -groundSnap;

                if (Input.GetButtonDown("Jump"))
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {

                if ((Time.time - lastGroundedTime) <= coyoteTime && Input.GetButtonDown("Jump"))
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }


            velocity.y += gravity * dt;
            controller.Move(velocity * dt);
        }


        void HandleCameraRig(float dt)
        {

            Vector3 targetPivotPos = transform.position + new Vector3(0f, cameraHeight, 0f);
            camPivot.position = Vector3.Lerp(camPivot.position, targetPivotPos, 1f - Mathf.Exp(-camFollowDamp * dt));


            Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = camPivot.position - (camRot * Vector3.forward) * cameraDistance;


            Vector3 toCam = desiredPos - camPivot.position;
            float dist = toCam.magnitude;

            Vector3 finalPos = desiredPos;
            if (dist > 0.01f && Physics.SphereCast(camPivot.position, cameraSphereRadius, toCam.normalized, out RaycastHit hit, dist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = camPivot.position + toCam.normalized * Mathf.Max(hit.distance - cameraWallPadding, 0.1f);
            }


            sceneCamera.transform.position = finalPos;
            sceneCamera.transform.rotation = Quaternion.LookRotation(camPivot.position - sceneCamera.transform.position, Vector3.up);
        }


        static Vector3 YawToForward(float yawDeg)
        {
            float r = yawDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)).normalized;
        }

        static float NormalizeSignedAngle(float angle)
        {

            float a = Mathf.Repeat(angle + 180f, 360f) - 180f;
            return a;
        }

        static float WrapAngle360(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }
    }


}
