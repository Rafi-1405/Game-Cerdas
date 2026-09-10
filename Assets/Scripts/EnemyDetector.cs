using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Alert,
        Suspicious
    }

    [Header("Target")]
    [SerializeField]
    private Transform player;

    [Header("AI Parameters")]
    [SerializeField]
    [Min(0f)]
    private float alertRadius = 4f;
    [SerializeField]
    [Min(0f)]
    private float suspiciousRadius = 8f;

    [Header("Visual")]
    [SerializeField]
    private Color idleColor = Color.blue;

    [SerializeField]
    private Color alertColor = Color.red;

    [SerializeField]
    private Color suspiciousColor = Color.yellow;

    [Header("Debug")]
    [SerializeField]
    private EnemyState currentState;

    [SerializeField]
    private float currentDistance;

    private Renderer enemyRenderer;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();

        currentState = EnemyState.Alert;
        SetState(EnemyState.Idle);
    }

    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
    {
        if (player == null)
            return;

        currentDistance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (currentDistance <= alertRadius)
        {
            SetState(EnemyState.Alert);
        }
        else if (currentDistance <= suspiciousRadius)
        {
            SetState(EnemyState.Suspicious);
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }

    void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log(
            "Enemy State → " + currentState
        );

        if (enemyRenderer == null)
            return;

        if (currentState == EnemyState.Alert)
        {
            enemyRenderer.material.color = alertColor;
        }
        else if (currentState == EnemyState.Suspicious)
        {
            enemyRenderer.material.color = suspiciousColor;
        }
        else
        {
            enemyRenderer.material.color = idleColor;
        }
    }

    void OnDrawGizmosSelected()
    {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(
        transform.position,
        alertRadius
    );

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(
        transform.position,
        suspiciousRadius
    );
}
}
