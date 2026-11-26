using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
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
    /// Controls 3D player locomotion, Project Zomboid RMB combat aim stance,
    /// Shift Tap-to-Dodge vs Hold-to-Sprint, 3-hit melee combo with crit & hitstop,
    /// crouch stealth, crate interaction, Quick-Belt shortcuts, God Mode, and health.
    /// ASD-STE100: This controller moves the player, aims, dodges, punches enemies, and opens supplies crates.
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
            public float AimWalkSpeed = 3.1f;
            public float RotationSpeed = 14f;
            public float AimRotationSpeed = 20f;
            public float Gravity = -15f;

            [Header("Crouch Dimensions")]
            public float StandingHeight = 1.8f;
            public float CrouchHeight = 1.15f;

            [Header("Aim Stance")]
            public float AimDeadzoneRadius = 0.35f;

            [Header("Tap-Dodge vs Hold-Sprint")]
            public float ShiftHoldThreshold = 0.20f;
            public float DodgeSpeed = 15.0f;
            public float DodgeDuration = 0.24f;
            public float DodgeIframeDuration = 0.22f;
            public float DodgeStaminaCost = 22f;
            public float DodgeCooldown = 0.75f;
            public float DodgeBufferWindow = 0.12f;

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
            public float ComboResetWindow = 0.95f;
            public float ComboFinisherMultiplier = 1.35f;
            public float CritChance = 0.15f;
            public float CritMultiplier = 1.5f;
            public float HitstopDuration = 0.035f;

            [Header("Interaction Settings")]
            public float InteractionScanRadius = 2.4f;
        }
        #endregion

        public const string PrefTapDodgeThreshold = "DeadDepths_TapDodgeThreshold";

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
        public bool IsAiming { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsGodMode { get; private set; }
        public bool IsInvulnerable => IsGodMode || iframeRemaining > 0f;
        public float CurrentSpeed { get; private set; }
        public float TerrainMultiplier => terrainMultiplier;
        public float CurrentAnimSpeedMult { get; private set; } = 1.0f;
        public Vector3 LastValidAimPoint { get; private set; }
        public Vector3 LastValidAimDir { get; private set; } = Vector3.forward;
        public Vector3 CursorScreenPosition { get; private set; }
        public bool IsInteractingWithCrate => isInteractingWithCrate;
        public SuppliesCrate NearbyCrate => activeTargetCrate;

        public bool IsPunchingAnimActive =>
            animator != null && Application.isPlaying && animator.GetCurrentAnimatorStateInfo(0).IsName("Punch");

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashCrouching = Animator.StringToHash("Crouching");
        private static readonly int HashPunch = Animator.StringToHash("Punch");
        private static readonly int HashDead = Animator.StringToHash("Dead");
        private static readonly int HashAnimSpeedMult = Animator.StringToHash("AnimSpeedMult");

        private CharacterController charController;
        private Animator animator;
        private bool hasAnimSpeedMultParam = false;
        private bool animParamChecked = false;
        private Camera cachedMainCamera;
        private readonly Collider[] crateOverlapBuffer = new Collider[32];
        private DeadDepthsFlashlight flashlight;
        private float verticalVelocity = 0f;
        private float terrainMultiplier = 1.0f;
        private float nextPunchTime = 0f;
        private float footstepTimer = 0f;
        private SuppliesCrate activeTargetCrate;
        private bool isInteractingWithCrate = false;
        private Coroutine activePunchRoutine;
        private Coroutine activeHitstopRoutine;

        // Aim state
        private bool aimInputHeld = false;
        private bool testAimOverrideActive = false;
        private float lastValidAimDist = 4.0f;

        // Dodge & Sprint state (unscaled time so hitstop never skews tap vs hold)
        private bool shiftWasHeld = false;
        private float shiftHoldDuration = 0f;
        private float dodgeBufferedUntil = -1f;
        private float nextDodgeTime = -10f;
        private float dodgeTimer = 0f;
        private float iframeRemaining = 0f;
        private Vector3 dodgeDirection = Vector3.forward;
        private bool dodgeIsBackstep = false;

        // 3-Hit Melee Combo state
        private int comboStep = 0;
        private float lastPunchTriggerTime = -10f;
        private float pendingDamageMultiplier = 1.0f;
        private bool pendingIsCrit = false;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnPunchExecuted;
        public event Action OnDodgeExecuted;
        public event Action<bool> OnGodModeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ApplyFallbackDefaults();

            charController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            flashlight = GetComponent<DeadDepthsFlashlight>();
            if (animator != null && animator.GetComponent<PlayerAimIK>() == null)
            {
                animator.gameObject.AddComponent<PlayerAimIK>();
            }
            if (GetComponent<DeadDepthsAimReticle>() == null)
            {
                gameObject.AddComponent<DeadDepthsAimReticle>();
            }

            EnforceAnimatorNoRootMotion();
            LastValidAimDir = transform.forward;
            LastValidAimPoint = transform.position + transform.forward * 4f + Vector3.up * 0.9f;

            CurrentHealth = playerConfig.MaxHealth;
            CurrentStamina = playerConfig.MaxStamina;
            LoadSettings();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (playerConfig == null) playerConfig = new Config();
            if (playerConfig.WalkSpeed <= 0f) playerConfig.WalkSpeed = 4.5f;
            if (playerConfig.RunSpeed <= 0f) playerConfig.RunSpeed = 7.5f;
            if (playerConfig.CrouchSpeed <= 0f) playerConfig.CrouchSpeed = 2.5f;
            if (playerConfig.AimWalkSpeed <= 0f) playerConfig.AimWalkSpeed = 3.1f;
            if (playerConfig.RotationSpeed <= 0f) playerConfig.RotationSpeed = 14f;
            if (playerConfig.AimRotationSpeed <= 0f) playerConfig.AimRotationSpeed = 20f;
            if (playerConfig.StandingHeight <= 0f) playerConfig.StandingHeight = 1.8f;
            if (playerConfig.CrouchHeight <= 0f) playerConfig.CrouchHeight = 1.15f;
            if (playerConfig.AimDeadzoneRadius <= 0f) playerConfig.AimDeadzoneRadius = 0.35f;
            if (playerConfig.ShiftHoldThreshold <= 0f) playerConfig.ShiftHoldThreshold = 0.20f;
            if (playerConfig.DodgeSpeed <= 0f) playerConfig.DodgeSpeed = 15.0f;
            if (playerConfig.DodgeDuration <= 0f) playerConfig.DodgeDuration = 0.24f;
            if (playerConfig.DodgeIframeDuration <= 0f) playerConfig.DodgeIframeDuration = 0.22f;
            if (playerConfig.DodgeStaminaCost <= 0f) playerConfig.DodgeStaminaCost = 22f;
            if (playerConfig.DodgeCooldown <= 0f) playerConfig.DodgeCooldown = 0.75f;
            if (playerConfig.DodgeBufferWindow <= 0f) playerConfig.DodgeBufferWindow = 0.12f;
            if (playerConfig.MaxStamina <= 0f) playerConfig.MaxStamina = 100f;
            if (playerConfig.StaminaDrainRate <= 0f) playerConfig.StaminaDrainRate = 22f;
            if (playerConfig.StaminaRegenRate <= 0f) playerConfig.StaminaRegenRate = 18f;
            if (playerConfig.MaxHealth <= 0f) playerConfig.MaxHealth = 100f;
            if (playerConfig.PunchRange <= 0f) playerConfig.PunchRange = 2.0f;
            if (playerConfig.PunchDamage <= 0f) playerConfig.PunchDamage = 25f;
            if (playerConfig.PunchCooldown <= 0f) playerConfig.PunchCooldown = 0.5f;
            if (playerConfig.PunchArcAngle <= 0f) playerConfig.PunchArcAngle = 110f;
            if (playerConfig.ComboResetWindow <= 0f) playerConfig.ComboResetWindow = 0.95f;
            if (playerConfig.ComboFinisherMultiplier <= 0f) playerConfig.ComboFinisherMultiplier = 1.35f;
            if (playerConfig.CritChance <= 0f) playerConfig.CritChance = 0.15f;
            if (playerConfig.CritMultiplier <= 0f) playerConfig.CritMultiplier = 1.5f;
            if (playerConfig.HitstopDuration <= 0f) playerConfig.HitstopDuration = 0.035f;
            if (playerConfig.InteractionScanRadius <= 0f) playerConfig.InteractionScanRadius = 2.4f;
        }

        public void LoadSettings()
        {
            if (PlayerPrefs.HasKey(PrefTapDodgeThreshold))
            {
                playerConfig.ShiftHoldThreshold = Mathf.Clamp(
                    PlayerPrefs.GetFloat(PrefTapDodgeThreshold, 0.20f), 0.15f, 0.25f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnforceAnimatorNoRootMotion()
        {
            if (animator == null) return;
            animator.applyRootMotion = false;
            if (!IsDead && animator.transform != transform)
            {
                animator.transform.localPosition = Vector3.zero;
                animator.transform.localRotation = Quaternion.identity;
            }
        }

        private void CorrectPlayerDeathContact()
        {
            if (!IsDead || animator == null || animator.transform == transform) return;
            // Subtle vertical settle so death pose lies flush on ground plane
            Vector3 lp = animator.transform.localPosition;
            lp.x = 0f;
            lp.z = 0f;
            lp.y = Mathf.MoveTowards(lp.y, -0.04f, Time.deltaTime * 0.5f);
            animator.transform.localPosition = lp;
        }

        private void Update()
        {
            if (IsDead) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.IsInMainMenu)
            {
                if (GetKeyDown(Key.Escape) && SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsOpen)
                {
                    SettingsModalUI.Instance.Close();
                }
                ResetTransientInputStates();
                if (animator != null)
                {
                    animator.SetFloat(HashSpeed, 0f);
                    SetAnimatorSpeedMultiplier(1.0f);
                }
                return;
            }

            // Handle [ESC] pause toggle even while paused
            if (GetKeyDown(Key.Escape))
            {
                if (SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsOpen)
                {
                    SettingsModalUI.Instance.Close();
                }
                else if (gm != null)
                {
                    gm.TogglePause();
                }
                return;
            }

            if (Time.timeScale <= 0f ||
                (gm != null && gm.IsPaused) ||
                (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible))
            {
                ResetTransientInputStates();
                return;
            }

            if (iframeRemaining > 0f)
            {
                iframeRemaining = Mathf.Max(0f, iframeRemaining - Time.deltaTime);
            }

            HandleInput();
            HandleLocomotion();
            HandleCrateInteraction();
            HandleStamina();
        }

        private void LateUpdate()
        {
            EnforceAnimatorNoRootMotion();
            if (IsDead)
            {
                CorrectPlayerDeathContact();
            }
        }

        private void ResetTransientInputStates()
        {
            if (!testAimOverrideActive)
            {
                aimInputHeld = false;
                IsAiming = false;
            }
            IsSprinting = false;
            shiftWasHeld = false;
            shiftHoldDuration = 0f;
            dodgeBufferedUntil = -1f;
        }

        private bool IsPointerOverBlockingUI()
        {
            if (DeadDepthsDebugMode.IsPointerOverAnyPanel()) return true;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            return false;
        }

        private void HandleInput()
        {
            // God Mode Toggle (G)
            if (GetKeyDown(Key.G))
            {
                ToggleGodMode();
            }

            // Quick-Use Belt Slots [1] Bandage, [2] Medkit, [3] Adrenaline
            if (GameManager.Instance != null)
            {
                if (GetKeyDown(Key.Digit1)) GameManager.Instance.TryUseBeltSlot(0);
                else if (GetKeyDown(Key.Digit2)) GameManager.Instance.TryUseBeltSlot(1);
                else if (GetKeyDown(Key.Digit3)) GameManager.Instance.TryUseBeltSlot(2);
            }

            // Crouch Toggle (C) with headroom verification before standing up
            if (GetKeyDown(Key.C) && !IsDodging)
            {
                if (!IsCrouching)
                {
                    SetCrouchState(true);
                }
                else if (CanStandUp())
                {
                    SetCrouchState(false);
                }
            }

            // Hold RMB Aim Stance
            UpdateAimInputAndCursor();

            // Shift Tap-to-Dodge vs Hold-to-Sprint (unscaled real-time)
            HandleShiftDodgeSprintInput();

            // Retry buffered dodge if cooldown just finished
            if (Time.unscaledTime <= dodgeBufferedUntil && !IsDodging)
            {
                TryTriggerDodge();
            }

            // Punch Attack (Left Mouse, Space, or F)
            bool lmbPressed = GetMouseButtonDown(0) && !IsPointerOverBlockingUI();
            bool keyPunchPressed = GetKeyDown(Key.Space) || GetKeyDown(Key.F);
            if (lmbPressed || keyPunchPressed)
            {
                if (Time.time >= nextPunchTime && !isInteractingWithCrate && !IsDodging)
                {
                    ExecutePunch(lmbPressed);
                }
            }
        }

        private void UpdateAimInputAndCursor()
        {
            if (testAimOverrideActive)
            {
                IsAiming = !IsDead && !IsDodging && !isInteractingWithCrate;
                return;
            }

            if (GetMouseButtonDown(1) && !IsPointerOverBlockingUI())
            {
                aimInputHeld = true;
            }
            if (!GetMouseButton(1))
            {
                aimInputHeld = false;
            }

            IsAiming = aimInputHeld && !IsDead && !IsDodging && !isInteractingWithCrate;

            // Update cursor aim point on waist plane whenever pointer is available
            UpdateCursorAimPoint();
        }

        private void UpdateCursorAimPoint()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return;
            Vector2 screenPos = Mouse.current.position.ReadValue();
#else
            Vector2 screenPos = Input.mousePosition;
#endif
            CursorScreenPosition = screenPos;

            if (cachedMainCamera == null) cachedMainCamera = Camera.main;
            Camera cam = cachedMainCamera;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane waistPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + 0.9f, 0f));
            if (waistPlane.Raycast(ray, out float enter) && enter > 0f)
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 delta = hitPoint - transform.position;
                delta.y = 0f;
                if (delta.magnitude > playerConfig.AimDeadzoneRadius)
                {
                    lastValidAimDist = delta.magnitude;
                    LastValidAimPoint = hitPoint;
                    LastValidAimDir = delta.normalized;
                }
                else
                {
                    LastValidAimPoint = transform.position + LastValidAimDir * lastValidAimDist + Vector3.up * 0.9f;
                }
            }
        }

        /// <summary>
        /// Programmatic helper used by automated tests to verify RMB Aim stance and backpedaling.
        /// </summary>
        public void SetAimStateForTest(bool aiming, Vector3 aimWorldPoint)
        {
            testAimOverrideActive = aiming;
            aimInputHeld = aiming;
            IsAiming = aiming;
            if (aiming)
            {
                Vector3 delta = aimWorldPoint - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.001f)
                {
                    lastValidAimDist = Mathf.Max(1.0f, delta.magnitude);
                    LastValidAimDir = delta.normalized;
                    LastValidAimPoint = aimWorldPoint;
                    transform.rotation = Quaternion.LookRotation(LastValidAimDir);
                }
            }
        }

        private void HandleShiftDodgeSprintInput()
        {
            bool shiftPressed = GetKey(Key.LeftShift);
            if (shiftPressed)
            {
                if (!shiftWasHeld)
                {
                    shiftWasHeld = true;
                    shiftHoldDuration = 0f;
                }
                else
                {
                    shiftHoldDuration += Time.unscaledDeltaTime;
                }
            }
            else if (shiftWasHeld)
            {
                // Released Shift: if held shorter than threshold -> Tap-to-Dodge
                if (shiftHoldDuration < playerConfig.ShiftHoldThreshold)
                {
                    dodgeBufferedUntil = Time.unscaledTime + playerConfig.DodgeBufferWindow;
                    TryTriggerDodge();
                }
                shiftWasHeld = false;
                shiftHoldDuration = 0f;
            }
        }

        public bool TryTriggerDodge(Vector3? explicitDirection = null)
        {
            if (IsDead || IsDodging) return false;
            var gm = GameManager.Instance;
            if (gm != null && gm.IsInMainMenu) return false;
            if (Time.timeScale <= 0f) return false;
            if (Time.unscaledTime < nextDodgeTime) return false;
            if (!IsGodMode && CurrentStamina < playerConfig.DodgeStaminaCost) return false;

            // Cancel pending punch hit check if player dodges
            if (activePunchRoutine != null)
            {
                StopCoroutine(activePunchRoutine);
                activePunchRoutine = null;
            }
            if (animator != null)
            {
                animator.ResetTrigger(HashPunch);
            }

            // Cancel crate interaction smoothly if player dodges away
            if (isInteractingWithCrate)
            {
                CancelCrateInteraction();
            }

            // Uncrouch if headroom is clear
            if (IsCrouching && CanStandUp())
            {
                SetCrouchState(false);
            }

            Vector2 moveInput = GetMovementInput();
            Vector3 worldMove = GetIsoWorldMoveDirection(moveInput);
            bool hasMove = moveInput.sqrMagnitude > 0.01f;

            dodgeIsBackstep = false;
            if (explicitDirection.HasValue && explicitDirection.Value.sqrMagnitude > 0.001f)
            {
                Vector3 d = explicitDirection.Value;
                d.y = 0f;
                dodgeDirection = d.normalized;
            }
            else if (hasMove)
            {
                dodgeDirection = worldMove;
            }
            else if (IsAiming)
            {
                dodgeDirection = -transform.forward;
                dodgeIsBackstep = true;
            }
            else
            {
                dodgeDirection = transform.forward;
            }

            if (!IsGodMode)
            {
                CurrentStamina = Mathf.Max(0f, CurrentStamina - playerConfig.DodgeStaminaCost);
                OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
            }

            IsDodging = true;
            IsSprinting = false;
            IsAiming = false;
            dodgeTimer = 0f;
            dodgeBufferedUntil = -1f;
            nextDodgeTime = Time.unscaledTime + playerConfig.DodgeCooldown;
            iframeRemaining = playerConfig.DodgeIframeDuration;

            if (!dodgeIsBackstep && dodgeDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dodgeDirection);
            }

            AudioManager.Instance?.PlayCueScaled(SfxCue.PunchSwing, transform.position, 0.7f, 1.25f);
            CombatVfxManager.Instance?.PlayDodgeDust(transform.position);
            OnDodgeExecuted?.Invoke();
            return true;
        }

        private void SetCrouchState(bool crouch)
        {
            IsCrouching = crouch;
            if (charController != null)
            {
                float h = crouch ? playerConfig.CrouchHeight : playerConfig.StandingHeight;
                charController.height = h;
                charController.center = new Vector3(0f, h * 0.5f, 0f);
            }
            if (animator != null)
            {
                animator.SetBool(HashCrouching, IsCrouching);
            }
        }

        private bool CanStandUp()
        {
            if (IsGodMode || charController == null) return true;
            float r = Mathf.Max(0.2f, charController.radius * 0.85f);
            Vector3 pBottom = transform.position + Vector3.up * (playerConfig.CrouchHeight + 0.05f);
            Vector3 pTop = transform.position + Vector3.up * (playerConfig.StandingHeight - r);
            if (pTop.y <= pBottom.y) return true;

            Collider[] hits = Physics.OverlapCapsule(pBottom, pTop, r, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (col.gameObject.name == "PlayableGround") continue;
                return false;
            }
            return true;
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
                Vector3 safePos = ResolveUnobstructedExitPosition(transform.position);
                safePos.y = Mathf.Max(0.05f, safePos.y);
                transform.position = safePos;
                verticalVelocity = 0f;
            }

            if (charController != null)
            {
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

            if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 12f, NavMesh.AllAreas))
            {
                return navHit.position + Vector3.up * 0.05f;
            }

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
            if (activeHitstopRoutine != null)
            {
                StopCoroutine(activeHitstopRoutine);
                activeHitstopRoutine = null;
                Time.timeScale = 1f;
            }

            var gm = GameManager.Instance;
            var perf = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();
            perf?.SetTimePaused(gm != null && gm.IsInMainMenu);

            CurrentHealth = playerConfig.MaxHealth;
            CurrentStamina = playerConfig.MaxStamina;
            terrainMultiplier = 1.0f;
            verticalVelocity = 0f;
            IsCrouching = false;
            IsSprinting = false;
            IsAiming = false;
            IsDodging = false;
            aimInputHeld = false;
            testAimOverrideActive = false;
            shiftWasHeld = false;
            shiftHoldDuration = 0f;
            dodgeBufferedUntil = -1f;
            nextDodgeTime = -10f;
            iframeRemaining = 0f;
            comboStep = 0;
            lastPunchTriggerTime = -10f;
            pendingDamageMultiplier = 1.0f;
            pendingIsCrit = false;
            CurrentSpeed = 0f;
            CurrentAnimSpeedMult = 1.0f;
            nextPunchTime = 0f;
            footstepTimer = 0f;
            isInteractingWithCrate = false;
            activeTargetCrate = null;
            lastValidAimDist = 4.0f;
            LastValidAimDir = transform.forward;
            LastValidAimPoint = transform.position + transform.forward * 4f + Vector3.up * 0.9f;

            if (CrateProgressBarUI.Instance != null)
            {
                CrateProgressBarUI.Instance.Hide();
            }

            if (charController != null)
            {
                charController.height = playerConfig.StandingHeight;
                charController.center = new Vector3(0f, playerConfig.StandingHeight * 0.5f, 0f);
                charController.enabled = !IsGodMode;
                charController.detectCollisions = !IsGodMode;
            }

            if (animator != null)
            {
                EnforceAnimatorNoRootMotion();
                animator.SetBool(HashDead, false);
                animator.SetBool(HashCrouching, false);
                animator.SetFloat(HashSpeed, 0f);
                SetAnimatorSpeedMultiplier(1.0f);
                animator.ResetTrigger(HashPunch);
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

        private static Vector3 GetIsoWorldMoveDirection(Vector2 inputDir)
        {
            if (inputDir.sqrMagnitude <= 0.0001f) return Vector3.zero;
            Vector3 isoForward = new Vector3(1f, 0f, 1f).normalized;
            Vector3 isoRight = new Vector3(1f, 0f, -1f).normalized;
            return (isoRight * inputDir.x + isoForward * inputDir.y).normalized;
        }

        private void HandleLocomotion()
        {
            if (IsDodging)
            {
                UpdateDodgeMovement();
                return;
            }

            Vector2 inputDir = GetMovementInput();
            bool hasMoveInput = inputDir.sqrMagnitude > 0.01f;

            // Sprinting only after Shift held >= threshold, never while aiming/crouching/interacting
            bool wantsRun = shiftWasHeld &&
                            shiftHoldDuration >= playerConfig.ShiftHoldThreshold &&
                            hasMoveInput &&
                            !IsCrouching &&
                            !IsAiming &&
                            !isInteractingWithCrate &&
                            (IsGodMode || CurrentStamina > 5f);
            IsSprinting = wantsRun;

            float adrenalineMult = (GameManager.Instance != null && GameManager.Instance.IsAdrenalineActive) ? 1.22f : 1.0f;

            float baseSpeed;
            if (IsGodMode)
            {
                baseSpeed = IsSprinting ? playerConfig.GodModeRunSpeed : playerConfig.GodModeWalkSpeed;
            }
            else if (IsAiming)
            {
                baseSpeed = IsCrouching ? Mathf.Min(playerConfig.AimWalkSpeed, playerConfig.CrouchSpeed) : playerConfig.AimWalkSpeed;
            }
            else
            {
                baseSpeed = playerConfig.WalkSpeed;
                if (IsCrouching) baseSpeed = playerConfig.CrouchSpeed;
                else if (IsSprinting) baseSpeed = playerConfig.RunSpeed;
            }

            float effectiveTerrain = IsGodMode ? 1.0f : terrainMultiplier;
            float targetSpeed = baseSpeed * effectiveTerrain * adrenalineMult;
            CurrentSpeed = hasMoveInput ? targetSpeed : 0f;

            Vector3 worldMoveDir = hasMoveInput ? GetIsoWorldMoveDirection(inputDir) : Vector3.zero;

            // Rotation: Aiming rotates toward cursor; normal locomotion rotates toward movement
            if (IsAiming && LastValidAimDir.sqrMagnitude > 0.001f)
            {
                Quaternion aimRot = Quaternion.LookRotation(LastValidAimDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, Time.deltaTime * playerConfig.AimRotationSpeed);
            }
            else if (hasMoveInput)
            {
                Quaternion targetRot = Quaternion.LookRotation(worldMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * playerConfig.RotationSpeed);
            }

            if (IsGodMode)
            {
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

                if (hasMoveInput && charController.isGrounded)
                {
                    footstepTimer -= Time.deltaTime;
                    if (footstepTimer <= 0f)
                    {
                        float baseInterval = IsSprinting ? 0.30f : (IsCrouching ? 0.56f : 0.44f);
                        footstepTimer = baseInterval / Mathf.Max(0.5f, effectiveTerrain * adrenalineMult);
                        if (AudioManager.Instance != null)
                        {
                            bool inMud = terrainMultiplier < 0.95f || MuddyPatch.IsActorInMud(this);
                            AudioManager.Instance.PlayFootstep(transform.position, inMud);
                        }
                    }
                }
                else
                {
                    footstepTimer = Mathf.Min(footstepTimer, 0.08f);
                }
            }

            // Backpedaling animation detection & Animator Speed sync
            bool isBackpedaling = IsAiming && hasMoveInput && Vector3.Dot(worldMoveDir, transform.forward) < -0.35f;
            float animSpeedMult = isBackpedaling ? -0.85f : 1.0f;
            CurrentAnimSpeedMult = animSpeedMult;

            if (animator != null)
            {
                float targetAnimSpeed = 0f;
                if (hasMoveInput)
                {
                    if (isBackpedaling)
                    {
                        targetAnimSpeed = IsCrouching ? 1.8f : 2.3f;
                    }
                    else if (IsCrouching)
                    {
                        targetAnimSpeed = 1.1f * effectiveTerrain;
                    }
                    else if (IsSprinting || IsGodMode)
                    {
                        targetAnimSpeed = 5.2f * effectiveTerrain;
                    }
                    else
                    {
                        targetAnimSpeed = 2.3f * effectiveTerrain;
                    }
                }

                SetAnimatorSpeedMultiplier(animSpeedMult);
                if (Time.deltaTime > 0f)
                {
                    animator.SetFloat(HashSpeed, targetAnimSpeed, 0.08f, Time.deltaTime);
                }
                else
                {
                    animator.SetFloat(HashSpeed, targetAnimSpeed);
                }
            }

            if (hasMoveInput && isInteractingWithCrate)
            {
                CancelCrateInteraction();
            }
        }

        private void UpdateDodgeMovement()
        {
            dodgeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dodgeTimer / Mathf.Max(0.05f, playerConfig.DodgeDuration));
            // Ease-Out burst deceleration
            float easeOutFactor = Mathf.Pow(1f - t, 1.35f) * 1.15f + 0.15f;
            float stepSpeed = playerConfig.DodgeSpeed * Mathf.Clamp01(easeOutFactor);
            CurrentSpeed = stepSpeed;

            if (IsGodMode)
            {
                Vector3 nextPos = transform.position + dodgeDirection * (stepSpeed * Time.deltaTime);
                nextPos.y = 0.05f;
                transform.position = nextPos;
            }
            else if (charController != null)
            {
                if (!charController.enabled) charController.enabled = true;
                verticalVelocity = charController.isGrounded ? -0.5f : verticalVelocity + playerConfig.Gravity * Time.deltaTime;
                Vector3 move = dodgeDirection * stepSpeed + Vector3.up * verticalVelocity;
                charController.Move(move * Time.deltaTime);
            }

            if (animator != null)
            {
                SetAnimatorSpeedMultiplier(dodgeIsBackstep ? -1.1f : 1.25f);
                animator.SetFloat(HashSpeed, 5.2f);
            }

            if (dodgeTimer >= playerConfig.DodgeDuration)
            {
                IsDodging = false;
                CurrentAnimSpeedMult = 1.0f;
                SetAnimatorSpeedMultiplier(1.0f);
            }
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
            {
                hasAnimSpeedMultParam = false;
                animParamChecked = false;
                return;
            }
            if (animParamChecked) return;
            hasAnimSpeedMultParam = false;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == HashAnimSpeedMult && p.type == AnimatorControllerParameterType.Float)
                {
                    hasAnimSpeedMultParam = true;
                    break;
                }
            }
            animParamChecked = true;
        }

        private void SetAnimatorSpeedMultiplier(float value)
        {
            if (animator == null) return;
            if (!animParamChecked) CacheAnimatorParameters();
            if (hasAnimSpeedMultParam)
            {
                animator.SetFloat(HashAnimSpeedMult, value);
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

            float regenBoost = (GameManager.Instance != null && GameManager.Instance.IsAdrenalineActive) ? 2.0f : 1.0f;

            if (IsSprinting)
            {
                float drain = playerConfig.StaminaDrainRate * (regenBoost > 1f ? 0.5f : 1f);
                CurrentStamina = Mathf.Max(0f, CurrentStamina - drain * Time.deltaTime);
            }
            else if (!IsDodging)
            {
                CurrentStamina = Mathf.Min(playerConfig.MaxStamina, CurrentStamina + playerConfig.StaminaRegenRate * regenBoost * Time.deltaTime);
            }
            OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
        }

        private void ExecutePunch(bool orientToCursor = false)
        {
            nextPunchTime = Time.time + playerConfig.PunchCooldown;
            IsSprinting = false;

            // CRITICAL: Orient toward cursor ONLY on LMB or when IsAiming == true;
            // [F]/[Space] while un-aimed punches straight along transform.forward.
            if (orientToCursor || IsAiming)
            {
                UpdateCursorAimPoint();
                if (LastValidAimDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(LastValidAimDir);
                }
            }

            // 3-Hit Combo progression
            if (Time.time - lastPunchTriggerTime <= playerConfig.ComboResetWindow)
            {
                comboStep = (comboStep + 1) % 3;
            }
            else
            {
                comboStep = 0;
            }
            lastPunchTriggerTime = Time.time;

            bool isFinisher = comboStep == 2;
            bool isCrit = UnityEngine.Random.value < playerConfig.CritChance;
            pendingDamageMultiplier = (isFinisher ? playerConfig.ComboFinisherMultiplier : 1.0f) *
                                      (isCrit ? playerConfig.CritMultiplier : 1.0f);
            pendingIsCrit = isCrit;

            if (animator != null)
            {
                if (Application.isPlaying && animator.GetCurrentAnimatorStateInfo(0).IsName("Punch"))
                {
                    animator.ResetTrigger(HashPunch);
                    animator.CrossFadeInFixedTime("Punch", 0.05f, 0, 0f);
                }
                else
                {
                    animator.SetTrigger(HashPunch);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCueScaled(SfxCue.PunchSwing, transform.position, 1f, isFinisher ? 0.88f : 1.0f);
            }
            CombatVfxManager.Instance?.PlayPunchSwing(transform.position, transform.forward, isFinisher);

            OnPunchExecuted?.Invoke();

            if (playerConfig.PunchHitDelay > 0f && Application.isPlaying)
            {
                if (activePunchRoutine != null) StopCoroutine(activePunchRoutine);
                activePunchRoutine = StartCoroutine(ExecutePunchHitRoutine(pendingDamageMultiplier, pendingIsCrit));
            }
            else
            {
                PerformPunchHitDetectionInternal(pendingDamageMultiplier, pendingIsCrit);
            }
        }

        private IEnumerator ExecutePunchHitRoutine(float damageMult, bool isCrit)
        {
            yield return new WaitForSeconds(playerConfig.PunchHitDelay);
            activePunchRoutine = null;
            if (!IsDead)
            {
                PerformPunchHitDetectionInternal(damageMult, isCrit);
            }
        }

        /// <summary>
        /// Direct deterministic hit check used by Test #12 (exact PunchDamage).
        /// </summary>
        public void PerformPunchHitDetection()
        {
            PerformPunchHitDetectionInternal(1.0f, false);
        }

        private void PerformPunchHitDetectionInternal(float damageMultiplier, bool isCrit)
        {
            Vector3 hitCenter = transform.position + transform.forward * (playerConfig.PunchRange * 0.55f);
            Collider[] hitCols = Physics.OverlapSphere(hitCenter, playerConfig.PunchRange, ~0, QueryTriggerInteraction.Ignore);
            bool hitSomething = false;
            var hitZombies = new System.Collections.Generic.HashSet<AI.ZombieController>();
            var hitBlockers = new System.Collections.Generic.HashSet<BlockingObject>();
            Vector3 playerChest = transform.position + Vector3.up * 1.0f;

            float dmg = playerConfig.PunchDamage * Mathf.Max(0.1f, damageMultiplier);

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
                    Vector3 targetChest = col.bounds.center;
                    if (IsPunchOccludedByWall(playerChest, targetChest, col))
                    {
                        continue;
                    }

                    var zombie = col.GetComponentInParent<AI.ZombieController>();
                    if (zombie != null && !zombie.IsDead && hitZombies.Add(zombie))
                    {
                        zombie.TakeDamage(dmg, transform.position);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayCue(SfxCue.PunchHit, col.transform.position);
                        CombatVfxManager.Instance?.PlayHitBlood(col.transform.position, isCrit);
                        DamagePopupManager.Instance?.ShowDamage(zombie.transform.position + Vector3.up * 2.0f, Mathf.RoundToInt(dmg), isCrit);
                        hitSomething = true;
                        continue;
                    }

                    var blocker = col.GetComponentInParent<BlockingObject>();
                    if (blocker != null && blocker.IsBreakable && !blocker.IsDestroyed && hitBlockers.Add(blocker))
                    {
                        blocker.ApplyDamage(dmg);
                        DamagePopupManager.Instance?.ShowDamage(blocker.transform.position + Vector3.up * 1.4f, Mathf.RoundToInt(dmg), false);
                        hitSomething = true;
                    }
                }
            }

            if (hitSomething)
            {
                if (IsometricCameraController.Instance != null)
                {
                    IsometricCameraController.Instance.TriggerShake(isCrit ? 0.30f : 0.20f, 0.16f);
                }

                if (Application.isPlaying && playerConfig.HitstopDuration > 0f && gameObject.activeInHierarchy)
                {
                    if (activeHitstopRoutine != null) StopCoroutine(activeHitstopRoutine);
                    activeHitstopRoutine = StartCoroutine(HitstopRoutine(playerConfig.HitstopDuration));
                }
            }
        }

        private IEnumerator HitstopRoutine(float duration)
        {
            var gm = GameManager.Instance;
            if (Time.timeScale <= 0f || (gm != null && (gm.IsPaused || gm.IsInMainMenu)))
            {
                activeHitstopRoutine = null;
                yield break;
            }

            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);

            if (gm == null || (!gm.IsPaused && !(CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible)))
            {
                Time.timeScale = 1.0f;
            }
            activeHitstopRoutine = null;
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
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, playerConfig.InteractionScanRadius, crateOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            SuppliesCrate nearestCrate = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = crateOverlapBuffer[i];
                if (col == null) continue;
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
            bool hasMoveInput = GetMovementInput().sqrMagnitude > 0.01f;
            if (isHoldingInteract && activeTargetCrate != null && !IsDodging && !hasMoveInput)
            {
                if (!isInteractingWithCrate)
                {
                    isInteractingWithCrate = true;
                    IsAiming = false;
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
            if (IsDead || IsInvulnerable) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));
            OnHealthChanged?.Invoke(CurrentHealth, playerConfig.MaxHealth);

            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.RecordDamageReceived(amount);
                PlayerPerformanceTracker.Instance.UpdatePlayerHealth(CurrentHealth, playerConfig.MaxHealth);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PlayerHurt, transform.position);
            }

            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.TriggerShake(0.35f, 0.25f);
            }

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

        public void RestoreStamina(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentStamina = Mathf.Min(playerConfig.MaxStamina, CurrentStamina + amount);
            OnStaminaChanged?.Invoke(CurrentStamina, playerConfig.MaxStamina);
        }

        private void Die()
        {
            if (activePunchRoutine != null)
            {
                StopCoroutine(activePunchRoutine);
                activePunchRoutine = null;
            }
            if (activeHitstopRoutine != null)
            {
                StopCoroutine(activeHitstopRoutine);
                activeHitstopRoutine = null;
                Time.timeScale = 1f;
            }

            IsAiming = false;
            IsDodging = false;
            IsSprinting = false;
            iframeRemaining = 0f;
            aimInputHeld = false;
            testAimOverrideActive = false;
            CancelCrateInteraction();
            var perf = PlayerPerformanceTracker.Instance ?? FindAnyObjectByType<PlayerPerformanceTracker>();
            perf?.SetTimePaused(true);

            if (animator != null)
            {
                animator.ResetTrigger(HashPunch);
                animator.SetBool(HashDead, true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PlayerDeath, transform.position);
            }

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
                case Key.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
                case Key.Digit1: return Keyboard.current.digit1Key.wasPressedThisFrame;
                case Key.Digit2: return Keyboard.current.digit2Key.wasPressedThisFrame;
                case Key.Digit3: return Keyboard.current.digit3Key.wasPressedThisFrame;
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

        private bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            if (button == 0) return Mouse.current.leftButton.isPressed;
            if (button == 1) return Mouse.current.rightButton.isPressed;
            return false;
#else
            return Input.GetMouseButton(button);
#endif
        }
        #endregion
    }
}
