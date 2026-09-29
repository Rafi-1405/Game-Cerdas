using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-400)]
public sealed class GridManager : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)] public int width = 10;
    [Min(1)] public int height = 10;
    [Min(0.1f)] public float cellSize = 1f;
    [Header("Obstacle Detection")]
    public LayerMask obstacleMask;
    [Min(0.1f)] public float obstacleCheckHeight = 0.5f;
    [Range(0.1f, 0.49f)] public float obstacleCheckRadius = 0.4f;
    [Header("Visualization")]
    public bool showGrid = true;
    [Min(0.01f)] public float visualHeight = 0.05f;

    public GridNode[,] grid { get; private set; }
    private Transform visualParent;

    private void Start()
    {
        if (grid == null) CreateGrid();
    }

    public void CreateGrid()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cellSize = Mathf.Max(0.1f, cellSize);
        if (visualParent != null) Destroy(visualParent.gameObject);

        grid = new GridNode[width, height];
        visualParent = new GameObject("GridVisuals").transform;
        visualParent.SetParent(transform, false);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Vector3 worldPosition = GetWorldPosition(x, y);
            Vector3 center = worldPosition + Vector3.up * obstacleCheckHeight;
            Vector3 extents = new Vector3(cellSize * obstacleCheckRadius, 0.45f, cellSize * obstacleCheckRadius);
            bool blocked = obstacleMask.value != 0 && Physics.CheckBox(
                center, extents, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);
            GridNode node = new GridNode(x, y, worldPosition, !blocked);
            grid[x, y] = node;
            if (showGrid) CreateVisual(node);
        }
    }

    public Vector3 GetWorldPosition(int x, int y) => transform.position + new Vector3(x * cellSize, 0f, y * cellSize);

    public bool TryGetNodeFromWorldPosition(Vector3 worldPosition, out GridNode node)
    {
        node = null;
        if (grid == null || cellSize <= 0f) return false;
        Vector3 local = worldPosition - transform.position;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.z / cellSize);
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        node = grid[x, y];
        return true;
    }

    public GridNode NodeFromWorldPosition(Vector3 worldPosition) =>
        TryGetNodeFromWorldPosition(worldPosition, out GridNode node) ? node : null;

    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>(4);
        TryAddNeighbor(node.x + 1, node.y, neighbors);
        TryAddNeighbor(node.x - 1, node.y, neighbors);
        TryAddNeighbor(node.x, node.y + 1, neighbors);
        TryAddNeighbor(node.x, node.y - 1, neighbors);
        return neighbors;
    }

    private void TryAddNeighbor(int x, int y, List<GridNode> neighbors)
    {
        if (x >= 0 && x < width && y >= 0 && y < height) neighbors.Add(grid[x, y]);
    }

    public void ResetSearchData()
    {
        if (grid == null) return;
        foreach (GridNode node in grid)
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parent = null;
            if (showGrid) SetNodeColor(node, node.walkable ? Color.white : Color.black);
        }
    }

    public void SetNodeColor(GridNode node, Color color)
    {
        if (node == null || node.visual == null) return;
        Renderer renderer = node.visual.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = color;
    }

    private void CreateVisual(GridNode node)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = $"Node_{node.x}_{node.y}";
        tile.transform.SetParent(visualParent, true);
        tile.transform.position = node.worldPosition + Vector3.down * (visualHeight * 0.5f);
        tile.transform.localScale = new Vector3(cellSize * 0.9f, visualHeight, cellSize * 0.9f);
        Collider tileCollider = tile.GetComponent<Collider>();
        if (tileCollider != null) Destroy(tileCollider);
        node.visual = tile;
        SetNodeColor(node, node.walkable ? Color.white : Color.black);
    }
}
