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
        private static readonly List<IMudSlowable> staleActorsBuffer = new List<IMudSlowable>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activePatchesByActor.Clear();
            staleActorsBuffer.Clear();
        }

        private static bool IsActorValid(IMudSlowable actor)
        {
            if (actor == null) return false;
            if (actor is UnityEngine.Object unityObj && unityObj == null) return false;
            if (actor is DeadDepths.AI.ZombieController z && z.IsDead) return false;
            return true;
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
            if (!IsActorValid(slowable))
            {
                if (slowable != null) activePatchesByActor.Remove(slowable);
                return false;
            }
            if (!activePatchesByActor.TryGetValue(slowable, out var set)) return false;
            set.RemoveWhere(p => p == null);
            if (set.Count == 0)
            {
                activePatchesByActor.Remove(slowable);
                return false;
            }
            return true;
        }

        private void Update()
        {
            if (actorsInsideThisPatch.Count > 0)
            {
                PruneStaleActors();
            }
        }

        private void PruneStaleActors()
        {
            if (actorsInsideThisPatch.Count == 0) return;
            staleActorsBuffer.Clear();
            foreach (var actor in actorsInsideThisPatch)
            {
                if (!IsActorValid(actor))
                {
                    staleActorsBuffer.Add(actor);
                }
            }
            for (int i = 0; i < staleActorsBuffer.Count; i++)
            {
                UnregisterActorExit(staleActorsBuffer[i]);
            }
            staleActorsBuffer.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var slowable = other.GetComponentInParent<IMudSlowable>();
            if (IsActorValid(slowable))
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
            if (!IsActorValid(slowable)) return;
            PruneStaleActors();
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
            if (ReferenceEquals(slowable, null)) return;
            actorsInsideThisPatch.Remove(slowable);

            bool aliveUnityObj = !(slowable is UnityEngine.Object unityObj && unityObj == null);

            if (activePatchesByActor.TryGetValue(slowable, out var set))
            {
                set.Remove(this);
                set.RemoveWhere(p => p == null);
                if (set.Count == 0)
                {
                    activePatchesByActor.Remove(slowable);
                    if (aliveUnityObj) slowable.SetTerrainSpeedMultiplier(1.0f);
                }
            }
            else if (aliveUnityObj)
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
