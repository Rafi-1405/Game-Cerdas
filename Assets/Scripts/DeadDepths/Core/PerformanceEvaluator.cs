using System;
using UnityEngine;

namespace DeadDepths.Core
{
    public enum DifficultyTier
    {
        Struggling,
        Balanced,
        PerformingWell
    }

    /// <summary>
    /// Calculates normalized player performance scores and difficulty classifications.
    /// ASD-STE100: This module evaluates player statistics into a normalized score.
    /// </summary>
    public static class PerformanceEvaluator
    {
        #region CONFIG
        public static class Config
        {
            public const float WeightHealth = 0.30f;
            public const float WeightKills = 0.20f;
            public const float WeightTime = 0.15f;
            public const float WeightDamage = 0.20f;
            public const float WeightResources = 0.15f;

            public const float ThresholdStrugglingMax = 0.39f;
            public const float ThresholdBalancedMax = 0.69f;
        }
        #endregion

        /// <summary>
        /// Calculates the final performance score between 0.0 and 1.0.
        /// </summary>
        public static float EvaluatePerformance(PlayerPerformanceTracker tracker, out DifficultyTier classification)
        {
            if (tracker == null)
            {
                classification = DifficultyTier.Balanced;
                return 0.5f;
            }

            var cfg = tracker.TrackerConfig;

            // Health remaining score (30%)
            float healthScore = tracker.MaxHealth > 0f
                ? Mathf.Clamp01(tracker.CurrentHealth / tracker.MaxHealth)
                : 0.5f;

            // Kill efficiency score (20%)
            float killScore = tracker.ZombiesEncountered > 0
                ? Mathf.Clamp01((float)tracker.ZombiesKilled / tracker.ZombiesEncountered)
                : 1.0f;

            // Time efficiency score (15%)
            float actualTime = Mathf.Max(1f, tracker.ElapsedLevelTime);
            float timeScore = Mathf.Clamp01(cfg.TargetCompletionTime / actualTime);

            // Damage avoidance score (20%)
            float damageScore = cfg.DamageThreshold > 0f
                ? Mathf.Clamp01(1f - (tracker.DamageReceived / cfg.DamageThreshold))
                : 1.0f;

            // Resource efficiency score (15%)
            float resourceScore = cfg.ResourceBudget > 0f
                ? Mathf.Clamp01(1f - (tracker.ResourcesConsumed / cfg.ResourceBudget))
                : 1.0f;

            float performanceScore =
                (Config.WeightHealth * healthScore) +
                (Config.WeightKills * killScore) +
                (Config.WeightTime * timeScore) +
                (Config.WeightDamage * damageScore) +
                (Config.WeightResources * resourceScore);

            performanceScore = Mathf.Clamp01(performanceScore);

            if (performanceScore <= Config.ThresholdStrugglingMax)
            {
                classification = DifficultyTier.Struggling;
            }
            else if (performanceScore <= Config.ThresholdBalancedMax)
            {
                classification = DifficultyTier.Balanced;
            }
            else
            {
                classification = DifficultyTier.PerformingWell;
            }

            return performanceScore;
        }

        /// <summary>
        /// Calculates the final score directly from normalized factor values (0.0 to 1.0).
        /// </summary>
        public static float CalculateDirectScore(float healthScore, float killScore, float timeScore, float damageScore, float resourceScore, out DifficultyTier classification)
        {
            float performanceScore =
                (Config.WeightHealth * Mathf.Clamp01(healthScore)) +
                (Config.WeightKills * Mathf.Clamp01(killScore)) +
                (Config.WeightTime * Mathf.Clamp01(timeScore)) +
                (Config.WeightDamage * Mathf.Clamp01(damageScore)) +
                (Config.WeightResources * Mathf.Clamp01(resourceScore));

            performanceScore = Mathf.Clamp01(performanceScore);

            if (performanceScore <= Config.ThresholdStrugglingMax)
            {
                classification = DifficultyTier.Struggling;
            }
            else if (performanceScore <= Config.ThresholdBalancedMax)
            {
                classification = DifficultyTier.Balanced;
            }
            else
            {
                classification = DifficultyTier.PerformingWell;
            }

            return performanceScore;
        }
    }
}
