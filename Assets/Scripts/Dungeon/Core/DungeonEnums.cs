namespace DungeonGenerator.Core
{
    /// <summary>
    /// Types of grid cells in the procedural dungeon layout.
    /// </summary>
    public enum CellType
    {
        Empty = 0,
        RoomFloor = 1,
        CorridorFloor = 2,
        Wall = 3,
        Door = 4
    }

    /// <summary>
    /// Camera modes supported by the camera controller.
    /// </summary>
    public enum CameraViewMode
    {
        IsometricTopDown = 0,
        ThirdPersonFollow = 1,
        Overview = 2,
        FreeFlyEditor = 3
    }

    /// <summary>
    /// Types of spawnable entities in the dungeon.
    /// </summary>
    public enum EntityType
    {
        StartPoint = 0,
        GoalPoint = 1,
        Enemy = 2,
        Item = 3
    }
}
