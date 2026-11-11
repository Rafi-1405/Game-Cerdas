using System;
using UnityEngine;

namespace DungeonGenerator.Core
{
    /// <summary>
    /// Central configuration data for procedural dungeon generation.
    /// </summary>
    [Serializable]
    public class DungeonConfig
    {
        #region CONFIG
        public static class DEFAULT_CONFIG
        {
            // Grid and world dimensions
            public const int DEFAULT_MAP_WIDTH = 48;
            public const int DEFAULT_MAP_HEIGHT = 48;
            public const float DEFAULT_TILE_SIZE = 2.0f;
            public const float DEFAULT_WALL_HEIGHT = 2.4f;

            // BSP partitioning thresholds
            public const int DEFAULT_MIN_PARTITION_SIZE = 10;
            public const int DEFAULT_MAX_SPLIT_DEPTH = 4;
            public const float DEFAULT_MIN_SPLIT_RATIO = 0.40f;
            public const float DEFAULT_MAX_SPLIT_RATIO = 0.60f;

            // Room constraints
            public const int DEFAULT_MIN_ROOM_WIDTH = 6;
            public const int DEFAULT_MIN_ROOM_HEIGHT = 6;
            public const int DEFAULT_MAX_ROOM_WIDTH = 14;
            public const int DEFAULT_MAX_ROOM_HEIGHT = 14;
            public const int DEFAULT_ROOM_PADDING = 1;

            // Corridor constraints
            public const int DEFAULT_CORRIDOR_WIDTH = 1;

            // Entity counts and constraints
            public const int DEFAULT_SAFE_ZONE_RADIUS = 4;
            public const int DEFAULT_ENEMY_COUNT = 6;
            public const int DEFAULT_ITEM_COUNT = 8;
            public const int DEFAULT_MAX_VALIDATION_RETRIES = 10;

            // Random seed default
            public const int DEFAULT_SEED = 12345;
            public const bool DEFAULT_USE_RANDOM_SEED = true;
        }
        #endregion

        [Header("Seed Settings")]
        [Tooltip("When true, generates a new random seed every run.")]
        public bool useRandomSeed = DEFAULT_CONFIG.DEFAULT_USE_RANDOM_SEED;

        [Tooltip("Active seed used for deterministic generation.")]
        public int seed = DEFAULT_CONFIG.DEFAULT_SEED;

        [Header("Map Dimensions (Grid Cells)")]
        [Range(24, 96)]
        public int mapWidth = DEFAULT_CONFIG.DEFAULT_MAP_WIDTH;

        [Range(24, 96)]
        public int mapHeight = DEFAULT_CONFIG.DEFAULT_MAP_HEIGHT;

        [Header("BSP Partitioning")]
        [Range(8, 20)]
        public int minPartitionSize = DEFAULT_CONFIG.DEFAULT_MIN_PARTITION_SIZE;

        [Range(2, 6)]
        public int maxSplitDepth = DEFAULT_CONFIG.DEFAULT_MAX_SPLIT_DEPTH;

        [Range(0.2f, 0.5f)]
        public float minSplitRatio = DEFAULT_CONFIG.DEFAULT_MIN_SPLIT_RATIO;

        [Range(0.5f, 0.8f)]
        public float maxSplitRatio = DEFAULT_CONFIG.DEFAULT_MAX_SPLIT_RATIO;

        [Header("Room Parameters")]
        [Range(4, 12)]
        public int minRoomWidth = DEFAULT_CONFIG.DEFAULT_MIN_ROOM_WIDTH;

        [Range(4, 12)]
        public int minRoomHeight = DEFAULT_CONFIG.DEFAULT_MIN_ROOM_HEIGHT;

        [Range(8, 24)]
        public int maxRoomWidth = DEFAULT_CONFIG.DEFAULT_MAX_ROOM_WIDTH;

        [Range(8, 24)]
        public int maxRoomHeight = DEFAULT_CONFIG.DEFAULT_MAX_ROOM_HEIGHT;

        [Range(1, 3)]
        public int roomPadding = DEFAULT_CONFIG.DEFAULT_ROOM_PADDING;

        [Header("Corridor Parameters")]
        [Range(1, 3)]
        public int corridorWidth = DEFAULT_CONFIG.DEFAULT_CORRIDOR_WIDTH;

        [Header("Entity Placement")]
        [Range(2, 8)]
        public int safeZoneRadius = DEFAULT_CONFIG.DEFAULT_SAFE_ZONE_RADIUS;

        [Range(0, 20)]
        public int enemyCount = DEFAULT_CONFIG.DEFAULT_ENEMY_COUNT;

        [Range(1, 25)]
        public int itemCount = DEFAULT_CONFIG.DEFAULT_ITEM_COUNT;

        [Header("Validation Rules")]
        [Range(1, 20)]
        public int maxValidationRetries = DEFAULT_CONFIG.DEFAULT_MAX_VALIDATION_RETRIES;

        [Header("3D World Scale")]
        public float tileSize = DEFAULT_CONFIG.DEFAULT_TILE_SIZE;
        public float wallHeight = DEFAULT_CONFIG.DEFAULT_WALL_HEIGHT;

        /// <summary>
        /// Creates a deep copy of the configuration instance.
        /// </summary>
        public DungeonConfig Clone()
        {
            return (DungeonConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Validates and clamps configuration values to safe boundaries.
        /// </summary>
        public void Validate()
        {
            mapWidth = Mathf.Max(24, mapWidth);
            mapHeight = Mathf.Max(24, mapHeight);
            minPartitionSize = Mathf.Clamp(minPartitionSize, 8, Mathf.Min(mapWidth, mapHeight) / 2);
            minRoomWidth = Mathf.Clamp(minRoomWidth, 4, minPartitionSize - (roomPadding * 2));
            minRoomHeight = Mathf.Clamp(minRoomHeight, 4, minPartitionSize - (roomPadding * 2));
            maxRoomWidth = Mathf.Max(minRoomWidth, maxRoomWidth);
            maxRoomHeight = Mathf.Max(minRoomHeight, maxRoomHeight);
            corridorWidth = Mathf.Clamp(corridorWidth, 1, 3);
            safeZoneRadius = Mathf.Clamp(safeZoneRadius, 2, 8);
            enemyCount = Mathf.Max(0, enemyCount);
            itemCount = Mathf.Max(0, itemCount);
            maxValidationRetries = Mathf.Clamp(maxValidationRetries, 1, 30);
            tileSize = Mathf.Max(0.5f, tileSize);
            wallHeight = Mathf.Max(1.0f, wallHeight);
        }
    }
}
