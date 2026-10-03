using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum5.FSM
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sneakSpeed = 2.5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Status (Read-Only)")]
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool isMoving;

        public float CurrentSpeed => currentSpeed;
        public bool IsMoving => isMoving;

        private CharacterController controller;
        private float verticalVelocity;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            Vector3 inputDirection = ReadMovementInput();
            isMoving = inputDirection.sqrMagnitude > 0.001f;

            // Reset posisi dengan tombol R
            if (IsResetPressed())
            {
                ResetPosition();
                return;
            }

            // Tentukan kecepatan gerak (Sneak Ctrl, Run Shift, Default Walk)
            float speed = walkSpeed;
            if (IsSneakPressed()) speed = sneakSpeed;
            else if (IsRunPressed()) speed = runSpeed;

            currentSpeed = isMoving ? speed : 0f;

            // Gerakan horizontal
            Vector3 move = inputDirection * currentSpeed;

            // Gravitasi & Grounded
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);

            // Rotasi menghadap arah gerak
            if (isMoving)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private Vector3 ReadMovementInput()
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
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            return direction;
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

        private bool IsResetPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        [ContextMenu("Reset Position")]
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            verticalVelocity = 0f;
            controller.enabled = true;
            Debug.Log("[SimplePlayerController] Posisi Player di-reset.", this);
        }
    }
}
