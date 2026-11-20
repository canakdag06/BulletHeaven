using System.Collections.Generic;
using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// On each cooldown, strikes up to <see cref="WeaponBase.Amount"/> living enemies inside searchRadius.
    /// Each strike deals <see cref="WeaponBase.Damage"/> to every living enemy within <see cref="WeaponBase.Area"/> of that target.
    /// Reads: damage, cooldown, amount, area. searchRadius is not scaled by stats.
    /// </summary>
    public class LightningWeapon : WeaponBase
    {
        [Header("Targeting")]
        [Tooltip("Enemies farther than this from the player are ignored. Not scaled by Area.")]
        [SerializeField, Min(0f)] private float searchRadius = 10f;

        [Header("Visuals")]
        [Tooltip("Height of the impact above the enemy's feet.")]
        [SerializeField] private float effectHeight = 1f;
        [Tooltip("How far above the impact the bolt starts. The strike falls straight down onto the enemy.")]
        [SerializeField, Min(0f)] private float boltHeight = 12f;

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(0.6f, 0.8f, 1f, 0.8f);

        private Enemy.Enemy[] _candidates = new Enemy.Enemy[64];

        // ── Fire ──────────────────────────────────────────────────────────────

        protected override bool TryFire()
        {
            int count = CollectCandidates();
            if (count == 0) return false;

            int strikes = Mathf.Min(Amount, count);
            float damage = Damage;
            float splashSqr = Area * Area;

            for (int i = 0; i < strikes; i++)
            {
                int pick = Random.Range(i, count);
                Enemy.Enemy swapped = _candidates[i];
                _candidates[i] = _candidates[pick];
                _candidates[pick] = swapped;

                Enemy.Enemy target = _candidates[i];
                if (target == null || target.IsDead || target.InPool) continue;

                Vector3 point = target.transform.position;
                DealSplash(point, splashSqr, damage);
                SpawnBolt(point);
            }

            return true;
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
                if (enemy.IsDead || enemy.InPool) continue;
                if (FlatSqrDistance(origin, enemy.transform.position) > radiusSqr) continue;

                if (count == _candidates.Length)
                    System.Array.Resize(ref _candidates, _candidates.Length * 2);

                _candidates[count++] = enemy;
            }

            return count;
        }

        private static void DealSplash(Vector3 point, float radiusSqr, float damage)
        {
            IReadOnlyList<Enemy.Enemy> active = EnemyRegistry.Active;

            for (int i = 0; i < active.Count; i++)
            {
                Enemy.Enemy enemy = active[i];
                if (enemy.IsDead || enemy.InPool) continue;
                if (FlatSqrDistance(point, enemy.transform.position) > radiusSqr) continue;

                enemy.TakeDamage(damage);
            }
        }

        private void SpawnBolt(Vector3 point)
        {
            if (PoolManager.Instance.Get(EPoolType.Lightning) is not LightningStrike strike) return;

            Vector3 impact = point + Vector3.up * effectHeight;
            Vector3 sky = impact + Vector3.up * boltHeight;
            strike.Play(sky, impact, Area);
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
        }
    }
}
