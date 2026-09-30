using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(0)]
public sealed class AStarPathfinder : MonoBehaviour
{
    public GridManager gridManager;
    public Transform startMarker;
    public Transform goalMarker;
    public Transform agent;
    [Header("Input")]
    public Camera inputCamera;
    public bool allowShiftClickGoal = true;
    [Header("Debug")]
    public bool showOpenClosed = true;
    [System.NonSerialized]
    public List<GridNode> currentPath = new List<GridNode>();

    private void Start() => FindPath();

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        bool moveGoal = allowShiftClickGoal && keyboard.leftShiftKey.isPressed;
        bool moveStart = keyboard.leftCtrlKey.isPressed;
        if (!moveGoal && !moveStart) return;
        if (gridManager == null || gridManager.grid == null) return;

        Camera clickCamera = inputCamera != null ? inputCamera : Camera.main;
        if (clickCamera == null) return;

        Ray ray = clickCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane gridPlane = new Plane(Vector3.up, gridManager.transform.position);
        if (!gridPlane.Raycast(ray, out float distance)) return;

        Vector3 clickedPosition = ray.GetPoint(distance);
        if (!gridManager.TryGetNodeFromWorldPosition(clickedPosition, out GridNode node) || !node.walkable) return;

        Transform marker = moveGoal ? goalMarker : startMarker;
        if (marker == null) return;

        float markerHeight = marker.position.y - gridManager.transform.position.y;
        marker.position = node.worldPosition + Vector3.up * markerHeight;
        FindPath();
    }

    [ContextMenu("Find Path")]
    public void FindPath()
    {
        currentPath.Clear();
        if (gridManager == null || startMarker == null || goalMarker == null || gridManager.grid == null)
        {
            Debug.LogWarning("AStarPathfinder: GridManager, StartMarker, GoalMarker, atau grid belum siap.", this);
            return;
        }
        ResetAgentToStart();
        gridManager.ResetSearchData();
        GridNode startNode = gridManager.NodeFromWorldPosition(startMarker.position);
        GridNode goalNode = gridManager.NodeFromWorldPosition(goalMarker.position);
        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("AStarPathfinder: Start atau Goal berada di luar grid.", this);
            return;
        }
        if (!startNode.walkable || !goalNode.walkable)
        {
            Debug.LogWarning("AStarPathfinder: Start atau Goal berada pada obstacle.", this);
            return;
        }

        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        startNode.gCost = 0;
        startNode.hCost = GetHeuristic(startNode, goalNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            GridNode currentNode = GetLowestFCostNode(openSet);
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            if (showOpenClosed) gridManager.SetNodeColor(currentNode, new Color(1f, 0.6f, 0.2f));
            if (currentNode == goalNode)
            {
                currentPath = ReconstructPath(startNode, goalNode);
                VisualizeFinalPath(startNode, goalNode);
                Debug.Log($"A*: path ditemukan, {currentPath.Count} node ({(currentPath.Count - 1) * 10} cost).", this);
                return;
            }
            foreach (GridNode neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;
                int tentativeGCost = currentNode.gCost + 10;
                if (tentativeGCost >= neighbor.gCost) continue;
                neighbor.parent = currentNode;
                neighbor.gCost = tentativeGCost;
                neighbor.hCost = GetHeuristic(neighbor, goalNode);
                if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                if (showOpenClosed) gridManager.SetNodeColor(neighbor, Color.yellow);
            }
        }
        Debug.LogWarning("A*: path tidak ditemukan; periksa obstacle dan konektivitas grid.", this);
    }

    private void ResetAgentToStart()
    {
        Transform controlledAgent = agent;
        if (controlledAgent == null && startMarker.TryGetComponent(out AgentPathFollower startFollower))
            controlledAgent = startFollower.transform;
        if (controlledAgent == null) return;

        Vector3 resetPosition = startMarker.position;
        if (gridManager.TryGetNodeFromWorldPosition(startMarker.position, out GridNode startNode))
            resetPosition = startNode.worldPosition;
        resetPosition.y = controlledAgent.position.y;
        controlledAgent.position = resetPosition;

        AgentPathFollower follower = controlledAgent.GetComponent<AgentPathFollower>();
        if (follower != null) follower.ResetPathProgress();
    }

    private GridNode GetLowestFCostNode(List<GridNode> openSet)
    {
        GridNode best = openSet[0];
        for (int i = 1; i < openSet.Count; i++)
        {
            GridNode candidate = openSet[i];
            if (candidate.FCost < best.FCost || (candidate.FCost == best.FCost && candidate.hCost < best.hCost))
                best = candidate;
        }
        return best;
    }

    private int GetHeuristic(GridNode a, GridNode b) => (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * 10;

    private List<GridNode> ReconstructPath(GridNode startNode, GridNode goalNode)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current = goalNode;
        while (current != null && current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }
        if (current != startNode) return new List<GridNode>();
        path.Add(startNode);
        path.Reverse();
        return path;
    }

    private void VisualizeFinalPath(GridNode startNode, GridNode goalNode)
    {
        foreach (GridNode node in currentPath) gridManager.SetNodeColor(node, Color.cyan);
        gridManager.SetNodeColor(startNode, Color.green);
        gridManager.SetNodeColor(goalNode, Color.red);
    }
}
