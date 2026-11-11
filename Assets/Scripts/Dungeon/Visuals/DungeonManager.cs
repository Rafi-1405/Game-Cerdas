using System;
using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using DungeonGenerator.Generation;
using DungeonGenerator.UI;
using DungeonGenerator.Validation;
using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Master coordinator for procedural dungeon generation, validation, scene building, and gameplay lifecycle.
    /// </summary>
    [RequireComponent(typeof(DungeonSceneBuilder))]
    public class DungeonManager : MonoBehaviour
    {
        #region CONFIG
        public static class MANAGER_CONFIG
        {
            // Default seed on awake
            public const int INITIAL_DEFAULT_SEED = 42819;

            // Camera bounds safety margin
            public const float CAMERA_SPAN_PADDING = 1.15f;
        }
        #endregion

        public static DungeonManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private DungeonConfig _config = new DungeonConfig();

        [Header("Component References")]
        [SerializeField] private DungeonSceneBuilder _sceneBuilder;
        [SerializeField] private DungeonCameraController _cameraController;
        [SerializeField] private DungeonMinimapUI _minimapUI;
        [SerializeField] private DungeonGizmos _gizmos;
        [SerializeField] private DungeonHUD _hud;

        // Runtime states
        public DungeonConfig Config => _config;
        public DungeonGrid CurrentGrid { get; private set; }
        public DungeonPlacementData CurrentPlacements { get; private set; }
        public ValidationReport CurrentValidationReport { get; private set; }
        public DungeonCameraController CameraController => _cameraController;

        public bool IsGoalReached { get; private set; }
        public int CollectedItemsCount { get; private set; }
        public int TotalItemsCount { get; private set; }
        public float PlayTimer { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ResolveComponents();
        }

        private void Start()
        {
            GenerateDungeon();
        }

        private void Update()
        {
            if (!IsGoalReached)
            {
                PlayTimer += Time.deltaTime;
            }
        }

        private void ResolveComponents()
        {
            if (_sceneBuilder == null) _sceneBuilder = GetComponent<DungeonSceneBuilder>();
            if (_cameraController == null) _cameraController = FindAnyObjectByType<DungeonCameraController>();
            if (_minimapUI == null) _minimapUI = GetComponent<DungeonMinimapUI>();
            if (_gizmos == null) _gizmos = GetComponent<DungeonGizmos>();
            if (_hud == null) _hud = GetComponent<DungeonHUD>();
        }

        /// <summary>
        /// Generates a complete procedural dungeon with BSP partitioning and reachability validation.
        /// </summary>
        public void GenerateDungeon(int? seedOverride = null)
        {
            ResolveComponents();
            _config.Validate();

            int initialSeed = seedOverride ?? (_config.useRandomSeed ? UnityEngine.Random.Range(10000, 999999) : _config.seed);
            _config.seed = initialSeed;

            int retryCount = 0;
            bool generationSuccess = false;
            int activeSeed = initialSeed;

            DungeonBSPGenerator generator = new DungeonBSPGenerator(_config);

            while (retryCount < _config.maxValidationRetries && !generationSuccess)
            {
                // 1. Generate BSP grid
                CurrentGrid = generator.Generate(activeSeed);

                // 2. Resolve entity placements
                System.Random rng = new System.Random(activeSeed);
                CurrentPlacements = DungeonEntityPlacer.ResolvePlacements(CurrentGrid, _config, rng);

                // 3. Validate connectivity using BFS
                CurrentValidationReport = DungeonValidator.Validate(
                    CurrentGrid,
                    CurrentPlacements.StartPosition,
                    CurrentPlacements.GoalPosition,
                    CurrentPlacements.ItemPositions,
                    retryCount
                );

                if (CurrentValidationReport.IsFullyValid)
                {
                    generationSuccess = true;
                    break;
                }

                // If invalid, attempt corridor repair
                bool repaired = DungeonValidator.RepairDirectCorridor(
                    CurrentGrid,
                    CurrentPlacements.StartPosition,
                    CurrentPlacements.GoalPosition
                );

                if (repaired)
                {
                    CurrentValidationReport = DungeonValidator.Validate(
                        CurrentGrid,
                        CurrentPlacements.StartPosition,
                        CurrentPlacements.GoalPosition,
                        CurrentPlacements.ItemPositions,
                        retryCount
                    );

                    if (CurrentValidationReport.IsFullyValid)
                    {
                        generationSuccess = true;
                        break;
                    }
                }

                // Increment seed for retry
                retryCount++;
                activeSeed = initialSeed + (retryCount * 7919);
            }

            _config.seed = activeSeed;

            // 4. Build 3D physical world
            _sceneBuilder.BuildScene(CurrentGrid, CurrentPlacements, _config);

            // 5. Connect Camera
            if (_cameraController != null && _sceneBuilder.PlayerInstance != null)
            {
                _cameraController.SetTarget(_sceneBuilder.PlayerInstance.transform);
                float span = Mathf.Max(CurrentGrid.Width, CurrentGrid.Height) * CurrentGrid.TileSize * MANAGER_CONFIG.CAMERA_SPAN_PADDING;
                _cameraController.SetOverviewPosition(Vector3.zero, span);
            }

            // 6. Connect Minimap
            if (_minimapUI != null)
            {
                Transform playerTransform = _sceneBuilder.PlayerInstance != null ? _sceneBuilder.PlayerInstance.transform : null;
                _minimapUI.SetData(CurrentGrid, CurrentPlacements, CurrentValidationReport, playerTransform);
            }

            // 7. Connect Gizmos
            if (_gizmos != null)
            {
                _gizmos.SetDebugData(CurrentGrid, CurrentPlacements, CurrentValidationReport);
            }

            // 8. Sync HUD
            if (_hud != null)
            {
                _hud.SyncSeedDisplay(_config.seed);
            }

            // 9. Reset gameplay status
            IsGoalReached = false;
            CollectedItemsCount = 0;
            TotalItemsCount = CurrentPlacements.ItemPositions.Count;
            PlayTimer = 0.0f;

            Debug.Log($"[DungeonManager] Generated seed: {_config.seed} | Valid: {CurrentValidationReport?.IsFullyValid} | Path length: {CurrentValidationReport?.PathLength} | Retries: {retryCount}");
        }

        /// <summary>
        /// Regenerates dungeon using current active seed to demonstrate deterministic reproducibility.
        /// </summary>
        public void RegenerateSameSeed()
        {
            GenerateDungeon(_config.seed);
        }

        /// <summary>
        /// Invoked when player reaches goal shrine trigger.
        /// </summary>
        public void OnGoalReached()
        {
            IsGoalReached = true;
            Debug.Log($"[DungeonManager] Goal reached! Time: {PlayTimer:F2}s, Items: {CollectedItemsCount}/{TotalItemsCount}");
        }

        /// <summary>
        /// Invoked when player picks up a collectible item.
        /// </summary>
        public void OnItemCollected(CollectibleItem item)
        {
            CollectedItemsCount++;
            Debug.Log($"[DungeonManager] Item collected ({CollectedItemsCount}/{TotalItemsCount})");
        }
    }
}
