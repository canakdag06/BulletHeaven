using System.Collections.Generic;
using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Control
{
    public class TargetingSystem : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float targetCheckInterval = 0.2f;

        public Transform CurrentTarget => IsValidTarget(_currentTarget) ? _currentTarget.transform : null;

        private Enemy.Enemy _currentTarget;
        private float       _targetCheckTimer;

        private void Update()
        {
            _targetCheckTimer -= Time.deltaTime;
            if (_targetCheckTimer <= 0f)
            {
                FindClosestTarget();
                _targetCheckTimer = targetCheckInterval;
            }
        }

        private void FindClosestTarget()
        {
            IReadOnlyList<Enemy.Enemy> enemies = EnemyRegistry.Active;
            Vector3 position = transform.position;

            float closestSqrDistance = detectionRadius * detectionRadius;
            Enemy.Enemy closestEnemy = null;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy.Enemy enemy = enemies[i];
                if (enemy.IsDead) continue;

                float sqrDistance = (position - enemy.transform.position).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance) continue;
                if (!HasLineOfSight(enemy.transform)) continue;

                closestSqrDistance = sqrDistance;
                closestEnemy = enemy;
            }

            _currentTarget = closestEnemy;
        }

        private static bool IsValidTarget(Enemy.Enemy target)
        {
            return target != null && !target.InPool && !target.IsDead;
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 origin    = transform.position + Vector3.up;
            Vector3 targetPos = target.position    + Vector3.up;
            Vector3 direction = targetPos - origin;
            return !Physics.Raycast(origin, direction.normalized, direction.magnitude, obstacleMask);
        }
    }
}
