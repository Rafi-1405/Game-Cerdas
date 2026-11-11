using System;
using System.Collections.Generic;
using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Generation
{
    /// <summary>
    /// Binary Space Partitioning (BSP) tree node for room and partition splitting.
    /// </summary>
    public class BSPNode
    {
        #region CONFIG
        public static class BSP_CONFIG
        {
            // Aspect ratio threshold to force orientation split
            public const float ASPECT_RATIO_SPLIT_THRESHOLD = 1.25f;

            // Minimum margin between partition edge and room border
            public const int MIN_ROOM_MARGIN = 1;
        }
        #endregion

        public RectInt Bounds { get; private set; }
        public BSPNode LeftChild { get; private set; }
        public BSPNode RightChild { get; private set; }
        public RectInt? Room { get; private set; }

        public bool IsLeaf => LeftChild == null && RightChild == null;

        public BSPNode(RectInt bounds)
        {
            Bounds = bounds;
        }

        /// <summary>
        /// Recursively splits the node into two child partitions.
        /// </summary>
        public bool Split(System.Random rng, DungeonConfig config, int currentDepth)
        {
            if (currentDepth >= config.maxSplitDepth)
            {
                return false;
            }

            // Determine split orientation: horizontal or vertical
            bool splitHorizontal;
            float ratio = (float)Bounds.width / Bounds.height;

            if (ratio > BSP_CONFIG.ASPECT_RATIO_SPLIT_THRESHOLD)
            {
                splitHorizontal = false; // Split vertically because width is larger
            }
            else if (ratio < (1.0f / BSP_CONFIG.ASPECT_RATIO_SPLIT_THRESHOLD))
            {
                splitHorizontal = true;  // Split horizontally because height is larger
            }
            else
            {
                splitHorizontal = rng.NextDouble() >= 0.5;
            }

            int minSize = config.minPartitionSize;
            int maxDimension = splitHorizontal ? Bounds.height : Bounds.width;

            if (maxDimension < minSize * 2)
            {
                return false;
            }

            // Calculate split position
            float splitPercent = (float)(config.minSplitRatio + (rng.NextDouble() * (config.maxSplitRatio - config.minSplitRatio)));
            int splitPoint = Mathf.RoundToInt(maxDimension * splitPercent);

            // Ensure both child sizes respect minimum partition size
            if (splitPoint < minSize || (maxDimension - splitPoint) < minSize)
            {
                splitPoint = maxDimension / 2;
            }

            if (splitHorizontal)
            {
                RectInt top = new RectInt(Bounds.x, Bounds.y + splitPoint, Bounds.width, Bounds.height - splitPoint);
                RectInt bottom = new RectInt(Bounds.x, Bounds.y, Bounds.width, splitPoint);

                LeftChild = new BSPNode(bottom);
                RightChild = new BSPNode(top);
            }
            else
            {
                RectInt left = new RectInt(Bounds.x, Bounds.y, splitPoint, Bounds.height);
                RectInt right = new RectInt(Bounds.x + splitPoint, Bounds.y, Bounds.width - splitPoint, Bounds.height);

                LeftChild = new BSPNode(left);
                RightChild = new BSPNode(right);
            }

            // Recurse into child partitions
            LeftChild.Split(rng, config, currentDepth + 1);
            RightChild.Split(rng, config, currentDepth + 1);

            return true;
        }

        /// <summary>
        /// Populates list with all leaf partitions.
        /// </summary>
        public void GetLeaves(List<BSPNode> leaves)
        {
            if (IsLeaf)
            {
                leaves.Add(this);
                return;
            }

            LeftChild?.GetLeaves(leaves);
            RightChild?.GetLeaves(leaves);
        }

        /// <summary>
        /// Generates a room inside this leaf partition.
        /// </summary>
        public void CreateRoom(System.Random rng, DungeonConfig config, DungeonGrid grid)
        {
            if (!IsLeaf) return;

            int padding = Mathf.Max(BSP_CONFIG.MIN_ROOM_MARGIN, config.roomPadding);
            int availWidth = Bounds.width - (padding * 2);
            int availHeight = Bounds.height - (padding * 2);

            int minW = Mathf.Clamp(config.minRoomWidth, 3, availWidth);
            int minH = Mathf.Clamp(config.minRoomHeight, 3, availHeight);
            int maxW = Mathf.Clamp(config.maxRoomWidth, minW, availWidth);
            int maxH = Mathf.Clamp(config.maxRoomHeight, minH, availHeight);

            int roomWidth = rng.Next(minW, maxW + 1);
            int roomHeight = rng.Next(minH, maxH + 1);

            int maxOffsetX = availWidth - roomWidth;
            int maxOffsetY = availHeight - roomHeight;

            int roomX = Bounds.x + padding + (maxOffsetX > 0 ? rng.Next(0, maxOffsetX + 1) : 0);
            int roomY = Bounds.y + padding + (maxOffsetY > 0 ? rng.Next(0, maxOffsetY + 1) : 0);

            RectInt roomRect = new RectInt(roomX, roomY, roomWidth, roomHeight);
            Room = roomRect;
            grid.AddRoom(roomRect);
        }

        /// <summary>
        /// Retrieves a valid room from this node or randomly from child descendants.
        /// </summary>
        public RectInt? GetRepresentativeRoom(System.Random rng)
        {
            if (IsLeaf)
            {
                return Room;
            }

            RectInt? leftRoom = LeftChild?.GetRepresentativeRoom(rng);
            RectInt? rightRoom = RightChild?.GetRepresentativeRoom(rng);

            if (leftRoom.HasValue && rightRoom.HasValue)
            {
                return (rng.NextDouble() < 0.5) ? leftRoom : rightRoom;
            }

            return leftRoom ?? rightRoom;
        }
    }
}
