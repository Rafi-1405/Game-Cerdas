using UnityEngine;

namespace Praktikum5.FSM
{
    public class EnemyPerception : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform player;

        [Header("Vision Parameters")]
        [SerializeField] private float visionRange = 10f;
        [SerializeField, Range(0f, 360f)] private float visionAngle = 90f;
        [SerializeField] private float eyeHeight = 1f;

        [Header("Layer Mask")]
        [Tooltip("Layer rintangan yang memblokir pandangan (misal: Obstacle / Wall).")]
        [SerializeField] private LayerMask obstacleMask;

        public bool CanSeePlayer { get; private set; }
        public Transform PlayerTarget => player;

        public float DistanceToPlayer
        {
            get
            {
                if (player == null) return Mathf.Infinity;
                return Vector3.Distance(transform.position, player.position);
            }
        }

        private void Awake()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            // Jika obstacleMask belum disetel di inspector, sertakan Layer Obstacle dan Wall
            if (obstacleMask == 0)
            {
                obstacleMask = LayerMask.GetMask("Obstacle", "Wall");
                if (obstacleMask == 0)
                {
                    obstacleMask = LayerMask.GetMask("Default");
                }
            }
        }

        private void Update()
        {
            CanSeePlayer = CheckPlayerVisibility();
        }

        public bool CheckPlayerVisibility()
        {
            if (player == null) return false;

            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = player.position + Vector3.up * 0.8f;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;

            // 1. Periksa jarak
            if (distance > visionRange)
                return false;

            // 2. Periksa sudut pandang (Field of View)
            Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);
            Vector3 forwardDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);

            if (horizontalDirection.sqrMagnitude > 0.001f && forwardDirection.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(forwardDirection, horizontalDirection);
                if (angle > visionAngle * 0.5f)
                    return false;
            }

            // 3. Periksa halangan fisik (Line of Sight Raycast)
            bool blocked = Physics.Raycast(origin, direction.normalized, distance, obstacleMask);
            if (blocked)
                return false;

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            // Lingkaran jangkauan pandang
            Gizmos.color = CanSeePlayer ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);

            // Garis batas sudut pandang kiri & kanan
            Vector3 leftDirection = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightDirection = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, leftDirection * visionRange);
            Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, rightDirection * visionRange);

            // Garis ray ke Player
            if (player != null)
            {
                Gizmos.color = CanSeePlayer ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, player.position + Vector3.up * 0.8f);
            }
        }
    }
}
