using System;
using UnityEngine;
using DeadDepths.Core;

namespace DeadDepths.UI
{
    /// <summary>
    /// Displays the Evacuation Scoreboard upon reaching the extraction zone with Continue Run (keeps score).
    /// ASD-STE100: This module displays the completion scoreboard when the player extracts.
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

        public void Show(int levelNumber)
        {
            currentLevelNumber = levelNumber;
            continueClicked = false;
            isVisible = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;
        }

        public void Hide()
        {
            isVisible = false;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
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

            // Header
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

            // Action Buttons
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
