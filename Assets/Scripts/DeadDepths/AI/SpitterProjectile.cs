using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Interactive;
using DeadDepths.Player;
using UTS.Audio;

namespace DeadDepths.AI
{
    /// <summary>
    /// Toxic projectile spewed by the Spitter zombie archetype.
    /// ASD-STE100: This toxic projectile flies toward a captured position and damages the player.
    /// Follows Section 5.3 of the specification.
    /// </summary>
    public class SpitterProjectile : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public float Speed = 18f;
            public float Damage = 15f;
            public float MaxLifetime = 4.0f;
            public float Radius = 0.35f;
        }
        #endregion

        [SerializeField] private Config projConfig = new Config();
        public Config ProjConfig => projConfig;

        private Vector3 moveDirection;
        private float spawnTime;
        private bool isLaunched = false;

        public void Launch(Vector3 targetWorldPosition)
        {
            moveDirection = (targetWorldPosition - transform.position).normalized;
            if (moveDirection.sqrMagnitude < 0.001f) moveDirection = transform.forward;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            spawnTime = Time.time;
            isLaunched = true;
        }

        private void Update()
        {
            if (!isLaunched) return;

            float moveDist = projConfig.Speed * Time.deltaTime;
            Vector3 step = moveDirection * moveDist;

            // Spherecast to check collision before moving
            if (Physics.SphereCast(transform.position, projConfig.Radius, moveDirection, out RaycastHit hit, moveDist))
            {
                OnHit(hit.collider, hit.point);
                return;
            }

            transform.position += step;

            if (Time.time - spawnTime >= projConfig.MaxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnHit(Collider other, Vector3 hitPoint)
        {
            // 1. Check Player hit
            var player = other.GetComponentInParent<DeadDepthsPlayerController>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(projConfig.Damage);
                DestroyProjectile();
                return;
            }

            // 2. Check Breakable/Unbreakable Blocker
            var blocker = other.GetComponentInParent<BlockingObject>();
            if (blocker != null)
            {
                if (blocker.IsBreakable)
                {
                    blocker.ApplyDamage(projConfig.Damage);
                }
                DestroyProjectile();
                return;
            }

            // 3. Hit ground or boundary
            DestroyProjectile();
        }

        private void DestroyProjectile()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchHit, transform.position);
            }
            Destroy(gameObject);
        }
    }
}
