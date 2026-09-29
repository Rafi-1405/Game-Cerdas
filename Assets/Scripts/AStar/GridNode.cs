using UnityEngine;

/// <summary>A single cell in the 4-direction A* grid.</summary>
public sealed class GridNode
{
    public readonly int x;
    public readonly int y;
    public readonly Vector3 worldPosition;
    public readonly bool walkable;
    public int gCost = int.MaxValue;
    public int hCost;
    public GridNode parent;
    public GameObject visual;
    public int FCost => gCost == int.MaxValue ? int.MaxValue : gCost + hCost;

    public GridNode(int x, int y, Vector3 worldPosition, bool walkable)
    {
        this.x = x;
        this.y = y;
        this.worldPosition = worldPosition;
        this.walkable = walkable;
    }
}
