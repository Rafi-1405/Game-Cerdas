using UnityEngine;
using Praktikum5.FSM;

namespace UTS.Player
{
    [DisallowMultipleComponent]
    public class PlayerSoundEmitter : MonoBehaviour
    {
        [Header("Sound Radius (Project Zomboid Tiles)")]
        [Tooltip("Suara saat diam (tidak bergerak): 0m")]
        [SerializeField] private float idleRadius = 0f;

        [Tooltip("Suara saat mengendap (Sneak / Ctrl): 1.8m")]
        [SerializeField] private float sneakRadius = 1.8f;

        [Tooltip("Suara saat jalan santai (Walk): 6.5m")]
        [SerializeField] private float walkRadius = 6.5f;

        [Tooltip("Suara saat lari / sprint (Run / Shift): 16m")]
        [SerializeField] private float runRadius = 16f;

        [Tooltip("Peredaman suara jika terhalang dinding/rintangan (0.5 = 50% reduksi radius)")]
        [Range(0f, 1f)]
        [SerializeField] private float wallAttenuation = 0.5f;

        [Header("References")]
        [SerializeField] private SimplePlayerController playerController;

        public float CurrentSoundRadius
        {
            get
            {
                if (playerController == null)
                {
                    playerController = GetComponent<SimplePlayerController>();
                }

                if (playerController == null || playerController.IsDead)
                {
                    return 0f;
                }

                switch (playerController.CurrentMovementState)
                {
                    case global::PlayerMovementState.Idle:
                        return idleRadius;
                    case global::PlayerMovementState.Sneak:
                        return sneakRadius;
                    case global::PlayerMovementState.Walk:
                        return walkRadius;
                    case global::PlayerMovementState.Run:
                        return runRadius;
                    default:
                        return idleRadius;
                }
            }
        }

        public float WallAttenuation => wallAttenuation;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<SimplePlayerController>();
            }
        }

        /// <summary>
        /// Mengecek apakah suara pemain dapat didengar oleh entitas (zombie) di posisi target.
        /// Memperhitungkan jarak horizontal dan peredaman dinding (50%) jika Line of Sight terhalang.
        /// </summary>
        public bool CanBeHeardBy(Vector3 listenerPosition, LayerMask obstacleMask)
        {
            float radius = CurrentSoundRadius;
            if (radius <= 0.001f)
            {
                return false;
            }

            Vector3 emitterPos = transform.position + Vector3.up * 0.9f;
            Vector3 listenerPos = listenerPosition + Vector3.up * 1.0f;

            Vector3 planarDiff = listenerPosition - transform.position;
            planarDiff.y = 0f;
            float planarDist = planarDiff.magnitude;

            // Jika jarak melebihi batas maksimal radius suara terbuka, langsung tolak
            if (planarDist > radius)
            {
                return false;
            }

            // Cek oklusi dinding/rintangan
            Vector3 rayDir = listenerPos - emitterPos;
            float rayDist = rayDir.magnitude;
            bool occluded = false;

            if (rayDist > 0.05f)
            {
                occluded = Physics.Raycast(emitterPos, rayDir.normalized, Mathf.Max(0f, rayDist - 0.05f), obstacleMask, QueryTriggerInteraction.Ignore);
            }

            // Redam radius suara 50% jika ada dinding yang menghalangi
            float effectiveRadius = occluded ? radius * (1f - wallAttenuation) : radius;

            return planarDist <= effectiveRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.4f);
            float radius = CurrentSoundRadius > 0.01f ? CurrentSoundRadius : 0.5f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
