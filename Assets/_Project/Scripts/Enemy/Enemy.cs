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
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private NavMeshAgent agent;
        private Animator animator;
        private Transform playerTransform;

        private float currentHealth;
        private bool isDead;
        private float destinationTimer;
        private float attackTimer;

        public bool IsDead => isDead;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.autoBraking = false;
        }

        private void Update()
        {
            if (isDead || playerTransform == null) return;

            TickDestination();
            UpdateAnimator();

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;
        }

        // ------------------ Pool Callbacks --------------------
        public override void OnGet()
        {
            base.OnGet();
            isDead = false;
            currentHealth = maxHealth;
            destinationTimer = 0f;
            attackTimer = 0f;

            agent.enabled = true;
            agent.isStopped = false;

            animator.ResetTrigger(DeadHash);
            animator.SetFloat(SpeedHash, 0f);

            CachePlayer();
        }

        public override void OnRelease()
        {
            base.OnRelease();

            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
        }

        public void Configure(int health, float speed, int dmg)
        {
            maxHealth = health;
            damage = dmg;
            agent.speed = speed;
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;

            if (currentHealth <= 0)
                Die();
        }


        private void CachePlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            else
                Debug.LogWarning("[Enemy] Player not found.");
        }

        private void TickDestination()
        {
            destinationTimer -= Time.deltaTime;
            if (destinationTimer > 0f) return;

            destinationTimer = destinationUpdateInterval;

            if (agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(playerTransform.position);
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

        private void Die()
        {
            isDead = true;

            agent.isStopped = true;
            animator.SetTrigger(DeadHash);

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
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }


    }
}
