using System.Collections.Generic;
using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// On each cooldown, strikes up to <see cref="WeaponBase.Amount"/> living enemies inside searchRadius.
    /// Each strike deals <see cref="WeaponBase.Damage"/> to every living enemy whose collider overlaps <see cref="WeaponBase.Area"/> around that target.
    /// Reads: damage, cooldown, amount, area. searchRadius is not scaled by stats.
    /// </summary>
    public class ThunderboltWeapon : WeaponBase
    {
        [Header("Targeting")]
        [Tooltip("Enemies farther than this from the player are ignored. Not scaled by Area.")]
        [SerializeField, Min(0f)] private float searchRadius = 10f;

        [Header("Splash")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("Max colliders checked per strike. Colliders past this count are skipped.")]
        [SerializeField, Min(1)] private int maxSplashHits = 32;

        [Header("Visuals")]
        [Tooltip("Height of the effect above the enemy's feet.")]
        [SerializeField] private float effectHeight;

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(0.6f, 0.8f, 1f, 0.8f);
        [SerializeField] private Color splashGizmoColor = new Color(1f, 0.75f, 0.2f, 0.9f);

        private Enemy.Enemy[] _candidates = new Enemy.Enemy[64];
        private Vector3[]      _splashPoints = new Vector3[8];
        private int            _splashPointCount;
        private Collider[]     _splashHits;
        private Enemy.Enemy[]  _damaged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _splashHits = new Collider[maxSplashHits];
            _damaged    = new Enemy.Enemy[maxSplashHits];
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        protected override bool TryFire()
        {
            int count = CollectCandidates();
            _splashPointCount = 0;
            if (count == 0) return false;

            int strikes = Mathf.Min(Amount, count);
            float damage = Damage;
            float radius = Area;

            for (int i = 0; i < strikes; i++)
            {
                int pick = Random.Range(i, count);
                Enemy.Enemy swapped = _candidates[i];
                _candidates[i] = _candidates[pick];
                _candidates[pick] = swapped;

                Enemy.Enemy target = _candidates[i];
                if (target == null || target.IsDead || target.InPool) continue;

                Vector3 point = target.transform.position;
                DealSplash(point, radius, damage);
                SpawnBolt(point);
                RecordSplashPoint(point);
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

        private void DealSplash(Vector3 point, float radius, float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(point, radius, _splashHits, enemyLayer, QueryTriggerInteraction.Collide);
            int damagedCount = 0;

            for (int i = 0; i < count; i++)
            {
                Enemy.Enemy enemy = _splashHits[i].GetComponentInParent<Enemy.Enemy>();
                if (enemy == null || enemy.IsDead || enemy.InPool) continue;
                if (AlreadyDamaged(enemy, damagedCount)) continue;

                _damaged[damagedCount++] = enemy;
                enemy.TakeDamage(damage);
            }
        }

        private bool AlreadyDamaged(Enemy.Enemy enemy, int damagedCount)
        {
            for (int i = 0; i < damagedCount; i++)
            {
                if (_damaged[i] == enemy) return true;
            }

            return false;
        }

        private void SpawnBolt(Vector3 point)
        {
            if (PoolManager.Instance.Get(EPoolType.Thunderbolt) is not ThunderboltStrike strike) return;

            strike.Play(point + Vector3.up * effectHeight);
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void RecordSplashPoint(Vector3 point)
        {
            if (_splashPointCount == _splashPoints.Length)
                System.Array.Resize(ref _splashPoints, _splashPoints.Length * 2);

            _splashPoints[_splashPointCount++] = point;
        }

        private void OnDrawGizmos()
        {
            if (!drawRangeGizmo) return;

            Gizmos.color = rangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            float splashRadius = GetSplashRadius();
            if (splashRadius <= 0f) return;

            Gizmos.color = splashGizmoColor;

            if (!Application.isPlaying)
            {
                Gizmos.DrawWireSphere(transform.position, splashRadius);
                return;
            }

            for (int i = 0; i < _splashPointCount; i++)
                Gizmos.DrawWireSphere(_splashPoints[i], splashRadius);
        }

        private float GetSplashRadius()
        {
            if (Application.isPlaying && Data != null)
                return Area;

            if (Definition == null) return 0f;

            WeaponLevelData data = Definition.GetLevel(Mathf.Max(1, Level));
            return data != null ? data.area : 0f;
        }
    }
}
