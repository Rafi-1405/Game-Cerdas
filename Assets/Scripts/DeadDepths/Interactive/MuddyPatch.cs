using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.Interactive
{
    public interface IMudSlowable
    {
        void SetTerrainSpeedMultiplier(float multiplier);
    }

    /// <summary>
    /// Applies a 30% movement speed reduction (0.7 multiplier) to actors on muddy terrain.
    /// Tracks overlapping patches per actor so exiting one patch does not cancel slow while still in another.
    /// ASD-STE100: This trigger slows actors when they walk on mud.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MuddyPatch : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Tooltip("Speed multiplier applied to actors on mud (30% reduction).")]
            public float MudSpeedMultiplier = 0.70f;
        }
        #endregion

        [SerializeField] private Config patchConfig = new Config();
        public Config PatchConfig => patchConfig;

        private static readonly Dictionary<IMudSlowable, HashSet<MuddyPatch>> activePatchesByActor =
            new Dictionary<IMudSlowable, HashSet<MuddyPatch>>();

        private readonly HashSet<IMudSlowable> actorsInsideThisPatch = new HashSet<IMudSlowable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activePatchesByActor.Clear();
        }

        private void Awake()
        {
            if (patchConfig == null) patchConfig = new Config();
            if (patchConfig.MudSpeedMultiplier <= 0f) patchConfig.MudSpeedMultiplier = 0.70f;
        }

        private void OnValidate()
        {
            if (patchConfig == null) patchConfig = new Config();
            if (patchConfig.MudSpeedMultiplier <= 0f) patchConfig.MudSpeedMultiplier = 0.70f;
        }

        public static bool IsActorInMud(IMudSlowable slowable)
        {
            return slowable != null &&
                   activePatchesByActor.TryGetValue(slowable, out var set) &&
                   set.Count > 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            var slowable = other.GetComponentInParent<IMudSlowable>();
            if (slowable != null)
            {
                RegisterActorEnter(slowable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var slowable = other.GetComponentInParent<IMudSlowable>();
            if (slowable != null)
            {
                UnregisterActorExit(slowable);
            }
        }

        public void RegisterActorEnter(IMudSlowable slowable)
        {
            if (slowable == null) return;
            if (!activePatchesByActor.TryGetValue(slowable, out var set))
            {
                set = new HashSet<MuddyPatch>();
                activePatchesByActor[slowable] = set;
            }
            set.Add(this);
            actorsInsideThisPatch.Add(slowable);
            slowable.SetTerrainSpeedMultiplier(patchConfig.MudSpeedMultiplier);
        }

        public void UnregisterActorExit(IMudSlowable slowable)
        {
            if (slowable == null) return;
            actorsInsideThisPatch.Remove(slowable);

            if (activePatchesByActor.TryGetValue(slowable, out var set))
            {
                set.Remove(this);
                if (set.Count == 0)
                {
                    activePatchesByActor.Remove(slowable);
                    slowable.SetTerrainSpeedMultiplier(1.0f);
                }
            }
            else
            {
                slowable.SetTerrainSpeedMultiplier(1.0f);
            }
        }

        private void OnDisable()
        {
            CleanupTrackedActors();
        }

        private void OnDestroy()
        {
            CleanupTrackedActors();
        }

        private void CleanupTrackedActors()
        {
            if (actorsInsideThisPatch.Count == 0) return;
            var snapshot = new List<IMudSlowable>(actorsInsideThisPatch);
            foreach (var actor in snapshot)
            {
                UnregisterActorExit(actor);
            }
            actorsInsideThisPatch.Clear();
        }
    }
}
