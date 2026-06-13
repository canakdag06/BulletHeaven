using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class TargetingSystem : MonoBehaviour
    {
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float targetCheckInterval = 0.2f;

        public Transform CurrentTarget { get; private set; }

        private float _targetCheckTimer;

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
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

            float closestDistance = Mathf.Infinity;
            Transform closestEnemy = null;

            foreach (Collider enemy in enemiesInRange)
            {
                if (enemy.TryGetComponent(out IDamageable damageable) && damageable.IsDead) continue;

                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy < closestDistance)
                {
                    closestDistance = distanceToEnemy;
                    closestEnemy = enemy.transform;
                }
            }

            CurrentTarget = closestEnemy;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
