using System;
using UnityEngine;
using UnityEngine.AI;
using BulletHeaven.Core;

namespace BulletHeaven.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : PoolableBehaviour, IDamageable
    {
        public override EPoolType PoolType => EPoolType.Enemy;

        [Header("Navigation")]
        [SerializeField] private float destinationUpdateInterval = 0.4f;

        [Header("Visuals")]
        [SerializeField] private Transform    model;
        [SerializeField] private AnimatedMesh animatedMesh;
        [SerializeField] private MeshRenderer meshRenderer;

        public NavMeshAgent      Agent           { get; private set; }
        public Collider          HitCollider     { get; private set; }
        public AnimatedMesh      AnimatedMesh    => animatedMesh;
        public Transform         PlayerTransform { get; private set; }
        public EnemyDefinitionSO Definition      { get; private set; }

        public float  DestinationUpdateInterval => destinationUpdateInterval;
        public string WalkAnimName              => Definition.WalkAnimName;
        public string DeathAnimName             => Definition.DeathAnimName;
        public int    Damage                    => Definition.Damage;
        public float  AttackInterval            => Definition.AttackInterval;

        public bool IsDead => _currentState is EnemyDeadState;

        public event Action OnEnemyRemoved;

        private IEnemyState _currentState;
        private float       _currentHealth;
        private Material[]  _defaultMaterials;
        private BoxCollider _boxCollider;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            Agent       = GetComponent<NavMeshAgent>();
            HitCollider  = GetComponent<Collider>();
            _boxCollider = HitCollider as BoxCollider;

            if (animatedMesh == null) animatedMesh = GetComponentInChildren<AnimatedMesh>();
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (model == null && animatedMesh != null) model = animatedMesh.transform;

            _defaultMaterials = meshRenderer != null ? meshRenderer.sharedMaterials : null;
        }

        private void Update()
        {
            _currentState?.Tick();
        }

        // ── Pool Callbacks ────────────────────────────────────────────────────

        public override void OnGet()
        {
            base.OnGet();
            CachePlayer();
        }

        public override void OnRelease()
        {
            base.OnRelease();
            OnEnemyRemoved = null;

            _currentState?.Exit();
            _currentState = null;

            StopAgent();
        }

        // ── State Machine ─────────────────────────────────────────────────────

        public void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        // ── Freeze ────────────────────────────────────────────────────────────

        /// <summary>Stops movement and state ticks when the player dies.</summary>
        public void Freeze()
        {
            if (_currentState is EnemyDeadState) return;

            _currentState?.Exit();
            _currentState = null;

            StopAgent();
        }

        /// <summary>Stops and disables the agent. Safe to call when the agent is off the NavMesh.</summary>
        public void StopAgent()
        {
            if (!Agent.enabled) return;

            if (Agent.isOnNavMesh)
                Agent.isStopped = true;

            Agent.enabled = false;
        }

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>
        /// Applies the definition's stats and visuals, then starts chasing.
        /// Must be called after the enemy is taken from the pool and positioned.
        /// </summary>
        public void Initialize(EnemyDefinitionSO definition, float healthMultiplier = 1f, float speedMultiplier = 1f)
        {
            Definition = definition;

            _currentHealth = Mathf.Max(1, Mathf.RoundToInt(definition.MaxHealth * healthMultiplier));
            Agent.speed    = definition.MoveSpeed * speedMultiplier;

            ApplyBody(definition);
            ApplyVisuals(definition);
            ChangeState(new EnemyChaseState(this));
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (_currentState is EnemyDeadState) return;

            _currentHealth -= amount;
            SpawnHitEffect();

            if (_currentHealth <= 0)
                Die();
        }

        // ── Unity Messages ────────────────────────────────────────────────────

        private void OnCollisionStay(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;
            (_currentState as EnemyChaseState)?.OnHitPlayer(collision.gameObject);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Die()
        {
            GameManager.Instance?.OnEnemyDefeated();
            OnEnemyRemoved?.Invoke();
            ChangeState(new EnemyDeadState(this));
        }

        private void ApplyBody(EnemyDefinitionSO definition)
        {
            if (_boxCollider != null)
            {
                _boxCollider.size   = definition.ColliderSize;
                _boxCollider.center = definition.ColliderCenter;
            }

            Agent.radius = definition.AgentRadius;
        }

        private void ApplyVisuals(EnemyDefinitionSO definition)
        {
            if (model != null)
            {
                model.localScale    = definition.ModelScale;
                model.localRotation = Quaternion.Euler(definition.ModelRotation);
            }

            if (meshRenderer != null)
            {
                Material[] materials = definition.Materials;
                meshRenderer.sharedMaterials = materials != null && materials.Length > 0
                    ? materials
                    : _defaultMaterials;
            }

            animatedMesh?.SetAnimationSet(definition.AnimationSet);
        }

        private void SpawnHitEffect()
        {
            HitEffect effect = PoolManager.Instance.Get(EPoolType.BloodDirectional) as HitEffect;
            if (effect == null || PlayerTransform == null) return;

            Vector3 direction = (PlayerTransform.position - transform.position).normalized;
            effect.Play(transform.position + Vector3.up, direction);
        }

        private void CachePlayer()
        {
            PlayerTransform = GameManager.Instance?.PlayerTransform;
            if (PlayerTransform == null)
                Debug.LogWarning("[Enemy] PlayerTransform not registered in GameManager.");
        }
    }
}
