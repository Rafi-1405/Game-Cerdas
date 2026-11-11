using System;
using System.Collections.Generic;
using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Entities
{
    /// <summary>
    /// Placement data storing all resolved entity spawn coordinates.
    /// </summary>
    public class DungeonPlacementData
    {
        public Vector2Int StartPosition;
        public Vector2Int GoalPosition;
        public RectInt StartRoom;
        public RectInt GoalRoom;
        public int SafeZoneRadius;
        public List<Vector2Int> EnemyPositions = new List<Vector2Int>();
        public List<Vector2Int> ItemPositions = new List<Vector2Int>();
    }

    /// <summary>
    /// Algorithmic entity placer ensuring valid floors, safe zones, and unblocked paths.
    /// </summary>
    public static class DungeonEntityPlacer
    {
        #region CONFIG
        public static class PLACER_CONFIG
        {
            // Minimum distance between spawned enemies
            public const float MIN_ENEMY_SPACING = 2.5f;

            // Minimum distance between spawned items
            public const float MIN_ITEM_SPACING = 2.0f;

            // Maximum attempts to find random placement candidates
            public const int MAX_PLACEMENT_ATTEMPTS = 150;
        }
        #endregion

        /// <summary>
        /// Resolves placements for Start, Goal, Enemies, and Items.
        /// </summary>
        public static DungeonPlacementData ResolvePlacements(
            DungeonGrid grid,
            DungeonConfig config,
            System.Random rng)
        {
            DungeonPlacementData data = new DungeonPlacementData
            {
                SafeZoneRadius = config.safeZoneRadius
            };

            if (grid.Rooms.Count == 0)
            {
                Debug.LogError("Cannot place entities: No rooms exist in the generated grid.");
                return data;
            }

            // 1. Select Start Room and Goal Room (choose maximum separation)
            data.StartRoom = grid.Rooms[0];
            data.StartPosition = GetRoomCenter(data.StartRoom);

            RectInt bestGoalRoom = data.StartRoom;
            float maxDist = -1.0f;

            foreach (RectInt room in grid.Rooms)
            {
                Vector2Int center = GetRoomCenter(room);
                float dist = Vector2.Distance(center, data.StartPosition);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestGoalRoom = room;
                }
            }

            data.GoalRoom = bestGoalRoom;
            data.GoalPosition = GetRoomCenter(bestGoalRoom);

            // 2. Identify occupied cells to prevent overlaps
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>
            {
                data.StartPosition,
                data.GoalPosition
            };

            // 3. Place Enemies outside safe zone
            int enemiesPlaced = 0;
            int enemyAttempts = 0;

            while (enemiesPlaced < config.enemyCount && enemyAttempts < PLACER_CONFIG.MAX_PLACEMENT_ATTEMPTS)
            {
                enemyAttempts++;

                // Pick a random room excluding the start room
                int roomIdx = rng.Next(1, grid.Rooms.Count);
                RectInt room = grid.Rooms[roomIdx];

                int cellX = rng.Next(room.xMin + 1, room.xMax - 1);
                int cellY = rng.Next(room.yMin + 1, room.yMax - 1);
                Vector2Int candidate = new Vector2Int(cellX, cellY);

                if (!grid.IsWalkable(candidate) || occupiedCells.Contains(candidate))
                {
                    continue;
                }

                // Check safe zone distance from Start
                float distToStart = Vector2.Distance(candidate, data.StartPosition);
                if (distToStart <= config.safeZoneRadius)
                {
                    continue;
                }

                // Check distance to other enemies
                bool tooClose = false;
                foreach (Vector2Int existingEnemy in data.EnemyPositions)
                {
                    if (Vector2.Distance(candidate, existingEnemy) < PLACER_CONFIG.MIN_ENEMY_SPACING)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                data.EnemyPositions.Add(candidate);
                occupiedCells.Add(candidate);
                enemiesPlaced++;
            }

            // 4. Place Items on walkable floors
            int itemsPlaced = 0;
            int itemAttempts = 0;

            while (itemsPlaced < config.itemCount && itemAttempts < PLACER_CONFIG.MAX_PLACEMENT_ATTEMPTS)
            {
                itemAttempts++;

                int cellIdx = rng.Next(0, grid.WalkableCells.Count);
                Vector2Int candidate = grid.WalkableCells[cellIdx];

                if (occupiedCells.Contains(candidate))
                {
                    continue;
                }

                // Check distance to existing items
                bool tooClose = false;
                foreach (Vector2Int existingItem in data.ItemPositions)
                {
                    if (Vector2.Distance(candidate, existingItem) < PLACER_CONFIG.MIN_ITEM_SPACING)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                data.ItemPositions.Add(candidate);
                occupiedCells.Add(candidate);
                itemsPlaced++;
            }

            return data;
        }

        private static Vector2Int GetRoomCenter(RectInt room)
        {
            return new Vector2Int(room.xMin + (room.width / 2), room.yMin + (room.height / 2));
        }
    }
}
