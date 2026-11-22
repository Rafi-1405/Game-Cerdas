using System;
using UnityEngine;
using DeadDepths.Core;

namespace DeadDepths.Player
{
    /// <summary>
    /// Controls the isometric follow camera with smooth damping, cursor aim look-ahead (10% deadzone, 3.2m cap),
    /// Main Menu diorama orbit, and camera shake.
    /// ASD-STE100: This camera follows the player from an isometric angle.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Tooltip("Isometric pitch angle in degrees.")]
            public float PitchAngle = 52f;

            [Tooltip("Isometric yaw angle in degrees.")]
            public float YawAngle = 45f;

            [Tooltip("Distance from camera to player.")]
            public float Distance = 16f;

            [Tooltip("Target offset above player root.")]
            public Vector3 TargetOffset = new Vector3(0f, 1.2f, 0f);

            [Tooltip("Smooth follow damping speed.")]
            public float FollowDamping = 10f;

            [Header("Aim Look-Ahead")]
            public bool EnableAimLookAhead = true;
            public float LookAheadDeadzone = 0.10f;
            public float MaxLookAheadDistance = 3.2f;
            public float LookAheadDamping = 6.0f;
        }
        #endregion

        public const string PrefAimLookAhead = "DeadDepths_AimLookAhead";

        [SerializeField] private Config camConfig = new Config();
        public Config CamConfig => camConfig;

        [SerializeField] private Transform target;

        public static IsometricCameraController Instance { get; private set; }

        public Vector3 CurrentLookAheadOffset => currentLookAheadOffset;

        private Vector3 currentLookAheadOffset = Vector3.zero;
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;
        private bool hasSnappedToTarget = false;
        private float menuOrbitAngle = 35f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ApplyFallbackDefaults();
            LoadSettings();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (camConfig == null) camConfig = new Config();
            if (camConfig.PitchAngle <= 0f) camConfig.PitchAngle = 52f;
            if (camConfig.Distance <= 0f) camConfig.Distance = 16f;
            if (camConfig.FollowDamping <= 0f) camConfig.FollowDamping = 10f;
            if (camConfig.LookAheadDeadzone <= 0f) camConfig.LookAheadDeadzone = 0.10f;
            if (camConfig.MaxLookAheadDistance <= 0f) camConfig.MaxLookAheadDistance = 3.2f;
            if (camConfig.LookAheadDamping <= 0f) camConfig.LookAheadDamping = 6.0f;
        }

        public void LoadSettings()
        {
            if (PlayerPrefs.HasKey(PrefAimLookAhead))
            {
                camConfig.EnableAimLookAhead = PlayerPrefs.GetInt(PrefAimLookAhead, 1) == 1;
            }
        }

        public void SetAimLookAheadEnabled(bool enabled, bool save = true)
        {
            camConfig.EnableAimLookAhead = enabled;
            if (!enabled) currentLookAheadOffset = Vector3.zero;
            if (save)
            {
                PlayerPrefs.SetInt(PrefAimLookAhead, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasSnappedToTarget = false;
            currentLookAheadOffset = Vector3.zero;
        }

        public void SnapToTarget(Transform newTarget = null)
        {
            if (newTarget != null)
            {
                target = newTarget;
            }
            if (target == null) return;

            currentLookAheadOffset = Vector3.zero;
            Quaternion isoRotation = Quaternion.Euler(camConfig.PitchAngle, camConfig.YawAngle, 0f);
            Vector3 focusPoint = target.position + camConfig.TargetOffset;
            transform.position = focusPoint - (isoRotation * Vector3.forward * camConfig.Distance);
            transform.rotation = isoRotation;
            hasSnappedToTarget = true;
        }

        public void TriggerShake(float intensity = 0.25f, float duration = 0.2f)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
        }

        /// <summary>
        /// Updates aim look-ahead offset smoothly; public so automated tests can verify deadzone & 3.2m clamp.
        /// </summary>
        public void UpdateAimLookAhead(DeadDepthsPlayerController playerCtrl, Vector2 normalizedViewportOffset, float dt)
        {
            Vector3 desiredLookAhead = Vector3.zero;

            if (camConfig.EnableAimLookAhead && playerCtrl != null && playerCtrl.IsAiming && !playerCtrl.IsDead)
            {
                float mag = normalizedViewportOffset.magnitude;
                if (mag > camConfig.LookAheadDeadzone)
                {
                    float scaled = Mathf.Clamp01((mag - camConfig.LookAheadDeadzone) / Mathf.Max(0.05f, 0.5f - camConfig.LookAheadDeadzone));
                    Vector3 aimDir = playerCtrl.LastValidAimPoint - playerCtrl.transform.position;
                    aimDir.y = 0f;
                    if (aimDir.sqrMagnitude > 0.01f)
                    {
                        float targetDist = Mathf.Min(aimDir.magnitude * 0.45f, camConfig.MaxLookAheadDistance * scaled);
                        desiredLookAhead = aimDir.normalized * Mathf.Clamp(targetDist, 0f, camConfig.MaxLookAheadDistance);
                    }
                }
            }

            float k = 1f - Mathf.Exp(-camConfig.LookAheadDamping * Mathf.Max(0f, dt));
            currentLookAheadOffset = Vector3.Lerp(currentLookAheadOffset, desiredLookAhead, k);
            if (currentLookAheadOffset.magnitude > camConfig.MaxLookAheadDistance)
            {
                currentLookAheadOffset = currentLookAheadOffset.normalized * camConfig.MaxLookAheadDistance;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                if (target == null) return;
            }

            // Main Menu Diorama Orbit
            if (GameManager.Instance != null && GameManager.Instance.IsInMainMenu)
            {
                menuOrbitAngle += Time.deltaTime * 4.5f;
                Quaternion orbitRot = Quaternion.Euler(40f, menuOrbitAngle, 0f);
                // Shift focus slightly left in camera space so the live forest diorama sits cleanly on the right 65%
                Vector3 rightShift = orbitRot * Vector3.right * -2.2f;
                Vector3 dioramaCenter = target.position + Vector3.up * 1.4f + rightShift;
                Vector3 orbitPos = dioramaCenter - (orbitRot * Vector3.forward * (camConfig.Distance + 3.5f));
                transform.position = Vector3.Lerp(transform.position, orbitPos, 1f - Mathf.Exp(-4f * Time.deltaTime));
                transform.rotation = Quaternion.Slerp(transform.rotation, orbitRot, 1f - Mathf.Exp(-4f * Time.deltaTime));
                return;
            }

            if (!hasSnappedToTarget)
            {
                SnapToTarget(target);
                return;
            }

            var playerCtrl = DeadDepthsPlayerController.Instance;
            Vector2 vpOffset = Vector2.zero;
            var cam = GetComponent<Camera>();
            if (cam != null && playerCtrl != null)
            {
                Vector3 vp = cam.ScreenToViewportPoint(playerCtrl.CursorScreenPosition);
                vpOffset = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            }
            UpdateAimLookAhead(playerCtrl, vpOffset, Time.deltaTime);

            Quaternion isoRotation = Quaternion.Euler(camConfig.PitchAngle, camConfig.YawAngle, 0f);
            Vector3 focusPoint = target.position + camConfig.TargetOffset + currentLookAheadOffset;
            Vector3 desiredPosition = focusPoint - (isoRotation * Vector3.forward * camConfig.Distance);

            if (shakeDuration > 0f)
            {
                shakeDuration -= Time.unscaledDeltaTime;
                if (shakeDuration <= 0f)
                {
                    shakeDuration = 0f;
                    shakeIntensity = 0f;
                }
                else
                {
                    Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * shakeIntensity;
                    desiredPosition += shakeOffset;
                    shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, Time.unscaledDeltaTime * 8f);
                }
            }

            float dampFactor = 1f - Mathf.Exp(-camConfig.FollowDamping * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, dampFactor);
            transform.rotation = isoRotation;
        }
    }
}
