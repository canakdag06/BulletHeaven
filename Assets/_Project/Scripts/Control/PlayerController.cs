using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Control
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Dependencies")]
        [SerializeField] private TargetingSystem targetingSystem;
        public Joystick joystick;

        private Rigidbody rb;
        private Animator animator;
        private Vector3 moveInput;

        private readonly int moveXHash = Animator.StringToHash("MoveX");
        private readonly int moveZHash = Animator.StringToHash("MoveZ");

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            GameManager.Instance.RegisterPlayer(transform);
        }

        private void Update()
        {
            GatherInput();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            MovePlayer();
            RotatePlayer();
        }

        private void GatherInput()
        {
            if (joystick == null) return;

            moveInput = new Vector3(joystick.Horizontal, 0f, joystick.Vertical);

            if (moveInput.magnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void MovePlayer()
        {
            rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.z * moveSpeed);
        }

        private void RotatePlayer()
        {
            Transform currentTarget = targetingSystem != null ? targetingSystem.CurrentTarget : null;

            if (currentTarget != null)
            {
                Vector3 directionToTarget = currentTarget.position - transform.position;
                directionToTarget.y = 0f;

                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
                }
            }
            else if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            }
        }

        private void UpdateAnimations()
        {
            Vector3 worldMovement = moveInput;
            Vector3 localMovement = transform.InverseTransformDirection(worldMovement);

            animator.SetFloat(moveXHash, localMovement.x);
            animator.SetFloat(moveZHash, localMovement.z);
        }

    }
}
