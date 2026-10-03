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
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyFSM : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyHealth health;

        [Header("Patrol Settings")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float waypointTolerance = 0.5f;

        [Header("Chase Settings")]
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float lostPlayerDelay = 2f;

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

        private int currentPatrolIndex = 0;
        private float lostPlayerTimer = 0f;
        private float nextAttackTime = 0f;
        private bool fleeTriggered = false;
        private PlayerHealth playerHealth;

        public EnemyState CurrentState => currentState;
        public float AttackRange => attackRange;
        public float AttackExitRange => attackExitRange;
        public EnemyHealth Health => health;

        public event Action<EnemyState> OnStateChanged;
        public event Action OnAttack;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (health == null) health = GetComponent<EnemyHealth>();

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }

            AutoFindWaypointsIfEmpty();
            AutoFindSafePointIfEmpty();
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
            if (health != null && health.IsDead)
            {
                if (currentState != EnemyState.Dead)
                {
                    ChangeState(EnemyState.Dead);
                }
                return;
            }

            // Prioritas Kedua: Flee saat HP kritis (<= lowHealthThreshold)
            if (!fleeTriggered && health != null && health.CurrentHealth <= lowHealthThreshold && currentState != EnemyState.Flee)
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
            }
        }

        // ==========================================
        // STATE TRANSITION & LIFECYCLE HOOKS
        // ==========================================

        public void ChangeState(EnemyState newState)
        {
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
                    SetPatrolDestination();
                    break;

                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    lostPlayerTimer = 0f;
                    if (player != null) agent.SetDestination(player.position);
                    break;

                case EnemyState.Attack:
                    agent.isStopped = true;
                    agent.ResetPath();
                    FacePlayer();
                    break;

                case EnemyState.Flee:
                    agent.isStopped = false;
                    agent.speed = fleeSpeed;
                    if (safePoint != null)
                    {
                        agent.SetDestination(safePoint.position);
                    }
                    break;

                case EnemyState.Dead:
                    agent.isStopped = true;
                    agent.ResetPath();
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
                agent.SetDestination(wp.position);
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
                lostPlayerTimer = 0f;
                if (agent.isOnNavMesh && player != null)
                {
                    agent.SetDestination(player.position);
                }

                // Transisi ke Attack jika dalam attackRange
                if (distance <= attackRange)
                {
                    ChangeState(EnemyState.Attack);
                    return;
                }
            }
            else
            {
                // Player terhalang / keluar dari pandangan
                lostPlayerTimer += Time.deltaTime;
                if (lostPlayerTimer >= lostPlayerDelay)
                {
                    ChangeState(EnemyState.Patrol);
                    return;
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

            // Hysteresis: Kembali ke Chase HANYA jika jarak > attackExitRange ATAU kehilangan Line of Sight
            if (distance > attackExitRange || (perception != null && !perception.CanSeePlayer))
            {
                ChangeState(EnemyState.Chase);
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
                if (health != null)
                {
                    health.Heal(health.MaxHealth);
                }
                fleeTriggered = false;
                ChangeState(EnemyState.Patrol);
                return;
            }
            // Kondisi 2: Player berada di jarak aman (>= safeDistance)
            else if (playerDistance >= safeDistance)
            {
                Debug.Log($"[{gameObject.name}] Berhasil menjauh dari Player (>= {safeDistance}m). Memulihkan HP dan kembali ke Patrol.", this);
                if (health != null)
                {
                    health.Heal(health.MaxHealth);
                }
                fleeTriggered = false;
                ChangeState(EnemyState.Patrol);
                return;
            }
            else if (agent.isOnNavMesh && !agent.hasPath)
            {
                agent.SetDestination(safePoint.position);
            }
        }

        private void UpdateDead()
        {
            // Tidak melakukan aksi apa pun (agen mati)
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
