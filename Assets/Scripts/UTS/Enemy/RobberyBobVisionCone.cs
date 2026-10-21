using UnityEngine;
using UTS.Zombies;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class RobberyBobVisionCone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyFSM fsm;
        private IVisionSource visionSource;
        private static bool warnedMissingMaterial;
        private static bool warnedDisabledCone;

        [Header("Cone Configuration")]
        [SerializeField, Range(30, 120)] private int rayCount = 72;
        [SerializeField] private float groundOffset = 0.04f;
        [SerializeField] private float raycastHeight = 0.65f;
        [SerializeField] private LayerMask obstacleLayers;

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
        private Material outlineMaterial;

        private Vector3[] vertices;
        private int[] triangles;
        private Vector2[] uvs;
        private Vector3[] outlinePoints;
        private readonly RaycastHit[] raycastHits = new RaycastHit[16];

        private void Awake()
        {
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (fsm == null) fsm = GetComponent<EnemyFSM>();
            ResolveVisionSource();
            ConfigureObstacleMask();

            SetupVisionConeRenderer();
        }

        private void OnEnable()
        {
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (lineRenderer != null) lineRenderer.enabled = showOutline;
        }

        private void ResolveVisionSource()
        {
            // Explicitly prefer the swarm provider when both components exist.
            ZombieSwarmAgent swarm = GetComponent<ZombieSwarmAgent>();
            visionSource = swarm != null ? (IVisionSource)swarm : perception as IVisionSource;
        }

        private void ConfigureObstacleMask()
        {
            if (obstacleLayers == 0)
            {
                obstacleLayers = LayerMask.GetMask("Default", "Wall", "Obstacle");
                if (obstacleLayers == 0)
                {
                    obstacleLayers = (1 << 0) | (1 << 3) | (1 << 7);
                }
            }
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

            if (coneMaterial != null)
            {
                if (Application.isPlaying) Destroy(coneMaterial);
                else DestroyImmediate(coneMaterial);
            }

            if (outlineMaterial != null)
            {
                if (Application.isPlaying) Destroy(outlineMaterial);
                else DestroyImmediate(outlineMaterial);
            }
        }

        private void SetupVisionConeRenderer()
        {
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

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                if (!warnedMissingMaterial)
                {
                    warnedMissingMaterial = true;
                    Debug.LogWarning("[RobberyBobVisionCone] No compatible cone shader was found.", this);
                }
                return;
            }

            coneMaterial = new Material(shader)
            {
                color = normalColor
            };
            meshRenderer.sharedMaterial = coneMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

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

            outlineMaterial = new Material(shader) { color = outlineNormalColor };
            lineRenderer.sharedMaterial = outlineMaterial;
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
            outlinePoints = new Vector3[rayCount + 3];

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
            if (visionSource == null) ResolveVisionSource();

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

            float visionRange = visionSource != null ? visionSource.VisionRange : 10f;
            float visionAngle = visionSource != null ? visionSource.VisionAngle : 90f;
            if (obstacleLayers == 0) ConfigureObstacleMask();

            Vector3 worldPos = transform.position;

            // Cari ketinggian tanah aktual di bawah zombie (karena NavMeshAgent baseOffset biasanya 1m di atas ground)
            float groundY = worldPos.y - 1.0f + groundOffset;
            if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2.5f, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                groundY = groundHit.point.y + groundOffset;
            }

            Vector3 groundOrigin = new Vector3(worldPos.x, groundY, worldPos.z);
            Vector3 originRay = new Vector3(worldPos.x, groundY + Mathf.Max(0.5f, raycastHeight), worldPos.z);

            vertices[0] = transform.InverseTransformPoint(groundOrigin);
            uvs[0] = new Vector2(0.5f, 0f);

            float halfAngle = visionAngle * 0.5f;
            float angleStep = visionAngle / rayCount;

            if (outlinePoints == null || outlinePoints.Length != rayCount + 3)
            {
                outlinePoints = new Vector3[rayCount + 3];
            }
            outlinePoints[0] = groundOrigin;

            const float startOffset = 0.35f; // Jarak start raycast di luar capsule collider zombie (radius 0.30m)
            float maxCastDist = Mathf.Max(0.1f, visionRange - startOffset);

            for (int i = 0; i <= rayCount; i++)
            {
                float currentAngle = -halfAngle + (angleStep * i);
                Vector3 worldDir = Quaternion.Euler(0f, currentAngle, 0f) * transform.forward;
                Vector3 rayStart = originRay + worldDir * startOffset;

                int count = Physics.RaycastNonAlloc(rayStart, worldDir, raycastHits, maxCastDist, obstacleLayers, QueryTriggerInteraction.Ignore);
                float closestDist = float.MaxValue;

                for (int h = 0; h < count; h++)
                {
                    RaycastHit hit = raycastHits[h];
                    if (hit.collider == null) continue;
                    Transform hitT = hit.collider.transform;

                    // Abaikan jika collider milik zombie ini sendiri
                    if (hitT == transform || hitT.IsChildOf(transform)) continue;

                    // Abaikan Player (agar cone tetap melingkupi player saat terlihat)
                    if (hitT.CompareTag("Player") || hit.collider.GetComponentInParent<SimplePlayerController>() != null) continue;

                    // Abaikan Zombie lain
                    if (hit.collider.GetComponentInParent<ZombieSwarmAgent>() != null || hit.collider.GetComponentInParent<EnemyFSM>() != null) continue;

                    float actualDist = hit.distance + startOffset;
                    if (actualDist < closestDist)
                    {
                        closestDist = actualDist;
                    }
                }

                float hitDistance = visionRange;
                if (closestDist < float.MaxValue)
                {
                    // Potong tepat di depan dinding (mundurkan 0.04m agar tidak tembus atau z-fight dengan permukaan dinding)
                    hitDistance = Mathf.Max(0.1f, closestDist - 0.04f);
                }

                Vector3 perimeterWorldPoint = groundOrigin + worldDir * hitDistance;
                perimeterWorldPoint.y = groundY;

                vertices[i + 1] = transform.InverseTransformPoint(perimeterWorldPoint);
                uvs[i + 1] = new Vector2((float)i / rayCount, 1f);
                outlinePoints[i + 1] = perimeterWorldPoint;
            }

            // Tutup outline kembali ke titik asal
            outlinePoints[rayCount + 2] = groundOrigin;

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
            bool isAlert = visionSource != null && visionSource.IsSeeingPlayer;
            Color targetColor = isAlert ? alertColor : normalColor;
            Color targetOutlineColor = isAlert ? outlineAlertColor : outlineNormalColor;

            if (coneMaterial != null)
            {
                coneMaterial.color = targetColor;
            }
            else if (!warnedMissingMaterial)
            {
                warnedMissingMaterial = true;
                Debug.LogWarning("[RobberyBobVisionCone] Cone material or shader is missing.", this);
            }

            if (lineRenderer != null && outlineMaterial != null)
            {
                outlineMaterial.color = targetOutlineColor;
            }
        }
    }
}
