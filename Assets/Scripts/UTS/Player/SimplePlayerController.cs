using System;
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
        [SerializeField] private float aimWalkSpeed = 3.0f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Status (Read-Only)")]
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool isMoving;
        [SerializeField] private bool isAiming;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 25f;
        [SerializeField] private float staminaRegenRate = 20f;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Attack Settings")]
        [SerializeField] private EnemyFSM enemy;
        [SerializeField] private float attackDistance = 3f;
        [SerializeField] private float attackDamage = 20f;

        private float currentStamina = 100f;
        private float currentHealth;
        private bool isExhausted = false;

        public float CurrentSpeed => currentSpeed;
        public bool IsMoving => isMoving;
        public bool IsAiming => isAiming;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaRatio => maxStamina > 0f ? currentStamina / maxStamina : 0f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;
        public global::PlayerMovementState CurrentMovementState { get; private set; }
        public UTS.Inventory.PlayerInventory Inventory { get; private set; }
        public UTS.Player.PlayerFlashlight Flashlight => flashlight;
        public UTS.Player.PlayerSoundEmitter SoundEmitter => soundEmitter;

        private CharacterController controller;
        private UTS.Player.PlayerFlashlight flashlight;
        private UTS.Player.PlayerSoundEmitter soundEmitter;
        private float verticalVelocity;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentStamina = maxStamina;
            currentHealth = maxHealth;
            Inventory = GetComponent<UTS.Inventory.PlayerInventory>();
            if (Inventory == null)
            {
                Inventory = gameObject.AddComponent<UTS.Inventory.PlayerInventory>();
            }

            flashlight = GetComponent<UTS.Player.PlayerFlashlight>();
            if (flashlight == null)
            {
                flashlight = gameObject.AddComponent<UTS.Player.PlayerFlashlight>();
            }

            soundEmitter = GetComponent<UTS.Player.PlayerSoundEmitter>();
            if (soundEmitter == null)
            {
                soundEmitter = gameObject.AddComponent<UTS.Player.PlayerSoundEmitter>();
            }
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

            // Tentukan status Aiming (Tahan RMB)
            isAiming = IsAimPressed();
            if (flashlight != null)
            {
                flashlight.SetAiming(isAiming);
            }

            // Tentukan kecepatan gerak & konsumsi Stamina
            bool runPressed = IsRunPressed();

            // Jika Shift dilepas, reset status kelelahan sehingga stamina bisa meregenerasi
            if (!runPressed)
            {
                isExhausted = false;
            }

            float speed = walkSpeed;

            if (isAiming)
            {
                speed = aimWalkSpeed;
            }
            else if (IsSneakPressed())
            {
                speed = sneakSpeed;
            }
            else if (runPressed && isMoving && !isExhausted)
            {
                if (currentStamina > 0f)
                {
                    speed = runSpeed;
                    currentStamina -= staminaDrainRate * Time.deltaTime;

                    if (currentStamina <= 0f)
                    {
                        currentStamina = 0f;
                        isExhausted = true; // Kunci kelelahan: otomatis melambat ke jalan santai
                        speed = walkSpeed;
                    }
                }
                else
                {
                    currentStamina = 0f;
                    isExhausted = true;
                    speed = walkSpeed;
                }
            }
            else
            {
                speed = walkSpeed;
            }

            // Regenerasi stamina HANYA jika Shift TIDAK sedang ditekan dan stamina belum penuh
            if (!runPressed && currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }

            currentSpeed = isMoving ? speed : 0f;
            if (!isMoving)
            {
                CurrentMovementState = global::PlayerMovementState.Idle;
            }
            else if (isAiming)
            {
                CurrentMovementState = global::PlayerMovementState.Walk;
            }
            else if (IsSneakPressed())
            {
                CurrentMovementState = global::PlayerMovementState.Sneak;
            }
            else if (runPressed && speed == runSpeed)
            {
                CurrentMovementState = global::PlayerMovementState.Run;
            }
            else
            {
                CurrentMovementState = global::PlayerMovementState.Walk;
            }

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

            // Rotasi menghadap arah gerak atau arah bidikan kamera
            if (isAiming)
            {
                Transform camTransform = Camera.main != null ? Camera.main.transform : null;
                if (camTransform != null)
                {
                    Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
                    if (camForward.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetAimRotation = Quaternion.LookRotation(camForward, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetAimRotation, rotationSpeed * Time.deltaTime);
                    }
                }
            }
            else if (isMoving)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (IsAttackTriggered())
            {
                TryAttackEnemy();
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

            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            Vector3 direction;
            if (camTransform != null)
            {
                Vector3 camForward = Vector3.Scale(camTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 camRight = Vector3.Scale(camTransform.right, new Vector3(1f, 0f, 1f)).normalized;
                direction = camForward * vertical + camRight * horizontal;
            }
            else
            {
                direction = new Vector3(horizontal, 0f, vertical);
            }

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

        private bool IsAimPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private bool IsAttackTriggered()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public void TryAttackEnemy()
        {
            // Cari EnemyFSM terdekat yang masih hidup
            EnemyFSM targetEnemy = null;
            float closestDist = float.MaxValue;

            EnemyFSM[] allEnemies = FindObjectsByType<EnemyFSM>(FindObjectsInactive.Exclude);
            foreach (var e in allEnemies)
            {
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    targetEnemy = e;
                }
            }

            if (targetEnemy != null && closestDist <= attackDistance)
            {
                enemy = targetEnemy;
                targetEnemy.TakeDamage(attackDamage);
                Debug.Log($"[SimplePlayerController] Player menyerang {targetEnemy.name}! Jarak: {closestDist:F2}m (Damage: {attackDamage})", this);
            }
            else if (targetEnemy != null)
            {
                Debug.Log($"[SimplePlayerController] Enemy terdekat ({targetEnemy.name}) terlalu jauh ({closestDist:F2}m > {attackDistance}m) untuk diserang.", this);
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (IsDead) Debug.LogWarning("[Player] Player Kalah / Mati!", this);
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        [ContextMenu("Reset Position")]
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            verticalVelocity = 0f;
            currentStamina = maxStamina;
            isExhausted = false;
            ResetHealth();
            controller.enabled = true;
            Debug.Log("[SimplePlayerController] Posisi & HP Player di-reset.", this);
        }
    }
}
