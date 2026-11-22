using System;
using UnityEngine;
using DeadDepths.Interactive;
using DeadDepths.Player;

namespace DeadDepths.AI
{
    /// <summary>
    /// Visual directional indicator that tracks the player during the Charger telegraph phase,
    /// clipping cleanly against solid walls, trees, and unbreakable obstacles.
    /// ASD-STE100: This indicator shows the direction of the Charger attack.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ChargerTelegraph : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public float Length = 14f;
            public float Width = 0.65f;
            public Color TelegraphColor = new Color(1f, 0.2f, 0.1f, 0.78f);
        }
        #endregion

        [SerializeField] private Config telegraphConfig = new Config();
        private LineRenderer line;
        private Material lineMaterial;

        private void Awake()
        {
            if (telegraphConfig == null) telegraphConfig = new Config();
            if (telegraphConfig.Length <= 0f) telegraphConfig.Length = 14f;
            if (telegraphConfig.Width <= 0f) telegraphConfig.Width = 0.65f;

            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = telegraphConfig.Width;
            line.endWidth = telegraphConfig.Width * 1.45f;

            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (s != null)
            {
                lineMaterial = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
                lineMaterial.color = telegraphConfig.TelegraphColor;
                line.sharedMaterial = lineMaterial;
            }
            line.startColor = telegraphConfig.TelegraphColor;
            line.endColor = new Color(telegraphConfig.TelegraphColor.r, telegraphConfig.TelegraphColor.g, telegraphConfig.TelegraphColor.b, 0.25f);
            line.enabled = false;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                if (Application.isPlaying) Destroy(lineMaterial);
                else DestroyImmediate(lineMaterial);
                lineMaterial = null;
            }
        }

        public void ShowTelegraph(Vector3 start, Vector3 direction)
        {
            if (line == null) line = GetComponent<LineRenderer>();
            if (line == null) return;

            line.enabled = true;
            Vector3 dir = direction;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            dir.Normalize();

            float clippedLength = telegraphConfig.Length;
            Vector3 rayOrigin = new Vector3(start.x, start.y + 0.9f, start.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, dir, telegraphConfig.Length, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                    if (hit.collider.gameObject.name == "PlayableGround") continue;
                    if (hit.collider.GetComponentInParent<DeadDepthsPlayerController>() != null) continue;
                    if (hit.collider.GetComponentInParent<ZombieController>() != null) continue;
                    var blocker = hit.collider.GetComponentInParent<BlockingObject>();
                    if (blocker != null && blocker.IsBreakable) continue;

                    clippedLength = Mathf.Max(0.8f, hit.distance);
                    break;
                }
            }

            Vector3 groundStart = new Vector3(start.x, start.y + 0.12f, start.z);
            Vector3 groundEnd = groundStart + dir * clippedLength;

            line.SetPosition(0, groundStart);
            line.SetPosition(1, groundEnd);
        }

        public void HideTelegraph()
        {
            if (line != null) line.enabled = false;
        }
    }
}
