using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;
using DeadDepths.UI;
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

            [Header("Patrol Settings")]
            public float PatrolRadius = 8.0f;
            public float PatrolWaitTime = 2.0f;

            [Header("Walker Specific")]
            public float MeleeDamage = 18f;
            public float MeleeCooldown = 1.4f;
            public float MeleeHitDelay = 0.35f;

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
        public ArchetypeConfig AiConfig => aiConfig;
        public ZombieState CurrentState { get; private set; } = ZombieState.IdlePatrol;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public NavMeshAgent Agent => agent;
        public Vector3 LastKnownPlayerPos => lastKnownPlayerPos;
        public Vector3 CurrentPatrolTarget => currentPatrolTarget;
        public bool HasLineOfSightToPlayer { get; private set; }

        public string SubPhaseName
        {
            get
            {
                if (archetype == ZombieArchetype.Spitter && spitterPhase != SpitterPhase.None)
                    return spitterPhase.ToString();
                if (archetype == ZombieArchetype.Charger && chargerPhase != ChargerPhase.None)
                    return chargerPhase.ToString();
                return string.Empty;
            }
        }

        private NavMeshAgent agent;
        private Animator animator;
        private Transform playerTarget;
        private DeadDepthsPlayerController playerController;
        private Renderer[] childRenderers;
        private MaterialPropertyBlock propBlock;
        private Color baseTintColor = Color.white;
        private Coroutine hitFlashRoutine;
        private Coroutine meleeHitRoutine;

        private float terrainMultiplier = 1.0f;
        private float nextAttackTime = 0f;
        private float stateTimer = 0f;
        private Vector3 spawnPosition;
        private Vector3 lastKnownPlayerPos;
        private Vector3 currentPatrolTarget;
        private bool hasPatrolTarget = false;
        private float patrolPauseTimer = 0f;
        private float patrolMoveTimer = 0f;
        private ChargerTelegraph chargerTelegraph;

        // Spitter state variables
        private enum SpitterPhase { None, Preparation, Recovery }
        private SpitterPhase spitterPhase = SpitterPhase.None;
        private bool spitterAnimTriggered = false;
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
            EnforceAnimatorNoRootMotion();
            chargerTelegraph = GetComponentInChildren<ChargerTelegraph>();
            childRenderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
            spawnPosition = transform.position;
            lastKnownPlayerPos = transform.position;

            ConfigureArchetypeSettings();
            CurrentHealth = aiConfig.MaxHealth;
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

        private void Start()
        {
            FindPlayer();
        }

        /// <summary>
        /// Explicitly initializes archetype stats, scale, and visual differentiation at spawn time.
        /// Fixes B1 where Awake() previously ran before Reflection changed archetype.
        /// </summary>
        public void InitializeArchetype(ZombieArchetype arch)
        {
            archetype = arch;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            EnforceAnimatorNoRootMotion();
            if (childRenderers == null || childRenderers.Length == 0) childRenderers = GetComponentsInChildren<Renderer>();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            spawnPosition = transform.position;
            lastKnownPlayerPos = transform.position;
            CurrentState = ZombieState.IdlePatrol;
            spitterPhase = SpitterPhase.None;
            chargerPhase = ChargerPhase.None;
            hasPatrolTarget = false;

            ConfigureArchetypeSettings();
            CurrentHealth = aiConfig.MaxHealth;

            if (arch == ZombieArchetype.Charger)
            {
                chargerTelegraph = GetComponentInChildren<ChargerTelegraph>();
                if (chargerTelegraph == null)
                {
                    chargerTelegraph = gameObject.AddComponent<ChargerTelegraph>();
                }
            }
        }

        private void ConfigureArchetypeSettings()
        {
            switch (archetype)
            {
                case ZombieArchetype.Spitter:
                    aiConfig.BaseMoveSpeed = 6.2f;
                    aiConfig.MaxHealth = 60f;
                    aiConfig.AttackRange = aiConfig.SpitterAttackRadius;
                    transform.localScale = Vector3.one * 0.95f;
                    baseTintColor = new Color(0.45f, 1.0f, 0.50f);
                    break;
                case ZombieArchetype.Charger:
                    aiConfig.BaseMoveSpeed = 2.4f;
                    aiConfig.MaxHealth = 160f;
                    aiConfig.AttackRange = aiConfig.ChargerActivationRange;
                    transform.localScale = Vector3.one * 1.3f;
                    baseTintColor = new Color(1.0f, 0.42f, 0.36f);
                    break;
                case ZombieArchetype.Walker:
                default:
                    aiConfig.BaseMoveSpeed = 3.5f;
                    aiConfig.MaxHealth = 80f;
                    aiConfig.AttackRange = 2.0f;
                    transform.localScale = Vector3.one;
                    baseTintColor = new Color(0.85f, 0.88f, 0.85f);
                    break;
            }

            if (agent != null)
            {
                agent.speed = aiConfig.BaseMoveSpeed;
                agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360f);
            }

            ApplyRendererColor(baseTintColor);
        }

        private void ApplyRendererColor(Color c)
        {
            if (childRenderers == null || propBlock == null) return;
            foreach (var r in childRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", c);
                propBlock.SetColor("_Color", c);
                r.SetPropertyBlock(propBlock);
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
            if (Time.timeScale <= 0f || (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible))
            {
                return;
            }

            if (playerTarget == null)
            {
                FindPlayer();
                if (playerTarget == null) return;
            }

            HasLineOfSightToPlayer = CanSeePlayer();

            // Speed adjustment per FSM state and mud multiplier
            if (agent != null && agent.enabled && chargerPhase != ChargerPhase.Charging)
            {
                float targetSpeed = aiConfig.BaseMoveSpeed;
                if (CurrentState == ZombieState.IdlePatrol)
                {
                    // Cap patrol speed so fast archetypes (e.g. Spitter) walk rather than sprint while patrolling
                    targetSpeed = Mathf.Min(aiConfig.BaseMoveSpeed * 0.45f, 1.25f);
                }
                else if (CurrentState == ZombieState.Search)
                {
                    targetSpeed = aiConfig.BaseMoveSpeed * 0.80f;
                }
                agent.speed = targetSpeed * terrainMultiplier;
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

            // Sync Animator Speed parameter with Zombie.controller thresholds:
            // 0 = Idle, 0.9 = Walk, 2.3 = Run
            if (animator != null)
            {
                float actualWorldSpeed = 0f;
                if (chargerPhase == ChargerPhase.Charging)
                {
                    actualWorldSpeed = aiConfig.ChargerSpeed;
                }
                else if (chargerPhase == ChargerPhase.StepBack)
                {
                    actualWorldSpeed = 1.5f;
                }
                else if (agent != null && agent.enabled)
                {
                    actualWorldSpeed = agent.velocity.magnitude;
                }

                if (Time.deltaTime > 0f)
                {
                    animator.SetFloat("Speed", actualWorldSpeed, 0.1f, Time.deltaTime);
                }
                else
                {
                    animator.SetFloat("Speed", actualWorldSpeed);
                }
            }
        }

        private void LateUpdate()
        {
            EnforceAnimatorNoRootMotion();
        }

        private bool CanSeePlayer()
        {
            if (playerTarget == null || (playerController != null && playerController.IsDead)) return false;

            Vector3 dir = playerTarget.position - transform.position;
            float dist = dir.magnitude;
            if (dist > aiConfig.VisionRange) return false;

            Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, flatDir.normalized);
                if (angle > aiConfig.VisionAngle * 0.5f) return false;
            }

            // Line of sight raycast ignoring triggers (e.g. MuddyPatch) and other zombies
            Vector3 eyePos = transform.position + Vector3.up * 1.4f;
            Vector3 targetCenter = playerTarget.position + Vector3.up * 1.0f;
            return HasUnobstructedLineOfSightToPlayer(eyePos, targetCenter);
        }

        private bool HasUnobstructedLineOfSightToPlayer(Vector3 fromPos, Vector3 toPos)
        {
            if (playerTarget == null) return false;
            Vector3 delta = toPos - fromPos;
            float dist = delta.magnitude;
            if (dist <= 0.01f) return true;

            RaycastHit[] hits = Physics.RaycastAll(fromPos, delta / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return true;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<ZombieController>() != null) continue;
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
            if (HasLineOfSightToPlayer)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase);
                if (AudioManager.Instance != null && UnityEngine.Random.value < 0.3f)
                {
                    AudioManager.Instance.PlayCue(SfxCue.ZombieGroan, transform.position);
                }
                return;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            agent.isStopped = false;

            if (!hasPatrolTarget)
            {
                patrolPauseTimer -= Time.deltaTime;
                if (patrolPauseTimer <= 0f)
                {
                    Vector2 randCircle = UnityEngine.Random.insideUnitCircle * aiConfig.PatrolRadius;
                    Vector3 candidate = spawnPosition + new Vector3(randCircle.x, 0f, randCircle.y);
                    if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 4.0f, NavMesh.AllAreas))
                    {
                        currentPatrolTarget = navHit.position;
                        agent.SetDestination(currentPatrolTarget);
                        hasPatrolTarget = true;
                        patrolMoveTimer = 0f;
                    }
                    else
                    {
                        patrolPauseTimer = 1.0f;
                    }
                }
            }
            else
            {
                patrolMoveTimer += Time.deltaTime;
                if ((!agent.pathPending && (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.8f) ||
                     agent.pathStatus == NavMeshPathStatus.PathInvalid)) || patrolMoveTimer > 6.5f)
                {
                    hasPatrolTarget = false;
                    patrolMoveTimer = 0f;
                    patrolPauseTimer = UnityEngine.Random.Range(1.0f, aiConfig.PatrolWaitTime);
                }
            }
        }

        private void UpdateChase()
        {
            if (playerTarget == null || (playerController != null && playerController.IsDead))
            {
                TransitionToState(ZombieState.IdlePatrol);
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Update destination
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position);
            }

            // Check if player is in attack range and unobstructed (Walkers don't require FOV angle once chasing, only unobstructed LOS)
            bool hasDirectLOS = HasUnobstructedLineOfSightToPlayer(transform.position + Vector3.up * 1.1f, playerTarget.position + Vector3.up * 1.0f);
            bool canEngage = archetype == ZombieArchetype.Walker
                ? (distToPlayer <= aiConfig.AttackRange && hasDirectLOS)
                : (distToPlayer <= aiConfig.AttackRange && HasLineOfSightToPlayer);

            if (canEngage)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Attack);
                return;
            }

            // Track last known position & lose sight timeout
            if (HasLineOfSightToPlayer)
            {
                lastKnownPlayerPos = playerTarget.position;
                stateTimer = 0f;
            }
            else
            {
                stateTimer += Time.deltaTime;
                if (stateTimer > 4.0f)
                {
                    TransitionToState(ZombieState.Search);
                }
            }
        }

        private void UpdateSearch()
        {
            if (HasLineOfSightToPlayer)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase);
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPos);
            }

            stateTimer += Time.deltaTime;
            bool reachedLastKnown = agent != null && agent.enabled && agent.isOnNavMesh &&
                                    !agent.pathPending && agent.remainingDistance <= 1.2f;

            if (stateTimer > 5.0f || (reachedLastKnown && stateTimer > 2.0f))
            {
                hasPatrolTarget = false;
                TransitionToState(ZombieState.IdlePatrol);
            }
        }

        private void UpdateAttack()
        {
            if (playerTarget == null || (playerController != null && playerController.IsDead))
            {
                if (chargerTelegraph != null) chargerTelegraph.HideTelegraph();
                spitterPhase = SpitterPhase.None;
                chargerPhase = ChargerPhase.None;
                if (agent != null)
                {
                    agent.enabled = true;
                    if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 8.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(navHit.position);
                    }
                    if (agent.isOnNavMesh) agent.isStopped = false;
                }
                TransitionToState(ZombieState.IdlePatrol);
                return;
            }

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
            bool hasDirectLOS = HasUnobstructedLineOfSightToPlayer(transform.position + Vector3.up * 1.1f, playerTarget.position + Vector3.up * 1.0f);
            if (dist > aiConfig.AttackRange * 1.3f || !hasDirectLOS)
            {
                TransitionToState(ZombieState.Chase);
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // Face player
            Vector3 lookDir = (playerTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 10f);
            }

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + aiConfig.MeleeCooldown;

                if (animator != null) animator.SetTrigger("Attack");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCue(SfxCue.ZombieAttack, transform.position);

                if (meleeHitRoutine != null) StopCoroutine(meleeHitRoutine);
                meleeHitRoutine = StartCoroutine(WalkerMeleeHitRoutine());
            }
        }

        private IEnumerator WalkerMeleeHitRoutine()
        {
            yield return new WaitForSeconds(aiConfig.MeleeHitDelay);
            meleeHitRoutine = null;

            if (IsDead || playerTarget == null || playerController == null || playerController.IsDead) yield break;

            Vector3 flatDelta = playerTarget.position - transform.position;
            flatDelta.y = 0f;
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            bool facingPlayer = flatDelta.sqrMagnitude <= 0.001f || dist <= 1.2f || Vector3.Dot(transform.forward, flatDelta.normalized) >= 0.25f;

            if (dist <= aiConfig.AttackRange * 1.35f &&
                facingPlayer &&
                HasUnobstructedLineOfSightToPlayer(transform.position + Vector3.up * 1.1f, playerTarget.position + Vector3.up * 1.0f))
            {
                playerController.TakeDamage(aiConfig.MeleeDamage);
            }
        }
        #endregion

        #region Spitter Attack Sequence (Section 5.3)
        private void UpdateSpitterAttack()
        {
            float dt = Time.deltaTime;
            chargerPhaseTimer += dt;

            // Face player during preparation
            if (spitterPhase == SpitterPhase.Preparation || spitterPhase == SpitterPhase.None)
            {
                Vector3 lookDir = playerTarget.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), dt * 8f);
                }
            }

            switch (spitterPhase)
            {
                case SpitterPhase.None:
                    // Step 2: Preparation - Stop moving for 1.0s
                    spitterPhase = SpitterPhase.Preparation;
                    spitterAnimTriggered = false;
                    chargerPhaseTimer = 0f;
                    if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                    break;

                case SpitterPhase.Preparation:
                    // Trigger Attack animation slightly before projectile release so swing aligns with spit
                    if (!spitterAnimTriggered && chargerPhaseTimer >= Mathf.Max(0f, aiConfig.SpitterPreparationDuration - 0.30f))
                    {
                        spitterAnimTriggered = true;
                        if (animator != null) animator.SetTrigger("Attack");
                    }

                    if (chargerPhaseTimer >= aiConfig.SpitterPreparationDuration)
                    {
                        // Step 3 & 4: Capture player chest position and spew projectile
                        if (!spitterAnimTriggered && animator != null)
                        {
                            spitterAnimTriggered = true;
                            animator.SetTrigger("Attack");
                        }
                        capturedSpitTarget = playerTarget.position + Vector3.up * 1.0f;
                        LaunchToxicProjectile(capturedSpitTarget);

                        spitterPhase = SpitterPhase.Recovery;
                        chargerPhaseTimer = 0f;
                    }
                    break;

                case SpitterPhase.Recovery:
                    if (chargerPhaseTimer >= aiConfig.SpitterRecoveryDuration)
                    {
                        spitterPhase = SpitterPhase.None;
                        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
                        TransitionToState(ZombieState.Chase);
                    }
                    break;
            }
        }

        private void LaunchToxicProjectile(Vector3 targetPos)
        {
            GameObject projObj = new GameObject("ToxicSpitProjectile");
            if (WorldGenerator.Instance != null && WorldGenerator.Instance.WorldRoot != null)
            {
                projObj.transform.SetParent(WorldGenerator.Instance.WorldRoot, true);
            }
            Vector3 forwardOffset = transform.forward * 0.6f;
            projObj.transform.position = transform.position + Vector3.up * 1.45f + forwardOffset;

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

            proj.Launch(targetPos, transform);

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
                    chargerPhase = ChargerPhase.TelegraphTracking;
                    chargerPhaseTimer = 0f;
                    if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                    break;

                case ChargerPhase.TelegraphTracking:
                    Vector3 trackDir = playerTarget.position - transform.position;
                    trackDir.y = 0f;
                    if (trackDir.sqrMagnitude > 0.001f)
                    {
                        trackDir.Normalize();
                        transform.rotation = Quaternion.LookRotation(trackDir);
                    }
                    else
                    {
                        trackDir = transform.forward;
                    }

                    if (chargerTelegraph != null)
                    {
                        chargerTelegraph.ShowTelegraph(transform.position, trackDir);
                    }

                    if (chargerPhaseTimer >= aiConfig.ChargerTelegraphDuration)
                    {
                        lockedChargeDirection = trackDir;
                        if (chargerTelegraph != null) chargerTelegraph.HideTelegraph();

                        chargerPhase = ChargerPhase.StepBack;
                        chargerPhaseTimer = 0f;
                    }
                    break;

                case ChargerPhase.StepBack:
                    Vector3 stepBackDelta = -lockedChargeDirection * (1.5f * dt);
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.Move(stepBackDelta);
                    }
                    else
                    {
                        transform.position += stepBackDelta;
                    }

                    if (chargerPhaseTimer >= aiConfig.ChargerStepBackDuration)
                    {
                        chargerPhase = ChargerPhase.Charging;
                        chargerPhaseTimer = 0f;
                        if (agent != null) agent.enabled = false;
                        if (animator != null) animator.SetTrigger("Attack");
                    }
                    break;

                case ChargerPhase.Charging:
                    float chargeStep = aiConfig.ChargerSpeed * dt;
                    Vector3 moveVec = lockedChargeDirection * chargeStep;

                    // Start spherecast slightly behind center, but ignore hits behind the charger's back
                    const float backOffset = 0.15f;
                    Vector3 castOrigin = transform.position + Vector3.up * 0.9f - lockedChargeDirection * backOffset;
                    RaycastHit[] hits = Physics.SphereCastAll(
                        castOrigin,
                        0.5f,
                        lockedChargeDirection,
                        chargeStep + backOffset,
                        ~0,
                        QueryTriggerInteraction.Ignore);

                    if (hits != null && hits.Length > 0)
                    {
                        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                        foreach (var hit in hits)
                        {
                            if (hit.collider == null) continue;
                            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                            if (hit.collider.gameObject.name == "PlayableGround") continue;

                            var player = hit.collider.GetComponentInParent<DeadDepthsPlayerController>();
                            if (player != null)
                            {
                                player.TakeDamage(aiConfig.ChargerImpactDamage);
                                StartChargerStun();
                                return;
                            }

                            // Ignore obstacles behind or laterally grazing the Charger when spherecast starts slightly behind center
                            if (hit.distance <= 0.001f)
                            {
                                Vector3 centerPos = transform.position + Vector3.up * 0.9f;
                                Vector3 closest = hit.collider.ClosestPoint(centerPos);
                                Vector3 toHit = closest - centerPos;
                                toHit.y = 0f;
                                if (toHit.sqrMagnitude > 0.0001f)
                                {
                                    if (Vector3.Dot(toHit.normalized, lockedChargeDirection) < 0.2f)
                                    {
                                        continue;
                                    }
                                }
                                else if (Vector3.Dot(hit.normal, lockedChargeDirection) > 0.1f)
                                {
                                    continue;
                                }
                            }

                            var blocker = hit.collider.GetComponentInParent<BlockingObject>();
                            if (blocker != null)
                            {
                                if (blocker.IsBreakable)
                                {
                                    blocker.ApplyDamage(aiConfig.ChargerImpactDamage);
                                }
                                StartChargerStun();
                                return;
                            }

                            // Ignore other zombies while charging through
                            if (hit.collider.GetComponentInParent<ZombieController>() != null)
                            {
                                continue;
                            }

                            // Hit solid wall/tree/ground obstacle -> 5.0s stun
                            StartChargerStun();
                            return;
                        }
                    }

                    transform.position += moveVec;

                    if (chargerPhaseTimer > 4.0f)
                    {
                        StartChargerStun();
                    }
                    break;

                case ChargerPhase.Stunned:
                    if (chargerPhaseTimer >= aiConfig.ChargerStunDuration)
                    {
                        chargerPhase = ChargerPhase.None;
                        if (agent != null)
                        {
                            agent.enabled = true;
                            if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 8.0f, NavMesh.AllAreas))
                            {
                                agent.Warp(navHit.position);
                            }
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
            if (CurrentState == ZombieState.Attack && chargerTelegraph != null)
            {
                chargerTelegraph.HideTelegraph();
            }
            CurrentState = newState;
            stateTimer = 0f;
        }

        public void TakeDamage(float amount, Vector3? attackerPos = null)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.PunchHit, transform.position);
                if (UnityEngine.Random.value < 0.5f)
                    AudioManager.Instance.PlayCue(SfxCue.ZombieGroan, transform.position);
            }

            // Visual hurt flash (no false Attack animation trigger!)
            if (gameObject.activeInHierarchy)
            {
                if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = StartCoroutine(HitFlashRoutine());
            }

            // Slight knockback away from attacker (except Charger mid-charge)
            if (attackerPos.HasValue && chargerPhase != ChargerPhase.Charging && CurrentHealth > 0f)
            {
                Vector3 kbDir = transform.position - attackerPos.Value;
                kbDir.y = 0f;
                if (kbDir.sqrMagnitude > 0.001f)
                {
                    Vector3 targetPos = transform.position + kbDir.normalized * 0.35f;
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.Move(kbDir.normalized * 0.35f);
                    }
                    else
                    {
                        transform.position = targetPos;
                    }
                }
            }

            // Alert zombie to Chase if hit while patrolling or searching
            if (CurrentHealth > 0f && (CurrentState == ZombieState.IdlePatrol || CurrentState == ZombieState.Search))
            {
                if (playerTarget != null) lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase);
            }

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            ApplyRendererColor(new Color(1f, 0.25f, 0.25f));
            yield return new WaitForSeconds(0.10f);
            ApplyRendererColor(baseTintColor);
            hitFlashRoutine = null;
        }

        private void Die()
        {
            if (meleeHitRoutine != null)
            {
                StopCoroutine(meleeHitRoutine);
                meleeHitRoutine = null;
            }
            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = null;
            }
            if (chargerTelegraph != null)
            {
                chargerTelegraph.HideTelegraph();
            }

            CurrentState = ZombieState.Dead;
            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
            }

            if (animator != null) animator.SetBool("Dead", true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieDeath, transform.position);
            }

            // Notify performance tracker (DDA) and GameManager (Score)
            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.RecordZombieKill();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordZombieKill(archetype);
            }

            // Disable colliders
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;

            Destroy(gameObject, 3.0f);
        }

        public void SetTerrainSpeedMultiplier(float multiplier)
        {
            if (chargerPhase == ChargerPhase.Charging) return;
            terrainMultiplier = Mathf.Clamp(multiplier, 0.3f, 1.0f);
        }
    }
}
