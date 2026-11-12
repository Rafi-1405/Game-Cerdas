using System;
using UnityEngine;

namespace DeadDepths.PCG
{
    public enum CellType
    {
        Empty = 0,
        WalkableGround = 1,
        MudPatch = 2,
        TreeBoundary = 3,
        StructureWall = 4,
        StructureFloor = 5,
        StructureDoorway = 6,
        BlockingObject = 7,
        SuppliesCrate = 8,
        ZombieSpawn = 9,
        PlayerStart = 10,
        ExtractionZone = 11
    }

    [Serializable]
    public class MapGenerationConfig
    {
        [Header("Map Dimensions")]
        public int GridWidth = 60;
        public int GridHeight = 60;
        public float CellSize = 2.0f; // Each grid cell is 2x2 world units (120x120m world)

        [Header("Seed Settings")]
        public int Seed = 1337;
        public bool UseRandomSeed = true;

        [Header("Environment Densities")]
        public int BoundaryTreeThickness = 3;
        public int MudPatchCount = 8;
        public int MudPatchMinRadius = 3;
        public int MudPatchMaxRadius = 6;

        public int AbandonedStructureCount = 4;
        public int StructureMinWidth = 5;
        public int StructureMaxWidth = 8;
        public int StructureMinHeight = 5;
        public int StructureMaxHeight = 8;

        public int CratesPerStructure = 2;
        public float SafeZoneRadius = 7.0f;
    }
}
