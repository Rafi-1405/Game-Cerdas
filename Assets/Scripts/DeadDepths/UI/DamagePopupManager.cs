using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DeadDepths.Core;

namespace DeadDepths.UI
{
    /// <summary>
    /// Object-pooled zero-GC world-to-screen floating damage, crit, heal, and status popups.
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        private struct ActivePopup
        {
            public TMP_Text Label;
            public RectTransform Rect;
            public Vector3 WorldPos;
            public Vector3 WorldVel;
            public float Age;
            public float Lifetime;
            public Color BaseColor;
            public float BaseScale;
            public bool InUse;
        }

        private const int PoolCapacity = 32;
        private ActivePopup[] pool;
        private DeadDepthsUIRoot uiRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            uiRoot = GetComponentInParent<DeadDepthsUIRoot>();
            BuildPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildPool()
        {
            if (pool != null) return;
            pool = new ActivePopup[PoolCapacity];
            for (int i = 0; i < PoolCapacity; i++)
            {
                var tmp = uiRoot != null
                    ? uiRoot.CreateText(transform, $"Popup_{i}", string.Empty, 24f, FontStyles.Bold, Color.white,
                        TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(220f, 44f))
                    : null;
                if (tmp != null)
                {
                    tmp.raycastTarget = false;
                    tmp.gameObject.SetActive(false);
                    pool[i] = new ActivePopup
                    {
                        Label = tmp,
                        Rect = tmp.rectTransform,
                        InUse = false
                    };
                }
            }
        }

        public void ShowDamage(Vector3 worldPos, int damage, bool isCrit)
        {
            if (isCrit)
                SpawnPopup(worldPos, $"CRIT! -{damage}", new Color(1f, 0.72f, 0.16f), 1.25f, 0.85f);
            else
                SpawnPopup(worldPos, $"-{damage}", new Color(0.98f, 0.92f, 0.88f), 1.0f, 0.65f);
        }

        public void ShowHeal(Vector3 worldPos, int amount)
        {
            SpawnPopup(worldPos, $"+{amount} HP", new Color(0.32f, 0.95f, 0.52f), 1.05f, 0.85f);
        }

        public void ShowText(Vector3 worldPos, string msg, Color color)
        {
            SpawnPopup(worldPos, msg, color, 1.05f, 0.95f);
        }

        private void SpawnPopup(Vector3 worldPos, string text, Color color, float baseScale, float lifetime)
        {
            if (pool == null || !DeadDepthsUIRoot.IsCanvasActive) return;
            if (GameManager.Instance != null && GameManager.Instance.IsInMainMenu) return;

            int slot = -1;
            float oldest = -1f;
            int oldestIdx = 0;
            for (int i = 0; i < PoolCapacity; i++)
            {
                if (!pool[i].InUse)
                {
                    slot = i;
                    break;
                }
                if (pool[i].Age > oldest)
                {
                    oldest = pool[i].Age;
                    oldestIdx = i;
                }
            }
            if (slot < 0) slot = oldestIdx;

            ref ActivePopup p = ref pool[slot];
            if (p.Label == null) return;

            p.InUse = true;
            p.Age = 0f;
            p.Lifetime = lifetime;
            p.BaseColor = color;
            p.BaseScale = baseScale;
            p.WorldPos = worldPos + new Vector3(UnityEngine.Random.Range(-0.22f, 0.22f), 0f, UnityEngine.Random.Range(-0.22f, 0.22f));
            p.WorldVel = new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 1.5f, 0f);

            p.Label.text = text;
            p.Label.color = color;
            p.Label.gameObject.SetActive(true);
            PositionPopup(ref p, Camera.main);
        }

        private void Update()
        {
            if (pool == null) return;
            Camera cam = Camera.main;
            float dt = Time.unscaledDeltaTime;
            bool reducedMotion = SettingsModalUI.IsReducedMotionEnabled;

            for (int i = 0; i < PoolCapacity; i++)
            {
                if (!pool[i].InUse) continue;
                ref ActivePopup p = ref pool[i];
                p.Age += dt;
                if (p.Age >= p.Lifetime)
                {
                    p.InUse = false;
                    if (p.Label != null) p.Label.gameObject.SetActive(false);
                    continue;
                }

                if (!reducedMotion)
                {
                    p.WorldPos += p.WorldVel * dt;
                    p.WorldVel.y = Mathf.Max(0.25f, p.WorldVel.y - dt * 1.6f);
                }

                float norm = p.Age / p.Lifetime;
                float alpha = norm > 0.6f ? Mathf.Clamp01(1f - (norm - 0.6f) / 0.4f) : 1f;
                Color c = p.BaseColor;
                c.a = alpha;
                p.Label.color = c;

                float pop = reducedMotion ? 1f : (norm < 0.15f ? Mathf.Lerp(0.7f, 1.18f, norm / 0.15f) : Mathf.Lerp(1.18f, 1f, (norm - 0.15f) / 0.85f));
                p.Rect.localScale = Vector3.one * (p.BaseScale * pop);

                PositionPopup(ref p, cam);
            }
        }

        private void PositionPopup(ref ActivePopup p, Camera cam)
        {
            if (cam == null || uiRoot == null || uiRoot.MasterCanvas == null) return;
            Vector3 sp = cam.WorldToScreenPoint(p.WorldPos);
            if (sp.z <= 0f)
            {
                p.Label.enabled = false;
                return;
            }
            p.Label.enabled = true;
            float scale = uiRoot.MasterCanvas.scaleFactor;
            if (scale <= 0.001f) scale = 1f;
            p.Rect.anchoredPosition = new Vector2(sp.x / scale, sp.y / scale);
        }
    }
}
