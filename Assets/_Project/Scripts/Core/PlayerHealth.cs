using System;
using System.Collections;
using UnityEngine;
using BulletHeaven.Progression;

namespace BulletHeaven.Core
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField, Min(0.1f)] private float regenTickInterval = 1f;

        [Header("Hit Flash")]
        [SerializeField] private Color  flashColor    = Color.red;
        [SerializeField] private float  flashDuration = 2f;
        [SerializeField] private int    flashCount    = 10;

        public float CurrentHealth  { get; private set; }
        public float MaxHealth      => Mathf.Max(1f, stats.Get(StatType.MaxHealth));
        public float Armor          => Mathf.Max(0f, stats.Get(StatType.Armor));
        public float Regen          => Mathf.Max(0f, stats.Get(StatType.Regen));
        public bool  IsDead         { get; private set; }
        public bool  IsInvincible   { get; private set; }

        /// <summary>Fired on every health change. Params: (currentHealth, maxHealth)</summary>
        public event Action<float, float> OnHealthChanged;

        private Renderer[]            renderers;
        private MaterialPropertyBlock propBlock;
        private Coroutine             flashCoroutine;
        private Animator              animator;
        private bool                  isFlashing;
        private PlayerStats           stats;
        private float                 lastMaxHealth;
        private float                 regenTimer;

        private static readonly int BaseColorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId        = Shader.PropertyToID("_Color");
        private static readonly int DeathTriggerId = Animator.StringToHash("Death");

        private void Awake()
        {
            stats         = GetComponent<PlayerStats>();
            CurrentHealth = MaxHealth;
            lastMaxHealth = MaxHealth;
            renderers     = GetComponentsInChildren<Renderer>();
            propBlock     = new MaterialPropertyBlock();
            animator      = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += ResetHealth;

            stats.OnStatChanged += HandleStatChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= ResetHealth;

            if (stats != null)
                stats.OnStatChanged -= HandleStatChanged;
        }

        private void Update()
        {
            TickRegen();
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        public void SetInvincible(bool value) => IsInvincible = value;

        public void TakeDamage(float amount)
        {
            if (IsDead || IsInvincible || isFlashing) return;

            float currentArmor = Armor;
            if (currentArmor > 0f)
                amount = Mathf.Max(1f, amount - currentArmor);

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            flashCoroutine = StartCoroutine(FlashRoutine());

            if (CurrentHealth <= 0f)
                Die();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void TickRegen()
        {
            float perSecond = Regen;
            if (IsDead || perSecond <= 0f || CurrentHealth >= MaxHealth)
            {
                regenTimer = 0f;
                return;
            }

            regenTimer += Time.deltaTime;
            if (regenTimer < regenTickInterval) return;

            CurrentHealth = Mathf.Min(CurrentHealth + perSecond * regenTimer, MaxHealth);
            regenTimer    = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        // Gaining max health also heals by the same amount; losing it only clamps.
        private void HandleStatChanged(StatType type)
        {
            if (type != StatType.MaxHealth) return;

            float newMax = MaxHealth;
            float delta  = newMax - lastMaxHealth;
            lastMaxHealth = newMax;

            if (!IsDead && delta > 0f) CurrentHealth += delta;
            CurrentHealth = Mathf.Min(CurrentHealth, newMax);
            OnHealthChanged?.Invoke(CurrentHealth, newMax);
        }

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
            regenTimer    = 0f;
            lastMaxHealth = MaxHealth;
            CurrentHealth = lastMaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, lastMaxHealth);

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
