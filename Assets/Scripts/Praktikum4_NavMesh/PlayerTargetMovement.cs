using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum4.NavMeshNavigation
{
    public class PlayerTargetMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Boundary / Floor Constraints")]
        [SerializeField] private bool lockYPosition = true;
        [SerializeField] private float fixedY = 0.5f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            if (lockYPosition)
            {
                fixedY = initialPosition.y;
            }
        }

        private void Update()
        {
            Vector3 inputDirection = ReadInput();

            // Fitur Reset Posisi (Tombol R)
            if (IsResetTriggered())
            {
                ResetPosition();
                return;
            }

            // Rotasi menghadap arah gerak
            if (inputDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Gerakan (Jika tanpa Rigidbody aktif, gunakan transform)
            if (rb == null || rb.isKinematic)
            {
                Vector3 newPos = transform.position + inputDirection * (moveSpeed * Time.deltaTime);
                if (lockYPosition) newPos.y = fixedY;
                transform.position = newPos;
            }
        }

        private void FixedUpdate()
        {
            if (rb != null && !rb.isKinematic)
            {
                Vector3 inputDirection = ReadInput();
                Vector3 targetVelocity = inputDirection * moveSpeed;
                targetVelocity.y = rb.linearVelocity.y; // Menjaga gravitasi
                rb.linearVelocity = targetVelocity;

                if (lockYPosition && Mathf.Abs(rb.position.y - fixedY) > 0.05f)
                {
                    Vector3 corrected = rb.position;
                    corrected.y = fixedY;
                    rb.position = corrected;
                }
            }
        }

        private Vector3 ReadInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
#endif

            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        private bool IsResetTriggered()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        [ContextMenu("Reset to Initial Position")]
        public void ResetPosition()
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Debug.Log("[PlayerTargetMovement] Posisi Target di-reset ke titik awal.", this);
        }
    }
}
