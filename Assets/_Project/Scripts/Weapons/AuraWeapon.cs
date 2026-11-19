using UnityEngine;
using BulletHeaven.Core;

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
        [SerializeField, Min(0.01f)] private float visualBaseDiameter = 1f;

        private Collider[] _hits;
        private float      _visualRadius = -1f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _hits = new Collider[maxHits];
        }

        private void LateUpdate()
        {
            if (Data == null || radiusVisual == null) return;

            float radius = Area;
            if (Mathf.Approximately(radius, _visualRadius)) return;

            _visualRadius = radius;
            float scale = radius * 2f / visualBaseDiameter;
            Vector3 current = radiusVisual.localScale;
            radiusVisual.localScale = new Vector3(scale, current.y, scale);
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
    }
}
