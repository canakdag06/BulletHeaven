using System.Collections.Generic;
using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Lobs <see cref="WeaponBase.Amount"/> bottles at living enemies inside searchRadius.
    /// A bottle flies in an arc, then leaves a pool that ticks <see cref="WeaponBase.Damage"/> for <see cref="WeaponBase.Duration"/>.
    /// Reads: damage, cooldown, amount, spread (pool radius), speed (throw speed), duration.
    /// searchRadius, the arc and the tick interval are not scaled.
    /// </summary>
    public class ThrowableWeapon : WeaponBase
    {
        [Header("Targeting")]
        [Tooltip("Enemies farther than this from the player are ignored. Not scaled by Spread.")]
        [SerializeField, Min(0f)] private float searchRadius = 9f;
        [Tooltip("Random horizontal offset added to each landing point.")]
        [SerializeField, Min(0f)] private float landingJitter = 0.6f;

        [Header("Throw")]
        [Tooltip("Height above this weapon where the bottle starts.")]
        [SerializeField] private float throwHeight = 1.2f;
        [Tooltip("Extra height at the top of the arc. Not scaled.")]
        [SerializeField, Min(0f)] private float arcHeight = 2.2f;

        [Header("Pool")]
        [Tooltip("Seconds between damage ticks. Each tick deals Damage once. Not scaled.")]
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(0.45f, 0.75f, 1f, 0.85f);
        [SerializeField] private Color poolGizmoColor = new Color(0.3f, 0.9f, 1f, 0.9f);

        private Enemy.Enemy[] _candidates = new Enemy.Enemy[32];

        // ── Fire ──────────────────────────────────────────────────────────────

        protected override bool TryFire()
        {
            int count = CollectCandidates();
            if (count == 0) return false;

            Vector3 origin = transform.position + Vector3.up * throwHeight;
            float damage = Damage;
            float radius = Spread;
            float duration = Duration;
            float speed = Speed;
            int bottles = Amount;
            int launched = 0;

            for (int i = 0; i < bottles; i++)
            {
                Enemy.Enemy target = _candidates[Random.Range(0, count)];
                if (target == null || target.IsDead || target.InPool) continue;

                if (PoolManager.Instance.Get(EPoolType.Bottle) is not ThrownBottle bottle) continue;

                Vector2 jitter = Random.insideUnitCircle * landingJitter;
                Vector3 landing = target.transform.position;
                landing.x += jitter.x;
                landing.z += jitter.y;

                bottle.Launch(origin, landing, speed, arcHeight, damage, radius, duration, tickInterval, enemyLayer);
                launched++;
            }

            return launched > 0;
        }

        private int CollectCandidates()
        {
            IReadOnlyList<Enemy.Enemy> active = EnemyRegistry.Active;
            Vector3 origin = transform.position;
            float radiusSqr = searchRadius * searchRadius;
            int count = 0;

            for (int i = 0; i < active.Count; i++)
            {
                Enemy.Enemy enemy = active[i];
                if (enemy == null || enemy.IsDead || enemy.InPool) continue;
                if (FlatSqrDistance(origin, enemy.transform.position) > radiusSqr) continue;

                if (count == _candidates.Length)
                    System.Array.Resize(ref _candidates, _candidates.Length * 2);

                _candidates[count++] = enemy;
            }

            return count;
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!drawRangeGizmo) return;

            Gizmos.color = rangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            if (Application.isPlaying) return;

            float radius = GetPoolRadius();
            if (radius <= 0f) return;

            Gizmos.color = poolGizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private float GetPoolRadius()
        {
            if (Application.isPlaying && Data != null)
                return Spread;

            if (Definition == null) return 0f;

            WeaponLevelData data = Definition.GetLevel(Mathf.Max(1, Level));
            return data != null ? data.spread : 0f;
        }
    }
}
