using System.Collections.Generic;
using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using DungeonGenerator.Validation;
using UnityEngine;

namespace DungeonGenerator.UI
{
    /// <summary>
    /// Runtime 2D minimap renderer generating texture representations of the dungeon topology.
    /// </summary>
    public class DungeonMinimapUI : MonoBehaviour
    {
        #region CONFIG
        public static class MINIMAP_CONFIG
        {
            // Minimap color palette
            public static readonly Color COLOR_BG = new Color(0.05f, 0.07f, 0.09f, 0.85f);
            public static readonly Color COLOR_WALL = new Color(0.13f, 0.15f, 0.18f, 1.0f);
            public static readonly Color COLOR_ROOM = new Color(0.19f, 0.22f, 0.28f, 1.0f);
            public static readonly Color COLOR_CORRIDOR = new Color(0.28f, 0.32f, 0.39f, 1.0f);
            public static readonly Color COLOR_START = new Color(0.10f, 0.80f, 0.40f, 1.0f);
            public static readonly Color COLOR_SAFE_ZONE = new Color(0.10f, 0.80f, 0.40f, 0.40f);
            public static readonly Color COLOR_GOAL = new Color(0.98f, 0.75f, 0.10f, 1.0f);
            public static readonly Color COLOR_BFS_PATH = new Color(0.0f, 0.85f, 1.0f, 1.0f);
            public static readonly Color COLOR_ENEMY = new Color(0.95f, 0.25f, 0.25f, 1.0f);
            public static readonly Color COLOR_ITEM = new Color(0.20f, 0.60f, 1.0f, 1.0f);
            public static readonly Color COLOR_PLAYER = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            // Display dimensions
            public const int MINIMAP_TEXTURE_SIZE = 512;
            public const float SCREEN_MARGIN = 20.0f;
            public const float MAP_WINDOW_SIZE = 240.0f;
        }
        #endregion

        [SerializeField] private bool _showMinimap = true;

        private Texture2D _mapTexture;
        private DungeonGrid _grid;
        private DungeonPlacementData _placements;
        private ValidationReport _report;
        private Transform _playerTransform;

        public bool ShowMinimap => _showMinimap;

        public void ToggleMinimap()
        {
            _showMinimap = !_showMinimap;
        }

        public void SetData(DungeonGrid grid, DungeonPlacementData placements, ValidationReport report, Transform player)
        {
            _grid = grid;
            _placements = placements;
            _report = report;
            _playerTransform = player;

            BakeStaticMapTexture();
        }

        private void Update()
        {
            if (DungeonInputReader.WasMinimapTogglePressed())
            {
                ToggleMinimap();
            }
        }

        /// <summary>
        /// Renders static dungeon walls, floors, corridors, items, and path onto a Texture2D.
        /// </summary>
        public void BakeStaticMapTexture()
        {
            if (_grid == null) return;

            int size = MINIMAP_CONFIG.MINIMAP_TEXTURE_SIZE;
            if (_mapTexture == null || _mapTexture.width != size)
            {
                _mapTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = MINIMAP_CONFIG.COLOR_BG;
            }

            // Map grid cells to texture pixels
            float scaleX = (float)size / _grid.Width;
            float scaleY = (float)size / _grid.Height;

            for (int gx = 0; gx < _grid.Width; gx++)
            {
                for (int gy = 0; gy < _grid.Height; gy++)
                {
                    CellType cell = _grid.GetCell(gx, gy);
                    Color cellColor;

                    switch (cell)
                    {
                        case CellType.RoomFloor:
                            cellColor = MINIMAP_CONFIG.COLOR_ROOM;
                            break;
                        case CellType.CorridorFloor:
                            cellColor = MINIMAP_CONFIG.COLOR_CORRIDOR;
                            break;
                        case CellType.Wall:
                            cellColor = MINIMAP_CONFIG.COLOR_WALL;
                            break;
                        default:
                            continue;
                    }

                    int pxMin = Mathf.FloorToInt(gx * scaleX);
                    int pxMax = Mathf.FloorToInt((gx + 1) * scaleX);
                    int pyMin = Mathf.FloorToInt(gy * scaleY);
                    int pyMax = Mathf.FloorToInt((gy + 1) * scaleY);

                    for (int px = pxMin; px < pxMax; px++)
                    {
                        for (int py = pyMin; py < pyMax; py++)
                        {
                            if (px >= 0 && px < size && py >= 0 && py < size)
                            {
                                pixels[py * size + px] = cellColor;
                            }
                        }
                    }
                }
            }

            // Draw BFS shortest path
            if (_report != null && _report.ShortestPath != null)
            {
                foreach (Vector2Int pathNode in _report.ShortestPath)
                {
                    DrawMarker(pixels, size, pathNode.x * scaleX, pathNode.y * scaleY, 2, MINIMAP_CONFIG.COLOR_BFS_PATH);
                }
            }

            // Draw Items
            if (_placements != null)
            {
                foreach (Vector2Int item in _placements.ItemPositions)
                {
                    DrawMarker(pixels, size, item.x * scaleX, item.y * scaleY, 2, MINIMAP_CONFIG.COLOR_ITEM);
                }

                // Draw Enemies
                foreach (Vector2Int enemy in _placements.EnemyPositions)
                {
                    DrawMarker(pixels, size, enemy.x * scaleX, enemy.y * scaleY, 3, MINIMAP_CONFIG.COLOR_ENEMY);
                }

                // Draw Start Pad
                DrawMarker(pixels, size, _placements.StartPosition.x * scaleX, _placements.StartPosition.y * scaleY, 4, MINIMAP_CONFIG.COLOR_START);

                // Draw Goal Monument
                DrawMarker(pixels, size, _placements.GoalPosition.x * scaleX, _placements.GoalPosition.y * scaleY, 5, MINIMAP_CONFIG.COLOR_GOAL);
            }

            _mapTexture.SetPixels(pixels);
            _mapTexture.Apply();
        }

        private void DrawMarker(Color[] pixels, int textureSize, float centerX, float centerY, int radius, Color color)
        {
            int cx = Mathf.RoundToInt(centerX);
            int cy = Mathf.RoundToInt(centerY);

            for (int rx = -radius; rx <= radius; rx++)
            {
                for (int ry = -radius; ry <= radius; ry++)
                {
                    if ((rx * rx) + (ry * ry) <= (radius * radius))
                    {
                        int px = cx + rx;
                        int py = cy + ry;
                        if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        {
                            pixels[py * textureSize + px] = color;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!_showMinimap || _mapTexture == null || _grid == null) return;

            float windowSize = MINIMAP_CONFIG.MAP_WINDOW_SIZE;
            float margin = MINIMAP_CONFIG.SCREEN_MARGIN;
            Rect mapRect = new Rect(Screen.width - windowSize - margin, margin, windowSize, windowSize);

            // Background border
            GUI.Box(new Rect(mapRect.x - 4, mapRect.y - 4, mapRect.width + 8, mapRect.height + 26), "Dungeon Radar (M)");

            // Draw minimap texture
            GUI.DrawTexture(mapRect, _mapTexture, ScaleMode.StretchToFill, true);

            // Draw real-time Player Marker
            if (_playerTransform != null)
            {
                Vector2Int playerGridPos = _grid.WorldToGridPosition(_playerTransform.position);
                float normalizedX = Mathf.Clamp01((float)playerGridPos.x / _grid.Width);
                float normalizedY = Mathf.Clamp01((float)playerGridPos.y / _grid.Height);

                // Invert Y because GUI coordinates start from top-left while texture starts from bottom-left
                float playerScreenX = mapRect.x + (normalizedX * mapRect.width);
                float playerScreenY = mapRect.y + ((1.0f - normalizedY) * mapRect.height);

                Rect playerDot = new Rect(playerScreenX - 4, playerScreenY - 4, 8, 8);
                GUI.color = MINIMAP_CONFIG.COLOR_PLAYER;
                GUI.DrawTexture(playerDot, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }
}
