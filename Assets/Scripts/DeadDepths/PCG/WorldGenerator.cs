using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DeadDepths.AI;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Master procedural generator that creates an interconnected multi-cluster world
    /// with corridors, forest boundaries, detailed cabins, loot crates, obstacles, NavMesh, and zombies.
    /// ASD-STE100: This module creates the cluster-based outdoor world and all map entities.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        #region CONFIG
        [SerializeField] private MapGenerationConfig mapConfig = new MapGenerationConfig();
        public MapGenerationConfig MapConfig => mapConfig;

        [Header("Materials")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material mudMaterial;
        [SerializeField] private Material barkMaterial;
        [SerializeField] private Material leafMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material cabinFloorMaterial;
        [SerializeField] private Material crateMaterial;
        [SerializeField] private Material helicopterMaterial;
        [SerializeField] private Material helipadMaterial;
        [SerializeField] private Material blockerMaterial;
        [SerializeField] private Material unbreakableBlockerMaterial;

        public Material GroundMaterial => groundMaterial;
        public Material MudMaterial => mudMaterial;
        public Material BarkMaterial => barkMaterial;
        public Material LeafMaterial => leafMaterial;
        public Material WallMaterial => wallMaterial;
        public Material CabinFloorMaterial => cabinFloorMaterial;
        public Material CrateMaterial => crateMaterial;
        public Material HelicopterMaterial => helicopterMaterial;
        public Material HelipadMaterial => helipadMaterial;
        public Material BlockerMaterial => blockerMaterial;
        public Material UnbreakableBlockerMaterial => unbreakableBlockerMaterial;

        [Header("Prefabs / Templates")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject medkitItemPrefab;
        [SerializeField] private GameObject bandageItemPrefab;
        [SerializeField] private GameObject firstAidItemPrefab;

        [Header("Bundled Shaders (Standalone Safety)")]
        [SerializeField] private Shader bundledLitShader;
        [SerializeField] private Shader bundledParticleUnlitShader;
        [SerializeField] private Shader bundledUnlitShader;
        #endregion

        public static WorldGenerator Instance { get; private set; }

        private CellType[,] grid;
        private Transform worldRoot;
        private NavMeshRuntimeBaker navBaker;

        private readonly List<ClusterInfo> clusters = new List<ClusterInfo>();
        private readonly List<CorridorEdge> corridors = new List<CorridorEdge>();
        private readonly List<Material> transientMaterials = new List<Material>();
        private readonly List<Renderer> treeRenderers = new List<Renderer>();

        public Vector3 PlayerSpawnPosition { get; private set; }
        public Vector3 ExtractionPosition { get; private set; }
        public int TotalZombiesSpawned { get; private set; }
        public Transform WorldRoot => worldRoot;

        public int LastSeed { get; private set; }
        public int GridWidth => mapConfig.GridWidth;
        public int GridHeight => mapConfig.GridHeight;
        public IReadOnlyList<ClusterInfo> Clusters => clusters;
        public IReadOnlyList<CorridorEdge> Corridors => corridors;
        public IReadOnlyList<Renderer> TreeRenderers => treeRenderers;
        public int StartClusterId { get; private set; } = -1;
        public int EvacuationClusterId { get; private set; } = -1;
        public int TotalTreesSpawned { get; private set; }
        public int TotalStructuresSpawned { get; private set; }
        public int TotalWallsSpawned { get; private set; }
        public int TotalBlockersSpawned { get; private set; }
        public int BreakableBlockersSpawned { get; private set; }
        public int TotalCratesSpawned { get; private set; }
        public int TotalMudPatchesSpawned { get; private set; }
        public bool ConnectivityValid { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public CellType GetCell(int x, int y)
        {
            if (grid == null || x < 0 || x >= mapConfig.GridWidth || y < 0 || y >= mapConfig.GridHeight)
                return CellType.TreeBoundary;
            return grid[x, y];
        }

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            int gx = Mathf.RoundToInt(worldPos.x / Mathf.Max(0.01f, mapConfig.CellSize));
            int gy = Mathf.RoundToInt(worldPos.z / Mathf.Max(0.01f, mapConfig.CellSize));
            return new Vector2Int(
                Mathf.Clamp(gx, 0, mapConfig.GridWidth - 1),
                Mathf.Clamp(gy, 0, mapConfig.GridHeight - 1));
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            navBaker = GetComponent<NavMeshRuntimeBaker>();
            if (navBaker == null) navBaker = gameObject.AddComponent<NavMeshRuntimeBaker>();
            InitializeDefaultMaterials();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CleanupTransientMaterials();
        }

        private void CleanupTransientMaterials()
        {
            for (int i = 0; i < transientMaterials.Count; i++)
            {
                if (transientMaterials[i] != null)
                {
                    if (Application.isPlaying) Destroy(transientMaterials[i]);
                    else DestroyImmediate(transientMaterials[i]);
                }
            }
            transientMaterials.Clear();
        }

        private Material CreateTransientLitMaterial(Color c, float smoothness = 0.25f)
        {
            Shader litShader = bundledLitShader != null
                ? bundledLitShader
                : (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
            if (litShader == null) return null;
            Material m = new Material(litShader) { hideFlags = HideFlags.HideAndDontSave, color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            transientMaterials.Add(m);
            return m;
        }

        private void InitializeDefaultMaterials()
        {
            if (groundMaterial == null)
            {
                groundMaterial = Resources.Load<Material>("Ground_Grass") ?? CreateTransientLitMaterial(new Color(0.24f, 0.35f, 0.22f));
            }
            if (mudMaterial == null)
            {
                mudMaterial = CreateTransientLitMaterial(new Color(0.22f, 0.16f, 0.10f), 0.8f);
            }
            if (barkMaterial == null)
            {
                barkMaterial = CreateTransientLitMaterial(new Color(0.26f, 0.18f, 0.12f));
            }
            if (leafMaterial == null)
            {
                leafMaterial = CreateTransientLitMaterial(new Color(0.12f, 0.28f, 0.15f));
            }
            if (wallMaterial == null)
            {
                wallMaterial = CreateTransientLitMaterial(new Color(0.35f, 0.35f, 0.38f));
            }
            if (cabinFloorMaterial == null)
            {
                cabinFloorMaterial = crateMaterial != null ? crateMaterial : CreateTransientLitMaterial(new Color(0.30f, 0.22f, 0.14f));
            }
            if (crateMaterial == null)
            {
                crateMaterial = CreateTransientLitMaterial(new Color(0.48f, 0.34f, 0.20f));
            }
            if (helicopterMaterial == null)
            {
                helicopterMaterial = CreateTransientLitMaterial(new Color(0.18f, 0.24f, 0.19f));
            }
            if (helipadMaterial == null)
            {
                helipadMaterial = CreateTransientLitMaterial(new Color(0.28f, 0.30f, 0.33f), 0.2f);
            }
            if (blockerMaterial == null)
            {
                blockerMaterial = CreateTransientLitMaterial(new Color(0.42f, 0.40f, 0.38f));
            }
            if (unbreakableBlockerMaterial == null)
            {
                unbreakableBlockerMaterial = blockerMaterial;
            }
        }

        public void GenerateWorld(int levelNumber, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            if (navBaker == null) navBaker = GetComponent<NavMeshRuntimeBaker>();
            CleanupTransientMaterials();
            InitializeDefaultMaterials();

            if (ddaProfile == null)
            {
                ddaProfile = new DifficultyController.LevelDifficultyProfile();
            }

            if (worldRoot != null)
            {
                worldRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(worldRoot.gameObject);
                else DestroyImmediate(worldRoot.gameObject);
            }

            worldRoot = new GameObject($"GeneratedWorld_Level_{levelNumber}").transform;

            ZombieController.ClearStateChangeLog();
            clusters.Clear();
            corridors.Clear();
            treeRenderers.Clear();
            StartClusterId = -1;
            EvacuationClusterId = -1;
            TotalTreesSpawned = 0;
            TotalStructuresSpawned = 0;
            TotalWallsSpawned = 0;
            TotalBlockersSpawned = 0;
            BreakableBlockersSpawned = 0;
            TotalCratesSpawned = 0;
            TotalMudPatchesSpawned = 0;
            TotalZombiesSpawned = 0;
            ConnectivityValid = false;

            LastSeed = mapConfig.UseRandomSeed
                ? UnityEngine.Random.Range(1000, 999999)
                : mapConfig.Seed + levelNumber * 101;
            UnityEngine.Random.InitState(LastSeed);

            int w = mapConfig.GridWidth;
            int h = mapConfig.GridHeight;
            grid = new CellType[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    grid[x, y] = CellType.TreeBoundary;
                }
            }

            CreateTerrainGround(w, h);
            GenerateClustersAndCorridors(w, h);

            AssignStartAndEvacuationClusters(out Vector2Int startCell, out Vector2Int extractionCell);
            ReserveArea(startCell, 3, CellType.PlayerStart);
            ReserveArea(extractionCell, 4, CellType.ExtractionZone);

            PlayerSpawnPosition = CellToWorld(startCell.x, startCell.y);
            ExtractionPosition = CellToWorld(extractionCell.x, extractionCell.y);

            CreateMudPatches(w, h, startCell, extractionCell);

            List<Vector2Int> structureDoorways = new List<Vector2Int>();
            List<Vector2Int> crateCells = new List<Vector2Int>();
            CreateAbandonedStructures(w, h, structureDoorways, crateCells);

            if (crateCells.Count < 3)
            {
                PlaceFallbackCratesInClusters(crateCells, 3);
            }

            CreateBlockingObjects(w, h, ddaProfile.BlockingObjectsCount);

            List<Vector2Int> criticalPoints = new List<Vector2Int>(structureDoorways);
            criticalPoints.AddRange(crateCells);
            ConnectivityValid = WorldValidator.ValidateConnectivity(grid, w, h, startCell, extractionCell, criticalPoints);
            if (!ConnectivityValid)
            {
                Debug.LogWarning("[WorldGenerator] Connectivity check failed; carving direct fallback corridor.");
                CarveCorridor(startCell, extractionCell, Mathf.Max(1, mapConfig.CorridorHalfWidth));
                ConnectivityValid = WorldValidator.ValidateConnectivity(grid, w, h, startCell, extractionCell, criticalPoints);
            }

            CreateClusterTreeForest(w, h);
            CreateEnvironmentScatter(w, h);
            SpawnSuppliesCrates(crateCells, ddaProfile);
            CreateExtractionHelicopter(ExtractionPosition);

            Physics.SyncTransforms();
            if (navBaker != null)
            {
                navBaker.BuildNavMesh();
            }

            SpawnPlayerActor(PlayerSpawnPosition);
            SpawnZombies(levelNumber, w, h, startCell, extractionCell, ddaProfile);

            Debug.Log($"[WorldGenerator] Level {levelNumber} generated | Seed={LastSeed} | Clusters={clusters.Count} | Corridors={corridors.Count} | Trees={TotalTreesSpawned} | Walls={TotalWallsSpawned} | Blockers={TotalBlockersSpawned} | Crates={TotalCratesSpawned} | Zombies={TotalZombiesSpawned} | Connected={ConnectivityValid}");
        }

        private void GenerateClustersAndCorridors(int w, int h)
        {
            int minC = Mathf.Clamp(mapConfig.MinClusters, 3, 8);
            int maxC = Mathf.Clamp(mapConfig.MaxClusters, minC, 10);
            int targetCount = UnityEngine.Random.Range(minC, maxC + 1);

            int thickness = mapConfig.BoundaryTreeThickness;
            int minR = Mathf.Max(4, mapConfig.ClusterMinRadius);
            int maxR = Mathf.Max(minR, mapConfig.ClusterMaxRadius);
            int margin = thickness + maxR + 1;

            for (int i = 0; i < targetCount; i++)
            {
                int rx = UnityEngine.Random.Range(minR, maxR + 1);
                int ry = UnityEngine.Random.Range(minR, maxR + 1);
                Vector2Int chosenCenter = Vector2Int.zero;
                bool found = false;

                for (int attempt = 0; attempt < 80; attempt++)
                {
                    int cx = UnityEngine.Random.Range(margin, Mathf.Max(margin + 1, w - margin));
                    int cy = UnityEngine.Random.Range(margin, Mathf.Max(margin + 1, h - margin));
                    Vector2Int candidate = new Vector2Int(cx, cy);

                    bool tooClose = false;
                    float minDistRequired = attempt < 50 ? 15f : 11f;
                    foreach (var other in clusters)
                    {
                        float avgRad = (rx + ry + other.RadiusX + other.RadiusY) * 0.25f;
                        float req = Mathf.Max(minDistRequired, avgRad * 1.65f);
                        if (Vector2Int.Distance(candidate, other.Center) < req)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        chosenCenter = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    float angle = (i / (float)targetCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                    float spreadX = (w * 0.5f - margin) * 0.75f;
                    float spreadY = (h * 0.5f - margin) * 0.75f;
                    chosenCenter = new Vector2Int(
                        Mathf.Clamp(Mathf.RoundToInt(w * 0.5f + Mathf.Cos(angle) * spreadX), margin, w - margin - 1),
                        Mathf.Clamp(Mathf.RoundToInt(h * 0.5f + Mathf.Sin(angle) * spreadY), margin, h - margin - 1));
                }

                var cluster = new ClusterInfo
                {
                    Id = i,
                    Center = chosenCenter,
                    RadiusX = rx,
                    RadiusY = ry,
                    NoisePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    Role = ClusterRole.Normal
                };
                clusters.Add(cluster);
                CarveOrganicCluster(cluster, w, h, thickness);
            }

            if (clusters.Count > 1)
            {
                bool[] inTree = new bool[clusters.Count];
                inTree[0] = true;
                int edgesAdded = 0;

                while (edgesAdded < clusters.Count - 1)
                {
                    float bestDist = float.MaxValue;
                    int bestFrom = -1;
                    int bestTo = -1;

                    for (int a = 0; a < clusters.Count; a++)
                    {
                        if (!inTree[a]) continue;
                        for (int b = 0; b < clusters.Count; b++)
                        {
                            if (inTree[b]) continue;
                            float d = Vector2Int.Distance(clusters[a].Center, clusters[b].Center);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                bestFrom = a;
                                bestTo = b;
                            }
                        }
                    }

                    if (bestFrom >= 0 && bestTo >= 0)
                    {
                        inTree[bestTo] = true;
                        corridors.Add(new CorridorEdge(bestFrom, bestTo));
                        CarveCorridor(clusters[bestFrom].Center, clusters[bestTo].Center, mapConfig.CorridorHalfWidth);
                        edgesAdded++;
                    }
                    else
                    {
                        break;
                    }
                }

                for (int e = 0; e < mapConfig.ExtraLoopCorridors && clusters.Count >= 4; e++)
                {
                    int a = UnityEngine.Random.Range(0, clusters.Count);
                    int b = (a + UnityEngine.Random.Range(2, clusters.Count)) % clusters.Count;
                    if (a != b && !HasCorridorEdge(a, b))
                    {
                        corridors.Add(new CorridorEdge(a, b));
                        CarveCorridor(clusters[a].Center, clusters[b].Center, mapConfig.CorridorHalfWidth);
                    }
                }
            }
        }

        private bool HasCorridorEdge(int a, int b)
        {
            foreach (var edge in corridors)
            {
                if ((edge.FromClusterId == a && edge.ToClusterId == b) ||
                    (edge.FromClusterId == b && edge.ToClusterId == a))
                {
                    return true;
                }
            }
            return false;
        }

        private void CarveOrganicCluster(ClusterInfo cluster, int w, int h, int thickness)
        {
            int pad = 2;
            for (int x = cluster.Center.x - cluster.RadiusX - pad; x <= cluster.Center.x + cluster.RadiusX + pad; x++)
            {
                for (int y = cluster.Center.y - cluster.RadiusY - pad; y <= cluster.Center.y + cluster.RadiusY + pad; y++)
                {
                    if (x < thickness || x >= w - thickness || y < thickness || y >= h - thickness)
                        continue;

                    float dx = (x - cluster.Center.x) / (float)Mathf.Max(1, cluster.RadiusX);
                    float dy = (y - cluster.Center.y) / (float)Mathf.Max(1, cluster.RadiusY);
                    float normDist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float organicEdge = 1.0f + 0.14f * Mathf.Sin(angle * 3f + cluster.NoisePhase) + 0.08f * Mathf.Cos(angle * 5f);

                    if (normDist <= organicEdge)
                    {
                        grid[x, y] = CellType.WalkableGround;
                    }
                }
            }
        }

        private void CarveCorridor(Vector2Int from, Vector2Int to, int halfWidth)
        {
            int thickness = mapConfig.BoundaryTreeThickness;
            int w = mapConfig.GridWidth;
            int h = mapConfig.GridHeight;

            bool horizontalFirst = UnityEngine.Random.value < 0.5f;
            Vector2Int elbow = horizontalFirst ? new Vector2Int(to.x, from.y) : new Vector2Int(from.x, to.y);

            CarveSegment(from, elbow, halfWidth, thickness, w, h);
            CarveSegment(elbow, to, halfWidth, thickness, w, h);
        }

        private void CarveSegment(Vector2Int a, Vector2Int b, int halfWidth, int thickness, int w, int h)
        {
            int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
            if (steps == 0)
            {
                CarveBrush(a.x, a.y, halfWidth, thickness, w, h);
                return;
            }

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                int cx = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
                int cy = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
                CarveBrush(cx, cy, halfWidth, thickness, w, h);
            }
        }

        private void CarveBrush(int cx, int cy, int halfWidth, int thickness, int w, int h)
        {
            for (int ox = -halfWidth; ox <= halfWidth; ox++)
            {
                for (int oy = -halfWidth; oy <= halfWidth; oy++)
                {
                    int gx = cx + ox;
                    int gy = cy + oy;
                    if (gx >= thickness && gx < w - thickness && gy >= thickness && gy < h - thickness)
                    {
                        if (grid[gx, gy] == CellType.TreeBoundary)
                        {
                            grid[gx, gy] = CellType.WalkableGround;
                        }
                    }
                }
            }
        }

        private void AssignStartAndEvacuationClusters(out Vector2Int startCell, out Vector2Int extractionCell)
        {
            if (clusters.Count == 0)
            {
                startCell = new Vector2Int(8, 8);
                extractionCell = new Vector2Int(mapConfig.GridWidth - 10, mapConfig.GridHeight - 10);
                return;
            }

            if (clusters.Count == 1)
            {
                clusters[0].Role = ClusterRole.Start;
                StartClusterId = 0;
                EvacuationClusterId = 0;
                startCell = clusters[0].Center;
                extractionCell = clusters[0].Center;
                return;
            }

            int bestStart = UnityEngine.Random.Range(0, clusters.Count);
            int bestEvac = 0;
            float maxDist = -1f;

            for (int i = 0; i < clusters.Count; i++)
            {
                if (i == bestStart) continue;
                float d = Vector2Int.Distance(clusters[bestStart].Center, clusters[i].Center);
                if (d > maxDist)
                {
                    maxDist = d;
                    bestEvac = i;
                }
            }

            clusters[bestStart].Role = ClusterRole.Start;
            clusters[bestEvac].Role = ClusterRole.Evacuation;
            StartClusterId = clusters[bestStart].Id;
            EvacuationClusterId = clusters[bestEvac].Id;

            startCell = clusters[bestStart].Center;
            extractionCell = clusters[bestEvac].Center;
        }

        private void CreateTerrainGround(int w, int h)
        {
            GameObject ground = new GameObject("PlayableGround");
            ground.transform.SetParent(worldRoot, false);

            float totalWidth = w * mapConfig.CellSize;
            float totalHeight = h * mapConfig.CellSize;
            ground.transform.position = new Vector3(totalWidth * 0.5f, 0f, totalHeight * 0.5f);

            var mf = ground.AddComponent<MeshFilter>();
            var mr = ground.AddComponent<MeshRenderer>();
            mr.sharedMaterial = groundMaterial;

            Mesh plane = new Mesh { name = "GroundMesh" };
            plane.vertices = new Vector3[]
            {
                new Vector3(-totalWidth * 0.5f, 0f, -totalHeight * 0.5f),
                new Vector3(totalWidth * 0.5f, 0f, -totalHeight * 0.5f),
                new Vector3(totalWidth * 0.5f, 0f, totalHeight * 0.5f),
                new Vector3(-totalWidth * 0.5f, 0f, totalHeight * 0.5f)
            };
            plane.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            plane.uv = new Vector2[] { new Vector2(0, 0), new Vector2(totalWidth * 0.1f, 0), new Vector2(totalWidth * 0.1f, totalHeight * 0.1f), new Vector2(0, totalHeight * 0.1f) };
            plane.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            plane.RecalculateBounds();
            mf.sharedMesh = plane;

            var boxCol = ground.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(totalWidth, 0.2f, totalHeight);
            boxCol.center = new Vector3(0f, -0.1f, 0f);
        }

        private void CreateClusterTreeForest(int w, int h)
        {
            GameObject boundaryGroup = new GameObject("TreeBoundary");
            boundaryGroup.transform.SetParent(worldRoot, false);

            Mesh treeMesh = ProceduralGeometry.CreateTreeMesh();

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (grid[x, y] != CellType.TreeBoundary) continue;

                    bool isOuterRim = (x < 2 || x >= w - 2 || y < 2 || y >= h - 2);
                    bool nearWalkable = false;

                    if (!isOuterRim)
                    {
                        for (int ox = -3; ox <= 3 && !nearWalkable; ox++)
                        {
                            for (int oy = -3; oy <= 3; oy++)
                            {
                                int nx = x + ox;
                                int ny = y + oy;
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h && grid[nx, ny] != CellType.TreeBoundary)
                                {
                                    nearWalkable = true;
                                    break;
                                }
                            }
                        }
                    }

                    bool sparseDeepTree = !isOuterRim && !nearWalkable && ((x + y) % 3 == 0 && (x * 7 + y * 13) % 5 == 0);
                    if (!isOuterRim && !nearWalkable && !sparseDeepTree) continue;

                    Vector3 basePos = CellToWorld(x, y);
                    Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0f, UnityEngine.Random.Range(-0.4f, 0.4f));

                    GameObject tree = new GameObject($"BoundaryTree_{x}_{y}");
                    tree.transform.SetParent(boundaryGroup.transform, false);
                    tree.transform.position = basePos + jitter;
                    tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    float scale = UnityEngine.Random.Range(0.85f, 1.35f);
                    tree.transform.localScale = Vector3.one * scale;

                    var mf = tree.AddComponent<MeshFilter>();
                    mf.sharedMesh = treeMesh;
                    var mr = tree.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { barkMaterial, leafMaterial };
                    treeRenderers.Add(mr);

                    var col = tree.AddComponent<CapsuleCollider>();
                    col.radius = 0.45f;
                    col.height = 5.0f;
                    col.center = new Vector3(0f, 2.5f, 0f);

                    TotalTreesSpawned++;
                }
            }
        }

        private void CreateEnvironmentScatter(int w, int h)
        {
            GameObject scatterGroup = new GameObject("EnvScatter");
            scatterGroup.transform.SetParent(worldRoot, false);

            Material stoneMat = CreateTransientLitMaterial(new Color(0.28f, 0.29f, 0.31f), 0.15f);
            Material tuftMat = CreateTransientLitMaterial(new Color(0.14f, 0.24f, 0.13f), 0.08f);

            for (int x = mapConfig.BoundaryTreeThickness; x < w - mapConfig.BoundaryTreeThickness; x++)
            {
                for (int y = mapConfig.BoundaryTreeThickness; y < h - mapConfig.BoundaryTreeThickness; y++)
                {
                    if (grid[x, y] != CellType.WalkableGround) continue;
                    if (UnityEngine.Random.value > 0.08f) continue;

                    Vector3 basePos = CellToWorld(x, y) + new Vector3(UnityEngine.Random.Range(-0.75f, 0.75f), 0f, UnityEngine.Random.Range(-0.75f, 0.75f));
                    bool isRock = UnityEngine.Random.value < 0.35f;

                    GameObject prop;
                    if (isRock)
                    {
                        prop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        prop.name = "EnvRock";
                        prop.transform.localScale = new Vector3(UnityEngine.Random.Range(0.22f, 0.45f), UnityEngine.Random.Range(0.12f, 0.24f), UnityEngine.Random.Range(0.22f, 0.42f));
                        prop.transform.position = basePos + Vector3.up * 0.06f;
                        prop.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    }
                    else
                    {
                        prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        prop.name = "EnvGrassTuft";
                        prop.transform.localScale = new Vector3(0.14f, UnityEngine.Random.Range(0.18f, 0.35f), 0.14f);
                        prop.transform.position = basePos + Vector3.up * 0.12f;
                        prop.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-12f, 12f));
                        prop.GetComponent<Renderer>().sharedMaterial = tuftMat;
                    }

                    prop.transform.SetParent(scatterGroup.transform, true);
                    var col = prop.GetComponent<Collider>();
                    if (col != null)
                    {
                        if (Application.isPlaying) Destroy(col);
                        else DestroyImmediate(col);
                    }
                }
            }
        }

        private void CreateMudPatches(int w, int h, Vector2Int startCell, Vector2Int extractionCell)
        {
            GameObject mudGroup = new GameObject("MuddyPatches");
            mudGroup.transform.SetParent(worldRoot, false);

            int placed = 0;
            for (int attempt = 0; attempt < mapConfig.MudPatchCount * 10 && placed < mapConfig.MudPatchCount; attempt++)
            {
                if (clusters.Count == 0) break;
                var cluster = clusters[UnityEngine.Random.Range(0, clusters.Count)];
                if (cluster.Role == ClusterRole.Start) continue;

                int ox = UnityEngine.Random.Range(-cluster.RadiusX / 2, cluster.RadiusX / 2 + 1);
                int oy = UnityEngine.Random.Range(-cluster.RadiusY / 2, cluster.RadiusY / 2 + 1);
                int cx = Mathf.Clamp(cluster.Center.x + ox, 4, w - 5);
                int cy = Mathf.Clamp(cluster.Center.y + oy, 4, h - 5);

                if (grid[cx, cy] != CellType.WalkableGround) continue;

                int radius = UnityEngine.Random.Range(mapConfig.MudPatchMinRadius, mapConfig.MudPatchMaxRadius + 1);
                if (Vector2Int.Distance(new Vector2Int(cx, cy), startCell) < radius + 4f ||
                    Vector2Int.Distance(new Vector2Int(cx, cy), extractionCell) < radius + 4f)
                {
                    continue;
                }
                float worldRadX = radius * mapConfig.CellSize;
                float worldRadZ = radius * mapConfig.CellSize;

                Vector3 centerPos = CellToWorld(cx, cy);

                GameObject patchObj = new GameObject($"MudPatch_{placed}");
                patchObj.transform.SetParent(mudGroup.transform, false);
                patchObj.transform.position = centerPos;

                var mf = patchObj.AddComponent<MeshFilter>();
                mf.sharedMesh = ProceduralGeometry.CreateMudPatchMesh(worldRadX, worldRadZ);
                var mr = patchObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mudMaterial;

                var patchCollider = patchObj.AddComponent<BoxCollider>();
                patchCollider.isTrigger = true;
                patchCollider.size = new Vector3(worldRadX * 2f, 1.2f, worldRadZ * 2f);
                patchCollider.center = new Vector3(0f, 0.5f, 0f);

                patchObj.AddComponent<MuddyPatch>();

                for (int gx = cx - radius; gx <= cx + radius; gx++)
                {
                    for (int gy = cy - radius; gy <= cy + radius; gy++)
                    {
                        if (gx >= 0 && gx < w && gy >= 0 && gy < h)
                        {
                            if (grid[gx, gy] == CellType.WalkableGround)
                            {
                                if (Vector2.Distance(new Vector2(gx, gy), new Vector2(cx, cy)) <= radius)
                                {
                                    grid[gx, gy] = CellType.MudPatch;
                                }
                            }
                        }
                    }
                }

                placed++;
                TotalMudPatchesSpawned++;
            }
        }

        private void CreateAbandonedStructures(int w, int h, List<Vector2Int> doorways, List<Vector2Int> crateCells)
        {
            GameObject structGroup = new GameObject("AbandonedStructures");
            structGroup.transform.SetParent(worldRoot, false);

            Material trimMat = CreateTransientLitMaterial(new Color(0.20f, 0.16f, 0.12f), 0.15f);

            List<ClusterInfo> candidateClusters = new List<ClusterInfo>();
            foreach (var c in clusters)
            {
                if (c.Role == ClusterRole.Normal) candidateClusters.Add(c);
            }
            if (candidateClusters.Count == 0) candidateClusters.AddRange(clusters);

            int maxAttempts = mapConfig.AbandonedStructureCount * 15;
            for (int attempt = 0; attempt < maxAttempts && TotalStructuresSpawned < mapConfig.AbandonedStructureCount; attempt++)
            {
                var cluster = candidateClusters[attempt % candidateClusters.Count];
                int sw = UnityEngine.Random.Range(mapConfig.StructureMinWidth, mapConfig.StructureMaxWidth);
                int sh = UnityEngine.Random.Range(mapConfig.StructureMinHeight, mapConfig.StructureMaxHeight);

                int sx = cluster.Center.x + UnityEngine.Random.Range(-cluster.RadiusX + 1, Mathf.Max(-cluster.RadiusX + 2, cluster.RadiusX - sw));
                int sy = cluster.Center.y + UnityEngine.Random.Range(-cluster.RadiusY + 1, Mathf.Max(-cluster.RadiusY + 2, cluster.RadiusY - sh));

                if (sx < mapConfig.BoundaryTreeThickness + 1 || sy < mapConfig.BoundaryTreeThickness + 1 ||
                    sx + sw >= w - mapConfig.BoundaryTreeThickness - 1 || sy + sh >= h - mapConfig.BoundaryTreeThickness - 1)
                {
                    continue;
                }

                bool validSpot = true;
                for (int x = sx - 1; x <= sx + sw + 1 && validSpot; x++)
                {
                    for (int y = sy - 1; y <= sy + sh + 1; y++)
                    {
                        CellType cell = grid[x, y];
                        if (cell == CellType.TreeBoundary ||
                            cell == CellType.PlayerStart ||
                            cell == CellType.ExtractionZone ||
                            cell == CellType.StructureWall ||
                            cell == CellType.StructureFloor ||
                            cell == CellType.StructureDoorway)
                        {
                            validSpot = false;
                            break;
                        }
                    }
                }
                if (!validSpot) continue;

                Vector2 structCenter = new Vector2(sx + sw * 0.5f, sy + sh * 0.5f);
                Vector2 toClusterCenter = (Vector2)cluster.Center - structCenter;
                int doorSide;
                if (Mathf.Abs(toClusterCenter.y) >= Mathf.Abs(toClusterCenter.x))
                    doorSide = toClusterCenter.y >= 0 ? 1 : 0;
                else
                    doorSide = toClusterCenter.x >= 0 ? 3 : 2;

                int doorPos = (doorSide == 0 || doorSide == 1) ? sx + (sw / 2) : sy + (sh / 2);

                GameObject cabin = new GameObject($"Structure_{TotalStructuresSpawned}");
                cabin.transform.SetParent(structGroup.transform, false);

                // Wood floor plank pad inside cabin
                Vector3 floorCenter = CellToWorld(sx, sy) + new Vector3(sw * mapConfig.CellSize * 0.5f, 0.02f, sh * mapConfig.CellSize * 0.5f);
                GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObj.name = "CabinWoodFloor";
                floorObj.transform.SetParent(cabin.transform, false);
                floorObj.transform.position = floorCenter;
                floorObj.transform.localScale = new Vector3((sw + 0.5f) * mapConfig.CellSize, 0.04f, (sh + 0.5f) * mapConfig.CellSize);
                floorObj.GetComponent<Renderer>().sharedMaterial = cabinFloorMaterial;
                var floorCol = floorObj.GetComponent<Collider>();
                if (floorCol != null)
                {
                    if (Application.isPlaying) Destroy(floorCol);
                    else DestroyImmediate(floorCol);
                }

                // Warm lantern light inside cabin so abandoned structures invite exploration in the night
                GameObject lanternGo = new GameObject("CabinLantern");
                lanternGo.transform.SetParent(cabin.transform, false);
                lanternGo.transform.position = floorCenter + Vector3.up * 2.4f;
                var lantern = lanternGo.AddComponent<Light>();
                lantern.type = LightType.Point;
                lantern.color = new Color(1.0f, 0.72f, 0.36f);
                lantern.intensity = 3.2f;
                lantern.range = 8.5f;
                lantern.shadows = LightShadows.None;

                for (int x = sx; x <= sx + sw; x++)
                {
                    for (int y = sy; y <= sy + sh; y++)
                    {
                        bool isEdge = (x == sx || x == sx + sw || y == sy || y == sy + sh);
                        if (isEdge)
                        {
                            bool isDoor = (doorSide == 0 && y == sy && x == doorPos) ||
                                          (doorSide == 1 && y == sy + sh && x == doorPos) ||
                                          (doorSide == 2 && x == sx && y == doorPos) ||
                                          (doorSide == 3 && x == sx + sw && y == doorPos);

                            if (isDoor)
                            {
                                grid[x, y] = CellType.StructureDoorway;
                                doorways.Add(new Vector2Int(x, y));
                            }
                            else
                            {
                                grid[x, y] = CellType.StructureWall;
                                BuildWallBlock(cabin.transform, CellToWorld(x, y), trimMat);
                                TotalWallsSpawned++;
                            }
                        }
                        else
                        {
                            grid[x, y] = CellType.StructureFloor;
                        }
                    }
                }

                int placed = 0;
                for (int cx = sx + 1; cx < sx + sw && placed < mapConfig.CratesPerStructure; cx++)
                {
                    for (int cy = sy + 1; cy < sy + sh && placed < mapConfig.CratesPerStructure; cy++)
                    {
                        if (grid[cx, cy] == CellType.StructureFloor)
                        {
                            grid[cx, cy] = CellType.SuppliesCrate;
                            crateCells.Add(new Vector2Int(cx, cy));
                            placed++;
                        }
                    }
                }

                TotalStructuresSpawned++;
            }
        }

        private void PlaceFallbackCratesInClusters(List<Vector2Int> crateCells, int targetCount)
        {
            foreach (var c in clusters)
            {
                if (crateCells.Count >= targetCount) break;
                if (c.Role == ClusterRole.Start) continue;

                int rx = Mathf.Max(2, c.RadiusX - 1);
                int ry = Mathf.Max(2, c.RadiusY - 1);
                for (int ox = -rx; ox <= rx && crateCells.Count < targetCount; ox += 2)
                {
                    for (int oy = -ry; oy <= ry && crateCells.Count < targetCount; oy += 2)
                    {
                        int gx = c.Center.x + ox;
                        int gy = c.Center.y + oy;
                        if (gx >= 0 && gx < mapConfig.GridWidth && gy >= 0 && gy < mapConfig.GridHeight)
                        {
                            if (grid[gx, gy] == CellType.WalkableGround)
                            {
                                grid[gx, gy] = CellType.SuppliesCrate;
                                crateCells.Add(new Vector2Int(gx, gy));
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void BuildWallBlock(Transform parent, Vector3 pos, Material trimMat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "StructureWall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos + new Vector3(0f, 1.6f, 0f);
            wall.transform.localScale = new Vector3(mapConfig.CellSize, 3.2f, mapConfig.CellSize);

            var mr = wall.GetComponent<Renderer>();
            mr.sharedMaterial = wallMaterial;
            wall.layer = LayerMask.NameToLayer("Default");

            // Roofline trim cap added as child object so parent Renderer material contract stays intact
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "WallTrimCap";
            cap.transform.SetParent(wall.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            cap.transform.localScale = new Vector3(1.06f, 0.06f, 1.06f);
            cap.GetComponent<Renderer>().sharedMaterial = trimMat;
            var capCol = cap.GetComponent<Collider>();
            if (capCol != null)
            {
                if (Application.isPlaying) Destroy(capCol);
                else DestroyImmediate(capCol);
            }
        }

        private void CreateBlockingObjects(int w, int h, int count)
        {
            GameObject blockerGroup = new GameObject("BlockingObjects");
            blockerGroup.transform.SetParent(worldRoot, false);

            Material plankMat = CreateTransientLitMaterial(new Color(0.45f, 0.30f, 0.16f), 0.15f);
            Material capMat = CreateTransientLitMaterial(new Color(0.24f, 0.26f, 0.28f), 0.35f);

            int maxAttempts = count * 25;
            for (int attempt = 0; attempt < maxAttempts && TotalBlockersSpawned < count; attempt++)
            {
                if (clusters.Count == 0) break;
                var cluster = clusters[UnityEngine.Random.Range(0, clusters.Count)];

                int bx = cluster.Center.x + UnityEngine.Random.Range(-cluster.RadiusX + 1, cluster.RadiusX);
                int by = cluster.Center.y + UnityEngine.Random.Range(-cluster.RadiusY + 1, cluster.RadiusY);

                if (bx <= mapConfig.BoundaryTreeThickness || bx >= w - mapConfig.BoundaryTreeThickness - 1 ||
                    by <= mapConfig.BoundaryTreeThickness || by >= h - mapConfig.BoundaryTreeThickness - 1)
                    continue;

                if (grid[bx, by] != CellType.WalkableGround && grid[bx, by] != CellType.MudPatch)
                    continue;

                if (!HasOpenWalkableNeighbors(bx, by, w, h)) continue;

                grid[bx, by] = CellType.BlockingObject;

                bool isBreakable = UnityEngine.Random.value < 0.65f;
                Vector3 worldPos = CellToWorld(bx, by);

                GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = isBreakable ? "Breakable_Barricade" : "Unbreakable_Pillar";
                blocker.transform.SetParent(blockerGroup.transform, false);

                if (isBreakable)
                {
                    blocker.transform.position = worldPos + new Vector3(0f, 0.75f, 0f);
                    blocker.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    BreakableBlockersSpawned++;

                    // Cross-plank child details (no collider, parent Renderer remains blockerMaterial)
                    CreateDetailChildCube(blocker.transform, "BarricadePlank_1", new Vector3(0f, 0.15f, 0.52f), new Vector3(1.1f, 0.14f, 0.08f), new Vector3(0f, 0f, 14f), plankMat);
                    CreateDetailChildCube(blocker.transform, "BarricadePlank_2", new Vector3(0f, -0.15f, -0.52f), new Vector3(1.1f, 0.14f, 0.08f), new Vector3(0f, 0f, -12f), plankMat);
                }
                else
                {
                    blocker.transform.position = worldPos + new Vector3(0f, 1.5f, 0f);
                    blocker.transform.localScale = new Vector3(1.4f, 3.0f, 1.4f);

                    CreateDetailChildCube(blocker.transform, "PillarTopCap", new Vector3(0f, 0.52f, 0f), new Vector3(1.14f, 0.08f, 1.14f), Vector3.zero, capMat);
                    CreateDetailChildCube(blocker.transform, "PillarBaseTrim", new Vector3(0f, -0.47f, 0f), new Vector3(1.12f, 0.08f, 1.12f), Vector3.zero, capMat);
                }

                var mr = blocker.GetComponent<Renderer>();
                mr.sharedMaterial = isBreakable
                    ? blockerMaterial
                    : (unbreakableBlockerMaterial != null ? unbreakableBlockerMaterial : blockerMaterial);

                var comp = blocker.AddComponent<BlockingObject>();
                comp.BlockerConfig.IsBreakable = isBreakable;
                TotalBlockersSpawned++;
            }
        }

        private static void CreateDetailChildCube(Transform parent, string name, Vector3 localPos, Vector3 localScale, Vector3 localEuler, Material mat)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPos;
            child.transform.localScale = localScale;
            child.transform.localEulerAngles = localEuler;
            child.GetComponent<Renderer>().sharedMaterial = mat;
            var col = child.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private bool HasOpenWalkableNeighbors(int x, int y, int w, int h)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) return false;
                    CellType t = grid[nx, ny];
                    if (t != CellType.WalkableGround && t != CellType.MudPatch)
                        return false;
                }
            }
            return true;
        }

        private void SpawnSuppliesCrates(List<Vector2Int> crateCells, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            GameObject crateGroup = new GameObject("SuppliesCrates");
            crateGroup.transform.SetParent(worldRoot, false);

            Mesh crateMesh = ProceduralGeometry.CreateCrateMesh();
            Mesh lidMesh = ProceduralGeometry.CreateCrateLidMesh();
            int healingSuppliesRemaining = ddaProfile.HealingSuppliesCount;

            foreach (var cell in crateCells)
            {
                Vector3 pos = CellToWorld(cell.x, cell.y);

                GameObject crateObj = new GameObject("SuppliesCrate");
                crateObj.transform.SetParent(crateGroup.transform, false);
                crateObj.transform.position = pos;

                var mf = crateObj.AddComponent<MeshFilter>();
                mf.sharedMesh = crateMesh;
                var mr = crateObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = crateMaterial;

                // Hinged Lid child object
                GameObject lidObj = new GameObject("Lid");
                lidObj.transform.SetParent(crateObj.transform, false);
                lidObj.transform.localPosition = new Vector3(0f, 1.0f, -0.60f);
                var lidMf = lidObj.AddComponent<MeshFilter>();
                lidMf.sharedMesh = lidMesh;
                var lidMr = lidObj.AddComponent<MeshRenderer>();
                lidMr.sharedMaterial = crateMaterial;

                var col = crateObj.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.0f, 1.2f);
                col.center = new Vector3(0f, 0.5f, 0f);

                var crateComp = crateObj.AddComponent<SuppliesCrate>();
                crateComp.SetLootPreviewPrefabs(medkitItemPrefab, bandageItemPrefab, firstAidItemPrefab);

                List<CrateLootItem> loot = new List<CrateLootItem>();

                if (healingSuppliesRemaining > 0)
                {
                    loot.Add(new CrateLootItem("Medkit", 1, true));
                    healingSuppliesRemaining--;
                }
                else
                {
                    loot.Add(new CrateLootItem("Bandage", 2, true));
                }

                if (UnityEngine.Random.value < ddaProfile.LootQualityFactor)
                {
                    loot.Add(new CrateLootItem("Rifle Ammo", UnityEngine.Random.Range(15, 30)));
                }
                else
                {
                    loot.Add(new CrateLootItem("Pistol Ammo", UnityEngine.Random.Range(8, 16)));
                }

                loot.Add(new CrateLootItem("Scrap Metal", UnityEngine.Random.Range(2, 6)));
                crateComp.ConfigureLoot(loot);
                TotalCratesSpawned++;
            }
        }

        private static Texture2D cachedSmokeTexture;

        private static Texture2D GetOrCreateSmokeTexture()
        {
            if (cachedSmokeTexture != null) return cachedSmokeTexture;

            const int res = 64;
            cachedSmokeTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2(0.5f, 0.5f);
            Color[] pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res;
                    float d = Vector2.Distance(new Vector2(u, v), center) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, a);
                }
            }

            cachedSmokeTexture.SetPixels(pixels);
            cachedSmokeTexture.Apply();
            return cachedSmokeTexture;
        }

        private void CreateExtractionHelicopter(Vector3 pos)
        {
            GameObject heliObj = new GameObject("ExtractionHelicopter");
            heliObj.transform.SetParent(worldRoot, false);
            heliObj.transform.position = pos;

            var mf = heliObj.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralGeometry.CreateHelicopterMesh();
            var mr = heliObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = helicopterMaterial;

            var col = heliObj.AddComponent<BoxCollider>();
            col.size = new Vector3(3.5f, 3f, 7f);
            col.center = new Vector3(0f, 1.5f, 0f);

            // Concrete Helipad base + painted 'H' child marker
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "HelipadConcrete";
            pad.transform.SetParent(heliObj.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            pad.transform.localScale = new Vector3(13.5f, 0.04f, 13.5f);
            pad.GetComponent<Renderer>().sharedMaterial = helipadMaterial;
            var padCol = pad.GetComponent<Collider>();
            if (padCol != null)
            {
                if (Application.isPlaying) Destroy(padCol);
                else DestroyImmediate(padCol);
            }

            Material stripeMat = CreateTransientLitMaterial(new Color(0.95f, 0.78f, 0.18f), 0.2f);
            CreateDetailChildCube(heliObj.transform, "HelipadMark_L", new Vector3(-2.4f, 0.055f, 0f), new Vector3(0.45f, 0.02f, 5.2f), Vector3.zero, stripeMat);
            CreateDetailChildCube(heliObj.transform, "HelipadMark_R", new Vector3(2.4f, 0.055f, 0f), new Vector3(0.45f, 0.02f, 5.2f), Vector3.zero, stripeMat);
            CreateDetailChildCube(heliObj.transform, "HelipadMark_C", new Vector3(0f, 0.055f, 0f), new Vector3(4.8f, 0.02f, 0.55f), Vector3.zero, stripeMat);

            // MainRotor child
            GameObject rotorObj = new GameObject("MainRotor");
            rotorObj.transform.SetParent(heliObj.transform, false);
            rotorObj.transform.localPosition = new Vector3(0f, 2.95f, 0f);
            var rotorMf = rotorObj.AddComponent<MeshFilter>();
            rotorMf.sharedMesh = ProceduralGeometry.CreateHelicopterRotorMesh();
            var rotorMr = rotorObj.AddComponent<MeshRenderer>();
            rotorMr.sharedMaterial = helicopterMaterial;

            // TailRotor child
            GameObject tailRotorObj = new GameObject("TailRotor");
            tailRotorObj.transform.SetParent(heliObj.transform, false);
            tailRotorObj.transform.localPosition = new Vector3(0.22f, 2.35f, -5.4f);
            var tailMf = tailRotorObj.AddComponent<MeshFilter>();
            tailMf.sharedMesh = ProceduralGeometry.CreateTailRotorMesh();
            var tailMr = tailRotorObj.AddComponent<MeshRenderer>();
            tailMr.sharedMaterial = helicopterMaterial;

            // Rescue green smoke flare with URP ParticleSystem plume
            GameObject flare = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flare.name = "GreenSmokeCanister";
            flare.transform.SetParent(heliObj.transform, false);
            flare.transform.localPosition = new Vector3(3.5f, 0.3f, 0f);
            flare.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var flareCol = flare.GetComponent<Collider>();
            if (flareCol != null)
            {
                if (Application.isPlaying) Destroy(flareCol);
                else DestroyImmediate(flareCol);
            }
            var flareRend = flare.GetComponent<Renderer>();
            Color smokeColor = new Color(0.12f, 1.0f, 0.40f, 0.85f);
            Material flareMat = CreateTransientLitMaterial(new Color(0.1f, 0.85f, 0.3f), 0.3f);
            flareRend.sharedMaterial = flareMat;

            var ps = flare.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = 4.0f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 9.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = smokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.14f;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 3.2f;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var forceOverLifetime = ps.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
            forceOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            forceOverLifetime.y = new ParticleSystem.MinMaxCurve(1.4f);
            forceOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.45f);
            sizeCurve.AddKey(0.25f, 0.95f);
            sizeCurve.AddKey(0.6f, 1.85f);
            sizeCurve.AddKey(1f, 3.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(smokeColor, 0f),
                    new GradientColorKey(new Color(0.2f, 1f, 0.5f), 0.4f),
                    new GradientColorKey(new Color(0.25f, 0.85f, 0.45f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.08f),
                    new GradientAlphaKey(0.85f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var psRend = flare.GetComponent<ParticleSystemRenderer>();
            if (psRend != null)
            {
                psRend.renderMode = ParticleSystemRenderMode.Billboard;
                Shader smokeShader = bundledParticleUnlitShader != null
                    ? bundledParticleUnlitShader
                    : (Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                       Shader.Find("Particles/Standard Unlit") ??
                       bundledUnlitShader ??
                       Shader.Find("Universal Render Pipeline/Unlit") ??
                       Shader.Find("Sprites/Default") ??
                       bundledLitShader ??
                       Shader.Find("Universal Render Pipeline/Lit"));
                if (smokeShader != null)
                {
                    Material smokeMat = new Material(smokeShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    smokeMat.color = smokeColor;
                    if (smokeMat.HasProperty("_BaseColor")) smokeMat.SetColor("_BaseColor", smokeColor);
                    if (smokeMat.HasProperty("_Surface")) smokeMat.SetFloat("_Surface", 1f);
                    if (smokeMat.HasProperty("_Blend")) smokeMat.SetFloat("_Blend", 0f);
                    if (smokeMat.HasProperty("_SrcBlend")) smokeMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    if (smokeMat.HasProperty("_DstBlend")) smokeMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    if (smokeMat.HasProperty("_ZWrite")) smokeMat.SetFloat("_ZWrite", 0f);
                    smokeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    smokeMat.SetOverrideTag("RenderType", "Transparent");
                    smokeMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    smokeMat.mainTexture = GetOrCreateSmokeTexture();
                    transientMaterials.Add(smokeMat);
                    psRend.sharedMaterial = smokeMat;
                }
            }

            var light = flare.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 1f, 0.4f);
            light.intensity = 2.8f;
            light.range = 9.0f;

            heliObj.AddComponent<SafeZoneController>();
            heliObj.AddComponent<ExtractionHelicopter>();
        }

        private void SpawnPlayerActor(Vector3 pos)
        {
            var existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null)
            {
                var cc = existing.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                existing.transform.position = pos + Vector3.up * 0.1f;
                existing.transform.rotation = Quaternion.identity;
                Physics.SyncTransforms();

                var playerCtrl = existing.GetComponent<DeadDepthsPlayerController>();
                if (playerCtrl != null)
                {
                    playerCtrl.ResetStateForLevel();
                }
                else if (cc != null)
                {
                    cc.enabled = true;
                }

                if (IsometricCameraController.Instance != null)
                {
                    IsometricCameraController.Instance.SnapToTarget(existing.transform);
                }
                return;
            }

            GameObject spawned;
            if (playerPrefab != null)
            {
                spawned = Instantiate(playerPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
            }
            else
            {
                spawned = new GameObject("Player");
                spawned.tag = "Player";
                spawned.transform.position = pos + Vector3.up * 0.1f;

                var cc = spawned.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.radius = 0.4f;

                spawned.AddComponent<DeadDepthsPlayerController>();
                spawned.AddComponent<DeadDepthsFlashlight>();

                var playerModelPrefab = Resources.Load<GameObject>("PlayerChar") ?? FindModelAsset("PlayerChar");
                if (playerModelPrefab != null)
                {
                    var visual = Instantiate(playerModelPrefab, spawned.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    var visualAnim = visual.GetComponentInChildren<Animator>();
                    if (visualAnim != null)
                    {
                        visualAnim.applyRootMotion = false;
                        if (visualAnim.GetComponent<PlayerAimIK>() == null) visualAnim.gameObject.AddComponent<PlayerAimIK>();
                    }
                }
                else
                {
                    var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    capsule.transform.SetParent(spawned.transform, false);
                    capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    if (Application.isPlaying) Destroy(capsule.GetComponent<Collider>());
                    else DestroyImmediate(capsule.GetComponent<Collider>());
                }
            }

            Physics.SyncTransforms();

            var spawnedCtrl = spawned.GetComponent<DeadDepthsPlayerController>();
            if (spawnedCtrl != null)
            {
                spawnedCtrl.ResetStateForLevel();
            }

            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.SnapToTarget(spawned.transform);
            }
        }

        private void SpawnZombies(int levelNumber, int w, int h, Vector2Int startCell, Vector2Int extractionCell, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            GameObject hordeGroup = new GameObject("ZombieHorde");
            hordeGroup.transform.SetParent(worldRoot, false);

            int walkers, spitters, chargers;
            var diff = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (diff != null)
            {
                diff.CalculateEnemyCountsForLevel(ddaProfile, levelNumber, out walkers, out spitters, out chargers);
            }
            else
            {
                int lvl = Mathf.Max(1, levelNumber);
                int total = lvl == 1 ? 4 : Mathf.Clamp(4 + (lvl - 1), 3, 26);
                spitters = Mathf.RoundToInt(total * ddaProfile.SpitterProportion);
                chargers = Mathf.RoundToInt(total * ddaProfile.ChargerProportion);
                walkers = Mathf.Max(1, total - spitters - chargers);
            }

            int totalDesired = walkers + spitters + chargers;
            List<Vector2Int> validCells = CollectValidZombieCells(w, h, startCell, extractionCell, 11f);
            if (validCells.Count < totalDesired)
            {
                validCells = CollectValidZombieCells(w, h, startCell, extractionCell, 8f);
            }

            for (int i = validCells.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                Vector2Int tmp = validCells[i];
                validCells[i] = validCells[j];
                validCells[j] = tmp;
            }

            int cursor = 0;
            cursor = SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Walker, walkers, validCells, cursor);
            cursor = SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Spitter, spitters, validCells, cursor);
            SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Charger, chargers, validCells, cursor);

            if (PlayerPerformanceTracker.Instance != null)
            {
                var player = DeadDepthsPlayerController.Instance;
                float hp = player != null ? player.CurrentHealth : 100f;
                float maxHp = player != null ? player.MaxHealth : 100f;
                PlayerPerformanceTracker.Instance.StartNewLevel(hp, maxHp, TotalZombiesSpawned);
            }
        }

        private List<Vector2Int> CollectValidZombieCells(int w, int h, Vector2Int startCell, Vector2Int extractionCell, float minStartDist)
        {
            List<Vector2Int> validCells = new List<Vector2Int>();
            for (int x = mapConfig.BoundaryTreeThickness; x < w - mapConfig.BoundaryTreeThickness; x++)
            {
                for (int y = mapConfig.BoundaryTreeThickness; y < h - mapConfig.BoundaryTreeThickness; y++)
                {
                    CellType c = grid[x, y];
                    if (c != CellType.WalkableGround && c != CellType.MudPatch) continue;
                    if (Vector2Int.Distance(new Vector2Int(x, y), startCell) < minStartDist) continue;
                    if (Vector2Int.Distance(new Vector2Int(x, y), extractionCell) < 3f) continue;
                    validCells.Add(new Vector2Int(x, y));
                }
            }
            return validCells;
        }

        private int SpawnArchetypeGroup(Transform parent, ZombieArchetype arch, int count, List<Vector2Int> validCells, int cellCursor)
        {
            if (validCells.Count == 0) return cellCursor;

            for (int i = 0; i < count; i++)
            {
                Vector2Int cell = validCells[cellCursor % validCells.Count];
                cellCursor++;

                Vector3 spawnPos = CellToWorld(cell.x, cell.y);
                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                {
                    spawnPos = navHit.position;
                }

                GameObject zObj;
                if (zombiePrefab != null)
                {
                    zObj = Instantiate(zombiePrefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), parent);
                    zObj.name = $"Zombie_{arch}_{i}";
                }
                else
                {
                    zObj = new GameObject($"Zombie_{arch}_{i}");
                    zObj.transform.SetParent(parent, false);
                    zObj.transform.position = spawnPos;

                    var agent = zObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
                    agent.radius = 0.45f;
                    agent.height = 1.8f;

                    var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    capsule.transform.SetParent(zObj.transform, false);
                    capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    capsule.transform.localScale = new Vector3(0.85f, 0.9f, 0.85f);
                }

                var spawnedAgent = zObj.GetComponent<NavMeshAgent>();
                if (spawnedAgent != null && spawnedAgent.enabled)
                {
                    spawnedAgent.Warp(spawnPos);
                }

                var ctrl = zObj.GetComponent<ZombieController>();
                if (ctrl == null) ctrl = zObj.AddComponent<ZombieController>();

                ctrl.InitializeArchetype(arch);
                TotalZombiesSpawned++;
            }

            return cellCursor;
        }

        private GameObject FindModelAsset(string modelName)
        {
            return Resources.Load<GameObject>(modelName);
        }

        private void ReserveArea(Vector2Int center, int radius, CellType type)
        {
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (x >= mapConfig.BoundaryTreeThickness && x < mapConfig.GridWidth - mapConfig.BoundaryTreeThickness &&
                        y >= mapConfig.BoundaryTreeThickness && y < mapConfig.GridHeight - mapConfig.BoundaryTreeThickness)
                    {
                        grid[x, y] = type;
                    }
                }
            }
        }

        public Vector3 CellToWorld(int gx, int gy)
        {
            return new Vector3(gx * mapConfig.CellSize, 0f, gy * mapConfig.CellSize);
        }
    }
}
