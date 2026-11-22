using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeadDepths.Audio;
using DeadDepths.Core;
using UTS.Audio;

namespace DeadDepths.UI
{
    /// <summary>
    /// Displays the Evacuation Scoreboard upon reaching the extraction zone with Continue Run (keeps score).
    /// Renders via publish-grade uGUI + Rank Seal (S/A/B/C) + 5-Metric DDA Breakdown when DeadDepthsUIRoot is active,
    /// while preserving all [NonSerialized] IMGUI Texture2D/GUIStyle reflection contracts for Automated Tests #14 & #20.
    /// </summary>
    public class CompletionScreenUI : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public Color DimBackdropColor = new Color(0.04f, 0.05f, 0.08f, 0.90f);
            public Color PanelColor = new Color(0.09f, 0.11f, 0.16f, 0.96f);
            public Color PanelBorderColor = new Color(0.24f, 0.72f, 0.44f, 0.95f);
            public Color TextColor = new Color(0.92f, 0.96f, 1.0f);
            public Color AccentColor = new Color(0.35f, 0.92f, 0.55f);
            public Color ButtonColor = new Color(0.18f, 0.65f, 0.35f);
            public Color ButtonHoverColor = new Color(0.24f, 0.82f, 0.44f);
            public Color SecondaryButtonColor = new Color(0.28f, 0.32f, 0.40f);
            public Color SecondaryButtonHoverColor = new Color(0.38f, 0.44f, 0.54f);
            public int TitleFontSize = 24;
            public int ButtonFontSize = 16;
            public float ButtonWidth = 210f;
            public float ButtonHeight = 44f;
        }
        #endregion

        [SerializeField] private Config screenConfig = new Config();
        public Config ScreenConfig => screenConfig;

        public static CompletionScreenUI Instance { get; private set; }
        public bool IsVisible => isVisible;

        [NonSerialized] private bool isVisible = false;
        [NonSerialized] private int currentLevelNumber = 1;
        [NonSerialized] private bool continueClicked = false;

        [NonSerialized] private Texture2D texBackdrop;
        [NonSerialized] private Texture2D texPanel;
        [NonSerialized] private Texture2D texBorder;
        [NonSerialized] private Texture2D texButton;
        [NonSerialized] private Texture2D texButtonHover;
        [NonSerialized] private Texture2D texSecButton;
        [NonSerialized] private Texture2D texSecButtonHover;
        [NonSerialized] private GUIStyle titleStyle;
        [NonSerialized] private GUIStyle subtitleStyle;
        [NonSerialized] private GUIStyle rowLabelStyle;
        [NonSerialized] private GUIStyle rowValueStyle;
        [NonSerialized] private GUIStyle hlRowLabelStyle;
        [NonSerialized] private GUIStyle hlRowValueStyle;
        [NonSerialized] private GUIStyle buttonStyle;
        [NonSerialized] private GUIStyle secButtonStyle;

        // uGUI Canvas View References
        [NonSerialized] private GameObject canvasPanelRoot;
        [NonSerialized] private TMP_Text uguiTitle, uguiSubtitle, uguiRankLetter, uguiTotalScore, uguiNewHighBadge, uguiMetricsTable, uguiDdaNext;
        [NonSerialized] private Image uguiRankSealBg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            isVisible = false;
            currentLevelNumber = 1;
            continueClicked = false;
            ResetStyles();
            InitializeTextures();
        }

        private void Start()
        {
            EnsureCanvasPanelBuilt();
            if (canvasPanelRoot != null) canvasPanelRoot.SetActive(isVisible);
        }

        private void OnEnable()
        {
            ResetStyles();
            EnsureTextures();
        }

        private void OnDisable()
        {
            ResetStyles();
        }

        private void InitializeTextures()
        {
            DestroyTextures();
            texBackdrop = CreateSolidTexture(screenConfig.DimBackdropColor);
            texPanel = CreateSolidTexture(screenConfig.PanelColor);
            texBorder = CreateSolidTexture(screenConfig.PanelBorderColor);
            texButton = CreateSolidTexture(screenConfig.ButtonColor);
            texButtonHover = CreateSolidTexture(screenConfig.ButtonHoverColor);
            texSecButton = CreateSolidTexture(screenConfig.SecondaryButtonColor);
            texSecButtonHover = CreateSolidTexture(screenConfig.SecondaryButtonHoverColor);
        }

        private void EnsureTextures()
        {
            if (texBackdrop == null) texBackdrop = CreateSolidTexture(screenConfig.DimBackdropColor);
            if (texPanel == null) texPanel = CreateSolidTexture(screenConfig.PanelColor);
            if (texBorder == null) texBorder = CreateSolidTexture(screenConfig.PanelBorderColor);
            if (texButton == null) texButton = CreateSolidTexture(screenConfig.ButtonColor);
            if (texButtonHover == null) texButtonHover = CreateSolidTexture(screenConfig.ButtonHoverColor);
            if (texSecButton == null) texSecButton = CreateSolidTexture(screenConfig.SecondaryButtonColor);
            if (texSecButtonHover == null) texSecButtonHover = CreateSolidTexture(screenConfig.SecondaryButtonHoverColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels(new Color[] { color, color, color, color });
            tex.Apply();
            return tex;
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref texBackdrop);
            DestroyTexture(ref texPanel);
            DestroyTexture(ref texBorder);
            DestroyTexture(ref texButton);
            DestroyTexture(ref texButtonHover);
            DestroyTexture(ref texSecButton);
            DestroyTexture(ref texSecButtonHover);
        }

        private static void DestroyTexture(ref Texture2D tex)
        {
            if (tex != null)
            {
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
                tex = null;
            }
        }

        private void ResetStyles()
        {
            titleStyle = null;
            subtitleStyle = null;
            rowLabelStyle = null;
            rowValueStyle = null;
            hlRowLabelStyle = null;
            hlRowValueStyle = null;
            buttonStyle = null;
            secButtonStyle = null;
        }

        private void EnsureCanvasPanelBuilt()
        {
            if (canvasPanelRoot != null) return;
            var uiRoot = DeadDepthsUIRoot.Instance;
            if (uiRoot == null || uiRoot.CompletionLayer == null) return;

            var scrim = uiRoot.CreatePanel(uiRoot.CompletionLayer, "CompletionScrim",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                null, new Color(0.02f, 0.03f, 0.05f, 0.84f), true);
            canvasPanelRoot = scrim.gameObject;

            var card = uiRoot.CreatePanel(scrim, "CompletionCard",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, true);

            uguiTitle = uiRoot.CreateText(card, "Title", "SECTOR 1 EXTRACTION COMPLETE", 30f, FontStyles.Bold,
                new Color(0.36f, 0.94f, 0.58f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(34f, -26f), new Vector2(-180f, 38f));

            uguiSubtitle = uiRoot.CreateText(card, "Subtitle", "HELICOPTER EVACUATION CONFIRMED", 15f, FontStyles.Bold,
                new Color(0.68f, 0.76f, 0.84f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(34f, -66f), new Vector2(-180f, 24f));

            // Rank Seal (S / A / B / C)
            uguiRankSealBg = uiRoot.CreateImage(card, "RankSeal",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -24f), new Vector2(94f, 94f),
                DeadDepthsUIArtFactory.RankSealSprite, new Color(0.95f, 0.80f, 0.24f, 1f), false);

            uguiRankLetter = uiRoot.CreateText(uguiRankSealBg.rectTransform, "RankLetter", "S", 46f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Total Score + New High Score Badge
            var scoreBand = uiRoot.CreatePanel(card, "ScoreBand",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(-68f, 64f),
                DeadDepthsUIArtFactory.BarTrackSprite, new Color(0.10f, 0.14f, 0.18f, 0.96f), false);

            uguiTotalScore = uiRoot.CreateText(scoreBand, "TotalScore", "TOTAL RUN SCORE: 0", 24f, FontStyles.Bold,
                new Color(0.96f, 0.98f, 1f), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.7f, 1f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));

            uguiNewHighBadge = uiRoot.CreateText(scoreBand, "NewHigh", "NEW HIGH SCORE!", 16f, FontStyles.Bold,
                new Color(1f, 0.85f, 0.25f), TextAlignmentOptions.Right,
                new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(-20f, 0f));

            // 5-Metric DDA Breakdown Table
            uguiMetricsTable = uiRoot.CreateText(card, "MetricsTable", "", 17f, FontStyles.Normal,
                new Color(0.86f, 0.90f, 0.95f), TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(-68f, 220f));

            uguiDdaNext = uiRoot.CreateText(card, "DdaNext", "", 16f, FontStyles.Bold,
                new Color(0.45f, 0.85f, 0.98f), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(-68f, 28f));

            // Action Buttons
            uiRoot.CreateButton(card, "ContinueRunBtn", "CONTINUE RUN (NEXT SECTOR)", 18f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-148f, 28f), new Vector2(330f, 50f),
                new Color(0.16f, 0.52f, 0.30f, 1f), new Color(0.24f, 0.70f, 0.42f, 1f), () =>
                {
                    if (!continueClicked)
                    {
                        continueClicked = true;
                        AudioManager.Instance?.PlayCue(SfxCue.UI);
                        OnContinueSelected();
                    }
                });

            uiRoot.CreateButton(card, "MainMenuBtn", "MAIN MENU", 18f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(184f, 28f), new Vector2(220f, 50f),
                new Color(0.18f, 0.22f, 0.28f, 1f), new Color(0.28f, 0.34f, 0.44f, 1f), () =>
                {
                    if (!continueClicked)
                    {
                        continueClicked = true;
                        AudioManager.Instance?.PlayCue(SfxCue.UI);
                        Hide();
                        GameManager.Instance?.EnterMainMenu();
                    }
                });

            canvasPanelRoot.SetActive(false);
        }

        private void RefreshCanvasContent()
        {
            EnsureCanvasPanelBuilt();
            if (canvasPanelRoot == null) return;

            var gm = GameManager.Instance;
            int totalScore = gm != null ? gm.TotalScore : 0;
            int levelScore = gm != null ? gm.LevelScore : 0;
            int levelBonus = gm != null ? gm.LastLevelBonus : 0;
            int totalKills = gm != null ? gm.TotalZombiesKilled : 0;
            int levelKills = gm != null ? gm.LevelZombiesKilled : 0;
            int totalCrates = gm != null ? gm.TotalCratesOpened : 0;
            int levelCrates = gm != null ? gm.LevelCratesOpened : 0;
            float clearTime = gm != null ? gm.LastLevelClearTime : 0f;
            float perf = gm != null ? gm.LastEvaluatedPerfScore : 0.5f;
            string nextTier = gm != null ? gm.LastEvaluatedTier.ToString() : "Balanced";

            string rank = perf >= 0.80f ? "S" : (perf >= 0.62f ? "A" : (perf >= 0.42f ? "B" : "C"));
            Color rankCol = rank == "S" ? new Color(1f, 0.82f, 0.22f)
                          : (rank == "A" ? new Color(0.32f, 0.92f, 0.54f)
                          : (rank == "B" ? new Color(0.36f, 0.76f, 0.98f) : new Color(0.75f, 0.78f, 0.82f)));

            if (uguiTitle != null) uguiTitle.text = $"SECTOR {currentLevelNumber} EXTRACTION COMPLETE";
            if (uguiSubtitle != null) uguiSubtitle.text = $"SECTORS CLEARED: {(gm != null ? gm.LevelsCleared : currentLevelNumber)}   |   SECTOR SCORE: +{levelScore} (INCL. +{levelBonus} BONUS)";
            if (uguiRankLetter != null) uguiRankLetter.text = rank;
            if (uguiRankSealBg != null) uguiRankSealBg.color = rankCol;
            if (uguiTotalScore != null) uguiTotalScore.text = $"TOTAL RUN SCORE: {totalScore}";
            if (uguiNewHighBadge != null) uguiNewHighBadge.gameObject.SetActive(gm != null && gm.IsNewHighScore);

            var tracker = PlayerPerformanceTracker.Instance;
            float hpPct = tracker != null && tracker.MaxHealth > 0f ? (tracker.CurrentHealth / tracker.MaxHealth) * 100f : 100f;
            float dmgTaken = tracker != null ? tracker.DamageReceived : 0f;
            float resUsed = tracker != null ? tracker.ResourcesConsumed : 0f;

            if (uguiMetricsTable != null)
            {
                uguiMetricsTable.text =
                    $"<b>5-METRIC DDA PERFORMANCE BREAKDOWN (SCORE: {perf:F2})</b>\n" +
                    $"  • Health Remaining (30%):       {hpPct:0}%\n" +
                    $"  • Zombies Eliminated (20%):     {levelKills} this sector ({totalKills} total)\n" +
                    $"  • Extraction Time (15%):        {clearTime:F1}s\n" +
                    $"  • Damage Taken (20%):           {dmgTaken:0} dmg\n" +
                    $"  • Supplies & Resources (15%):   {levelCrates} crates opened ({totalCrates} total), {resUsed:0.0} heals used";
            }

            if (uguiDdaNext != null)
            {
                uguiDdaNext.text = $"NEXT SECTOR DDA ADAPTATION TIER: {nextTier.ToUpperInvariant()}";
            }
        }

        public void Show(int levelNumber)
        {
            currentLevelNumber = levelNumber;
            continueClicked = false;
            isVisible = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;

            RefreshCanvasContent();
            if (canvasPanelRoot != null) canvasPanelRoot.SetActive(true);
        }

        public void Hide()
        {
            isVisible = false;
            Time.timeScale = 1f;
            if (canvasPanelRoot != null) canvasPanelRoot.SetActive(false);
        }

        private void OnGUI()
        {
            if (DeadDepthsUIRoot.IsCanvasActive) return;
            if (!isVisible) return;

            GUI.depth = -10;
            EnsureTextures();
            InitStyles();

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texBackdrop);

            float panelW = 500f;
            float panelH = 400f;
            float px = (Screen.width - panelW) * 0.5f;
            float py = (Screen.height - panelH) * 0.5f;

            GUI.DrawTexture(new Rect(px - 2, py - 2, panelW + 4, panelH + 4), texBorder);
            GUI.DrawTexture(new Rect(px, py, panelW, panelH), texPanel);

            GUI.Label(new Rect(px + 20f, py + 18f, panelW - 40f, 36f), "EVACUATION SCOREBOARD", titleStyle);
            GUI.Label(new Rect(px + 20f, py + 54f, panelW - 40f, 24f), $"Supplies Collection {currentLevelNumber} Cleared", subtitleStyle);

            var gm = GameManager.Instance;
            int totalScore = gm != null ? gm.TotalScore : 0;
            int levelScore = gm != null ? gm.LevelScore : 0;
            int levelBonus = gm != null ? gm.LastLevelBonus : 0;
            int levelsCleared = gm != null ? gm.LevelsCleared : currentLevelNumber;
            int totalKills = gm != null ? gm.TotalZombiesKilled : 0;
            int levelKills = gm != null ? gm.LevelZombiesKilled : 0;
            int totalCrates = gm != null ? gm.TotalCratesOpened : 0;
            int levelCrates = gm != null ? gm.LevelCratesOpened : 0;
            float clearTime = gm != null ? gm.LastLevelClearTime : 0f;
            string nextTier = gm != null ? gm.LastEvaluatedTier.ToString() : "Balanced";

            float rowY = py + 94f;
            float rowStep = 30f;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "TOTAL RUN SCORE (KEPT)", $"{totalScore}", true); rowY += rowStep;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "Sector Score / Extraction Bonus", $"+{levelScore} (incl. +{levelBonus} bonus)"); rowY += rowStep;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "Sectors Cleared", $"{levelsCleared}"); rowY += rowStep;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "Zombies Eliminated", $"{totalKills} total ({levelKills} this sector)"); rowY += rowStep;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "Supplies Crates Opened", $"{totalCrates} total ({levelCrates} this sector)"); rowY += rowStep;
            DrawScoreRow(px + 36f, rowY, panelW - 72f, "Extraction Time / Next DDA Tier", $"{clearTime:F1}s  |  {nextTier}");

            float btnW = screenConfig.ButtonWidth;
            float btnH = screenConfig.ButtonHeight;
            float btnY = py + panelH - btnH - 26f;

            Rect continueRect = new Rect(px + 32f, btnY, btnW, btnH);
            if (GUI.Button(continueRect, "Continue Run", buttonStyle))
            {
                if (!continueClicked)
                {
                    continueClicked = true;
                    OnContinueSelected();
                }
            }

            Rect newRunRect = new Rect(px + panelW - btnW - 32f, btnY, btnW, btnH);
            if (GUI.Button(newRunRect, "New Run (Reset)", secButtonStyle))
            {
                if (!continueClicked)
                {
                    continueClicked = true;
                    Hide();
                    var gmInstance = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
                    if (gmInstance != null)
                    {
                        gmInstance.RetryRun();
                    }
                }
            }
        }

        private void DrawScoreRow(float x, float y, float w, string label, string value, bool highlight = false)
        {
            if (highlight)
            {
                GUI.Label(new Rect(x, y, w * 0.58f, 26f), label, hlRowLabelStyle);
                GUI.Label(new Rect(x + w * 0.58f, y, w * 0.42f, 26f), value, hlRowValueStyle);
            }
            else
            {
                GUI.Label(new Rect(x, y, w * 0.58f, 26f), label, rowLabelStyle);
                GUI.Label(new Rect(x + w * 0.58f, y, w * 0.42f, 26f), value, rowValueStyle);
            }
        }

        private void InitStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = screenConfig.TitleFontSize,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = screenConfig.AccentColor;
            }

            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                subtitleStyle.normal.textColor = screenConfig.TextColor;
            }

            if (rowLabelStyle == null)
            {
                rowLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 13,
                    fontStyle = FontStyle.Normal
                };
                rowLabelStyle.normal.textColor = new Color(0.78f, 0.84f, 0.92f);
            }

            if (rowValueStyle == null)
            {
                rowValueStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
                rowValueStyle.normal.textColor = screenConfig.TextColor;
            }

            if (hlRowLabelStyle == null)
            {
                hlRowLabelStyle = new GUIStyle(rowLabelStyle);
                hlRowLabelStyle.normal.textColor = screenConfig.AccentColor;
            }

            if (hlRowValueStyle == null)
            {
                hlRowValueStyle = new GUIStyle(rowValueStyle)
                {
                    fontSize = 16
                };
                hlRowValueStyle.normal.textColor = screenConfig.AccentColor;
            }

            if (buttonStyle == null ||
                buttonStyle.normal.background == null ||
                buttonStyle.hover.background == null ||
                buttonStyle.active.background == null ||
                buttonStyle.focused.background == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = screenConfig.ButtonFontSize,
                    fontStyle = FontStyle.Bold
                };
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = Color.white;
                buttonStyle.focused.textColor = Color.white;
                buttonStyle.normal.background = texButton;
                buttonStyle.hover.background = texButtonHover;
                buttonStyle.active.background = texButtonHover;
                buttonStyle.focused.background = texButton;
            }

            if (secButtonStyle == null ||
                secButtonStyle.normal.background == null ||
                secButtonStyle.hover.background == null ||
                secButtonStyle.active.background == null ||
                secButtonStyle.focused.background == null)
            {
                secButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                secButtonStyle.normal.textColor = Color.white;
                secButtonStyle.hover.textColor = Color.white;
                secButtonStyle.active.textColor = Color.white;
                secButtonStyle.focused.textColor = Color.white;
                secButtonStyle.normal.background = texSecButton;
                secButtonStyle.hover.background = texSecButtonHover;
                secButtonStyle.active.background = texSecButtonHover;
                secButtonStyle.focused.background = texSecButton;
            }
        }

        private void OnContinueSelected()
        {
            Hide();
            var gm = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.ProceedToNextLevel();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            isVisible = false;
            continueClicked = false;
            ResetStyles();
            DestroyTextures();
        }
    }
}
