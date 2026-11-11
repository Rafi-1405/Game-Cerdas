using System.Collections.Generic;
using DungeonGenerator.Core;
using DungeonGenerator.Entities;
using DungeonGenerator.Generation;
using DungeonGenerator.Validation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGenerator.Tests
{
    /// <summary>
    /// Automated test suite validating BSP generation, seed determinism, BFS reachability, and safe zones.
    /// </summary>
    [TestFixture]
    public class DungeonGeneratorTests
    {
        #region CONFIG
        public static class TEST_CONFIG
        {
            public const int TEST_SEED_A = 77123;
            public const int TEST_SEED_B = 99451;
            public const int DEFAULT_WIDTH = 48;
            public const int DEFAULT_HEIGHT = 48;
            public const int SAFE_ZONE_RADIUS = 4;
        }
        #endregion

        private DungeonConfig CreateStandardConfig(int seed)
        {
            DungeonConfig config = new DungeonConfig
            {
                mapWidth = TEST_CONFIG.DEFAULT_WIDTH,
                mapHeight = TEST_CONFIG.DEFAULT_HEIGHT,
                seed = seed,
                useRandomSeed = false,
                minRoomWidth = 6,
                minRoomHeight = 6,
                maxRoomWidth = 14,
                maxRoomHeight = 14,
                corridorWidth = 1,
                safeZoneRadius = TEST_CONFIG.SAFE_ZONE_RADIUS,
                enemyCount = 6,
                itemCount = 8
            };
            config.Validate();
            return config;
        }

        [Test]
        public void Test_SameSeed_ProducesIdenticalLayout()
        {
            // Arrange
            DungeonConfig config1 = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            DungeonConfig config2 = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);

            DungeonBSPGenerator generator1 = new DungeonBSPGenerator(config1);
            DungeonBSPGenerator generator2 = new DungeonBSPGenerator(config2);

            // Act
            DungeonGrid grid1 = generator1.Generate(TEST_CONFIG.TEST_SEED_A);
            DungeonGrid grid2 = generator2.Generate(TEST_CONFIG.TEST_SEED_A);

            // Assert
            Assert.AreEqual(grid1.Rooms.Count, grid2.Rooms.Count, "Room counts must be identical for identical seed.");
            Assert.AreEqual(grid1.WalkableCells.Count, grid2.WalkableCells.Count, "Walkable cell counts must match.");

            for (int i = 0; i < grid1.Rooms.Count; i++)
            {
                Assert.AreEqual(grid1.Rooms[i], grid2.Rooms[i], $"Room {i} must match exact bounds.");
            }
        }

        [Test]
        public void Test_DifferentSeed_ProducesDifferentLayout()
        {
            // Arrange
            DungeonConfig configA = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            DungeonConfig configB = CreateStandardConfig(TEST_CONFIG.TEST_SEED_B);

            DungeonBSPGenerator genA = new DungeonBSPGenerator(configA);
            DungeonBSPGenerator genB = new DungeonBSPGenerator(configB);

            // Act
            DungeonGrid gridA = genA.Generate(TEST_CONFIG.TEST_SEED_A);
            DungeonGrid gridB = genB.Generate(TEST_CONFIG.TEST_SEED_B);

            // Assert
            bool hasDifference = (gridA.Rooms.Count != gridB.Rooms.Count) ||
                                 (gridA.WalkableCells.Count != gridB.WalkableCells.Count);

            if (!hasDifference && gridA.Rooms.Count > 0)
            {
                hasDifference = gridA.Rooms[0] != gridB.Rooms[0];
            }

            Assert.IsTrue(hasDifference, "Different seeds must generate varied layouts.");
        }

        [Test]
        public void Test_BFS_StartToGoal_IsAlwaysReachable()
        {
            // Arrange
            DungeonConfig config = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            DungeonBSPGenerator generator = new DungeonBSPGenerator(config);
            DungeonGrid grid = generator.Generate(TEST_CONFIG.TEST_SEED_A);

            System.Random rng = new System.Random(TEST_CONFIG.TEST_SEED_A);
            DungeonPlacementData placements = DungeonEntityPlacer.ResolvePlacements(grid, config, rng);

            // Act
            ValidationReport report = DungeonValidator.Validate(
                grid,
                placements.StartPosition,
                placements.GoalPosition,
                placements.ItemPositions
            );

            // Assert
            Assert.IsTrue(report.IsGoalReachable, "Goal must be reachable from Start position.");
            Assert.Greater(report.PathLength, 0, "BFS shortest path must have valid steps.");
            Assert.IsTrue(report.AreItemsReachable, "All placed items must be reachable.");
            Assert.IsTrue(report.IsFullyValid, "Dungeon validation report must pass completely.");
        }

        [Test]
        public void Test_SafeZone_HasNoEnemies()
        {
            // Arrange
            DungeonConfig config = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            DungeonBSPGenerator generator = new DungeonBSPGenerator(config);
            DungeonGrid grid = generator.Generate(TEST_CONFIG.TEST_SEED_A);

            System.Random rng = new System.Random(TEST_CONFIG.TEST_SEED_A);
            DungeonPlacementData placements = DungeonEntityPlacer.ResolvePlacements(grid, config, rng);

            // Assert
            foreach (Vector2Int enemyPos in placements.EnemyPositions)
            {
                float distance = Vector2.Distance(enemyPos, placements.StartPosition);
                Assert.Greater(distance, config.safeZoneRadius, $"Enemy at {enemyPos} must be outside safe zone radius of {config.safeZoneRadius}.");
            }
        }

        [Test]
        public void Test_Entities_PlacedOnValidFloors()
        {
            // Arrange
            DungeonConfig config = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            DungeonBSPGenerator generator = new DungeonBSPGenerator(config);
            DungeonGrid grid = generator.Generate(TEST_CONFIG.TEST_SEED_A);

            System.Random rng = new System.Random(TEST_CONFIG.TEST_SEED_A);
            DungeonPlacementData placements = DungeonEntityPlacer.ResolvePlacements(grid, config, rng);

            // Assert
            Assert.IsTrue(grid.IsWalkable(placements.StartPosition), "Start position must be on walkable floor.");
            Assert.IsTrue(grid.IsWalkable(placements.GoalPosition), "Goal position must be on walkable floor.");

            foreach (Vector2Int enemy in placements.EnemyPositions)
            {
                Assert.IsTrue(grid.IsWalkable(enemy), $"Enemy at {enemy} must be on walkable floor.");
            }

            foreach (Vector2Int item in placements.ItemPositions)
            {
                Assert.IsTrue(grid.IsWalkable(item), $"Item at {item} must be on walkable floor.");
            }
        }

        [Test]
        public void Test_CorridorWidth_ParameterImpact()
        {
            // Arrange
            DungeonConfig configNarrow = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            configNarrow.corridorWidth = 1;

            DungeonConfig configWide = CreateStandardConfig(TEST_CONFIG.TEST_SEED_A);
            configWide.corridorWidth = 3;

            DungeonBSPGenerator genNarrow = new DungeonBSPGenerator(configNarrow);
            DungeonBSPGenerator genWide = new DungeonBSPGenerator(configWide);

            // Act
            DungeonGrid gridNarrow = genNarrow.Generate(TEST_CONFIG.TEST_SEED_A);
            DungeonGrid gridWide = genWide.Generate(TEST_CONFIG.TEST_SEED_A);

            // Assert
            Assert.Greater(gridWide.WalkableCells.Count, gridNarrow.WalkableCells.Count, "Wider corridor must produce more floor cells.");
        }
    }
}
