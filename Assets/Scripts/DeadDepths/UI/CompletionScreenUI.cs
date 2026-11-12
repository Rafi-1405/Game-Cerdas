using System;
using UnityEngine;
using DeadDepths.Core;

namespace DeadDepths.UI
{
    /// <summary>
    /// Displays the minimalist level completion screen with only level title and Continue button.
    /// ASD-STE100: This module displays the completion screen when the player extracts.
    /// Follows Section 4.2 of the specification.
    /// </summary>
    public class CompletionScreenUI : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public Color DimBackdropColor = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            public Color TextColor = new Color(0.92f, 0.96f, 1.0f);
            public Color ButtonColor = new Color(0.18f, 0.65f, 0.35f);
            public Color ButtonHoverColor = new Color(0.24f, 0.82f, 0.44f);
            public int TitleFontSize = 28;
            public int ButtonFontSize = 18;
            public float ButtonWidth = 200f;
            public float ButtonHeight = 48f;
        }
        #endregion

        [SerializeField] private Config screenConfig = new Config();
        public Config ScreenConfig => screenConfig;

        public static CompletionScreenUI Instance { get; private set; }

        private bool isVisible = false;
        private int currentLevelNumber = 1;
        private bool continueClicked = false;

        private Texture2D texBackdrop;
        private Texture2D texButton;
        private Texture2D texButtonHover;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeTextures();
        }

        private void InitializeTextures()
        {
            texBackdrop = CreateSolidTexture(screenConfig.DimBackdropColor);
            texButton = CreateSolidTexture(screenConfig.ButtonColor);
            texButtonHover = CreateSolidTexture(screenConfig.ButtonHoverColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new Color[] { color, color, color, color });
            tex.Apply();
            return tex;
        }

        public void Show(int levelNumber)
        {
            currentLevelNumber = levelNumber;
            continueClicked = false;
            isVisible = true;

            // Pause gameplay behind screen
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

            // Draw full screen dim backdrop
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texBackdrop);

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = screenConfig.TitleFontSize,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = screenConfig.TextColor;
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = screenConfig.ButtonFontSize,
                    fontStyle = FontStyle.Bold
                };
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.normal.background = texButton;
                buttonStyle.hover.background = texButtonHover;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            // 1. Completion Text: Supplies Collection X Cleared
            string titleText = $"Supplies Collection {currentLevelNumber} Cleared";
            GUI.Label(new Rect(centerX - 350f, centerY - 80f, 700f, 60f), titleText, titleStyle);

            // 2. Action Button: [ Continue ]
            float btnW = screenConfig.ButtonWidth;
            float btnH = screenConfig.ButtonHeight;
            Rect btnRect = new Rect(centerX - (btnW * 0.5f), centerY + 30f, btnW, btnH);

            if (GUI.Button(btnRect, "Continue", buttonStyle))
            {
                if (!continueClicked)
                {
                    continueClicked = true;
                    OnContinueSelected();
                }
            }
        }

        private void OnContinueSelected()
        {
            Hide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ProceedToNextLevel();
            }
        }

        private void OnDestroy()
        {
            if (texBackdrop != null) Destroy(texBackdrop);
            if (texButton != null) Destroy(texButton);
            if (texButtonHover != null) Destroy(texButtonHover);
        }
    }
}
