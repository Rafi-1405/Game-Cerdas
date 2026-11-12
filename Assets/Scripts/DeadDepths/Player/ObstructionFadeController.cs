using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.Player
{
    /// <summary>
    /// Fades obstructing foreground geometry between the isometric camera and the player.
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
        [SerializeField] private Transform playerTarget;

        private class FadedRendererState
        {
            public Renderer Renderer;
            public float TargetAlpha;
            public float CurrentAlpha;
            public MaterialPropertyBlock PropBlock;
            public Color OriginalColor;
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

            // 1. Raycast / SphereCast to detect obstructions
            RaycastHit[] hits = Physics.SphereCastAll(camPos, fadeConfig.SphereCastRadius, dir.normalized, dist, fadeConfig.ObstructionLayers);
            HashSet<Renderer> currentHits = new HashSet<Renderer>();

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(playerTarget) || hit.collider.transform == playerTarget) continue;

                var rends = hit.collider.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    if (r == null || !r.enabled) continue;
                    currentHits.Add(r);
                    RegisterOrUpdateFadedRenderer(r);
                }
            }

            // 2. Update alpha transitions
            float dt = Time.deltaTime;
            toRemoveList.Clear();

            foreach (var kvp in trackedRenderers)
            {
                var state = kvp.Value;
                bool isCurrentlyObstructing = currentHits.Contains(state.Renderer);

                state.TargetAlpha = isCurrentlyObstructing ? fadeConfig.FadedAlpha : 1.0f;
                state.CurrentAlpha = Mathf.MoveTowards(state.CurrentAlpha, state.TargetAlpha, dt * fadeConfig.FadeSpeed);

                // Apply to MaterialPropertyBlock
                state.Renderer.GetPropertyBlock(state.PropBlock);
                Color c = state.OriginalColor;
                c.a = state.CurrentAlpha;
                state.PropBlock.SetColor("_BaseColor", c);
                state.PropBlock.SetColor("_Color", c);
                state.Renderer.SetPropertyBlock(state.PropBlock);

                // If fully restored and not obstructing, remove tracking
                if (!isCurrentlyObstructing && Mathf.Approximately(state.CurrentAlpha, 1.0f))
                {
                    toRemoveList.Add(state.Renderer);
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
                Color orig = Color.white;
                if (rend.sharedMaterial != null)
                {
                    if (rend.sharedMaterial.HasProperty("_BaseColor"))
                        orig = rend.sharedMaterial.GetColor("_BaseColor");
                    else if (rend.sharedMaterial.HasProperty("_Color"))
                        orig = rend.sharedMaterial.GetColor("_Color");
                }

                state = new FadedRendererState
                {
                    Renderer = rend,
                    TargetAlpha = fadeConfig.FadedAlpha,
                    CurrentAlpha = 1.0f,
                    PropBlock = new MaterialPropertyBlock(),
                    OriginalColor = orig,
                    LastObstructedTime = Time.time
                };
                trackedRenderers.Add(rend, state);
            }
            else
            {
                state.LastObstructedTime = Time.time;
            }
        }
    }
}
