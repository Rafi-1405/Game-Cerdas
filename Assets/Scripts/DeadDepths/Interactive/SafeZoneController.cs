using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Player;
using UTS.Audio;

namespace DeadDepths.Interactive
{
    /// <summary>
    /// Enforces the 3/3 Supply Intel objective and 4.0s continuous hold inside the evacuation safe zone.
    /// ASD-STE100: This controller validates extraction requirements and completes the sector.
    /// </summary>
    public class SafeZoneController : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public float SafeZoneRadius = 7.0f;
            public float HoldDurationRequired = 4.0f;
        }
        #endregion

        [SerializeField] private Config zoneConfig = new Config();
        public Config ZoneConfig => zoneConfig;

        public static SafeZoneController Instance { get; private set; }

        public bool IsPlayerInsideZone { get; private set; }
        public bool IsHoldingExtraction { get; private set; }
        public bool HasExtracted { get; private set; }
        public float CurrentHoldTime { get; private set; }
        public float HoldProgress => Mathf.Clamp01(CurrentHoldTime / Mathf.Max(0.1f, zoneConfig.HoldDurationRequired));
        public float HoldRemainingSeconds => Mathf.Max(0f, zoneConfig.HoldDurationRequired - CurrentHoldTime);

        public bool HasRequiredIntel
        {
            get
            {
                var gm = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
                return gm == null || gm.HasRequiredSupplyIntel;
            }
        }

        private Transform playerTransform;
        private DeadDepthsPlayerController playerController;
        private float nextTickTime = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;
            ApplyFallbackDefaults();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (zoneConfig == null) zoneConfig = new Config();
            if (zoneConfig.SafeZoneRadius <= 0f) zoneConfig.SafeZoneRadius = 7.0f;
            if (zoneConfig.HoldDurationRequired <= 0f) zoneConfig.HoldDurationRequired = 4.0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (HasExtracted) return;
            if (Time.timeScale <= 0f) return;

            var gm = GameManager.Instance;
            if (gm != null && (gm.IsInMainMenu || !gm.IsLevelActive))
            {
                IsPlayerInsideZone = false;
                IsHoldingExtraction = false;
                CurrentHoldTime = 0f;
                return;
            }

            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    playerTransform = p.transform;
                    playerController = p.GetComponent<DeadDepthsPlayerController>();
                }
                if (playerTransform == null) return;
            }

            if (playerController != null && playerController.IsDead)
            {
                IsPlayerInsideZone = false;
                IsHoldingExtraction = false;
                CurrentHoldTime = 0f;
                return;
            }

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(playerTransform.position.x, 0f, playerTransform.position.z));

            IsPlayerInsideZone = dist <= zoneConfig.SafeZoneRadius;
            EvaluateExtractionHold(Time.deltaTime);
        }

        /// <summary>
        /// Evaluates hold progress over dt seconds; exposed cleanly for both runtime Update and unit tests.
        /// </summary>
        public void EvaluateExtractionHold(float dt)
        {
            if (HasExtracted) return;

            if (!IsPlayerInsideZone || !HasRequiredIntel)
            {
                IsHoldingExtraction = false;
                CurrentHoldTime = Mathf.MoveTowards(CurrentHoldTime, 0f, dt * 1.5f);
                return;
            }

            if (!IsHoldingExtraction)
            {
                IsHoldingExtraction = true;
                nextTickTime = Time.time + 0.9f;
                AudioManager.Instance?.PlayCue(SfxCue.ExtractionTick, transform.position);
            }

            CurrentHoldTime += Mathf.Max(0f, dt);

            if (Time.time >= nextTickTime && CurrentHoldTime < zoneConfig.HoldDurationRequired)
            {
                nextTickTime = Time.time + 1.0f;
                AudioManager.Instance?.PlayCue(SfxCue.ExtractionTick, transform.position);
            }

            if (CurrentHoldTime >= zoneConfig.HoldDurationRequired)
            {
                TriggerExtraction();
            }
        }

        public void SetPlayerInsideForTest(bool inside)
        {
            IsPlayerInsideZone = inside;
        }

        public void TriggerExtraction()
        {
            if (HasExtracted) return;
            if (!HasRequiredIntel) return;

            HasExtracted = true;
            IsHoldingExtraction = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ExtractionSuccess, transform.position);
            }

            var gm = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.CompleteLevel();
            }
        }
    }
}
