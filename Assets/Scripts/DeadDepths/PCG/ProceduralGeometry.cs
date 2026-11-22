using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.PCG
{
    /// <summary>
    /// Generates 3D meshes mathematically for environment assets.
    /// ASD-STE100: This module creates 3D shapes using mathematical formulas.
    /// </summary>
    public static class ProceduralGeometry
    {
        public const float GoldenRatio = 1.61803398875f;
        public const float GoldenAngle = 137.507764f * Mathf.Deg2Rad;

        /// <summary>
        /// Creates a procedural pine / forest tree with trunk (submesh 0) and phyllotaxis foliage layers (submesh 1).
        /// </summary>
        public static Mesh CreateTreeMesh(float height = 6.0f, float trunkRadius = 0.35f, int foliageTiers = 4)
        {
            Mesh mesh = new Mesh { name = "Procedural_Tree" };
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trunkTriangles = new List<int>();
            List<int> foliageTriangles = new List<int>();

            int segments = 8;
            float trunkHeight = height * 0.45f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(cos * trunkRadius, 0f, sin * trunkRadius));
                normals.Add(new Vector3(cos, 0f, sin));
                uvs.Add(new Vector2((float)i / segments, 0f));

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

                trunkTriangles.Add(b1);
                trunkTriangles.Add(t1);
                trunkTriangles.Add(b2);

                trunkTriangles.Add(b2);
                trunkTriangles.Add(t1);
                trunkTriangles.Add(t2);
            }

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

                Vector3 apex = new Vector3(0f, tierTop, 0f);
                vertices.Add(apex);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f, 1f));

                float angularOffset = tier * GoldenAngle;
                for (int s = 0; s <= foliageSegments; s++)
                {
                    float angle = angularOffset + ((float)s / foliageSegments * Mathf.PI * 2f);
                    float r = radius * (1f + 0.12f * Mathf.Sin(angle * 3f + tier));
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * r, tierBottom, Mathf.Sin(angle) * r);

                    Vector3 n = (pos - new Vector3(0f, tierBottom, 0f)).normalized + Vector3.up * 0.4f;
                    vertices.Add(pos);
                    normals.Add(n.normalized);
                    uvs.Add(new Vector2((float)s / foliageSegments, 0f));
                }

                int undersideCenterIndex = vertices.Count;
                vertices.Add(new Vector3(0f, tierBottom, 0f));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(0.5f, 0.5f));

                for (int s = 0; s < foliageSegments; s++)
                {
                    int v1 = tierBaseIndex + 1 + s;
                    int v2 = tierBaseIndex + 1 + s + 1;

                    foliageTriangles.Add(tierBaseIndex);
                    foliageTriangles.Add(v2);
                    foliageTriangles.Add(v1);

                    foliageTriangles.Add(undersideCenterIndex);
                    foliageTriangles.Add(v1);
                    foliageTriangles.Add(v2);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trunkTriangles, 0);
            mesh.SetTriangles(foliageTriangles, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a procedural supplies crate mesh.
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
        /// Creates a hinged lid mesh pivoted along its back edge (-Z) so rotating X by -65 deg opens it cleanly.
        /// </summary>
        public static Mesh CreateCrateLidMesh(float width = 1.26f, float height = 0.12f, float depth = 1.26f)
        {
            Mesh mesh = new Mesh { name = "Procedural_CrateLid" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            AddBox(v, n, uv, tri, new Vector3(0f, height * 0.5f, depth * 0.5f), new Vector3(width, height, depth));

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

            float cabinW = 1.8f;
            float cabinH = 2.0f;
            float cabinL = 4.5f;

            Vector3 cabinOffset = new Vector3(0f, 1.2f, 0f);
            AddBox(v, n, uv, tri, cabinOffset, new Vector3(cabinW, cabinH, cabinL));

            Vector3 tailOffset = cabinOffset + new Vector3(0f, 0.4f, -3.8f);
            AddBox(v, n, uv, tri, tailOffset, new Vector3(0.5f, 0.6f, 3.5f));

            Vector3 finOffset = tailOffset + new Vector3(0f, 0.7f, -1.6f);
            AddBox(v, n, uv, tri, finOffset, new Vector3(0.15f, 1.4f, 0.8f));

            Vector3 skidL = new Vector3(-1.1f, 0.2f, 0f);
            Vector3 skidR = new Vector3(1.1f, 0.2f, 0f);
            AddBox(v, n, uv, tri, skidL, new Vector3(0.2f, 0.2f, 4.0f));
            AddBox(v, n, uv, tri, skidR, new Vector3(0.2f, 0.2f, 4.0f));

            AddBox(v, n, uv, tri, new Vector3(-1.0f, 0.7f, 1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(-1.0f, 0.7f, -1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(1.0f, 0.7f, 1.0f), new Vector3(0.12f, 0.9f, 0.12f));
            AddBox(v, n, uv, tri, new Vector3(1.0f, 0.7f, -1.0f), new Vector3(0.12f, 0.9f, 0.12f));

            Vector3 mastPos = cabinOffset + new Vector3(0f, cabinH * 0.5f + 0.35f, 0f);
            AddBox(v, n, uv, tri, mastPos, new Vector3(0.3f, 0.7f, 0.3f));

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a centered procedural main rotor hub and blades mesh so the MainRotor child can spin around local +Y.
        /// </summary>
        public static Mesh CreateHelicopterRotorMesh()
        {
            Mesh mesh = new Mesh { name = "Procedural_HelicopterRotor" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(0.55f, 0.2f, 0.55f));
            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(7.5f, 0.08f, 0.45f));
            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(0.45f, 0.08f, 7.5f));

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Creates a centered tail rotor mesh spinning around local +X.
        /// </summary>
        public static Mesh CreateTailRotorMesh()
        {
            Mesh mesh = new Mesh { name = "Procedural_TailRotor" };
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();

            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(0.18f, 0.22f, 0.22f));
            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(0.06f, 1.35f, 0.16f));
            AddBox(v, n, uv, tri, Vector3.zero, new Vector3(0.06f, 0.16f, 1.35f));

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

            v.Add(new Vector3(0f, 0.03f, 0f));
            n.Add(Vector3.up);
            uv.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
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
                tri.Add(i + 1);
                tri.Add(i);
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
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, -hy, hz), c + new Vector3(hx, -hy, hz), c + new Vector3(hx, hy, hz), c + new Vector3(-hx, hy, hz), Vector3.forward);
            AddQuad(v, n, uv, tri, c + new Vector3(hx, -hy, -hz), c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx, hy, -hz), c + new Vector3(hx, hy, -hz), Vector3.back);
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx, -hy, hz), c + new Vector3(-hx, hy, hz), c + new Vector3(-hx, hy, -hz), Vector3.left);
            AddQuad(v, n, uv, tri, c + new Vector3(hx, -hy, hz), c + new Vector3(hx, -hy, -hz), c + new Vector3(hx, hy, -hz), c + new Vector3(hx, hy, hz), Vector3.right);
            AddQuad(v, n, uv, tri, c + new Vector3(-hx, hy, hz), c + new Vector3(hx, hy, hz), c + new Vector3(hx, hy, -hz), c + new Vector3(-hx, hy, -hz), Vector3.up);
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
            tri.Add(baseIdx + 1);
            tri.Add(baseIdx + 2);

            tri.Add(baseIdx);
            tri.Add(baseIdx + 2);
            tri.Add(baseIdx + 3);
        }
    }
}
