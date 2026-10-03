using UnityEngine;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class RobberyBobVisionCone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyFSM fsm;

        [Header("Cone Configuration")]
        [SerializeField, Range(30, 120)] private int rayCount = 72;
        [SerializeField] private float groundOffset = 0.04f;
        [SerializeField] private float raycastHeight = 0.5f;

        [Header("Robbery Bob Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 0.12f, 0.12f, 0.35f);
        [SerializeField] private Color alertColor = new Color(1f, 0.02f, 0.02f, 0.65f);
        [SerializeField] private bool showOutline = true;
        [SerializeField] private Color outlineNormalColor = new Color(1f, 0.25f, 0.25f, 0.85f);
        [SerializeField] private Color outlineAlertColor = new Color(1f, 0.05f, 0.05f, 1f);
        [SerializeField] private float outlineWidth = 0.05f;

        private GameObject coneObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh coneMesh;
        private Material coneMaterial;

        private GameObject outlineObject;
        private LineRenderer lineRenderer;

        private Vector3[] vertices;
        private int[] triangles;
        private Vector2[] uvs;
        private Vector3[] outlinePoints;

        private void Awake()
        {
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (fsm == null) fsm = GetComponent<EnemyFSM>();

            SetupVisionConeRenderer();
        }

        private void OnEnable()
        {
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (lineRenderer != null) lineRenderer.enabled = showOutline;
        }

        private void OnDisable()
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (coneObject != null)
            {
                if (Application.isPlaying) Destroy(coneObject);
                else DestroyImmediate(coneObject);
            }

            if (outlineObject != null)
            {
                if (Application.isPlaying) Destroy(outlineObject);
                else DestroyImmediate(outlineObject);
            }
        }

        private void SetupVisionConeRenderer()
        {
            // Pastikan objek child mesh ada
            Transform existingCone = transform.Find("RobberyBob_VisionCone");
            if (existingCone != null)
            {
                coneObject = existingCone.gameObject;
                meshFilter = coneObject.GetComponent<MeshFilter>();
                meshRenderer = coneObject.GetComponent<MeshRenderer>();
            }
            else
            {
                coneObject = new GameObject("RobberyBob_VisionCone");
                coneObject.transform.SetParent(transform, false);
                coneObject.transform.localPosition = Vector3.zero;
                coneObject.transform.localRotation = Quaternion.identity;
                meshFilter = coneObject.AddComponent<MeshFilter>();
                meshRenderer = coneObject.AddComponent<MeshRenderer>();
            }

            coneMesh = new Mesh { name = "RobberyBob_ConeMesh" };
            coneMesh.MarkDynamic();
            meshFilter.mesh = coneMesh;

            // Shader Sprites/Default atau Unlit/Transparent untuk transparansi bersih
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("UI/Default");

            coneMaterial = new Material(shader)
            {
                color = normalColor
            };
            meshRenderer.sharedMaterial = coneMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Setup LineRenderer untuk outline batas luar
            Transform existingOutline = transform.Find("RobberyBob_VisionOutline");
            if (existingOutline != null)
            {
                outlineObject = existingOutline.gameObject;
                lineRenderer = outlineObject.GetComponent<LineRenderer>();
            }
            else
            {
                outlineObject = new GameObject("RobberyBob_VisionOutline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = Vector3.zero;
                outlineObject.transform.localRotation = Quaternion.identity;
                lineRenderer = outlineObject.AddComponent<LineRenderer>();
            }

            lineRenderer.material = new Material(shader) { color = outlineNormalColor };
            lineRenderer.startWidth = outlineWidth;
            lineRenderer.endWidth = outlineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            AllocateArrays();
        }

        private void AllocateArrays()
        {
            int vertCount = rayCount + 2;
            vertices = new Vector3[vertCount];
            triangles = new int[rayCount * 3];
            uvs = new Vector2[vertCount];
            outlinePoints = new Vector3[rayCount + 2]; // dari origin -> perimeter arc -> origin

            // Pre-hitung indeks segitiga (tetap sepanjang waktu)
            for (int i = 0; i < rayCount; i++)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        private void LateUpdate()
        {
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (fsm == null) fsm = GetComponent<EnemyFSM>();

            // Jika Enemy mati, sembunyikan cone
            if (fsm != null && fsm.CurrentState == EnemyState.Dead)
            {
                if (meshRenderer != null && meshRenderer.enabled) meshRenderer.enabled = false;
                if (lineRenderer != null && lineRenderer.enabled) lineRenderer.enabled = false;
                return;
            }

            if (meshRenderer != null && !meshRenderer.enabled) meshRenderer.enabled = true;
            if (lineRenderer != null && showOutline && !lineRenderer.enabled) lineRenderer.enabled = true;

            UpdateVisionConeGeometry();
            UpdateVisionConeColor();
        }

        private void UpdateVisionConeGeometry()
        {
            if (coneMesh == null || meshFilter == null) return;

            float visionRange = perception != null ? perception.VisionRange : 10f;
            float visionAngle = perception != null ? perception.VisionAngle : 90f;
            LayerMask obstacleMask = perception != null ? perception.ObstacleMask : LayerMask.GetMask("Obstacle", "Wall");

            Vector3 worldPos = transform.position;
            Vector3 originRay = worldPos + Vector3.up * raycastHeight;
            Vector3 groundOrigin = worldPos + Vector3.up * groundOffset;

            // Titik tengah (Local space untuk mesh)
            vertices[0] = transform.InverseTransformPoint(groundOrigin);
            uvs[0] = new Vector2(0.5f, 0f);

            float halfAngle = visionAngle * 0.5f;
            float angleStep = visionAngle / rayCount;

            outlinePoints[0] = groundOrigin;

            for (int i = 0; i <= rayCount; i++)
            {
                float currentAngle = -halfAngle + (angleStep * i);
                Vector3 worldDir = Quaternion.Euler(0f, currentAngle, 0f) * transform.forward;

                float hitDistance = visionRange;
                Vector3 perimeterWorldPoint;

                // Raycast oklusi rintangan/tembok
                if (Physics.Raycast(originRay, worldDir, out RaycastHit hit, visionRange, obstacleMask))
                {
                    hitDistance = hit.distance;
                    // Tempelkan di atas permukaan lantai pada titik benturan tembok
                    perimeterWorldPoint = hit.point;
                    perimeterWorldPoint.y = worldPos.y + groundOffset;
                }
                else
                {
                    perimeterWorldPoint = groundOrigin + worldDir * hitDistance;
                    perimeterWorldPoint.y = worldPos.y + groundOffset;
                }

                vertices[i + 1] = transform.InverseTransformPoint(perimeterWorldPoint);
                uvs[i + 1] = new Vector2((float)i / rayCount, 1f);
                outlinePoints[i + 1] = perimeterWorldPoint;
            }

            coneMesh.Clear();
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.uv = uvs;
            coneMesh.RecalculateNormals();

            // Update border line outline
            if (showOutline && lineRenderer != null)
            {
                lineRenderer.positionCount = outlinePoints.Length;
                lineRenderer.SetPositions(outlinePoints);
            }
        }

        private void UpdateVisionConeColor()
        {
            bool isAlert = perception != null && perception.CanSeePlayer;
            Color targetColor = isAlert ? alertColor : normalColor;
            Color targetOutlineColor = isAlert ? outlineAlertColor : outlineNormalColor;

            if (coneMaterial != null)
            {
                coneMaterial.color = targetColor;
            }

            if (lineRenderer != null && lineRenderer.material != null)
            {
                lineRenderer.material.color = targetOutlineColor;
            }
        }
    }
}
