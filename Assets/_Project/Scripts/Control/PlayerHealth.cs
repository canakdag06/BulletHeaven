using System;
using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth     => maxHealth;
        public bool  IsDead        { get; private set; }

        /// <summary>Fired on every health change. Params: (currentHealth, maxHealth)</summary>
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += ResetHealth;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= ResetHealth;
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
                Die();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Die()
        {
            IsDead = true;
            GameManager.Instance?.PlayerDied();
        }

        private void ResetHealth()
        {
            IsDead        = false;
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
