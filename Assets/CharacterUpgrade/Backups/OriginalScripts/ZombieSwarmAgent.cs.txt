using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Praktikum5.FSM;
using UTS.Player;

namespace UTS.Zombies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieSwarmAgent : MonoBehaviour, IVisionSource
    {
        public enum PZZombieState
        {
            IdleWander,
            Investigating,
            Chasing,
            Attacking,
            Dead
        }

        private static readonly HashSet<ZombieSwarmAgent> activeAgents = new HashSet<ZombieSwarmAgent>();
        private static bool warnedMissingObstacleLayers;

        [Header("Perception (Project Zomboid Senses)")]
        [Tooltip("Jarak pandang visual kerucut zombie (10m)")]
        [SerializeField] private float visionRange = 10f;

        [Tooltip("Sudut kerucut pandang zombie (90 derajat)")]
        [SerializeField, Range(0f, 360f)] private float visionAngle = 90f;

        [Tooltip("Ketinggian mata zombie (1.3m)")]
        [SerializeField] private float eyeHeight = 1.3f;

        [Tooltip("Ketinggian dada target pemain (0.9m)")]
        [SerializeField] private float targetChestHeight = 0.9f;

        [Tooltip("Ketinggian kepala target pemain (1.5m)")]
        [SerializeField] private float targetHeadHeight = 1.5f;

        [Tooltip("Radius sentuhan fisik dari belakang (0.4m)")]
        [SerializeField] private float closeSenseRadius = 0.4f;

        [Tooltip("Layer tembok dan rintangan untuk pengecekan Line of Sight")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("Memory (Project Zomboid Sandbox Preset)")]
        [Tooltip("Durasi Sandbox Memory PZ mengejar titik LKP (Default: 8.0s)")]
        [SerializeField] private float sandboxMemoryDuration = 8.0f;

        [Tooltip("Durasi inspeksi/mencari jejak saat tiba di titik LKP atau suara (3.5s)")]
        [SerializeField] private float investigateDuration = 3.5f;

        [Header("Movement (Romero Shambler Speeds)")]
        [Tooltip("Kecepatan jalan santai berkelana (Wander: 0.9 m/s)")]
        [SerializeField] private float wanderSpeed = 0.9f;

        [Tooltip("Kecepatan jalan menyelidiki suara (Investigate: 1.3 m/s)")]
        [SerializeField] private float investigateSpeed = 1.3f;

        [Tooltip("Kecepatan mengejar target (Chase: 2.3 m/s)")]
        [SerializeField] private float chaseSpeed = 2.3f;

        [Tooltip("Akselerasi Romero Shambler (4.0 m/s2)")]
        [SerializeField] private float acceleration = 4.0f;

        [Tooltip("Kecepatan rotasi saat berbelok")]
        [SerializeField] private float angularSpeed = 360f;

        [Tooltip("Radius area wander lokal dari titik asal spawn")]
        [SerializeField] private float wanderRadius = 6.0f;

        [Header("Combat (Romero Shambler PZ)")]
        [Tooltip("Jarak kontak serangan (1.2m)")]
        [SerializeField] private float attackRange = 1.2f;

        [Tooltip("Jarak lepas dari mode serang jika pemain menjauh (1.8m)")]
        [SerializeField] private float attackExitRange = 1.8f;

        [Tooltip("Jeda waktu antar serangan (1.2s)")]
        [SerializeField] private float attackCooldown = 1.2f;

        [Tooltip("Besaran luka per serangan (15 HP)")]
        [SerializeField] private float attackDamage = 15f;

        [Header("Steering & Separation")]
        [SerializeField] private float separationRadius = 2.0f;
        [SerializeField] private float separationWeight = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLogging;

        // Komponen
        private NavMeshAgent agent;
        private EnemyFSM enemyFsm;
        private Transform player;
        private SimplePlayerController playerController;
        private PlayerSoundEmitter playerSoundEmitter;
        private ZombieHordeManager hordeManager;

        // FSM & Timer
        private PZZombieState currentState = PZZombieState.IdleWander;
        private Vector3 spawnPosition;
        private Vector3 lastKnownPosition;
        private Vector3 investigateTarget;
        private Vector3 currentWanderTarget;
        private Vector3 lastRequestedDestination;

        private float memoryTimer;
        private float inspectTimer;
        private float wanderWaitTimer;
        private float nextAttackTime;
        private float nextDestinationUpdateTime;
        private float huntTimer;
        private bool isSeeingPlayer;
        private bool deathHandled;
        private bool hasRequestedDestination;

        // Implementasi IVisionSource
        public static IEnumerable<ZombieSwarmAgent> ActiveAgents => activeAgents;
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public LayerMask ObstacleMask => obstacleMask;
        public bool IsSeeingPlayer => isSeeingPlayer;
        public PZZombieState CurrentState => currentState;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyFsm = GetComponent<EnemyFSM>();

            // Nonaktifkan EnemyFSM internal agar tidak menimpa pergerakan zombie
            if (enemyFsm != null) enemyFsm.enabled = false;

            // Nonaktifkan EnemyPerception jika terpasang
            EnemyPerception perception = GetComponent<EnemyPerception>();
            if (perception != null) perception.enabled = false;

            // Nonaktifkan NPCSensor lama agar tidak membocorkan deteksi 2m tembus dinding
            global::NPCSensor sensor = GetComponent<global::NPCSensor>();
            if (sensor != null) sensor.enabled = false;

            // Pastikan RobberyBobVisionCone terpasang dan aktif untuk visual kerucut merah
            RobberyBobVisionCone visionCone = GetComponent<RobberyBobVisionCone>();
            if (visionCone == null) visionCone = gameObject.AddComponent<RobberyBobVisionCone>();
            visionCone.enabled = true;

            // Konfigurasi NavMeshAgent sesuai Romero Shambler PZ
            agent.updateRotation = false;
            agent.autoBraking = false;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = attackRange * 0.75f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(30, 80);

            ConfigureObstacleMask();
            spawnPosition = transform.position;
            currentWanderTarget = transform.position;
            lastKnownPosition = transform.position;
            investigateTarget = transform.position;
        }

        private void OnEnable() => activeAgents.Add(this);
        private void OnDisable() => activeAgents.Remove(this);

        private void Start()
        {
            FindPlayerIfNeeded();
            if (hordeManager == null) hordeManager = FindAnyObjectByType<ZombieHordeManager>();
            wanderWaitTimer = Random.Range(1f, 4f);
            SetState(PZZombieState.IdleWander, "Spawn");
            if (enemyFsm != null) enemyFsm.SetDisplayStateOnly(EnemyState.Patrol);
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            FindPlayerIfNeeded();

            // Cek status kematian Enemy
            if (enemyFsm != null && enemyFsm.IsDead)
            {
                if (!deathHandled)
                {
                    deathHandled = true;
                    SetState(PZZombieState.Dead, "Enemy Dead");
                }
                StopAgent();
                return;
            }

            // Jika game over atau player mati, zombie kembali santai
            if ((hordeManager != null && hordeManager.IsGameOver) || (playerController != null && playerController.IsDead))
            {
                if (currentState != PZZombieState.IdleWander && currentState != PZZombieState.Dead)
                {
                    SetState(PZZombieState.IdleWander, "Player Dead / Game Over");
                }
                UpdateIdleWander();
                ApplyFacingAndSeparation(wanderSpeed);
                return;
            }

            // Hitung timer alarm objektif jika ada
            if (huntTimer > 0f) huntTimer = Mathf.Max(0f, huntTimer - Time.deltaTime);

            // Cek persepsi visual multi-point (Dual-Raycast)
            isSeeingPlayer = CanSeePlayer();

            // Cek persepsi pendengaran suara langkah kaki pemain
            bool heardPlayerSound = CheckHearingPlayer();

            // Eksekusi Logika FSM Project Zomboid
            UpdateFSM(isSeeingPlayer, heardPlayerSound);
        }

        private void UpdateFSM(bool seePlayer, bool heardPlayer)
        {
            // Jika pemain terlihat secara visual atau ada alarm hunt aktif
            if (seePlayer || huntTimer > 0f)
            {
                lastKnownPosition = player.position;
                memoryTimer = sandboxMemoryDuration; // Reset memori 8.0s

                float distanceToPlayer = PlanarDistance(transform.position, player.position);

                // Masuk atau bertahan dalam mode Attack
                if (distanceToPlayer <= attackRange)
                {
                    SetState(PZZombieState.Attacking, "Contact Attack");
                    UpdateAttacking();
                    return;
                }
                else if (currentState == PZZombieState.Attacking && distanceToPlayer <= attackExitRange)
                {
                    // Masih dalam jarak toleransi serang (1.2m - 1.8m)
                    UpdateAttacking();
                    return;
                }
                else
                {
                    // Kejar pemain langsung (Line of Sight jelas)
                    SetState(PZZombieState.Chasing, "Visual Chase");
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    RequestDestination(player.position);
                    ApplyFacingAndSeparation(chaseSpeed);
                    return;
                }
            }

            // LoS terputus, atau belum melihat pemain
            switch (currentState)
            {
                case PZZombieState.Attacking:
                    // Pemain menjauh melebihi exit range atau terputus LoS
                    SetState(PZZombieState.Chasing, "Player Exited Attack Range");
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    RequestDestination(lastKnownPosition);
                    ApplyFacingAndSeparation(chaseSpeed);
                    break;

                case PZZombieState.Chasing:
                    // LoS baru saja putus: LKP tracking selama 8.0s Sandbox Memory
                    memoryTimer -= Time.deltaTime;

                    // Jika mendengar suara pemain baru saat mengejar LKP, perbarui LKP ke titik suara
                    if (heardPlayer && player != null)
                    {
                        lastKnownPosition = player.position;
                    }

                    if (memoryTimer <= 0f)
                    {
                        // 8 detik habis dalam perjalanan menuju LKP -> Zombie menyerah dan kembali Wander
                        SetState(PZZombieState.IdleWander, "Sandbox Memory Expired (8s)");
                        agent.speed = wanderSpeed;
                    }
                    else
                    {
                        // Lanjutkan mengejar koordinat LKP
                        agent.isStopped = false;
                        agent.speed = chaseSpeed;
                        RequestDestination(lastKnownPosition);
                        ApplyFacingAndSeparation(chaseSpeed);

                        // Cek apakah sudah tiba di titik LKP
                        bool reachedLKP = PlanarDistance(transform.position, lastKnownPosition) <= 1.2f ||
                                          (agent.hasPath && !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.5f));
                        if (reachedLKP)
                        {
                            // Tiba di LKP sebelum 8s -> Bertransisi ke Investigating dan inspeksi selama 3.5s
                            SetState(PZZombieState.Investigating, "Arrived at LKP");
                            investigateTarget = lastKnownPosition;
                            inspectTimer = investigateDuration;
                            agent.isStopped = true;
                        }
                    }
                    break;

                case PZZombieState.Investigating:
                    // Jika mendengar suara pemain baru saat investigasi, perbarui titik sasaran
                    if (heardPlayer && player != null)
                    {
                        investigateTarget = player.position;
                        inspectTimer = investigateDuration;
                        agent.isStopped = false;
                    }

                    agent.speed = investigateSpeed;
                    float distToTarget = PlanarDistance(transform.position, investigateTarget);
                    bool arrivedAtTarget = distToTarget <= 1.2f ||
                                           (agent.hasPath && !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.5f));

                    if (!arrivedAtTarget)
                    {
                        // Bergerak santai menuju titik investigasi (1.3 m/s)
                        agent.isStopped = false;
                        RequestDestination(investigateTarget);
                        ApplyFacingAndSeparation(investigateSpeed);
                    }
                    else
                    {
                        // Sudah tiba di titik suara / LKP: Berhenti & inspeksi selama 3.5s
                        agent.isStopped = true;
                        inspectTimer -= Time.deltaTime;
                        if (inspectTimer <= 0f)
                        {
                            // Tidak ada kontak baru setelah 3.5 detik -> Kembali ke Wander
                            SetState(PZZombieState.IdleWander, "Investigation Complete");
                            wanderWaitTimer = Random.Range(2f, 4f);
                        }
                    }
                    break;

                case PZZombieState.IdleWander:
                    // Jika mendengar suara langkah/lari pemain
                    if (heardPlayer && player != null)
                    {
                        SetState(PZZombieState.Investigating, "Heard Sound");
                        investigateTarget = player.position;
                        inspectTimer = investigateDuration;
                        agent.isStopped = false;
                        agent.speed = investigateSpeed;
                        RequestDestination(investigateTarget);
                        ApplyFacingAndSeparation(investigateSpeed);
                        return;
                    }

                    UpdateIdleWander();
                    ApplyFacingAndSeparation(wanderSpeed);
                    break;

                case PZZombieState.Dead:
                    StopAgent();
                    break;
            }
        }

        private void UpdateAttacking()
        {
            agent.isStopped = true;

            if (player != null)
            {
                // Putar badan menghadap pemain saat menyerang
                Vector3 lookDir = Flatten(player.position - transform.position);
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized, Vector3.up), 8f * Time.deltaTime);
                }

                // Eksekusi serangan tiap interval attackCooldown (1.2 detik)
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    if (playerController != null)
                    {
                        playerController.TakeDamage(attackDamage);
                        if (debugLogging)
                        {
                            Debug.Log($"[ZombieSwarmAgent] {name} menyerang player! Damage: {attackDamage}", this);
                        }
                    }
                }
            }
        }

        private void UpdateIdleWander()
        {
            agent.speed = wanderSpeed;

            if (wanderWaitTimer > 0f)
            {
                // Sedang berdiri diam (shambling pause)
                agent.isStopped = true;
                wanderWaitTimer -= Time.deltaTime;
                if (wanderWaitTimer <= 0f)
                {
                    PickNewWanderTarget();
                }
                return;
            }

            // Sedang berjalan menuju target wander
            agent.isStopped = false;
            RequestDestination(currentWanderTarget);

            if (PlanarDistance(transform.position, currentWanderTarget) <= 1.0f ||
                (agent.hasPath && !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.5f)))
            {
                // Tiba di target wander -> diam sejenak 3.5s - 6.5s
                wanderWaitTimer = Random.Range(3.5f, 6.5f);
                agent.isStopped = true;
            }
        }

        private void PickNewWanderTarget()
        {
            Vector3 origin = transform.position;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, agent.areaMask))
                {
                    currentWanderTarget = hit.position;
                    return;
                }
            }
            currentWanderTarget = transform.position;
        }

        private bool CanSeePlayer()
        {
            if (player == null) return false;

            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayerHorizontal = Flatten(player.position - transform.position);
            float distance = toPlayerHorizontal.magnitude;

            if (distance > visionRange || distance < 0.001f) return false;

            // Jika dalam jarak sangat dekat (physical touch 0.4m), deteksi sentuhan dari belakang
            // Tetap lakukan raycast oklusi rintangan agar tidak tembus dinding 0.3m
            if (distance <= closeSenseRadius)
            {
                Vector3 toChestNear = (player.position + Vector3.up * targetChestHeight) - origin;
                float nearDist = toChestNear.magnitude;
                bool occluded = Physics.Raycast(origin, toChestNear.normalized, Mathf.Max(0f, nearDist - 0.05f), obstacleMask, QueryTriggerInteraction.Ignore);
                if (!occluded)
                {
                    return true;
                }
            }

            // Pengecekan sudut kerucut pandang 90 derajat (setengah sudut 45)
            float angle = Vector3.Angle(Flatten(transform.forward), toPlayerHorizontal.normalized);
            if (angle > visionAngle * 0.5f)
            {
                return false;
            }

            // Dual-point Raycast: Mata Zombie -> Dada Pemain (0.9m) & Kepala Pemain (1.5m)
            Vector3 chestPoint = player.position + Vector3.up * targetChestHeight;
            Vector3 headPoint = player.position + Vector3.up * targetHeadHeight;

            Vector3 toChest = chestPoint - origin;
            float chestDist = toChest.magnitude;
            bool chestVisible = !Physics.Raycast(origin, toChest.normalized, Mathf.Max(0f, chestDist - 0.05f), obstacleMask, QueryTriggerInteraction.Ignore);

            if (chestVisible) return true;

            Vector3 toHead = headPoint - origin;
            float headDist = toHead.magnitude;
            bool headVisible = !Physics.Raycast(origin, toHead.normalized, Mathf.Max(0f, headDist - 0.05f), obstacleMask, QueryTriggerInteraction.Ignore);

            return headVisible;
        }

        private bool CheckHearingPlayer()
        {
            if (playerSoundEmitter == null)
            {
                if (playerController != null && playerController.SoundEmitter != null)
                {
                    playerSoundEmitter = playerController.SoundEmitter;
                }
                else if (player != null)
                {
                    playerSoundEmitter = player.GetComponent<PlayerSoundEmitter>();
                }
            }

            if (playerSoundEmitter == null) return false;

            return playerSoundEmitter.CanBeHeardBy(transform.position, obstacleMask);
        }

        private void ApplyFacingAndSeparation(float currentSpeed)
        {
            // Hitung separation nudge agar zombie tidak saling menembus
            Vector3 separation = Vector3.zero;
            foreach (ZombieSwarmAgent other in activeAgents)
            {
                if (other == null || other == this) continue;
                Vector3 offset = Flatten(transform.position - other.transform.position);
                float dist = offset.magnitude;
                if (dist > 0.001f && dist < separationRadius)
                {
                    separation += offset / dist * ((separationRadius - dist) / separationRadius);
                }
            }

            Vector3 nudge = Vector3.ClampMagnitude(separation * separationWeight, currentSpeed * 0.4f);
            if (nudge.sqrMagnitude > 0.0001f)
            {
                agent.Move(nudge * Time.deltaTime);
            }

            // Rotasi halus menghadap arah gerak
            Vector3 facingVelocity = Flatten(agent.desiredVelocity);
            if (facingVelocity.sqrMagnitude < 0.01f) facingVelocity = Flatten(agent.velocity);
            if (facingVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facingVelocity.normalized, Vector3.up), 8f * Time.deltaTime);
            }
        }

        private void RequestDestination(Vector3 target)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            if (!hasRequestedDestination || Time.time >= nextDestinationUpdateTime || PlanarDistance(lastRequestedDestination, target) > 0.4f)
            {
                agent.SetDestination(target);
                lastRequestedDestination = target;
                hasRequestedDestination = true;
                nextDestinationUpdateTime = Time.time + 0.25f;
            }
        }

        private void SetState(PZZombieState newState, string reason)
        {
            if (currentState == newState) return;
            currentState = newState;

            // Sinkronisasi badge overhead HUD di FSMGameHUD melalui EnemyFSM
            EnemyState displayState = newState == PZZombieState.IdleWander ? EnemyState.Patrol :
                                      newState == PZZombieState.Investigating ? EnemyState.Investigate :
                                      newState == PZZombieState.Chasing ? EnemyState.Chase :
                                      newState == PZZombieState.Attacking ? EnemyState.Attack :
                                      EnemyState.Dead;

            if (enemyFsm != null)
            {
                enemyFsm.SetDisplayStateOnly(displayState);
            }

            if (debugLogging)
            {
                Debug.Log($"[ZombieSwarmAgent] {name}: {newState} ({reason})", this);
            }
        }

        private void ConfigureObstacleMask()
        {
            int obstacleLayers = LayerMask.GetMask("Obstacle", "Wall");
            if (obstacleLayers == 0) obstacleLayers = (1 << 3) | (1 << 7);
            obstacleMask = obstacleLayers;

            obstacleMask.value &= ~(1 << gameObject.layer);
            if (player != null) obstacleMask.value &= ~(1 << player.gameObject.layer);
            obstacleMask.value &= ~LayerMask.GetMask("Player", "Zombie", "Zombies");
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
            if (player == null) return;
            playerController = player.GetComponent<SimplePlayerController>();
            playerSoundEmitter = player.GetComponent<PlayerSoundEmitter>();
            ConfigureObstacleMask();
        }

        public void Initialize(Transform playerTarget, ZombieHordeManager manager)
        {
            hordeManager = manager;
            BindPlayer(playerTarget);
        }

        public void ReceiveHuntOrder(float duration)
        {
            huntTimer = Mathf.Max(huntTimer, duration);
            if (currentState != PZZombieState.Dead)
            {
                SetState(PZZombieState.Chasing, "Hunt Order / Alarm");
            }
        }

        public void ReceiveAlert(Vector3 targetPosition, float duration)
        {
            // Kompatibilitas jika dipanggil sistem eksternal
            if (currentState == PZZombieState.Attacking || currentState == PZZombieState.Chasing || currentState == PZZombieState.Dead) return;
            investigateTarget = targetPosition;
            inspectTimer = investigateDuration;
            SetState(PZZombieState.Investigating, "External Alert");
        }

        private void StopAgent()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
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
            Gizmos.color = isSeeingPlayer ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);

            Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, left * visionRange);
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, right * visionRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, closeSenseRadius);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastKnownPosition, 0.25f);

                Vector3 target = currentState == PZZombieState.Chasing ? lastKnownPosition :
                                 currentState == PZZombieState.Investigating ? investigateTarget :
                                 currentWanderTarget;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target);
                Gizmos.DrawSphere(target, 0.18f);
            }
        }
    }
}
