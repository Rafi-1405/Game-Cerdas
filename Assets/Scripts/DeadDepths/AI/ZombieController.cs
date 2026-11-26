using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Controls zombie behaviors, shared FSM, archetype-specific attacks, MainMenu isolation,
    /// crouch stealth detection, bone-attached visual accessories, and smooth corpse sinking.
    /// ASD-STE100: This AI controller guides Walker, Spitter, and Charger zombies.
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
            public float ChargerMaxChargeDuration = 1.3f;
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
        public bool HasPatrolTarget => hasPatrolTarget;
        public bool HasLineOfSightToPlayer { get; private set; }

        public string CurrentTargetLabel
        {
            get
            {
                if (IsDead || CurrentState == ZombieState.Dead) return "None";
                switch (CurrentState)
                {
                    case ZombieState.Chase:
                    case ZombieState.Attack:
                        return "Player";
                    case ZombieState.Search:
                        return "LKP";
                    case ZombieState.IdlePatrol:
                        return hasPatrolTarget ? "Patrol" : "None";
                    default:
                        return "None";
                }
            }
        }

        public struct StateChangeEntry
        {
            public float Time;
            public string ZombieName;
            public ZombieArchetype Archetype;
            public ZombieState FromState;
            public ZombieState ToState;
            public string Reason;
        }

        private const int MaxStateLogEntries = 10;
        private static readonly List<StateChangeEntry> stateChangeLog = new List<StateChangeEntry>(MaxStateLogEntries);
        public static IReadOnlyList<StateChangeEntry> StateChangeLog => stateChangeLog;

        private static readonly List<ZombieController> activeZombies = new List<ZombieController>(32);
        public static IReadOnlyList<ZombieController> ActiveZombies => activeZombies;

        private static readonly RaycastHit[] losHitBuffer = new RaycastHit[16];
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashDead = Animator.StringToHash("Dead");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            stateChangeLog.Clear();
            activeZombies.Clear();
        }

        public static void ClearStateChangeLog()
        {
            stateChangeLog.Clear();
        }

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

        private readonly List<GameObject> spawnedAccessories = new List<GameObject>();
        private readonly List<Material> spawnedAccessoryMaterials = new List<Material>();

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

        // Shared sub-phase timer for Spitter & Charger
        private enum SpitterPhase { None, Preparation, Recovery }
        private SpitterPhase spitterPhase = SpitterPhase.None;
        private bool spitterAnimTriggered = false;
        private Vector3 capturedSpitTarget;

        private enum ChargerPhase { None, TelegraphTracking, StepBack, Charging, Stunned }
        private ChargerPhase chargerPhase = ChargerPhase.None;
        private Vector3 lockedChargeDirection;
        private float subPhaseTimer = 0f;

        private float corpseTimer = 0f;
        private float nextPathUpdateTime = 0f;
        private Vector3 lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        private void OnEnable()
        {
            if (!activeZombies.Contains(this))
            {
                activeZombies.Add(this);
            }
        }

        private void OnDisable()
        {
            activeZombies.Remove(this);
        }

        private void Awake()
        {
            ApplyFallbackDefaults();
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
            FindPlayer();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (aiConfig == null) aiConfig = new ArchetypeConfig();
            if (aiConfig.VisionRange <= 0f) aiConfig.VisionRange = 16f;
            if (aiConfig.VisionAngle <= 0f) aiConfig.VisionAngle = 110f;
            if (aiConfig.ChargerMaxChargeDuration <= 0f) aiConfig.ChargerMaxChargeDuration = 1.3f;
        }

        private void OnDestroy()
        {
            CleanupAccessories();
        }

        private void CleanupAccessories()
        {
            for (int i = 0; i < spawnedAccessories.Count; i++)
            {
                if (spawnedAccessories[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedAccessories[i]);
                    else DestroyImmediate(spawnedAccessories[i]);
                }
            }
            spawnedAccessories.Clear();

            for (int i = 0; i < spawnedAccessoryMaterials.Count; i++)
            {
                if (spawnedAccessoryMaterials[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedAccessoryMaterials[i]);
                    else DestroyImmediate(spawnedAccessoryMaterials[i]);
                }
            }
            spawnedAccessoryMaterials.Clear();
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
        /// Explicitly initializes archetype stats, scale, bone accessories, and visual tint at spawn time.
        /// </summary>
        public void InitializeArchetype(ZombieArchetype arch)
        {
            archetype = arch;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            EnforceAnimatorNoRootMotion();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            spawnPosition = transform.position;
            lastKnownPlayerPos = transform.position;
            CurrentState = ZombieState.IdlePatrol;
            stateTimer = 0f;
            subPhaseTimer = 0f;
            spitterPhase = SpitterPhase.None;
            chargerPhase = ChargerPhase.None;
            hasPatrolTarget = false;
            corpseTimer = 0f;

            AttachArchetypeAccessories(arch);
            childRenderers = GetComponentsInChildren<Renderer>();

            ConfigureArchetypeSettings();
            CurrentHealth = aiConfig.MaxHealth;
            FindPlayer();

            if (arch == ZombieArchetype.Charger)
            {
                chargerTelegraph = GetComponentInChildren<ChargerTelegraph>();
                if (chargerTelegraph == null)
                {
                    chargerTelegraph = gameObject.AddComponent<ChargerTelegraph>();
                }
            }
        }

        private void AttachArchetypeAccessories(ZombieArchetype arch)
        {
            CleanupAccessories();
            if (arch == ZombieArchetype.Walker) return;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (arch == ZombieArchetype.Spitter)
            {
                Transform chest = animator != null && animator.isHuman
                    ? (animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest))
                    : null;
                if (chest == null) chest = transform;

                GameObject sac = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sac.name = "ArchetypeAccessory_VenomSac";
                RemoveCollidersImmediate(sac);
                sac.transform.SetParent(chest, false);
                if (chest == transform)
                    sac.transform.localPosition = new Vector3(0f, 1.32f, 0.20f);
                else
                    sac.transform.localPosition = new Vector3(0f, 0.05f, 0.16f);
                sac.transform.localScale = new Vector3(0.28f, 0.30f, 0.24f);

                Material sacMat = new Material(litShader) { hideFlags = HideFlags.HideAndDontSave };
                sacMat.color = new Color(0.25f, 0.95f, 0.35f);
                if (sacMat.HasProperty("_BaseColor")) sacMat.SetColor("_BaseColor", new Color(0.25f, 0.95f, 0.35f));
                if (sacMat.HasProperty("_EmissionColor"))
                {
                    sacMat.EnableKeyword("_EMISSION");
                    sacMat.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.25f) * 1.4f);
                }
                sac.GetComponent<Renderer>().sharedMaterial = sacMat;
                spawnedAccessoryMaterials.Add(sacMat);
                spawnedAccessories.Add(sac);
            }
            else if (arch == ZombieArchetype.Charger)
            {
                Material armorMat = new Material(litShader) { hideFlags = HideFlags.HideAndDontSave };
                armorMat.color = new Color(0.28f, 0.14f, 0.14f);
                if (armorMat.HasProperty("_BaseColor")) armorMat.SetColor("_BaseColor", new Color(0.28f, 0.14f, 0.14f));
                if (armorMat.HasProperty("_Metallic")) armorMat.SetFloat("_Metallic", 0.65f);
                spawnedAccessoryMaterials.Add(armorMat);

                Transform leftArm = animator != null && animator.isHuman
                    ? (animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) ?? animator.GetBoneTransform(HumanBodyBones.LeftShoulder))
                    : null;
                Transform rightArm = animator != null && animator.isHuman
                    ? (animator.GetBoneTransform(HumanBodyBones.RightUpperArm) ?? animator.GetBoneTransform(HumanBodyBones.RightShoulder))
                    : null;
                Transform head = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.Head)
                    : null;

                CreateArmorPauldron(leftArm ?? transform, leftArm == null ? new Vector3(-0.34f, 1.45f, 0f) : Vector3.zero, armorMat);
                CreateArmorPauldron(rightArm ?? transform, rightArm == null ? new Vector3(0.34f, 1.45f, 0f) : Vector3.zero, armorMat);

                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "ArchetypeAccessory_ChargerEyes";
                RemoveCollidersImmediate(visor);
                visor.transform.SetParent(head ?? transform, false);
                visor.transform.localPosition = head == null ? new Vector3(0f, 1.62f, 0.16f) : new Vector3(0f, 0.06f, 0.12f);
                visor.transform.localScale = new Vector3(0.16f, 0.04f, 0.06f);
                Material eyeMat = new Material(litShader) { hideFlags = HideFlags.HideAndDontSave };
                eyeMat.color = new Color(1f, 0.15f, 0.10f);
                if (eyeMat.HasProperty("_BaseColor")) eyeMat.SetColor("_BaseColor", new Color(1f, 0.15f, 0.10f));
                if (eyeMat.HasProperty("_EmissionColor"))
                {
                    eyeMat.EnableKeyword("_EMISSION");
                    eyeMat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.1f) * 2.2f);
                }
                visor.GetComponent<Renderer>().sharedMaterial = eyeMat;
                spawnedAccessoryMaterials.Add(eyeMat);
                spawnedAccessories.Add(visor);
            }
        }

        private void CreateArmorPauldron(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "ArchetypeAccessory_Pauldron";
            RemoveCollidersImmediate(p);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = new Vector3(0.24f, 0.22f, 0.26f);
            p.GetComponent<Renderer>().sharedMaterial = mat;
            spawnedAccessories.Add(p);
        }

        private static void RemoveCollidersImmediate(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
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
                if (r.gameObject.name.StartsWith("ArchetypeAccessory_", StringComparison.Ordinal)) continue;
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
            if (IsDead)
            {
                corpseTimer += Time.deltaTime;
                if (corpseTimer >= 2.0f)
                {
                    transform.position += Vector3.down * (Time.deltaTime * 0.65f);
                }
                return;
            }

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

            if (agent != null && agent.enabled && chargerPhase != ChargerPhase.Charging)
            {
                float targetSpeed = aiConfig.BaseMoveSpeed;
                if (CurrentState == ZombieState.IdlePatrol)
                {
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
                    animator.SetFloat(HashSpeed, actualWorldSpeed, 0.1f, Time.deltaTime);
                }
                else
                {
                    animator.SetFloat(HashSpeed, actualWorldSpeed);
                }
            }
        }

        private void LateUpdate()
        {
            EnforceAnimatorNoRootMotion();
        }

        private bool CanSeePlayer()
        {
            // Never aggro during Main Menu diorama
            if (GameManager.Instance != null && GameManager.Instance.IsInMainMenu) return false;
            if (playerTarget == null || (playerController != null && playerController.IsDead)) return false;

            float effectiveVisionRange = (playerController != null && playerController.IsCrouching)
                ? aiConfig.VisionRange * 0.65f
                : aiConfig.VisionRange;

            Vector3 dir = playerTarget.position - transform.position;
            if (dir.sqrMagnitude > effectiveVisionRange * effectiveVisionRange) return false;

            Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, flatDir.normalized);
                if (angle > aiConfig.VisionAngle * 0.5f) return false;
            }

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

            int hitCount = Physics.RaycastNonAlloc(fromPos, delta / dist, losHitBuffer, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hitCount == 0) return true;

            float closestBlockingDist = float.MaxValue;
            float closestPlayerDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = losHitBuffer[i].collider;
                if (col == null) continue;
                Transform colTr = col.transform;
                if (colTr == transform || colTr.IsChildOf(transform)) continue;
                if (col.GetComponentInParent<ZombieController>() != null) continue;

                float hitDist = losHitBuffer[i].distance;
                if (colTr == playerTarget || colTr.IsChildOf(playerTarget))
                {
                    if (hitDist < closestPlayerDist) closestPlayerDist = hitDist;
                }
                else
                {
                    if (hitDist < closestBlockingDist) closestBlockingDist = hitDist;
                }
            }

            return closestBlockingDist >= closestPlayerDist;
        }

        private void UpdateIdlePatrol()
        {
            if (HasLineOfSightToPlayer)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase, "Player spotted (LOS)");
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
            if ((GameManager.Instance != null && GameManager.Instance.IsInMainMenu) ||
                playerTarget == null || (playerController != null && playerController.IsDead))
            {
                TransitionToState(ZombieState.IdlePatrol, "Target dead/lost");
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (Time.time >= nextPathUpdateTime || (playerTarget.position - lastNavDestination).sqrMagnitude > 0.16f)
                {
                    agent.SetDestination(playerTarget.position);
                    lastNavDestination = playerTarget.position;
                    nextPathUpdateTime = Time.time + 0.15f;
                }
            }

            bool hasDirectLOS = HasUnobstructedLineOfSightToPlayer(transform.position + Vector3.up * 1.1f, playerTarget.position + Vector3.up * 1.0f);
            bool canEngage = archetype == ZombieArchetype.Walker
                ? (distToPlayer <= aiConfig.AttackRange && hasDirectLOS)
                : (distToPlayer <= aiConfig.AttackRange && HasLineOfSightToPlayer);

            if (canEngage)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Attack, "In attack range");
                return;
            }

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
                    TransitionToState(ZombieState.Search, "Lost LOS > 4s");
                }
            }
        }

        private void UpdateSearch()
        {
            if (HasLineOfSightToPlayer)
            {
                lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase, "Reacquired LOS");
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (Time.time >= nextPathUpdateTime || (lastKnownPlayerPos - lastNavDestination).sqrMagnitude > 0.16f)
                {
                    agent.SetDestination(lastKnownPlayerPos);
                    lastNavDestination = lastKnownPlayerPos;
                    nextPathUpdateTime = Time.time + 0.25f;
                }
            }

            stateTimer += Time.deltaTime;
            bool reachedLastKnown = agent != null && agent.enabled && agent.isOnNavMesh &&
                                    !agent.pathPending && agent.remainingDistance <= 1.2f;

            if (stateTimer > 5.0f || (reachedLastKnown && stateTimer > 2.0f))
            {
                hasPatrolTarget = false;
                TransitionToState(ZombieState.IdlePatrol, "Search timeout");
            }
        }

        private void UpdateAttack()
        {
            if ((GameManager.Instance != null && GameManager.Instance.IsInMainMenu) ||
                playerTarget == null || (playerController != null && playerController.IsDead))
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
                TransitionToState(ZombieState.IdlePatrol, "Target dead/null");
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
                TransitionToState(ZombieState.Chase, "Target out of melee range");
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            Vector3 lookDir = (playerTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 10f);
            }

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + aiConfig.MeleeCooldown;

                if (animator != null) animator.SetTrigger(HashAttack);
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

        #region Spitter Attack Sequence
        private void UpdateSpitterAttack()
        {
            float dt = Time.deltaTime;
            subPhaseTimer += dt;

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
                    spitterPhase = SpitterPhase.Preparation;
                    spitterAnimTriggered = false;
                    subPhaseTimer = 0f;
                    if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                    break;

                case SpitterPhase.Preparation:
                    if (!spitterAnimTriggered && subPhaseTimer >= Mathf.Max(0f, aiConfig.SpitterPreparationDuration - 0.30f))
                    {
                        spitterAnimTriggered = true;
                        if (animator != null) animator.SetTrigger(HashAttack);
                    }

                    if (subPhaseTimer >= aiConfig.SpitterPreparationDuration)
                    {
                        if (!spitterAnimTriggered && animator != null)
                        {
                            spitterAnimTriggered = true;
                            animator.SetTrigger(HashAttack);
                        }
                        capturedSpitTarget = playerTarget.position + Vector3.up * 1.0f;
                        LaunchToxicProjectile(capturedSpitTarget);

                        spitterPhase = SpitterPhase.Recovery;
                        subPhaseTimer = 0f;
                    }
                    break;

                case SpitterPhase.Recovery:
                    if (subPhaseTimer >= aiConfig.SpitterRecoveryDuration)
                    {
                        spitterPhase = SpitterPhase.None;
                        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
                        TransitionToState(ZombieState.Chase, "Spit recovery complete");
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

            GameObject sphereVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereVisual.transform.SetParent(projObj.transform, false);
            sphereVisual.transform.localScale = Vector3.one * 0.45f;
            var rend = sphereVisual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            mat.color = new Color(0.2f, 1.0f, 0.3f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.2f, 1.0f, 0.3f) * 1.5f);
            }
            rend.sharedMaterial = mat;
            proj.RegisterRuntimeMaterial(mat);
            Destroy(sphereVisual.GetComponent<Collider>());

            proj.Launch(targetPos, transform);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieAttack, transform.position);
            }
        }
        #endregion

        #region Charger Attack Sequence
        private void UpdateChargerAttack()
        {
            float dt = Time.deltaTime;
            subPhaseTimer += dt;

            switch (chargerPhase)
            {
                case ChargerPhase.None:
                    chargerPhase = ChargerPhase.TelegraphTracking;
                    subPhaseTimer = 0f;
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

                    if (subPhaseTimer >= aiConfig.ChargerTelegraphDuration)
                    {
                        lockedChargeDirection = trackDir;
                        if (chargerTelegraph != null) chargerTelegraph.HideTelegraph();

                        chargerPhase = ChargerPhase.StepBack;
                        subPhaseTimer = 0f;
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

                    if (subPhaseTimer >= aiConfig.ChargerStepBackDuration)
                    {
                        chargerPhase = ChargerPhase.Charging;
                        subPhaseTimer = 0f;
                        if (agent != null) agent.enabled = false;
                        if (animator != null) animator.SetTrigger(HashAttack);
                        CombatVfxManager.Instance?.PlayDodgeDust(transform.position);
                    }
                    break;

                case ChargerPhase.Charging:
                    float chargeStep = aiConfig.ChargerSpeed * dt;
                    Vector3 moveVec = lockedChargeDirection * chargeStep;

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

                            if (hit.collider.GetComponentInParent<ZombieController>() != null)
                            {
                                continue;
                            }

                            StartChargerStun();
                            return;
                        }
                    }

                    transform.position += moveVec;

                    if (subPhaseTimer >= aiConfig.ChargerMaxChargeDuration)
                    {
                        StartChargerStun();
                    }
                    break;

                case ChargerPhase.Stunned:
                    if (subPhaseTimer >= aiConfig.ChargerStunDuration)
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
                        TransitionToState(ZombieState.Chase, "Stun recovered");
                    }
                    break;
            }
        }

        private void StartChargerStun()
        {
            chargerPhase = ChargerPhase.Stunned;
            subPhaseTimer = 0f;

            CombatVfxManager.Instance?.PlayWallShockwave(transform.position);

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

        public void TransitionToState(ZombieState newState, string reason = "Condition")
        {
            if (CurrentState == newState) return;
            ZombieState prevState = CurrentState;
            if (prevState == ZombieState.Attack && chargerTelegraph != null)
            {
                chargerTelegraph.HideTelegraph();
            }
            CurrentState = newState;
            stateTimer = 0f;
            nextPathUpdateTime = 0f;
            RecordStateChange(prevState, newState, reason);
        }

        private void RecordStateChange(ZombieState fromState, ZombieState toState, string reason)
        {
            if (stateChangeLog.Count >= MaxStateLogEntries)
            {
                stateChangeLog.RemoveAt(0);
            }

            stateChangeLog.Add(new StateChangeEntry
            {
                Time = Time.time,
                ZombieName = gameObject.name,
                Archetype = archetype,
                FromState = fromState,
                ToState = toState,
                Reason = string.IsNullOrEmpty(reason) ? "-" : reason
            });
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

            if (gameObject.activeInHierarchy)
            {
                if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = StartCoroutine(HitFlashRoutine());
            }

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

            if (CurrentHealth > 0f && (CurrentState == ZombieState.IdlePatrol || CurrentState == ZombieState.Search))
            {
                if (playerTarget == null) FindPlayer();
                if (playerTarget != null) lastKnownPlayerPos = playerTarget.position;
                TransitionToState(ZombieState.Chase, "Damaged by player");
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

            if (CurrentState != ZombieState.Dead)
            {
                RecordStateChange(CurrentState, ZombieState.Dead, "Health <= 0");
            }
            CurrentState = ZombieState.Dead;
            corpseTimer = 0f;

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
            }

            if (animator != null) animator.SetBool(HashDead, true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.ZombieDeath, transform.position);
            }

            if (PlayerPerformanceTracker.Instance != null)
            {
                PlayerPerformanceTracker.Instance.RecordZombieKill();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordZombieKill(archetype);
            }

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
