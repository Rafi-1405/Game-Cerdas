using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;
using UTS.Audio;

namespace DeadDepths.AI
{
    /// <summary>
    /// Controls zombie behaviors, shared FSM, and archetype-specific attacks.
    /// ASD-STE100: This AI controller guides Walker, Spitter, and Charger zombies.
    /// Follows Section 5 of the specification.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieController : MonoBehaviour, IMudSlowable
    {
        #region CONFIG
        [Serializable]
        public class ArchetypeConfig
        {
            [Header("General Attributes")]
            public float MaxHealth = 80f;
            public float BaseMoveSpeed = 3.5f;
            public float VisionRange = 16f;
            public float VisionAngle = 110f;
            public float AttackRange = 2.0f;

            [Header("Walker Specific")]
            public float MeleeDamage = 18f;
            public float MeleeCooldown = 1.4f;

            [Header("Spitter Specific")]
            public float SpitterAttackRadius = 10f;
            public float SpitterPreparationDuration = 1.0f;
            public float SpitterRecoveryDuration = 3.0f;

            [Header("Charger Specific")]
            public float ChargerActivationRange = 9.0f;
            public float ChargerTelegraphDuration = 3.0f;
            public float ChargerStepBackDuration = 0.5f;
            public float ChargerSpeed = 15f;
            public float ChargerStunDuration = 5.0f;
            public float ChargerImpactDamage = 35f;
        }
        #endregion

        [SerializeField] private ZombieArchetype archetype = ZombieArchetype.Walker;
        [SerializeField] private ArchetypeConfig aiConfig = new ArchetypeConfig();

        public ZombieArchetype Archetype => archetype;
        public ZombieState CurrentState { get; private set; } = ZombieState.IdlePatrol;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        private NavMeshAgent agent;
        private Animator animator;
        private Transform playerTarget;
        private DeadDepthsPlayerController playerController;

        private float terrainMultiplier = 1.0f;
        private float nextAttackTime = 0f;
        private float stateTimer = 0f;
        private Vector3 lastKnownPlayerPos;
        private ChargerTelegraph chargerTelegraph;

        // Spitter state variables
        private enum SpitterPhase { None, Preparation, Recovery }
        private SpitterPhase spitterPhase = SpitterPhase.None;
        private Vector3 capturedSpitTarget;

        // Charger state variables
        private enum ChargerPhase { None, TelegraphTracking, StepBack, Charging, Stunned }
        private ChargerPhase chargerPhase = ChargerPhase.None;
        private Vector3 lockedChargeDirection;
        private float chargerPhaseTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            chargerTelegraph = GetComponentInChildren<ChargerTelegraph>();

            ConfigureArchetypeSettings();
            CurrentHealth = aiConfig.MaxHealth;
        }

        private void Start()
        {
            FindPlayer();
        }

        private void ConfigureArchetypeSettings()
        {
            switch (archetype)
            {
                case ZombieArchetype.Spitter:
                    aiConfig.BaseMoveSpeed = 6.2f;
                    aiConfig.MaxHealth = 60f;
                    aiConfig.AttackRange = aiConfig.SpitterAttackRadius;
                    break;
                case ZombieArchetype.Charger:
                    aiConfig.BaseMoveSpeed = 2.4f;
                    aiConfig.MaxHealth = 160f;
                    aiConfig.AttackRange = aiConfig.ChargerActivationRange;
                    transform.localScale = Vector3.one * 1.3f;
                    break;
                case ZombieArchetype.Walker:
                default:
                    aiConfig.BaseMoveSpeed = 3.5f;
                    aiConfig.MaxHealth = 80f;
                    aiConfig.AttackRange = 2.0f;
                    break;
            }

            if (agent != null)
            {
                agent.speed = aiConfig.BaseMoveSpeed;
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTarget = p.transform;
                playerController = p.GetComponent<DeadDepthsPlayerController>();
            }
        }

        private void Update()
        {
            if (IsDead) return;

            if (playerTarget == null)
            {
                FindPlayer();
                if (playerTarget == null) return;
            }

            // Normal speed adjustment with mud
            if (agent != null && agent.enabled && chargerPhase != ChargerPhase.Charging)
            {
                agent.speed = aiConfig.BaseMoveSpeed * terrainMultiplier;
            }

            switch (CurrentState)
            {
                case ZombieState.IdlePatrol:
                    UpdateIdlePatrol();
                    break;
                case ZombieState.Chase:
                    UpdateChase();
                    break;
                case ZombieState.Attack:
                    UpdateAttack();
                    break;
                case ZombieState.Search:
                    UpdateSearch();
                    break;
            }

            // Update animator
            if (animator != null && agent != null)
            {
                float normSpeed = agent.velocity.magnitude / Mathf.Max(0.1f, aiConfig.BaseMoveSpeed);
                animator.SetFloat("Speed", normSpeed);
            }
        }

        private bool CanSeePlayer()
        {
            if (playerTarget == null || playerController.IsDead) return false;

            Vector3 dir = playerTarget.position - transform.position;
            float dist = dir.magnitude;
            if (dist > aiConfig.VisionRange) return false;

            float angle = Vector3.Angle(transform.forward, dir.normalized);
            if (angle > aiConfig.VisionAngle * 0.5f) return false;

            // Line of sight raycast
            Vector3 eyePos = transform.position + Vector3.up * 1.4f;
            Vector3 targetCenter = playerTarget.position + Vector3.up * 1.0f;

            if (Physics.Linecast(eyePos, targetCenter, out RaycastHit hit))
            {
                if (hit.collider.transform == playerTarget || hit.collider.transform.IsChildOf(playerTarget))
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        private void UpdateIdlePatrol()
        {
            if (CanSeePlayer())
            {
                TransitionToState(ZombieState.Chase);
                if (AudioManager.Instance != null && UnityEngine.Random.value < 0.3f)
                {
                    AudioManager.Instance.PlayCue(SfxCue.ZombieGroan, transform.position);
                }
            }
        }

        private void UpdateChase()
        {
            if (playerTarget == null || playerController.IsDead)
            {
                TransitionToState(ZombieState.IdlePatrol);
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Check if player in attack range
            if (distToPlayer <= aiConfig.AttackRange)
            {
                TransitionToState(ZombieState.Attack);
                return;
            }

            // Update destination
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position);
            }

            // Lost player line of sight
            if (!CanSeePlayer())
            {
                lastKnownPlayerPos = playerTarget.position;
                stateTimer += Time.deltaTime;
                if (stateTimer > 4.0f)
                {
                    TransitionToState(ZombieState.Search);
                }
            }
            else
            {
                stateTimer = 0f;
            }
        }

        private void UpdateSearch()
        {
            if (CanSeePlayer())
            {
                TransitionToState(ZombieState.Chase);
                return;
            }

            stateTimer += Time.deltaTime;
            if (stateTimer > 5.0f)
            {
                TransitionToState(ZombieState.IdlePatrol);
            }
        }

        private void UpdateAttack()
        {
            switch (archetype)
            {
                case ZombieArchetype.Walker:
                    UpdateWalkerAttack();
                    break;
                case ZombieArchetype.Spitter:
                    UpdateSpitterAttack();
                    break;
                case ZombieArchetype.Charger:
                    UpdateChargerAttack();
                    break;
            }
        }

        #region Walker Attack
        private void UpdateWalkerAttack()
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            if (dist > aiConfig.AttackRange * 1.3f)
            {
                TransitionToState(ZombieState.Chase);
                return;
            }

            // Face player
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + aiConfig.MeleeCooldown;

                if (animator != null) animator.SetTrigger("Attack");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCue(SfxCue.ZombieAttack, transform.position);

                if (dist <= aiConfig.AttackRange)
                {
                    playerController.TakeDamage(aiConfig.MeleeDamage);
                }
            }
        }
        #endregion

        #region Spitter Attack Sequence (Section 5.3)
        private void UpdateSpitterAttack()
        {
            float dt = Time.deltaTime;
            chargerPhaseTimer += dt;

            switch (spitterPhase)
            {
                case SpitterPhase.None:
                    // Step 2: Preparation - Stop moving for exactly 1.0s
                    spitterPhase = SpitterPhase.Preparation;
                    chargerPhaseTimer = 0f;
                    if (agent != null && agent.enabled) agent.isStopped = true;
                    break;

                case SpitterPhase.Preparation:
                    // Remain stationary for 1.0s
                    if (chargerPhaseTimer >= aiConfig.SpitterPreparationDuration)
                    {
                        // Step 3 & 4: Capture player position and spew projectile
                        capturedSpitTarget = playerTarget.position;
                        LaunchToxicProjectile(capturedSpitTarget);

                        spitterPhase = SpitterPhase.Recovery;
                        chargerPhaseTimer = 0f;
                    }
                    break;

                case SpitterPhase.Recovery:
                    // Step 5: Remain stationary for exactly 3.0s after launching
                    if (chargerPhaseTimer >= aiConfig.SpitterRecoveryDuration)
                    {
                        // Step 6: Resume chase
                        spitterPhase = SpitterPhase.None;
                        if (agent != null && agent.enabled) agent.isStopped = false;
                        TransitionToState(ZombieState.Chase);
                    }
                    break;
            }
        }

        private void LaunchToxicProjectile(Vector3 targetPos)
        {
            GameObject projObj = new GameObject("ToxicSpitProjectile");
            projObj.transform.position = transform.position + Vector3.up * 1.5f;

            var proj = projObj.AddComponent<SpitterProjectile>();
            var sphereCol = projObj.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.35f;

            // Visual green venom sphere
            GameObject sphereVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereVisual.transform.SetParent(projObj.transform, false);
            sphereVisual.transform.localScale = Vector3.one * 0.45f;
            var rend = sphereVisual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.2f, 1.0f, 0.3f);
            rend.material = mat;
            Destroy(sphereVisual.GetComponent<Collider>());

            proj.Launch(targetPos);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieAttack, transform.position);
            }
        }
        #endregion

        #region Charger Attack Sequence (Section 5.4)
        private void UpdateChargerAttack()
        {
            float dt = Time.deltaTime;
            chargerPhaseTimer += dt;

            switch (chargerPhase)
            {
                case ChargerPhase.None:
                    // Step 2: Telegraph - display directional indicator toward player
                    chargerPhase = ChargerPhase.TelegraphTracking;
                    chargerPhaseTimer = 0f;
                    if (agent != null && agent.enabled) agent.isStopped = true;
                    break;

                case ChargerPhase.TelegraphTracking:
                    // Step 3: For 3.0s, indicator continuously follows player
                    Vector3 trackDir = (playerTarget.position - transform.position).normalized;
                    trackDir.y = 0f;
                    transform.rotation = Quaternion.LookRotation(trackDir);

                    if (chargerTelegraph != null)
                    {
                        chargerTelegraph.ShowTelegraph(transform.position, trackDir);
                    }

                    if (chargerPhaseTimer >= aiConfig.ChargerTelegraphDuration)
                    {
                        // Step 4: Lock last captured player position and calculate charge direction
                        lockedChargeDirection = trackDir;
                        if (chargerTelegraph != null) chargerTelegraph.HideTelegraph();

                        // Step 5: Step back for 0.5s
                        chargerPhase = ChargerPhase.StepBack;
                        chargerPhaseTimer = 0f;
                    }
                    break;

                case ChargerPhase.StepBack:
                    // Step back slightly along reverse locked direction
                    transform.position -= lockedChargeDirection * (1.5f * dt);

                    if (chargerPhaseTimer >= aiConfig.ChargerStepBackDuration)
                    {
                        // Step 6: Charge rapidly along locked direction
                        chargerPhase = ChargerPhase.Charging;
                        chargerPhaseTimer = 0f;
                        if (agent != null) agent.enabled = false; // Disable NavMesh to allow high speed straight charge
                    }
                    break;

                case ChargerPhase.Charging:
                    float chargeStep = aiConfig.ChargerSpeed * dt;
                    Vector3 moveVec = lockedChargeDirection * chargeStep;

                    // Spherecast to prevent tunneling through walls at high speed (Section 5.4)
                    if (Physics.SphereCast(transform.position + Vector3.up * 0.8f, 0.6f, lockedChargeDirection, out RaycastHit hit, chargeStep))
                    {
                        // Check what was hit
                        var player = hit.collider.GetComponentInParent<DeadDepthsPlayerController>();
                        if (player != null)
                        {
                            player.TakeDamage(aiConfig.ChargerImpactDamage);
                        }

                        var blocker = hit.collider.GetComponentInParent<BlockingObject>();
                        if (blocker != null && blocker.IsBreakable)
                        {
                            blocker.ApplyDamage(aiConfig.ChargerImpactDamage);
                        }

                        // Wall collision triggers 5.0s stun (Step 7 & 8)
                        StartChargerStun();
                        return;
                    }

                    transform.position += moveVec;

                    // Timeout safety so Charger never charges infinitely
                    if (chargerPhaseTimer > 4.0f)
                    {
                        StartChargerStun();
                    }
                    break;

                case ChargerPhase.Stunned:
                    // Step 8: Remain stunned for 5.0 seconds
                    if (chargerPhaseTimer >= aiConfig.ChargerStunDuration)
                    {
                        // Step 9: Resume normal behavior
                        chargerPhase = ChargerPhase.None;
                        if (agent != null)
                        {
                            agent.enabled = true;
                            if (agent.isOnNavMesh) agent.isStopped = false;
                        }
                        TransitionToState(ZombieState.Chase);
                    }
                    break;
            }
        }

        private void StartChargerStun()
        {
            chargerPhase = ChargerPhase.Stunned;
            chargerPhaseTimer = 0f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchHit, transform.position);
                AudioManager.Instance.PlayCue(SfxCue.BodyFall, transform.position);
            }

            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.TriggerShake(0.5f, 0.35f);
            }
        }
        #endregion

        private void TransitionToState(ZombieState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            stateTimer = 0f;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieAttack, transform.position);
            }

            // Hurt flinch
            if (animator != null) animator.SetTrigger("Attack");

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            CurrentState = ZombieState.Dead;
            if (agent != null && agent.enabled) agent.isStopped = true;

            if (animator != null) animator.SetBool("Dead", true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieDeath, transform.position);
            }

            // Notify performance tracker for DDA
            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.RecordZombieKill();
            }

            // Disable colliders
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;

            Destroy(gameObject, 3.0f);
        }

        public void SetTerrainSpeedMultiplier(float multiplier)
        {
            // Charger during active charge is not slowed by mud (Section 5.5)
            if (chargerPhase == ChargerPhase.Charging) return;
            terrainMultiplier = Mathf.Clamp(multiplier, 0.3f, 1.0f);
        }
    }
}
