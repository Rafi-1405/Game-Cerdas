using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class AgentPathFollower : MonoBehaviour
{
    public AStarPathfinder pathfinder;
    [Min(0.1f)] public float moveSpeed = 2.5f;
    [Min(0.01f)] public float waypointTolerance = 0.08f;
    public bool followPath = true;
    private int currentWaypointIndex;

    private void Update()
    {
        if (!followPath || pathfinder == null || pathfinder.currentPath == null || pathfinder.currentPath.Count == 0)
            return;
        if (currentWaypointIndex >= pathfinder.currentPath.Count) return;
        Vector3 destination = pathfinder.currentPath[currentWaypointIndex].worldPosition + Vector3.up * 0.35f;
        Vector3 offset = destination - transform.position;
        if (offset.sqrMagnitude <= waypointTolerance * waypointTolerance)
        {
            currentWaypointIndex++;
            return;
        }
        Vector3 direction = offset.normalized;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetPath(AStarPathfinder newPathfinder)
    {
        pathfinder = newPathfinder;
        currentWaypointIndex = 0;
    }
}
