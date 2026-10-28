using BulletHeaven.Core;
using UnityEngine;

namespace BulletHeaven.Collectibles
{
    /// <summary>
    /// Scans for collectibles around the player without allocating (NonAlloc + preallocated buffer)
    /// and hands each one off through <see cref="ICollectible"/>.
    /// </summary>
    public class PlayerCollector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float     pickupRadius  = 3f;
        [SerializeField]            private LayerMask collectibleMask;
        [SerializeField, Min(0f)]   private float     scanInterval  = 0.1f;
        [SerializeField, Min(1)]    private int       maxHitsPerScan = 64;

        public PlayerExperience Experience { get; private set; }
        public Transform        Target     => transform;
        public float            PickupRadius
        {
            get => pickupRadius;
            set => pickupRadius = Mathf.Max(0.1f, value);
        }

        private Collider[] _hits;
        private float      _nextScanTime;

        private void Awake()
        {
            Experience = GetComponent<PlayerExperience>();
            _hits      = new Collider[maxHitsPerScan];
        }

        private void Update()
        {
            if (Time.time < _nextScanTime) return;
            _nextScanTime = Time.time + scanInterval;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, pickupRadius, _hits, collectibleMask, QueryTriggerInteraction.Collide);

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
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
