using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class TargetingSystem : MonoBehaviour
    {
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float targetCheckInterval = 0.2f;

        public Transform CurrentTarget => IsActiveTarget(_currentTarget) ? _currentTarget : null;

        private Transform _currentTarget;
        private float     _targetCheckTimer;
        private readonly Collider[] _overlapBuffer = new Collider[32];

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
            int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _overlapBuffer, enemyLayer);

            float closestSqrDistance = Mathf.Infinity;
            Transform closestEnemy = null;

            for (int i = 0; i < count; i++)
            {
                Collider enemy = _overlapBuffer[i];
                if (!IsActiveTarget(enemy.transform)) continue;
                if (enemy.TryGetComponent(out IDamageable damageable) && damageable.IsDead) continue;
                if (!HasLineOfSight(enemy.transform)) continue;

                float sqrDistance = (transform.position - enemy.transform.position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestEnemy = enemy.transform;
                }
            }

            _currentTarget = closestEnemy;
        }

        private static bool IsActiveTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
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
