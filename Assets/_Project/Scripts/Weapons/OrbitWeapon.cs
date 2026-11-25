using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Bodies orbit the player for <see cref="WeaponBase.Duration"/>, then hide for <see cref="WeaponBase.Cooldown"/>.
    /// A touch damages an enemy once. The body must leave and touch again to damage it again.
    /// Reads: damage, cooldown, amount, spread (radius), speed (degrees per second), duration.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class OrbitWeapon : WeaponBase
    {
        [Header("Orbit")]
        [Tooltip("Body kept on this prefab. Copied once each time Amount grows. Shown for Duration, hidden for Cooldown.")]
        [SerializeField] private OrbitOrbiter orbiterTemplate;
        [Tooltip("Height above the player's feet. Not scaled by stats.")]
        [SerializeField] private float orbitHeight;

        [Header("Damage")]
        [SerializeField] private LayerMask enemyLayer;

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(0.95f, 0.85f, 0.4f, 0.9f);

        private Transform      _anchor;
        private OrbitOrbiter[] _orbiters = new OrbitOrbiter[4];
        private int            _orbiterCount;
        private int            _slotCount;
        private float          _angle;
        private float          _visibleTime;
        private bool           _passing;
        private bool           _shown;
        private bool           _awaitingCooldown;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (orbiterTemplate == null)
            {
                Debug.LogError($"[{GetType().Name}] Orbiter template is not assigned.", this);
                return;
            }

            // Kept out of the player hierarchy. The player rigidbody is not kinematic, and these
            // kinematic bodies must not be parented under it. The root never rotates, so yaw cannot spin the ring.
            _anchor = new GameObject(name + " Orbit").transform;
            orbiterTemplate.transform.SetParent(_anchor, false);
            orbiterTemplate.gameObject.SetActive(false);
            Adopt(orbiterTemplate);
        }

        private void OnDisable() => SetShown(false);

        private void OnDestroy()
        {
            if (_anchor != null)
                Destroy(_anchor.gameObject);
        }

        protected override void Update()
        {
            if (Data != null && AnyOrbiterActive())
                _angle = Mathf.Repeat(_angle + Speed * Time.deltaTime, 360f);

            if (_passing && Data != null)
            {
                if (OwnerCanAttack)
                    TickPass();
                else
                    SetShown(false);
            }

            base.Update();
        }

        private void FixedUpdate()
        {
            // Also runs while bodies shrink out after a pass, so they keep following the player.
            if (_slotCount <= 0) return;

            PlaceActive(teleport: false);
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a pass. Returns true only when that pass ends, so the base cooldown is the time the bodies stay hidden.
        /// </summary>
        protected override bool TryFire()
        {
            if (_awaitingCooldown)
            {
                _awaitingCooldown = false;
                return true;
            }

            if (_passing || orbiterTemplate == null || _anchor == null)
                return false;

            StartPass();
            return false;
        }

        private void StartPass()
        {
            _slotCount = Amount;
            EnsureOrbiters(_slotCount);
            if (_orbiterCount <= 0) return;

            _visibleTime = 0f;
            _passing     = true;
            SetShown(true);
        }

        private void TickPass()
        {
            _visibleTime += Time.deltaTime;

            if (_visibleTime >= Duration)
            {
                FinishPass();
                return;
            }

            if (!_shown)
                SetShown(true);
        }

        private void FinishPass()
        {
            _passing          = false;
            _awaitingCooldown = true;
            SetShown(false);
        }

        // ── Orbiters ──────────────────────────────────────────────────────────

        private void Adopt(OrbitOrbiter orbiter)
        {
            if (_orbiterCount == _orbiters.Length)
                System.Array.Resize(ref _orbiters, _orbiters.Length * 2);

            orbiter.Bind(this);
            _orbiters[_orbiterCount++] = orbiter;
        }

        // The pool is for shared, short-lived objects. These few bodies live with the weapon and only toggle.
        private void EnsureOrbiters(int count)
        {
            while (_orbiterCount < count)
            {
                OrbitOrbiter copy = Instantiate(orbiterTemplate, _anchor);
                copy.name = "Orbiter " + (_orbiterCount + 1);
                copy.gameObject.SetActive(false);
                Adopt(copy);
            }
        }

        private void SetShown(bool shown)
        {
            if (_anchor == null || _shown == shown) return;

            _shown = shown;
            int count = shown ? _slotCount : 0;

            for (int i = 0; i < _orbiterCount; i++)
            {
                if (i < count) _orbiters[i].Show();
                else           _orbiters[i].Hide();
            }

            if (shown)
                PlaceActive(teleport: true);
        }

        private bool AnyOrbiterActive()
        {
            for (int i = 0; i < _orbiterCount; i++)
                if (_orbiters[i].gameObject.activeSelf) return true;
            return false;
        }

        private void PlaceActive(bool teleport)
        {
            for (int i = 0; i < _slotCount && i < _orbiterCount; i++)
            {
                if (!_orbiters[i].gameObject.activeSelf) continue;
                PlaceOne(i, teleport);
            }
        }

        private void PlaceOne(int index, bool teleport)
        {
            if (_anchor == null || _slotCount <= 0) return;

            Vector3 center = transform.position + Vector3.up * orbitHeight;
            float step     = 360f / _slotCount;
            float degrees  = _angle + step * index;
            float rad      = degrees * Mathf.Deg2Rad;
            float radius   = Spread;
            Vector3 position = center + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
            _orbiters[index].Place(position, Quaternion.Euler(0f, degrees, 0f), teleport);
        }

        // ── Hits ──────────────────────────────────────────────────────────────

        public void NotifyContact(Collider other)
        {
            if (!_passing || !_shown || !OwnerCanAttack || other == null) return;
            if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

            Enemy.Enemy enemy = other.GetComponentInParent<Enemy.Enemy>();
            if (enemy == null || enemy.IsDead || enemy.InPool) return;

            enemy.TakeDamage(Damage);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!drawRangeGizmo) return;

            float radius = GetGizmoRadius();
            if (radius <= 0f) return;

            Gizmos.color = rangeGizmoColor;
            DrawCircle(transform.position + Vector3.up * orbitHeight, radius);
        }

        private float GetGizmoRadius()
        {
            if (Application.isPlaying && Data != null)
                return Spread;

            if (Definition == null) return 0f;

            WeaponLevelData data = Definition.GetLevel(Mathf.Max(1, Level));
            return data != null ? data.spread : 0f;
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 32;
            Vector3 previous = center + new Vector3(0f, 0f, radius);

            for (int i = 1; i <= segments; i++)
            {
                float rad  = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
