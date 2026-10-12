using System;
using UnityEngine;
using UnityEngine.AI;

namespace Praktikum5.FSM
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead,
        Investigate
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    public class EnemyFSM : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private global::NPCSensor hearingSensor;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Patrol Settings")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float waypointTolerance = 0.5f;
        [SerializeField] private float investigationWaitTime = 3f;

        [Header("Chase Settings")]
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField, Min(0f)] private float destinationUpdateThreshold = 0.25f;

        [Header("Attack Settings (Hysteresis)")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackExitRange = 2.75f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Flee Settings")]
        [SerializeField] private Transform safePoint;
        [SerializeField] private float fleeSpeed = 5f;
        [SerializeField] private float lowHealthThreshold = 30f;
        [SerializeField] private float safeDistance = 12f;

        [Header("Current State (Read-Only)")]
        [SerializeField] private EnemyState currentState;

        [Header("State Indicator")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.3f, 0f);
        [SerializeField] private bool enableWorldTextMesh;

        private int currentPatrolIndex = 0;
        private float nextAttackTime = 0f;
        private float investigationTimer;
        private Vector3 lastKnownPlayerPosition;
        private Vector3 lastRequestedDestination;
        private bool hasLastKnownPlayerPosition;
        private bool hasReachedInvestigationPoint;
        private bool hasRequestedDestination;
        private bool fleeTriggered = false;
        private SimplePlayerController playerHealth;
        private GameObject indicatorObject;
        private TextMesh indicatorText;
        private float currentHealth;

        public EnemyState CurrentState => currentState;
        public float AttackRange => attackRange;
        public float AttackExitRange => attackExitRange;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;

        public event Action<EnemyState> OnStateChanged;
        public event Action OnAttack;
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        // Lets an external controller own movement while this component still
        // provides its state display and notifications.
        public void SetDisplayStateOnly(EnemyState state)
        {
            if (currentState == state) return;

            currentState = state;
            OnStateChanged?.Invoke(currentState);
        }

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (hearingSensor == null) hearingSensor = GetComponent<global::NPCSensor>();
            currentHealth = maxHealth;

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (player != null)
            {
                playerHealth = player.GetComponent<SimplePlayerController>();
            }

            AutoFindWaypointsIfEmpty();
            AutoFindSafePointIfEmpty();
            if (enableWorldTextMesh) SetupTextMesh();
        }

        private void OnEnable()
        {
            OnStateChanged += UpdateIndicator;
            OnHealthChanged += UpdateIndicator;
        }

        private void OnDisable()
        {
            OnStateChanged -= UpdateIndicator;
            OnHealthChanged -= UpdateIndicator;
        }

        private void Start()
        {
            ChangeState(EnemyState.Patrol);
        }

        private void Update()
        {
            // ==========================================
            // 1. GLOBAL TRANSITIONS PRIORITY
            // ==========================================

            // Prioritas Tertinggi: Dead saat HP <= 0
            if (IsDead)
            {
                if (currentState != EnemyState.Dead)
                {
                    ChangeState(EnemyState.Dead);
                }
                return;
            }

            if (perception != null && perception.CanSeePlayer &&
                (playerHealth == null || !playerHealth.IsDead) && currentState != EnemyState.Chase)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            // Prioritas Kedua: Flee saat HP kritis (<= lowHealthThreshold)
            if (!fleeTriggered && currentHealth <= lowHealthThreshold && currentState != EnemyState.Flee)
            {
                fleeTriggered = true;
                ChangeState(EnemyState.Flee);
                return;
            }

            // ==========================================
            // 2. STATE EXECUTION
            // ==========================================
            switch (currentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
                case EnemyState.Flee:
                    UpdateFlee();
                    break;
                case EnemyState.Dead:
                    UpdateDead();
                    break;
                case EnemyState.Investigate:
                    UpdateInvestigate();
                    break;
            }
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null && indicatorObject != null)
            {
                indicatorObject.transform.rotation = cam.transform.rotation;
            }
        }

        private void SetupTextMesh()
        {
            indicatorObject = new GameObject("FSM_State_Indicator");
            indicatorObject.transform.SetParent(transform);
            indicatorObject.transform.localPosition = offset;
            indicatorObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            indicatorText = indicatorObject.AddComponent<TextMesh>();
            indicatorText.fontSize = 50;
            indicatorText.characterSize = 0.12f;
            indicatorText.alignment = TextAlignment.Center;
            indicatorText.anchor = TextAnchor.MiddleCenter;
            indicatorText.fontStyle = FontStyle.Bold;

            UpdateIndicator(currentState);
        }

        private void UpdateIndicator(EnemyState state)
        {
            if (indicatorText == null) return;

            indicatorText.text = $"[{state.ToString().ToUpper()}]\nHP: {currentHealth:F0}/{maxHealth:F0}";

            switch (state)
            {
                case EnemyState.Patrol:
                    indicatorText.color = Color.green;
                    break;
                case EnemyState.Investigate:
                    indicatorText.color = new Color(0.05f, 0.85f, 0.9f);
                    break;
                case EnemyState.Chase:
                    indicatorText.color = new Color(1f, 0.55f, 0f);
                    break;
                case EnemyState.Attack:
                    indicatorText.color = Color.red;
                    break;
                case EnemyState.Flee:
                    indicatorText.color = Color.yellow;
                    break;
                case EnemyState.Dead:
                    indicatorText.color = Color.gray;
                    break;
            }
        }

        private void UpdateIndicator(float current, float max)
        {
            UpdateIndicator(currentState);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (IsDead) OnDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        [ContextMenu("Debug: Kill Enemy")]
        private void DebugKill()
        {
            TakeDamage(maxHealth);
        }

        // ==========================================
        // STATE TRANSITION & LIFECYCLE HOOKS
        // ==========================================

        public void ChangeState(EnemyState newState)
        {
            if (newState == EnemyState.Chase && (perception == null || !perception.CanSeePlayer))
            {
                newState = hasLastKnownPlayerPosition ? EnemyState.Investigate : EnemyState.Patrol;
            }

            if (currentState == newState && currentState != EnemyState.Patrol) return;

            ExitState(currentState);
            EnemyState previousState = currentState;
            currentState = newState;

            Debug.Log($"[{gameObject.name}] FSM Transisi: {previousState} -> {currentState}", this);
            OnStateChanged?.Invoke(currentState);

            EnterState(currentState);
        }

        private void EnterState(EnemyState state)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            switch (state)
            {
                case EnemyState.Patrol:
                    agent.isStopped = false;
                    agent.speed = patrolSpeed;
                    hasRequestedDestination = false;
                    SetPatrolDestination();
                    break;

                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    if (perception != null && perception.CanSeePlayer && player != null)
                    {
                        lastKnownPlayerPosition = player.position;
                        hasLastKnownPlayerPosition = true;
                    }
                    if (hasLastKnownPlayerPosition)
                    {
                        SetAgentDestination(lastKnownPlayerPosition);
                    }
                    break;

                case EnemyState.Attack:
                    agent.isStopped = true;
                    agent.ResetPath();
                    hasRequestedDestination = false;
                    FacePlayer();
                    break;

                case EnemyState.Flee:
                    agent.isStopped = false;
                    agent.speed = fleeSpeed;
                    if (safePoint != null)
                    {
                        hasRequestedDestination = false;
                        SetAgentDestination(safePoint.position);
                    }
                    break;

                case EnemyState.Dead:
                    agent.isStopped = true;
                    agent.ResetPath();
                    hasRequestedDestination = false;
                    break;

                case EnemyState.Investigate:
                    agent.isStopped = false;
                    agent.speed = patrolSpeed;
                    investigationTimer = investigationWaitTime;
                    hasReachedInvestigationPoint = false;
                    hasRequestedDestination = false;
                    if (hasLastKnownPlayerPosition)
                    {
                        SetAgentDestination(lastKnownPlayerPosition);
                    }
                    else if (hearingSensor != null && agent.isOnNavMesh)
                    {
                        lastKnownPlayerPosition = hearingSensor.LastHeardPosition;
                        hasLastKnownPlayerPosition = true;
                        SetAgentDestination(lastKnownPlayerPosition);
                    }
                    break;
            }
        }

        private void ExitState(EnemyState state)
        {
            if (agent == null) return;

            switch (state)
            {
                case EnemyState.Attack:
                    if (agent.isOnNavMesh) agent.isStopped = false;
                    break;
            }
        }

        // ==========================================
        // STATE UPDATES
        // ==========================================

        private void UpdatePatrol()
        {
            // Jika Player terdeteksi dan Player masih hidup, langsung beralih ke Chase
            if (perception != null && perception.CanSeePlayer && (playerHealth == null || !playerHealth.IsDead))
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            if (hearingSensor != null && hearingSensor.HeardPlayerThisFrame)
            {
                lastKnownPlayerPosition = hearingSensor.LastHeardPosition;
                hasLastKnownPlayerPosition = true;
                ChangeState(EnemyState.Investigate);
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                SetPatrolDestination();
            }
            else if (agent.isOnNavMesh && !agent.hasPath)
            {
                SetPatrolDestination();
            }
        }

        private void SetPatrolDestination()
        {
            if (patrolPoints == null || patrolPoints.Length == 0 || !agent.isOnNavMesh) return;

            Transform wp = patrolPoints[currentPatrolIndex];
            if (wp != null)
            {
                SetAgentDestination(wp.position);
            }
        }

        private void SetAgentDestination(Vector3 destination)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            float thresholdSquared = destinationUpdateThreshold * destinationUpdateThreshold;
            if (hasRequestedDestination &&
                (destination - lastRequestedDestination).sqrMagnitude <= thresholdSquared)
            {
                return;
            }

            if (agent.SetDestination(destination))
            {
                lastRequestedDestination = destination;
                hasRequestedDestination = true;
            }
        }

        private void UpdateChase()
        {
            // Jika Player sudah mati, hentikan pengejaran dan kembali ke Patrol
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log($"[{gameObject.name}] Player tereliminasi (HP 0). Menghentikan pengejaran, kembali ke Patrol.", this);
                ChangeState(EnemyState.Patrol);
                return;
            }

            float distance = perception != null ? perception.DistanceToPlayer : float.MaxValue;

            if (perception != null && perception.CanSeePlayer)
            {
                if (player != null)
                {
                    lastKnownPlayerPosition = player.position;
                    hasLastKnownPlayerPosition = true;
                }
                if (agent.isOnNavMesh && player != null)
                {
                    SetAgentDestination(player.position);
                }

                // Transisi ke Attack jika dalam attackRange
                if (distance <= attackRange)
                {
                    ChangeState(EnemyState.Attack);
                    return;
                }

                return;
            }

            if (!hasLastKnownPlayerPosition)
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                SetAgentDestination(lastKnownPlayerPosition);
                if (!agent.pathPending &&
                    agent.remainingDistance <= Mathf.Max(waypointTolerance, agent.stoppingDistance))
                {
                    ChangeState(EnemyState.Investigate);
                }
            }
        }

        private void UpdateAttack()
        {
            // Jika Player sudah mati, hentikan serangan dan kembali ke Patrol
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log($"[{gameObject.name}] Player tereliminasi (HP 0). Menghentikan serangan, kembali ke Patrol.", this);
                ChangeState(EnemyState.Patrol);
                return;
            }

            float distance = perception != null ? perception.DistanceToPlayer : float.MaxValue;

            FacePlayer();

            if (perception != null && perception.CanSeePlayer && distance > attackExitRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            if (perception == null || !perception.CanSeePlayer)
            {
                ChangeState(EnemyState.Investigate);
                return;
            }

            // Melancarkan serangan berkala (Attack Cooldown)
            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        private void AttackPlayer()
        {
            Debug.Log($"[{gameObject.name}] Menyerang Player! ({attackDamage} damage)", this);
            OnAttack?.Invoke();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        private void FacePlayer()
        {
            if (player == null) return;

            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        private void UpdateFlee()
        {
            if (safePoint == null) return;

            float playerDistance = perception != null ? perception.DistanceToPlayer : float.MaxValue;
            float safePointDistance = Vector3.Distance(transform.position, safePoint.position);

            // Kondisi 1: Tiba di SafePoint (<= 1.5m) -> Sembuhkan HP penuh, reset flee, kembali patroli
            if (safePointDistance <= 1.5f)
            {
                Debug.Log($"[{gameObject.name}] Tiba di SafePoint! Memulihkan HP penuh dan kembali ke Patrol.", this);
                Heal(maxHealth);
                fleeTriggered = false;
                ChangeState(EnemyState.Patrol);
                return;
            }
            // Kondisi 2: Player berada di jarak aman (>= safeDistance)
            else if (playerDistance >= safeDistance)
            {
                Debug.Log($"[{gameObject.name}] Berhasil menjauh dari Player (>= {safeDistance}m). Memulihkan HP dan kembali ke Patrol.", this);
                Heal(maxHealth);
                fleeTriggered = false;
                ChangeState(EnemyState.Patrol);
                return;
            }
            else if (agent.isOnNavMesh && !agent.hasPath)
            {
                SetAgentDestination(safePoint.position);
            }
        }

        private void UpdateDead()
        {
            // Tidak melakukan aksi apa pun (agen mati)
        }

        private void UpdateInvestigate()
        {
            if (perception != null && perception.CanSeePlayer)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            if (hearingSensor != null && hearingSensor.HeardPlayerThisFrame)
            {
                lastKnownPlayerPosition = hearingSensor.LastHeardPosition;
                hasLastKnownPlayerPosition = true;
                hasReachedInvestigationPoint = false;
                investigationTimer = investigationWaitTime;
                if (agent.isOnNavMesh)
                {
                    hasRequestedDestination = false;
                    SetAgentDestination(lastKnownPlayerPosition);
                }
                return;
            }

            if (agent.isOnNavMesh && !hasReachedInvestigationPoint)
            {
                SetAgentDestination(lastKnownPlayerPosition);
                if (!agent.pathPending &&
                    agent.remainingDistance <= Mathf.Max(waypointTolerance, agent.stoppingDistance))
                {
                    hasReachedInvestigationPoint = true;
                    investigationTimer = investigationWaitTime;
                }
                return;
            }

            if (hasReachedInvestigationPoint)
            {
                investigationTimer -= Time.deltaTime;
                if (investigationTimer <= 0f)
                {
                    ChangeState(EnemyState.Patrol);
                }
            }
        }

        // ==========================================
        // AUTO CONFIGURATION HELPERS
        // ==========================================

        private void AutoFindWaypointsIfEmpty()
        {
            if (patrolPoints != null && patrolPoints.Length > 0) return;

            GameObject ppParent = GameObject.Find("PatrolPoints");
            if (ppParent != null && ppParent.transform.childCount > 0)
            {
                patrolPoints = new Transform[ppParent.transform.childCount];
                for (int i = 0; i < ppParent.transform.childCount; i++)
                {
                    patrolPoints[i] = ppParent.transform.GetChild(i);
                }
            }
        }

        private void AutoFindSafePointIfEmpty()
        {
            if (safePoint != null) return;

            GameObject sp = GameObject.Find("SafePoint");
            if (sp != null) safePoint = sp.transform;
        }

        private void OnDrawGizmosSelected()
        {
            // Attack Range (Merah)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Attack Exit Range / Hysteresis (Magenta)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackExitRange);

            // Safe Distance (Cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }
    }
}
