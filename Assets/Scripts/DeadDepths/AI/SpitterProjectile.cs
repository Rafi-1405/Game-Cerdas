using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;
using DeadDepths.UI;
using UTS.Audio;

namespace DeadDepths.AI
{
    /// <summary>
    /// Toxic projectile spewed by the Spitter zombie archetype.
    /// ASD-STE100: This toxic projectile flies toward a captured position and damages the player.
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
        private Transform ownerRoot;
        private Material runtimeMaterial;

        public void RegisterRuntimeMaterial(Material mat)
        {
            runtimeMaterial = mat;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial);
                else DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        public void Launch(Vector3 targetWorldPosition, Transform owner = null)
        {
            ownerRoot = owner;
            moveDirection = (targetWorldPosition - transform.position).normalized;
            if (moveDirection.sqrMagnitude < 0.001f) moveDirection = transform.forward;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            spawnTime = Time.time;
            isLaunched = true;
        }

        private void Update()
        {
            if (!isLaunched) return;
            if (Time.timeScale <= 0f || (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible))
            {
                return;
            }

            float moveDist = projConfig.Speed * Time.deltaTime;
            Vector3 step = moveDirection * moveDist;

            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,
                projConfig.Radius,
                moveDirection,
                moveDist,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (ownerRoot != null && (hit.collider.transform == ownerRoot || hit.collider.transform.IsChildOf(ownerRoot)))
                    {
                        continue;
                    }
                    if (hit.collider.GetComponentInParent<ZombieController>() != null)
                    {
                        continue;
                    }

                    OnHit(hit.collider, hit.point);
                    return;
                }
            }

            transform.position += step;

            if (Time.time - spawnTime >= projConfig.MaxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnHit(Collider other, Vector3 hitPoint)
        {
            CombatVfxManager.Instance?.PlayAcidSplash(hitPoint == Vector3.zero ? transform.position : hitPoint);

            var player = other.GetComponentInParent<DeadDepthsPlayerController>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(projConfig.Damage);
                DestroyProjectile();
                return;
            }

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
