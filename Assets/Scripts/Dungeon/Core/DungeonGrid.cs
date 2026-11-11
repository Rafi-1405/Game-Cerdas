using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator.Core
{
    /// <summary>
    /// Pure 2D grid data model representing dungeon layout and cells.
    /// </summary>
    public class DungeonGrid
    {
        #region CONFIG
        public static class GRID_CONFIG
        {
            // Neighbor offset vectors for 4-directional pathfinding
            public static readonly Vector2Int[] CARDINAL_DIRECTIONS = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // North
                new Vector2Int(1, 0),   // East
                new Vector2Int(0, -1),  // South
                new Vector2Int(-1, 0)   // West
            };

            // Neighbor offset vectors for 8-directional wall placement
            public static readonly Vector2Int[] ALL_NEIGHBOR_OFFSETS = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(1, 0),
                new Vector2Int(1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, 1)
            };
        }
        #endregion

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float TileSize { get; private set; }

        private readonly CellType[,] _cells;
        private readonly List<RectInt> _rooms = new List<RectInt>();
        private readonly HashSet<Vector2Int> _corridorCells = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _walkableCells = new List<Vector2Int>();

        public IReadOnlyList<RectInt> Rooms => _rooms;
        public IReadOnlyCollection<Vector2Int> CorridorCells => _corridorCells;
        public IReadOnlyList<Vector2Int> WalkableCells => _walkableCells;

        public DungeonGrid(int width, int height, float tileSize = 2.0f)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            TileSize = Mathf.Max(0.1f, tileSize);
            _cells = new CellType[Width, Height];
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool InBounds(Vector2Int pos)
        {
            return InBounds(pos.x, pos.y);
        }

        public CellType GetCell(int x, int y)
        {
            if (!InBounds(x, y)) return CellType.Empty;
            return _cells[x, y];
        }

        public CellType GetCell(Vector2Int pos)
        {
            return GetCell(pos.x, pos.y);
        }

        public void SetCell(int x, int y, CellType type)
        {
            if (!InBounds(x, y)) return;
            _cells[x, y] = type;
        }

        public void SetCell(Vector2Int pos, CellType type)
        {
            SetCell(pos.x, pos.y, type);
        }

        public bool IsWalkable(int x, int y)
        {
            CellType cell = GetCell(x, y);
            return cell == CellType.RoomFloor || cell == CellType.CorridorFloor || cell == CellType.Door;
        }

        public bool IsWalkable(Vector2Int pos)
        {
            return IsWalkable(pos.x, pos.y);
        }

        public void AddRoom(RectInt roomRect)
        {
            _rooms.Add(roomRect);

            for (int x = roomRect.xMin; x < roomRect.xMax; x++)
            {
                for (int y = roomRect.yMin; y < roomRect.yMax; y++)
                {
                    if (InBounds(x, y))
                    {
                        SetCell(x, y, CellType.RoomFloor);
                    }
                }
            }
        }

        public void CarveCorridorCell(int x, int y)
        {
            if (!InBounds(x, y)) return;

            CellType current = GetCell(x, y);
            if (current == CellType.Empty || current == CellType.Wall)
            {
                SetCell(x, y, CellType.CorridorFloor);
                _corridorCells.Add(new Vector2Int(x, y));
            }
        }

        public void BuildWalkableCache()
        {
            _walkableCells.Clear();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (IsWalkable(x, y))
                    {
                        _walkableCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        /// <summary>
        /// Surrounds all walkable floor cells with solid walls where neighboring cells are empty.
        /// </summary>
        public void SurroundWalkableWithWalls()
        {
            List<Vector2Int> wallsToPlace = new List<Vector2Int>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (IsWalkable(x, y)) continue;

                    bool hasWalkableNeighbor = false;
                    foreach (Vector2Int offset in GRID_CONFIG.ALL_NEIGHBOR_OFFSETS)
                    {
                        int nx = x + offset.x;
                        int ny = y + offset.y;
                        if (InBounds(nx, ny) && IsWalkable(nx, ny))
                        {
                            hasWalkableNeighbor = true;
                            break;
                        }
                    }

                    if (hasWalkableNeighbor)
                    {
                        wallsToPlace.Add(new Vector2Int(x, y));
                    }
                }
            }

            foreach (Vector2Int wallPos in wallsToPlace)
            {
                SetCell(wallPos.x, wallPos.y, CellType.Wall);
            }

            BuildWalkableCache();
        }

        /// <summary>
        /// Converts grid coordinates (X, Y) to 3D world position (X, 0, Z).
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y, float yHeight = 0.0f)
        {
            float worldX = (x - (Width * 0.5f) + 0.5f) * TileSize;
            float worldZ = (y - (Height * 0.5f) + 0.5f) * TileSize;
            return new Vector3(worldX, yHeight, worldZ);
        }

        public Vector3 GridToWorldPosition(Vector2Int pos, float yHeight = 0.0f)
        {
            return GridToWorldPosition(pos.x, pos.y, yHeight);
        }

        /// <summary>
        /// Converts 3D world position back to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt((worldPos.x / TileSize) + (Width * 0.5f) - 0.5f);
            int y = Mathf.RoundToInt((worldPos.z / TileSize) + (Height * 0.5f) - 0.5f);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Returns orthogonal walkable neighbors for BFS pathfinding.
        /// </summary>
        public List<Vector2Int> GetWalkableNeighbors(Vector2Int current)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>(4);
            foreach (Vector2Int dir in GRID_CONFIG.CARDINAL_DIRECTIONS)
            {
                Vector2Int next = current + dir;
                if (InBounds(next) && IsWalkable(next))
                {
                    neighbors.Add(next);
                }
            }
            return neighbors;
        }
    }
}
