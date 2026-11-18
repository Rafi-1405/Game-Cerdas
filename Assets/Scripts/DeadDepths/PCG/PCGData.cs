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

    public enum ClusterRole
    {
        Normal = 0,
        Start = 1,
        Evacuation = 2
    }

    [Serializable]
    public class ClusterInfo
    {
        public int Id;
        public Vector2Int Center;
        public int RadiusX;
        public int RadiusY;
        public float NoisePhase;
        public ClusterRole Role;
    }

    [Serializable]
    public struct CorridorEdge
    {
        public int FromClusterId;
        public int ToClusterId;

        public CorridorEdge(int fromId, int toId)
        {
            FromClusterId = fromId;
            ToClusterId = toId;
        }
    }

    [Serializable]
    public class MapGenerationConfig
    {
        [Header("Map Dimensions")]
        public int GridWidth = 64;
        public int GridHeight = 64;
        public float CellSize = 2.0f; // Each grid cell is 2x2 world units (128x128m world)

        [Header("Seed Settings")]
        public int Seed = 1337;
        public bool UseRandomSeed = true;

        [Header("Cluster & Corridor Settings")]
        public int MinClusters = 4;
        public int MaxClusters = 6;
        public int ClusterMinRadius = 6;
        public int ClusterMaxRadius = 9;
        public int CorridorHalfWidth = 1; // 3-cell wide corridors (6m)
        public int ExtraLoopCorridors = 1;

        [Header("Environment Densities")]
        public int BoundaryTreeThickness = 3;
        public int MudPatchCount = 6;
        public int MudPatchMinRadius = 2;
        public int MudPatchMaxRadius = 4;

        public int AbandonedStructureCount = 4;
        public int StructureMinWidth = 5;
        public int StructureMaxWidth = 7;
        public int StructureMinHeight = 5;
        public int StructureMaxHeight = 7;

        public int CratesPerStructure = 2;
        public float SafeZoneRadius = 7.0f;
    }
}
