using UnityEngine;
using UnityEngine.AI;

namespace Praktikum4.NavMeshNavigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshChaser : MonoBehaviour
    {
        [Header("Target & Movement Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float stoppingDistance = 1.5f;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 12f;

        [Header("Smart Repathing")]
        [Tooltip("Batas frekuensi repath dalam detik (mencegah kalkulasi setiap frame).")]
        [SerializeField] private float repathInterval = 0.25f;

        [Tooltip("Jika target bergerak lebih jauh dari jarak ini, segera jadwalkan repath.")]
        [SerializeField] private float repathDistanceThreshold = 0.5f;

        [Header("Status & Diagnostics (Read-Only)")]
        [SerializeField] private bool arrivedAtTarget;
        [SerializeField] private float remainingDistance;
        [SerializeField] private bool hasPath;
        [SerializeField] private bool isPathPending;

        private NavMeshAgent agent;
        private Vector3 lastTargetPosition;
        private float repathTimer;

        public NavMeshAgent Agent => agent;
        public Transform Target { get => target; set => target = value; }
        public bool ArrivedAtTarget => arrivedAtTarget;
        public float RemainingDistance => remainingDistance;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ApplyAgentSettings();
        }

        private void Start()
        {
            if (target != null)
            {
                lastTargetPosition = target.position;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
            }
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            UpdateSmartRepathing();
            UpdateStatusDiagnostics();
        }

        private void ApplyAgentSettings()
        {
            if (agent == null) return;
            agent.stoppingDistance = stoppingDistance;
            agent.speed = moveSpeed;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = acceleration;
            agent.autoBraking = true;
        }

        private void UpdateSmartRepathing()
        {
            if (target == null) return;

            repathTimer -= Time.deltaTime;
            float targetMovedDistance = Vector3.Distance(target.position, lastTargetPosition);

            // Repath HANYA jika target berpindah cukup jauh ATAU interval waktu habis
            if (targetMovedDistance >= repathDistanceThreshold || repathTimer <= 0f)
            {
                agent.SetDestination(target.position);
                lastTargetPosition = target.position;
                repathTimer = repathInterval;
            }
        }

        private void UpdateStatusDiagnostics()
        {
            remainingDistance = agent.remainingDistance;
            hasPath = agent.hasPath;
            isPathPending = agent.pathPending;

            // Agen dianggap sampai jika path sudah siap dan sisa jarak di bawah stopping distance
            if (!agent.pathPending)
            {
                arrivedAtTarget = agent.remainingDistance <= agent.stoppingDistance &&
                                  (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f);
            }
            else
            {
                arrivedAtTarget = false;
            }
        }

        private void OnValidate()
        {
            if (agent != null)
            {
                ApplyAgentSettings();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisasi stopping distance di Scene View
            Gizmos.color = arrivedAtTarget ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
