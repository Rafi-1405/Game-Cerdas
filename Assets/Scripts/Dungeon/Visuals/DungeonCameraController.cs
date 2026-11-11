using DungeonGenerator.Core;
using DungeonGenerator.UI;
using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Multi-mode camera controller supporting Isometric Top-Down, Third-Person, Overview,
    /// and Unity Editor-style Scene View free navigation (Flythrough, Look, Pan, Orbit, Zoom).
    /// </summary>
    public class DungeonCameraController : MonoBehaviour
    {
        #region CONFIG
        public static class CAMERA_CONFIG
        {
            // Isometric Top-Down parameters
            public static readonly Vector3 ISOMETRIC_OFFSET = new Vector3(0.0f, 16.0f, -12.0f);
            public static readonly Vector3 ISOMETRIC_ROTATION = new Vector3(55.0f, 0.0f, 0.0f);

            // Third-Person parameters
            public static readonly Vector3 THIRD_PERSON_OFFSET = new Vector3(0.0f, 3.2f, -4.5f);
            public static readonly Vector3 THIRD_PERSON_ROTATION = new Vector3(20.0f, 0.0f, 0.0f);

            // Overview parameters
            public const float OVERVIEW_PITCH = 90.0f;
            public const float OVERVIEW_HEIGHT_MULTIPLIER = 1.35f;

            // Damping and zoom
            public const float FOLLOW_DAMPING = 6.0f;
            public const float ZOOM_SPEED = 4.0f;
            public const float MIN_ZOOM = 0.5f;
            public const float MAX_ZOOM = 3.0f;

            // Scene View Free Navigation parameters
            public const float FLY_NORMAL_SPEED = 14.0f;
            public const float FLY_FAST_SPEED = 32.0f;
            public const float FLY_MIN_SPEED = 2.0f;
            public const float FLY_MAX_SPEED = 60.0f;
            public const float MOUSE_LOOK_SENSITIVITY = 0.15f;
            public const float MOUSE_PAN_SENSITIVITY = 0.025f;
            public const float MOUSE_ORBIT_SENSITIVITY = 0.20f;
            public const float SCROLL_ZOOM_STEP = 5.0f;
            public const float PITCH_MIN = -88.0f;
            public const float PITCH_MAX = 88.0f;
            public const float FOCUS_DISTANCE = 14.0f;
            public const float FOCUS_PITCH = 45.0f;
        }
        #endregion

        [SerializeField] private CameraViewMode _currentMode = CameraViewMode.IsometricTopDown;
        [SerializeField] private Transform _target;

        private float _currentZoom = 1.0f;
        private Vector3 _overviewPosition = Vector3.up * 40.0f;

        // Scene View navigation state
        private float _pitch = 55.0f;
        private float _yaw = 0.0f;
        private float _currentFlySpeed = CAMERA_CONFIG.FLY_NORMAL_SPEED;
        private Vector3 _orbitPivot = Vector3.zero;
        private float _orbitDistance = CAMERA_CONFIG.FOCUS_DISTANCE;
        private bool _isCursorLocked = false;

        public CameraViewMode CurrentMode => _currentMode;
        public bool IsFreeFlyActive => _currentMode == CameraViewMode.FreeFlyEditor || DungeonInputReader.IsRightMouseButtonHeld();

        public void SetTarget(Transform target)
        {
            _target = target;
            if (_target != null)
            {
                _orbitPivot = _target.position + (Vector3.up * 1.0f);
            }
        }

        public void SetOverviewPosition(Vector3 center, float dungeonSpan)
        {
            float height = Mathf.Max(25.0f, dungeonSpan * CAMERA_CONFIG.OVERVIEW_HEIGHT_MULTIPLIER);
            _overviewPosition = new Vector3(center.x, height, center.z);
        }

        public void CycleCameraMode()
        {
            int next = ((int)_currentMode + 1) % 4;
            SetCameraMode((CameraViewMode)next);
        }

        public void SetCameraMode(CameraViewMode mode)
        {
            _currentMode = mode;
            if (_currentMode == CameraViewMode.FreeFlyEditor)
            {
                SyncOrientationFromTransform();
            }
        }

        /// <summary>
        /// Smoothly frames and focuses the camera on the target entity (F key).
        /// </summary>
        public void FocusOnTarget()
        {
            if (_target == null) return;

            _orbitPivot = _target.position + (Vector3.up * 1.0f);
            _orbitDistance = CAMERA_CONFIG.FOCUS_DISTANCE;
            _pitch = CAMERA_CONFIG.FOCUS_PITCH;

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0.0f);
            transform.position = _orbitPivot - (rot * Vector3.forward * _orbitDistance);
            transform.rotation = rot;
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void Update()
        {
            // Cycle camera mode with C key
            if (DungeonInputReader.WasCameraTogglePressed())
            {
                CycleCameraMode();
            }

            // Focus camera on target with F key
            if (DungeonInputReader.WasFocusKeyPressed())
            {
                FocusOnTarget();
            }

            // Handle Scene View Editor mouse controls
            HandleMouseNavigation();
        }

        private void LateUpdate()
        {
            switch (_currentMode)
            {
                case CameraViewMode.IsometricTopDown:
                    UpdateIsometricCamera();
                    break;
                case CameraViewMode.ThirdPersonFollow:
                    UpdateThirdPersonCamera();
                    break;
                case CameraViewMode.Overview:
                    UpdateOverviewCamera();
                    break;
                case CameraViewMode.FreeFlyEditor:
                    UpdateFreeFlyCamera();
                    break;
            }
        }

        /// <summary>
        /// Handles Scene View free mouse look, flythrough, panning, orbiting, and zooming.
        /// </summary>
        private void HandleMouseNavigation()
        {
            // Ignore mouse navigation clicks when pointer is over the HUD
            if (DungeonHUD.IsPointerOverHUD && !_isCursorLocked)
            {
                return;
            }

            // 1. Scene View Flythrough Mode (Hold Right Mouse Button)
            if (DungeonInputReader.IsRightMouseButtonHeld())
            {
                if (!_isCursorLocked)
                {
                    LockCursor();
                    if (_currentMode != CameraViewMode.FreeFlyEditor)
                    {
                        SetCameraMode(CameraViewMode.FreeFlyEditor);
                    }
                }

                // Mouse look: change camera direction (yaw & pitch)
                Vector2 mouseDelta = DungeonInputReader.GetMouseDelta();
                _yaw += mouseDelta.x * CAMERA_CONFIG.MOUSE_LOOK_SENSITIVITY;
                _pitch -= mouseDelta.y * CAMERA_CONFIG.MOUSE_LOOK_SENSITIVITY;
                _pitch = Mathf.Clamp(_pitch, CAMERA_CONFIG.PITCH_MIN, CAMERA_CONFIG.PITCH_MAX);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);

                // WASDQE Flythrough movement
                Vector3 flyInput = DungeonInputReader.GetFlyMovementVector();
                float speed = DungeonInputReader.IsShiftKeyHeld() ? CAMERA_CONFIG.FLY_FAST_SPEED : _currentFlySpeed;
                Vector3 moveDelta = (transform.forward * flyInput.z) + (transform.right * flyInput.x) + (Vector3.up * flyInput.y);
                transform.position += moveDelta * (speed * Time.deltaTime);

                // Adjust flight speed with scroll wheel while holding RMB
                float speedScroll = DungeonInputReader.GetMouseScrollDelta();
                if (Mathf.Abs(speedScroll) > 0.001f)
                {
                    _currentFlySpeed = Mathf.Clamp(_currentFlySpeed + (speedScroll * 8.0f), CAMERA_CONFIG.FLY_MIN_SPEED, CAMERA_CONFIG.FLY_MAX_SPEED);
                }
            }
            else if (_isCursorLocked)
            {
                UnlockCursor();
            }

            // 2. Pan Mode (Hold Middle Mouse Button or Alt + Middle Mouse Button)
            if (DungeonInputReader.IsMiddleMouseButtonHeld())
            {
                if (_currentMode != CameraViewMode.FreeFlyEditor)
                {
                    SetCameraMode(CameraViewMode.FreeFlyEditor);
                }

                Vector2 panDelta = DungeonInputReader.GetMouseDelta();
                Vector3 pan = (-transform.right * (panDelta.x * CAMERA_CONFIG.MOUSE_PAN_SENSITIVITY)) +
                              (-transform.up * (panDelta.y * CAMERA_CONFIG.MOUSE_PAN_SENSITIVITY));
                transform.position += pan;
                _orbitPivot += pan;
            }

            // 3. Orbit Mode (Hold Alt + Left Mouse Button)
            if (DungeonInputReader.IsAltKeyHeld() && DungeonInputReader.IsLeftMouseButtonHeld())
            {
                if (_currentMode != CameraViewMode.FreeFlyEditor)
                {
                    SetCameraMode(CameraViewMode.FreeFlyEditor);
                }

                Vector2 orbitDelta = DungeonInputReader.GetMouseDelta();
                _yaw += orbitDelta.x * CAMERA_CONFIG.MOUSE_ORBIT_SENSITIVITY;
                _pitch -= orbitDelta.y * CAMERA_CONFIG.MOUSE_ORBIT_SENSITIVITY;
                _pitch = Mathf.Clamp(_pitch, CAMERA_CONFIG.PITCH_MIN, CAMERA_CONFIG.PITCH_MAX);

                Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0.0f);
                transform.position = _orbitPivot - (rot * Vector3.forward * _orbitDistance);
                transform.rotation = rot;
            }

            // 4. Zoom / Dolly (Mouse Scroll Wheel when not holding RMB)
            if (!DungeonInputReader.IsRightMouseButtonHeld())
            {
                float scroll = DungeonInputReader.GetMouseScrollDelta();
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    if (_currentMode == CameraViewMode.FreeFlyEditor)
                    {
                        // Dolly camera forward/backward along view vector
                        transform.position += transform.forward * (scroll * CAMERA_CONFIG.SCROLL_ZOOM_STEP);
                        _orbitDistance = Mathf.Max(2.0f, _orbitDistance - (scroll * CAMERA_CONFIG.SCROLL_ZOOM_STEP));
                    }
                    else
                    {
                        _currentZoom = Mathf.Clamp(_currentZoom - (scroll * CAMERA_CONFIG.ZOOM_SPEED), CAMERA_CONFIG.MIN_ZOOM, CAMERA_CONFIG.MAX_ZOOM);
                    }
                }
            }
        }

        private void UpdateIsometricCamera()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position + (CAMERA_CONFIG.ISOMETRIC_OFFSET * _currentZoom);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(CAMERA_CONFIG.ISOMETRIC_ROTATION), Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);
        }

        private void UpdateThirdPersonCamera()
        {
            if (_target == null) return;

            Vector3 followOffset = _target.rotation * (CAMERA_CONFIG.THIRD_PERSON_OFFSET * _currentZoom);
            Vector3 targetPos = _target.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);

            Quaternion targetRot = Quaternion.LookRotation(_target.position + (Vector3.up * 1.5f) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);
        }

        private void UpdateOverviewCamera()
        {
            transform.position = Vector3.Lerp(transform.position, _overviewPosition * _currentZoom, Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(CAMERA_CONFIG.OVERVIEW_PITCH, 0.0f, 0.0f), Time.deltaTime * CAMERA_CONFIG.FOLLOW_DAMPING);
        }

        private void UpdateFreeFlyCamera()
        {
            // Free fly mode maintains its current manual position and rotation
        }

        private void SyncOrientationFromTransform()
        {
            Vector3 euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
            if (_pitch > 180.0f) _pitch -= 360.0f;
            _pitch = Mathf.Clamp(_pitch, CAMERA_CONFIG.PITCH_MIN, CAMERA_CONFIG.PITCH_MAX);

            if (_target != null)
            {
                _orbitPivot = _target.position + (Vector3.up * 1.0f);
            }
            else
            {
                _orbitPivot = transform.position + (transform.forward * CAMERA_CONFIG.FOCUS_DISTANCE);
            }
            _orbitDistance = Vector3.Distance(transform.position, _orbitPivot);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _isCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _isCursorLocked = false;
        }
    }
}
