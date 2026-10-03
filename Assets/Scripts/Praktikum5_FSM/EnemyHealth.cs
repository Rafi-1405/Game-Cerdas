using System;
using UnityEngine;

namespace Praktikum5.FSM
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            Debug.Log($"[{gameObject.name}] HP berkurang {damage:F0} -> Sisa HP: {CurrentHealth:F0}/{maxHealth:F0}", this);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsDead)
            {
                Debug.Log($"[{gameObject.name}] Mati (Health <= 0).", this);
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        [ContextMenu("Debug: Kurangi 20 HP")]
        private void DebugTake20Damage()
        {
            TakeDamage(20f);
        }

        [ContextMenu("Debug: Set Low HP (25)")]
        private void DebugSetLowHP()
        {
            CurrentHealth = 25f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        [ContextMenu("Debug: Kill Enemy")]
        private void DebugKill()
        {
            TakeDamage(maxHealth);
        }
    }
}
