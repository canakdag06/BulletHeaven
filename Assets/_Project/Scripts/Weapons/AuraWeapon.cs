using UnityEngine;
using BulletHeaven.Core;
using BulletHeaven.Progression;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Damages every living enemy within <see cref="WeaponBase.Area"/> of the player on each cooldown.
    /// Reads: damage, cooldown, area (radius).
    /// </summary>
    public class AuraWeapon : WeaponBase
    {
        [Header("Detection")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("Max colliders checked per pulse. Enemies past this count are skipped that pulse.")]
        [SerializeField, Min(1)] private int maxHits = 64;

        [Header("Visuals")]
        [Tooltip("Child scaled on X/Z to match the radius. Leave empty for no visual.")]
        [SerializeField] private Transform radiusVisual;
        [Tooltip("Diameter of the visual mesh at scale 1 (Unity cylinder/quad = 1).")]
        [SerializeField, Min(0.01f)] private float visualBaseDiameter;
        [Tooltip("Kept at a fixed world rotation so the effect does not spin with the player.")]
        [SerializeField] private Transform visualRoot;

        private Collider[] _hits;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _hits = new Collider[maxHits];
        }

        private void OnEnable()
        {
            if (Stats != null) Stats.OnStatChanged += OnStatChanged;
            UpdateVisualScale();
        }

        private void OnDisable()
        {
            if (Stats != null) Stats.OnStatChanged -= OnStatChanged;
        }

        private void LateUpdate()
        {
            if (visualRoot != null)
                visualRoot.rotation = Quaternion.identity;
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        protected override bool TryFire()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, Area, _hits, enemyLayer, QueryTriggerInteraction.Collide);
            float damage = Damage;

            for (int i = 0; i < count; i++)
            {
                IDamageable damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;

                damageable.TakeDamage(damage);
            }

            return true;
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        protected override void OnLevelChanged() => UpdateVisualScale();

        private void OnStatChanged(StatType type)
        {
            if (type == StatType.Area) UpdateVisualScale();
        }

        private void UpdateVisualScale()
        {
            if (Data == null || radiusVisual == null) return;

            float scale = Area * 2f / visualBaseDiameter;
            Vector3 current = radiusVisual.localScale;
            radiusVisual.localScale = new Vector3(scale, current.y, scale);
        }
    }
}
