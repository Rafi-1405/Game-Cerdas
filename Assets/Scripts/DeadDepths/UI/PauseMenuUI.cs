using TMPro;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using UTS.Audio;

namespace DeadDepths.UI
{
    /// <summary>
    /// [ESC] Pause Menu overlay with 70% dim scrim, Resume, Settings, Restart Sector, Main Menu, and Quit.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private DeadDepthsUIRoot uiRoot;
        private GameObject panelRoot;

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
                GameManager.Instance.OnPauseChanged -= SyncVisibility;
                GameManager.Instance.OnPauseChanged += SyncVisibility;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPauseChanged -= SyncVisibility;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseChanged -= SyncVisibility;
                GameManager.Instance.OnPauseChanged += SyncVisibility;
                SyncVisibility(GameManager.Instance.IsPaused);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPauseChanged -= SyncVisibility;
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            var scrim = uiRoot.CreatePanel(transform, "PauseScrim",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                null, new Color(0.02f, 0.03f, 0.05f, 0.72f), true);
            panelRoot = scrim.gameObject;

            var card = uiRoot.CreatePanel(scrim, "PauseCard",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 460f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, true);

            uiRoot.CreateText(card, "Title", "OPERATION PAUSED", 28f, FontStyles.Bold,
                new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(360f, 40f));

            uiRoot.CreateText(card, "Hint", "Press [ESC] to resume", 14f, FontStyles.Normal,
                new Color(0.60f, 0.68f, 0.78f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(360f, 22f));

            Color norm = new Color(0.14f, 0.18f, 0.24f, 1f);
            Color hi = new Color(0.24f, 0.34f, 0.46f, 1f);

            float y = -115f;
            uiRoot.CreateButton(card, "ResumeBtn", "RESUME", 19f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 48f),
                new Color(0.18f, 0.48f, 0.32f, 1f), new Color(0.24f, 0.65f, 0.42f, 1f), () =>
                {
                    GameManager.Instance?.SetPaused(false);
                });
            y -= 62f;

            uiRoot.CreateButton(card, "SettingsBtn", "SETTINGS", 18f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 46f),
                norm, hi, () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    SettingsModalUI.Instance?.Open();
                });
            y -= 60f;

            uiRoot.CreateButton(card, "RestartBtn", "RESTART RUN (SECTOR 1)", 17f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 46f),
                norm, hi, () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    GameManager.Instance?.RetryRun();
                });
            y -= 60f;

            uiRoot.CreateButton(card, "MainMenuBtn", "MAIN MENU", 17f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 46f),
                norm, hi, () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    GameManager.Instance?.EnterMainMenu();
                });
            y -= 60f;

            uiRoot.CreateButton(card, "QuitBtn", "QUIT TO DESKTOP", 17f,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 46f),
                new Color(0.32f, 0.14f, 0.16f, 1f), new Color(0.52f, 0.20f, 0.22f, 1f), QuitApp);
        }

        private void SyncVisibility(bool paused)
        {
            if (panelRoot != null) panelRoot.SetActive(paused);
            if (!paused && SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsOpen)
            {
                SettingsModalUI.Instance.Close();
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
