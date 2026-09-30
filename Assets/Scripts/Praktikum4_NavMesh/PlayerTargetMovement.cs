using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum4.NavMeshNavigation
{
    public enum TargetMovementState
    {
        Idle,
        Sneak,
        Walk,
        Run
    }

    public class PlayerTargetMovement : MonoBehaviour
    {
        [Header("Movement Speeds (Stealth Mechanics)")]
        [SerializeField] private float sneakSpeed = 2.5f;
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float runSpeed = 8.0f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Boundary / Floor Constraints")]
        [SerializeField] private bool lockYPosition = true;
        [SerializeField] private float fixedY = 0.5f;

        [Header("Current State (Read-Only)")]
        [SerializeField] private TargetMovementState currentState = TargetMovementState.Idle;
        [SerializeField] private float currentSpeed;

        public TargetMovementState CurrentState => currentState;
        public float CurrentSpeed => currentSpeed;

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

            // Tentukan status dan kecepatan gerak (Stealth Mechanics)
            if (inputDirection.sqrMagnitude < 0.001f)
            {
                currentState = TargetMovementState.Idle;
                currentSpeed = 0f;
            }
            else if (IsSneakPressed())
            {
                currentState = TargetMovementState.Sneak;
                currentSpeed = sneakSpeed;
            }
            else if (IsRunPressed())
            {
                currentState = TargetMovementState.Run;
                currentSpeed = runSpeed;
            }
            else
            {
                currentState = TargetMovementState.Walk;
                currentSpeed = walkSpeed;
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
                Vector3 newPos = transform.position + inputDirection * (currentSpeed * Time.deltaTime);
                if (lockYPosition) newPos.y = fixedY;
                transform.position = newPos;
            }
        }

        private void FixedUpdate()
        {
            if (rb != null && !rb.isKinematic)
            {
                Vector3 inputDirection = ReadInput();
                Vector3 targetVelocity = inputDirection * currentSpeed;
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

        private bool IsSneakPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
        }

        private bool IsRunPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
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
