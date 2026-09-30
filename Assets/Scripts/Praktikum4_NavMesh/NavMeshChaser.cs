using UnityEngine;
using UnityEngine.AI;

namespace Praktikum4.NavMeshNavigation
{
    public enum NavMeshNavigationMode
    {
        DirectChase,    // Mode standar Praktikum 4 (Selalu mengejar Target)
        PatrolAndChase  // Mode FSM Praktikum 2 (Patroli jika target jauh, kejar jika dekat)
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshChaser : MonoBehaviour
    {
        [Header("Mode Navigasi")]
        [SerializeField] private NavMeshNavigationMode navigationMode = NavMeshNavigationMode.DirectChase;

        [Header("Target & Movement Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float stoppingDistance = 1.5f;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 12f;

        [Header("Patrol Waypoints (Mode PatrolAndChase)")]
        [SerializeField] private Transform[] patrolWaypoints;
        [SerializeField] private float waypointWaitTime = 2f;
        [SerializeField] private float detectionRange = 10f;
        private int currentWaypointIndex = 0;
        private float waypointWaitTimer;

        [Header("UI Feedback Indicators")]
        [SerializeField] private GameObject exclamationMark; // Muncul saat Chase (!)
        [SerializeField] private GameObject questionMark;    // Muncul saat Arrived / Curiga (?)

        [Header("Smart Repathing")]
        [Tooltip("Batas frekuensi repath dalam detik (mencegah kalkulasi setiap frame).")]
        [SerializeField] private float repathInterval = 0.25f;

        [Tooltip("Jika target bergerak lebih jauh dari jarak ini, segera jadwalkan repath.")]
        [SerializeField] private float repathDistanceThreshold = 0.5f;

        [Header("Status & Diagnostics (Read-Only)")]
        [SerializeField] private bool isChasingTarget;
        [SerializeField] private bool arrivedAtTarget;
        [SerializeField] private float remainingDistance;
        [SerializeField] private bool hasPath;
        [SerializeField] private bool isPathPending;

        private NavMeshAgent agent;
        private Vector3 lastTargetPosition;
        private float repathTimer;

        public NavMeshAgent Agent => agent;
        public Transform Target { get => target; set => target = value; }
        public NavMeshNavigationMode NavigationMode { get => navigationMode; set => navigationMode = value; }
        public bool ArrivedAtTarget => arrivedAtTarget;
        public float RemainingDistance => remainingDistance;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ApplyAgentSettings();
            EnsureIndicators();
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

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (exclamationMark != null && exclamationMark.activeSelf)
            {
                exclamationMark.transform.rotation = cam.transform.rotation;
            }
            if (questionMark != null && questionMark.activeSelf)
            {
                questionMark.transform.rotation = cam.transform.rotation;
            }
        }

        private void EnsureIndicators()
        {
            if (exclamationMark == null)
            {
                Transform found = transform.Find("Indikator_Seru");
                if (found != null) exclamationMark = found.gameObject;
            }
            if (questionMark == null)
            {
                Transform found = transform.Find("Indikator_Tanya");
                if (found != null) questionMark = found.gameObject;
            }

            // Jika belum ada GameObject indikator, buat secara prosedural TextMesh di atas kepala
            if (exclamationMark == null)
            {
                exclamationMark = CreateRuntimeIndicator("Indikator_Seru", "!", Color.red);
            }
            if (questionMark == null)
            {
                questionMark = CreateRuntimeIndicator("Indikator_Tanya", "?", Color.yellow);
            }
        }

        private GameObject CreateRuntimeIndicator(string objName, string text, Color color)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            TextMesh tm = obj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 52;
            tm.characterSize = 0.15f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;

            obj.SetActive(false);
            return obj;
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
            if (navigationMode == NavMeshNavigationMode.DirectChase)
            {
                isChasingTarget = target != null;
                ExecuteChaseTarget();
            }
            else
            {
                // Mode PatrolAndChase: Cek jarak ke target
                float distanceToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
                if (distanceToTarget <= detectionRange)
                {
                    isChasingTarget = true;
                    ExecuteChaseTarget();
                }
                else
                {
                    isChasingTarget = false;
                    ExecutePatrolWaypoints();
                }
            }
        }

        private void ExecuteChaseTarget()
        {
            if (target == null) return;

            repathTimer -= Time.deltaTime;
            float targetMovedDistance = Vector3.Distance(target.position, lastTargetPosition);

            // Repath HANYA jika target berpindah cukup jauh ATAU interval waktu habis
            if (targetMovedDistance >= repathDistanceThreshold || repathTimer <= 0f)
            {
                agent.stoppingDistance = stoppingDistance;
                agent.SetDestination(target.position);
                lastTargetPosition = target.position;
                repathTimer = repathInterval;
            }
        }

        private void ExecutePatrolWaypoints()
        {
            if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

            Transform wp = patrolWaypoints[currentWaypointIndex];
            if (wp == null) return;

            agent.stoppingDistance = 0.5f;

            if (!agent.pathPending && agent.remainingDistance <= 0.6f)
            {
                waypointWaitTimer -= Time.deltaTime;
                if (waypointWaitTimer <= 0f)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                    agent.SetDestination(patrolWaypoints[currentWaypointIndex].position);
                    waypointWaitTimer = waypointWaitTime;
                }
            }
            else if (!agent.hasPath)
            {
                agent.SetDestination(wp.position);
            }
        }

        private void UpdateStatusDiagnostics()
        {
            remainingDistance = agent.remainingDistance;
            hasPath = agent.hasPath;
            isPathPending = agent.pathPending;

            // Agen dianggap sampai jika path sudah siap dan sisa jarak di bawah stopping distance
            if (!agent.pathPending && isChasingTarget)
            {
                arrivedAtTarget = agent.remainingDistance <= agent.stoppingDistance &&
                                  (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f);
            }
            else
            {
                arrivedAtTarget = false;
            }

            // Update balon indikator reaksi UI di atas kepala
            if (exclamationMark != null)
            {
                exclamationMark.SetActive(isChasingTarget && !arrivedAtTarget);
            }

            if (questionMark != null)
            {
                questionMark.SetActive(arrivedAtTarget || (!isChasingTarget && navigationMode == NavMeshNavigationMode.PatrolAndChase && waypointWaitTimer > 0f));
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
