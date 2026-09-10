using UnityEngine;

public class NPCSensor : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float viewRadius = 8f;
    [Range(0f, 360f)][SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float eyeHeight = 1.2f;

    public bool CanSeePlayer { get; private set; }
    public Transform Player => player;

    private void Update() => DetectPlayer();

    private void DetectPlayer()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        
        if (player != null && CanSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, player.position + Vector3.up * 0.5f);
        }
    }
}