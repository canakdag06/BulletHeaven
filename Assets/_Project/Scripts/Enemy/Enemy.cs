using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using BulletHeaven.Core;

namespace BulletHeaven.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class Enemy : PoolableBehaviour, IDamageable
    {
        public override EPoolType PoolType => EPoolType.Enemy;

        [Header("Combat")]
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackInterval = 1f;

        [Header("Navigation")]
        [SerializeField] private float destinationUpdateInterval = 0.4f;   // ~2-3 per second

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DeadHash  = Animator.StringToHash("Dead");

        private NavMeshAgent  _agent;
        private Animator      _animator;
        private Transform     _playerTransform;

        private int   _currentHealth;
        private bool  _isDead;
        private float _destinationTimer;
        private float _attackTimer;

        public bool IsDead => _isDead;

        protected override void Awake()
        {
            base.Awake();
            _agent    = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _agent.autoBraking = false;
        }

        private void Update()
        {
            if (_isDead || _playerTransform == null) return;

            TickDestination();
            UpdateAnimator();

            if (_attackTimer > 0f)
                _attackTimer -= Time.deltaTime;
        }

        // ------------------ Pool Callbacks --------------------
        public override void OnGet()
        {
            base.OnGet();
            _isDead            = false;
            _currentHealth     = maxHealth;
            _destinationTimer  = 0f;
            _attackTimer       = 0f;

            _agent.enabled    = true;
            _agent.isStopped  = false;

            _animator.ResetTrigger(DeadHash);
            _animator.SetFloat(SpeedHash, 0f);

            CachePlayer();
        }

        public override void OnRelease()
        {
            base.OnRelease();

            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.enabled   = false;
            }
        }

        public void Configure(int health, float speed, int dmg)
        {
            maxHealth  = health;
            damage     = dmg;
            _agent.speed = speed;
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;

            if (_currentHealth <= 0)
                Die();
        }


        private void CachePlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
            else
                Debug.LogWarning("[Enemy] Player not found.");
        }

        private void TickDestination()
        {
            _destinationTimer -= Time.deltaTime;
            if (_destinationTimer > 0f) return;

            _destinationTimer = destinationUpdateInterval;

            if (_agent.enabled && _agent.isOnNavMesh)
                _agent.SetDestination(_playerTransform.position);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_isDead) return;
            if (!collision.gameObject.CompareTag("Player")) return;
            if (_attackTimer > 0f) return;

            _attackTimer = attackInterval;

            if (collision.gameObject.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(damage);
        }

        private void Die()
        {
            _isDead = true;

            _agent.isStopped = true;
            _animator.SetTrigger(DeadHash);

            // Notify GameManager
            GameManager.Instance?.OnEnemyDefeated();

            // Do not return to pool before death animation finishes;
            // if no animation event, automatically return after 1.5 seconds.
            StartCoroutine(ReturnAfterDelay(1.5f));
        }

        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Release();
        }

        private void UpdateAnimator()
        {
            _animator.SetFloat(SpeedHash, _agent.velocity.magnitude);
        }


    }
}
