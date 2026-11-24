using UnityEngine;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// One orbiting body. Forwards trigger overlaps to <see cref="OrbitWeapon"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class OrbitOrbiter : MonoBehaviour
    {
        private OrbitWeapon _weapon;
        private Rigidbody   _body;
        private int         _index;
        private bool        _closed;
        private bool        _ready;

        public void Bind(OrbitWeapon weapon, int index)
        {
            _weapon = weapon;
            _index  = index;
            EnsurePhysics();
        }

        public void SetClosed(bool closed) => _closed = closed;

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

        private void OnTriggerEnter(Collider other) => Report(other);

        private void OnTriggerStay(Collider other) => Report(other);

        private void Report(Collider other)
        {
            if (_closed) return;
            _weapon?.NotifyContact(_index, other);
        }

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
