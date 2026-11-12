using System;
using System.Collections.Generic;
using UnityEngine;
using DeadDepths.AI;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Master procedural generator that coordinates the 13 generation steps in Section 7.1.
    /// ASD-STE100: This module creates the continuous outdoor world and all map entities.
    /// Follows Section 7 of the specification.
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
        [SerializeField] private Material crateMaterial;
        [SerializeField] private Material helicopterMaterial;
        [SerializeField] private Material blockerMaterial;

        [Header("Prefabs / Templates")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject playerPrefab;
        #endregion

        public static WorldGenerator Instance { get; private set; }

        private CellType[,] grid;
        private Transform worldRoot;
        private NavMeshRuntimeBaker navBaker;

        public Vector3 PlayerSpawnPosition { get; private set; }
        public Vector3 ExtractionPosition { get; private set; }
        public int TotalZombiesSpawned { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            navBaker = GetComponent<NavMeshRuntimeBaker>();
            if (navBaker == null) navBaker = gameObject.AddComponent<NavMeshRuntimeBaker>();
            InitializeDefaultMaterials();
        }

        private void InitializeDefaultMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (groundMaterial == null)
            {
                groundMaterial = Resources.Load<Material>("Ground_Grass") ?? new Material(litShader) { color = new Color(0.24f, 0.35f, 0.22f) };
            }
            if (mudMaterial == null)
            {
                mudMaterial = new Material(litShader) { color = new Color(0.22f, 0.16f, 0.10f) };
                if (mudMaterial.HasProperty("_Smoothness")) mudMaterial.SetFloat("_Smoothness", 0.8f);
            }
            if (barkMaterial == null)
            {
                barkMaterial = new Material(litShader) { color = new Color(0.26f, 0.18f, 0.12f) };
            }
            if (leafMaterial == null)
            {
                leafMaterial = new Material(litShader) { color = new Color(0.12f, 0.28f, 0.15f) };
            }
            if (wallMaterial == null)
            {
                wallMaterial = new Material(litShader) { color = new Color(0.35f, 0.35f, 0.38f) };
            }
            if (crateMaterial == null)
            {
                crateMaterial = new Material(litShader) { color = new Color(0.48f, 0.34f, 0.20f) };
            }
            if (helicopterMaterial == null)
            {
                helicopterMaterial = new Material(litShader) { color = new Color(0.18f, 0.24f, 0.19f) };
            }
            if (blockerMaterial == null)
            {
                blockerMaterial = new Material(litShader) { color = new Color(0.42f, 0.40f, 0.38f) };
            }
        }

        /// <summary>
        /// Executes the full multi-stage procedural generation pipeline.
        /// </summary>
        public void GenerateWorld(int levelNumber, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            // Clean up previous world objects
            if (worldRoot != null)
            {
                Destroy(worldRoot.gameObject);
            }

            worldRoot = new GameObject($"GeneratedWorld_Level_{levelNumber}").transform;

            // 1. Initialize Seed & Grid
            int seed = mapConfig.UseRandomSeed ? UnityEngine.Random.Range(1000, 999999) : mapConfig.Seed + levelNumber;
            UnityEngine.Random.InitState(seed);

            int w = mapConfig.GridWidth;
            int h = mapConfig.GridHeight;
            grid = new CellType[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    grid[x, y] = CellType.WalkableGround;
                }
            }

            // 2. Playable Ground Surface
            CreateTerrainGround(w, h);

            // 3. Dense Outer Tree Boundary
            CreateTreeBoundary(w, h);

            // 4. Muddy Patches
            CreateMudPatches(w, h);

            // 5. Reserved Player Start & Extraction Zone
            Vector2Int startCell = new Vector2Int(mapConfig.BoundaryTreeThickness + 4, mapConfig.BoundaryTreeThickness + 4);
            Vector2Int extractionCell = new Vector2Int(w - mapConfig.BoundaryTreeThickness - 6, h - mapConfig.BoundaryTreeThickness - 6);

            ReserveArea(startCell, 4, CellType.PlayerStart);
            ReserveArea(extractionCell, 5, CellType.ExtractionZone);

            PlayerSpawnPosition = CellToWorld(startCell.x, startCell.y);
            ExtractionPosition = CellToWorld(extractionCell.x, extractionCell.y);

            // 6. Abandoned Structures
            List<Vector2Int> structureDoorways = new List<Vector2Int>();
            List<Vector2Int> crateCells = new List<Vector2Int>();
            CreateAbandonedStructures(w, h, structureDoorways, crateCells);

            // 7. Blocking Objects
            CreateBlockingObjects(w, h, ddaProfile.BlockingObjectsCount);

            // 8. Supplies Crates (placed inside structures)
            SpawnSuppliesCrates(crateCells, ddaProfile);

            // 9. Extraction Safe Zone & Helicopter
            CreateExtractionHelicopter(ExtractionPosition);

            // 10. Validate World Connectivity
            bool isValid = WorldValidator.ValidateConnectivity(grid, w, h, startCell, extractionCell, structureDoorways);
            if (!isValid)
            {
                Debug.LogWarning("[WorldGenerator] World connectivity validation warning. Clearing straight path.");
                ClearStraightCorridor(startCell, extractionCell);
            }

            // 11. NavMesh Baking
            if (navBaker != null)
            {
                navBaker.BuildNavMesh();
            }

            // 12. Spawn Player
            SpawnPlayerActor(PlayerSpawnPosition);

            // 13. Spawn Zombie Groups
            SpawnZombies(w, h, startCell, ddaProfile);
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

            // Generate plane mesh
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

        private void CreateTreeBoundary(int w, int h)
        {
            GameObject boundaryGroup = new GameObject("TreeBoundary");
            boundaryGroup.transform.SetParent(worldRoot, false);

            Mesh treeMesh = ProceduralGeometry.CreateTreeMesh();

            int thickness = mapConfig.BoundaryTreeThickness;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (x < thickness || x >= w - thickness || y < thickness || y >= h - thickness)
                    {
                        grid[x, y] = CellType.TreeBoundary;

                        // Spawn tree instances with natural jitter
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
                        mr.sharedMaterials = new Material[] { leafMaterial };

                        var col = tree.AddComponent<CapsuleCollider>();
                        col.radius = 0.55f;
                        col.height = 5.0f;
                        col.center = new Vector3(0f, 2.5f, 0f);
                    }
                }
            }
        }

        private void CreateMudPatches(int w, int h)
        {
            GameObject mudGroup = new GameObject("MuddyPatches");
            mudGroup.transform.SetParent(worldRoot, false);

            for (int i = 0; i < mapConfig.MudPatchCount; i++)
            {
                int cx = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 8, w - mapConfig.BoundaryTreeThickness - 8);
                int cy = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 8, h - mapConfig.BoundaryTreeThickness - 8);
                int radius = UnityEngine.Random.Range(mapConfig.MudPatchMinRadius, mapConfig.MudPatchMaxRadius);

                float worldRadX = radius * mapConfig.CellSize;
                float worldRadZ = radius * mapConfig.CellSize;

                Vector3 centerPos = CellToWorld(cx, cy);

                GameObject patchObj = new GameObject($"MudPatch_{i}");
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

                // Mark cells in grid
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
            }
        }

        private void CreateAbandonedStructures(int w, int h, List<Vector2Int> doorways, List<Vector2Int> crateCells)
        {
            GameObject structGroup = new GameObject("AbandonedStructures");
            structGroup.transform.SetParent(worldRoot, false);

            for (int i = 0; i < mapConfig.AbandonedStructureCount; i++)
            {
                int sw = UnityEngine.Random.Range(mapConfig.StructureMinWidth, mapConfig.StructureMaxWidth);
                int sh = UnityEngine.Random.Range(mapConfig.StructureMinHeight, mapConfig.StructureMaxHeight);

                int sx = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 4, w - mapConfig.BoundaryTreeThickness - sw - 4);
                int sy = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 4, h - mapConfig.BoundaryTreeThickness - sh - 4);

                // Check for collisions with reserved start or extraction
                bool overlaps = false;
                for (int x = sx - 1; x <= sx + sw + 1; x++)
                {
                    for (int y = sy - 1; y <= sy + sh + 1; y++)
                    {
                        if (grid[x, y] == CellType.PlayerStart || grid[x, y] == CellType.ExtractionZone || grid[x, y] == CellType.StructureWall)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (overlaps) break;
                }
                if (overlaps) continue;

                // Build structure walls with door cutout
                int doorSide = UnityEngine.Random.Range(0, 4); // 0=South, 1=North, 2=West, 3=East
                int doorPos = doorSide == 0 || doorSide == 1 ? sx + (sw / 2) : sy + (sh / 2);

                GameObject cabin = new GameObject($"Structure_{i}");
                cabin.transform.SetParent(structGroup.transform, false);

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
                                BuildWallBlock(cabin.transform, CellToWorld(x, y));
                            }
                        }
                        else
                        {
                            grid[x, y] = CellType.StructureFloor;
                        }
                    }
                }

                // Place crates inside this structure
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
            }
        }

        private void BuildWallBlock(Transform parent, Vector3 pos)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "StructureWall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos + new Vector3(0f, 1.6f, 0f);
            wall.transform.localScale = new Vector3(mapConfig.CellSize, 3.2f, mapConfig.CellSize);

            var mr = wall.GetComponent<Renderer>();
            mr.sharedMaterial = wallMaterial;

            // Obstruction see-through will fade these walls when obstructing the camera
            wall.layer = LayerMask.NameToLayer("Default");
        }

        private void CreateBlockingObjects(int w, int h, int count)
        {
            GameObject blockerGroup = new GameObject("BlockingObjects");
            blockerGroup.transform.SetParent(worldRoot, false);

            for (int i = 0; i < count; i++)
            {
                int bx = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 2, w - mapConfig.BoundaryTreeThickness - 2);
                int by = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 2, h - mapConfig.BoundaryTreeThickness - 2);

                if (grid[bx, by] == CellType.WalkableGround || grid[bx, by] == CellType.MudPatch)
                {
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
                    }
                    else
                    {
                        blocker.transform.position = worldPos + new Vector3(0f, 1.5f, 0f);
                        blocker.transform.localScale = new Vector3(1.4f, 3.0f, 1.4f);
                    }

                    var mr = blocker.GetComponent<Renderer>();
                    mr.sharedMaterial = blockerMaterial;

                    var comp = blocker.AddComponent<BlockingObject>();
                    comp.BlockerConfig.IsBreakable = isBreakable;
                }
            }
        }

        private void SpawnSuppliesCrates(List<Vector2Int> crateCells, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            GameObject crateGroup = new GameObject("SuppliesCrates");
            crateGroup.transform.SetParent(worldRoot, false);

            Mesh crateMesh = ProceduralGeometry.CreateCrateMesh();

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

                var col = crateObj.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.0f, 1.2f);
                col.center = new Vector3(0f, 0.5f, 0f);

                var crateComp = crateObj.AddComponent<SuppliesCrate>();

                // Configure DDA loot
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

                // Weapons & Resources based on LootQualityFactor
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
            }
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

            // Add rescue green smoke flare
            GameObject flare = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flare.name = "GreenSmokeCanister";
            flare.transform.SetParent(heliObj.transform, false);
            flare.transform.position = pos + new Vector3(3.5f, 0.3f, 0f);
            flare.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var flareRend = flare.GetComponent<Renderer>();
            flareRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.1f, 1f, 0.3f)
            };

            var light = flare.AddComponent<Light>();
            light.color = new Color(0.2f, 1f, 0.4f);
            light.intensity = 2.5f;
            light.range = 8.0f;

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
                if (cc != null) cc.enabled = true;
                return;
            }

            if (playerPrefab != null)
            {
                Instantiate(playerPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
            }
            else
            {
                // Fallback: Create player root with CharacterController & DeadDepthsPlayerController
                GameObject p = new GameObject("Player");
                p.tag = "Player";
                p.transform.position = pos + Vector3.up * 0.1f;

                var cc = p.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.radius = 0.4f;

                p.AddComponent<DeadDepthsPlayerController>();

                // Load PlayerChar model from CharacterUpgrade
                var playerModelPrefab = Resources.Load<GameObject>("PlayerChar") ??
                                        FindModelAsset("PlayerChar");
                if (playerModelPrefab != null)
                {
                    var visual = Instantiate(playerModelPrefab, p.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    capsule.transform.SetParent(p.transform, false);
                    capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    Destroy(capsule.GetComponent<Collider>());
                }
            }
        }

        private void SpawnZombies(int w, int h, Vector2Int startCell, DifficultyController.LevelDifficultyProfile ddaProfile)
        {
            GameObject hordeGroup = new GameObject("ZombieHorde");
            hordeGroup.transform.SetParent(worldRoot, false);

            int walkers, spitters, chargers;
            var diff = DifficultyController.Instance ?? FindAnyObjectByType<DifficultyController>();
            if (diff != null)
            {
                diff.CalculateEnemyCounts(ddaProfile, out walkers, out spitters, out chargers);
            }
            else
            {
                walkers = Mathf.RoundToInt(ddaProfile.TotalZombies * ddaProfile.WalkerProportion);
                spitters = Mathf.RoundToInt(ddaProfile.TotalZombies * ddaProfile.SpitterProportion);
                chargers = ddaProfile.TotalZombies - walkers - spitters;
            }
            TotalZombiesSpawned = walkers + spitters + chargers;

            SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Walker, walkers, w, h, startCell);
            SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Spitter, spitters, w, h, startCell);
            SpawnArchetypeGroup(hordeGroup.transform, ZombieArchetype.Charger, chargers, w, h, startCell);

            // Register total spawned with performance tracker
            if (PlayerPerformanceTracker.Instance != null)
            {
                var player = DeadDepthsPlayerController.Instance;
                float hp = player != null ? player.CurrentHealth : 100f;
                float maxHp = player != null ? player.MaxHealth : 100f;
                PlayerPerformanceTracker.Instance.StartNewLevel(hp, maxHp, TotalZombiesSpawned);
            }
        }

        private void SpawnArchetypeGroup(Transform parent, ZombieArchetype arch, int count, int w, int h, Vector2Int startCell)
        {
            for (int i = 0; i < count; i++)
            {
                int zx = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 3, w - mapConfig.BoundaryTreeThickness - 3);
                int zy = UnityEngine.Random.Range(mapConfig.BoundaryTreeThickness + 3, h - mapConfig.BoundaryTreeThickness - 3);

                // Avoid player spawn zone (minimum 12m distance)
                if (Vector2Int.Distance(new Vector2Int(zx, zy), startCell) < 8) continue;
                if (grid[zx, zy] == CellType.TreeBoundary || grid[zx, zy] == CellType.StructureWall) continue;

                Vector3 spawnPos = CellToWorld(zx, zy) + Vector3.up * 0.1f;

                GameObject zObj = null;
                if (zombiePrefab != null)
                {
                    zObj = Instantiate(zombiePrefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), parent);
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

                    var rend = capsule.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Color c = Color.gray;
                        if (arch == ZombieArchetype.Spitter) c = new Color(0.2f, 0.9f, 0.4f);
                        else if (arch == ZombieArchetype.Charger) c = new Color(0.85f, 0.25f, 0.2f);
                        rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = c };
                    }
                }

                // Add or configure ZombieController
                var ctrl = zObj.GetComponent<ZombieController>();
                if (ctrl == null) ctrl = zObj.AddComponent<ZombieController>();
                // Set archetype reflection/field
                SetZombieArchetype(ctrl, arch);

                if (arch == ZombieArchetype.Charger)
                {
                    var teleg = zObj.GetComponentInChildren<ChargerTelegraph>();
                    if (teleg == null) zObj.AddComponent<ChargerTelegraph>();
                }
            }
        }

        private void SetZombieArchetype(ZombieController ctrl, ZombieArchetype arch)
        {
            var field = typeof(ZombieController).GetField("archetype", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ctrl, arch);
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
                    if (x >= 0 && x < mapConfig.GridWidth && y >= 0 && y < mapConfig.GridHeight)
                    {
                        grid[x, y] = type;
                    }
                }
            }
        }

        private void ClearStraightCorridor(Vector2Int from, Vector2Int to)
        {
            int cx = from.x;
            int cy = from.y;
            while (cx != to.x || cy != to.y)
            {
                if (cx < to.x) cx++;
                else if (cx > to.x) cx--;

                if (cy < to.y) cy++;
                else if (cy > to.y) cy--;

                if (grid[cx, cy] == CellType.StructureWall || grid[cx, cy] == CellType.TreeBoundary)
                {
                    grid[cx, cy] = CellType.WalkableGround;
                }
            }
        }

        public Vector3 CellToWorld(int gx, int gy)
        {
            return new Vector3(gx * mapConfig.CellSize, 0f, gy * mapConfig.CellSize);
        }
    }
}
