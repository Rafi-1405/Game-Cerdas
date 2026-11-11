using System.Collections.Generic;
using DungeonGenerator.Core;
using UnityEngine;

namespace DungeonGenerator.Validation
{
    /// <summary>
    /// Comprehensive validation report of dungeon reachability and topology.
    /// </summary>
    public class ValidationReport
    {
        public bool IsGoalReachable;
        public bool AreItemsReachable;
        public int PathLength;
        public int ReachableFloorsCount;
        public int TotalFloorsCount;
        public float ConnectivityPercentage;
        public int RetryAttempts;
        public List<Vector2Int> ShortestPath = new List<Vector2Int>();
        public string DiagnosticMessage = string.Empty;

        public bool IsFullyValid => IsGoalReachable && AreItemsReachable && PathLength > 0;
    }

    /// <summary>
    /// BFS-based connectivity validator and repair utility for procedural dungeons.
    /// </summary>
    public static class DungeonValidator
    {
        #region CONFIG
        public static class VALIDATOR_CONFIG
        {
            // Cardinal movement offsets for BFS
            public static readonly Vector2Int[] BFS_DIRECTIONS = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };

            // Status message templates
            public const string MSG_SUCCESS = "VALIDATION SUCCESS: Goal and items are fully reachable.";
            public const string MSG_UNREACHABLE_GOAL = "VALIDATION FAILED: Goal position is unreachable from Start.";
            public const string MSG_UNREACHABLE_ITEMS = "VALIDATION FAILED: One or more mandatory items are unreachable.";
            public const string MSG_INVALID_START_GOAL = "VALIDATION FAILED: Start or Goal is on an unwalkable cell.";
        }
        #endregion

        /// <summary>
        /// Validates reachability from Start to Goal and all item positions using BFS.
        /// </summary>
        public static ValidationReport Validate(
            DungeonGrid grid,
            Vector2Int start,
            Vector2Int goal,
            IReadOnlyList<Vector2Int> itemPositions = null,
            int retryAttempts = 0)
        {
            ValidationReport report = new ValidationReport
            {
                RetryAttempts = retryAttempts,
                TotalFloorsCount = grid.WalkableCells.Count
            };

            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
            {
                report.IsGoalReachable = false;
                report.DiagnosticMessage = VALIDATOR_CONFIG.MSG_INVALID_START_GOAL;
                return report;
            }

            // Execute BFS from Start
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            bool goalFound = false;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                if (current == goal)
                {
                    goalFound = true;
                }

                foreach (Vector2Int dir in VALIDATOR_CONFIG.BFS_DIRECTIONS)
                {
                    Vector2Int neighbor = current + dir;

                    if (grid.InBounds(neighbor) && grid.IsWalkable(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            report.IsGoalReachable = goalFound;
            report.ReachableFloorsCount = visited.Count;
            report.ConnectivityPercentage = report.TotalFloorsCount > 0
                ? (float)visited.Count / report.TotalFloorsCount * 100.0f
                : 0.0f;

            // Reconstruct path to Goal if reached
            if (goalFound)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int curr = goal;
                while (curr != start)
                {
                    path.Add(curr);
                    curr = parentMap[curr];
                }
                path.Add(start);
                path.Reverse();

                report.ShortestPath = path;
                report.PathLength = path.Count;
            }

            // Verify item reachability
            report.AreItemsReachable = true;
            if (itemPositions != null)
            {
                foreach (Vector2Int itemPos in itemPositions)
                {
                    if (!visited.Contains(itemPos))
                    {
                        report.AreItemsReachable = false;
                        break;
                    }
                }
            }

            // Determine diagnostic summary
            if (!report.IsGoalReachable)
            {
                report.DiagnosticMessage = VALIDATOR_CONFIG.MSG_UNREACHABLE_GOAL;
            }
            else if (!report.AreItemsReachable)
            {
                report.DiagnosticMessage = VALIDATOR_CONFIG.MSG_UNREACHABLE_ITEMS;
            }
            else
            {
                report.DiagnosticMessage = $"{VALIDATOR_CONFIG.MSG_SUCCESS} Path steps: {report.PathLength}, Reachable cells: {report.ReachableFloorsCount}/{report.TotalFloorsCount} ({report.ConnectivityPercentage:F1}%)";
            }

            return report;
        }

        /// <summary>
        /// Attempts to repair connectivity by carving a direct corridor between two positions.
        /// </summary>
        public static bool RepairDirectCorridor(DungeonGrid grid, Vector2Int from, Vector2Int to)
        {
            if (grid == null) return false;

            int minX = Mathf.Min(from.x, to.x);
            int maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y);
            int maxY = Mathf.Max(from.y, to.y);

            // Carve horizontal then vertical line
            for (int x = minX; x <= maxX; x++)
            {
                grid.CarveCorridorCell(x, from.y);
            }
            for (int y = minY; y <= maxY; y++)
            {
                grid.CarveCorridorCell(to.x, y);
            }

            grid.SurroundWalkableWithWalls();
            return true;
        }
    }
}
