using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.UI;
using UTS.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadDepths.Player
{
    /// <summary>
    /// Controls 3D player locomotion, combat, crate interaction, God Mode, and health in isometric view.
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

            [Header("God Mode Settings (Testing)")]
            public float GodModeWalkSpeed = 22f;
            public float GodModeRunSpeed = 32f;

            [Header("Stamina Settings")]
            public float MaxStamina = 100f;
            public float StaminaDrainRate = 22f;
            public float StaminaRegenRate = 18f;

            [Header("Combat Settings")]
            public float MaxHealth = 100f;
            public float PunchRange = 2.0f;
            public float PunchDamage = 25f;
            public float PunchCooldown = 0.5f;
            public float PunchHitDelay = 0.16f;
            public float PunchArcAngle = 110f;

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
        public bool IsGodMode { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float TerrainMultiplier => terrainMultiplier;

        private CharacterController charController;
        private Animator animator;
        private float verticalVelocity = 0f;
        private float terrainMultiplier = 1.0f;
        private float nextPunchTime = 0f;
        private SuppliesCrate activeTargetCrate;
        private bool isInteractingWithCrate = false;
        private Coroutine activePunchRoutine;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnPunchExecuted;
        public event Action<bool> OnGodModeChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            charController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            EnforceAnimatorNoRootMotion();

            CurrentHealth = playerConfig.MaxHealth;
            CurrentStamina = playerConfig.MaxStamina;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnforceAnimatorNoRootMotion()
        {
            if (animator == null) return;
            animator.applyRootMotion = false;
            if (animator.transform != transform)
            {
                animator.transform.localPosition = Vector3.zero;
                animator.transform.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            if (IsDead) return;
            if (Time.timeScale <= 0f || (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible))
            {
                return;
            }

            HandleInput();
            HandleLocomotion();
            HandleCrateInteraction();
            HandleStamina();
        }

        private void LateUpdate()
        {
            EnforceAnimatorNoRootMotion();
        }

        private void HandleInput()
        {
            // God Mode Toggle (G)
            if (GetKeyDown(Key.G))
            {
                ToggleGodMode();
            }

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

        public void ToggleGodMode()
        {
            SetGodMode(!IsGodMode);
        }

        public void SetGodMode(bool enabled)
        {
            if (charController == null) charController = GetComponent<CharacterController>();
            IsGodMode = enabled;

            if (!enabled)
            {
                // Keep player safely grounded and outside any solid wall/tree collider when exiting noclip
                Vector3 safePos = ResolveUnobstructedExitPosition(transform.position);
                safePos.y = Mathf.Max(0.05f, safePos.y);
                transform.position = safePos;
                verticalVelocity = 0f;
            }

            if (charController != null)
            {
                // Disable CharacterController during God Mode so Noclip passes cleanly through walls/trees/zombies
                charController.enabled = !enabled;
                charController.detectCollisions = !enabled;
            }

            OnGodModeChanged?.Invoke(IsGodMode);
            Debug.Log($"[DeadDepthsPlayer] God Mode {(IsGodMode ? "ENABLED (Speed + Invulnerable + Noclip)" : "DISABLED")}");
        }

        private Vector3 ResolveUnobstructedExitPosition(Vector3 pos)
        {
            float radius = charController != null ? charController.radius : 0.35f;
            if (!IsPositionBlocked(pos, radius)) return pos;

            // Search outward in rings for an unobstructed spot
            for (float dist = 1.0f; dist <= 8.0f; dist += 1.0f)
            {
                for (int i = 0; i < 12; i++)
                {
                    float ang = (i / 12f) * Mathf.PI * 2f;
                    Vector3 candidate = pos + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                    candidate.y = 0.05f;
                    if (!IsPositionBlocked(candidate, radius))
                    {
                        return candidate;
                    }
                }
            }

            // NavMesh fallback
            if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 12f, NavMesh.AllAreas))
            {
                return navHit.position + Vector3.up * 0.05f;
            }

            // WorldGenerator spawn fallback
            if (WorldGenerator.Instance != null)
            {
                return WorldGenerator.Instance.PlayerSpawnPosition + Vector3.up * 0.05f;
            }

            return pos;
        }

        private bool IsPositionBlocked(Vector3 pos, float radius)
        {
            Vector3 p1 = pos + Vector3.up * (radius + 0.15f);
            Vector3 p2 = pos + Vector3.up * 1.45f;
            Collider[] hits = Physics.OverlapCapsule(p1, p2, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (col.gameObject.name == "PlayableGround") continue;
                return true;
            }
            return false;
        }

        public void ResetStateForLevel()
        {
            if (charController == null) charController = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (activePunchRoutine != null)
            {
                StopCoroutine(activePunchRoutine);
                activePunchRoutine = null;
            }

            CurrentHealth = playerConfig.MaxHealth;
            CurrentStamina = playerConfig.MaxStamina;
            terrainMultiplier = 1.0f;
            verticalVelocity = 0f;
            IsCrouching = false;
            IsSprinting = false;
            CurrentSpeed = 0f;
            nextPunchTime = 0f;
            isInteractingWithCrate = false;
            activeTargetCrate = null;

            if (CrateProgressBarUI.Instance != null)
            {
                CrateProgressBarUI.Instance.Hide();
            }

            if (charController != null)
            {
                charController.enabled = !IsGodMode;
                charController.detectCollisions = !IsGodMode;
            }

            if (animator != null)
            {
                EnforceAnimatorNoRootMotion();
                animator.SetBool("Dead", false);
                animator.SetBool("Crouching", false);
                animator.SetFloat("Speed", 0f);
                animator.ResetTrigger("Punch");
                if (Application.isPlaying)
                {
                    animator.Play("Standing", 0, 0f);
                    animator.Update(0f);
                    EnforceAnimatorNoRootMotion();
                }
            }

            OnHealthChanged?.Invoke(CurrentHealth, playerConfig.MaxHealth);
            OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
        }

        private void HandleLocomotion()
        {
            Vector2 inputDir = GetMovementInput();
            bool hasMoveInput = inputDir.sqrMagnitude > 0.01f;

            // Sprinting check
            bool wantsRun = GetKey(Key.LeftShift) && hasMoveInput && !IsCrouching && (IsGodMode || CurrentStamina > 5f);
            IsSprinting = wantsRun;

            // Determine target speed
            float baseSpeed;
            if (IsGodMode)
            {
                baseSpeed = IsSprinting ? playerConfig.GodModeRunSpeed : playerConfig.GodModeWalkSpeed;
            }
            else
            {
                baseSpeed = playerConfig.WalkSpeed;
                if (IsCrouching) baseSpeed = playerConfig.CrouchSpeed;
                else if (IsSprinting) baseSpeed = playerConfig.RunSpeed;
            }

            // Apply terrain speed modifier (ignored in God Mode)
            float effectiveTerrain = IsGodMode ? 1.0f : terrainMultiplier;
            float targetSpeed = baseSpeed * effectiveTerrain;
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

            if (IsGodMode)
            {
                // Noclip movement directly via transform (bypasses wall/tree colliders)
                Vector3 nextPos = transform.position + worldMoveDir * (CurrentSpeed * Time.deltaTime);
                nextPos.y = 0.05f;
                transform.position = nextPos;
            }
            else
            {
                if (charController != null && !charController.enabled)
                {
                    charController.enabled = true;
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
            }

            // Sync Animator Speed parameter with Player.controller thresholds:
            // Standing BlendTree: 0 = Idle, 2.3 = Walk, 5.2 = Run
            // Crouch BlendTree:   0 = CrouchIdle, 1.1 = CrouchWalk
            if (animator != null)
            {
                float targetAnimSpeed = 0f;
                if (hasMoveInput)
                {
                    if (IsCrouching)
                        targetAnimSpeed = 1.1f * effectiveTerrain;
                    else if (IsSprinting || IsGodMode)
                        targetAnimSpeed = 5.2f * effectiveTerrain;
                    else
                        targetAnimSpeed = 2.3f * effectiveTerrain;
                }

                if (Time.deltaTime > 0f)
                {
                    animator.SetFloat("Speed", targetAnimSpeed, 0.08f, Time.deltaTime);
                }
                else
                {
                    animator.SetFloat("Speed", targetAnimSpeed);
                }
            }

            // Interrupt crate opening if player moves
            if (hasMoveInput && isInteractingWithCrate)
            {
                CancelCrateInteraction();
            }
        }

        private void HandleStamina()
        {
            if (IsGodMode)
            {
                CurrentStamina = playerConfig.MaxStamina;
                OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
                return;
            }

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
                if (Application.isPlaying && animator.GetCurrentAnimatorStateInfo(0).IsName("Punch"))
                {
                    animator.ResetTrigger("Punch");
                    animator.CrossFadeInFixedTime("Punch", 0.05f, 0, 0f);
                }
                else
                {
                    animator.SetTrigger("Punch");
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchSwing, transform.position);
            }

            OnPunchExecuted?.Invoke();

            // Sync hit detection with the punch animation swing impact
            if (playerConfig.PunchHitDelay > 0f && Application.isPlaying)
            {
                if (activePunchRoutine != null) StopCoroutine(activePunchRoutine);
                activePunchRoutine = StartCoroutine(ExecutePunchHitRoutine());
            }
            else
            {
                PerformPunchHitDetection();
            }
        }

        private IEnumerator ExecutePunchHitRoutine()
        {
            yield return new WaitForSeconds(playerConfig.PunchHitDelay);
            activePunchRoutine = null;
            if (!IsDead)
            {
                PerformPunchHitDetection();
            }
        }

        public void PerformPunchHitDetection()
        {
            Vector3 hitCenter = transform.position + transform.forward * (playerConfig.PunchRange * 0.55f);
            Collider[] hitCols = Physics.OverlapSphere(hitCenter, playerConfig.PunchRange, ~0, QueryTriggerInteraction.Ignore);
            bool hitSomething = false;
            var hitZombies = new System.Collections.Generic.HashSet<AI.ZombieController>();
            var hitBlockers = new System.Collections.Generic.HashSet<BlockingObject>();
            Vector3 playerChest = transform.position + Vector3.up * 1.0f;

            foreach (var col in hitCols)
            {
                if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

                Vector3 toTarget = col.transform.position - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;
                if (dist > playerConfig.PunchRange + 0.55f) continue;

                Vector3 dirToTarget = dist > 0.001f ? toTarget / dist : transform.forward;

                if (dist <= 0.9f || Vector3.Angle(transform.forward, dirToTarget) <= playerConfig.PunchArcAngle * 0.5f)
                {
                    // Check solid wall/tree occlusion so punches cannot connect through solid walls (other zombies don't block cleave)
                    Vector3 targetChest = col.bounds.center;
                    if (IsPunchOccludedByWall(playerChest, targetChest, col))
                    {
                        continue;
                    }

                    // 1. Check for Zombies
                    var zombie = col.GetComponentInParent<AI.ZombieController>();
                    if (zombie != null && !zombie.IsDead && hitZombies.Add(zombie))
                    {
                        zombie.TakeDamage(playerConfig.PunchDamage, transform.position);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayCue(SfxCue.PunchHit, col.transform.position);
                        hitSomething = true;
                        continue;
                    }

                    // 2. Check for Breakable Blockers
                    var blocker = col.GetComponentInParent<BlockingObject>();
                    if (blocker != null && blocker.IsBreakable && !blocker.IsDestroyed && hitBlockers.Add(blocker))
                    {
                        blocker.ApplyDamage(playerConfig.PunchDamage);
                        hitSomething = true;
                    }
                }
            }

            if (hitSomething && IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.TriggerShake(0.20f, 0.16f);
            }
        }

        private bool IsPunchOccludedByWall(Vector3 fromPos, Vector3 toPos, Collider targetCol)
        {
            Vector3 delta = toPos - fromPos;
            float dist = delta.magnitude;
            if (dist <= 0.01f) return false;

            RaycastHit[] hits = Physics.RaycastAll(fromPos, delta / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var occHit in hits)
            {
                var c = occHit.collider;
                if (c == null) continue;
                if (c == targetCol || c.transform == transform || c.transform.IsChildOf(transform)) continue;
                if (c.transform.IsChildOf(targetCol.transform) || targetCol.transform.IsChildOf(c.transform)) continue;
                if (c.gameObject.name == "PlayableGround") continue;
                if (c.GetComponentInParent<AI.ZombieController>() != null) continue;
                return true;
            }
            return false;
        }

        private void HandleCrateInteraction()
        {
            // Scan for nearby crate within actual CrateConfig.InteractionDistance
            Collider[] cols = Physics.OverlapSphere(transform.position, playerConfig.InteractionScanRadius);
            SuppliesCrate nearestCrate = null;
            float nearestDist = float.MaxValue;

            foreach (var col in cols)
            {
                var crate = col.GetComponentInParent<SuppliesCrate>();
                if (crate != null && !crate.IsOpened)
                {
                    float d = Vector3.Distance(transform.position, crate.transform.position);
                    float maxDist = Mathf.Min(playerConfig.InteractionScanRadius, crate.CrateConfig.InteractionDistance);
                    if (d <= maxDist && d < nearestDist)
                    {
                        nearestDist = d;
                        nearestCrate = crate;
                    }
                }
            }

            if (isInteractingWithCrate && activeTargetCrate != nearestCrate)
            {
                CancelCrateInteraction();
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
                    float ratio = activeTargetCrate.CurrentProgress / Mathf.Max(0.1f, activeTargetCrate.CrateConfig.OpenDuration);
                    CrateProgressBarUI.Instance.UpdateProgress(ratio);
                }

                if (activeTargetCrate != null && (activeTargetCrate.IsOpened || !activeTargetCrate.IsInteracting))
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
            if (IsDead || IsGodMode) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));
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
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(playerConfig.MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, playerConfig.MaxHealth);
            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.UpdatePlayerHealth(CurrentHealth, playerConfig.MaxHealth);
            }
        }

        private void Die()
        {
            if (activePunchRoutine != null)
            {
                StopCoroutine(activePunchRoutine);
                activePunchRoutine = null;
            }

            CancelCrateInteraction();

            if (animator != null)
            {
                animator.ResetTrigger("Punch");
                animator.SetBool("Dead", true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PlayerDeath, transform.position);
            }

            // Ensure mouse cursor is unlocked so player can click Retry button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
                case Key.G: return Keyboard.current.gKey.wasPressedThisFrame;
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
