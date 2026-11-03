using BulletHeaven.Core;
using BulletHeaven.Progression;
using UnityEngine;

namespace BulletHeaven.Collectibles
{
    /// <summary>
    /// Scans for collectibles around the player without allocating (NonAlloc + preallocated buffer)
    /// and hands each one off through <see cref="ICollectible"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCollector : MonoBehaviour
    {
        [SerializeField]            private LayerMask collectibleMask;
        [SerializeField, Min(0f)]   private float     scanInterval  = 0.1f;
        [SerializeField, Min(1)]    private int       maxHitsPerScan = 64;

        public PlayerExperience Experience { get; private set; }
        public Transform        Target     => transform;
        public float            PickupRadius => Mathf.Max(0.1f, _stats.Get(StatType.PickupRadius));

        private PlayerStats _stats;
        private Collider[]  _hits;
        private float       _nextScanTime;

        private void Awake()
        {
            Experience = GetComponent<PlayerExperience>();
            _stats     = GetComponent<PlayerStats>();
            _hits      = new Collider[maxHitsPerScan];
        }

        private void Update()
        {
            if (Time.time < _nextScanTime) return;
            _nextScanTime = Time.time + scanInterval;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, PickupRadius, _hits, collectibleMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].TryGetComponent(out ICollectible collectible) && collectible.CanBeCollected)
                    collectible.BeginCollect(this);

                _hits[i] = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            PlayerStats stats = _stats != null ? _stats : GetComponent<PlayerStats>();
            if (stats == null) return;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, stats.Get(StatType.PickupRadius)));
        }
    }
}
