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
        [SerializeField] private float maxShootAngle = 15f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visuals")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Dependencies")]
        [SerializeField] private TargetingSystem targetingSystem;

        private float         _fireTimer;
        private PlayerHealth  _playerHealth;

        private void Awake()
        {
            _playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (targetingSystem == null) return;
            if (_playerHealth != null && (_playerHealth.IsDead || _playerHealth.IsInvincible)) return;
            TryShoot(targetingSystem.CurrentTarget);
        }

        private void TryShoot(Transform target)
        {
            if (target == null) return;
            if (!IsFacingTarget(target)) return;

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                PerformHitscan(target);
                _fireTimer = fireRate;
            }
        }

        private bool IsFacingTarget(Transform target)
        {
            Vector3 directionToTarget = target.position - transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude < 0.01f) return true; // if the target is very close, we consider it as facing

            Vector3 forward = transform.forward;
            forward.y = 0f;

            return Vector3.Angle(forward, directionToTarget) <= maxShootAngle; // if the angle is less than the max shoot angle, we consider it as facing
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
                SpawnMuzzleFlash();
                SpawnBulletTrail(firePoint.position, hit.point);
            }
        }

        private void SpawnBulletTrail(Vector3 start, Vector3 end)
        {
            BulletTrail trail = PoolManager.Instance.Get(EPoolType.BulletTrail) as BulletTrail;
            trail?.SetUpTrail(start, end);
        }

        private void SpawnMuzzleFlash()
        {
            MuzzleFlash flash = PoolManager.Instance.Get(EPoolType.MuzzleFlash) as MuzzleFlash;
            flash?.Play(firePoint.position, firePoint.rotation);
        }
    }
}