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
        private OrbitWeapon _weapon;
        private Rigidbody   _body;
        private bool        _ready;

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

        private void Awake() => EnsurePhysics();

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
