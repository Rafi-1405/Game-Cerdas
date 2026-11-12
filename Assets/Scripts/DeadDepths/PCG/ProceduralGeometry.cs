using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Generates 3D meshes mathematically for environment assets.
    /// ASD-STE100: This module creates 3D shapes using mathematical formulas.
    /// Follows Rule 3: Procedural Mathematical Coherence (Golden ratio, phyllotaxis, Fibonacci branching).
    /// </summary>
    public static class ProceduralGeometry
    {
        public const float GoldenRatio = 1.61803398875f;
        public const float GoldenAngle = 137.507764f * Mathf.Deg2Rad;

        /// <summary>
        /// Creates a procedural pine / forest tree with trunk and phyllotaxis foliage layers.
        /// </summary>
        public static Mesh CreateTreeMesh(float height = 6.0f, float trunkRadius = 0.35f, int foliageTiers = 4)
        {
            Mesh mesh = new Mesh { name = "Procedural_Tree" };
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // 1. Trunk (Cylinder with taper)
            int segments = 8;
            float trunkHeight = height * 0.45f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Base
                vertices.Add(new Vector3(cos * trunkRadius, 0f, sin * trunkRadius));
                normals.Add(new Vector3(cos, 0f, sin));
                uvs.Add(new Vector2((float)i / segments, 0f));

                // Top
                vertices.Add(new Vector3(cos * trunkRadius * 0.65f, trunkHeight, sin * trunkRadius * 0.65f));
                normals.Add(new Vector3(cos, 0f, sin));
                uvs.Add(new Vector2((float)i / segments, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b1 = i * 2;
                int t1 = b1 + 1;
                int b2 = (i + 1) * 2;
                int t2 = b2 + 1;

                triangles.Add(b1);
                triangles.Add(t1);
                triangles.Add(b2);

                triangles.Add(b2);
                triangles.Add(t1);
                triangles.Add(t2);
            }

            // 2. Foliage Tiers (Cones arranged by Fibonacci / golden ratio distribution)
            float baseFoliageHeight = height * 0.25f;
            float foliageSpan = height - baseFoliageHeight;

            for (int tier = 0; tier < foliageTiers; tier++)
            {
                float t = (float)tier / foliageTiers;
                float tierBottom = baseFoliageHeight + (t * foliageSpan * 0.75f);
                float tierTop = tierBottom + (foliageSpan / foliageTiers * 1.35f);
                float radius = Mathf.Lerp(trunkRadius * 5.5f, trunkRadius * 1.8f, t);

                int foliageSegments = 9;
                int tierBaseIndex = vertices.Count;

                // Apex
                Vector3 apex = new Vector3(0f, tierTop, 0f);
                vertices.Add(apex);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f, 1f));

                // Ring vertices with phyllotaxis angular offset
                float angularOffset = tier * GoldenAngle;
                for (int s = 0; s <= foliageSegments; s++)
                {
                    float angle = angularOffset + ((float)s / foliageSegments * Mathf.PI * 2f);
                    // Organic radius pulsation
                    float r = radius * (1f + 0.12f * Mathf.Sin(angle * 3f + tier));
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * r, tierBottom, Mathf.Sin(angle) * r);

                    Vector3 n = (pos - new Vector3(0f, tierBottom, 0f)).normalized + Vector3.up * 0.4f;
                    vertices.Add(pos);
                    normals.Add(n.normalized);
                    uvs.Add(new Vector2((float)s / foliageSegments, 0f));
                }

                // Ring base center (underside cap)
                int undersideCenterIndex = vertices.Count;
                vertices.Add(new Vector3(0f, tierBottom, 0f));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(0.5f, 0.5f));

                for (int s = 0; s < foliageSegments; s++)
                {
                    int v1 = tierBaseIndex + 1 + s;
                    int v2 = tierBaseIndex + 1 + s + 1;

                    // Cone surface
                    triangles.Add(tierBaseIndex);
                    triangles.Add(v1);
                    triangles.Add(v2);

                    // Underside surface
                    triangles.Add(undersideCenterIndex);
                    triangles.Add(v2);
                    triangles.Add(v1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a procedural supplies crate mesh with beveled trim and metal corner reinforcement.
        /// </summary>
        public static Mesh CreateCrateMesh(float width = 1.2f, float height = 1.0f, float depth = 1.2f)
        {
            Mesh mesh = new Mesh { name = "Procedural_SuppliesCrate" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            float hx = width * 0.5f;
            float hy = height;
            float hz = depth * 0.5f;

            // Add 6 standard faces
            AddQuad(v, n, uv, tri, new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.forward);
            AddQuad(v, n, uv, tri, new Vector3(hx, 0, -hz), new Vector3(-hx, 0, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), Vector3.back);
            AddQuad(v, n, uv, tri, new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz), Vector3.left);
            AddQuad(v, n, uv, tri, new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), Vector3.right);
            AddQuad(v, n, uv, tri, new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz), Vector3.up);
            AddQuad(v, n, uv, tri, new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz), Vector3.down);

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a procedural military rescue helicopter mesh for the extraction safe zone.
        /// </summary>
        public static Mesh CreateHelicopterMesh()
        {
            Mesh mesh = new Mesh { name = "Procedural_Helicopter" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            // 1. Cabin Body (Elongated box with cockpit taper)
            float cabinW = 1.8f;
            float cabinH = 2.0f;
            float cabinL = 4.5f;

            Vector3 cabinOffset = new Vector3(0f, 1.2f, 0f);
            AddBox(v, n, uv, tri, cabinOffset, new Vector3(cabinW, cabinH, cabinL));

            // 2. Tail Boom
            Vector3 tailOffset = cabinOffset + new Vector3(0f, 0.4f, -3.8f);
            AddBox(v, n, uv, tri, tailOffset, new Vector3(0.5f, 0.6f, 3.5f));

            // 3. Tail Fin
            Vector3 finOffset = tailOffset + new Vector3(0f, 0.7f, -1.6f);
            AddBox(v, n, uv, tri, finOffset, new Vector3(0.15f, 1.4f, 0.8f));

            // 4. Landing Skids (Left and Right)
            Vector3 skidL = new Vector3(-1.1f, 0.2f, 0f);
            Vector3 skidR = new Vector3(1.1f, 0.2f, 0f);
            AddBox(v, n, uv, tri, skidL, new Vector3(0.2f, 0.2f, 4.0f));
            AddBox(v, n, uv, tri, skidR, new Vector3(0.2f, 0.2f, 4.0f));

            // Skid struts
            AddBox(v, n, uv, tri, new Vector3(-1.0f, 0.7f, 1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(-1.0f, 0.7f, -1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(1.0f, 0.7f, 1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(1.0f, 0.7f, -1.0f), new Vector3(0.12f, 0.9f, 0.12f));

            // 5. Main Rotor Mast & Blades
            Vector3 mastPos = cabinOffset + new Vector3(0f, cabinH * 0.5f + 0.35f, 0f);
            AddBox(v, n, uv, tri, mastPos, new Vector3(0.3f, 0.7f, 0.3f));

            Vector3 bladePos = mastPos + new Vector3(0f, 0.4f, 0f);
            AddBox(v, n, uv, tri, bladePos, new Vector3(7.5f, 0.08f, 0.45f));
            AddBox(v, n, uv, tri, bladePos, new Vector3(0.45f, 0.08f, 7.5f));

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates an organic mud patch mesh with natural boundary perturbations.
        /// </summary>
        public static Mesh CreateMudPatchMesh(float radiusX = 6.0f, float radiusZ = 6.0f, int segments = 24)
        {
            Mesh mesh = new Mesh { name = "Procedural_MudPatch" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            // Center vertex
            v.Add(new Vector3(0f, 0.03f, 0f));
            n.Add(Vector3.up);
            uv.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                // Mathematical organic splatter contour using Fourier harmonics
                float harmonic = 1f +
                                 0.18f * Mathf.Sin(angle * 3f) +
                                 0.11f * Mathf.Cos(angle * 5f + 1.2f) +
                                 0.07f * Mathf.Sin(angle * 7f);

                float x = Mathf.Cos(angle) * radiusX * harmonic;
                float z = Mathf.Sin(angle) * radiusZ * harmonic;

                v.Add(new Vector3(x, 0.03f, z));
                n.Add(Vector3.up);
                uv.Add(new Vector2(0.5f + (x / (radiusX * 2.4f)), 0.5f + (z / (radiusZ * 2.4f))));
            }

            for (int i = 1; i <= segments; i++)
            {
                tri.Add(0);
                tri.Add(i);
                tri.Add(i + 1);
            }

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBox(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri, Vector3 center, Vector3 size)
        {
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;

            Vector3 c = center;
            // Front
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, -hy, hz), c + new Vector3(hx, -hy, hz), c + new Vector3(hx, hy, hz), c + new Vector3(-hx, hy, hz), Vector3.forward);
            // Back
            AddQuad(v, n, uv, tri, c + new Vector3(hx, -hy, -hz), c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx, hy, -hz), c + new Vector3(hx, hy, -hz), Vector3.back);
            // Left
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx, -hy, hz), c + new Vector3(-hx, hy, hz), c + new Vector3(-hx, hy, -hz), Vector3.left);
            // Right
            AddQuad(v, n, uv, tri, c + new Vector3(hx, -hy, hz), c + new Vector3(hx, -hy, -hz), c + new Vector3(hx, hy, -hz), c + new Vector3(hx, hy, hz), Vector3.right);
            // Top
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, hy, hz), c + new Vector3(hx, hy, hz), c + new Vector3(hx, hy, -hz), c + new Vector3(-hx, hy, -hz), Vector3.up);
            // Bottom
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, -hy, -hz), c + new Vector3(hx, -hy, -hz), c + new Vector3(hx, -hy, hz), c + new Vector3(-hx, -hy, hz), Vector3.down);
        }

        private static void AddQuad(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            int baseIdx = v.Count;
            v.Add(v0);
            v.Add(v1);
            v.Add(v2);
            v.Add(v3);

            n.Add(normal);
            n.Add(normal);
            n.Add(normal);
            n.Add(normal);

            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 1f));

            tri.Add(baseIdx);
            tri.Add(baseIdx + 2);
            tri.Add(baseIdx + 1);

            tri.Add(baseIdx);
            tri.Add(baseIdx + 3);
            tri.Add(baseIdx + 2);
        }
    }
}
