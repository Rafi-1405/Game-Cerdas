using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCSensor : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    private PlayerController playerController;

    [Header("Visual Sensor (Mata)")]
    [SerializeField] private float viewRadius = 8f;
    [Range(0f, 360f)][SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float eyeHeight = 1.2f;

    [Header("Audio Sensor (Telinga)")]
    [SerializeField] private float walkHearRadius = 5f;
    [SerializeField] private float runHearRadius = 10f;

    // Properti publik untuk dibaca oleh Brain
    public bool CanSeePlayer { get; private set; }
    public bool CanHearPlayer { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public Transform Player => player;

    private void Start()
    {
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        DetectVisual();
        DetectAudio();
    }

    private void DetectVisual()
    {
        CanSeePlayer = false;
        if (player == null) return;

        Vector3 directionToPlayer = player.position - transform.position;
        if (directionToPlayer.magnitude > viewRadius) return;

        if (Vector3.Angle(transform.forward, directionToPlayer.normalized) > viewAngle / 2f) return;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = player.position + Vector3.up * 0.5f;

        if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, (targetPos - eyePos).magnitude, obstacleMask))
            return;

        CanSeePlayer = true;
    }

    private void DetectAudio()
    {
        CanHearPlayer = false;
        if (player == null || playerController == null) return;

        PlayerMovementState state = playerController.CurrentState;
        
        // Jika pemain diam atau mengendap-endap, tidak ada suara yang terdeteksi
        if (state == PlayerMovementState.Idle || state == PlayerMovementState.Sneak) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Deteksi berdasarkan state pergerakan
        if (state == PlayerMovementState.Walk && distance <= walkHearRadius)
        {
            CanHearPlayer = true;
            LastHeardPosition = player.position;
        }
        else if (state == PlayerMovementState.Run && distance <= runHearRadius)
        {
            CanHearPlayer = true;
            LastHeardPosition = player.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Gizmos untuk Audio Sensor (Telinga) - Lingkaran 360 derajat
        Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan transparan untuk batas Walk
        Gizmos.DrawWireSphere(transform.position, walkHearRadius);
        
        Gizmos.color = new Color(0, 0, 1, 0.3f); // Biru transparan untuk batas Run
        Gizmos.DrawWireSphere(transform.position, runHearRadius);

        // 2. Gizmos untuk Visual Sensor (Mata) - Bentuk Kipas
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

#if UNITY_EDITOR
        // Menggambar area kipas (Warna kuning transparan)
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.15f);
        
        // Kalkulasi batas kiri kipas
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        // Menggambar isi kipas
        Handles.DrawSolidArc(eyePos, Vector3.up, leftBoundary, viewAngle, viewRadius);
        
        // Menggambar garis batas kipas agar terlihat jelas
        Handles.color = Color.yellow;
        Handles.DrawWireArc(eyePos, Vector3.up, leftBoundary, viewAngle, viewRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePos, eyePos + leftBoundary * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + rightBoundary * viewRadius);
#endif

        // Garis laser penanda merah JIKA pemain terlihat (Ter-lock)
        if (player != null && CanSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePos, player.position + Vector3.up * 0.5f);
        }
    }
}