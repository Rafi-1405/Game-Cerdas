using UnityEngine;

namespace DeadDepths.Player
{
    /// <summary>
    /// Drives upper-body and head IK look-at toward the player's cursor aim point while holding RMB.
    /// Attached to PlayerModel (same GameObject as Animator with m_IKPass = 1).
    /// Zeroes IK weight cleanly during Punch, Dodge, and Dead states.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAimIK : MonoBehaviour
    {
        private Animator animator;
        private DeadDepthsPlayerController player;
        private float currentWeight = 0f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            player = GetComponentInParent<DeadDepthsPlayerController>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (player == null) player = GetComponentInParent<DeadDepthsPlayerController>();
            if (animator == null || player == null) return;

            bool shouldAimIK = player.IsAiming &&
                               !player.IsDead &&
                               !player.IsDodging &&
                               !player.IsPunchingAnimActive &&
                               Time.timeScale > 0f;

            float targetWeight = shouldAimIK ? 1.0f : 0.0f;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 12f);

            if (currentWeight <= 0.001f)
            {
                animator.SetLookAtWeight(0f);
                return;
            }

            animator.SetLookAtWeight(currentWeight, currentWeight * 0.45f, currentWeight * 0.85f, 0.25f, 0.55f);
            animator.SetLookAtPosition(player.LastValidAimPoint);
        }
    }
}
