using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Praktikum6.BT
{
    public enum EnemyAction
    {
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyBTController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform safePoint;

        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float waypointTolerance = 0.5f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float lostPlayerDelay = 2f;
        [SerializeField] private float fleeSpeed = 5f;
        [SerializeField] private float safeDistance = 12f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackExitRange = 2.75f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float lowHealthThreshold = 30f;

        [Header("Current Action (Read-Only)")]
        [SerializeField] private string currentAction = "PATROL";

        private BTNode rootNode;
        private PlayerHealth playerHealth;
        private EnemyAction activeAction = EnemyAction.Patrol;
        private int currentPatrolIndex;
        private bool patrolDestinationAssigned;
        private float lastSeenTime = float.NegativeInfinity;
        private Vector3 lastSeenPosition;

        public EnemyAction CurrentAction => activeAction;
        public string CurrentActionName => currentAction;
        public float AttackRange => attackRange;
        public EnemyHealth Health => health;

        public event Action<EnemyAction> OnActionChanged;
        public event Action OnAttack;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (health == null) health = GetComponent<EnemyHealth>();

            if (player == null && perception != null) player = perception.PlayerTarget;
            if (player == null)
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null) player = taggedPlayer.transform;
            }

            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
            if (patrolPoints == null || patrolPoints.Length == 0) FindPatrolPoints();
            if (safePoint == null)
            {
                GameObject point = GameObject.Find("SafePoint");
                if (point != null) safePoint = point.transform;
            }
        }

        private void Start()
        {
            BuildBehaviorTree();
        }

        private void Update()
        {
            // Death remains an interrupt above the tree, as in Praktikum 5.
            if (health != null && health.IsDead)
            {
                SetAction(EnemyAction.Dead);
                StopAgent();
                return;
            }

            if (rootNode != null) rootNode.Tick();
        }

        private void BuildBehaviorTree()
        {
            BTNode flee = new SequenceNode(new List<BTNode>
            {
                new ConditionNode(IsHealthLow),
                new ActionNode(Flee)
            });

            BTNode attack = new SequenceNode(new List<BTNode>
            {
                new ConditionNode(CanSeePlayerNow),
                new ConditionNode(IsPlayerInAttackRange),
                new ActionNode(PrepareAttack),
                new CooldownDecorator(new ActionNode(AttackPlayer), attackCooldown)
            });

            BTNode chase = new SequenceNode(new List<BTNode>
            {
                new ConditionNode(CanSeePlayerOrRecentlySeen),
                new ActionNode(ChasePlayer)
            });

            rootNode = new SelectorNode(new List<BTNode>
            {
                flee,
                attack,
                chase,
                new ActionNode(Patrol)
            });
        }

        private bool IsHealthLow()
        {
            return health != null && health.CurrentHealth <= lowHealthThreshold;
        }

        private bool CanSeePlayerNow()
        {
            if (player == null || perception == null || (playerHealth != null && playerHealth.IsDead)) return false;
            if (!perception.CheckPlayerVisibility()) return false;

            lastSeenTime = Time.time;
            lastSeenPosition = player.position;
            return true;
        }

        private bool CanSeePlayerOrRecentlySeen()
        {
            if (CanSeePlayerNow()) return true;
            if (playerHealth != null && playerHealth.IsDead) return false;
            return (activeAction == EnemyAction.Chase || activeAction == EnemyAction.Attack)
                && Time.time - lastSeenTime <= lostPlayerDelay;
        }

        private bool IsPlayerInAttackRange()
        {
            if (player == null) return false;
            float range = activeAction == EnemyAction.Attack ? attackExitRange : attackRange;
            return Vector3.Distance(transform.position, player.position) <= range;
        }

        private NodeState Patrol()
        {
            if (agent == null || !agent.isOnNavMesh || patrolPoints == null || patrolPoints.Length == 0)
                return NodeState.Failure;

            bool entering = activeAction != EnemyAction.Patrol;
            SetAction(EnemyAction.Patrol);
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0.2f;

            if (entering || !patrolDestinationAssigned)
            {
                SetPatrolDestination();
            }
            else if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                SetPatrolDestination();
            }
            else if (!agent.hasPath)
            {
                SetPatrolDestination();
            }

            return NodeState.Running;
        }

        private void SetPatrolDestination()
        {
            Transform point = patrolPoints[currentPatrolIndex];
            patrolDestinationAssigned = point != null && agent.SetDestination(point.position);
        }

        private NodeState ChasePlayer()
        {
            if (agent == null || !agent.isOnNavMesh || player == null) return NodeState.Failure;

            SetAction(EnemyAction.Chase);
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = attackRange * 0.8f;
            agent.SetDestination(lastSeenPosition);
            return NodeState.Running;
        }

        private NodeState PrepareAttack()
        {
            if (agent == null || !agent.isOnNavMesh || player == null) return NodeState.Failure;

            bool entering = activeAction != EnemyAction.Attack;
            SetAction(EnemyAction.Attack);
            agent.isStopped = true;
            if (entering) agent.ResetPath();
            FacePlayer();
            return NodeState.Success;
        }

        private NodeState AttackPlayer()
        {
            if (player == null || (playerHealth != null && playerHealth.IsDead)) return NodeState.Failure;

            OnAttack?.Invoke();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
            Debug.Log($"[{name}] BT Attack: {attackDamage} damage", this);
            return NodeState.Success;
        }

        private NodeState Flee()
        {
            if (agent == null || !agent.isOnNavMesh || safePoint == null) return NodeState.Failure;

            SetAction(EnemyAction.Flee);
            agent.isStopped = false;
            agent.speed = fleeSpeed;
            agent.stoppingDistance = 0.5f;

            float distanceToSafety = Vector3.Distance(transform.position, safePoint.position);
            float distanceToPlayer = perception != null ? perception.DistanceToPlayer : float.PositiveInfinity;
            if (distanceToSafety <= 1.5f || distanceToPlayer >= safeDistance)
            {
                // Keep Praktikum 5's recovery after reaching safety.
                if (health != null) health.Heal(health.MaxHealth);
                StopAgent();
                return NodeState.Success;
            }

            agent.SetDestination(safePoint.position);
            return NodeState.Running;
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void FacePlayer()
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        private void SetAction(EnemyAction action)
        {
            if (activeAction == action) return;
            activeAction = action;
            currentAction = action.ToString().ToUpperInvariant();
            OnActionChanged?.Invoke(action);
        }

        private void FindPatrolPoints()
        {
            GameObject group = GameObject.Find("PatrolPoints");
            if (group == null || group.transform.childCount == 0) return;

            patrolPoints = new Transform[group.transform.childCount];
            for (int i = 0; i < patrolPoints.Length; i++)
                patrolPoints[i] = group.transform.GetChild(i);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackExitRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }
    }
}
