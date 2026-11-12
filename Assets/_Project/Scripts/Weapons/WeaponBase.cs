using UnityEngine;
using BulletHeaven.Core;
using BulletHeaven.Progression;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Shared cooldown loop and stat scaling for every weapon. Subclasses only implement <see cref="TryFire"/>.
    /// Lives on the player or one of its children; finds <see cref="PlayerStats"/> and <see cref="PlayerHealth"/> in parents.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] private WeaponDefinitionSO definition;
        [SerializeField, Min(1)] private int startLevel = 1;

        public WeaponDefinitionSO Definition => definition;
        public int  Level      { get; private set; }
        public bool IsMaxLevel => definition == null || Level >= definition.MaxLevel;

        protected WeaponLevelData Data  { get; private set; }
        protected PlayerStats     Stats { get; private set; }

        private PlayerHealth _ownerHealth;
        private float        _cooldownTimer;

        // ── Scaled values ─────────────────────────────────────────────────────

        protected float Damage          => Data.damage          * Multiplier(StatType.Damage);
        protected float Cooldown        => Mathf.Max(0.01f, Data.cooldown * Multiplier(StatType.Cooldown));
        protected float ProjectileSpeed => Data.projectileSpeed * Multiplier(StatType.ProjectileSpeed);
        protected float Range           => Data.range;
        protected int   Amount          => Mathf.Max(1, Data.amount + (Stats != null ? Mathf.RoundToInt(Stats.Get(StatType.Amount)) : 0));

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            Stats        = GetComponentInParent<PlayerStats>();
            _ownerHealth = GetComponentInParent<PlayerHealth>();

            if (definition != null)
                SetLevel(startLevel);
            else
                Debug.LogError($"[{GetType().Name}] WeaponDefinitionSO is not assigned.", this);
        }

        protected bool OwnerIsDead     => _ownerHealth != null && _ownerHealth.IsDead;
        protected bool OwnerCanAttack => _ownerHealth == null || (!_ownerHealth.IsDead && !_ownerHealth.IsInvincible);

        protected virtual void Update()
        {
            if (Data == null || !OwnerCanAttack) return;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            if (TryFire())
                _cooldownTimer = Cooldown;
        }

        // ── Public ────────────────────────────────────────────────────────────

        /// <summary>Used when the weapon is added at runtime instead of being placed on a prefab.</summary>
        public void Initialize(WeaponDefinitionSO weaponDefinition, int level = 1)
        {
            definition = weaponDefinition;
            SetLevel(level);
        }

        public void LevelUp()
        {
            if (!IsMaxLevel) SetLevel(Level + 1);
        }

        // ── Subclass API ──────────────────────────────────────────────────────

        /// <summary>Returns true if the weapon actually fired, which restarts the cooldown.</summary>
        protected abstract bool TryFire();

        // ── Private ───────────────────────────────────────────────────────────

        private void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, Mathf.Max(1, definition.MaxLevel));
            Data  = definition.GetLevel(Level);
        }

        private float Multiplier(StatType type) => Stats != null ? Stats.Get(type) : 1f;
    }
}
