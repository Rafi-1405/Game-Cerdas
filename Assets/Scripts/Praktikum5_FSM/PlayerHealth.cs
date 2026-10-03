using System;
using UnityEngine;

namespace Praktikum5.FSM
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

            Debug.Log($"[Player] Menerima serangan {damage:F0} damage! Sisa HP: {CurrentHealth:F0}/{maxHealth:F0}", this);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Debug.LogWarning("[Player] Player Kalah / Mati!", this);
            }
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
