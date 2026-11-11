using System.Collections.Generic;
using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Constructs 3D procedural modular dungeon geometry and spawns game entities.
    /// </summary>
    public class DungeonSceneBuilder : MonoBehaviour
    {
        #region CONFIG
        public static class BUILDER_CONFIG
        {
            // Hierarchy naming
            public const string ROOT_CONTAINER_NAME = "--- GENERATED DUNGEON ---";
            public const string FLOORS_CONTAINER_NAME = "Floors";
            public const string WALLS_CONTAINER_NAME = "Walls";
            public const string ENTITIES_CONTAINER_NAME = "Entities";

            // Prefab / geometry dimensions
            public const float FLOOR_THICKNESS = 0.20f;
            public const float WALL_WIDTH_FACTOR = 1.0f;
            public const float PILLAR_SCALE = 0.35f;

            // Entity scales
            public static readonly Vector3 PLAYER_SCALE = new Vector3(0.85f, 0.85f, 0.85f);
            public static readonly Vector3 ENEMY_SCALE = new Vector3(0.80f, 0.80f, 0.80f);
            public static readonly Vector3 ITEM_SCALE = new Vector3(0.50f, 0.50f, 0.50f);
            public static readonly Vector3 GOAL_SCALE = new Vector3(1.20f, 1.80f, 1.20f);
        }
        #endregion

        private GameObject _dungeonRoot;
        private Transform _floorsRoot;
        private Transform _wallsRoot;
        private Transform _entitiesRoot;

        // Cached materials
        private Material _roomFloorMaterial;
        private Material _corridorFloorMaterial;
        private Material _wallMaterial;
        private Material _startPadMaterial;
        private Material _goalPadMaterial;
        private Material _goalCoreMaterial;
        private Material _enemyMaterial;
        private Material _itemMaterial;
        private Material _playerMaterial;
        private Material _playerVisorMaterial;

        public GameObject PlayerInstance { get; private set; }
        public PlayerMovement PlayerMovementInstance => PlayerInstance != null ? PlayerInstance.GetComponent<PlayerMovement>() : null;
        public GameObject GoalInstance { get; private set; }
        public List<GameObject> EnemyInstances { get; private set; } = new List<GameObject>();
        public List<GameObject> ItemInstances { get; private set; } = new List<GameObject>();

        private void InitializeMaterials()
        {
            if (_roomFloorMaterial == null)
            {
                _roomFloorMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.ROOM_FLOOR_COLOR, 0.3f);
                _corridorFloorMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.CORRIDOR_FLOOR_COLOR, 0.4f);
                _wallMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.WALL_COLOR, 0.2f);
                _startPadMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.START_PAD_COLOR, 0.8f);
                _goalPadMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.GOAL_PAD_COLOR, 0.7f);
                _goalCoreMaterial = DungeonMaterials.CreateEmissiveMaterial(
                    DungeonMaterials.MATERIAL_CONFIG.GOAL_PAD_COLOR,
                    DungeonMaterials.MATERIAL_CONFIG.GOAL_EMISSION_COLOR,
                    2.5f
                );
                _enemyMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.ENEMY_COLOR, 0.5f);
                _itemMaterial = DungeonMaterials.CreateEmissiveMaterial(
                    DungeonMaterials.MATERIAL_CONFIG.ITEM_COLOR,
                    DungeonMaterials.MATERIAL_CONFIG.ITEM_EMISSION_COLOR,
                    1.8f
                );
                _playerMaterial = DungeonMaterials.CreateLitMaterial(DungeonMaterials.MATERIAL_CONFIG.PLAYER_COLOR, 0.6f);
                _playerVisorMaterial = DungeonMaterials.CreateEmissiveMaterial(
                    DungeonMaterials.MATERIAL_CONFIG.PLAYER_VISOR_COLOR,
                    DungeonMaterials.MATERIAL_CONFIG.PLAYER_VISOR_COLOR,
                    1.5f
                );
            }
        }

        /// <summary>
        /// Clears all existing procedural dungeon geometry and entity instances.
        /// </summary>
        public void ClearDungeon()
        {
            EnemyInstances.Clear();
            ItemInstances.Clear();
            PlayerInstance = null;
            GoalInstance = null;

            if (_dungeonRoot != null)
            {
                DestroyImmediate(_dungeonRoot);
                _dungeonRoot = null;
            }

            GameObject existing = GameObject.Find(BUILDER_CONFIG.ROOT_CONTAINER_NAME);
            if (existing != null)
            {
                DestroyImmediate(existing);
            }
        }

        /// <summary>
        /// Builds complete 3D scene from grid data and placement points.
        /// </summary>
        public void BuildScene(DungeonGrid grid, DungeonPlacementData placements, DungeonConfig config)
        {
            ClearDungeon();
            InitializeMaterials();

            _dungeonRoot = new GameObject(BUILDER_CONFIG.ROOT_CONTAINER_NAME);
            _floorsRoot = new GameObject(BUILDER_CONFIG.FLOORS_CONTAINER_NAME).transform;
            _wallsRoot = new GameObject(BUILDER_CONFIG.WALLS_CONTAINER_NAME).transform;
            _entitiesRoot = new GameObject(BUILDER_CONFIG.ENTITIES_CONTAINER_NAME).transform;

            _floorsRoot.SetParent(_dungeonRoot.transform);
            _wallsRoot.SetParent(_dungeonRoot.transform);
            _entitiesRoot.SetParent(_dungeonRoot.transform);

            float tileSize = grid.TileSize;
            float wallHeight = config.wallHeight;

            // 1. Build Floor tiles and Wall blocks
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    CellType cell = grid.GetCell(x, y);

                    if (cell == CellType.RoomFloor || cell == CellType.CorridorFloor)
                    {
                        CreateFloorTile(grid, x, y, tileSize, cell);
                    }
                    else if (cell == CellType.Wall)
                    {
                        CreateWallBlock(grid, x, y, tileSize, wallHeight);
                    }
                }
            }

            // 2. Build Start pad & Player
            Vector3 startWorldPos = grid.GridToWorldPosition(placements.StartPosition, 0.05f);
            CreateStartPad(startWorldPos, tileSize);
            SpawnPlayer(startWorldPos);

            // 3. Build Goal monument
            Vector3 goalWorldPos = grid.GridToWorldPosition(placements.GoalPosition, 0.05f);
            SpawnGoal(goalWorldPos, tileSize);

            // 4. Spawn Enemies
            foreach (Vector2Int enemyPos in placements.EnemyPositions)
            {
                Vector3 enemyWorldPos = grid.GridToWorldPosition(enemyPos, 0.05f);
                SpawnEnemy(enemyWorldPos, grid, enemyPos);
            }

            // 5. Spawn Items
            foreach (Vector2Int itemPos in placements.ItemPositions)
            {
                Vector3 itemWorldPos = grid.GridToWorldPosition(itemPos, 0.70f);
                SpawnItem(itemWorldPos);
            }
        }

        private void CreateFloorTile(DungeonGrid grid, int x, int y, float tileSize, CellType cell)
        {
            Vector3 pos = grid.GridToWorldPosition(x, y, -BUILDER_CONFIG.FLOOR_THICKNESS * 0.5f);
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Floor_{x}_{y}";
            tile.transform.position = pos;
            tile.transform.localScale = new Vector3(tileSize, BUILDER_CONFIG.FLOOR_THICKNESS, tileSize);
            tile.transform.SetParent(_floorsRoot);

            MeshRenderer mr = tile.GetComponent<MeshRenderer>();
            mr.sharedMaterial = (cell == CellType.RoomFloor) ? _roomFloorMaterial : _corridorFloorMaterial;
        }

        private void CreateWallBlock(DungeonGrid grid, int x, int y, float tileSize, float wallHeight)
        {
            Vector3 pos = grid.GridToWorldPosition(x, y, wallHeight * 0.5f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{x}_{y}";
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(tileSize * BUILDER_CONFIG.WALL_WIDTH_FACTOR, wallHeight, tileSize * BUILDER_CONFIG.WALL_WIDTH_FACTOR);
            wall.transform.SetParent(_wallsRoot);

            MeshRenderer mr = wall.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _wallMaterial;
        }

        private void CreateStartPad(Vector3 worldPos, float tileSize)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "StartPad";
            pad.transform.position = worldPos;
            pad.transform.localScale = new Vector3(tileSize * 0.8f, 0.04f, tileSize * 0.8f);
            pad.transform.SetParent(_entitiesRoot);

            Collider col = pad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            MeshRenderer mr = pad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _startPadMaterial;
        }

        private void SpawnPlayer(Vector3 worldPos)
        {
            PlayerInstance = new GameObject("Player");
            PlayerInstance.tag = "Player";
            PlayerInstance.transform.position = worldPos;
            PlayerInstance.transform.SetParent(_entitiesRoot);

            // Add CharacterController and PlayerMovement
            CharacterController cc = PlayerInstance.AddComponent<CharacterController>();
            PlayerMovement pm = PlayerInstance.AddComponent<PlayerMovement>();

            // Visual Model Child
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(PlayerInstance.transform);
            visual.transform.localPosition = new Vector3(0.0f, 0.8f, 0.0f);
            visual.transform.localScale = BUILDER_CONFIG.PLAYER_SCALE;

            Collider capsuleCol = visual.GetComponent<Collider>();
            if (capsuleCol != null) DestroyImmediate(capsuleCol);

            MeshRenderer mr = visual.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _playerMaterial;

            // Directional Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(visual.transform);
            visor.transform.localPosition = new Vector3(0.0f, 0.35f, 0.40f);
            visor.transform.localScale = new Vector3(0.60f, 0.25f, 0.35f);

            Collider visorCol = visor.GetComponent<Collider>();
            if (visorCol != null) DestroyImmediate(visorCol);

            MeshRenderer visorMr = visor.GetComponent<MeshRenderer>();
            visorMr.sharedMaterial = _playerVisorMaterial;
        }

        private void SpawnGoal(Vector3 worldPos, float tileSize)
        {
            GoalInstance = new GameObject("GoalShrine");
            GoalInstance.transform.position = worldPos;
            GoalInstance.transform.SetParent(_entitiesRoot);

            // Base pedestal
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(GoalInstance.transform);
            pedestal.transform.localPosition = new Vector3(0.0f, 0.15f, 0.0f);
            pedestal.transform.localScale = new Vector3(tileSize * 0.9f, 0.15f, tileSize * 0.9f);
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = _goalPadMaterial;

            // Floating rotating crystal core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "CrystalCore";
            core.transform.SetParent(GoalInstance.transform);
            core.transform.localPosition = new Vector3(0.0f, 1.20f, 0.0f);
            core.transform.localRotation = Quaternion.Euler(45.0f, 45.0f, 45.0f);
            core.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            core.GetComponent<MeshRenderer>().sharedMaterial = _goalCoreMaterial;

            Collider coreCol = core.GetComponent<Collider>();
            if (coreCol != null) DestroyImmediate(coreCol);

            // Beacon Point Light
            GameObject lightObj = new GameObject("GoalLight");
            lightObj.transform.SetParent(GoalInstance.transform);
            lightObj.transform.localPosition = new Vector3(0.0f, 1.5f, 0.0f);
            Light beaconLight = lightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = DungeonMaterials.MATERIAL_CONFIG.GOAL_EMISSION_COLOR;
            beaconLight.range = 8.0f;
            beaconLight.intensity = 2.5f;

            // Trigger Collider and Script
            BoxCollider trigger = GoalInstance.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(tileSize * 0.9f, 2.5f, tileSize * 0.9f);
            trigger.center = new Vector3(0.0f, 1.25f, 0.0f);

            GoalInstance.AddComponent<DungeonGoalTrigger>();
        }

        private void SpawnEnemy(Vector3 worldPos, DungeonGrid grid, Vector2Int spawnCell)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = $"Enemy_{spawnCell.x}_{spawnCell.y}";
            enemy.transform.position = worldPos + new Vector3(0.0f, 0.8f, 0.0f);
            enemy.transform.localScale = BUILDER_CONFIG.ENEMY_SCALE;
            enemy.transform.SetParent(_entitiesRoot);

            MeshRenderer mr = enemy.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _enemyMaterial;

            // Eye marker to show heading
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "Eye";
            eye.transform.SetParent(enemy.transform);
            eye.transform.localPosition = new Vector3(0.0f, 0.45f, 0.40f);
            eye.transform.localScale = new Vector3(0.5f, 0.2f, 0.3f);
            DestroyImmediate(eye.GetComponent<Collider>());
            eye.GetComponent<MeshRenderer>().sharedMaterial = _playerVisorMaterial;

            // Patrol AI
            EnemyPatrolAI ai = enemy.AddComponent<EnemyPatrolAI>();
            ai.Initialize(grid, spawnCell);

            EnemyInstances.Add(enemy);
        }

        private void SpawnItem(Vector3 worldPos)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "CollectibleItem";
            item.transform.position = worldPos;
            item.transform.rotation = Quaternion.Euler(45.0f, 45.0f, 45.0f);
            item.transform.localScale = BUILDER_CONFIG.ITEM_SCALE;
            item.transform.SetParent(_entitiesRoot);

            MeshRenderer mr = item.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _itemMaterial;

            // Point Light
            GameObject lightObj = new GameObject("ItemLight");
            lightObj.transform.SetParent(item.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light itemLight = lightObj.AddComponent<Light>();
            itemLight.type = LightType.Point;
            itemLight.color = DungeonMaterials.MATERIAL_CONFIG.ITEM_EMISSION_COLOR;
            itemLight.range = 3.5f;
            itemLight.intensity = 1.2f;

            // Trigger and script
            BoxCollider col = item.GetComponent<BoxCollider>();
            col.isTrigger = true;

            item.AddComponent<CollectibleItem>();
            ItemInstances.Add(item);
        }
    }
}
