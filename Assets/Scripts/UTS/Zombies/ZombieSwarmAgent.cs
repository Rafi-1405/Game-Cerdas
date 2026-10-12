using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Praktikum5.FSM;

namespace UTS.Zombies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieSwarmAgent : MonoBehaviour, IVisionSource
    {
        private enum BehaviorState { Patrol, Chase, Investigate, Pause, Search, Attack, Dead }
        private static readonly HashSet<ZombieSwarmAgent> activeAgents = new HashSet<ZombieSwarmAgent>();
        private static int nextPatrolAreaGroupId = 1;
        private static bool warnedMissingObstacleLayers;

        [Header("Senses")]
        [SerializeField] private float visionRange = 24f;
        [SerializeField, Range(0f, 360f)] private float visionAngle = 144f;
        [SerializeField] private float eyeHeight = 1.3f;
        [SerializeField] private float targetEyeHeight = 0.8f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float closeSenseRadius = 5f;
        [SerializeField] private float chaseMemoryTime = 1f;
        [SerializeField] private float hearChaseRadius = 6f;
        [SerializeField] private float alertDuration = 4f;
        [SerializeField] private float neighborAlertRadius = 9f;
        [SerializeField] private float alertBroadcastInterval = 1.5f;
        [SerializeField] private float hearingUpdateInterval = 1.5f;

        [Header("Movement")]
        [SerializeField] private float wanderSpeed = 1.2f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float destinationRefreshInterval = 0.2f;
        [SerializeField] private float destinationMoveThreshold = 0.5f;
        [SerializeField] private float patrolTargetChangeInterval = 4f;

        [Header("Horde Steering")]
        [SerializeField] private float separationRadius = 3.4f;
        [SerializeField] private float separationWeight = 2.2f;

        [Header("Contact")]
        [SerializeField] private float contactRadius = 1.1f;
        [SerializeField] private float contactDamagePerSecond = 35f;

        [Header("Patrol Area")]
        [SerializeField, Min(0f)] private float spawnPatrolRadius = 5f;
        [SerializeField, Min(0f)] private float radiusPerSharedZombie = 0.75f;
        [SerializeField, Min(0.1f)] private float patrolArrivalDistance = 1.2f;
        [SerializeField, Min(1)] private int patrolPointAttempts = 16;
        [SerializeField, Min(0f)] private float searchPauseTime = 2f;
        [SerializeField, Min(0f)] private float searchDuration = 8f;
        [Header("Debug")]
        [SerializeField] private bool debugLogging;

        private NavMeshAgent agent;
        private EnemyFSM enemyFsm;
        private SimplePlayerController playerController;
        private global::NPCSensor hearingSensor;
        private Transform player;
        private ZombieHordeManager hordeManager;
        private BehaviorState behaviorState;
        private Vector3 patrolAreaCenter, patrolTarget, lastKnownPlayerPosition, lastSeenVelocity, searchDirection, pausePosition, heardPositionSnapshot, lastRequestedDestination;
        private Vector3 previousPlayerPosition, currentPlayerVelocity;
        private float alertTimer, chaseMemoryTimer, huntTimer, nextAlertBroadcastTime, nextHearingUpdateTime;
        private float pauseTimer, searchTimer, nextWanderChangeTime, nextDestinationUpdateTime, nextDebugLogTime;
        private int ownPatrolAreaGroupId, patrolAreaGroupId;
        private bool hasRequestedDestination, hasReachedInvestigationPoint, wasSeeingPlayer, wasHearingPlayer, deathHandled;

        public static IEnumerable<ZombieSwarmAgent> ActiveAgents => activeAgents;
        public bool IsAlerted => behaviorState != BehaviorState.Patrol && behaviorState != BehaviorState.Dead;
        public static int CreateSharedAreaGroupId() => nextPatrolAreaGroupId++;
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public LayerMask ObstacleMask => obstacleMask;
        public bool IsSeeingPlayer { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyFsm = GetComponent<EnemyFSM>();
            hearingSensor = GetComponent<global::NPCSensor>();
            EnemyPerception perception = GetComponent<EnemyPerception>();
            RobberyBobVisionCone visionCone = GetComponent<RobberyBobVisionCone>();
            if (enemyFsm != null) enemyFsm.enabled = false;
            if (perception != null) perception.enabled = false;
            if (visionCone == null) visionCone = gameObject.AddComponent<RobberyBobVisionCone>();
            visionCone.enabled = true;
            patrolAreaCenter = patrolTarget = transform.position;
            ownPatrolAreaGroupId = CreateSharedAreaGroupId();
            patrolAreaGroupId = ownPatrolAreaGroupId;
            agent.updateRotation = false;
            agent.autoBraking = false;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = contactRadius * 0.8f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(50, 100);
            ConfigureObstacleMask();
            FindPlayerIfNeeded();
        }

        private void OnEnable() => activeAgents.Add(this);
        private void OnDisable() => activeAgents.Remove(this);
        private void Start()
        {
            FindPlayerIfNeeded();
            if (hordeManager == null) hordeManager = FindAnyObjectByType<ZombieHordeManager>();
            if (debugLogging)
                Debug.Log($"[ZombieSwarmAgent] {name}: visionRange={visionRange}, visionAngle={visionAngle}, closeSenseRadius={closeSenseRadius}, chaseMemoryTime={chaseMemoryTime}", this);
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            FindPlayerIfNeeded();
            if (player == null || (hordeManager != null && hordeManager.IsGameOver) || (playerController != null && playerController.IsDead)) { StopAgent(); return; }
            if (enemyFsm != null && enemyFsm.IsDead)
            {
                if (!deathHandled) { deathHandled = true; SetBehaviorState(BehaviorState.Dead, "Dead"); }
                StopAgent(); return;
            }

            currentPlayerVelocity = Flatten((player.position - previousPlayerPosition) / Mathf.Max(Time.deltaTime, 0.001f));
            previousPlayerPosition = player.position;
            bool canSeePlayer = CanSeePlayer();
            IsSeeingPlayer = canSeePlayer;
            bool heardNearby = UpdateHearingSnapshot();
            UpdateAwareness(canSeePlayer, heardNearby);
            if (behaviorState == BehaviorState.Pause)
            {
                agent.isStopped = true;
                return;
            }
            if ((canSeePlayer || huntTimer > 0f) && PlanarDistance(transform.position, player.position) <= contactRadius)
            {
                SetBehaviorState(BehaviorState.Attack, "Contact");
                agent.isStopped = true;
                if (playerController != null) playerController.TakeDamage(contactDamagePerSecond * Time.deltaTime);
                return;
            }

            agent.isStopped = false;
            float speed = behaviorState == BehaviorState.Patrol ? wanderSpeed : chaseSpeed;
            agent.speed = speed;
            Vector3 target = GetCurrentTarget(canSeePlayer);
            RequestDestination(target);
            Vector3 nudge = CalculateSeparationNudge(speed);
            if (nudge.sqrMagnitude > 0.0001f) agent.Move(nudge * Time.deltaTime);
            Vector3 facingVelocity = Flatten(agent.desiredVelocity);
            if (facingVelocity.sqrMagnitude < 0.01f) facingVelocity = Flatten(agent.velocity);
            if (facingVelocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facingVelocity.normalized, Vector3.up), 10f * Time.deltaTime);
        }

        public void Initialize(Transform playerTarget, ZombieHordeManager manager) { hordeManager = manager; BindPlayer(playerTarget); }
        public void ReceiveHuntOrder(float duration) { huntTimer = Mathf.Max(huntTimer, duration); alertTimer = Mathf.Max(alertTimer, duration); SetBehaviorState(BehaviorState.Chase, "Hunt"); }
        public void ReceiveAlert(Vector3 targetPosition, float duration) => ReceiveAlert(targetPosition, duration, ownPatrolAreaGroupId);
        public void ReceiveAlert(Vector3 targetPosition, float duration, int groupId)
        {
            if (behaviorState == BehaviorState.Attack || chaseMemoryTimer > 0f || behaviorState == BehaviorState.Chase) return;
            SetSharedPatrolArea(targetPosition, groupId, duration, "Alert");
        }

        private void UpdateAwareness(bool canSeePlayer, bool heardNearby)
        {
            if (huntTimer > 0f) huntTimer = Mathf.Max(0f, huntTimer - Time.deltaTime);
            if (canSeePlayer)
            {
                lastKnownPlayerPosition = player.position;
                lastSeenVelocity = currentPlayerVelocity;
                chaseMemoryTimer = chaseMemoryTime;
                alertTimer = alertDuration;
                hasReachedInvestigationPoint = false;
                SetBehaviorState(BehaviorState.Chase, "Saw");
                if (!wasSeeingPlayer || Time.time >= nextAlertBroadcastTime)
                    BroadcastAlertIfDue(lastKnownPlayerPosition, patrolAreaGroupId, alertDuration);
                wasSeeingPlayer = true;
                return;
            }
            if (wasSeeingPlayer)
            {
                searchDirection = lastSeenVelocity.sqrMagnitude > 0.01f
                    ? lastSeenVelocity.normalized
                    : Flatten(lastKnownPlayerPosition - transform.position).normalized;
                wasSeeingPlayer = false;
            }
            if (huntTimer > 0f) { SetBehaviorState(BehaviorState.Chase, "Hunt"); return; }
            if (behaviorState == BehaviorState.Pause)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f) BeginSearch();
                return;
            }
            if (behaviorState == BehaviorState.Search)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f) BeginDriftingPatrol();
                return;
            }
            if (chaseMemoryTimer > 0f)
            {
                chaseMemoryTimer = Mathf.Max(0f, chaseMemoryTimer - Time.deltaTime);
                if (PlanarDistance(transform.position, lastKnownPlayerPosition) <= patrolArrivalDistance || chaseMemoryTimer <= 0f)
                {
                    BeginInvestigation("Memory expired");
                    return;
                }
                SetBehaviorState(BehaviorState.Chase, "Memory"); return;
            }
            if (heardNearby)
            {
                SetSharedPatrolArea(heardPositionSnapshot, ownPatrolAreaGroupId, alertDuration, "Heard");
                return;
            }
            if (behaviorState == BehaviorState.Chase) { BeginInvestigation("Lost sight"); return; }
            if (behaviorState == BehaviorState.Investigate)
            {
                if (PlanarDistance(transform.position, lastKnownPlayerPosition) <= patrolArrivalDistance) BeginPause();
                return;
            }
            if (alertTimer > 0f)
            {
                alertTimer = Mathf.Max(0f, alertTimer - Time.deltaTime);
                if (alertTimer > 0f) { SetBehaviorState(BehaviorState.Investigate, "Alert"); return; }
            }
            if (behaviorState != BehaviorState.Patrol) BeginDriftingPatrol();
        }

        private bool UpdateHearingSnapshot()
        {
            if (hearingSensor == null || !hearingSensor.CanHearPlayer)
            {
                wasHearingPlayer = false;
                return false;
            }

            bool refreshSnapshot = hearingSensor.HeardPlayerThisFrame || !wasHearingPlayer || Time.time >= nextHearingUpdateTime;
            wasHearingPlayer = true;
            if (!refreshSnapshot) return false;

            heardPositionSnapshot = hearingSensor.LastHeardPosition;
            nextHearingUpdateTime = Time.time + hearingUpdateInterval;
            return PlanarDistance(transform.position, heardPositionSnapshot) <= hearChaseRadius;
        }

        private void SetSharedPatrolArea(Vector3 center, int groupId, float duration, string reason)
        {
            bool areaChanged = PlanarDistance(patrolAreaCenter, center) > 2f || patrolAreaGroupId != groupId;
            lastKnownPlayerPosition = patrolAreaCenter = center;
            searchDirection = Flatten(center - transform.position).normalized;
            patrolAreaGroupId = groupId;
            alertTimer = Mathf.Max(alertTimer, duration);
            hasReachedInvestigationPoint = false;
            if (areaChanged) nextWanderChangeTime = 0f;
            SetBehaviorState(BehaviorState.Investigate, reason);
        }

        private void BeginInvestigation(string reason)
        {
            chaseMemoryTimer = 0f;
            if (searchDirection.sqrMagnitude <= 0.01f)
                searchDirection = Flatten(lastKnownPlayerPosition - transform.position).normalized;
            hasReachedInvestigationPoint = false;
            SetBehaviorState(BehaviorState.Investigate, reason);
        }

        private void BeginPause()
        {
            pausePosition = transform.position;
            pauseTimer = searchPauseTime;
            agent.isStopped = true;
            if (searchDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(searchDirection, Vector3.up);
            SetBehaviorState(BehaviorState.Pause, "Pause");
        }

        private void BeginSearch()
        {
            searchTimer = searchDuration;
            nextWanderChangeTime = 0f;
            SetBehaviorState(BehaviorState.Search, "Search");
        }

        private void BeginDriftingPatrol()
        {
            patrolAreaCenter = transform.position;
            patrolAreaGroupId = ownPatrolAreaGroupId;
            nextWanderChangeTime = 0f;
            SetBehaviorState(BehaviorState.Patrol, "Patrol");
        }

        private Vector3 GetCurrentTarget(bool canSeePlayer)
        {
            if (behaviorState == BehaviorState.Chase) return canSeePlayer || huntTimer > 0f ? player.position : lastKnownPlayerPosition;
            if (behaviorState == BehaviorState.Investigate) return GetInvestigateTarget();
            if (behaviorState == BehaviorState.Search) return GetSearchTarget();
            return GetPatrolTarget();
        }

        private Vector3 GetInvestigateTarget()
        {
            if (!hasReachedInvestigationPoint)
            {
                if (PlanarDistance(transform.position, lastKnownPlayerPosition) > patrolArrivalDistance) return lastKnownPlayerPosition;
                hasReachedInvestigationPoint = true;
                BeginPause();
                return transform.position;
            }
            return lastKnownPlayerPosition;
        }

        private Vector3 GetSearchTarget()
        {
            if (Time.time >= nextWanderChangeTime || PlanarDistance(transform.position, patrolTarget) <= patrolArrivalDistance)
                ChooseDirectionalSearchTarget();
            return patrolTarget;
        }

        private Vector3 GetPatrolTarget()
        {
            if (Time.time >= nextWanderChangeTime || PlanarDistance(transform.position, patrolTarget) <= patrolArrivalDistance)
            {
                patrolAreaCenter = transform.position;
                ChooseWanderTarget(patrolAreaCenter, GetPatrolRadius());
            }
            return patrolTarget;
        }

        private void ChooseDirectionalSearchTarget()
        {
            Vector3 forward = searchDirection.sqrMagnitude > 0.01f ? searchDirection : transform.forward;
            float sampleDistance = Mathf.Max(2f, agent.radius * 2f);
            for (int attempt = 0; attempt < patrolPointAttempts; attempt++)
            {
                Vector3 direction = Quaternion.Euler(0f, Random.Range(-50f, 50f), 0f) * forward;
                Vector3 candidate = pausePosition + direction * Random.Range(4f, 10f);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleDistance, agent.areaMask))
                { patrolTarget = hit.position; nextWanderChangeTime = Time.time + patrolTargetChangeInterval; return; }
            }
            patrolTarget = pausePosition;
            nextWanderChangeTime = Time.time + patrolTargetChangeInterval;
        }

        private void ChooseWanderTarget(Vector3 center, float radius)
        {
            float sampleDistance = Mathf.Max(2f, agent.radius * 2f);
            for (int attempt = 0; attempt < patrolPointAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleDistance, agent.areaMask) && PlanarDistance(hit.position, center) <= radius + sampleDistance)
                { patrolTarget = hit.position; nextWanderChangeTime = Time.time + patrolTargetChangeInterval; return; }
            }
            patrolTarget = center;
            nextWanderChangeTime = Time.time + patrolTargetChangeInterval;
        }

        private float GetPatrolRadius()
        {
            int count = 0;
            foreach (ZombieSwarmAgent other in activeAgents) if (other != null && other.patrolAreaGroupId == patrolAreaGroupId && other.behaviorState != BehaviorState.Dead) count++;
            return spawnPatrolRadius + radiusPerSharedZombie * Mathf.Sqrt(Mathf.Max(0, count - 1));
        }

        private void RequestDestination(Vector3 target)
        {
            if (!hasRequestedDestination || Time.time >= nextDestinationUpdateTime || PlanarDistance(lastRequestedDestination, target) > destinationMoveThreshold)
            { agent.SetDestination(target); lastRequestedDestination = target; hasRequestedDestination = true; nextDestinationUpdateTime = Time.time + destinationRefreshInterval; }
        }

        private Vector3 CalculateSeparationNudge(float currentSpeed)
        {
            Vector3 separation = Vector3.zero;
            foreach (ZombieSwarmAgent other in activeAgents)
            {
                if (other == null || other == this) continue;
                Vector3 offset = Flatten(transform.position - other.transform.position);
                float distance = offset.magnitude;
                if (distance > 0.001f && distance < separationRadius) separation += offset / distance * ((separationRadius - distance) / separationRadius);
            }
            return Vector3.ClampMagnitude(separation * separationWeight, currentSpeed * 0.5f);
        }

        private bool CanSeePlayer()
        {
            if (player == null) return false;
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = player.position + Vector3.up * targetEyeHeight - origin;
            Vector3 planarDirection = Flatten(toPlayer);
            float distance = planarDirection.magnitude;
            if (distance > visionRange || distance < 0.001f) return false;
            if (distance > closeSenseRadius && Vector3.Angle(Flatten(transform.forward), planarDirection) > visionAngle * 0.5f) return false;
            return !Physics.Raycast(origin, toPlayer.normalized, Mathf.Max(0f, toPlayer.magnitude - 0.05f), obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void ConfigureObstacleMask()
        {
            int obstacleLayers = LayerMask.GetMask("Obstacle", "Wall");
            if (obstacleMask.value == 0 || obstacleMask.value == -1) obstacleMask = obstacleLayers;
            if (obstacleMask.value == 0)
            {
                obstacleMask = LayerMask.GetMask("Default");
                if (!warnedMissingObstacleLayers) { warnedMissingObstacleLayers = true; Debug.LogWarning("[ZombieSwarmAgent] Obstacle and Wall layers are missing; vision uses Default only."); }
            }
            obstacleMask.value &= ~(1 << gameObject.layer);
            if (player != null) obstacleMask.value &= ~(1 << player.gameObject.layer);
            obstacleMask.value &= ~LayerMask.GetMask("Player", "Zombie", "Zombies");
        }

        private void FindPlayerIfNeeded()
        {
            if (player == null) { GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); if (playerObject != null) BindPlayer(playerObject.transform); }
        }
        private void BindPlayer(Transform target) { player = target; if (player == null) return; playerController = player.GetComponent<SimplePlayerController>(); previousPlayerPosition = player.position; ConfigureObstacleMask(); }
        private void BroadcastAlertIfDue(Vector3 target, int groupId, float duration)
        {
            if (Time.time < nextAlertBroadcastTime) return;
            foreach (ZombieSwarmAgent other in activeAgents) if (other != null && other != this && PlanarDistance(transform.position, other.transform.position) <= neighborAlertRadius) other.ReceiveAlert(target, duration, groupId);
            nextAlertBroadcastTime = Time.time + alertBroadcastInterval;
        }

        private void SetBehaviorState(BehaviorState state, string reason)
        {
            if (behaviorState == state) return;
            behaviorState = state;
            EnemyState displayState = state == BehaviorState.Patrol ? EnemyState.Patrol :
                                      state == BehaviorState.Chase ? EnemyState.Chase :
                                      state == BehaviorState.Attack ? EnemyState.Attack :
                                      state == BehaviorState.Dead ? EnemyState.Dead : EnemyState.Investigate;
            SetDisplayedState(displayState);
            if (debugLogging && Time.time >= nextDebugLogTime) { Debug.Log($"[ZombieSwarmAgent] {name}: {state} ({reason})", this); nextDebugLogTime = Time.time + 0.15f; }
        }
        private void SetDisplayedState(EnemyState state) { if (enemyFsm != null) enemyFsm.SetDisplayStateOnly(state); }
        private void StopAgent() { if (agent != null && agent.isOnNavMesh) agent.isStopped = true; }
        private static Vector3 Flatten(Vector3 value) { value.y = 0f; return value; }
        private static float PlanarDistance(Vector3 first, Vector3 second) => Flatten(first - second).magnitude;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAlerted ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, left * visionRange);
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, right * visionRange);
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, closeSenseRadius);
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan; Gizmos.DrawSphere(lastKnownPlayerPosition, 0.2f);
                Vector3 target = behaviorState == BehaviorState.Chase
                    ? (huntTimer > 0f && player != null ? player.position : lastKnownPlayerPosition)
                    : behaviorState == BehaviorState.Investigate
                        ? (hasReachedInvestigationPoint ? patrolTarget : lastKnownPlayerPosition)
                        : patrolTarget;
                Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, target); Gizmos.DrawSphere(target, 0.16f);
            }
        }
    }
}
