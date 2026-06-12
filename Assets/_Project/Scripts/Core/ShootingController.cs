using System.Collections;
using UnityEngine;

namespace BulletHeaven.Control
{
    public class ShootingController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private float weaponRange = 50f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visuals")]
        [SerializeField] private LineRenderer bulletTrailPrefab;
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
                // hit.collider.GetComponent<Enemy>().TakeDamage(10);)
                // Destroy(hit.collider.gameObject);

                if (hitEffectPrefab)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(effect, 1f);
                }
                StartCoroutine(DrawLine(firePoint.position, hit.point));
            }
        }

        private IEnumerator DrawLine(Vector3 start, Vector3 end)
        {
            LineRenderer trail = Instantiate(bulletTrailPrefab);
            trail.SetPosition(0, start);
            trail.SetPosition(1, end);
            yield return new WaitForSeconds(0.05f);
            Destroy(trail.gameObject);
        }
    }
}