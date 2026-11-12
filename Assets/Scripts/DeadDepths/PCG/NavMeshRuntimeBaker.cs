using System;
using UnityEngine;
using Unity.AI.Navigation;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Bakes the Unity NavMesh at runtime after procedural generation.
    /// ASD-STE100: This module builds navigation data for zombie pathfinding.
    /// </summary>
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private NavMeshSurface surface;

        public void BuildNavMesh()
        {
            if (surface == null)
            {
                surface = GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    surface = gameObject.AddComponent<NavMeshSurface>();
                }
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }
    }
}
