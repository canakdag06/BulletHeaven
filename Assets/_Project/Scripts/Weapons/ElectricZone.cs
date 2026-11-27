using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Pooled ground pool. Ticks damage on an interval for a duration, then returns to the pool.
    /// The whole effect scales so its world radius matches the radius passed in.
    /// </summary>
    public class ElectricZone : PoolableBehaviour
    {
        public override EPoolType PoolType => EPoolType.ElectricArea;

        [Tooltip("Effect root. Its prefab scale is the size at visualBaseRadius.")]
        [SerializeField] private Transform visual;
        [Tooltip("World radius of the effect when its scale is the prefab scale.")]
        [SerializeField, Min(0.01f)] private float visualBaseRadius = 1.5f;
        [Tooltip("Max colliders checked per tick. Colliders past this count are skipped.")]
        [SerializeField, Min(1)] private int maxHits = 32;

        private ParticleSystem[] _systems;
        private Collider[]       _hits;
        private Enemy.Enemy[]    _damaged;
        private Vector3          _baseScale = Vector3.one;

        private float     _radius;
        private float     _damage;
        private float     _timeLeft;
        private float     _tickIn;
        private float     _tickInterval;
        private LayerMask _enemyLayer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (visual == null)
                visual = transform;

            _baseScale = visual.localScale;
            _systems   = GetComponentsInChildren<ParticleSystem>(true);
            _hits      = new Collider[maxHits];
            _damaged   = new Enemy.Enemy[maxHits];
        }

        public override void OnGet()
        {
            base.OnGet();
            _timeLeft = 0f;
            StopEffects();
        }

        public override void OnRelease()
        {
            _timeLeft = 0f;
            StopEffects();
            base.OnRelease();
        }

        /// <summary>
        /// Places the pool and deals its first tick immediately.
        /// <paramref name="radius"/> is the world radius. Damage is dealt once per tick.
        /// </summary>
        public void Ignite(Vector3 position, float radius, float damage, float duration, float tickInterval, LayerMask enemyLayer)
        {
            transform.position = position;
            _radius       = Mathf.Max(0f, radius);
            _damage       = damage;
            _tickInterval = Mathf.Max(0.05f, tickInterval);
            _enemyLayer   = enemyLayer;
            _timeLeft     = duration;
            _tickIn       = _tickInterval;

            ApplyScale(_radius);
            RestartEffects();
            Tick();

            if (_timeLeft <= 0f)
                Release();
        }

        private void Update()
        {
            if (_timeLeft <= 0f || InPool) return;

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                Release();
                return;
            }

            _tickIn -= Time.deltaTime;
            if (_tickIn > 0f) return;

            _tickIn += _tickInterval;
            Tick();
        }

        // ── Damage ────────────────────────────────────────────────────────────

        private void Tick()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _radius, _hits, _enemyLayer, QueryTriggerInteraction.Collide);
            int damagedCount = 0;

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                Enemy.Enemy enemy = hit.GetComponentInParent<Enemy.Enemy>();
                if (enemy == null || enemy.IsDead || enemy.InPool) continue;
                if (AlreadyDamaged(enemy, damagedCount)) continue;

                _damaged[damagedCount++] = enemy;
                enemy.TakeDamage(_damage);
            }
        }

        private bool AlreadyDamaged(Enemy.Enemy enemy, int damagedCount)
        {
            for (int i = 0; i < damagedCount; i++)
            {
                if (_damaged[i] == enemy) return true;
            }

            return false;
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        private void ApplyScale(float radius)
        {
            if (visual == null) return;

            float scale = radius / visualBaseRadius;
            visual.localScale = _baseScale * scale;
        }

        private void RestartEffects()
        {
            if (_systems == null) return;

            for (int i = 0; i < _systems.Length; i++)
            {
                ParticleSystem ps = _systems[i];
                if (ps == null) continue;

                ParticleSystem.MainModule main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false);
            }
        }

        private void StopEffects()
        {
            if (_systems == null) return;

            for (int i = 0; i < _systems.Length; i++)
            {
                if (_systems[i] == null) continue;
                _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || InPool || _radius <= 0f) return;

            Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
