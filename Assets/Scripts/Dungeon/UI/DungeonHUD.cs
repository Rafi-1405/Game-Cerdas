using System;
using DungeonGenerator.Core;
using DungeonGenerator.Validation;
using DungeonGenerator.Visuals;
using UnityEngine;

namespace DungeonGenerator.UI
{
    /// <summary>
    /// Interactive high-resolution HUD with live resizable window, collapsible accordions, and minimizable controls.
    /// </summary>
    public class DungeonHUD : MonoBehaviour
    {
        #region CONFIG
        public static class HUD_CONFIG
        {
            // Panel default and clamping limits
            public const float DEFAULT_PANEL_WIDTH = 380.0f;
            public const float MIN_PANEL_WIDTH = 280.0f;
            public const float MAX_PANEL_WIDTH = 640.0f;

            public const float DEFAULT_PANEL_HEIGHT = 650.0f;
            public const float MIN_PANEL_HEIGHT = 300.0f;

            public const float SCREEN_MARGIN = 16.0f;
            public const float DEFAULT_UI_SCALE = 1.0f;
            public const float MIN_UI_SCALE = 0.8f;
            public const float MAX_UI_SCALE = 1.6f;

            // Font size settings
            public const int FONT_SIZE_TITLE = 17;
            public const int FONT_SIZE_HEADER = 14;
            public const int FONT_SIZE_BODY = 12;
            public const int FONT_SIZE_BUTTON = 13;

            // Palette colors
            public static readonly Color COLOR_PANEL_BG = new Color(0.08f, 0.10f, 0.14f, 0.94f);
            public static readonly Color COLOR_ACCENT = new Color(0.23f, 0.51f, 0.96f, 1.0f);
            public static readonly Color COLOR_PASS = new Color(0.18f, 0.80f, 0.44f, 1.0f);
            public static readonly Color COLOR_FAIL = new Color(0.92f, 0.28f, 0.28f, 1.0f);
            public static readonly Color COLOR_GOLD = new Color(0.96f, 0.65f, 0.12f, 1.0f);
            public static readonly Color COLOR_MUTED = new Color(0.65f, 0.70f, 0.78f, 1.0f);
        }
        #endregion

        [Header("State")]
        [SerializeField] private bool _isPanelVisible = true;
        [SerializeField] private bool _isMinimized = false;

        public static bool IsPointerOverHUD { get; private set; }

        [Header("Live Sizing")]
        [SerializeField] private float _panelWidth = HUD_CONFIG.DEFAULT_PANEL_WIDTH;
        [SerializeField] private float _panelHeight = HUD_CONFIG.DEFAULT_PANEL_HEIGHT;
        [SerializeField] private float _uiScale = HUD_CONFIG.DEFAULT_UI_SCALE;

        // Collapsible Accordion States
        private bool _foldoutSeed = true;
        private bool _foldoutParams = true;
        private bool _foldoutValidation = true;
        private bool _foldoutGuide = false;
        private bool _foldoutCamera = true;
        private bool _foldoutAppearance = false;
        private bool _foldoutShortcuts = false;

        // Sizing & Resizing handles
        private bool _isResizing = false;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartDimensions;

        // References and scroll
        private DungeonManager _manager;
        private string _seedInputString = "12345";
        private Vector2 _scrollPos;

        // Cached crisp GUI skin styles
        private GUISkin _customSkin;
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _foldoutButtonStyle;
        private Texture2D _panelBgTexture;
        private Texture2D _buttonNormalTex;
        private Texture2D _buttonHoverTex;

        private void Start()
        {
            _manager = DungeonManager.Instance;
            if (_manager != null)
            {
                _seedInputString = _manager.Config.seed.ToString();
            }

            _panelHeight = Mathf.Min(HUD_CONFIG.DEFAULT_PANEL_HEIGHT, Screen.height - (HUD_CONFIG.SCREEN_MARGIN * 2));
        }

        private void Update()
        {
            // Toggle HUD panel visibility via unified New Input System
            if (DungeonInputReader.WasHudTogglePressed())
            {
                _isPanelVisible = !_isPanelVisible;
            }
        }

        private void InitializeCrispSkin()
        {
            if (_customSkin != null) return;

            // Generate crisp solid textures
            _panelBgTexture = CreateSolidTexture(2, 2, HUD_CONFIG.COLOR_PANEL_BG);
            _buttonNormalTex = CreateSolidTexture(2, 2, new Color(0.18f, 0.22f, 0.28f, 0.95f));
            _buttonHoverTex = CreateSolidTexture(2, 2, new Color(0.26f, 0.32f, 0.42f, 1.0f));

            _customSkin = Instantiate(GUI.skin);

            // Window Style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _panelBgTexture, textColor = Color.white },
                onNormal = { background = _panelBgTexture, textColor = Color.white },
                padding = new RectOffset(12, 12, 10, 10),
                fontSize = HUD_CONFIG.FONT_SIZE_TITLE,
                fontStyle = FontStyle.Bold
            };

            // Header Style
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = HUD_CONFIG.FONT_SIZE_HEADER,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HUD_CONFIG.COLOR_ACCENT }
            };

            // Body Style
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = HUD_CONFIG.FONT_SIZE_BODY,
                normal = { textColor = Color.white },
                richText = true,
                wordWrap = true
            };

            // Bold Style
            _boldStyle = new GUIStyle(_bodyStyle)
            {
                fontStyle = FontStyle.Bold
            };

            // Button Style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonNormalTex, textColor = Color.white },
                hover = { background = _buttonHoverTex, textColor = Color.white },
                fontSize = HUD_CONFIG.FONT_SIZE_BUTTON,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 5, 5)
            };

            // Text Field Style
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = HUD_CONFIG.FONT_SIZE_BODY,
                padding = new RectOffset(6, 6, 4, 4),
                alignment = TextAnchor.MiddleLeft
            };

            // Foldout Button Style
            _foldoutButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonNormalTex, textColor = HUD_CONFIG.COLOR_ACCENT },
                hover = { background = _buttonHoverTex, textColor = Color.white },
                fontSize = HUD_CONFIG.FONT_SIZE_HEADER,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
        }

        private Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (_manager == null)
            {
                _manager = DungeonManager.Instance;
                if (_manager == null) return;
            }

            InitializeCrispSkin();

            // Apply Crisp UI Matrix Scale
            Matrix4x4 previousMatrix = GUI.matrix;
            if (Mathf.Abs(_uiScale - 1.0f) > 0.01f)
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_uiScale, _uiScale, 1.0f));
            }

            // Track whether mouse pointer is over the HUD
            Vector2 mouseGui = Event.current.mousePosition;
            Rect activeHudBounds;
            if (!_isPanelVisible)
            {
                activeHudBounds = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, 180, 42);
            }
            else if (_isMinimized)
            {
                activeHudBounds = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, 340, 50);
            }
            else
            {
                activeHudBounds = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, _panelWidth, _panelHeight);
            }

            float bannerWidth = 480.0f;
            float bannerX = ((Screen.width / _uiScale) - bannerWidth) * 0.5f;
            Rect bannerBounds = new Rect(bannerX, 10.0f, bannerWidth, 32.0f);

            IsPointerOverHUD = _isResizing || activeHudBounds.Contains(mouseGui) || bannerBounds.Contains(mouseGui);

            if (_isPanelVisible)
            {
                if (_isMinimized)
                {
                    DrawMinimizedFloatingBar();
                }
                else
                {
                    DrawFullControlWindow();
                }
            }
            else
            {
                DrawShowButton();
            }

            DrawNavigationBanner();
            DrawVictoryModal();

            // Restore GUI Matrix
            GUI.matrix = previousMatrix;
        }

        private void DrawShowButton()
        {
            Rect showRect = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, 160, 36);
            if (GUI.Button(showRect, "⚙ Show Controls (F1)", _buttonStyle))
            {
                _isPanelVisible = true;
                _isMinimized = false;
            }
        }

        private void DrawMinimizedFloatingBar()
        {
            float barWidth = 320.0f;
            float barHeight = 42.0f;
            Rect barRect = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, barWidth, barHeight);

            GUI.Box(barRect, string.Empty, _windowStyle);

            GUILayout.BeginArea(new Rect(barRect.x + 8, barRect.y + 6, barRect.width - 16, barRect.height - 12));
            GUILayout.BeginHorizontal();

            GUILayout.Label($"<b>Controls [Minimized]</b> | Seed: {_manager.Config.seed}", _bodyStyle);

            if (GUILayout.Button("＋ Expand", _buttonStyle, GUILayout.Width(75), GUILayout.Height(28)))
            {
                _isMinimized = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawFullControlWindow()
        {
            float maxAvailHeight = (Screen.height / _uiScale) - (HUD_CONFIG.SCREEN_MARGIN * 2);
            _panelHeight = Mathf.Clamp(_panelHeight, HUD_CONFIG.MIN_PANEL_HEIGHT, maxAvailHeight);
            _panelWidth = Mathf.Clamp(_panelWidth, HUD_CONFIG.MIN_PANEL_WIDTH, HUD_CONFIG.MAX_PANEL_WIDTH);

            Rect windowRect = new Rect(HUD_CONFIG.SCREEN_MARGIN, HUD_CONFIG.SCREEN_MARGIN, _panelWidth, _panelHeight);

            // Draw Window Background
            GUI.Box(windowRect, string.Empty, _windowStyle);

            // Draw Title Bar with Minimize and Hide Buttons
            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 8, windowRect.width - 20, 36));
            GUILayout.BeginHorizontal();

            GUILayout.Label("<b>Procedural Dungeon Control</b>", _headerStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("— Minimize", _buttonStyle, GUILayout.Width(82), GUILayout.Height(26)))
            {
                _isMinimized = true;
            }
            if (GUILayout.Button("✕", _buttonStyle, GUILayout.Width(30), GUILayout.Height(26)))
            {
                _isPanelVisible = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Quick Expand/Collapse Toolbar
            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 46, windowRect.width - 20, 28));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("⊞ Expand All", _buttonStyle, GUILayout.Height(24)))
            {
                SetAllFoldouts(true);
            }
            if (GUILayout.Button("⊟ Collapse All", _buttonStyle, GUILayout.Height(24)))
            {
                SetAllFoldouts(false);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Scrollable Content Region
            float scrollY = windowRect.y + 78;
            float scrollHeight = windowRect.height - 100;
            Rect scrollAreaRect = new Rect(windowRect.x + 8, scrollY, windowRect.width - 16, scrollHeight);

            GUILayout.BeginArea(scrollAreaRect);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // Section 1: Seed Settings
            DrawSectionSeed();

            GUILayout.Space(6);

            // Section 2: Generator Parameters
            DrawSectionParameters();

            GUILayout.Space(6);

            // Section 3: Connectivity & Validation
            DrawSectionValidation();

            GUILayout.Space(6);

            // Section 4: Camera View & Scene Navigation
            DrawSectionCamera();

            GUILayout.Space(6);

            // Section 5: Display & Window Size
            DrawSectionAppearance();

            GUILayout.Space(6);

            // Section 6: Shortcuts & Help
            DrawSectionShortcuts();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Draw Bottom-Right Live Resize Grip Handle
            DrawResizeHandle(windowRect);
        }

        private void SetAllFoldouts(bool state)
        {
            _foldoutSeed = state;
            _foldoutParams = state;
            _foldoutValidation = state;
            _foldoutGuide = state;
            _foldoutCamera = state;
            _foldoutAppearance = state;
            _foldoutShortcuts = state;
        }

        private void DrawSectionSeed()
        {
            if (GUILayout.Button((_foldoutSeed ? "▼ " : "► ") + $"1. Seed Settings (Active: {_manager.Config.seed})", _foldoutButtonStyle))
            {
                _foldoutSeed = !_foldoutSeed;
            }

            if (!_foldoutSeed) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Active Seed:", _bodyStyle, GUILayout.Width(80));
            _seedInputString = GUILayout.TextField(_seedInputString, _textFieldStyle, GUILayout.Width(110));

            if (GUILayout.Button("Apply", _buttonStyle, GUILayout.Width(55), GUILayout.Height(24)))
            {
                if (int.TryParse(_seedInputString, out int parsedSeed))
                {
                    _manager.GenerateDungeon(parsedSeed);
                }
            }
            if (GUILayout.Button("Random", _buttonStyle, GUILayout.Width(65), GUILayout.Height(24)))
            {
                int newSeed = UnityEngine.Random.Range(10000, 999999);
                _seedInputString = newSeed.ToString();
                _manager.GenerateDungeon(newSeed);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (GUILayout.Button("↺ Regenerate Same Seed", _buttonStyle, GUILayout.Height(28)))
            {
                if (int.TryParse(_seedInputString, out int currentSeed))
                {
                    _manager.GenerateDungeon(currentSeed);
                }
                else
                {
                    _manager.RegenerateSameSeed();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSectionParameters()
        {
            if (GUILayout.Button((_foldoutParams ? "▼ " : "► ") + "2. Generator Parameters", _foldoutButtonStyle))
            {
                _foldoutParams = !_foldoutParams;
            }

            if (!_foldoutParams) return;

            GUILayout.BeginVertical(GUI.skin.box);
            DungeonConfig cfg = _manager.Config;

            GUILayout.Label($"<b>Map Dimensions:</b> {cfg.mapWidth} x {cfg.mapHeight} cells", _bodyStyle);
            cfg.mapWidth = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.mapWidth, 24, 72));
            cfg.mapHeight = cfg.mapWidth;

            GUILayout.Label($"<b>Min Room Size:</b> {cfg.minRoomWidth}", _bodyStyle);
            cfg.minRoomWidth = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.minRoomWidth, 4, 10));
            cfg.minRoomHeight = cfg.minRoomWidth;

            GUILayout.Label($"<b>Max Room Size:</b> {cfg.maxRoomWidth}", _bodyStyle);
            cfg.maxRoomWidth = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.maxRoomWidth, 10, 22));
            cfg.maxRoomHeight = cfg.maxRoomWidth;

            GUILayout.Label($"<b>Corridor Width:</b> {cfg.corridorWidth} tiles", _bodyStyle);
            cfg.corridorWidth = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.corridorWidth, 1, 3));

            GUILayout.Label($"<b>Safe Zone Radius:</b> {cfg.safeZoneRadius} cells", _bodyStyle);
            cfg.safeZoneRadius = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.safeZoneRadius, 2, 8));

            GUILayout.Label($"<b>Enemy Count:</b> {cfg.enemyCount}", _bodyStyle);
            cfg.enemyCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.enemyCount, 0, 16));

            GUILayout.Label($"<b>Item Count:</b> {cfg.itemCount}", _bodyStyle);
            cfg.itemCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(cfg.itemCount, 0, 20));

            GUILayout.Space(6);

            // Collapsible Parameter Impact Guide
            if (GUILayout.Button((_foldoutGuide ? "▼ " : "► ") + "Parameter Impact Guide", _buttonStyle))
            {
                _foldoutGuide = !_foldoutGuide;
            }

            if (_foldoutGuide)
            {
                GUILayout.Label("<color=#7ee787><b>Parameter Impact Guide:</b></color>\n" +
                                "• <b>Map Dimensions</b>: Sets outer grid boundary and partition space.\n" +
                                "• <b>Min/Max Room Size</b>: Smaller values create labyrinths; larger values create spacious halls.\n" +
                                "• <b>Corridor Width</b>: Adjusts hallway traversal width from 1 to 3 tiles.\n" +
                                "• <b>Safe Zone</b>: Guarantees zero enemy spawns within start radius.\n" +
                                "• <b>Seed</b>: Fixed seed produces 100% deterministic layout.", _bodyStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSectionValidation()
        {
            ValidationReport report = _manager.CurrentValidationReport;
            string statusTag = report != null && report.IsFullyValid ? "PASS" : "FAIL";

            if (GUILayout.Button((_foldoutValidation ? "▼ " : "► ") + $"3. Connectivity & Validation ({statusTag})", _foldoutButtonStyle))
            {
                _foldoutValidation = !_foldoutValidation;
            }

            if (!_foldoutValidation) return;

            GUILayout.BeginVertical(GUI.skin.box);

            if (report != null)
            {
                GUI.color = report.IsFullyValid ? HUD_CONFIG.COLOR_PASS : HUD_CONFIG.COLOR_FAIL;
                GUILayout.Label($"<b>Status:</b> {(report.IsFullyValid ? "PASS (100% Reachable)" : "FAILED (Unreachable)")}", _boldStyle);
                GUI.color = Color.white;

                GUILayout.Label($"• <b>Goal Reachable:</b> {(report.IsGoalReachable ? "YES" : "NO")}", _bodyStyle);
                GUILayout.Label($"• <b>BFS Shortest Path:</b> {report.PathLength} steps", _bodyStyle);
                GUILayout.Label($"• <b>Reachable Cells:</b> {report.ReachableFloorsCount} / {report.TotalFloorsCount} ({report.ConnectivityPercentage:F1}%)", _bodyStyle);
                GUILayout.Label($"• <b>Rooms Generated:</b> {_manager.CurrentGrid?.Rooms.Count ?? 0}", _bodyStyle);
                GUILayout.Label($"• <b>Generation Retries:</b> {report.RetryAttempts}", _bodyStyle);
            }
            else
            {
                GUILayout.Label("No validation data available.", _bodyStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSectionCamera()
        {
            if (GUILayout.Button((_foldoutCamera ? "▼ " : "► ") + "4. Camera View & Navigation", _foldoutButtonStyle))
            {
                _foldoutCamera = !_foldoutCamera;
            }

            if (!_foldoutCamera) return;

            GUILayout.BeginVertical(GUI.skin.box);

            var cam = _manager != null ? _manager.CameraController : null;
            bool isFreeFly = cam != null && cam.IsInFreeFlyMode;

            // Navigation Style Primary Toggle Button (G Key)
            string navBtnText = isFreeFly
                ? "🎮 Switch to Character View (G Key)"
                : "✈ Switch to Free Fly View (G Key)";

            Color originalColor = GUI.color;
            GUI.color = isFreeFly ? HUD_CONFIG.COLOR_GOLD : HUD_CONFIG.COLOR_PASS;
            if (GUILayout.Button(navBtnText, _buttonStyle, GUILayout.Height(32)))
            {
                cam?.ToggleNavigationStyle();
            }
            GUI.color = originalColor;

            string statusText = isFreeFly
                ? "<color=#f39c12><b>Free Fly View</b> (Player Frozen)</color>"
                : "<color=#2ecc71><b>Character View</b> (Player Active)</color>";
            GUILayout.Label($"<b>Active Navigation:</b> {statusText}", _bodyStyle);

            GUILayout.Space(6);
            GUILayout.Label("<b>Follow Perspectives (C Key to cycle):</b>", _boldStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Isometric", _buttonStyle))
            {
                cam?.SetCameraMode(CameraViewMode.IsometricTopDown);
            }
            if (GUILayout.Button("Third Person", _buttonStyle))
            {
                cam?.SetCameraMode(CameraViewMode.ThirdPersonFollow);
            }
            if (GUILayout.Button("Overview", _buttonStyle))
            {
                cam?.SetCameraMode(CameraViewMode.Overview);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("🎯 Focus Player (F Key)", _buttonStyle))
            {
                cam?.FocusOnTarget();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>Active Controls:</b>", _headerStyle);
            if (isFreeFly)
            {
                GUILayout.Label("• <b>WASDQE</b>: Fly camera freely across the dungeon", _bodyStyle);
                GUILayout.Label("• <b>Hold RMB + Mouse</b>: Rotate view / Look direction", _bodyStyle);
                GUILayout.Label("• <b>Shift (Hold)</b>: Fast sprint flight speed", _bodyStyle);
                GUILayout.Label("• <b>Middle Mouse Drag</b>: Pan camera view", _bodyStyle);
                GUILayout.Label("• <b>Alt + Left Mouse Drag</b>: Orbit around target", _bodyStyle);
                GUILayout.Label("• <b>Mouse Scroll Wheel</b>: Dolly / Zoom in & out", _bodyStyle);
            }
            else
            {
                GUILayout.Label("• <b>WASD / Arrow Keys</b>: Move character to goal", _bodyStyle);
                GUILayout.Label("• <b>C Key</b>: Cycle follow camera perspective", _bodyStyle);
                GUILayout.Label("• <b>Mouse Scroll Wheel</b>: Zoom in and out on player", _bodyStyle);
                GUILayout.Label("• <b>G Key</b>: Freeze player and enter Free Fly", _bodyStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSectionAppearance()
        {
            if (GUILayout.Button((_foldoutAppearance ? "▼ " : "► ") + "5. Window Sizing & UI Scale", _foldoutButtonStyle))
            {
                _foldoutAppearance = !_foldoutAppearance;
            }

            if (!_foldoutAppearance) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"<b>Panel Width:</b> {_panelWidth:F0} px", _bodyStyle);
            _panelWidth = GUILayout.HorizontalSlider(_panelWidth, HUD_CONFIG.MIN_PANEL_WIDTH, HUD_CONFIG.MAX_PANEL_WIDTH);

            GUILayout.Label($"<b>Panel Height:</b> {_panelHeight:F0} px", _bodyStyle);
            _panelHeight = GUILayout.HorizontalSlider(_panelHeight, HUD_CONFIG.MIN_PANEL_HEIGHT, (Screen.height / _uiScale) - 40);

            GUILayout.Label($"<b>UI Scale:</b> {_uiScale:F2}x", _bodyStyle);
            _uiScale = GUILayout.HorizontalSlider(_uiScale, HUD_CONFIG.MIN_UI_SCALE, HUD_CONFIG.MAX_UI_SCALE);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Compact (300px)", _buttonStyle)) { _panelWidth = 300.0f; }
            if (GUILayout.Button("Standard (380px)", _buttonStyle)) { _panelWidth = 380.0f; }
            if (GUILayout.Button("Wide (500px)", _buttonStyle)) { _panelWidth = 500.0f; }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawSectionShortcuts()
        {
            if (GUILayout.Button((_foldoutShortcuts ? "▼ " : "► ") + "6. Controls & Session Status", _foldoutButtonStyle))
            {
                _foldoutShortcuts = !_foldoutShortcuts;
            }

            if (!_foldoutShortcuts) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("• <b>G Key</b>: Toggle Navigation (Free Fly ↔ Character Control)", _boldStyle);
            GUILayout.Label("• <b>WASD / Arrow Keys</b>: Move Player (Character View)", _bodyStyle);
            GUILayout.Label("• <b>WASDQE</b>: Fly Camera (Free Fly View)", _bodyStyle);
            GUILayout.Label($"• <b>C Key</b>: Cycle Camera Perspective ({_manager?.CameraController?.CurrentMode})", _bodyStyle);
            GUILayout.Label("• <b>F Key</b>: Focus Camera on Player", _bodyStyle);
            GUILayout.Label("• <b>M Key</b>: Toggle Minimap Radar", _bodyStyle);
            GUILayout.Label("• <b>F1 Key</b>: Toggle Controls Panel", _bodyStyle);
            GUILayout.Label("• <b>Mouse Wheel</b>: Camera Zoom", _bodyStyle);

            GUILayout.Space(6);
            GUILayout.Label($"<b>Items Collected:</b> {_manager?.CollectedItemsCount} / {_manager?.TotalItemsCount}", _boldStyle);
            GUILayout.Label($"<b>Session Timer:</b> {_manager?.PlayTimer:F1}s", _boldStyle);

            GUILayout.EndVertical();
        }

        private void DrawNavigationBanner()
        {
            if (_manager == null || _manager.CameraController == null) return;

            bool isFreeFly = _manager.CameraController.IsInFreeFlyMode;
            float bannerWidth = 480.0f;
            float bannerHeight = 32.0f;
            float bannerX = ((Screen.width / _uiScale) - bannerWidth) * 0.5f;
            float bannerY = 10.0f;

            Rect bannerRect = new Rect(bannerX, bannerY, bannerWidth, bannerHeight);

            Color originalColor = GUI.color;
            GUI.color = isFreeFly ? new Color(1.0f, 0.72f, 0.20f, 0.95f) : new Color(0.20f, 0.85f, 0.45f, 0.95f);

            string bannerText = isFreeFly
                ? "✈ FREE FLY (Player Frozen) | Press [G] to Control Player"
                : "🎮 CHARACTER VIEW (Player Active) | Press [G] for Free Fly";

            if (GUI.Button(bannerRect, bannerText, _buttonStyle))
            {
                _manager.CameraController.ToggleNavigationStyle();
            }

            GUI.color = originalColor;
        }

        private void DrawResizeHandle(Rect windowRect)
        {
            float handleSize = 18.0f;
            Rect handleRect = new Rect(windowRect.xMax - handleSize, windowRect.yMax - handleSize, handleSize, handleSize);

            GUI.Label(handleRect, "◢", _boldStyle);

            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            if (currentEvent.type == EventType.MouseDown && handleRect.Contains(mousePos))
            {
                _isResizing = true;
                _resizeStartMouse = mousePos;
                _resizeStartDimensions = new Vector2(_panelWidth, _panelHeight);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _isResizing)
            {
                Vector2 delta = mousePos - _resizeStartMouse;
                _panelWidth = Mathf.Clamp(_resizeStartDimensions.x + delta.x, HUD_CONFIG.MIN_PANEL_WIDTH, HUD_CONFIG.MAX_PANEL_WIDTH);
                _panelHeight = Mathf.Clamp(_resizeStartDimensions.y + delta.y, HUD_CONFIG.MIN_PANEL_HEIGHT, (Screen.height / _uiScale) - 30);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && _isResizing)
            {
                _isResizing = false;
                currentEvent.Use();
            }
        }

        private void DrawVictoryModal()
        {
            if (_manager == null || !_manager.IsGoalReached) return;

            float modalWidth = 480.0f;
            float modalHeight = 220.0f;
            float posX = ((Screen.width / _uiScale) - modalWidth) * 0.5f;
            float posY = ((Screen.height / _uiScale) - modalHeight) * 0.35f;

            Rect modalRect = new Rect(posX, posY, modalWidth, modalHeight);

            GUI.Box(modalRect, string.Empty, _windowStyle);

            GUILayout.BeginArea(new Rect(modalRect.x + 20, modalRect.y + 20, modalRect.width - 40, modalRect.height - 40));

            GUI.color = HUD_CONFIG.COLOR_GOLD;
            GUILayout.Label("<size=20><b>★ GOAL REACHED! DUNGEON CLEARED! ★</b></size>", _boldStyle);
            GUI.color = Color.white;

            GUILayout.Space(10);
            GUILayout.Label($"<b>Clear Time:</b> {_manager.PlayTimer:F1} seconds", _bodyStyle);
            GUILayout.Label($"<b>Items Collected:</b> {_manager.CollectedItemsCount} / {_manager.TotalItemsCount}", _bodyStyle);
            GUILayout.Label($"<b>Active Seed:</b> {_manager.Config.seed}", _bodyStyle);

            GUILayout.Space(14);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Next Random Seed", _buttonStyle, GUILayout.Height(36)))
            {
                int nextSeed = UnityEngine.Random.Range(10000, 999999);
                _seedInputString = nextSeed.ToString();
                _manager.GenerateDungeon(nextSeed);
            }
            if (GUILayout.Button("Replay Same Seed", _buttonStyle, GUILayout.Height(36)))
            {
                _manager.RegenerateSameSeed();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        public void SyncSeedDisplay(int seed)
        {
            _seedInputString = seed.ToString();
        }
    }
}
