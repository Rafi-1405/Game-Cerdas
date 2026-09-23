using UnityEngine;

public class SteeringSensor : MonoBehaviour
{
    [Header("Obstacle Sensor")]

    [SerializeField]
    private float sensorDistance = 3f;

    [SerializeField]
    private float sensorRadius = 0.5f;

    [SerializeField]
    private float sensorHeight = 0.5f;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Avoidance")]

    [SerializeField]
    private float forwardBias = 0.5f;

    private bool obstacleDetected;
    private RaycastHit lastHit;
    private Vector3 smoothedAvoidance; // Memori penghalus belokan

    public bool ObstacleDetected => obstacleDetected;

    public RaycastHit LastHit => lastHit;

    public Vector3 GetAvoidanceDirection(Vector3 movementDirection)
    {
        obstacleDetected = false;

        if (movementDirection.sqrMagnitude < 0.001f)
        {
            smoothedAvoidance = Vector3.Lerp(smoothedAvoidance, Vector3.zero, 10f * Time.deltaTime);
            return smoothedAvoidance;
        }

        movementDirection.Normalize();

        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 targetAvoidance = Vector3.zero;

        if (Physics.SphereCast(
            origin,
            sensorRadius,
            movementDirection,
            out lastHit,
            sensorDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            obstacleDetected = true;

            Vector3 avoidDirection = Vector3.ProjectOnPlane(lastHit.normal, Vector3.up);
            avoidDirection.y = 0f;

            if (avoidDirection.sqrMagnitude > 0.001f)
            {
                avoidDirection.Normalize();
            }

            // Kembalikan ke rumus murni modul (tidak boleh lebih dari 1.0 agar tidak menembus tembok)
            avoidDirection += movementDirection * forwardBias;
            
            targetAvoidance = avoidDirection.normalized;
        }

        // FITUR ANTI-GETAR / ANTI-SPIN
        // Transisi halus vektor agar NPC berbelok melengkung dengan mulus
        smoothedAvoidance = Vector3.Lerp(smoothedAvoidance, targetAvoidance, 12f * Time.deltaTime);
        
        return smoothedAvoidance;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        Vector3 direction =
            transform.forward;

        Gizmos.DrawWireSphere(
            origin,
            sensorRadius
        );

        Gizmos.DrawLine(
            origin,
            origin + direction * sensorDistance
        );

        Gizmos.DrawWireSphere(
            origin + direction * sensorDistance,
            sensorRadius
        );
    }
}