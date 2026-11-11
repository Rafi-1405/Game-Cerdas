using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Entities
{
    /// <summary>
    /// Smooth 3D player character controller with collision handling and animation bob.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        #region CONFIG
        public static class PLAYER_CONFIG
        {
            // Locomotion parameters
            public const float MOVE_SPEED = 7.0f;
            public const float ROTATION_SPEED = 14.0f;
            public const float GRAVITY_FORCE = -18.0f;

            // Character controller dimensions
            public const float CONTROLLER_RADIUS = 0.40f;
            public const float CONTROLLER_HEIGHT = 1.60f;
            public const float CONTROLLER_STEP_OFFSET = 0.35f;

            // Visual bobbing during movement
            public const float BOB_SPEED = 10.0f;
            public const float BOB_AMOUNT = 0.05f;
        }
        #endregion

        private CharacterController _characterController;
        private Transform _visualModel;
        private Vector3 _velocity;
        private float _bobTimer;
        private Vector3 _originalVisualLocalPos;

        public bool IsMoving { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            ConfigureController();

            Transform visual = transform.Find("Visual");
            if (visual != null)
            {
                _visualModel = visual;
                _originalVisualLocalPos = visual.localPosition;
            }
        }

        private void ConfigureController()
        {
            _characterController.radius = PLAYER_CONFIG.CONTROLLER_RADIUS;
            _characterController.height = PLAYER_CONFIG.CONTROLLER_HEIGHT;
            _characterController.stepOffset = PLAYER_CONFIG.CONTROLLER_STEP_OFFSET;
            _characterController.center = new Vector3(0.0f, PLAYER_CONFIG.CONTROLLER_HEIGHT * 0.5f, 0.0f);
        }

        private void Update()
        {
            HandleMovementInput();
            ApplyGravity();
            ApplyVisualBobbing();
        }

        private void HandleMovementInput()
        {
            Vector2 moveInput = DungeonInputReader.GetMovementVector();
            Vector3 direction = new Vector3(moveInput.x, 0.0f, moveInput.y);

            if (direction.magnitude >= 0.1f)
            {
                IsMoving = true;

                // Move character controller
                Vector3 moveDelta = direction * (PLAYER_CONFIG.MOVE_SPEED * Time.deltaTime);
                _characterController.Move(moveDelta);

                // Rotate smoothly toward movement heading
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, PLAYER_CONFIG.ROTATION_SPEED * Time.deltaTime);
            }
            else
            {
                IsMoving = false;
            }
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded && _velocity.y < 0.0f)
            {
                _velocity.y = -2.0f;
            }
            else
            {
                _velocity.y += PLAYER_CONFIG.GRAVITY_FORCE * Time.deltaTime;
            }

            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void ApplyVisualBobbing()
        {
            if (_visualModel == null) return;

            if (IsMoving)
            {
                _bobTimer += Time.deltaTime * PLAYER_CONFIG.BOB_SPEED;
                float bobOffset = Mathf.Sin(_bobTimer) * PLAYER_CONFIG.BOB_AMOUNT;
                _visualModel.localPosition = _originalVisualLocalPos + new Vector3(0.0f, bobOffset, 0.0f);
            }
            else
            {
                _bobTimer = 0.0f;
                _visualModel.localPosition = Vector3.Lerp(_visualModel.localPosition, _originalVisualLocalPos, Time.deltaTime * 10.0f);
            }
        }

        /// <summary>
        /// Teleports player safely to target world position.
        /// </summary>
        public void Teleport(Vector3 targetPosition)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
                transform.position = targetPosition;
                _characterController.enabled = true;
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
}
