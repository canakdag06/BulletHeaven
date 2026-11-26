using UnityEngine;
using BulletHeaven.Control;
using BulletHeaven.Enemy;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Swings a box along the player's last move direction, then the opposite way, alternating for <see cref="WeaponBase.Amount"/>.
    /// Each swing is one overlap query. Damage and the slash start on the same frame.
    /// Reads: damage, cooldown, amount, spread (length, width and height), pierce (max enemies, 0 = every enemy in the box).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class WhipWeapon : WeaponBase
    {
        private sealed class SlashFx
        {
            public ParticleSystem   Root;
            public ParticleSystem[] Systems;
            public Vector3          BaseScale;
        }

        [Header("Slash")]
        [Tooltip("One-shot slash kept on this prefab. Copied once each time Amount grows. It turns with the swing, steps forward by Forward Offset, and scales with Spread. Local +Z is the swing. Not pooled.")]
        [SerializeField] private ParticleSystem slashTemplate;

        [Header("Hitbox")]
        [Tooltip("Length along the swing, before Spread. The near face starts Forward Offset ahead of the player, then the box extends forward.")]
        [SerializeField, Min(0.01f)] private float baseLength = 2.5f;
        [Tooltip("Width of the box, before Spread.")]
        [SerializeField, Min(0.01f)] private float baseWidth = 1.6f;
        [Tooltip("Height of the box, before Spread. The box is centered on this weapon's height.")]
        [SerializeField, Min(0.01f)] private float baseHeight = 2f;
        [Tooltip("How far in front of the player the near face starts. Not scaled by Spread.")]
        [SerializeField, Min(0f)] private float forwardOffset = 0.4f;

        [Header("Series")]
        [Tooltip("Seconds between swings so the slashes do not start on the same frame. Not a stat.")]
        [SerializeField, Min(0f)] private float swingInterval = 0.15f;

        [Header("Detection")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("Max colliders checked per swing. Colliders past this count are skipped that swing.")]
        [SerializeField, Min(1)] private int maxHits = 64;

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(1f, 0.82f, 0.25f, 0.9f);

        private PlayerController _player;
        private Transform        _anchor;
        private Vector3          _slashBaseScale = Vector3.one;
        private SlashFx[]        _slashes = new SlashFx[4];
        private int              _slashCount;

        private Collider[]     _hits;
        private Enemy.Enemy[]  _enemies;
        private float[]        _distances;

        private Vector3    _seriesDirection;
        private int        _swingTotal;
        private int        _swingIndex;
        private float      _swingTimer;
        private bool       _swinging;
        private bool       _awaitingCooldown;

        private Vector3    _lastCenter;
        private Quaternion _lastRotation;
        private Vector3    _lastSize;
        private bool       _hasSwung;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            _hits      = new Collider[maxHits];
            _enemies   = new Enemy.Enemy[maxHits];
            _distances = new float[maxHits];
            _player    = GetComponentInParent<PlayerController>();

            if (_player == null)
                Debug.LogError($"[{GetType().Name}] PlayerController was not found in parents.", this);

            if (slashTemplate == null)
            {
                Debug.LogError($"[{GetType().Name}] Slash template is not assigned.", this);
                return;
            }

            // Kept out of the player hierarchy. The player turns toward a target, and the slash must not turn with it.
            // Prefab scale is the size at Spread 1. Each swing multiplies it by the current Spread.
            _slashBaseScale = slashTemplate.transform.localScale;
            _anchor = new GameObject(name + " Whip").transform;
            slashTemplate.transform.SetParent(_anchor, false);
            slashTemplate.gameObject.SetActive(false);
            Adopt(slashTemplate);
        }

        private void OnDisable()
        {
            if (_swinging)
                CancelSeries();
        }

        private void OnDestroy()
        {
            if (_anchor != null)
                Destroy(_anchor.gameObject);
        }

        protected override void Update()
        {
            if (_swinging && Data != null)
            {
                if (OwnerCanAttack)
                    TickSeries();
                else
                    CancelSeries();
            }

            base.Update();
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a series. Returns true only once the series has finished, so the base cooldown begins after the last swing.
        /// </summary>
        protected override bool TryFire()
        {
            if (_awaitingCooldown)
            {
                _awaitingCooldown = false;
                return true;
            }

            if (_swinging)
                return false;

            StartSeries();

            // A one-swing series is already over. Multi-swing series finish later, from Update.
            if (!_awaitingCooldown)
                return false;

            _awaitingCooldown = false;
            return true;
        }

        private void StartSeries()
        {
            _seriesDirection = ReadFacing();
            _swingTotal      = Amount;
            _swingIndex      = 0;
            _swingTimer      = 0f;
            _swinging        = true;

            EnsureSlashes(_swingTotal);
            Swing(_swingIndex);
            _swingIndex++;

            if (_swingIndex >= _swingTotal)
                FinishSeries();
        }

        private void TickSeries()
        {
            if (_swingIndex >= _swingTotal)
            {
                FinishSeries();
                return;
            }

            _swingTimer += Time.deltaTime;
            if (_swingTimer < swingInterval)
                return;

            _swingTimer = 0f;
            Swing(_swingIndex);
            _swingIndex++;

            if (_swingIndex >= _swingTotal)
                FinishSeries();
        }

        private void FinishSeries()
        {
            _swinging         = false;
            _awaitingCooldown = true;
        }

        private void CancelSeries()
        {
            _swinging         = false;
            _awaitingCooldown = false;
        }

        private Vector3 ReadFacing()
        {
            Vector3 facing = _player != null ? _player.FacingDirection : transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
                facing = Vector3.forward;

            return facing.normalized;
        }

        // ── Hits ──────────────────────────────────────────────────────────────

        private void Swing(int index)
        {
            Vector3 direction = (index & 1) == 0 ? _seriesDirection : -_seriesDirection;
            float spread = Spread;
            GetBox(direction, spread, out Vector3 center, out Quaternion rotation, out Vector3 halfExtents);

            _lastCenter    = center;
            _lastRotation  = rotation;
            _lastSize      = halfExtents * 2f;
            _hasSwung      = true;

            PlaySlash(index, transform.position + direction * forwardOffset, rotation, spread);

            int count = Physics.OverlapBoxNonAlloc(
                center, halfExtents, _hits, rotation, enemyLayer, QueryTriggerInteraction.Collide);
            DamageHits(count, direction);
        }

        private void DamageHits(int colliderCount, Vector3 hitDirection)
        {
            int unique = 0;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                Enemy.Enemy enemy = hit.GetComponentInParent<Enemy.Enemy>();
                if (enemy == null || enemy.IsDead || enemy.InPool) continue;
                if (ContainsEnemy(enemy, unique)) continue;

                _enemies[unique]   = enemy;
                _distances[unique] = FlatSqrDistance(transform.position, enemy.transform.position);
                unique++;
            }

            int limit = Pierce <= 0 ? unique : Mathf.Min(Pierce, unique);
            if (Pierce > 0)
                SelectNearest(unique, limit);

            for (int i = 0; i < limit; i++)
                HitEnemy(_enemies[i], hitDirection);
        }

        /// <summary>
        /// Damages one enemy from one swing.
        /// <paramref name="hitDirection"/> is the swing on the ground plane. Knockback will be applied here.
        /// </summary>
        private void HitEnemy(Enemy.Enemy enemy, Vector3 hitDirection)
        {
            if (enemy == null || enemy.IsDead || enemy.InPool) return;

            enemy.TakeDamage(Damage);
        }

        private bool ContainsEnemy(Enemy.Enemy enemy, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_enemies[i] == enemy) return true;
            }

            return false;
        }

        private void SelectNearest(int count, int limit)
        {
            for (int i = 0; i < limit; i++)
            {
                int nearest = i;
                float best  = _distances[i];

                for (int j = i + 1; j < count; j++)
                {
                    if (_distances[j] >= best) continue;

                    best    = _distances[j];
                    nearest = j;
                }

                if (nearest == i) continue;

                float distance = _distances[i];
                _distances[i] = _distances[nearest];
                _distances[nearest] = distance;

                Enemy.Enemy enemy = _enemies[i];
                _enemies[i] = _enemies[nearest];
                _enemies[nearest] = enemy;
            }
        }

        private void GetBox(Vector3 direction, float spread, out Vector3 center, out Quaternion rotation, out Vector3 halfExtents)
        {
            float length = baseLength * spread;
            float width  = baseWidth * spread;
            float height = baseHeight * spread;

            rotation    = Quaternion.LookRotation(direction, Vector3.up);
            halfExtents = new Vector3(width * 0.5f, height * 0.5f, length * 0.5f);
            center      = transform.position + direction * (forwardOffset + length * 0.5f);
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        // ── Slashes ───────────────────────────────────────────────────────────

        private void Adopt(ParticleSystem root)
        {
            if (_slashCount == _slashes.Length)
                System.Array.Resize(ref _slashes, _slashes.Length * 2);

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;

                ParticleSystem.MainModule main = systems[i].main;
                main.playOnAwake = false;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }

            _slashes[_slashCount++] = new SlashFx
            {
                Root      = root,
                Systems   = systems,
                BaseScale = _slashBaseScale
            };
        }

        // The pool is for shared, short-lived objects. These few slashes live with the weapon and only replay.
        private void EnsureSlashes(int count)
        {
            if (slashTemplate == null || _anchor == null) return;

            while (_slashCount < count)
            {
                ParticleSystem copy = Instantiate(slashTemplate, _anchor);
                copy.name = "Whip Slash " + (_slashCount + 1);
                copy.gameObject.SetActive(false);
                Adopt(copy);
            }
        }

        private void PlaySlash(int index, Vector3 origin, Quaternion rotation, float spread)
        {
            if (index < 0 || index >= _slashCount) return;

            SlashFx slash = _slashes[index];
            if (slash == null || slash.Root == null) return;

            // Pivot steps forward by Forward Offset. Prefab scale is Spread 1, so the graphic grows with the area.
            Transform slashTransform = slash.Root.transform;
            slashTransform.SetPositionAndRotation(origin, rotation);
            slashTransform.localScale = slash.BaseScale * spread;

            if (!slashTransform.gameObject.activeSelf)
                slashTransform.gameObject.SetActive(true);

            ParticleSystem[] systems = slash.Systems;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null) continue;

                ParticleSystem.MainModule main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }

            // Restart so the next series replays the slash instead of being ignored while it is still playing.
            slash.Root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slash.Root.Play(true);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!drawRangeGizmo) return;

            Gizmos.color = rangeGizmoColor;

            if (Application.isPlaying && _hasSwung)
            {
                DrawBox(_lastCenter, _lastRotation, _lastSize);
                return;
            }

            float spread = GetGizmoSpread();
            if (spread <= 0f) return;

            GetBox(GetPreviewDirection(), spread, out Vector3 center, out Quaternion rotation, out Vector3 halfExtents);
            DrawBox(center, rotation, halfExtents * 2f);
        }

        private Vector3 GetPreviewDirection()
        {
            PlayerController player = _player != null ? _player : GetComponentInParent<PlayerController>();
            Vector3 facing = player != null ? player.FacingDirection : transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = transform.forward;
                facing.y = 0f;
            }

            if (facing.sqrMagnitude < 0.0001f)
                facing = Vector3.forward;

            return facing.normalized;
        }

        private float GetGizmoSpread()
        {
            if (Application.isPlaying && Data != null)
                return Spread;

            if (Definition == null) return 0f;

            WeaponLevelData data = Definition.GetLevel(Mathf.Max(1, Level));
            return data != null ? data.spread : 0f;
        }

        private static void DrawBox(Vector3 center, Quaternion rotation, Vector3 size)
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = previous;
        }
    }
}
