using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum9.PCG
{
    /// <summary>
    /// Kontroler pergerakan Player mandiri untuk arena PCG.
    /// Mendukung New Input System dan Legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PCGPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private Vector3 verticalVelocity = Vector3.zero;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float horizontal = 0f;
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
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

            if (controller != null && controller.enabled)
            {
                // Handle horizontal movement
                Vector3 move = direction * moveSpeed;

                // Handle gravity
                if (controller.isGrounded && verticalVelocity.y < 0f)
                {
                    verticalVelocity.y = -2f;
                }
                else
                {
                    verticalVelocity.y += gravity * Time.deltaTime;
                }

                move += verticalVelocity;
                controller.Move(move * Time.deltaTime);

                // Rotate player towards movement direction
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
}
