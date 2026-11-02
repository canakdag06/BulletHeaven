using System;
using System.Collections;
using UnityEngine;

namespace BulletHeaven.Core
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Hit Flash")]
        [SerializeField] private Color  flashColor    = Color.red;
        [SerializeField] private float  flashDuration = 2f;
        [SerializeField] private int    flashCount    = 10;

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
        private bool                  isFlashing;

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
            if (IsDead || IsInvincible || isFlashing) return;

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            flashCoroutine = StartCoroutine(FlashRoutine());

            if (CurrentHealth <= 0f)
                Die();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator FlashRoutine()
        {
            isFlashing = true;

            // Longer gaps first, then shorter, so the blink speeds up.
            // Weights  n, n-1, ... 1  sum to n*(n+1)/2 and fill flashDuration.
            int weightSum = flashCount * (flashCount + 1) / 2;

            for (int i = 0; i < flashCount; i++)
            {
                float step     = flashDuration * (flashCount - i) / weightSum;
                float halfStep = step * 0.5f;

                SetTintColor(flashColor);
                yield return new WaitForSeconds(halfStep);
                SetTintColor(Color.white);
                yield return new WaitForSeconds(halfStep);
            }

            isFlashing     = false;
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
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }

            IsDead        = false;
            IsInvincible  = false;
            isFlashing    = false;
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
