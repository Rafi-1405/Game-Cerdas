using TMPro;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using UTS.Audio;

namespace DeadDepths.UI
{
    /// <summary>
    /// Live 3D Main Menu Diorama overlay: left-aligned gradient scrim with title,
    /// High Score / Best Sector records, Deploy button, Field Manual, Settings, and Quit,
    /// leaving the right 60-65% viewport clear for the orbiting 3D forest diorama.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }
        public bool IsVisible => menuRoot != null && menuRoot.activeSelf;

        private DeadDepthsUIRoot uiRoot;
        private GameObject menuRoot;
        private GameObject manualPanel;
        private TMP_Text recordText;

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
            BuildUI();
            SyncVisibility(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMainMenuChanged -= SyncVisibility;
                GameManager.Instance.OnMainMenuChanged += SyncVisibility;
                SyncVisibility(GameManager.Instance.IsInMainMenu);
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMainMenuChanged -= SyncVisibility;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMainMenuChanged -= SyncVisibility;
                GameManager.Instance.OnMainMenuChanged += SyncVisibility;
                SyncVisibility(GameManager.Instance.IsInMainMenu);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMainMenuChanged -= SyncVisibility;
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            GameObject root = new GameObject("MainMenuContainer");
            root.transform.SetParent(transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            menuRoot = root;

            // Left 44% gradient scrim framing the live 3D forest diorama on the right 60-65%
            uiRoot.CreateImage(rootRt, "LeftScrim",
                new Vector2(0f, 0f), new Vector2(0.46f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.HorizontalScrimSprite, Color.white, false);

            // Title block
            uiRoot.CreateText(rootRt, "Tagline", "SECTOR QUARANTINE // TACTICAL EXTRACTION", 15f, FontStyles.Bold,
                new Color(0.36f, 0.88f, 0.58f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -86f), new Vector2(540f, 24f));

            uiRoot.CreateText(rootRt, "MainTitle", "DEAD DEPTHS", 62f, FontStyles.Bold,
                new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -118f), new Vector2(560f, 76f));

            uiRoot.CreateText(rootRt, "SubDesc",
                "Infiltrate procedural infected sectors, recover 3 Supply Intel crates, and hold the Extraction Helipad against adaptive Walker, Spitter, and Charger hordes.",
                17f, FontStyles.Normal, new Color(0.70f, 0.76f, 0.84f), TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -204f), new Vector2(500f, 76f));

            // High Score / Best Sector Card
            var recCard = uiRoot.CreatePanel(rootRt, "RecordCard",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -296f), new Vector2(420f, 74f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, false);

            recordText = uiRoot.CreateText(recCard, "RecordText", "HIGH SCORE: 0   |   BEST SECTOR: 1", 18f, FontStyles.Bold,
                new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -12f));

            // Navigation Buttons
            Color norm = new Color(0.12f, 0.16f, 0.22f, 0.96f);
            Color hi = new Color(0.22f, 0.30f, 0.42f, 1f);

            float y = -410f;
            uiRoot.CreateButton(rootRt, "DeployBtn", "DEPLOY / PLAY", 22f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, y), new Vector2(360f, 60f),
                new Color(0.16f, 0.50f, 0.30f, 1f), new Color(0.24f, 0.68f, 0.42f, 1f), () =>
                {
                    SyncVisibility(false);
                    if (SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsOpen)
                    {
                        SettingsModalUI.Instance.Close();
                    }
                    GameManager.Instance?.StartPlayFromMainMenu();
                });
            y -= 78f;

            uiRoot.CreateButton(rootRt, "ManualBtn", "CONTROLS & FIELD MANUAL", 17f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, y), new Vector2(360f, 48f),
                norm, hi, ToggleFieldManual);
            y -= 62f;

            uiRoot.CreateButton(rootRt, "SettingsBtn", "SETTINGS", 17f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, y), new Vector2(360f, 48f),
                norm, hi, () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    SettingsModalUI.Instance?.Open();
                });
            y -= 62f;

            uiRoot.CreateButton(rootRt, "QuitBtn", "QUIT", 17f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, y), new Vector2(360f, 48f),
                new Color(0.28f, 0.12f, 0.14f, 0.96f), new Color(0.46f, 0.18f, 0.22f, 1f), QuitApp);

            uiRoot.CreateText(rootRt, "VersionFooter", "UNITY 6 URP  |  PROJECT ZOMBOID COMBAT STANCE  |  5-METRIC DDA", 13f, FontStyles.Normal,
                new Color(0.45f, 0.52f, 0.60f), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(72f, 34f), new Vector2(560f, 24f));

            // Collapsible Field Manual overlay on right side
            var manCard = uiRoot.CreatePanel(rootRt, "FieldManualCard",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-72f, 0f), new Vector2(560f, 520f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, true);
            manualPanel = manCard.gameObject;

            uiRoot.CreateText(manCard, "ManTitle", "FIELD MANUAL // TACTICAL CONTROLS", 22f, FontStyles.Bold,
                new Color(0.36f, 0.88f, 0.58f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-48f, 32f));

            string controlsBody =
                "<b>[WASD / Arrows]</b>  Isometric movement (avoid dark mud pools)\n" +
                "<b>[Hold RMB]</b>  Combat Aim Stance (free cursor aim, backpedal, look-ahead)\n" +
                "<b>[Tap Shift]</b>  Quick Dodge burst with i-frames (cancels crates & crouch)\n" +
                "<b>[Hold Shift]</b>  Tactical Sprint (drains Stamina)\n" +
                "<b>[LMB / Space / F]</b>  3-Hit Melee Combo & breakable barricades\n" +
                "<b>[C]</b>  Toggle Crouch Stealth (reduces zombie vision range by 35%)\n" +
                "<b>[T] / [L]</b>  Toggle Dual-Beam Tactical Flashlight\n" +
                "<b>[Hold E]</b>  Search Supplies Crate (+1 Supply Intel & Medical Loot)\n" +
                "<b>[1] [2] [3]</b>  Quick-Use Belt: Bandage / Medkit / Adrenaline\n" +
                "<b>[M] / [ESC]</b>  Toggle Corner Minimap / Pause Menu\n" +
                "<b>[F1] / [G]</b>  AI & PCG Debug Inspector / God Mode";

            uiRoot.CreateText(manCard, "ManBody", controlsBody, 16f, FontStyles.Normal,
                new Color(0.86f, 0.90f, 0.96f), TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(-48f, 370f));

            uiRoot.CreateButton(manCard, "CloseManBtn", "CLOSE MANUAL", 16f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(200f, 42f),
                norm, hi, () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    manualPanel.SetActive(false);
                });

            manualPanel.SetActive(false);
        }

        private void ToggleFieldManual()
        {
            AudioManager.Instance?.PlayCue(SfxCue.UI);
            if (manualPanel != null) manualPanel.SetActive(!manualPanel.activeSelf);
        }

        private void SyncVisibility(bool inMainMenu)
        {
            if (menuRoot != null) menuRoot.SetActive(inMainMenu);
            if (manualPanel != null && !inMainMenu) manualPanel.SetActive(false);

            if (inMainMenu && recordText != null && GameManager.Instance != null)
            {
                recordText.text = $"HIGH SCORE: {GameManager.Instance.HighScore}   |   BEST SECTOR: {GameManager.Instance.BestSector}";
            }
        }

        private static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
