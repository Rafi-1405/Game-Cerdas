using System;
using UnityEngine;

namespace DeadDepths.Core
{
    /// <summary>
    /// Configures procedural difficulty parameters for level generation.
    /// ASD-STE100: This module provides generation settings based on player difficulty.
    /// </summary>
    public class DifficultyController : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class LevelDifficultyProfile
        {
            public int TotalZombies = 18;
            public float WalkerProportion = 0.65f;
            public float SpitterProportion = 0.20f;
            public float ChargerProportion = 0.15f;

            [Range(0f, 1f)]
            public float LootQualityFactor = 0.5f;

            public int HealingSuppliesCount = 5;
            public int BlockingObjectsCount = 24;
        }

        [Serializable]
        public class DdaPresetConfig
        {
            [Header("Sector Zombie Scaling")]
            [Tooltip("Base zombie count for Sector 1 (strictly less than 5).")]
            public int BaseSectorOneZombies = 4;

            [Tooltip("Additional zombies added for each subsequent sector.")]
            public int ZombiesPerSectorIncrement = 1;

            [Tooltip("Upper cap for procedural zombie spawning in high sectors.")]
            public int MaxZombiesCap = 26;

            public LevelDifficultyProfile Struggling = new LevelDifficultyProfile
            {
                TotalZombies = 3,
                WalkerProportion = 0.80f,
                SpitterProportion = 0.12f,
                ChargerProportion = 0.08f,
                LootQualityFactor = 0.75f,
                HealingSuppliesCount = 7,
                BlockingObjectsCount = 16
            };

            public LevelDifficultyProfile Balanced = new LevelDifficultyProfile
            {
                TotalZombies = 4,
                WalkerProportion = 0.65f,
                SpitterProportion = 0.20f,
                ChargerProportion = 0.15f,
                LootQualityFactor = 0.50f,
                HealingSuppliesCount = 5,
                BlockingObjectsCount = 24
            };

            public LevelDifficultyProfile PerformingWell = new LevelDifficultyProfile
            {
                TotalZombies = 4,
                WalkerProportion = 0.45f,
                SpitterProportion = 0.30f,
                ChargerProportion = 0.25f,
                LootQualityFactor = 0.30f,
                HealingSuppliesCount = 3,
                BlockingObjectsCount = 32
            };
        }
        #endregion

        [SerializeField] private DdaPresetConfig ddaConfig = new DdaPresetConfig();
        public DdaPresetConfig DdaConfig => ddaConfig;

        public static DifficultyController Instance { get; private set; }

        public DifficultyTier CurrentTier { get; private set; } = DifficultyTier.Balanced;
        public LevelDifficultyProfile CurrentProfile { get; private set; }
        public int ActiveLevelZombieCount { get; private set; } = 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            ApplyFallbackDefaults();
            if (Instance == null)
            {
                Instance = this;
                CurrentProfile = ddaConfig.Balanced;
                ActiveLevelZombieCount = GetZombieCountForLevel(1);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (ddaConfig == null) ddaConfig = new DdaPresetConfig();
            if (ddaConfig.BaseSectorOneZombies <= 0 || ddaConfig.BaseSectorOneZombies >= 5)
                ddaConfig.BaseSectorOneZombies = 4;
            if (ddaConfig.ZombiesPerSectorIncrement <= 0)
                ddaConfig.ZombiesPerSectorIncrement = 1;
            if (ddaConfig.MaxZombiesCap < 10)
                ddaConfig.MaxZombiesCap = 26;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Returns the target zombie count for a given sector:
        /// Sector 1 spawns fewer than 5 zombies (default 4), increasing by 1 per next sector.
        /// </summary>
        public int GetZombieCountForLevel(int levelNumber)
        {
            ApplyFallbackDefaults();
            int lvl = Mathf.Max(1, levelNumber);
            int baseCount = Mathf.Clamp(ddaConfig.BaseSectorOneZombies, 3, 4);
            int increment = Mathf.Max(1, ddaConfig.ZombiesPerSectorIncrement);
            int scaled = baseCount + (lvl - 1) * increment;
            if (lvl == 1)
            {
                return Mathf.Clamp(scaled, 3, 4);
            }
            return Mathf.Clamp(scaled, 3, ddaConfig.MaxZombiesCap);
        }

        /// <summary>
        /// Updates the difficulty profile for the next level based on the evaluated score.
        /// </summary>
        public LevelDifficultyProfile UpdateProfile(DifficultyTier tier)
        {
            ApplyFallbackDefaults();
            CurrentTier = tier;
            switch (tier)
            {
                case DifficultyTier.Struggling:
                    CurrentProfile = ddaConfig.Struggling;
                    break;
                case DifficultyTier.PerformingWell:
                    CurrentProfile = ddaConfig.PerformingWell;
                    break;
                case DifficultyTier.Balanced:
                default:
                    CurrentProfile = ddaConfig.Balanced;
                    break;
            }
            return CurrentProfile;
        }

        /// <summary>
        /// Calculates integer enemy counts for a specific sector using lower-sector scaling
        /// (< 5 zombies in Sector 1, +1 per next sector) and the DDA archetype proportions.
        /// </summary>
        public void CalculateEnemyCountsForLevel(LevelDifficultyProfile profile, int levelNumber, out int walkers, out int spitters, out int chargers)
        {
            int total = GetZombieCountForLevel(levelNumber);
            ActiveLevelZombieCount = total;
            CalculateEnemyCountsInternal(profile, total, out walkers, out spitters, out chargers);
        }

        /// <summary>
        /// Calculates integer enemy counts from proportions while respecting total budget.
        /// </summary>
        public void CalculateEnemyCounts(LevelDifficultyProfile profile, out int walkers, out int spitters, out int chargers)
        {
            int total = Mathf.Max(3, profile != null ? profile.TotalZombies : GetZombieCountForLevel(1));
            ActiveLevelZombieCount = total;
            CalculateEnemyCountsInternal(profile, total, out walkers, out spitters, out chargers);
        }

        private static void CalculateEnemyCountsInternal(LevelDifficultyProfile profile, int total, out int walkers, out int spitters, out int chargers)
        {
            total = Mathf.Max(3, total);
            float wProp = profile != null ? profile.WalkerProportion : 0.65f;
            float sProp = profile != null ? profile.SpitterProportion : 0.20f;
            float cProp = profile != null ? profile.ChargerProportion : 0.15f;

            float sum = wProp + sProp + cProp;
            if (sum <= 0f) sum = 1f;

            float normS = sProp / sum;
            float normC = cProp / sum;

            spitters = Mathf.RoundToInt(total * normS);
            chargers = Mathf.RoundToInt(total * normC);
            walkers = total - spitters - chargers;

            while (walkers < 1)
            {
                walkers++;
                if (spitters >= chargers && spitters > 0) spitters--;
                else if (chargers > 0) chargers--;
                else break;
            }
        }
    }
}
