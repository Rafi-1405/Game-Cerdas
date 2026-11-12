using System;
using UnityEngine;
using DeadDepths.Audio;
using UTS.Audio;

namespace DeadDepths.Interactive
{
    /// <summary>
    /// Represents an environmental blocking obstacle (breakable or unbreakable).
    /// ASD-STE100: This object blocks movement and projectiles. Breakable objects can be destroyed.
    /// Follows Section 2.5 of the specification.
    /// </summary>
    public class BlockingObject : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public bool IsBreakable = false;
            public float MaxHealth = 50f;
            public Color FlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        }
        #endregion

        [SerializeField] private Config blockerConfig = new Config();
        public Config BlockerConfig => blockerConfig;

        public bool IsBreakable => blockerConfig.IsBreakable;
        public float CurrentHealth { get; private set; }
        public bool IsDestroyed { get; private set; } = false;

        private Renderer objRenderer;
        private MaterialPropertyBlock propBlock;
        private Color originalColor = Color.white;

        private void Awake()
        {
            CurrentHealth = blockerConfig.MaxHealth;
            objRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();

            if (objRenderer != null && objRenderer.sharedMaterial != null)
            {
                if (objRenderer.sharedMaterial.HasProperty("_BaseColor"))
                    originalColor = objRenderer.sharedMaterial.GetColor("_BaseColor");
                else if (objRenderer.sharedMaterial.HasProperty("_Color"))
                    originalColor = objRenderer.sharedMaterial.GetColor("_Color");
            }
        }

        public void ApplyDamage(float damage)
        {
            if (!blockerConfig.IsBreakable || IsDestroyed) return;

            CurrentHealth -= Mathf.Max(0f, damage);

            // Play hit audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchHit, transform.position);
            }

            // Visual hit flash
            StartCoroutine(HitFlashRoutine());

            if (CurrentHealth <= 0f)
            {
                DestroyBlocker();
            }
        }

        private System.Collections.IEnumerator HitFlashRoutine()
        {
            if (objRenderer != null)
            {
                objRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", blockerConfig.FlashColor);
                propBlock.SetColor("_Color", blockerConfig.FlashColor);
                objRenderer.SetPropertyBlock(propBlock);

                yield return new WaitForSeconds(0.08f);

                propBlock.SetColor("_BaseColor", originalColor);
                propBlock.SetColor("_Color", originalColor);
                objRenderer.SetPropertyBlock(propBlock);
            }
        }

        private void DestroyBlocker()
        {
            IsDestroyed = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.BodyFall, transform.position);
            }

            // Disable colliders immediately so projectiles and actors pass through
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
