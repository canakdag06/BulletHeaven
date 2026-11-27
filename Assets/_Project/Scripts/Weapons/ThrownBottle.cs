using UnityEngine;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Pooled bottle. Flies in an arc, spawns an <see cref="ElectricZone"/> on landing, then returns to the pool.
    /// The mesh is a child of this object. Local +Z points along the throw.
    /// </summary>
    public class ThrownBottle : PoolableBehaviour
    {
        public override EPoolType PoolType => EPoolType.Bottle;

        [Tooltip("Shortest flight, so a nearby landing still shows the arc.")]
        [SerializeField, Min(0.05f)] private float minFlightTime = 0.18f;
        [Tooltip("Longest flight. A far landing is clamped to this.")]
        [SerializeField, Min(0.05f)] private float maxFlightTime = 0.9f;

        private Vector3   _origin;
        private Vector3   _landing;
        private float     _arcHeight;
        private float     _flightTime;
        private float     _elapsed;
        private float     _damage;
        private float     _radius;
        private float     _duration;
        private float     _tickInterval;
        private LayerMask _enemyLayer;
        private bool      _flying;

        // ── Launch ────────────────────────────────────────────────────────────

        public void Launch(
            Vector3 origin,
            Vector3 landing,
            float speed,
            float arcHeight,
            float damage,
            float radius,
            float duration,
            float tickInterval,
            LayerMask enemyLayer)
        {
            _origin       = origin;
            _landing      = landing;
            _arcHeight    = arcHeight;
            _damage       = damage;
            _radius       = radius;
            _duration     = duration;
            _tickInterval = tickInterval;
            _enemyLayer   = enemyLayer;
            _elapsed      = 0f;
            _flying       = true;

            float distance = FlatDistance(origin, landing);
            float flightSpeed = Mathf.Max(0.01f, speed);
            float longest = Mathf.Max(minFlightTime, maxFlightTime);
            _flightTime = Mathf.Clamp(distance / flightSpeed, minFlightTime, longest);

            Place(0f);
        }

        public override void OnGet()
        {
            base.OnGet();
            _flying = false;
        }

        public override void OnRelease()
        {
            _flying = false;
            base.OnRelease();
        }

        // ── Flight ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_flying || InPool) return;

            _elapsed += Time.deltaTime;
            float t = _flightTime <= 0f ? 1f : Mathf.Clamp01(_elapsed / _flightTime);
            Place(t);

            if (t >= 1f)
                Land();
        }

        private void Place(float t)
        {
            Vector3 position = Sample(t);
            Vector3 ahead = Sample(Mathf.Min(1f, t + 0.05f));
            Vector3 direction = ahead - position;

            if (direction.sqrMagnitude < 0.0001f)
                direction = _landing - _origin;

            if (direction.sqrMagnitude < 0.0001f || Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.999f)
                direction = Vector3.forward;

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
        }

        private Vector3 Sample(float t)
        {
            Vector3 position = Vector3.Lerp(_origin, _landing, t);
            position.y += _arcHeight * 4f * t * (1f - t);
            return position;
        }

        private void Land()
        {
            _flying = false;

            if (PoolManager.Instance.Get(EPoolType.ElectricArea) is ElectricZone zone)
                zone.Ignite(_landing, _radius, _damage, _duration, _tickInterval, _enemyLayer);

            Release();
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
