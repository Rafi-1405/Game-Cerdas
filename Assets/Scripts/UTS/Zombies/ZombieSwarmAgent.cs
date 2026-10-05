using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Praktikum5.FSM;

namespace UTS.Zombies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieSwarmAgent : MonoBehaviour
    {
        private static readonly HashSet<ZombieSwarmAgent> activeAgents = new HashSet<ZombieSwarmAgent>();

        [Header("Senses")]
        [SerializeField] private float visionRange = 24f;
        [SerializeField, Range(0f, 360f)] private float visionAngle = 144f;
        [SerializeField] private float eyeHeight = 1.3f;
        [SerializeField] private float targetEyeHeight = 0.8f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private float alertDuration = 4f;
        [SerializeField] private float neighborAlertRadius = 9f;
        [SerializeField] private float alertBroadcastInterval = 0.5f;

        [Header("Movement")]
        [SerializeField] private float wanderSpeed = 1.2f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float predictionTime = 0.5f;
        [SerializeField] private float maxFlankOffset = 4f;
        [SerializeField] private float wanderChangeInterval = 1.2f;

        [Header("Horde Steering")]
        [SerializeField] private float neighborRadius = 9f;
        [SerializeField] private float separationRadius = 3.4f;
        [SerializeField] private float separationWeight = 2.2f;
        [SerializeField] private float alignmentWeight = 0.6f;
        [SerializeField] private float cohesionWeight = 0.25f;
        [SerializeField] private float wallCheckDistance = 2.5f;
        [SerializeField] private float wallCheckRadius = 0.35f;
        [SerializeField] private float wallAvoidanceWeight = 3f;

        [Header("Contact")]
        [SerializeField] private float contactRadius = 1.1f;
        [SerializeField] private float contactDamagePerSecond = 35f;

        private NavMeshAgent agent;
        private EnemyFSM enemyFsm;
        private EnemyHealth enemyHealth;
        private PlayerHealth playerHealth;
        private SimplePlayerController playerController;
        private global::NPCSensor hearingSensor;
        private Transform player;
        private ZombieHordeManager hordeManager;
        private Vector3 lastKnownPlayerPosition;
        private Vector3 estimatedPlayerVelocity;
        private Vector3 previousPlayerPosition;
        private Vector3 wanderDirection;
        private float alertTimer;
        private float nextAlertBroadcastTime;
        private float nextWanderChangeTime;
        private float flankSide;
        private bool hasPlayerPositionSample;
        private bool deathHandled;

        public static IEnumerable<ZombieSwarmAgent> ActiveAgents => activeAgents;
        public bool IsAlerted => alertTimer > 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyFsm = GetComponent<EnemyFSM>();
            enemyHealth = GetComponent<EnemyHealth>();
            hearingSensor = GetComponent<global::NPCSensor>();

            if (enemyFsm != null)
            {
                enemyFsm.enabled = false;
            }

            agent.updateRotation = false;
            agent.autoBraking = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            if (obstacleMask.value == 0)
            {
                obstacleMask = LayerMask.GetMask("Obstacle", "Wall");
                if (obstacleMask.value == 0)
                {
                    obstacleMask = ~0;
                }
            }

            flankSide = Random.value < 0.5f ? -1f : 1f;
            wanderDirection = transform.forward;
            nextWanderChangeTime = Time.time + Random.Range(0.2f, wanderChangeInterval);
            FindPlayerIfNeeded();
        }

        private void OnEnable()
        {
            activeAgents.Add(this);
        }

        private void OnDisable()
        {
            activeAgents.Remove(this);
        }

        private void Start()
        {
            FindPlayerIfNeeded();
            if (hordeManager == null)
            {
                hordeManager = FindAnyObjectByType<ZombieHordeManager>();
            }
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            FindPlayerIfNeeded();
            if (player == null)
            {
                StopAgent();
                return;
            }

            if (hordeManager != null && hordeManager.IsGameOver)
            {
                StopAgent();
                return;
            }

            if (enemyHealth != null && enemyHealth.IsDead)
            {
                if (!deathHandled)
                {
                    deathHandled = true;
                    SetDisplayedState(EnemyState.Dead);
                }
                StopAgent();
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                StopAgent();
                return;
            }

            UpdatePlayerVelocity();
            bool canSeePlayer = CanSeePlayer();
            bool canHearPlayer = hearingSensor != null && hearingSensor.CanHearPlayer;

            if (canSeePlayer)
            {
                lastKnownPlayerPosition = player.position;
                alertTimer = alertDuration;
            }
            else if (canHearPlayer)
            {
                lastKnownPlayerPosition = hearingSensor.LastHeardPosition;
                alertTimer = alertDuration;
            }
            else
            {
                alertTimer = Mathf.Max(0f, alertTimer - Time.deltaTime);
            }

            if (alertTimer > 0f && Time.time >= nextAlertBroadcastTime)
            {
                BroadcastAlert();
                nextAlertBroadcastTime = Time.time + alertBroadcastInterval;
            }

            float playerDistance = PlanarDistance(transform.position, player.position);
            if (playerDistance <= contactRadius)
            {
                SetDisplayedState(EnemyState.Attack);
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamagePerSecond * Time.deltaTime);
                }
                return;
            }

            bool isChasing = canSeePlayer;
            bool isInvestigating = !canSeePlayer && alertTimer > 0f;
            if (isChasing)
            {
                SetDisplayedState(EnemyState.Chase);
            }
            else if (isInvestigating)
            {
                SetDisplayedState(EnemyState.Investigate);
            }
            else
            {
                SetDisplayedState(EnemyState.Patrol);
            }

            agent.isStopped = false;
            float maxSpeed = isChasing || isInvestigating ? chaseSpeed : wanderSpeed;
            agent.speed = maxSpeed;

            Vector3 targetPosition = isChasing
                ? GetPredictedFlankTarget()
                : isInvestigating
                    ? lastKnownPlayerPosition
                    : GetWanderTarget();

            Vector3 desiredDirection = Flatten(targetPosition - transform.position).normalized;
            Vector3 steering = CalculateSwarmSteering(desiredDirection, maxSpeed);
            steering = ApplyWallAvoidance(steering, maxSpeed);
            steering = Vector3.ClampMagnitude(steering, acceleration);

            Vector3 nextVelocity = Vector3.ClampMagnitude(
                Flatten(agent.velocity) + steering * Time.deltaTime,
                maxSpeed
            );
            agent.velocity = nextVelocity;

            if (nextVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion facing = Quaternion.LookRotation(nextVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, 10f * Time.deltaTime);
            }
        }

        public void Initialize(Transform playerTarget, ZombieHordeManager manager)
        {
            hordeManager = manager;
            BindPlayer(playerTarget);
        }

        public void ReceiveAlert(Vector3 targetPosition, float duration)
        {
            lastKnownPlayerPosition = targetPosition;
            alertTimer = Mathf.Max(alertTimer, duration);
        }

        private void FindPlayerIfNeeded()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    BindPlayer(playerObject.transform);
                }
            }
        }

        private void BindPlayer(Transform target)
        {
            player = target;
            if (player == null)
            {
                return;
            }

            playerHealth = player.GetComponent<PlayerHealth>();
            playerController = player.GetComponent<SimplePlayerController>();
            previousPlayerPosition = player.position;
            hasPlayerPositionSample = true;
        }

        private void UpdatePlayerVelocity()
        {
            if (!hasPlayerPositionSample)
            {
                previousPlayerPosition = player.position;
                hasPlayerPositionSample = true;
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
            estimatedPlayerVelocity = Flatten((player.position - previousPlayerPosition) / deltaTime);
            previousPlayerPosition = player.position;
        }

        private bool CanSeePlayer()
        {
            Vector3 toPlayer = player.position + Vector3.up * targetEyeHeight -
                               (transform.position + Vector3.up * eyeHeight);
            Vector3 planarDirection = Flatten(toPlayer);
            float distance = planarDirection.magnitude;
            if (distance > visionRange || distance < 0.001f)
            {
                return false;
            }

            Vector3 heading = Flatten(agent.velocity);
            if (heading.sqrMagnitude < 0.01f)
            {
                heading = Flatten(transform.forward);
            }

            if (Vector3.Angle(heading, planarDirection) > visionAngle * 0.5f)
            {
                return false;
            }

            float rayDistance = Mathf.Max(0f, toPlayer.magnitude - 0.5f);
            return !Physics.Raycast(
                transform.position + Vector3.up * eyeHeight,
                toPlayer.normalized,
                rayDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );
        }

        private Vector3 GetPredictedFlankTarget()
        {
            Vector3 predictedPosition = player.position + estimatedPlayerVelocity * predictionTime;
            Vector3 toTarget = Flatten(predictedPosition - transform.position);
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return predictedPosition;
            }

            Vector3 side = Vector3.Cross(Vector3.up, toTarget.normalized) *
                           flankSide * Mathf.Min(toTarget.magnitude, maxFlankOffset);
            return predictedPosition + side;
        }

        private Vector3 GetWanderTarget()
        {
            if (Time.time >= nextWanderChangeTime)
            {
                Vector3 heading = Flatten(agent.velocity);
                if (heading.sqrMagnitude < 0.01f)
                {
                    heading = Flatten(transform.forward);
                }

                Vector3 jitter = Random.insideUnitSphere;
                jitter.y = 0f;
                wanderDirection = (heading.normalized * 2f + jitter).normalized;
                nextWanderChangeTime = Time.time + wanderChangeInterval;
            }

            Vector3 currentHeading = Flatten(agent.velocity);
            if (currentHeading.sqrMagnitude < 0.01f)
            {
                currentHeading = Flatten(transform.forward);
            }

            return transform.position + currentHeading.normalized * 4f + wanderDirection * 2.5f;
        }

        private Vector3 CalculateSwarmSteering(Vector3 desiredDirection, float maxSpeed)
        {
            Vector3 currentVelocity = Flatten(agent.velocity);
            Vector3 steering = desiredDirection * maxSpeed - currentVelocity;
            Vector3 separation = Vector3.zero;
            Vector3 velocitySum = Vector3.zero;
            Vector3 positionSum = Vector3.zero;
            int neighborCount = 0;

            foreach (ZombieSwarmAgent other in activeAgents)
            {
                if (other == null || other == this || other.agent == null)
                {
                    continue;
                }

                Vector3 offset = Flatten(transform.position - other.transform.position);
                float distance = offset.magnitude;
                if (distance < 0.001f)
                {
                    continue;
                }

                if (distance < separationRadius)
                {
                    separation += offset / distance * ((separationRadius - distance) / separationRadius);
                }

                if (distance < neighborRadius)
                {
                    neighborCount++;
                    velocitySum += Flatten(other.agent.velocity);
                    positionSum += other.transform.position;
                }
            }

            steering += separation * maxSpeed * separationWeight;
            if (neighborCount > 0)
            {
                Vector3 averageVelocity = velocitySum / neighborCount;
                Vector3 averagePosition = positionSum / neighborCount;
                steering += (averageVelocity - currentVelocity) * alignmentWeight;

                Vector3 cohesionDirection = Flatten(averagePosition - transform.position).normalized;
                steering += (cohesionDirection * maxSpeed - currentVelocity) * cohesionWeight;
            }

            return steering;
        }

        private Vector3 ApplyWallAvoidance(Vector3 steering, float maxSpeed)
        {
            Vector3 heading = Flatten(agent.velocity + steering);
            if (heading.sqrMagnitude < 0.01f)
            {
                heading = Flatten(transform.forward);
            }

            if (Physics.SphereCast(
                    transform.position + Vector3.up * eyeHeight * 0.5f,
                    wallCheckRadius,
                    heading.normalized,
                    out RaycastHit hit,
                    wallCheckDistance,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal).normalized;
                if (Vector3.Dot(tangent, heading) < 0f)
                {
                    tangent = -tangent;
                }

                float strength = 1f - hit.distance / wallCheckDistance;
                steering += (hit.normal + tangent * 1.5f) * (strength * maxSpeed * wallAvoidanceWeight);
            }

            return steering;
        }

        private void BroadcastAlert()
        {
            foreach (ZombieSwarmAgent other in activeAgents)
            {
                if (other == null || other == this)
                {
                    continue;
                }

                if (PlanarDistance(transform.position, other.transform.position) <= neighborAlertRadius)
                {
                    other.ReceiveAlert(lastKnownPlayerPosition, alertDuration * 0.75f);
                }
            }
        }

        private void SetDisplayedState(EnemyState state)
        {
            if (enemyFsm != null && enemyFsm.CurrentState != state)
            {
                enemyFsm.ChangeState(state);
            }
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            return Flatten(first - second).magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAlerted ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Vector3 forward = Application.isPlaying && agent != null && agent.velocity.sqrMagnitude > 0.01f
                ? agent.velocity.normalized
                : transform.forward;
            Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * forward;
            Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * forward;
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, left * visionRange);
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, right * visionRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, neighborRadius);
        }
    }
}
