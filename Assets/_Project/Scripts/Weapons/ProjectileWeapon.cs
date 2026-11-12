using System.Collections;
using UnityEngine;
using BulletHeaven.Core;
using BulletHeaven.Control;

namespace BulletHeaven.Weapons
{
    /// <summary>Fires pooled projectiles at the closest target picked by <see cref="TargetingSystem"/>.</summary>
    public class ProjectileWeapon : WeaponBase
    {
        [Header("Aiming")]
        [SerializeField] private Transform       firePoint;
        [SerializeField] private float           aimHeight = 1f;
        [SerializeField] private LayerMask       enemyLayer;
        [SerializeField] private TargetingSystem targetingSystem;

        [Tooltip("Only fire when the player faces the target within Max Shoot Angle.")]
        [SerializeField] private bool  requireFacingTarget;
        [SerializeField] private float maxShootAngle = 15f;

        [Header("Burst")]
        [Tooltip("Delay between projectiles in the same shot.")]
        [SerializeField, Min(0f)] private float shotInterval = 0.1f;

        [Header("Visuals")]
        [SerializeField] private MuzzleFlash muzzleFlash;

        private Coroutine _burst;

        protected override void Awake()
        {
            base.Awake();
            if (targetingSystem == null) targetingSystem = GetComponentInParent<TargetingSystem>();
            if (firePoint == null)       firePoint       = transform;
        }

        private void OnDisable()
        {
            if (_burst == null) return;
            StopCoroutine(_burst);
            _burst = null;
        }

        protected override bool TryFire()
        {
            if (_burst != null) return false;

            Transform target = targetingSystem != null ? targetingSystem.CurrentTarget : null;
            int amount = Amount;
            if (!TryLaunch(target, 0, amount)) return false;

            if (amount > 1)
                _burst = StartCoroutine(FireRemaining(target, amount));

            return true;
        }

        private IEnumerator FireRemaining(Transform target, int amount)
        {
            for (int i = 1; i < amount; i++)
            {
                yield return new WaitForSeconds(shotInterval);

                while (!OwnerCanAttack)
                {
                    if (OwnerIsDead)
                    {
                        _burst = null;
                        yield break;
                    }

                    yield return null;
                }

                TryLaunch(target, i, amount);
            }

            _burst = null;
        }

        private bool TryLaunch(Transform target, int index, int amount)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (requireFacingTarget && !IsFacingTarget(target)) return false;

            Vector3 origin    = firePoint.position;
            Vector3 direction = target.position + Vector3.up * aimHeight - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

            float spread = amount > 1 ? Data.spreadAngle : 0f;
            float step   = amount > 1 ? spread / (amount - 1) : 0f;
            Vector3 shotDirection = Quaternion.AngleAxis(-spread * 0.5f + step * index, Vector3.up) * direction;

            if (PoolManager.Instance.Get(EPoolType.Projectile) is not Projectile projectile) return false;

            projectile.Launch(origin, shotDirection, ProjectileSpeed, Damage, Range, enemyLayer);
            if (muzzleFlash) muzzleFlash.Play();
            return true;
        }

        private bool IsFacingTarget(Transform target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return true;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            return Vector3.Angle(forward, toTarget) <= maxShootAngle;
        }
    }
}
