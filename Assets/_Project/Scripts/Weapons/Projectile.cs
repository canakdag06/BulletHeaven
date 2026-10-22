using UnityEngine;
using BulletHeaven.Core;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : PoolableBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactEffectLifetime = 1f;

    public override EPoolType PoolType => EPoolType.Projectile;

    private TrailRenderer _trail;
    private Vector3       _direction;
    private float         _speed;
    private float         _damage;
    private float         _remainingDistance;
    private LayerMask     _hitLayers;
    private bool          _hasHit;

    protected override void Awake()
    {
        base.Awake();

        // Kinematic rigidbody so trigger events fire even against enemy colliders without a rigidbody.
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        GetComponent<Collider>().isTrigger = true;
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, float damage, float maxDistance, LayerMask hitLayers)
    {
        _direction         = direction.normalized;
        _speed             = speed;
        _damage            = damage;
        _remainingDistance = maxDistance;
        _hitLayers         = hitLayers;
        _hasHit            = false;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(_direction));
        _trail?.Clear();
    }

    private void Update()
    {
        float step = _speed * Time.deltaTime;
        transform.position += _direction * step;

        _remainingDistance -= step;
        if (_remainingDistance <= 0f)
            Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || InPool) return;
        if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;

        _hasHit = true;
        damageable.TakeDamage(_damage);
        SpawnImpactEffect(other.ClosestPoint(transform.position));
        Release();
    }

    private void SpawnImpactEffect(Vector3 point)
    {
        if (!impactEffectPrefab) return;

        GameObject effect = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(-_direction));
        Destroy(effect, impactEffectLifetime);
    }
}
