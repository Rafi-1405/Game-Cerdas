using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DeadDepths.AI;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadDepths.UI
{
    /// <summary>
    /// Comprehensive Debug Mode (toggle with F1) providing live inspection for:
    /// 1. AI Zombie FSM states, FOV vision cones, Line-of-Sight, NavMesh paths, and targets.
    /// 2. Procedural Cluster Map, Wall, Resource (Crates), and DDA generation + live Grid Minimap.
    /// 3. Quick testing controls including God Mode (G), Map Regeneration, and Evacuation Warp.
    /// </summary>
    public class DeadDepthsDebugMode : MonoBehaviour
    {
        public static DeadDepthsDebugMode Instance { get; private set; }

        [SerializeField] private bool isDebugVisible = false;
        [SerializeField] private bool showAiWorldOverlay = true;
        [SerializeField] private bool showMapResourcePanel = true;
        [SerializeField] private bool showAiPanel = true;

        public bool IsDebugVisible => isDebugVisible;

        [NonSerialized] private Texture2D texPanelBg;
        [NonSerialized] private Texture2D texBorder;
        [NonSerialized] private Texture2D texLinePixel;
        [NonSerialized] private Texture2D minimapTexture;
        [NonSerialized] private int lastBuiltSeed = -1;
        [NonSerialized] private int lastBuiltClusterCount = -1;

        [NonSerialized] private GUIStyle headerStyle;
        [NonSerialized] private GUIStyle bodyStyle;
        [NonSerialized] private GUIStyle tagStyle;
        [NonSerialized] private GUIStyle buttonStyle;

        [NonSerialized] private readonly List<ZombieController> cachedZombies = new List<ZombieController>();
        [NonSerialized] private float zombieRefreshTimer = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            lastBuiltSeed = -1;
            lastBuiltClusterCount = -1;
            cachedZombies.Clear();
            zombieRefreshTimer = 0f;
            ResetStyles();
            InitializeTextures();
        }

        private void OnEnable()
        {
            ResetStyles();
            EnsureTextures();
        }

        private void OnDisable()
        {
            ResetStyles();
        }

        private void InitializeTextures()
        {
            DestroyTextures();
            texPanelBg = CreateSolidTexture(new Color(0.05f, 0.07f, 0.11f, 0.88f));
            texBorder = CreateSolidTexture(new Color(0.25f, 0.72f, 0.95f, 0.85f));
            texLinePixel = CreateSolidTexture(Color.white);
        }

        private void EnsureTextures()
        {
            if (texPanelBg == null) texPanelBg = CreateSolidTexture(new Color(0.05f, 0.07f, 0.11f, 0.88f));
            if (texBorder == null) texBorder = CreateSolidTexture(new Color(0.25f, 0.72f, 0.95f, 0.85f));
            if (texLinePixel == null) texLinePixel = CreateSolidTexture(Color.white);
        }

        private Texture2D CreateSolidTexture(Color c)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixels(new Color[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref texPanelBg);
            DestroyTexture(ref texBorder);
            DestroyTexture(ref texLinePixel);
            DestroyTexture(ref minimapTexture);
        }

        private static void DestroyTexture(ref Texture2D tex)
        {
            if (tex != null)
            {
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
                tex = null;
            }
        }

        private void ResetStyles()
        {
            headerStyle = null;
            bodyStyle = null;
            tagStyle = null;
            buttonStyle = null;
        }

        private void Update()
        {
            if (WasF1Pressed())
            {
                ToggleDebugMode();
            }

            if (!isDebugVisible) return;

            zombieRefreshTimer -= Time.unscaledDeltaTime;
            if (zombieRefreshTimer <= 0f)
            {
                zombieRefreshTimer = 0.15f;
                RefreshZombieCache();
            }
        }

        public void ToggleDebugMode()
        {
            isDebugVisible = !isDebugVisible;
            if (isDebugVisible)
            {
                RefreshZombieCache();
                EnsureMinimapTexture(true);
            }
        }

        public void SetDebugVisible(bool visible)
        {
            isDebugVisible = visible;
            if (isDebugVisible)
            {
                RefreshZombieCache();
                EnsureMinimapTexture(true);
            }
        }

        private bool WasF1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) return true;
#endif
            return false;
        }

        private void RefreshZombieCache()
        {
            cachedZombies.Clear();
            var found = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
            if (found != null) cachedZombies.AddRange(found);
        }

        private void EnsureMinimapTexture(bool force = false)
        {
            var wg = WorldGenerator.Instance;
            if (wg == null) return;

            if (!force && minimapTexture != null &&
                lastBuiltSeed == wg.LastSeed &&
                lastBuiltClusterCount == wg.Clusters.Count)
            {
                return;
            }

            int w = wg.GridWidth;
            int h = wg.GridHeight;
            if (w <= 0 || h <= 0) return;

            if (minimapTexture == null || minimapTexture.width != w || minimapTexture.height != h)
            {
                DestroyTexture(ref minimapTexture);
                minimapTexture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CellType cell = wg.GetCell(x, y);
                    Color c;
                    switch (cell)
                    {
                        case CellType.TreeBoundary: c = new Color(0.08f, 0.22f, 0.10f); break;
                        case CellType.WalkableGround: c = new Color(0.28f, 0.34f, 0.26f); break;
                        case CellType.MudPatch: c = new Color(0.40f, 0.27f, 0.16f); break;
                        case CellType.StructureWall: c = new Color(0.65f, 0.65f, 0.72f); break;
                        case CellType.StructureFloor: c = new Color(0.42f, 0.42f, 0.46f); break;
                        case CellType.StructureDoorway: c = new Color(0.75f, 0.75f, 0.40f); break;
                        case CellType.BlockingObject: c = new Color(0.95f, 0.55f, 0.15f); break;
                        case CellType.SuppliesCrate: c = new Color(1.00f, 0.90f, 0.20f); break;
                        case CellType.PlayerStart: c = new Color(0.25f, 0.60f, 0.95f); break;
                        case CellType.ExtractionZone: c = new Color(0.15f, 0.95f, 0.45f); break;
                        default: c = Color.black; break;
                    }
                    minimapTexture.SetPixel(x, y, c);
                }
            }

            minimapTexture.Apply();
            lastBuiltSeed = wg.LastSeed;
            lastBuiltClusterCount = wg.Clusters.Count;
        }

        private void OnGUI()
        {
            if (!isDebugVisible) return;
            if (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible) return;

            GUI.depth = 1;
            EnsureTextures();
            InitStyles();

            // 1. World-projected AI Overlay (Repaint only so it never consumes UI button clicks)
            if (showAiWorldOverlay && Event.current.type == EventType.Repaint)
            {
                DrawAiWorldOverlay();
            }

            // 2. Left Panel: AI Zombie Debug + Testing Toolbar
            DrawLeftAiDebugPanel();

            // 3. Right Panel: Map / Wall / Resource Generation + Minimap
            if (showMapResourcePanel)
            {
                DrawRightMapResourcePanel();
            }
        }

        private void DrawLeftAiDebugPanel()
        {
            float panelX = 14f;
            float panelY = 14f;
            float panelW = 335f;
            float panelH = showAiPanel ? Mathf.Min(Screen.height - 105f, 470f) : 108f;

            GUI.DrawTexture(new Rect(panelX - 1, panelY - 1, panelW + 2, panelH + 2), texBorder);
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), texPanelBg);

            float y = panelY + 8f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 20f), "DEAD DEPTHS DEBUG MODE [F1]", headerStyle);
            y += 24f;

            // Quick Action Buttons
            var player = DeadDepthsPlayerController.Instance;
            bool godOn = player != null && player.IsGodMode;
            float btnW = (panelW - 32f) / 3f;

            if (GUI.Button(new Rect(panelX + 10f, y, btnW, 24f), godOn ? "God: ON (G)" : "God: OFF (G)", buttonStyle))
            {
                if (player != null) player.ToggleGodMode();
            }
            if (GUI.Button(new Rect(panelX + 16f + btnW, y, btnW, 24f), "Regen Map", buttonStyle))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartLevel(GameManager.Instance.CurrentLevel);
                    EnsureMinimapTexture(true);
                    RefreshZombieCache();
                }
            }
            if (GUI.Button(new Rect(panelX + 22f + btnW * 2f, y, btnW, 24f), "Warp -> Evac", buttonStyle))
            {
                WarpPlayerToExtraction();
            }
            y += 28f;

            // Sub-toggles
            if (GUI.Button(new Rect(panelX + 10f, y, btnW, 22f), showAiPanel ? "AI List: ON" : "AI List: OFF", buttonStyle))
                showAiPanel = !showAiPanel;
            if (GUI.Button(new Rect(panelX + 16f + btnW, y, btnW, 22f), showAiWorldOverlay ? "FOV/Path: ON" : "FOV/Path: OFF", buttonStyle))
                showAiWorldOverlay = !showAiWorldOverlay;
            if (GUI.Button(new Rect(panelX + 22f + btnW * 2f, y, btnW, 22f), showMapResourcePanel ? "Map/PCG: ON" : "Map/PCG: OFF", buttonStyle))
                showMapResourcePanel = !showMapResourcePanel;
            y += 28f;

            if (!showAiPanel) return;

            // AI Zombie Summary Counts
            int alive = 0, walkers = 0, spitters = 0, chargers = 0;
            int patrol = 0, chase = 0, attack = 0, search = 0;

            foreach (var z in cachedZombies)
            {
                if (z == null || z.IsDead) continue;
                alive++;
                if (z.Archetype == ZombieArchetype.Walker) walkers++;
                else if (z.Archetype == ZombieArchetype.Spitter) spitters++;
                else if (z.Archetype == ZombieArchetype.Charger) chargers++;

                switch (z.CurrentState)
                {
                    case ZombieState.IdlePatrol: patrol++; break;
                    case ZombieState.Chase: chase++; break;
                    case ZombieState.Attack: attack++; break;
                    case ZombieState.Search: search++; break;
                }
            }

            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                $"AI ZOMBIE DEBUG | Alive: {alive} (W:{walkers} S:{spitters} C:{chargers})", headerStyle);
            y += 20f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f),
                $"FSM: Patrol={patrol}  Chase={chase}  Attack={attack}  Search={search}", bodyStyle);
            y += 20f;

            // Sort nearest zombies to player for compact live inspector
            Vector3 pPos = player != null ? player.transform.position : Vector3.zero;
            cachedZombies.RemoveAll(z => z == null);
            cachedZombies.Sort((a, b) =>
            {
                if (a.IsDead != b.IsDead) return a.IsDead ? 1 : -1;
                return Vector3.Distance(a.transform.position, pPos).CompareTo(Vector3.Distance(b.transform.position, pPos));
            });

            int maxRows = Mathf.Min(12, cachedZombies.Count);
            for (int i = 0; i < maxRows && y < panelY + panelH - 22f; i++)
            {
                var z = cachedZombies[i];
                if (z.IsDead) continue;
                float dist = Vector3.Distance(z.transform.position, pPos);
                string sub = string.IsNullOrEmpty(z.SubPhaseName) ? "" : $"({z.SubPhaseName})";
                string los = z.HasLineOfSightToPlayer ? "LOS" : "---";
                string row = $"#{i + 1:00} [{z.Archetype,-7}] {z.CurrentState}{sub,-11} HP:{z.CurrentHealth:0}/{z.AiConfig.MaxHealth:0} {dist:0.0}m {los}";
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f), row, bodyStyle);
                y += 18f;
            }
        }

        private void DrawRightMapResourcePanel()
        {
            var wg = WorldGenerator.Instance;
            if (wg == null) return;
            EnsureMinimapTexture();

            float panelW = 265f;
            float panelH = 495f;
            float panelX = Screen.width - panelW - 14f;
            float panelY = 48f;

            GUI.DrawTexture(new Rect(panelX - 1, panelY - 1, panelW + 2, panelH + 2), texBorder);
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), texPanelBg);

            float y = panelY + 8f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 20f), "MAP, WALL & RESOURCE DEBUG", headerStyle);
            y += 22f;

            var crates = FindObjectsByType<SuppliesCrate>(FindObjectsSortMode.None);
            int openedCrates = 0;
            foreach (var c in crates)
            {
                if (c != null && c.IsOpened) openedCrates++;
            }

            var diff = DifficultyController.Instance;
            var tracker = PlayerPerformanceTracker.Instance;
            float livePerf = tracker != null ? PerformanceEvaluator.EvaluatePerformance(tracker, out _) : 0.5f;
            string tier = diff != null ? diff.CurrentTier.ToString() : "Balanced";

            string[] lines = new string[]
            {
                $"Seed: {wg.LastSeed} | Grid: {wg.GridWidth}x{wg.GridHeight}",
                $"Clusters: {wg.Clusters.Count} | Corridors: {wg.Corridors.Count}",
                $"Start Cluster: #{wg.StartClusterId} | Evac: #{wg.EvacuationClusterId}",
                $"Trees (Forest): {wg.TotalTreesSpawned}",
                $"Structures: {wg.TotalStructuresSpawned} | Walls: {wg.TotalWallsSpawned}",
                $"Blockers: {wg.TotalBlockersSpawned} ({wg.BreakableBlockersSpawned} breakable)",
                $"Resources (Crates): {openedCrates}/{wg.TotalCratesSpawned} opened",
                $"Mud Patches: {wg.TotalMudPatchesSpawned} | BFS: {(wg.ConnectivityValid ? "VALID" : "FAIL")}",
                $"DDA: {tier} | Perf Score: {livePerf:F2}"
            };

            foreach (var line in lines)
            {
                GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 18f), line, bodyStyle);
                y += 18f;
            }

            y += 6f;

            // Draw Live Grid Minimap
            float mapSize = panelW - 24f;
            Rect mapRect = new Rect(panelX + 12f, y, mapSize, mapSize);
            GUI.DrawTexture(new Rect(mapRect.x - 1, mapRect.y - 1, mapRect.width + 2, mapRect.height + 2), texBorder);
            if (minimapTexture != null)
            {
                GUI.DrawTexture(mapRect, minimapTexture);
            }

            // Dynamic markers on Minimap: Zombies, Evacuation, Player
            float cellW = mapRect.width / Mathf.Max(1, wg.GridWidth);
            float cellH = mapRect.height / Mathf.Max(1, wg.GridHeight);

            // Cluster center dots
            foreach (var c in wg.Clusters)
            {
                Color cc = c.Role == ClusterRole.Start ? Color.cyan :
                           c.Role == ClusterRole.Evacuation ? Color.green : new Color(1f, 1f, 1f, 0.45f);
                DrawMinimapDot(mapRect, wg, CellToWorldCenter(wg, c.Center), 5f, cc);
            }

            // Live Zombies
            foreach (var z in cachedZombies)
            {
                if (z == null || z.IsDead) continue;
                Color zc = z.Archetype == ZombieArchetype.Spitter ? Color.green :
                           z.Archetype == ZombieArchetype.Charger ? Color.red : new Color(1f, 0.6f, 0.6f);
                DrawMinimapDot(mapRect, wg, z.transform.position, 4f, zc);
            }

            // Live Player
            var player = DeadDepthsPlayerController.Instance;
            if (player != null)
            {
                DrawMinimapDot(mapRect, wg, player.transform.position, 7f, Color.cyan);
            }

            y += mapSize + 6f;
            GUI.Label(new Rect(panelX + 10f, y, panelW - 20f, 32f),
                "Cyan=Player | Green=Evac | Yellow=Crate\nGray=Wall | Brown=Mud | Orange=Blocker", bodyStyle);
        }

        private Vector3 CellToWorldCenter(WorldGenerator wg, Vector2Int cell)
        {
            return wg.CellToWorld(cell.x, cell.y);
        }

        private void DrawMinimapDot(Rect mapRect, WorldGenerator wg, Vector3 worldPos, float size, Color color)
        {
            Vector2Int cell = wg.WorldToCell(worldPos);
            float nx = (cell.x + 0.5f) / Mathf.Max(1, wg.GridWidth);
            float ny = (cell.y + 0.5f) / Mathf.Max(1, wg.GridHeight);

            float sx = mapRect.x + nx * mapRect.width - size * 0.5f;
            float sy = mapRect.y + mapRect.height - (ny * mapRect.height) - size * 0.5f;

            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(sx, sy, size, size), texLinePixel);
            GUI.color = prev;
        }

        private void DrawAiWorldOverlay()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            var player = DeadDepthsPlayerController.Instance;
            Vector3 playerEye = player != null ? player.transform.position + Vector3.up * 1.0f : Vector3.zero;

            foreach (var z in cachedZombies)
            {
                if (z == null || z.IsDead) continue;

                Vector3 zPos = z.transform.position;
                Vector3 headScreen = cam.WorldToScreenPoint(zPos + Vector3.up * 2.2f);
                if (headScreen.z <= 0f) continue;

                // 1. Floating State & HP tag
                float sx = headScreen.x;
                float sy = Screen.height - headScreen.y;
                string sub = string.IsNullOrEmpty(z.SubPhaseName) ? "" : $":{z.SubPhaseName}";
                string tag = $"[{z.Archetype}] {z.CurrentState}{sub} ({z.CurrentHealth:0}/{z.AiConfig.MaxHealth:0})";
                GUI.Label(new Rect(sx - 90f, sy - 18f, 180f, 20f), tag, tagStyle);

                // 2. Vision Cone (FOV) edges
                float halfAngle = z.AiConfig.VisionAngle * 0.5f;
                float range = z.AiConfig.VisionRange;
                Vector3 origin = zPos + Vector3.up * 0.2f;
                Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * z.transform.forward;
                Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * z.transform.forward;

                Color fovColor = z.CurrentState == ZombieState.Attack ? new Color(1f, 0.2f, 0.2f, 0.65f) :
                                 z.CurrentState == ZombieState.Chase ? new Color(1f, 0.7f, 0.1f, 0.65f) :
                                 new Color(0.2f, 0.9f, 0.4f, 0.45f);

                DrawWorldLine(cam, origin, origin + leftDir * range, fovColor, 1.5f);
                DrawWorldLine(cam, origin, origin + rightDir * range, fovColor, 1.5f);
                DrawWorldLine(cam, origin + leftDir * range, origin + rightDir * range, fovColor, 1.0f);

                // 3. Line-of-Sight ray to player
                if (player != null && Vector3.Distance(zPos, player.transform.position) <= range)
                {
                    Color losColor = z.HasLineOfSightToPlayer ? new Color(0.2f, 1f, 0.2f, 0.8f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
                    DrawWorldLine(cam, zPos + Vector3.up * 1.4f, playerEye, losColor, 1.5f);
                }

                // 4. NavMesh path corners
                if (z.Agent != null && z.Agent.enabled && z.Agent.hasPath && z.Agent.path.corners.Length > 1)
                {
                    Vector3[] corners = z.Agent.path.corners;
                    for (int c = 0; c < corners.Length - 1; c++)
                    {
                        DrawWorldLine(cam, corners[c] + Vector3.up * 0.15f, corners[c + 1] + Vector3.up * 0.15f, new Color(0.1f, 0.85f, 1f, 0.85f), 2.0f);
                    }
                }

                // 5. LastKnownPlayerPos marker during Search state
                if (z.CurrentState == ZombieState.Search)
                {
                    Vector3 lkpScreen = cam.WorldToScreenPoint(z.LastKnownPlayerPos + Vector3.up * 0.5f);
                    if (lkpScreen.z > 0f)
                    {
                        DrawWorldLine(cam, zPos + Vector3.up * 0.5f, z.LastKnownPlayerPos + Vector3.up * 0.5f, new Color(1f, 0.35f, 1f, 0.7f), 1.5f);
                        GUI.Label(new Rect(lkpScreen.x - 45f, Screen.height - lkpScreen.y - 10f, 90f, 18f), "[SEARCH TARGET]", tagStyle);
                    }
                }
            }
        }

        private void DrawWorldLine(Camera cam, Vector3 worldA, Vector3 worldB, Color color, float thickness)
        {
            Vector3 sa = cam.WorldToScreenPoint(worldA);
            Vector3 sb = cam.WorldToScreenPoint(worldB);
            if (sa.z <= 0f || sb.z <= 0f) return;

            Vector2 p1 = new Vector2(sa.x, Screen.height - sa.y);
            Vector2 p2 = new Vector2(sb.x, Screen.height - sb.y);
            DrawScreenLine(p1, p2, color, thickness);
        }

        private void DrawScreenLine(Vector2 p1, Vector2 p2, Color color, float width)
        {
            Vector2 delta = p2 - p1;
            float len = delta.magnitude;
            if (len < 1f) return;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, p1);
            GUI.DrawTexture(new Rect(p1.x, p1.y - width * 0.5f, len, width), texLinePixel);

            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        private void WarpPlayerToExtraction()
        {
            var wg = WorldGenerator.Instance;
            var player = DeadDepthsPlayerController.Instance;
            if (wg == null || player == null) return;

            var cc = player.GetComponent<CharacterController>();
            bool prevCC = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            player.transform.position = wg.ExtractionPosition + new Vector3(0f, 0.1f, -3.5f);
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = prevCC;

            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.SnapToTarget(player.transform);
            }
        }

        private void OnDrawGizmos()
        {
            if (!isDebugVisible) return;

            // Scene View PCG Cluster & Corridor Gizmos
            var wg = WorldGenerator.Instance;
            if (wg != null)
            {
                foreach (var c in wg.Clusters)
                {
                    Gizmos.color = c.Role == ClusterRole.Start ? Color.cyan :
                                   c.Role == ClusterRole.Evacuation ? Color.green : Color.yellow;
                    Vector3 center = wg.CellToWorld(c.Center.x, c.Center.y) + Vector3.up * 0.5f;
                    Gizmos.DrawWireSphere(center, ((c.RadiusX + c.RadiusY) * 0.5f) * wg.MapConfig.CellSize);
                }

                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                foreach (var edge in wg.Corridors)
                {
                    if (edge.FromClusterId < wg.Clusters.Count && edge.ToClusterId < wg.Clusters.Count)
                    {
                        Vector3 a = wg.CellToWorld(wg.Clusters[edge.FromClusterId].Center.x, wg.Clusters[edge.FromClusterId].Center.y) + Vector3.up * 0.6f;
                        Vector3 b = wg.CellToWorld(wg.Clusters[edge.ToClusterId].Center.x, wg.Clusters[edge.ToClusterId].Center.y) + Vector3.up * 0.6f;
                        Gizmos.DrawLine(a, b);
                    }
                }
            }

            // Scene View AI Zombie Gizmos
            var zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
            foreach (var z in zombies)
            {
                if (z == null || z.IsDead) continue;

                Vector3 pos = z.transform.position + Vector3.up * 0.3f;
                float half = z.AiConfig.VisionAngle * 0.5f;
                Vector3 left = Quaternion.Euler(0f, -half, 0f) * z.transform.forward * z.AiConfig.VisionRange;
                Vector3 right = Quaternion.Euler(0f, half, 0f) * z.transform.forward * z.AiConfig.VisionRange;

                Gizmos.color = z.HasLineOfSightToPlayer ? Color.red : Color.green;
                Gizmos.DrawLine(pos, pos + left);
                Gizmos.DrawLine(pos, pos + right);

                if (z.CurrentState == ZombieState.Search)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(z.LastKnownPlayerPos + Vector3.up * 0.5f, 0.6f);
                }

                if (z.Agent != null && z.Agent.enabled && z.Agent.hasPath)
                {
                    Gizmos.color = Color.cyan;
                    var corners = z.Agent.path.corners;
                    for (int i = 0; i < corners.Length - 1; i++)
                    {
                        Gizmos.DrawLine(corners[i] + Vector3.up * 0.2f, corners[i + 1] + Vector3.up * 0.2f);
                    }
                }
            }
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                headerStyle.normal.textColor = new Color(0.35f, 0.88f, 1f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft
                };
                bodyStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);
            }

            if (tagStyle == null)
            {
                tagStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                tagStyle.normal.textColor = new Color(1f, 0.95f, 0.45f);
            }

            if (buttonStyle == null || buttonStyle.normal.background == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ResetStyles();
            DestroyTextures();
        }
    }
}
