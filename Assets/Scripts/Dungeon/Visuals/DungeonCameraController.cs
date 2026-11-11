using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Multi-mode camera controller supporting Isometric Top-Down, Third-Person, and Overview perspectives.
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
            public const float MIN_ZOOM = 4.0f;
            public const float MAX_ZOOM = 45.0f;
        }
        #endregion

        [SerializeField] private CameraViewMode _currentMode = CameraViewMode.IsometricTopDown;
        [SerializeField] private Transform _target;

        private float _currentZoom = 1.0f;
        private Vector3 _overviewPosition = Vector3.up * 40.0f;

        public CameraViewMode CurrentMode => _currentMode;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetOverviewPosition(Vector3 center, float dungeonSpan)
        {
            float height = Mathf.Max(25.0f, dungeonSpan * CAMERA_CONFIG.OVERVIEW_HEIGHT_MULTIPLIER);
            _overviewPosition = new Vector3(center.x, height, center.z);
        }

        public void CycleCameraMode()
        {
            int next = ((int)_currentMode + 1) % 3;
            SetCameraMode((CameraViewMode)next);
        }

        public void SetCameraMode(CameraViewMode mode)
        {
            _currentMode = mode;
        }

        private void Update()
        {
            // Cycle camera mode with C key or controller
            if (DungeonInputReader.WasCameraTogglePressed())
            {
                CycleCameraMode();
            }

            // Adjust zoom with mouse scroll
            float scroll = DungeonInputReader.GetMouseScrollDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _currentZoom = Mathf.Clamp(_currentZoom - (scroll * CAMERA_CONFIG.ZOOM_SPEED), 0.5f, 2.5f);
            }
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
    }
}
