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

        [Header("Combat")]
        [SerializeField] private int   maxHealth      = 30;
        [SerializeField] private int   damage         = 10;
        [SerializeField] private float attackInterval = 1f;

        [Header("Navigation")]
        [SerializeField] private float destinationUpdateInterval = 0.4f;

        [Header("Animation")]
        [SerializeField] private AnimatedMesh animatedMesh;
        [SerializeField] private string       walkAnimName;
        [SerializeField] private string       deathAnimName;

        public NavMeshAgent Agent           { get; private set; }
        public AnimatedMesh AnimatedMesh    => animatedMesh;
        public Transform    PlayerTransform { get; private set; }

        public float  DestinationUpdateInterval => destinationUpdateInterval;
        public string WalkAnimName              => walkAnimName;
        public string DeathAnimName             => deathAnimName;
        public int    Damage                    => damage;
        public float  AttackInterval            => attackInterval;

        public bool IsDead => _currentState is EnemyDeadState;

        public event Action OnEnemyRemoved;

        private IEnemyState _currentState;
        private float _currentHealth;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            Agent = GetComponent<NavMeshAgent>();
            _currentHealth = maxHealth;
        }

        private void Update()
        {
            _currentState?.Tick();
        }

        // ── Pool Callbacks ────────────────────────────────────────────────────

        public override void OnGet()
        {
            base.OnGet();
            _currentHealth = maxHealth;

            CachePlayer();
            ChangeState(new EnemyChaseState(this));
        }

        public override void OnRelease()
        {
            base.OnRelease();
            OnEnemyRemoved = null;

            _currentState?.Exit();
            _currentState = null;

            if (Agent.enabled)
            {
                Agent.isStopped = true;
                Agent.enabled   = false;
            }
        }

        // ── State Machine ─────────────────────────────────────────────────────

        public void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        // ── Configuration ─────────────────────────────────────────────────────

        public void Configure(int health, float speed, int dmg)
        {
            maxHealth      = health;
            _currentHealth = health;
            damage         = dmg;
            Agent.speed    = speed;
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

        private void OnColliderStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            (_currentState as EnemyChaseState)?.OnHitPlayer(other.gameObject);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Die()
        {
            GameManager.Instance?.OnEnemyDefeated();
            OnEnemyRemoved?.Invoke();
            ChangeState(new EnemyDeadState(this));
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
