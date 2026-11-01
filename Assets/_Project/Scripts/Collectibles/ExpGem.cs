using System;
using BulletHeaven.Core;
using UnityEngine;

namespace BulletHeaven.Collectibles
{
    /// <summary>
    /// Pooled EXP pickup. Never instantiated or destroyed at runtime:
    /// taken via <see cref="PoolManager.Get"/> and returned via <see cref="PoolableBehaviour.Release"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExpGem : PoolableBehaviour, ICollectible
    {
        [Serializable]
        private struct GemVisual
        {
            public ExpTier tier;
            public GameObject model;
        }

        public override EPoolType PoolType => EPoolType.ExpGem;

        [SerializeField, Min(0f)]   private float startSpeed   = 4f;
        [SerializeField, Min(0f)]   private float acceleration = 30f;
        [SerializeField, Min(0.01f)] private float pickupDistance = 0.4f;
        [SerializeField]            private float heightOffset = 0.5f;

        [Header("Visuals")]
        [SerializeField] private GemVisual[] visuals;

        public int  Amount         { get; private set; }
        public bool CanBeCollected => !InPool && _collector == null;

        private PlayerCollector _collector;
        private float           _speed;
        private System.Action   _releaseAction;

        protected override void Awake()
        {
            base.Awake();
            _releaseAction = Release;
        }

        // ── Pool Callbacks ────────────────────────────────────────────────────

        public override void OnGet()
        {
            base.OnGet();
            _collector = null;
            _speed     = 0f;

            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += _releaseAction;
        }

        public override void OnRelease()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= _releaseAction;

            _collector = null;
            Amount     = 0;
            HideVisuals();
            base.OnRelease();
        }

        // ── Public ────────────────────────────────────────────────────────────

        /// <summary>Must be called right after taking the gem from the pool.</summary>
        public void Initialize(Vector3 position, ExpTier tier)
        {
            transform.position = position + Vector3.up * heightOffset;
            Amount = (int)tier;
            ApplyVisual(tier);
        }

        // ── ICollectible ──────────────────────────────────────────────────────

        public void BeginCollect(PlayerCollector collector)
        {
            _collector = collector;
            _speed     = startSpeed;
        }

        // ── Unity Messages ────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (InPool || !other.CompareTag("Player")) return;
            if (!other.TryGetComponent(out PlayerCollector collector)) return;

            _collector = collector;
            Complete();
        }

        private void Update()
        {
            if (_collector == null) return;

            Vector3 target = _collector.Target.position + Vector3.up * heightOffset;
            _speed += acceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude <= pickupDistance * pickupDistance)
                Complete();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Complete()
        {
            _collector.Experience?.AddExperience(Amount);
            Release();
        }

        private void ApplyVisual(ExpTier tier)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].model != null)
                    visuals[i].model.SetActive(visuals[i].tier == tier);
            }
        }

        private void HideVisuals()
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].model != null)
                    visuals[i].model.SetActive(false);
            }
        }
    }
}
