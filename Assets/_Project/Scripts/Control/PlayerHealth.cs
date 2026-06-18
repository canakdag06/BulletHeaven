using System;
using System.Collections;
using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Hit Flash")]
        [SerializeField] private Color  flashColor    = Color.red;
        [SerializeField] private float  flashDuration = 0.15f;
        [SerializeField] private int    flashCount    = 2;

        public float CurrentHealth  { get; private set; }
        public float MaxHealth      => maxHealth;
        public bool  IsDead         { get; private set; }
        public bool  IsInvincible   { get; private set; }

        /// <summary>Fired on every health change. Params: (currentHealth, maxHealth)</summary>
        public event Action<float, float> OnHealthChanged;

        private Renderer[]            renderers;
        private MaterialPropertyBlock propBlock;
        private Coroutine             flashCoroutine;
        private Animator              animator;

        private static readonly int BaseColorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId        = Shader.PropertyToID("_Color");
        private static readonly int DeathTriggerId = Animator.StringToHash("Death");

        private void Awake()
        {
            CurrentHealth = maxHealth;
            renderers     = GetComponentsInChildren<Renderer>();
            propBlock     = new MaterialPropertyBlock();
            animator      = GetComponentInChildren<Animator>();
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

        public void SetInvincible(bool value) => IsInvincible = value;

        public void TakeDamage(float amount)
        {
            if (IsDead || IsInvincible) return;

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());

            if (CurrentHealth <= 0f)
                Die();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator FlashRoutine()
        {
            float halfStep = flashDuration / (flashCount * 2);

            for (int i = 0; i < flashCount; i++)
            {
                SetTintColor(flashColor);
                yield return new WaitForSeconds(halfStep);
                SetTintColor(Color.white);
                yield return new WaitForSeconds(halfStep);
            }

            flashCoroutine = null;
        }

        private void SetTintColor(Color color)
        {
            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, color);
                propBlock.SetColor(ColorId, color);
                r.SetPropertyBlock(propBlock);
            }
        }

        private void Die()
        {
            IsDead = true;
            animator?.SetTrigger(DeathTriggerId);
            GameManager.Instance?.PlayerDied();
        }

        private void ResetHealth()
        {
            IsDead        = false;
            IsInvincible  = false;
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (animator != null)
            {
                animator.ResetTrigger(DeathTriggerId);
                animator.Rebind();
                animator.Update(0f);
            }

            SetTintColor(Color.white);
        }
    }
}
