using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Validates world connectivity and accessibility using flood-fill on the spatial grid.
    /// ASD-STE100: This module checks if the player can reach all key areas.
    /// Follows Section 7.6 of the specification.
    /// </summary>
    public static class WorldValidator
    {
        /// <summary>
        /// Validates that the extraction zone, structures, and crates are reachable from the player start.
        /// </summary>
        public static bool ValidateConnectivity(CellType[,] grid, int width, int height, Vector2Int startPos, Vector2Int extractionPos, List<Vector2Int> criticalPoints)
        {
            if (grid == null) return false;
            if (startPos.x < 0 || startPos.x >= width || startPos.y < 0 || startPos.y >= height) return false;

            bool[,] visited = new bool[width, height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            queue.Enqueue(startPos);
            visited[startPos.x, startPos.y] = true;

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nx = cur.x + dx[i];
                    int ny = cur.y + dy[i];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (!visited[nx, ny])
                        {
                            CellType type = grid[nx, ny];
                            // Impassable cell types
                            if (type != CellType.TreeBoundary && type != CellType.StructureWall)
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }

            // Check if extraction zone is reachable
            if (!visited[extractionPos.x, extractionPos.y])
            {
                return false;
            }

            // Check critical points (structure doorways, crates)
            if (criticalPoints != null)
            {
                foreach (var pt in criticalPoints)
                {
                    if (pt.x >= 0 && pt.x < width && pt.y >= 0 && pt.y < height)
                    {
                        if (!visited[pt.x, pt.y]) return false;
                    }
                }
            }

            return true;
        }
    }
}
