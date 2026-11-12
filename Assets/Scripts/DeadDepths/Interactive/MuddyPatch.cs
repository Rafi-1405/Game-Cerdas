using System;
using UnityEngine;

namespace DeadDepths.Interactive
{
    public interface IMudSlowable
    {
        void SetTerrainSpeedMultiplier(float multiplier);
    }

    /// <summary>
    /// Applies a 30% movement speed reduction (0.7 multiplier) to actors on muddy terrain.
    /// ASD-STE100: This trigger slows actors when they walk on mud.
    /// Follows Section 2.3 of the specification.
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

        private void OnTriggerEnter(Collider other)
        {
            var slowable = other.GetComponentInParent<IMudSlowable>();
            if (slowable != null)
            {
                slowable.SetTerrainSpeedMultiplier(patchConfig.MudSpeedMultiplier);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var slowable = other.GetComponentInParent<IMudSlowable>();
            if (slowable != null)
            {
                slowable.SetTerrainSpeedMultiplier(1.0f);
            }
        }
    }
}
