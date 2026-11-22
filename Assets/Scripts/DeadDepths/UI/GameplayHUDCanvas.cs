using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;
using UTS.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadDepths.UI
{
    /// <summary>
    /// Publish-grade uGUI Gameplay HUD (>82% clear viewport center):
    /// - Bottom-Left: HP bar (instant snap + 0.35s trailing ghost-drain), Stamina bar, AIM/CROUCH/LIGHT [T]/ADR badges
    /// - Bottom-Center: 3-Slot Quick-Use Belt ([1] Bandage, [2] Medkit, [3] Adrenaline)
    /// - Top-Left: Mission Objective Card (Supply Intel X/3 -> Hold Helipad 4.0s)
    /// - Top-Right: Sector, Score, Timer (tabular numerals), collapsible Corner Minimap [M], minimal DEV pill
    /// - Overlays: Crate Search Ring, Extraction Hold Ring, Screen-Edge Evac Compass, Item Toasts, Hurt Vignette, YOU DIED
    /// </summary>
    public class GameplayHUDCanvas : MonoBehaviour
    {
        public static GameplayHUDCanvas Instance { get; private set; }

        private DeadDepthsUIRoot uiRoot;
        private GameObject hudRoot;

        // Vignette & Death
        private Image hurtVignette;
        private float hurtFlashAlpha = 0f;
        private float lastRecordedHp = -1f;
        private GameObject deathOverlay;
        private TMP_Text deathStatsText;

        // Bottom-Left Bars & Badges
        private Image hpFillImg, hpGhostImg, stamFillImg;
        private TMP_Text hpText, stamText;
        private float hpGhostRatio = 1f;
        private float hpGhostDelay = 0f;
        private GameObject badgeAim, badgeCrouch, badgeLight, badgeAdrenaline;
        private TMP_Text badgeLightText;

        // Bottom-Center Belt
        private readonly Image[] beltCooldownOverlays = new Image[3];
        private readonly TMP_Text[] beltCountTexts = new TMP_Text[3];
        private readonly Image[] beltSlotFrames = new Image[3];

        // Top-Left Objective
        private TMP_Text objTitleText, objDetailText, objDistText;

        // Top-Right Score & Minimap
        private TMP_Text sectorScoreText, timerText;
        private GameObject devPill;
        private GameObject minimapBody;
        private RawImage minimapRaw;
        private RectTransform minimapPlayerDot, minimapEvacDot;
        private Texture2D minimapTex;
        private int lastMapSeed = -1;
        private bool minimapVisible = true;

        // Center / World Prompts & Rings
        private GameObject cratePromptPanel;
        private TMP_Text cratePromptLabel;
        private Image crateRingFill;
        private GameObject evacHoldPanel;
        private TMP_Text evacHoldLabel;
        private Image evacRingFill;
        private RectTransform evacCompassArrow;

        // Item Toast Stack
        private struct ToastEntry
        {
            public GameObject Root;
            public TMP_Text Label;
            public float Remaining;
        }
        private readonly List<ToastEntry> toastPool = new List<ToastEntry>(4);

        private DeadDepthsPlayerController boundPlayer;

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
            minimapVisible = SettingsModalUI.IsMinimapEnabled;
            BuildUI();
        }

        private void Start()
        {
            ItemNotificationManager.OnNotificationPosted += OnItemNotificationPosted;
            GameManager.Instance?.AddBeltItem(BeltItemType.Bandage, 0);
        }

        private void OnDestroy()
        {
            ItemNotificationManager.OnNotificationPosted -= OnItemNotificationPosted;
            if (boundPlayer != null) boundPlayer.OnHealthChanged -= OnPlayerHealthChanged;
            if (minimapTex != null)
            {
                if (Application.isPlaying) Destroy(minimapTex);
                else DestroyImmediate(minimapTex);
                minimapTex = null;
            }
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            GameObject root = new GameObject("GameplayHUDRoot");
            root.transform.SetParent(transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            hudRoot = root;

            // 1. Hurt / Low-HP Radial Vignette (passive, raycastTarget = false)
            hurtVignette = uiRoot.CreateImage(rootRt, "HurtVignette",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.RadialVignetteSprite, new Color(1f, 1f, 1f, 0f), false);

            // 2. Top-Left Mission Objective Card
            var objCard = uiRoot.CreatePanel(rootRt, "ObjectiveCard",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -22f), new Vector2(360f, 108f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);

            uiRoot.CreateImage(objCard, "ObjIcon",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(26f, 26f),
                DeadDepthsUIArtFactory.IconSupplyIntel, Color.white, false);

            objTitleText = uiRoot.CreateText(objCard, "ObjTitle", "MISSION OBJECTIVE", 13f, FontStyles.Bold,
                new Color(0.95f, 0.82f, 0.34f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(48f, -14f), new Vector2(-60f, 24f));

            objDetailText = uiRoot.CreateText(objCard, "ObjDetail", "SUPPLY INTEL: 0 / 3", 20f, FontStyles.Bold,
                new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(-32f, 28f));

            objDistText = uiRoot.CreateText(objCard, "ObjDist", "EVAC HELIPAD: --m", 14f, FontStyles.Normal,
                new Color(0.64f, 0.78f, 0.72f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -74f), new Vector2(-32f, 22f));

            // 3. Top-Right Score, Sector, Tabular Timer, DEV Pill & Collapsible Corner Minimap [M]
            var infoCard = uiRoot.CreatePanel(rootRt, "ScoreSectorCard",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(310f, 68f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);

            sectorScoreText = uiRoot.CreateText(infoCard, "SectorScore", "SECTOR 1   |   0 PTS", 17f, FontStyles.Bold,
                new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Right,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(-32f, 24f));

            timerText = uiRoot.CreateText(infoCard, "Timer", "<mspace=0.58em>00:00.0</mspace>", 16f, FontStyles.Bold,
                new Color(0.58f, 0.80f, 0.95f), TextAlignmentOptions.Right,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -36f), new Vector2(-32f, 22f));

            // Minimal DEV pill (only shown if [F1] or [G] is active)
            var devRt = uiRoot.CreatePanel(rootRt, "DevPill",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(180f, 28f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, new Color(0.95f, 0.65f, 0.12f, 0.92f), false);
            uiRoot.CreateText(devRt, "DevLabel", "DEV / GOD MODE", 13f, FontStyles.Bold,
                new Color(0.06f, 0.08f, 0.10f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            devPill = devRt.gameObject;
            devPill.SetActive(false);

            // Corner Minimap [M]
            var mapHeader = uiRoot.CreateButton(rootRt, "MinimapToggleBtn", "TACTICAL MAP [M]", 12f,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -96f), new Vector2(176f, 26f),
                new Color(0.10f, 0.13f, 0.18f, 0.92f), new Color(0.18f, 0.24f, 0.32f, 1f), ToggleMinimap);

            var mapCard = uiRoot.CreatePanel(rootRt, "MinimapCard",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -126f), new Vector2(176f, 176f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);
            minimapBody = mapCard.gameObject;

            GameObject rawGo = new GameObject("MinimapRaw");
            rawGo.transform.SetParent(mapCard, false);
            var rawRt = rawGo.AddComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = new Vector2(8f, 8f);
            rawRt.offsetMax = new Vector2(-8f, -8f);
            minimapRaw = rawGo.AddComponent<RawImage>();
            minimapRaw.raycastTarget = false;

            var evacImg = uiRoot.CreateImage(rawRt, "EvacDot", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f),
                DeadDepthsUIArtFactory.CircleSolidSprite, new Color(0.22f, 0.98f, 0.45f, 1f), false);
            minimapEvacDot = evacImg.rectTransform;

            var plImg = uiRoot.CreateImage(rawRt, "PlayerDot", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f),
                DeadDepthsUIArtFactory.CircleSolidSprite, new Color(0.25f, 0.85f, 1f, 1f), false);
            minimapPlayerDot = plImg.rectTransform;
            minimapBody.SetActive(minimapVisible);

            // 4. Bottom-Left Vital Bars (HP with 0.35s Ghost Drain + Stamina) & Status Badges
            var vitalsCard = uiRoot.CreatePanel(rootRt, "VitalsCard",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 22f), new Vector2(372f, 118f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);

            // HP Track + Ghost + Instant Fill
            var hpTrack = uiRoot.CreateImage(vitalsCard, "HPTrack",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-28f, 26f),
                DeadDepthsUIArtFactory.BarTrackSprite, Color.white, false);

            hpGhostImg = uiRoot.CreateImage(hpTrack.rectTransform, "HPGhost",
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.98f, 0.82f, 0.45f, 0.88f), false);
            hpGhostImg.rectTransform.offsetMin = new Vector2(3f, 3f);
            hpGhostImg.rectTransform.offsetMax = new Vector2(-3f, -3f);
            hpGhostImg.type = Image.Type.Filled;
            hpGhostImg.fillMethod = Image.FillMethod.Horizontal;

            hpFillImg = uiRoot.CreateImage(hpTrack.rectTransform, "HPFill",
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.84f, 0.19f, 0.22f, 1f), false);
            hpFillImg.rectTransform.offsetMin = new Vector2(3f, 3f);
            hpFillImg.rectTransform.offsetMax = new Vector2(-3f, -3f);
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;

            hpText = uiRoot.CreateText(hpTrack.rectTransform, "HPText", "HP 100 / 100", 14f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-20f, 0f));

            // Stamina Track
            var stamTrack = uiRoot.CreateImage(vitalsCard, "StamTrack",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(-28f, 18f),
                DeadDepthsUIArtFactory.BarTrackSprite, Color.white, false);

            stamFillImg = uiRoot.CreateImage(stamTrack.rectTransform, "StamFill",
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.24f, 0.68f, 0.95f, 1f), false);
            stamFillImg.rectTransform.offsetMin = new Vector2(3f, 3f);
            stamFillImg.rectTransform.offsetMax = new Vector2(-3f, -3f);
            stamFillImg.type = Image.Type.Filled;
            stamFillImg.fillMethod = Image.FillMethod.Horizontal;

            stamText = uiRoot.CreateText(stamTrack.rectTransform, "StamText", "STAMINA 100%", 11f, FontStyles.Bold,
                new Color(0.90f, 0.96f, 1f), TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-20f, 0f));

            // Tactical Status Badges (fit cleanly inside 372px VitalsCard)
            badgeAim = CreateStatusBadge(vitalsCard, "AIM [RMB]", 14f, new Color(0.95f, 0.80f, 0.25f));
            badgeCrouch = CreateStatusBadge(vitalsCard, "CROUCH [C]", 100f, new Color(0.45f, 0.85f, 0.65f));
            badgeLight = CreateStatusBadge(vitalsCard, "LIGHT [T]", 186f, new Color(0.95f, 0.92f, 0.60f), out badgeLightText);
            badgeAdrenaline = CreateStatusBadge(vitalsCard, "ADR", 272f, new Color(0.30f, 0.88f, 1f));

            // 5. Bottom-Center 3-Slot Quick-Use Belt ([1] Bandage, [2] Medkit, [3] Adrenaline)
            var beltRoot = uiRoot.CreatePanel(rootRt, "QuickBelt",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(274f, 84f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);

            CreateBeltSlot(beltRoot, 0, "[1]", "BANDAGE", DeadDepthsUIArtFactory.IconBandage, -84f);
            CreateBeltSlot(beltRoot, 1, "[2]", "MEDKIT", DeadDepthsUIArtFactory.IconMedkit, 0f);
            CreateBeltSlot(beltRoot, 2, "[3]", "ADREN", DeadDepthsUIArtFactory.IconAdrenaline, 84f);

            // 6. Crate Search Prompt & Radial Ring
            var crateRt = uiRoot.CreatePanel(rootRt, "CratePromptPanel",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(360f, 52f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);
            cratePromptPanel = crateRt.gameObject;

            var crateRingBg = uiRoot.CreateImage(crateRt, "RingBg",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(34f, 34f),
                DeadDepthsUIArtFactory.CircleRingSprite, new Color(0.25f, 0.28f, 0.34f, 0.85f), false);
            crateRingFill = uiRoot.CreateImage(crateRingBg.rectTransform, "RingFill",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.CircleRingSprite, new Color(1f, 0.88f, 0.30f, 1f), false);
            crateRingFill.type = Image.Type.Filled;
            crateRingFill.fillMethod = Image.FillMethod.Radial360;
            crateRingFill.fillOrigin = (int)Image.Origin360.Top;

            cratePromptLabel = uiRoot.CreateText(crateRt, "Label", "[HOLD E] SEARCH CRATE", 16f, FontStyles.Bold,
                new Color(1f, 0.90f, 0.40f), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(-64f, 0f));
            cratePromptPanel.SetActive(false);

            // 7. Extraction Hold Ring
            var evacRt = uiRoot.CreatePanel(rootRt, "EvacHoldPanel",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 194f), new Vector2(410f, 62f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);
            evacHoldPanel = evacRt.gameObject;

            var evacBg = uiRoot.CreateImage(evacRt, "RingBg",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f),
                DeadDepthsUIArtFactory.CircleRingSprite, new Color(0.22f, 0.30f, 0.26f, 0.9f), false);
            evacRingFill = uiRoot.CreateImage(evacBg.rectTransform, "RingFill",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.CircleRingSprite, new Color(0.28f, 0.96f, 0.52f, 1f), false);
            evacRingFill.type = Image.Type.Filled;
            evacRingFill.fillMethod = Image.FillMethod.Radial360;
            evacRingFill.fillOrigin = (int)Image.Origin360.Top;

            evacHoldLabel = uiRoot.CreateText(evacRt, "Label", "HOLD TO EXTRACT", 16f, FontStyles.Bold,
                new Color(0.38f, 0.96f, 0.58f), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(-74f, 0f));
            evacHoldPanel.SetActive(false);

            // 8. Screen-Edge Evac Compass Arrow
            var compImg = uiRoot.CreateImage(rootRt, "EvacCompass",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f),
                DeadDepthsUIArtFactory.IconHelipad, new Color(0.28f, 0.96f, 0.55f, 0.85f), false);
            evacCompassArrow = compImg.rectTransform;

            // 9. Item Toast Stack (Top-Center-Left below Objective)
            for (int i = 0; i < 4; i++)
            {
                var trt = uiRoot.CreatePanel(rootRt, $"ItemToast_{i}",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -142f - i * 38f), new Vector2(260f, 32f),
                    DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);
                var ttxt = uiRoot.CreateText(trt, "Txt", "", 14f, FontStyles.Bold,
                    new Color(0.38f, 0.96f, 0.58f), TextAlignmentOptions.Left,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(-24f, 0f));
                trt.gameObject.SetActive(false);
                toastPool.Add(new ToastEntry { Root = trt.gameObject, Label = ttxt, Remaining = 0f });
            }

            // 10. YOU DIED uGUI Overlay
            var deathScrim = uiRoot.CreatePanel(rootRt, "DeathOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                null, new Color(0.05f, 0.01f, 0.01f, 0.72f), true);
            deathOverlay = deathScrim.gameObject;

            var deathCard = uiRoot.CreatePanel(deathScrim, "DeathCard",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 290f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, true);

            uiRoot.CreateText(deathCard, "YouDiedTitle", "YOU DIED", 44f, FontStyles.Bold,
                new Color(0.95f, 0.20f, 0.20f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(400f, 54f));

            deathStatsText = uiRoot.CreateText(deathCard, "DeathStats", "FINAL SCORE: 0   |   SECTOR: 1", 18f, FontStyles.Bold,
                new Color(0.95f, 0.88f, 0.45f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(420f, 28f));

            uiRoot.CreateButton(deathCard, "RetryButton", "RETRY RUN", 20f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-102f, 58f), new Vector2(188f, 50f),
                new Color(0.68f, 0.16f, 0.16f, 1f), new Color(0.88f, 0.24f, 0.24f, 1f), () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    GameManager.Instance?.RetryRun();
                });

            uiRoot.CreateButton(deathCard, "MainMenuButton", "MAIN MENU", 18f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(102f, 58f), new Vector2(188f, 50f),
                new Color(0.16f, 0.20f, 0.26f, 1f), new Color(0.24f, 0.30f, 0.40f, 1f), () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    GameManager.Instance?.EnterMainMenu();
                });

            uiRoot.CreateText(deathCard, "RHint", "or press [R] to quick-restart", 14f, FontStyles.Normal,
                new Color(0.66f, 0.72f, 0.80f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(360f, 24f));

            deathOverlay.SetActive(false);
        }

        private GameObject CreateStatusBadge(RectTransform parent, string label, float xPos, Color accent)
        {
            return CreateStatusBadge(parent, label, xPos, accent, out _);
        }

        private GameObject CreateStatusBadge(RectTransform parent, string label, float xPos, Color accent, out TMP_Text textOut)
        {
            var rt = uiRoot.CreatePanel(parent, "Badge_" + label,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(xPos, 12f), new Vector2(82f, 22f),
                DeadDepthsUIArtFactory.BarTrackSprite, new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.95f), false);
            textOut = uiRoot.CreateText(rt, "Label", label, 11f, FontStyles.Bold, accent, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt.gameObject;
        }

        private void CreateBeltSlot(RectTransform parent, int idx, string keycap, string name, Sprite icon, float xOffset)
        {
            var slotRt = uiRoot.CreatePanel(parent, $"BeltSlot_{idx}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xOffset, 0f), new Vector2(74f, 68f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, new Color(0.14f, 0.17f, 0.22f, 1f), false);
            beltSlotFrames[idx] = slotRt.GetComponent<Image>();

            uiRoot.CreateImage(slotRt, "Icon",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(36f, 36f),
                icon, Color.white, false);

            var cdOverlay = uiRoot.CreateImage(slotRt, "Cooldown",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.04f, 0.05f, 0.08f, 0.72f), false);
            cdOverlay.type = Image.Type.Filled;
            cdOverlay.fillMethod = Image.FillMethod.Vertical;
            cdOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
            cdOverlay.fillAmount = 0f;
            beltCooldownOverlays[idx] = cdOverlay;

            uiRoot.CreateText(slotRt, "Key", keycap, 11f, FontStyles.Bold, new Color(0.68f, 0.78f, 0.88f), TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(34f, 18f));

            beltCountTexts[idx] = uiRoot.CreateText(slotRt, "Count", "0", 14f, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(32f, 20f));
        }

        private void OnItemNotificationPosted(string itemName, int quantity)
        {
            for (int i = 0; i < toastPool.Count; i++)
            {
                if (!toastPool[i].Root.activeSelf)
                {
                    var t = toastPool[i];
                    t.Remaining = 3.2f;
                    t.Label.text = $"+ {itemName} x{quantity}";
                    t.Root.SetActive(true);
                    toastPool[i] = t;
                    return;
                }
            }
            // Overwrite slot 0 if full
            var first = toastPool[0];
            first.Remaining = 3.2f;
            first.Label.text = $"+ {itemName} x{quantity}";
            first.Root.SetActive(true);
            toastPool[0] = first;
        }

        private void OnPlayerHealthChanged(float cur, float max)
        {
            if (lastRecordedHp >= 0f && cur < lastRecordedHp)
            {
                hurtFlashAlpha = 0.75f;
                hpGhostDelay = 0.35f;
            }
            lastRecordedHp = cur;
        }

        public void SyncMinimapPreference(bool show)
        {
            minimapVisible = show;
            if (minimapBody != null) minimapBody.SetActive(minimapVisible);
        }

        private void ToggleMinimap()
        {
            minimapVisible = !minimapVisible;
            if (minimapBody != null) minimapBody.SetActive(minimapVisible);
            AudioManager.Instance?.PlayCue(SfxCue.UI);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool inMenu = gm != null && gm.IsInMainMenu;
            bool compVisible = CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible;

            if (hudRoot != null) hudRoot.SetActive(!inMenu && !compVisible);
            if (inMenu || compVisible) return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                Keyboard.current.mKey.wasPressedThisFrame &&
                (gm == null || !gm.IsPaused) &&
                (boundPlayer == null || !boundPlayer.IsDead))
            {
                ToggleMinimap();
            }
#endif

            BindPlayerIfNeeded();
            UpdateToasts();
            UpdateVitalsAndBadges();
            UpdateObjectiveAndHeader();
            UpdateBelt();
            UpdateCrateAndExtractionOverlays();
            UpdateMinimap();
        }

        private void BindPlayerIfNeeded()
        {
            var current = DeadDepthsPlayerController.Instance;
            if (current == null)
            {
                var pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) current = pObj.GetComponent<DeadDepthsPlayerController>();
            }
            if (current != boundPlayer)
            {
                if (boundPlayer != null) boundPlayer.OnHealthChanged -= OnPlayerHealthChanged;
                boundPlayer = current;
                if (boundPlayer != null)
                {
                    lastRecordedHp = boundPlayer.CurrentHealth;
                    hpGhostRatio = boundPlayer.CurrentHealth / Mathf.Max(1f, boundPlayer.MaxHealth);
                    boundPlayer.OnHealthChanged += OnPlayerHealthChanged;
                }
            }
        }

        private void UpdateToasts()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < toastPool.Count; i++)
            {
                var t = toastPool[i];
                if (!t.Root.activeSelf) continue;
                t.Remaining -= dt;
                if (t.Remaining <= 0f)
                {
                    t.Root.SetActive(false);
                }
                toastPool[i] = t;
            }
        }

        private void UpdateVitalsAndBadges()
        {
            if (boundPlayer == null) return;

            float hpRatio = Mathf.Clamp01(boundPlayer.CurrentHealth / Mathf.Max(1f, boundPlayer.MaxHealth));
            float stamRatio = Mathf.Clamp01(boundPlayer.CurrentStamina / Mathf.Max(1f, boundPlayer.MaxStamina));

            if (hpFillImg != null) hpFillImg.fillAmount = hpRatio;

            if (hpGhostDelay > 0f)
            {
                hpGhostDelay -= Time.unscaledDeltaTime;
            }
            else
            {
                if (hpGhostRatio < hpRatio) hpGhostRatio = hpRatio;
                else hpGhostRatio = Mathf.MoveTowards(hpGhostRatio, hpRatio, Time.unscaledDeltaTime * 0.85f);
            }
            if (hpGhostImg != null) hpGhostImg.fillAmount = hpGhostRatio;

            if (hpText != null)
            {
                hpText.text = boundPlayer.IsGodMode
                    ? $"HP {Mathf.CeilToInt(boundPlayer.CurrentHealth)} / {Mathf.CeilToInt(boundPlayer.MaxHealth)} [GOD]"
                    : $"HP {Mathf.CeilToInt(boundPlayer.CurrentHealth)} / {Mathf.CeilToInt(boundPlayer.MaxHealth)}";
            }

            if (stamFillImg != null) stamFillImg.fillAmount = stamRatio;
            if (stamText != null) stamText.text = $"STAMINA {Mathf.CeilToInt(stamRatio * 100f)}%";

            // Hurt flash + Low-HP pulse vignette
            if (hurtFlashAlpha > 0f)
                hurtFlashAlpha = Mathf.MoveTowards(hurtFlashAlpha, 0f, Time.unscaledDeltaTime * 2.2f);

            float lowHpPulse = (!boundPlayer.IsDead && hpRatio <= 0.35f)
                ? (0.18f + 0.14f * Mathf.Sin(Time.unscaledTime * 5f))
                : 0f;
            if (hurtVignette != null)
            {
                float a = Mathf.Clamp01(Mathf.Max(hurtFlashAlpha, lowHpPulse));
                hurtVignette.color = new Color(1f, 1f, 1f, a);
            }

            if (badgeAim != null) badgeAim.SetActive(boundPlayer.IsAiming);
            if (badgeCrouch != null) badgeCrouch.SetActive(boundPlayer.IsCrouching);
            var flashlight = boundPlayer.GetComponent<DeadDepthsFlashlight>();
            if (badgeLight != null)
            {
                bool on = flashlight != null && flashlight.IsOn;
                badgeLight.SetActive(true);
                if (badgeLightText != null) badgeLightText.text = on ? "LIGHT ON" : "LIGHT OFF";
            }
            if (badgeAdrenaline != null)
            {
                badgeAdrenaline.SetActive(GameManager.Instance != null && GameManager.Instance.IsAdrenalineActive);
            }

            // Death Overlay
            if (deathOverlay != null)
            {
                deathOverlay.SetActive(boundPlayer.IsDead);
                if (boundPlayer.IsDead && deathStatsText != null && GameManager.Instance != null)
                {
                    deathStatsText.text = $"FINAL SCORE: {GameManager.Instance.TotalScore}   |   SECTOR: {GameManager.Instance.CurrentLevel}";
                }
            }
        }

        private void UpdateObjectiveAndHeader()
        {
            var gm = GameManager.Instance;
            int level = gm != null ? gm.CurrentLevel : 1;
            int score = gm != null ? gm.TotalScore : 0;
            float elapsed = PlayerPerformanceTracker.Instance != null ? PlayerPerformanceTracker.Instance.ElapsedLevelTime : 0f;

            if (sectorScoreText != null)
                sectorScoreText.text = $"SECTOR {level}   |   {score} PTS";

            if (timerText != null)
            {
                int mins = Mathf.FloorToInt(elapsed / 60f);
                float secs = elapsed % 60f;
                timerText.text = $"<mspace=0.58em>{mins:00}:{secs:04.1}</mspace>";
            }

            if (objDetailText != null && gm != null)
            {
                if (!gm.HasRequiredSupplyIntel)
                {
                    objDetailText.text = $"SUPPLY INTEL: {gm.SupplyIntelCollected} / {gm.RequiredSupplyIntel}";
                    objDetailText.color = new Color(1f, 0.88f, 0.35f);
                }
                else
                {
                    objDetailText.text = "REACH & HOLD HELIPAD (4.0s)";
                    objDetailText.color = new Color(0.35f, 0.96f, 0.56f);
                }
            }

            if (objDistText != null && boundPlayer != null && WorldGenerator.Instance != null)
            {
                Vector3 delta = WorldGenerator.Instance.ExtractionPosition - boundPlayer.transform.position;
                delta.y = 0f;
                float dist = delta.magnitude;
                float ang = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                if (ang < 0f) ang += 360f;
                string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
                string dir = dirs[Mathf.RoundToInt(ang / 45f) % 8];
                objDistText.text = dist < 4f ? "EVAC HELIPAD: ON PAD" : $"EVAC HELIPAD: {dist:0}m {dir}";
            }

            bool devOn = (DeadDepthsDebugMode.Instance != null && DeadDepthsDebugMode.Instance.IsDebugVisible) ||
                         (boundPlayer != null && boundPlayer.IsGodMode);
            if (devPill != null) devPill.SetActive(devOn);
        }

        private void UpdateBelt()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            int[] counts = { gm.BandageCount, gm.MedkitCount, gm.AdrenalineCount };
            for (int i = 0; i < 3; i++)
            {
                if (beltCountTexts[i] != null) beltCountTexts[i].text = counts[i].ToString();
                if (beltCooldownOverlays[i] != null) beltCooldownOverlays[i].fillAmount = gm.GetBeltCooldownRatio(i);
                if (beltSlotFrames[i] != null)
                {
                    beltSlotFrames[i].color = counts[i] > 0
                        ? new Color(0.18f, 0.22f, 0.28f, 1f)
                        : new Color(0.09f, 0.11f, 0.14f, 0.72f);
                }
            }
        }

        private void UpdateCrateAndExtractionOverlays()
        {
            if (boundPlayer == null || boundPlayer.IsDead)
            {
                if (cratePromptPanel != null) cratePromptPanel.SetActive(false);
                if (evacHoldPanel != null) evacHoldPanel.SetActive(false);
                if (evacCompassArrow != null) evacCompassArrow.gameObject.SetActive(false);
                return;
            }

            // Crate search prompt & progress ring
            SuppliesCrate nearbyCrate = null;
            float bestDist = float.MaxValue;
            Collider[] cols = Physics.OverlapSphere(boundPlayer.transform.position, boundPlayer.PlayerConfig.InteractionScanRadius);
            foreach (var c in cols)
            {
                var crate = c.GetComponentInParent<SuppliesCrate>();
                if (crate != null && !crate.IsOpened)
                {
                    float d = Vector3.Distance(boundPlayer.transform.position, crate.transform.position);
                    float maxD = Mathf.Min(boundPlayer.PlayerConfig.InteractionScanRadius, crate.CrateConfig.InteractionDistance);
                    if (d <= maxD && d < bestDist)
                    {
                        bestDist = d;
                        nearbyCrate = crate;
                    }
                }
            }

            if (cratePromptPanel != null)
            {
                bool showCrate = nearbyCrate != null;
                cratePromptPanel.SetActive(showCrate);
                if (showCrate)
                {
                    float ratio = nearbyCrate.IsInteracting
                        ? Mathf.Clamp01(nearbyCrate.CurrentProgress / Mathf.Max(0.1f, nearbyCrate.CrateConfig.OpenDuration))
                        : 0f;
                    if (crateRingFill != null) crateRingFill.fillAmount = ratio;
                    if (cratePromptLabel != null)
                        cratePromptLabel.text = nearbyCrate.IsInteracting
                            ? $"SEARCHING CRATE... {Mathf.RoundToInt(ratio * 100f)}%"
                            : "[HOLD E] SEARCH SUPPLIES CRATE";
                }
            }

            // Extraction hold ring
            var safeZone = SafeZoneController.Instance;
            if (evacHoldPanel != null)
            {
                bool showEvac = safeZone != null && safeZone.IsPlayerInsideZone && !safeZone.HasExtracted;
                evacHoldPanel.SetActive(showEvac);
                if (showEvac)
                {
                    if (safeZone.HasRequiredIntel)
                    {
                        if (evacRingFill != null) evacRingFill.fillAmount = safeZone.HoldProgress;
                        if (evacHoldLabel != null)
                        {
                            evacHoldLabel.text = $"HOLD TO EXTRACT ({safeZone.HoldRemainingSeconds:F1}s)";
                            evacHoldLabel.color = new Color(0.38f, 0.96f, 0.58f);
                        }
                    }
                    else
                    {
                        var gm = GameManager.Instance;
                        int have = gm != null ? gm.SupplyIntelCollected : 0;
                        int req = gm != null ? gm.RequiredSupplyIntel : 3;
                        if (evacRingFill != null) evacRingFill.fillAmount = 0f;
                        if (evacHoldLabel != null)
                        {
                            evacHoldLabel.text = $"NEED SUPPLY INTEL ({have}/{req})";
                            evacHoldLabel.color = new Color(1f, 0.65f, 0.25f);
                        }
                    }
                }
            }

            // Screen-Edge Evac Compass Arrow when Helipad is offscreen
            if (evacCompassArrow != null && WorldGenerator.Instance != null && Camera.main != null)
            {
                Vector3 evacWorld = WorldGenerator.Instance.ExtractionPosition + Vector3.up * 1.2f;
                Vector3 vp = Camera.main.WorldToViewportPoint(evacWorld);
                bool offscreen = vp.z < 0f || vp.x < 0.06f || vp.x > 0.94f || vp.y < 0.06f || vp.y > 0.94f;
                evacCompassArrow.gameObject.SetActive(offscreen);
                if (offscreen)
                {
                    if (vp.z < 0f) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }
                    Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f).normalized;
                    evacCompassArrow.anchoredPosition = new Vector2(
                        Mathf.Clamp(dir.x * 820f, -820f, 820f),
                        Mathf.Clamp(dir.y * 420f, -330f, 430f));
                }
            }
        }

        private void UpdateMinimap()
        {
            if (!minimapVisible || minimapRaw == null) return;
            var wg = WorldGenerator.Instance;
            if (wg == null) return;

            if (minimapTex == null || lastMapSeed != wg.LastSeed)
            {
                RebuildMinimapTexture(wg);
            }

            RectTransform mapRt = minimapRaw.rectTransform;
            float mw = mapRt.rect.width;
            float mh = mapRt.rect.height;

            if (boundPlayer != null && minimapPlayerDot != null)
            {
                Vector2Int cell = wg.WorldToCell(boundPlayer.transform.position);
                float nx = (cell.x + 0.5f) / Mathf.Max(1, wg.GridWidth);
                float ny = (cell.y + 0.5f) / Mathf.Max(1, wg.GridHeight);
                minimapPlayerDot.anchoredPosition = new Vector2(nx * mw, ny * mh);
            }

            if (minimapEvacDot != null)
            {
                Vector2Int ecell = wg.WorldToCell(wg.ExtractionPosition);
                float ex = (ecell.x + 0.5f) / Mathf.Max(1, wg.GridWidth);
                float ey = (ecell.y + 0.5f) / Mathf.Max(1, wg.GridHeight);
                minimapEvacDot.anchoredPosition = new Vector2(ex * mw, ey * mh);
            }
        }

        private void RebuildMinimapTexture(WorldGenerator wg)
        {
            int w = wg.GridWidth;
            int h = wg.GridHeight;
            if (w <= 0 || h <= 0) return;

            if (minimapTex == null || minimapTex.width != w || minimapTex.height != h)
            {
                if (minimapTex != null)
                {
                    if (Application.isPlaying) Destroy(minimapTex);
                    else DestroyImmediate(minimapTex);
                }
                minimapTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Color c;
                    switch (wg.GetCell(x, y))
                    {
                        case CellType.TreeBoundary: c = new Color(0.06f, 0.14f, 0.08f); break;
                        case CellType.WalkableGround: c = new Color(0.22f, 0.28f, 0.22f); break;
                        case CellType.MudPatch: c = new Color(0.32f, 0.22f, 0.14f); break;
                        case CellType.StructureWall: c = new Color(0.58f, 0.60f, 0.68f); break;
                        case CellType.StructureFloor: c = new Color(0.38f, 0.36f, 0.34f); break;
                        case CellType.StructureDoorway: c = new Color(0.75f, 0.72f, 0.42f); break;
                        case CellType.SuppliesCrate: c = new Color(1.0f, 0.85f, 0.22f); break;
                        case CellType.ExtractionZone: c = new Color(0.18f, 0.88f, 0.45f); break;
                        default: c = new Color(0.16f, 0.20f, 0.18f); break;
                    }
                    minimapTex.SetPixel(x, y, c);
                }
            }
            minimapTex.Apply();
            minimapRaw.texture = minimapTex;
            lastMapSeed = wg.LastSeed;
        }
    }
}
