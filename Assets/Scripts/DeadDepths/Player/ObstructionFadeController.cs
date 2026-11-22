using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DeadDepths.AI;
using DeadDepths.Interactive;

namespace DeadDepths.Player
{
    /// <summary>
    /// Fades obstructing foreground geometry between the isometric camera and the player.
    /// Swaps Opaque URP Lit materials to transient Transparent URP Lit clones while fading
    /// and restores the original shared materials once unobstructed or destroyed.
    /// ASD-STE100: This module makes obstructing obstacles transparent so the player remains visible.
    /// Follows Section 2.6 of the specification.
    /// </summary>
    public class ObstructionFadeController : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Range(0.05f, 0.9f)] public float FadedAlpha = 0.25f;
            public float FadeSpeed = 8.0f;
            public float SphereCastRadius = 0.6f;
            public LayerMask ObstructionLayers = ~0; // Default all layers
        }
        #endregion

        [SerializeField] private Config fadeConfig = new Config();
        public Config FadeConfig => fadeConfig;
        [SerializeField] private Transform playerTarget;

        private class FadedRendererState
        {
            public Renderer Renderer;
            public float TargetAlpha;
            public float CurrentAlpha;
            public Material[] OriginalSharedMaterials;
            public Material[] FadeMaterials;
            public Color[] OriginalColors;
            public bool IsUsingFadeMaterials;
            public float LastObstructedTime;
        }

        private readonly Dictionary<Renderer, FadedRendererState> trackedRenderers = new Dictionary<Renderer, FadedRendererState>();
        private readonly List<Renderer> toRemoveList = new List<Renderer>();

        private void Start()
        {
            if (playerTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }
        }

        private void Update()
        {
            UpdateFade(Time.deltaTime);
        }

        /// <summary>
        /// Evaluates camera-to-player obstructions and updates transparent material swaps over dt seconds.
        /// </summary>
        public void UpdateFade(float dt)
        {
            if (playerTarget == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTarget = p.transform;
                if (playerTarget == null) return;
            }

            Vector3 camPos = transform.position;
            Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
            Vector3 dir = targetPos - camPos;
            float dist = dir.magnitude;

            HashSet<Renderer> currentHits = new HashSet<Renderer>();
            if (dist > 0.01f)
            {
                // 1. Raycast / SphereCast to detect obstructions (ignore triggers & actors so tints/ground are preserved)
                RaycastHit[] hits = Physics.SphereCastAll(camPos, fadeConfig.SphereCastRadius, dir / dist, dist, fadeConfig.ObstructionLayers, QueryTriggerInteraction.Ignore);

                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.transform.IsChildOf(playerTarget) || hit.collider.transform == playerTarget) continue;
                    if (hit.collider.gameObject.name == "PlayableGround") continue;
                    if (hit.collider.GetComponentInParent<ZombieController>() != null) continue;
                    if (hit.collider.GetComponentInParent<SuppliesCrate>() != null) continue;
                    if (hit.collider.GetComponentInParent<BlockingObject>() != null) continue;

                    var rends = hit.collider.GetComponentsInChildren<Renderer>();
                    foreach (var r in rends)
                    {
                        if (r == null || !r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
                        currentHits.Add(r);
                        RegisterOrUpdateFadedRenderer(r);
                    }
                }
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
                state.TargetAlpha = isCurrentlyObstructing ? fadeConfig.FadedAlpha : 1.0f;
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

            foreach (var r in toRemoveList)
            {
                trackedRenderers.Remove(r);
            }
        }

        private void RegisterOrUpdateFadedRenderer(Renderer rend)
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
                    TargetAlpha = fadeConfig.FadedAlpha,
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
