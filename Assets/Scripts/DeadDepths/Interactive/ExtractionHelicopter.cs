using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using UTS.Audio;

namespace DeadDepths.Interactive
{
    /// <summary>
    /// Handles the extraction safe zone and helicopter completion trigger.
    /// ASD-STE100: This module completes the level when the player enters the safe zone and helicopter.
    /// Follows Section 4.1 of the specification.
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

        public bool IsPlayerInsideSafeZone { get; private set; } = false;
        public bool IsPlayerInsideHelicopter { get; private set; } = false;
        public bool HasExtracted { get; private set; } = false;

        private Transform mainRotor;
        private Transform playerTransform;

        private void Awake()
        {
            mainRotor = transform.Find("MainRotor");
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            // Spin rotor blades
            if (mainRotor != null)
            {
                mainRotor.Rotate(Vector3.up, heliConfig.RotorSpeed * Time.deltaTime, Space.Self);
            }

            if (HasExtracted) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Step 1: Check safe zone entry
            IsPlayerInsideSafeZone = dist <= heliConfig.SafeZoneRadius;

            // Step 2: Check helicopter entry AFTER entering safe zone
            if (IsPlayerInsideSafeZone && dist <= heliConfig.HelicopterCabinRadius)
            {
                TriggerExtraction();
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void TriggerExtraction()
        {
            if (HasExtracted) return;

            HasExtracted = true;
            IsPlayerInsideHelicopter = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ExtractionSuccess, transform.position);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
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
