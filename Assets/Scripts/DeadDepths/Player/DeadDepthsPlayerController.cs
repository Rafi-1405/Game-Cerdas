using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.UI;
using UTS.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadDepths.Player
{
    /// <summary>
    /// Controls 3D player locomotion, combat, crate interaction, and health in isometric view.
    /// ASD-STE100: This controller moves the player, punches enemies, and opens supplies crates.
    /// Follows Section 3 of the specification.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DeadDepthsPlayerController : MonoBehaviour, IMudSlowable
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Header("Locomotion Speeds")]
            public float WalkSpeed = 4.5f;
            public float RunSpeed = 7.5f;
            public float CrouchSpeed = 2.5f;
            public float RotationSpeed = 14f;
            public float Gravity = -15f;

            [Header("Stamina Settings")]
            public float MaxStamina = 100f;
            public float StaminaDrainRate = 22f;
            public float StaminaRegenRate = 18f;

            [Header("Combat Settings")]
            public float MaxHealth = 100f;
            public float PunchRange = 1.8f;
            public float PunchDamage = 25f;
            public float PunchCooldown = 0.5f;
            public float PunchArcAngle = 100f;

            [Header("Interaction Settings")]
            public float InteractionScanRadius = 2.4f;
        }
        #endregion

        [SerializeField] private Config playerConfig = new Config();
        public Config PlayerConfig => playerConfig;

        public static DeadDepthsPlayerController Instance { get; private set; }

        public float CurrentHealth { get; private set; }
        public float MaxHealth => playerConfig.MaxHealth;
        public float CurrentStamina { get; private set; }
        public float MaxStamina => playerConfig.MaxStamina;
        public bool IsDead => CurrentHealth <= 0f;
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float TerrainMultiplier => terrainMultiplier;

        private CharacterController charController;
        private Animator animator;
        private float verticalVelocity = 0f;
        private float terrainMultiplier = 1.0f;
        private float nextPunchTime = 0f;
        private SuppliesCrate activeTargetCrate;
        private bool isInteractingWithCrate = false;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnPunchExecuted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            charController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            CurrentHealth = playerConfig.MaxHealth;
            CurrentStamina = playerConfig.MaxStamina;
        }

        private void Update()
        {
            if (IsDead) return;

            HandleInput();
            HandleLocomotion();
            HandleCrateInteraction();
            HandleStamina();
        }

        private void HandleInput()
        {
            // Crouch Toggle
            if (GetKeyDown(Key.C))
            {
                IsCrouching = !IsCrouching;
                if (animator != null) animator.SetBool("Crouching", IsCrouching);
            }

            // Punch Attack (Left Mouse or Space or F)
            if (GetMouseButtonDown(0) || GetKeyDown(Key.Space) || GetKeyDown(Key.F))
            {
                if (Time.time >= nextPunchTime && !isInteractingWithCrate)
                {
                    ExecutePunch();
                }
            }
        }

        private void HandleLocomotion()
        {
            Vector2 inputDir = GetMovementInput();
            bool hasMoveInput = inputDir.sqrMagnitude > 0.01f;

            // Sprinting check
            bool wantsRun = GetKey(Key.LeftShift) && hasMoveInput && !IsCrouching && CurrentStamina > 5f;
            IsSprinting = wantsRun;

            // Determine target speed
            float baseSpeed = playerConfig.WalkSpeed;
            if (IsCrouching) baseSpeed = playerConfig.CrouchSpeed;
            else if (IsSprinting) baseSpeed = playerConfig.RunSpeed;

            // Apply terrain speed modifier (0.7 in mud)
            float targetSpeed = baseSpeed * terrainMultiplier;
            CurrentSpeed = hasMoveInput ? targetSpeed : 0f;

            // Convert input relative to isometric 45 degree camera orientation
            Vector3 worldMoveDir = Vector3.zero;
            if (hasMoveInput)
            {
                // Isometric direction: Forward is (+1, 0, +1), Right is (+1, 0, -1)
                Vector3 isoForward = new Vector3(1f, 0f, 1f).normalized;
                Vector3 isoRight = new Vector3(1f, 0f, -1f).normalized;
                worldMoveDir = (isoRight * inputDir.x + isoForward * inputDir.y).normalized;

                // Smooth rotation toward movement direction
                Quaternion targetRot = Quaternion.LookRotation(worldMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * playerConfig.RotationSpeed);
            }

            // Gravity
            if (charController.isGrounded)
            {
                verticalVelocity = -0.5f;
            }
            else
            {
                verticalVelocity += playerConfig.Gravity * Time.deltaTime;
            }

            Vector3 finalMove = (worldMoveDir * CurrentSpeed) + (Vector3.up * verticalVelocity);
            charController.Move(finalMove * Time.deltaTime);

            // Update animator parameters
            if (animator != null)
            {
                float normSpeed = CurrentSpeed / playerConfig.RunSpeed;
                animator.SetFloat("Speed", normSpeed);
            }

            // Interrupt crate opening if player moves
            if (hasMoveInput && isInteractingWithCrate)
            {
                CancelCrateInteraction();
            }
        }

        private void HandleStamina()
        {
            if (IsSprinting)
            {
                CurrentStamina = Mathf.Max(0f, CurrentStamina - playerConfig.StaminaDrainRate * Time.deltaTime);
            }
            else
            {
                CurrentStamina = Mathf.Min(playerConfig.MaxStamina, CurrentStamina + playerConfig.StaminaRegenRate * Time.deltaTime);
            }
            OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
        }

        private void ExecutePunch()
        {
            nextPunchTime = Time.time + playerConfig.PunchCooldown;

            if (animator != null)
            {
                animator.SetTrigger("Punch");
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchSwing, transform.position);
            }

            // Camera punch micro-shake
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.TriggerShake(0.18f, 0.15f);
            }

            OnPunchExecuted?.Invoke();

            // Detect hits in front arc
            Collider[] hitCols = Physics.OverlapSphere(transform.position + transform.forward * (playerConfig.PunchRange * 0.5f), playerConfig.PunchRange);
            foreach (var col in hitCols)
            {
                if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

                Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToTarget) <= playerConfig.PunchArcAngle * 0.5f)
                {
                    // 1. Check for Zombies
                    var zombie = col.GetComponentInParent<AI.ZombieController>();
                    if (zombie != null && !zombie.IsDead)
                    {
                        zombie.TakeDamage(playerConfig.PunchDamage);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayCue(SfxCue.PunchHit, col.transform.position);
                        continue;
                    }

                    // 2. Check for Breakable Blockers
                    var blocker = col.GetComponentInParent<BlockingObject>();
                    if (blocker != null && blocker.IsBreakable)
                    {
                        blocker.ApplyDamage(playerConfig.PunchDamage);
                    }
                }
            }
        }

        private void HandleCrateInteraction()
        {
            // Scan for nearby crate
            Collider[] cols = Physics.OverlapSphere(transform.position, playerConfig.InteractionScanRadius);
            SuppliesCrate nearestCrate = null;
            float nearestDist = float.MaxValue;

            foreach (var col in cols)
            {
                var crate = col.GetComponentInParent<SuppliesCrate>();
                if (crate != null && !crate.IsOpened)
                {
                    float d = Vector3.Distance(transform.position, crate.transform.position);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearestCrate = crate;
                    }
                }
            }

            activeTargetCrate = nearestCrate;

            bool isHoldingInteract = GetKey(Key.E);
            if (isHoldingInteract && activeTargetCrate != null)
            {
                if (!isInteractingWithCrate)
                {
                    isInteractingWithCrate = true;
                    activeTargetCrate.StartInteraction(transform);

                    if (CrateProgressBarUI.Instance != null)
                    {
                        CrateProgressBarUI.Instance.Show(transform);
                    }
                }

                activeTargetCrate.UpdateInteraction(Time.deltaTime);
                if (CrateProgressBarUI.Instance != null && activeTargetCrate != null)
                {
                    float ratio = activeTargetCrate.CurrentProgress / activeTargetCrate.CrateConfig.OpenDuration;
                    CrateProgressBarUI.Instance.UpdateProgress(ratio);
                }

                if (activeTargetCrate.IsOpened)
                {
                    CancelCrateInteraction();
                }
            }
            else if (isInteractingWithCrate)
            {
                CancelCrateInteraction();
            }
        }

        public void CancelCrateInteraction()
        {
            if (isInteractingWithCrate)
            {
                isInteractingWithCrate = false;
                if (activeTargetCrate != null) activeTargetCrate.CancelInteraction();
                if (CrateProgressBarUI.Instance != null) CrateProgressBarUI.Instance.Hide();
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, playerConfig.MaxHealth);

            // Record in performance tracker for DDA
            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.RecordDamageReceived(amount);
                PlayerPerformanceTracker.Instance.UpdatePlayerHealth(CurrentHealth, playerConfig.MaxHealth);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PlayerHurt, transform.position);
            }

            // Screen shake
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.TriggerShake(0.35f, 0.25f);
            }

            // Cancel any active crate opening on damage (Section 3.2)
            CancelCrateInteraction();

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(playerConfig.MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, playerConfig.MaxHealth);
            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.UpdatePlayerHealth(CurrentHealth, playerConfig.MaxHealth);
            }
        }

        private void Die()
        {
            if (animator != null)
            {
                animator.SetBool("Dead", true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PlayerDeath, transform.position);
            }
        }

        public void SetTerrainSpeedMultiplier(float multiplier)
        {
            terrainMultiplier = Mathf.Clamp(multiplier, 0.3f, 1.0f);
        }

        #region Input Helpers
        private Vector2 GetMovementInput()
        {
            float h = 0f;
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            }
#else
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
#endif
            return new Vector2(h, v).normalized;
        }

        private bool GetKeyDown(Key key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (key)
            {
                case Key.C: return Keyboard.current.cKey.wasPressedThisFrame;
                case Key.E: return Keyboard.current.eKey.wasPressedThisFrame;
                case Key.F: return Keyboard.current.fKey.wasPressedThisFrame;
                case Key.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), key.ToString()));
#endif
        }

        private bool GetKey(Key key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (key)
            {
                case Key.LeftShift: return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                case Key.E: return Keyboard.current.eKey.isPressed;
                default: return false;
            }
#else
            return Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), key.ToString()));
#endif
        }

        private bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
            if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
            return false;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }
        #endregion
    }
}
