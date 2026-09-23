using UnityEngine;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState { Patrol, Suspicious, Chase, Search }

    [Header("Components")]
    [SerializeField] private NPCSensor sensor;
    [SerializeField] private SteeringAgent steeringAgent;
    [SerializeField] private Renderer npcRenderer;

    [Header("Waypoints (Tanpa NavMesh)")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPauseTime = 2f;
    private float patrolWaitTimer;
    private int patrolIndex = 0;

    [Header("Settings")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float baseTurnSpeed = 10f;
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchTurnSpeed = 60f;
    [SerializeField] private float arrivalTolerance = 1.5f;

    [Header("UI Indicators & Visuals")]
    [SerializeField] private GameObject exclamationMark;
    [SerializeField] private GameObject questionMark;
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color searchColor = Color.blue;

    [Header("Shared Alert (Komunikasi Guard)")]
    [SerializeField] private float shoutRadius = 6f;
    [SerializeField] private LayerMask guardLayerMask;

    [Header("Current State (Debug)")]
    [SerializeField] private NPCState currentState;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition;
    private float searchTimer;
    private float searchTravelTimer;
    private GameObject searchMarker;

    private void Awake()
    {
        searchMarker = new GameObject("SearchMarker_" + gameObject.name);
    }

    private void Start()
    {
        currentState = NPCState.Patrol;
        patrolWaitTimer = patrolPauseTime;
    }

    private void Update()
    {
        UpdateMemory();
        MakeDecision();
        ExecuteCurrentState();
        UpdateVisuals();
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
        if (sensor.CanSeePlayer)
        {
            if (currentState != NPCState.Chase) AlertNearbyGuards();
            currentState = NPCState.Chase;
            return;
        }

        if (currentState == NPCState.Chase)
        {
            searchTimer = searchDuration;
            searchTravelTimer = 5f; // Batas waktu maksimal mencoba jalan ke titik (5 detik)
            currentState = NPCState.Search;
            return;
        }

        if (sensor.CanHearPlayer && currentState == NPCState.Patrol)
        {
            currentState = NPCState.Suspicious;
            return;
        }

        if (currentState == NPCState.Suspicious && !sensor.CanHearPlayer)
        {
            currentState = NPCState.Patrol;
            return;
        }

        if (currentState == NPCState.Search)
        {
            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTarget = new Vector3(lastKnownPosition.x, 0, lastKnownPosition.z);
            
            bool arrived = Vector3.Distance(flatPos, flatTarget) <= 0.6f;
            
            // Kurangi waktu batas jalan
            if (!arrived) searchTravelTimer -= Time.deltaTime;
            
            bool gaveUp = searchTravelTimer <= 0f;

            // Jika sudah sampai ATAU sudah nyerah karena nyangkut tembok
            if (arrived || gaveUp)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f)
                {
                    hasLastKnownPosition = false;
                    currentState = NPCState.Patrol;
                }
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
                    Vector3 flatWaypoint = new Vector3(patrolPoints[patrolIndex].position.x, 0, patrolPoints[patrolIndex].position.z);
                    float dist = Vector3.Distance(flatPos, flatWaypoint);

                    // Saat ke waypoint, kita ingin dia mendarat persis (StopRadius kecil)
                    steeringAgent.StopRadius = 0.5f;

                    if (dist <= arrivalTolerance && steeringAgent.Velocity.sqrMagnitude < 0.2f)
                    {
                        steeringAgent.IsStopped = true;
                        
                        // Menoleh ke kiri dan ke kanan selama waktu stop
                        float sweepAngle = Mathf.Sin(Time.time * 3f) * searchTurnSpeed * Time.deltaTime;
                        transform.Rotate(0f, sweepAngle, 0f);

                        patrolWaitTimer -= Time.deltaTime;
                        if (patrolWaitTimer <= 0f)
                        {
                            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                            patrolWaitTimer = patrolPauseTime;
                        }
                    }
                    else
                    {
                        steeringAgent.IsStopped = false;
                        steeringAgent.SetTarget(patrolPoints[patrolIndex], patrolSpeed);
                    }
                }
                else
                {
                    steeringAgent.StopRadius = 0.5f;
                    steeringAgent.IsStopped = false;
                    steeringAgent.ClearTarget();
                }
                break;

            case NPCState.Suspicious:
                steeringAgent.IsStopped = true; 
                Vector3 dirToSound = (sensor.LastHeardPosition - transform.position).normalized;
                dirToSound.y = 0; 
                if (dirToSound != Vector3.zero)
                {
                    float dist = Vector3.Distance(transform.position, sensor.LastHeardPosition);
                    float turnSpeed = baseTurnSpeed / Mathf.Max(dist, 1f);
                    Quaternion targetRot = Quaternion.LookRotation(dirToSound);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
                break;

            case NPCState.Chase:
                steeringAgent.IsStopped = false;
                // Saat mengejar Player, hentikan NPC sebelum menabrak badan Player
                steeringAgent.StopRadius = 1.5f; 
                steeringAgent.SetTarget(sensor.Player, chaseSpeed);
                break;

            case NPCState.Search:
                if (hasLastKnownPosition)
                {
                    searchMarker.transform.position = lastKnownPosition;
                    
                    // Kita ingin dia mendarat TEPAT di titik terakhir Player terlihat
                    steeringAgent.StopRadius = 0.5f;

                    float dist = Vector3.Distance(transform.position, lastKnownPosition);
                    
                    Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
                    Vector3 flatTarget = new Vector3(lastKnownPosition.x, 0, lastKnownPosition.z);
                    
                    bool arrived = Vector3.Distance(flatPos, flatTarget) <= 0.6f;
                    bool gaveUp = searchTravelTimer <= 0f;

                    // Baru menoleh JIKA sudah berdiri tepat di atas titik tersebut ATAU menyerah karena nyangkut
                    if (arrived || gaveUp)
                    {
                        steeringAgent.IsStopped = true; 
                        float sweepAngle = Mathf.Sin(Time.time * 3f) * searchTurnSpeed * Time.deltaTime;
                        transform.Rotate(0f, sweepAngle, 0f);
                    }
                    else
                    {
                        steeringAgent.IsStopped = false;
                        steeringAgent.SetTarget(searchMarker.transform, patrolSpeed);
                    }
                }
                break;
        }
    }

    // Fungsi komunikasi antar penjaga (Shared Alert)
    public void ReceiveAlert(Vector3 targetPos)
    {
        if (currentState == NPCState.Patrol || currentState == NPCState.Suspicious)
        {
            lastKnownPosition = targetPos;
            hasLastKnownPosition = true;
            searchTimer = searchDuration;
            currentState = NPCState.Search; // Guard yang dipanggil akan ikut mencari ke lokasi
        }
    }

    private void AlertNearbyGuards()
    {
        Collider[] allies = Physics.OverlapSphere(transform.position, shoutRadius, guardLayerMask);
        foreach (Collider ally in allies)
        {
            NPCBrain allyBrain = ally.GetComponent<NPCBrain>();
            if (allyBrain != null && allyBrain != this)
            {
                allyBrain.ReceiveAlert(sensor.Player.position);
            }
        }
    }

    private void UpdateVisuals()
    {
        // 1. Update Warna
        if (npcRenderer != null)
        {
            switch (currentState)
            {
                case NPCState.Patrol: npcRenderer.material.color = patrolColor; break;
                case NPCState.Suspicious: npcRenderer.material.color = suspiciousColor; break;
                case NPCState.Chase: npcRenderer.material.color = chaseColor; break;
                case NPCState.Search: npcRenderer.material.color = searchColor; break;
            }
        }

        // 2. Update Indikator UI (! dan ?)
        if (exclamationMark != null) 
            exclamationMark.SetActive(currentState == NPCState.Chase);
        
        if (questionMark != null) 
            questionMark.SetActive(currentState == NPCState.Suspicious || currentState == NPCState.Search);
    }

    private void OnDrawGizmos()
    {
        if (hasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }

        // Gambar radius teriakan peringatan
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange transparan
        Gizmos.DrawWireSphere(transform.position, shoutRadius);
    }
}