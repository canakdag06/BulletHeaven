using UnityEngine;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Bodies orbit the player until they hit an enemy. That body hides, then returns after <see cref="WeaponBase.Cooldown"/>.
    /// Each body keeps its own timer. Reads: damage, cooldown, amount, spread (radius), speed (degrees per second).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class OrbitWeapon : WeaponBase
    {
        [Header("Orbit")]
        [Tooltip("Body kept on this prefab. Copied once each time Amount grows. Turns off on impact and back on after Cooldown.")]
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
        private float[]        _recover  = new float[4];
        private int            _orbiterCount;
        private int            _slotCount;
        private float          _angle;
        private bool           _running;

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

        private void OnDisable() => HideAll();

        private void OnDestroy()
        {
            if (_anchor != null)
                Destroy(_anchor.gameObject);
        }

        protected override void Update()
        {
            if (_running && Data != null)
            {
                if (OwnerCanAttack)
                    TickOrbit();
                else
                    HideAll();
            }

            base.Update();
        }

        private void FixedUpdate()
        {
            if (!_running || !OwnerCanAttack) return;

            PlaceActive(teleport: false);
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Arms the ring once. Returns false so <see cref="WeaponBase"/> does not run a second timer.
        /// <see cref="WeaponBase.Cooldown"/> is the per-body delay after a hit.
        /// </summary>
        protected override bool TryFire()
        {
            if (_running || orbiterTemplate == null || _anchor == null)
                return false;

            Begin();
            return false;
        }

        private void Begin()
        {
            _slotCount = Amount;
            EnsureOrbiters(_slotCount);
            if (_orbiterCount <= 0) return;

            _running = true;

            for (int i = 0; i < _slotCount; i++)
            {
                _recover[i] = 0f;
                Show(i);
            }
        }

        private void TickOrbit()
        {
            _angle     = Mathf.Repeat(_angle + Speed * Time.deltaTime, 360f);
            _slotCount = Amount;
            EnsureOrbiters(_slotCount);

            for (int i = 0; i < _slotCount; i++)
            {
                if (_recover[i] > 0f)
                {
                    _recover[i] -= Time.deltaTime;
                    if (_recover[i] > 0f) continue;

                    _recover[i] = 0f;
                }

                if (!_orbiters[i].gameObject.activeSelf)
                    Show(i);
            }

            for (int i = _slotCount; i < _orbiterCount; i++)
            {
                _recover[i] = 0f;
                Hide(i);
            }
        }

        // ── Orbiters ──────────────────────────────────────────────────────────

        private void Adopt(OrbitOrbiter orbiter)
        {
            if (_orbiterCount == _orbiters.Length)
            {
                int size = _orbiters.Length * 2;
                System.Array.Resize(ref _orbiters, size);
                System.Array.Resize(ref _recover, size);
            }

            orbiter.Bind(this, _orbiterCount);
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

        private void Show(int index)
        {
            OrbitOrbiter body = _orbiters[index];
            body.SetClosed(false);

            if (!body.gameObject.activeSelf)
                body.gameObject.SetActive(true);

            PlaceOne(index, teleport: true);
        }

        private void Hide(int index)
        {
            GameObject body = _orbiters[index].gameObject;
            if (body.activeSelf)
                body.SetActive(false);
        }

        private void HideAll()
        {
            for (int i = 0; i < _orbiterCount; i++)
                Hide(i);
        }

        private void PlaceActive(bool teleport)
        {
            for (int i = 0; i < _slotCount && i < _orbiterCount; i++)
            {
                if (_recover[i] > 0f || !_orbiters[i].gameObject.activeSelf) continue;
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

        public void NotifyContact(int index, Collider other)
        {
            if (!_running || !OwnerCanAttack || other == null) return;
            if (index < 0 || index >= _slotCount) return;
            if (_recover[index] > 0f) return;
            if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

            Enemy.Enemy enemy = other.GetComponentInParent<Enemy.Enemy>();
            if (enemy == null || enemy.IsDead || enemy.InPool) return;

            // Close before damage so a second contact in this physics step cannot hit again.
            _recover[index] = Cooldown;
            _orbiters[index].SetClosed(true);
            Hide(index);
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
