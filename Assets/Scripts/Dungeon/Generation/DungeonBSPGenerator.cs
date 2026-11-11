using System;
using System.Collections.Generic;
using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Generation
{
    /// <summary>
    /// Generates grid-based dungeon layouts using Binary Space Partitioning (BSP).
    /// </summary>
    public class DungeonBSPGenerator
    {
        #region CONFIG
        public static class GENERATOR_CONFIG
        {
            // Edge margin for root partition
            public const int ROOT_BOUNDARY_MARGIN = 1;

            // Maximum corridor width clamping
            public const int MAX_CORRIDOR_WIDTH = 3;
        }
        #endregion

        public DungeonConfig Config { get; private set; }
        public BSPNode RootNode { get; private set; }
        public DungeonGrid Grid { get; private set; }

        public DungeonBSPGenerator(DungeonConfig config)
        {
            Config = config ?? new DungeonConfig();
            Config.Validate();
        }

        /// <summary>
        /// Executes full BSP dungeon generation pipeline.
        /// </summary>
        public DungeonGrid Generate(int? seedOverride = null)
        {
            int activeSeed = seedOverride ?? (Config.useRandomSeed ? UnityEngine.Random.Range(10000, 999999) : Config.seed);
            Config.seed = activeSeed;
            System.Random rng = new System.Random(activeSeed);

            // 1. Initialize grid
            Grid = new DungeonGrid(Config.mapWidth, Config.mapHeight, Config.tileSize);

            // 2. Initialize and split root BSP partition
            RectInt rootBounds = new RectInt(
                GENERATOR_CONFIG.ROOT_BOUNDARY_MARGIN,
                GENERATOR_CONFIG.ROOT_BOUNDARY_MARGIN,
                Config.mapWidth - (GENERATOR_CONFIG.ROOT_BOUNDARY_MARGIN * 2),
                Config.mapHeight - (GENERATOR_CONFIG.ROOT_BOUNDARY_MARGIN * 2)
            );

            RootNode = new BSPNode(rootBounds);
            RootNode.Split(rng, Config, 0);

            // 3. Create rooms in all leaf partitions
            List<BSPNode> leaves = new List<BSPNode>();
            RootNode.GetLeaves(leaves);

            foreach (BSPNode leaf in leaves)
            {
                leaf.CreateRoom(rng, Config, Grid);
            }

            // 4. Carve connecting corridors between sibling nodes
            ConnectTreePartitions(RootNode, rng);

            // 5. Build wall perimeter around all walkable cells
            Grid.SurroundWalkableWithWalls();

            return Grid;
        }

        /// <summary>
        /// Recursively connects rooms between left and right partition branches.
        /// </summary>
        private void ConnectTreePartitions(BSPNode node, System.Random rng)
        {
            if (node == null || node.IsLeaf) return;

            // Recurse first to children
            ConnectTreePartitions(node.LeftChild, rng);
            ConnectTreePartitions(node.RightChild, rng);

            // Connect a representative room from left and right subtree
            RectInt? leftRoom = node.LeftChild?.GetRepresentativeRoom(rng);
            RectInt? rightRoom = node.RightChild?.GetRepresentativeRoom(rng);

            if (leftRoom.HasValue && rightRoom.HasValue)
            {
                Vector2Int startPoint = GetRoomCenter(leftRoom.Value);
                Vector2Int endPoint = GetRoomCenter(rightRoom.Value);
                CarveLShapedCorridor(startPoint, endPoint, Config.corridorWidth, rng);
            }
        }

        /// <summary>
        /// Carves an L-shaped corridor connecting two points.
        /// </summary>
        private void CarveLShapedCorridor(Vector2Int start, Vector2Int end, int width, System.Random rng)
        {
            int safeWidth = Mathf.Clamp(width, 1, GENERATOR_CONFIG.MAX_CORRIDOR_WIDTH);
            bool horizontalFirst = rng.NextDouble() >= 0.5;

            Vector2Int corner = horizontalFirst ? new Vector2Int(end.x, start.y) : new Vector2Int(start.x, end.y);

            // Carve first segment
            CarveLinearSegment(start, corner, safeWidth);

            // Carve second segment
            CarveLinearSegment(corner, end, safeWidth);
        }

        /// <summary>
        /// Carves a straight horizontal or vertical corridor line.
        /// </summary>
        private void CarveLinearSegment(Vector2Int from, Vector2Int to, int width)
        {
            int minX = Mathf.Min(from.x, to.x);
            int maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y);
            int maxY = Mathf.Max(from.y, to.y);

            int halfWidth = width / 2;

            if (minX == maxX) // Vertical segment
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int w = -halfWidth; w <= halfWidth; w++)
                    {
                        Grid.CarveCorridorCell(minX + w, y);
                    }
                }
            }
            else // Horizontal segment
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int w = -halfWidth; w <= halfWidth; w++)
                    {
                        Grid.CarveCorridorCell(x, minY + w);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the center grid coordinate of a room rectangle.
        /// </summary>
        public static Vector2Int GetRoomCenter(RectInt room)
        {
            return new Vector2Int(room.xMin + (room.width / 2), room.yMin + (room.height / 2));
        }
    }
}
