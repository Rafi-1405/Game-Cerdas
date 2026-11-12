using System;
using UnityEngine;

namespace DeadDepths.Player
{
    /// <summary>
    /// Controls the isometric follow camera with smooth damping and camera shake.
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
        }
        #endregion

        [SerializeField] private Config camConfig = new Config();
        public Config CamConfig => camConfig;

        [SerializeField] private Transform target;

        public static IsometricCameraController Instance { get; private set; }

        private Vector3 currentVelocity;
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void TriggerShake(float intensity = 0.25f, float duration = 0.2f)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(shakeDuration, duration);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                if (target == null) return;
            }

            // Calculate isometric rotation
            Quaternion isoRotation = Quaternion.Euler(camConfig.PitchAngle, camConfig.YawAngle, 0f);

            // Calculate desired position
            Vector3 focusPoint = target.position + camConfig.TargetOffset;
            Vector3 desiredPosition = focusPoint - (isoRotation * Vector3.forward * camConfig.Distance);

            // Apply camera shake if active
            if (shakeDuration > 0f)
            {
                shakeDuration -= Time.unscaledDeltaTime;
                Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * shakeIntensity;
                desiredPosition += shakeOffset;
                shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, Time.unscaledDeltaTime * 8f);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * camConfig.FollowDamping);
            transform.rotation = isoRotation;
        }
    }
}
