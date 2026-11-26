using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeadDepths.AI;
using DeadDepths.Core;

namespace DeadDepths.UI
{
    /// <summary>
    /// Pooled world-space-projected uGUI health bars for injured or aggroed zombies.
    /// Hidden automatically during Main Menu and when [F1] Debug Mode is active.
    /// </summary>
    public class ZombieHealthBarUI : MonoBehaviour
    {
        private struct BarWidget
        {
            public RectTransform Root;
            public Image Fill;
        }

        private const int MaxBars = 24;
        private BarWidget[] barPool;
        private DeadDepthsUIRoot uiRoot;
        private Camera cachedMainCamera;

        private void Awake()
        {
            uiRoot = GetComponentInParent<DeadDepthsUIRoot>();
            BuildPool();
        }

        private void BuildPool()
        {
            if (barPool != null) return;
            barPool = new BarWidget[MaxBars];
            for (int i = 0; i < MaxBars; i++)
            {
                var root = uiRoot.CreatePanel(transform, $"ZombieHP_{i}",
                    Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 8f),
                    DeadDepthsUIArtFactory.BarTrackSprite, new Color(0.06f, 0.07f, 0.09f, 0.85f), false);

                var fillImg = uiRoot.CreateImage(root, "Fill",
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                    DeadDepthsUIArtFactory.BarFillSprite, new Color(0.86f, 0.22f, 0.22f, 0.95f), false);
                fillImg.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                fillImg.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;

                root.gameObject.SetActive(false);
                barPool[i] = new BarWidget { Root = root, Fill = fillImg };
            }
        }

        private void LateUpdate()
        {
            if (barPool == null || uiRoot == null || uiRoot.MasterCanvas == null) return;

            var gm = GameManager.Instance;
            bool hideAll = (gm != null && (gm.IsInMainMenu || gm.IsPaused)) ||
                           (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible) ||
                           (DeadDepthsDebugMode.Instance != null && DeadDepthsDebugMode.Instance.IsDebugVisible);

            if (hideAll)
            {
                HideUnusedFrom(0);
                return;
            }

            if (cachedMainCamera == null) cachedMainCamera = Camera.main;
            Camera cam = cachedMainCamera;
            if (cam == null)
            {
                HideUnusedFrom(0);
                return;
            }

            float scale = uiRoot.MasterCanvas.scaleFactor;
            if (scale <= 0.001f) scale = 1f;

            var zombies = ZombieController.ActiveZombies;
            int used = 0;
            for (int i = 0; i < zombies.Count && used < MaxBars; i++)
            {
                var z = zombies[i];
                if (z == null || z.IsDead) continue;

                bool injured = z.CurrentHealth < z.AiConfig.MaxHealth - 0.1f;
                bool aggro = z.CurrentState == ZombieState.Chase || z.CurrentState == ZombieState.Attack;
                if (!injured && !aggro) continue;

                float yOffset = z.Archetype == ZombieArchetype.Charger ? 2.65f : 2.15f;
                Vector3 sp = cam.WorldToScreenPoint(z.transform.position + Vector3.up * yOffset);
                if (sp.z <= 0f || sp.x < -40f || sp.x > Screen.width + 40f || sp.y < -40f || sp.y > Screen.height + 40f)
                    continue;

                ref BarWidget w = ref barPool[used];
                w.Root.gameObject.SetActive(true);
                w.Root.anchoredPosition = new Vector2(sp.x / scale, sp.y / scale);
                w.Fill.fillAmount = Mathf.Clamp01(z.CurrentHealth / Mathf.Max(1f, z.AiConfig.MaxHealth));
                w.Fill.color = z.Archetype == ZombieArchetype.Charger
                    ? new Color(0.95f, 0.35f, 0.18f, 0.96f)
                    : (z.Archetype == ZombieArchetype.Spitter ? new Color(0.32f, 0.88f, 0.42f, 0.96f) : new Color(0.88f, 0.20f, 0.22f, 0.96f));
                used++;
            }

            HideUnusedFrom(used);
        }

        private void HideUnusedFrom(int startIdx)
        {
            for (int i = startIdx; i < barPool.Length; i++)
            {
                if (barPool[i].Root != null && barPool[i].Root.gameObject.activeSelf)
                {
                    barPool[i].Root.gameObject.SetActive(false);
                }
            }
        }
    }
}
