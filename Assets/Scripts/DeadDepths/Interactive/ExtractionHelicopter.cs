using System;
using UnityEngine;
using DeadDepths.Core;
using DeadDepths.Player;

namespace DeadDepths.Interactive
{
    /// <summary>
    /// Handles helicopter rotor visuals and coordinates extraction state with SafeZoneController.
    /// ASD-STE100: This module drives the extraction helicopter and links to SafeZoneController.
    /// </summary>
    public class ExtractionHelicopter : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Tooltip("Radius of the safe zone perimeter.")]
            public float SafeZoneRadius = 7.0f;

            [Tooltip("Radius of the helicopter cabin entry trigger.")]
            public float HelicopterCabinRadius = 2.5f;

            [Tooltip("Rotation speed of main helicopter rotor blades in degrees per second.")]
            public float RotorSpeed = 720f;
        }
        #endregion

        [SerializeField] private Config heliConfig = new Config();
        public Config HeliConfig => heliConfig;

        public bool IsPlayerInsideSafeZone => safeZone != null && safeZone.IsPlayerInsideZone;
        public bool IsPlayerInsideHelicopter { get; private set; } = false;
        public bool HasExtracted => safeZone != null && safeZone.HasExtracted;

        private Transform mainRotor;
        private Transform tailRotor;
        private Transform playerTransform;
        private SafeZoneController safeZone;

        private void Awake()
        {
            ApplyFallbackDefaults();
            mainRotor = transform.Find("MainRotor");
            tailRotor = transform.Find("TailRotor");
            safeZone = GetComponent<SafeZoneController>();
            if (safeZone == null)
            {
                safeZone = gameObject.AddComponent<SafeZoneController>();
            }
            safeZone.ZoneConfig.SafeZoneRadius = heliConfig.SafeZoneRadius;
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (heliConfig == null) heliConfig = new Config();
            if (heliConfig.SafeZoneRadius <= 0f) heliConfig.SafeZoneRadius = 7.0f;
            if (heliConfig.HelicopterCabinRadius <= 0f) heliConfig.HelicopterCabinRadius = 2.5f;
            if (heliConfig.RotorSpeed <= 0f) heliConfig.RotorSpeed = 720f;
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            // Spin main and tail rotor blades
            if (mainRotor != null)
            {
                mainRotor.Rotate(Vector3.up, heliConfig.RotorSpeed * Time.deltaTime, Space.Self);
            }
            if (tailRotor != null)
            {
                tailRotor.Rotate(Vector3.right, heliConfig.RotorSpeed * 1.5f * Time.deltaTime, Space.Self);
            }

            if (HasExtracted)
            {
                IsPlayerInsideHelicopter = true;
                return;
            }

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            var playerCtrl = playerTransform.GetComponent<DeadDepthsPlayerController>();
            if (playerCtrl != null && playerCtrl.IsDead) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            IsPlayerInsideHelicopter = dist <= heliConfig.HelicopterCabinRadius && (safeZone == null || safeZone.HasExtracted);
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, heliConfig.SafeZoneRadius);

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, heliConfig.HelicopterCabinRadius);
        }
    }
}
