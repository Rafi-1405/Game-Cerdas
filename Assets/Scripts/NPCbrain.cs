using UnityEngine;
using UnityEngine.AI;

public class NPCBrain : MonoBehaviour
{
    public enum NPCState { Patrol, Chase, Search }

    [SerializeField] private NPCSensor sensor;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointTolerance = 0.7f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchTurnSpeed = 60f;

    [Header("Colors Indicator")]
    [SerializeField] private Color patrolColor = Color.darkBlue;
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
            currentState = NPCState.Chase;
            return;
        }

        if (currentState == NPCState.Chase && hasLastKnownPosition)
        {
            searchTimer = searchDuration;
            currentState = NPCState.Search;
            return;
        }

        if (currentState == NPCState.Search)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                hasLastKnownPosition = false;
                currentState = NPCState.Patrol;
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case NPCState.Patrol:
                agent.speed = patrolSpeed;
                if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
                {
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    GoToCurrentPatrolPoint();
                }
                break;

            case NPCState.Chase:
                agent.speed = chaseSpeed;
                if (sensor.Player != null) agent.SetDestination(sensor.Player.position);
                break;

            case NPCState.Search:
                agent.speed = patrolSpeed;
                if (hasLastKnownPosition) agent.SetDestination(lastKnownPosition);
                break;
        }
    }

    private void GoToCurrentPatrolPoint()
    {
        if (patrolPoints.Length > 0) agent.SetDestination(patrolPoints[patrolIndex].position);
    }
}