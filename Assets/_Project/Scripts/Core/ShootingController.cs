using System.Collections;
using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class ShootingController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private float weaponRange = 50f;
        [SerializeField] private float weaponDamage = 10f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visuals")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Dependencies")]
        [SerializeField] private TargetingSystem targetingSystem;

        private float _fireTimer;

        private void Update()
        {
            if (targetingSystem == null) return;
            TryShoot(targetingSystem.CurrentTarget);
        }

        private void TryShoot(Transform target)
        {
            if (target == null) return;

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                PerformHitscan(target);
                _fireTimer = fireRate;
            }
        }

        private void PerformHitscan(Transform target)
        {
            Vector3 targetCenter = target.position + Vector3.up * 1f;
            Vector3 direction = targetCenter - firePoint.position;

            if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, weaponRange, enemyLayer))
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(weaponDamage);

                if (hitEffectPrefab)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(effect, 1f);
                }
                SpawnBulletTrail(firePoint.position, hit.point);
            }
        }

        private void SpawnBulletTrail(Vector3 start, Vector3 end)
        {
            BulletTrail trail = PoolManager.Instance.Get(EPoolType.BulletTrail) as BulletTrail;
            trail?.SetUpTrail(start, end);
        }
    }
}