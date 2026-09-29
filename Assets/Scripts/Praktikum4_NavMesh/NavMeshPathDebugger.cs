using UnityEngine;
using UnityEngine.AI;

namespace Praktikum4.NavMeshNavigation
{
    [ExecuteAlways]
    public class NavMeshPathDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Visualization Settings")]
        [SerializeField] private bool showInGameView = true;
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private float pathElevation = 0.15f;
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private Color pathColor = new Color(1f, 0.55f, 0f, 1f); // Oranye terang
        [SerializeField] private Color cornerGizmoColor = Color.yellow;

        [Header("Diagnostics (Read-Only)")]
        [SerializeField] private int cornerCount;
        [SerializeField] private float totalPathLength;
        [SerializeField] private NavMeshPathStatus pathStatus;

        public float TotalPathLength => totalPathLength;
        public int CornerCount => cornerCount;
        public NavMeshPathStatus PathStatus => pathStatus;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            SetupLineRenderer();
        }

        private void Start()
        {
            SetupLineRenderer();
        }

        private void LateUpdate()
        {
            UpdatePathVisuals();
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = pathColor;

                // Gunakan shader default unlit jika belum ada material
                if (lineRenderer.sharedMaterial == null)
                {
                    Shader defaultShader = Shader.Find("Sprites/Default");
                    if (defaultShader == null) defaultShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (defaultShader != null)
                    {
                        lineRenderer.material = new Material(defaultShader) { color = pathColor };
                    }
                }
            }
        }

        private void UpdatePathVisuals()
        {
            if (agent == null) return;

            NavMeshPath path = agent.path;
            if (path == null || path.corners == null || path.corners.Length < 2)
            {
                cornerCount = 0;
                totalPathLength = 0f;
                if (lineRenderer != null) lineRenderer.positionCount = 0;
                return;
            }

            Vector3[] corners = path.corners;
            cornerCount = corners.Length;
            pathStatus = path.status;

            // Hitung total panjang lintasan
            float length = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                length += Vector3.Distance(corners[i], corners[i + 1]);
            }
            totalPathLength = length;

            // Render pada Game View via LineRenderer
            if (lineRenderer != null)
            {
                if (!showInGameView)
                {
                    lineRenderer.positionCount = 0;
                    return;
                }

                lineRenderer.positionCount = corners.Length;
                for (int i = 0; i < corners.Length; i++)
                {
                    // Naikkan sedikit agar tidak z-fighting dengan lantai
                    Vector3 elevatedCorner = corners[i] + Vector3.up * pathElevation;
                    lineRenderer.SetPosition(i, elevatedCorner);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showInSceneView || agent == null) return;

            NavMeshPath path = agent.path;
            if (path == null || path.corners == null || path.corners.Length < 2) return;

            Vector3[] corners = path.corners;

            // Gambar garis jalur di Scene View
            Gizmos.color = pathColor;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 p1 = corners[i] + Vector3.up * pathElevation;
                Vector3 p2 = corners[i + 1] + Vector3.up * pathElevation;
                Gizmos.DrawLine(p1, p2);
            }

            // Gambar bola pada setiap titik sudut (waypoints)
            Gizmos.color = cornerGizmoColor;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 pos = corners[i] + Vector3.up * pathElevation;
                Gizmos.DrawSphere(pos, 0.12f);
            }
        }
    }
}
