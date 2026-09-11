using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState { Patrol, Suspicious, Chase, Search }

    [Header("Components")]
    [SerializeField] private NPCSensor sensor;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Renderer npcRenderer;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointTolerance = 0.7f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Suspicious Settings")]
    [SerializeField] private float baseTurnSpeed = 10f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchTurnSpeed = 60f;

    [Header("Colors Indicator")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color searchColor = Color.blue;

    [Header("Current State (Debug)")]
    [SerializeField] private NPCState currentState;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition;
    private float searchTimer;
    private int patrolIndex = 0;

    private void Start()
    {
        currentState = NPCState.Patrol;
        GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        UpdateMemory();
        MakeDecision();
        ExecuteCurrentState();
        UpdateColor();
    }

    private void UpdateMemory()
    {
        if (sensor.CanSeePlayer)
        {
            lastKnownPosition = sensor.Player.position;
            hasLastKnownPosition = true;
        }
    }

    private void MakeDecision()
    {
        // 1. Prioritas Utama: Melihat Player (Mengejar)
        if (sensor.CanSeePlayer)
        {
            currentState = NPCState.Chase;
            return;
        }

        // 2. Kehilangan jejak setelah mengejar -> Mencari (Search)
        if (currentState == NPCState.Chase)
        {
            searchTimer = searchDuration;
            currentState = NPCState.Search;
            return;
        }

        // 3. Prioritas Kedua: Mendengar Suara (Suspicious)
        // DIBLOKIR saat sedang Search. Suspicious hanya aktif jika NPC sedang Patroli.
        if (sensor.CanHearPlayer && currentState == NPCState.Patrol)
        {
            currentState = NPCState.Suspicious;
            return;
        }

        // 4. Jika suara hilang saat Suspicious, kembali berpatroli
        if (currentState == NPCState.Suspicious && !sensor.CanHearPlayer)
        {
            currentState = NPCState.Patrol;
            GoToCurrentPatrolPoint();
            return;
        }

        // 5. Logika Durasi Pencarian (Search)
        if (currentState == NPCState.Search)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                hasLastKnownPosition = false;
                currentState = NPCState.Patrol;
                GoToCurrentPatrolPoint(); // PENTING: Mengembalikan target ke titik patroli terakhir
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
                {
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    GoToCurrentPatrolPoint();
                }
                break;

            case NPCState.Suspicious:
                agent.isStopped = true; // Berhenti berjalan
                
                // Menoleh ke arah sumber suara
                Vector3 dirToSound = (sensor.LastHeardPosition - transform.position).normalized;
                dirToSound.y = 0; 
                if (dirToSound != Vector3.zero)
                {
                    float dist = Vector3.Distance(transform.position, sensor.LastHeardPosition);
                    float turnSpeed = baseTurnSpeed / Mathf.Max(dist, 1f); // Semakin dekat, makin cepat menoleh
                    
                    Quaternion targetRot = Quaternion.LookRotation(dirToSound);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
                break;

            case NPCState.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                if (sensor.Player != null) 
                {
                    agent.SetDestination(sensor.Player.position);
                }
                break;

            case NPCState.Search:
                agent.speed = patrolSpeed;
                
                if (hasLastKnownPosition)
                {
                    agent.SetDestination(lastKnownPosition);
                    
                    // Jika sudah sampai di Last Known Position, maka "Melihat Sekitar"
                    if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
                    {
                        agent.isStopped = true;
                        // Rotasi bolak-balik menyapu pandangan
                        float sweepAngle = Mathf.Sin(Time.time * 3f) * searchTurnSpeed * Time.deltaTime;
                        transform.Rotate(0f, sweepAngle, 0f);
                    }
                    else
                    {
                        agent.isStopped = false;
                    }
                }
                break;
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints != null && patrolPoints.Length > 0) 
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    private void UpdateColor()
    {
        if (npcRenderer == null) return;
        
        switch (currentState)
        {
            case NPCState.Patrol: npcRenderer.material.color = patrolColor; break;
            case NPCState.Suspicious: npcRenderer.material.color = suspiciousColor; break;
            case NPCState.Chase: npcRenderer.material.color = chaseColor; break;
            case NPCState.Search: npcRenderer.material.color = searchColor; break;
        }
    }

    private void OnDrawGizmos()
    {
        if (hasLastKnownPosition)
        {
            // Menandai posisi terakhir pemain diingat
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }
    }
}