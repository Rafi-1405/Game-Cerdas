using System.Collections.Generic;
using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using DungeonGenerator.Validation;
using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Visual debug gizmo renderer displaying rooms, BFS paths, and safe zones.
    /// </summary>
    public class DungeonGizmos : MonoBehaviour
    {
        #region CONFIG
        public static class GIZMO_CONFIG
        {
            // Debug colors
            public static readonly Color ROOM_BOX_COLOR = new Color(0.2f, 0.6f, 1.0f, 0.35f);
            public static readonly Color ROOM_WIRE_COLOR = new Color(0.2f, 0.6f, 1.0f, 0.9f);
            public static readonly Color SAFE_ZONE_COLOR = new Color(0.1f, 0.9f, 0.4f, 0.35f);
            public static readonly Color BFS_PATH_COLOR = new Color(0.0f, 1.0f, 1.0f, 1.0f);
            public static readonly Color START_GIZMO_COLOR = new Color(0.1f, 1.0f, 0.4f, 1.0f);
            public static readonly Color GOAL_GIZMO_COLOR = new Color(1.0f, 0.8f, 0.0f, 1.0f);
            public static readonly Color ENEMY_GIZMO_COLOR = new Color(1.0f, 0.2f, 0.2f, 0.8f);
            public static readonly Color ITEM_GIZMO_COLOR = new Color(0.2f, 0.6f, 1.0f, 0.8f);

            // Path line thickness and height
            public const float PATH_LINE_HEIGHT = 0.4f;
            public const float SPHERE_RADIUS = 0.5f;
        }
        #endregion

        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private bool _showRooms = true;
        [SerializeField] private bool _showSafeZone = true;
        [SerializeField] private bool _showBFSPath = true;
        [SerializeField] private bool _showEntityMarkers = true;

        private DungeonGrid _grid;
        private DungeonPlacementData _placements;
        private ValidationReport _report;

        public void SetDebugData(DungeonGrid grid, DungeonPlacementData placements, ValidationReport report)
        {
            _grid = grid;
            _placements = placements;
            _report = report;
        }

        public void ToggleGizmos(bool show)
        {
            _showGizmos = show;
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _grid == null) return;

            // 1. Draw Rooms
            if (_showRooms)
            {
                foreach (RectInt room in _grid.Rooms)
                {
                    Vector3 center = _grid.GridToWorldPosition(
                        new Vector2Int(room.xMin + (room.width / 2), room.yMin + (room.height / 2)),
                        0.5f
                    );
                    Vector3 size = new Vector3(room.width * _grid.TileSize, 1.0f, room.height * _grid.TileSize);

                    Gizmos.color = GIZMO_CONFIG.ROOM_BOX_COLOR;
                    Gizmos.DrawCube(center, size);

                    Gizmos.color = GIZMO_CONFIG.ROOM_WIRE_COLOR;
                    Gizmos.DrawWireCube(center, size);
                }
            }

            if (_placements == null) return;

            // 2. Draw Safe Zone around Start
            if (_showSafeZone)
            {
                Vector3 startPos = _grid.GridToWorldPosition(_placements.StartPosition, 0.2f);
                float radius = _placements.SafeZoneRadius * _grid.TileSize;

                Gizmos.color = GIZMO_CONFIG.SAFE_ZONE_COLOR;
                Gizmos.DrawWireSphere(startPos, radius);
            }

            // 3. Draw Entity markers
            if (_showEntityMarkers)
            {
                // Start
                Gizmos.color = GIZMO_CONFIG.START_GIZMO_COLOR;
                Gizmos.DrawWireSphere(_grid.GridToWorldPosition(_placements.StartPosition, 0.5f), GIZMO_CONFIG.SPHERE_RADIUS);

                // Goal
                Gizmos.color = GIZMO_CONFIG.GOAL_GIZMO_COLOR;
                Gizmos.DrawWireSphere(_grid.GridToWorldPosition(_placements.GoalPosition, 0.5f), GIZMO_CONFIG.SPHERE_RADIUS * 1.5f);

                // Enemies
                Gizmos.color = GIZMO_CONFIG.ENEMY_GIZMO_COLOR;
                foreach (Vector2Int enemy in _placements.EnemyPositions)
                {
                    Gizmos.DrawWireSphere(_grid.GridToWorldPosition(enemy, 0.5f), GIZMO_CONFIG.SPHERE_RADIUS * 0.8f);
                }

                // Items
                Gizmos.color = GIZMO_CONFIG.ITEM_GIZMO_COLOR;
                foreach (Vector2Int item in _placements.ItemPositions)
                {
                    Gizmos.DrawWireSphere(_grid.GridToWorldPosition(item, 0.5f), GIZMO_CONFIG.SPHERE_RADIUS * 0.6f);
                }
            }

            // 4. Draw Shortest BFS Path from Start to Goal
            if (_showBFSPath && _report != null && _report.ShortestPath != null && _report.ShortestPath.Count > 1)
            {
                Gizmos.color = GIZMO_CONFIG.BFS_PATH_COLOR;
                for (int i = 0; i < _report.ShortestPath.Count - 1; i++)
                {
                    Vector3 p1 = _grid.GridToWorldPosition(_report.ShortestPath[i], GIZMO_CONFIG.PATH_LINE_HEIGHT);
                    Vector3 p2 = _grid.GridToWorldPosition(_report.ShortestPath[i + 1], GIZMO_CONFIG.PATH_LINE_HEIGHT);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
    }
}
