using UnityEngine;
using DeadDepths.Core;

namespace DeadDepths.Player
{
    /// <summary>
    /// Renders a crisp ground target ring and subtle directional sightline while holding RMB Aim.
    /// </summary>
    public class DeadDepthsAimReticle : MonoBehaviour
    {
        [SerializeField] private Shader bundledLineShader;
        private DeadDepthsPlayerController player;
        private LineRenderer ringRenderer;
        private LineRenderer lineRenderer;
        private Material reticleMat;
        private const int RingSegments = 28;

        private void Awake()
        {
            player = GetComponent<DeadDepthsPlayerController>();
            CreateRenderers();
        }

        private void OnDestroy()
        {
            if (reticleMat != null)
            {
                if (Application.isPlaying) Destroy(reticleMat);
                else DestroyImmediate(reticleMat);
                reticleMat = null;
            }
        }

        private void CreateRenderers()
        {
            if (ringRenderer != null && lineRenderer != null) return;

            Shader s = bundledLineShader != null
                ? bundledLineShader
                : (Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit"));
            if (reticleMat == null && s != null)
            {
                reticleMat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            }

            Transform existingRing = transform.Find("AimReticleRing");
            GameObject ringGo = existingRing != null ? existingRing.gameObject : new GameObject("AimReticleRing");
            if (existingRing == null) ringGo.transform.SetParent(transform, false);
            ringRenderer = ringGo.GetComponent<LineRenderer>();
            if (ringRenderer == null) ringRenderer = ringGo.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = true;
            ringRenderer.loop = true;
            ringRenderer.positionCount = RingSegments;
            ringRenderer.startWidth = 0.055f;
            ringRenderer.endWidth = 0.055f;
            ringRenderer.sharedMaterial = reticleMat;
            ringRenderer.startColor = new Color(0.95f, 0.82f, 0.35f, 0.85f);
            ringRenderer.endColor = new Color(0.95f, 0.82f, 0.35f, 0.85f);
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            ringRenderer.enabled = false;

            Transform existingLine = transform.Find("AimSightLine");
            GameObject lineGo = existingLine != null ? existingLine.gameObject : new GameObject("AimSightLine");
            if (existingLine == null) lineGo.transform.SetParent(transform, false);
            lineRenderer = lineGo.GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = lineGo.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.015f;
            lineRenderer.sharedMaterial = reticleMat;
            lineRenderer.startColor = new Color(0.95f, 0.82f, 0.35f, 0.55f);
            lineRenderer.endColor = new Color(0.95f, 0.82f, 0.35f, 0.15f);
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (player == null) player = GetComponent<DeadDepthsPlayerController>();
            if (ringRenderer == null || lineRenderer == null) return;

            bool show = player != null &&
                        player.IsAiming &&
                        !player.IsDead &&
                        Time.timeScale > 0f &&
                        (GameManager.Instance == null || !GameManager.Instance.IsInMainMenu);

            ringRenderer.enabled = show;
            lineRenderer.enabled = show;
            if (!show) return;

            Vector3 aimGround = new Vector3(player.LastValidAimPoint.x, player.transform.position.y + 0.06f, player.LastValidAimPoint.z);
            float radius = 0.42f + Mathf.Sin(Time.unscaledTime * 5f) * 0.03f;

            for (int i = 0; i < RingSegments; i++)
            {
                float a = (i / (float)RingSegments) * Mathf.PI * 2f;
                ringRenderer.SetPosition(i, aimGround + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            Vector3 startPos = player.transform.position + Vector3.up * 0.9f + player.transform.forward * 0.45f;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, aimGround);
        }
    }
}
