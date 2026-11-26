using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DeadDepths.AI;
using DeadDepths.Interactive;
using DeadDepths.PCG;

namespace DeadDepths.Player
{
    /// <summary>
    /// Fades obstructing foreground geometry (walls, trees including full foliage canopy)
    /// between the isometric camera and the player.
    /// Swaps Opaque URP Lit materials to transient Transparent URP Lit clones while fading
    /// and restores the original shared materials once unobstructed or destroyed.
    /// ASD-STE100: This module makes obstructing obstacles and trees transparent so the player remains visible.
    /// Follows Section 2.6 of the specification.
    /// </summary>
    public class ObstructionFadeController : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Range(0.05f, 0.9f)] public float FadedAlpha = 0.25f;
            [Range(0.05f, 0.5f)] public float TreeFadedAlpha = 0.14f;
            public float FadeSpeed = 8.0f;
            public float SphereCastRadius = 0.6f;
            public float TreeCanopyPadding = 0.55f;
            public LayerMask ObstructionLayers = ~0; // Default all layers
        }
        #endregion

        [SerializeField] private Config fadeConfig = new Config();
        public Config FadeConfig => fadeConfig;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Material transparentVariantTemplate;

        private WorldGenerator cachedWorldGen;

        private class FadedRendererState
        {
            public Renderer Renderer;
            public float TargetAlpha;
            public float CurrentAlpha;
            public float DesiredFadedAlpha;
            public Material[] OriginalSharedMaterials;
            public Material[] FadeMaterials;
            public Color[] OriginalColors;
            public bool IsUsingFadeMaterials;
            public float LastObstructedTime;
        }

        private readonly Dictionary<Renderer, FadedRendererState> trackedRenderers = new Dictionary<Renderer, FadedRendererState>(32);
        private readonly List<Renderer> toRemoveList = new List<Renderer>(16);
        private readonly HashSet<Renderer> currentHits = new HashSet<Renderer>();
        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];
        private readonly List<Renderer> tempRenderers = new List<Renderer>(8);

        private void Awake()
        {
            ApplyFallbackDefaults();
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (fadeConfig == null) fadeConfig = new Config();
            if (fadeConfig.FadedAlpha <= 0f) fadeConfig.FadedAlpha = 0.25f;
            if (fadeConfig.TreeFadedAlpha <= 0f) fadeConfig.TreeFadedAlpha = 0.14f;
            if (fadeConfig.FadeSpeed <= 0f) fadeConfig.FadeSpeed = 8.0f;
            if (fadeConfig.SphereCastRadius <= 0f) fadeConfig.SphereCastRadius = 0.6f;
            if (fadeConfig.TreeCanopyPadding <= 0f) fadeConfig.TreeCanopyPadding = 0.55f;
        }

        private void Start()
        {
            ApplyFallbackDefaults();
            if (playerTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }
        }

        private void LateUpdate()
        {
            UpdateFade(Time.deltaTime);
        }

        /// <summary>
        /// Evaluates camera-to-player obstructions (colliders and procedural tree canopies)
        /// and updates transparent material swaps over dt seconds.
        /// </summary>
        public void UpdateFade(float dt)
        {
            ApplyFallbackDefaults();

            if (playerTarget == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTarget = p.transform;
                if (playerTarget == null) return;
            }

            Vector3 camPos = transform.position;
            Vector3 torsoPos = playerTarget.position + Vector3.up * 1.0f;
            Vector3 headPos = playerTarget.position + Vector3.up * 1.7f;
            Vector3 dir = torsoPos - camPos;
            float dist = dir.magnitude;

            currentHits.Clear();
            if (dist > 0.01f)
            {
                Vector3 dirNorm = dir / dist;

                // 1a. Non-alloc SphereCast to detect solid collider obstructions (walls, tree trunks, etc.)
                int hitCount = Physics.SphereCastNonAlloc(
                    camPos, fadeConfig.SphereCastRadius, dirNorm, hitBuffer, dist,
                    fadeConfig.ObstructionLayers, QueryTriggerInteraction.Ignore);

                for (int h = 0; h < hitCount; h++)
                {
                    Collider col = hitBuffer[h].collider;
                    if (col == null) continue;
                    if (col.transform == playerTarget || col.transform.IsChildOf(playerTarget)) continue;
                    if (col.gameObject.name == "PlayableGround") continue;
                    if (col.GetComponentInParent<ZombieController>() != null) continue;
                    if (col.GetComponentInParent<SuppliesCrate>() != null) continue;
                    if (col.GetComponentInParent<BlockingObject>() != null) continue;

                    bool isTree = col.gameObject.name.StartsWith("BoundaryTree_", StringComparison.Ordinal);
                    float desiredAlpha = isTree ? fadeConfig.TreeFadedAlpha : fadeConfig.FadedAlpha;

                    tempRenderers.Clear();
                    col.GetComponentsInChildren(false, tempRenderers);
                    for (int i = 0; i < tempRenderers.Count; i++)
                    {
                        Renderer r = tempRenderers[i];
                        if (r == null || !r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
                        currentHits.Add(r);
                        RegisterOrUpdateFadedRenderer(r, desiredAlpha);
                    }
                }

                // 1b. Canopy-accurate tree check so wide foliage tiers fade whenever player is behind a tree
                DetectObstructingTreeCanopies(camPos, torsoPos, headPos);
            }

            // 2. Update alpha transitions and URP Transparent material swaps
            toRemoveList.Clear();

            foreach (var kvp in trackedRenderers)
            {
                var state = kvp.Value;
                if (state.Renderer == null)
                {
                    DestroyFadeMaterials(state);
                    toRemoveList.Add(kvp.Key);
                    continue;
                }

                bool isCurrentlyObstructing = currentHits.Contains(state.Renderer);
                state.TargetAlpha = isCurrentlyObstructing ? state.DesiredFadedAlpha : 1.0f;
                state.CurrentAlpha = Mathf.MoveTowards(state.CurrentAlpha, state.TargetAlpha, Mathf.Max(0f, dt) * fadeConfig.FadeSpeed);

                if (state.CurrentAlpha < 0.999f)
                {
                    if (!state.IsUsingFadeMaterials && state.FadeMaterials != null)
                    {
                        state.Renderer.sharedMaterials = state.FadeMaterials;
                        state.IsUsingFadeMaterials = true;
                    }

                    ApplyAlphaToFadeMaterials(state);
                }
                else
                {
                    state.CurrentAlpha = 1.0f;
                    if (state.IsUsingFadeMaterials && state.OriginalSharedMaterials != null)
                    {
                        state.Renderer.sharedMaterials = state.OriginalSharedMaterials;
                        state.IsUsingFadeMaterials = false;
                    }

                    if (!isCurrentlyObstructing)
                    {
                        DestroyFadeMaterials(state);
                        toRemoveList.Add(state.Renderer);
                    }
                }
            }

            for (int i = 0; i < toRemoveList.Count; i++)
            {
                trackedRenderers.Remove(toRemoveList[i]);
            }
        }

        private void DetectObstructingTreeCanopies(Vector3 camPos, Vector3 torsoPos, Vector3 headPos)
        {
            var wg = WorldGenerator.Instance;
            if (wg == null)
            {
                if (cachedWorldGen == null) cachedWorldGen = FindAnyObjectByType<WorldGenerator>();
                wg = cachedWorldGen;
            }
            if (wg == null) return;

            var trees = wg.TreeRenderers;
            if (trees == null || trees.Count == 0) return;

            Vector3 torsoVec = torsoPos - camPos;
            float torsoDist = torsoVec.magnitude;
            if (torsoDist <= 0.05f) return;
            Vector3 torsoDir = torsoVec / torsoDist;

            Vector3 headVec = headPos - camPos;
            float headDist = headVec.magnitude;
            Vector3 headDir = headDist > 0.05f ? headVec / headDist : torsoDir;

            float pad = fadeConfig.TreeCanopyPadding;

            for (int i = 0; i < trees.Count; i++)
            {
                Renderer r = trees[i];
                if (r == null || !r.enabled || currentHits.Contains(r)) continue;

                Transform t = r.transform;
                Vector3 treePos = t.position;
                float scale = Mathf.Max(0.25f, t.lossyScale.y);

                if (DoesRayHitTreeSilhouette(camPos, torsoDir, torsoDist, treePos, scale, pad) ||
                    DoesRayHitTreeSilhouette(camPos, headDir, headDist, treePos, scale, pad))
                {
                    currentHits.Add(r);
                    RegisterOrUpdateFadedRenderer(r, fadeConfig.TreeFadedAlpha);
                }
            }
        }

        private static bool DoesRayHitTreeSilhouette(Vector3 camPos, Vector3 rayDir, float rayDist, Vector3 treePos, float scale, float padding)
        {
            float dirX = rayDir.x;
            float dirZ = rayDir.z;
            float dirXZSq = dirX * dirX + dirZ * dirZ;
            if (dirXZSq < 0.0001f) return false;

            float dx = treePos.x - camPos.x;
            float dz = treePos.z - camPos.z;
            float tAxis = (dx * dirX + dz * dirZ) / dirXZSq;

            // Only fade trees positioned between the camera and the player
            if (tAxis < 0f || tAxis >= rayDist - 0.15f) return false;

            float rayAtX = camPos.x + dirX * tAxis;
            float rayAtY = camPos.y + rayDir.y * tAxis;
            float rayAtZ = camPos.z + dirZ * tAxis;

            float localY = (rayAtY - treePos.y) / scale;
            if (localY < -0.35f || localY > 6.3f) return false;

            float localRadius;
            if (localY < 1.35f)
            {
                localRadius = 0.50f;
            }
            else
            {
                float heightT = Mathf.Clamp01((localY - 1.35f) / 4.85f);
                localRadius = Mathf.Lerp(2.15f, 0.55f, heightT);
            }

            float worldRadius = localRadius * scale + padding;
            float ox = rayAtX - treePos.x;
            float oz = rayAtZ - treePos.z;
            return (ox * ox + oz * oz) <= (worldRadius * worldRadius);
        }

        private void RegisterOrUpdateFadedRenderer(Renderer rend, float desiredFadedAlpha)
        {
            if (!trackedRenderers.TryGetValue(rend, out var state))
            {
                Material[] origMats = rend.sharedMaterials;
                int count = origMats != null ? origMats.Length : 0;
                Material[] fadeMats = new Material[count];
                Color[] origColors = new Color[count];

                for (int i = 0; i < count; i++)
                {
                    Material src = origMats[i];
                    fadeMats[i] = CreateTransparentClone(src);

                    Color orig = Color.white;
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseColor"))
                            orig = src.GetColor("_BaseColor");
                        else if (src.HasProperty("_Color"))
                            orig = src.GetColor("_Color");
                    }
                    origColors[i] = orig;
                }

                state = new FadedRendererState
                {
                    Renderer = rend,
                    TargetAlpha = desiredFadedAlpha,
                    DesiredFadedAlpha = desiredFadedAlpha,
                    CurrentAlpha = 1.0f,
                    OriginalSharedMaterials = origMats,
                    FadeMaterials = fadeMats,
                    OriginalColors = origColors,
                    IsUsingFadeMaterials = false,
                    LastObstructedTime = Time.time
                };
                trackedRenderers.Add(rend, state);
            }
            else
            {
                state.DesiredFadedAlpha = desiredFadedAlpha;
                state.LastObstructedTime = Time.time;
            }
        }

        private static Material CreateTransparentClone(Material source)
        {
            if (source == null) return null;
            Material mat = new Material(source)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_BlendModePreserveSpecular")) mat.SetFloat("_BlendModePreserveSpecular", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static void ApplyAlphaToFadeMaterials(FadedRendererState state)
        {
            if (state.FadeMaterials == null) return;
            for (int i = 0; i < state.FadeMaterials.Length; i++)
            {
                Material mat = state.FadeMaterials[i];
                if (mat == null) continue;

                Color c = state.OriginalColors != null && i < state.OriginalColors.Length
                    ? state.OriginalColors[i]
                    : Color.white;
                c.a = state.CurrentAlpha;

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        private static void DestroyFadeMaterials(FadedRendererState state)
        {
            if (state.FadeMaterials == null) return;
            for (int i = 0; i < state.FadeMaterials.Length; i++)
            {
                if (state.FadeMaterials[i] != null)
                {
                    if (Application.isPlaying) Destroy(state.FadeMaterials[i]);
                    else DestroyImmediate(state.FadeMaterials[i]);
                    state.FadeMaterials[i] = null;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in trackedRenderers)
            {
                var state = kvp.Value;
                if (state.Renderer != null && state.IsUsingFadeMaterials && state.OriginalSharedMaterials != null)
                {
                    state.Renderer.sharedMaterials = state.OriginalSharedMaterials;
                    state.IsUsingFadeMaterials = false;
                }
                DestroyFadeMaterials(state);
            }
            trackedRenderers.Clear();
            toRemoveList.Clear();
        }
    }
}
