using System;
using UnityEngine;

namespace DeadDepths.Core
{
    /// <summary>
    /// Tracks player performance indicators during a level.
    /// ASD-STE100: This module records health, kills, damage, and time.
    /// </summary>
    public class PlayerPerformanceTracker : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Tooltip("Target time to complete a level in seconds.")]
            public float TargetCompletionTime = 180f;

            [Tooltip("Expected damage threshold for normalization.")]
            public float DamageThreshold = 100f;

            [Tooltip("Budget of consumed resources for normalization.")]
            public float ResourceBudget = 5f;
        }
        #endregion

        [SerializeField] private Config trackerConfig = new Config();
        public Config TrackerConfig => trackerConfig;

        public static PlayerPerformanceTracker Instance { get; private set; }

        public float StartingHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; } = 100f;
        public float MaxHealth { get; private set; } = 100f;

        public int ZombiesEncountered { get; private set; } = 0;
        public int ZombiesKilled { get; private set; } = 0;

        public float DamageReceived { get; private set; } = 0f;
        public float ResourcesConsumed { get; private set; } = 0f;
        public float LevelStartTime { get; private set; } = 0f;
        public float ElapsedLevelTime => Time.time - LevelStartTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Resets all tracking metrics for a new level.
        /// </summary>
        public void StartNewLevel(float playerHealth, float playerMaxHealth, int totalZombiesSpawned)
        {
            StartingHealth = playerHealth;
            CurrentHealth = playerHealth;
            MaxHealth = Mathf.Max(1f, playerMaxHealth);
            ZombiesEncountered = totalZombiesSpawned;
            ZombiesKilled = 0;
            DamageReceived = 0f;
            ResourcesConsumed = 0f;
            LevelStartTime = Time.time;
        }

        public void UpdatePlayerHealth(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = Mathf.Max(1f, maxHealth);
        }

        public void RecordDamageReceived(float amount)
        {
            DamageReceived += Mathf.Max(0f, amount);
        }

        public void RecordZombieKill()
        {
            ZombiesKilled++;
        }

        public void RecordResourceConsumed(float amount = 1f)
        {
            ResourcesConsumed += amount;
        }

        public void RegisterEncounteredZombie()
        {
            ZombiesEncountered++;
        }
    }
}
