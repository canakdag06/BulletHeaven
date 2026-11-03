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

        [Header("Visuals")]
        [SerializeField] private MuzzleFlash muzzleFlash;

        protected override void Awake()
        {
            base.Awake();
            if (targetingSystem == null) targetingSystem = GetComponentInParent<TargetingSystem>();
            if (firePoint == null)       firePoint       = transform;
        }

        protected override bool TryFire()
        {
            Transform target = targetingSystem != null ? targetingSystem.CurrentTarget : null;
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (requireFacingTarget && !IsFacingTarget(target)) return false;

            Vector3 origin    = firePoint.position;
            Vector3 direction = target.position + Vector3.up * aimHeight - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

            int   amount = Amount;
            float spread = amount > 1 ? Data.spreadAngle : 0f;
            float step   = amount > 1 ? spread / (amount - 1) : 0f;
            bool  fired  = false;

            for (int i = 0; i < amount; i++)
            {
                Vector3 shotDirection = Quaternion.AngleAxis(-spread * 0.5f + step * i, Vector3.up) * direction;

                if (PoolManager.Instance.Get(EPoolType.Projectile) is not Projectile projectile) continue;

                projectile.Launch(origin, shotDirection, ProjectileSpeed, Damage, Range, enemyLayer);
                fired = true;
            }

            if (fired && muzzleFlash) muzzleFlash.Play();
            return fired;
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
