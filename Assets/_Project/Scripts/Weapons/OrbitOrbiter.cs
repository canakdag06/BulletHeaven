using DG.Tweening;
using UnityEngine;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// One orbiting body. Reports the moment its trigger starts touching something.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class OrbitOrbiter : MonoBehaviour
    {
        [Header("Scale Animation")]
        [Tooltip("Seconds to grow from zero to full size when shown, and to shrink back when hidden. Zero disables the animation.")]
        [SerializeField] private float scaleDuration = 0.2f;
        [SerializeField] private Ease  showEase = Ease.OutBack;
        [SerializeField] private Ease  hideEase = Ease.InBack;

        private OrbitWeapon _weapon;
        private Rigidbody   _body;
        private bool        _ready;
        private Vector3     _baseScale;
        private Tween       _scaleTween;

        public void Bind(OrbitWeapon weapon)
        {
            _weapon = weapon;
            EnsurePhysics();
        }

        /// <summary>
        /// Teleport skips interpolation. Used when the body is shown again, so it does not slide in from the last pose.
        /// </summary>
        public void Place(Vector3 worldPosition, Quaternion worldRotation, bool teleport)
        {
            EnsurePhysics();
            if (_body == null) return;

            if (teleport)
            {
                _body.position = worldPosition;
                _body.rotation = worldRotation;
            }

            _body.MovePosition(worldPosition);
            _body.MoveRotation(worldRotation);
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
            EnsurePhysics();
        }

        /// <summary>
        /// Activates the body and grows it in. If it is still shrinking out, it grows back from its current size.
        /// </summary>
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                transform.localScale = Vector3.zero;
            }

            _scaleTween?.Kill();

            if (scaleDuration <= 0f)
            {
                transform.localScale = _baseScale;
                return;
            }

            _scaleTween = transform.DOScale(_baseScale, scaleDuration)
                .SetEase(showEase)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Shrinks the body to zero, then deactivates it.
        /// </summary>
        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            _scaleTween?.Kill();

            if (scaleDuration <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            _scaleTween = transform.DOScale(Vector3.zero, scaleDuration)
                .SetEase(hideEase)
                .SetLink(gameObject)
                .OnComplete(() => gameObject.SetActive(false));
        }

        // Copies are made from the template, so it must never be left at a partial scale.
        private void OnDisable()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
            transform.localScale = _baseScale;
        }

        private void OnTriggerEnter(Collider other) => _weapon?.NotifyContact(other);

        private void EnsurePhysics()
        {
            if (_ready) return;

            _body = GetComponent<Rigidbody>();
            if (_body == null) return;

            _body.isKinematic = true;
            _body.useGravity  = false;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints   = RigidbodyConstraints.FreezeRotation;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            _ready = true;
        }
    }
}
