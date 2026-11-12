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
            public LevelDifficultyProfile Struggling = new LevelDifficultyProfile
            {
                TotalZombies = 12,
                WalkerProportion = 0.80f,
                SpitterProportion = 0.12f,
                ChargerProportion = 0.08f,
                LootQualityFactor = 0.75f,
                HealingSuppliesCount = 7,
                BlockingObjectsCount = 16
            };

            public LevelDifficultyProfile Balanced = new LevelDifficultyProfile
            {
                TotalZombies = 18,
                WalkerProportion = 0.65f,
                SpitterProportion = 0.20f,
                ChargerProportion = 0.15f,
                LootQualityFactor = 0.50f,
                HealingSuppliesCount = 5,
                BlockingObjectsCount = 24
            };

            public LevelDifficultyProfile PerformingWell = new LevelDifficultyProfile
            {
                TotalZombies = 26,
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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                CurrentProfile = ddaConfig.Balanced;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Updates the difficulty profile for the next level based on the evaluated score.
        /// </summary>
        public LevelDifficultyProfile UpdateProfile(DifficultyTier tier)
        {
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
        /// Calculates integer enemy counts from proportions while respecting total budget.
        /// </summary>
        public void CalculateEnemyCounts(LevelDifficultyProfile profile, out int walkers, out int spitters, out int chargers)
        {
            int total = Mathf.Max(3, profile.TotalZombies);
            float sum = profile.WalkerProportion + profile.SpitterProportion + profile.ChargerProportion;
            if (sum <= 0f) sum = 1f;

            float normW = profile.WalkerProportion / sum;
            float normS = profile.SpitterProportion / sum;
            float normC = profile.ChargerProportion / sum;

            spitters = Mathf.RoundToInt(total * normS);
            chargers = Mathf.RoundToInt(total * normC);
            walkers = total - spitters - chargers;

            if (walkers < 1)
            {
                walkers = 1;
                if (spitters > chargers && spitters > 1) spitters--;
                else if (chargers > 1) chargers--;
            }
        }
    }
}
