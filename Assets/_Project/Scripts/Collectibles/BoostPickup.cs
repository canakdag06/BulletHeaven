using System;
using BulletHeaven.Core;
using UnityEngine;

namespace BulletHeaven.Collectibles
{
    public enum BoostKind
    {
        Health = 0,
        Magnet = 1,
    }

    /// <summary>
    /// Pooled boost. Health fills the player to max. Magnet pulls every live EXP gem
    /// and leaves other pickups where they are.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoostPickup : PoolableBehaviour, ICollectible
    {
        [Serializable]
        private struct BoostVisual
        {
            public BoostKind kind;
            public GameObject model;
        }

        public override EPoolType PoolType => EPoolType.BoostPickup;

        [SerializeField, Min(0f)]    private float startSpeed     = 4f;
        [SerializeField, Min(0f)]    private float acceleration   = 30f;
        [SerializeField, Min(0.01f)] private float pickupDistance = 0.4f;
        [SerializeField]             private float heightOffset   = 0.5f;
        [SerializeField]             private float spinSpeed      = 90f;

        [Header("Visuals")]
        [SerializeField] private BoostVisual[] visuals;

        public bool CanBeCollected => !InPool && _collector == null;

        private PlayerCollector _collector;
        private float           _speed;
        private BoostKind       _kind;
        private System.Action   _releaseAction;

        protected override void Awake()
        {
            base.Awake();
            _releaseAction = Release;
        }

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
            HideVisuals();
            base.OnRelease();
        }

        /// <summary>Must be called right after taking the pickup from the pool.</summary>
        public void Initialize(Vector3 position, BoostKind kind)
        {
            transform.position = position + Vector3.up * heightOffset;
            _kind = kind;
            ApplyVisual(kind);
        }

        public void BeginCollect(PlayerCollector collector)
        {
            _collector = collector;
            _speed     = startSpeed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (InPool || !other.CompareTag("Player")) return;
            if (!other.TryGetComponent(out PlayerCollector collector)) return;

            _collector = collector;
            Complete();
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            if (_collector == null) return;

            Vector3 target = _collector.Target.position + Vector3.up * heightOffset;
            _speed += acceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude <= pickupDistance * pickupDistance)
                Complete();
        }

        private void Complete()
        {
            if (_collector == null) return;

            PlayerCollector collector = _collector;
            _collector = null;

            switch (_kind)
            {
                case BoostKind.Health:
                    PlayerHealth health = collector.GetComponent<PlayerHealth>();
                    if (health != null)
                        health.Heal(health.MaxHealth);
                    break;

                case BoostKind.Magnet:
                    ExpGemRegistry.AttractAll(collector);
                    break;
            }

            Release();
        }

        private void ApplyVisual(BoostKind kind)
        {
            if (visuals == null) return;

            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].model != null)
                    visuals[i].model.SetActive(visuals[i].kind == kind);
            }
        }

        private void HideVisuals()
        {
            if (visuals == null) return;

            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].model != null)
                    visuals[i].model.SetActive(false);
            }
        }
    }
}
