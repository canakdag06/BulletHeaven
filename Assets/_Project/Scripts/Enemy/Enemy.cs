using System;
using System.Collections;
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
        [SerializeField] private int maxHealth = 30;
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackInterval = 1f;

        [Header("Navigation")]
        [SerializeField] private float destinationUpdateInterval = 0.4f;

        [Header("Animation")]
        [SerializeField] private AnimatedMesh animatedMesh;
        [SerializeField] private string walkAnimName;
        [SerializeField] private string deathAnimName;

        private NavMeshAgent agent;
        private Transform playerTransform;

        private float currentHealth;
        private bool isDead;
        private float destinationTimer;
        private float attackTimer;

        public bool IsDead => isDead;
        public event Action OnEnemyRemoved;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (isDead || playerTransform == null) return;

            TickDestination();
            UpdateAnimationSpeed();

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;
        }

        // ── Pool Callbacks ────────────────────────────────────────────────────

        public override void OnGet()
        {
            base.OnGet();
            isDead = false;
            currentHealth = maxHealth;
            destinationTimer = 0f;
            attackTimer = 0f;

            agent.enabled = true;
            agent.isStopped = false;

            if (animatedMesh != null)
            {
                animatedMesh.OnAnimationFinished = null;
                animatedMesh.PlayAnimation(walkAnimName, true);
            }

            CachePlayer();
        }

        public override void OnRelease()
        {
            base.OnRelease();
            OnEnemyRemoved = null;

            if (animatedMesh != null)
                animatedMesh.OnAnimationFinished = null;

            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
        }

        // ── Configuration ─────────────────────────────────────────────────────

        public void Configure(int health, float speed, int dmg)
        {
            maxHealth = health;
            currentHealth = health;
            damage = dmg;
            agent.speed = speed;
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            SpawnHitEffect();

            if (currentHealth <= 0)
                Die();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void SpawnHitEffect()
        {
            HitEffect effect = PoolManager.Instance.Get(EPoolType.BloodDirectional) as HitEffect;
            if (effect == null || playerTransform == null) return;

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            effect.Play(transform.position + Vector3.up, direction);
        }

        private void CachePlayer()
        {
            playerTransform = GameManager.Instance?.PlayerTransform;
            if (playerTransform == null)
                Debug.LogWarning("[Enemy] PlayerTransform not registered in GameManager.");
        }

        private void TickDestination()
        {
            destinationTimer -= Time.deltaTime;
            if (destinationTimer > 0f) return;

            destinationTimer = destinationUpdateInterval;

            if (agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(playerTransform.position);
        }

        private void UpdateAnimationSpeed()
        {
            if (animatedMesh == null || !agent.enabled) return;

            float normalizedSpeed = agent.velocity.magnitude / agent.speed;
            animatedMesh.SetSpeedMultiplier(normalizedSpeed);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (isDead) return;
            if (!collision.gameObject.CompareTag("Player")) return;
            if (attackTimer > 0f) return;

            attackTimer = attackInterval;

            if (collision.gameObject.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(damage);
        }

        private Coroutine _deathFallback;

        private void Die()
        {
            isDead = true;

            agent.isStopped = true;
            agent.enabled = false;

            GameManager.Instance?.OnEnemyDefeated();
            OnEnemyRemoved?.Invoke();

            if (animatedMesh != null)
            {
                animatedMesh.OnAnimationFinished = OnDeathAnimationFinished;
                animatedMesh.PlayAnimation(deathAnimName, false);
            }

            _deathFallback = StartCoroutine(ReturnAfterDelay(3f));
        }

        private void OnDeathAnimationFinished()
        {
            if (animatedMesh != null)
                animatedMesh.OnAnimationFinished = null;

            if (_deathFallback != null)
            {
                StopCoroutine(_deathFallback);
                _deathFallback = null;
            }

            Release();
        }

        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _deathFallback = null;
            Release();
        }
    }
}
