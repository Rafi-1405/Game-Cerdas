using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DeadDepths.Audio;
using DeadDepths.Player;
using UTS.Audio;

namespace DeadDepths.UI
{
    /// <summary>
    /// Publish-grade Settings modal controlling Audio Volumes, Tap-Dodge Threshold (0.15-0.25s),
    /// Aim Camera Look-Ahead, Corner Minimap, Quality (PC/Mobile), VSync, and Reduced Motion.
    /// </summary>
    public class SettingsModalUI : MonoBehaviour
    {
        public static SettingsModalUI Instance { get; private set; }

        public const string PrefQualityLevel = "DeadDepths_Quality";
        public const string PrefVSync = "DeadDepths_VSync";
        public const string PrefReducedMotion = "DeadDepths_ReducedMotion";
        public const string PrefShowMinimap = "DeadDepths_ShowMinimap";

        public static bool IsReducedMotionEnabled => PlayerPrefs.GetInt(PrefReducedMotion, 0) == 1;
        public static bool IsMinimapEnabled => PlayerPrefs.GetInt(PrefShowMinimap, 1) == 1;

        public bool IsOpen { get; private set; }

        private DeadDepthsUIRoot uiRoot;
        private GameObject modalRoot;

        private Slider masterSlider, sfxSlider, ambientSlider, dodgeSlider;
        private TMP_Text masterValTxt, sfxValTxt, ambientValTxt, dodgeValTxt;
        private Toggle lookAheadToggle, minimapToggle, reducedMotionToggle, vsyncToggle;
        private TMP_Text qualityBtnLabel;
        private int currentQualityIdx = 1; // 0=Mobile, 1=PC

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
            ApplySavedSettings();
            Close();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            // Full-screen dim scrim (blocks clicks behind modal)
            var scrimRt = uiRoot.CreatePanel(transform, "SettingsScrim",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                null, new Color(0.02f, 0.03f, 0.05f, 0.78f), true);
            modalRoot = scrimRt.gameObject;

            var card = uiRoot.CreatePanel(scrimRt, "SettingsCard",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 680f),
                DeadDepthsUIArtFactory.ChamferedCardSprite, Color.white, true);

            uiRoot.CreateText(card, "Title", "SYSTEM & GAMEPLAY SETTINGS", 28f, FontStyles.Bold,
                new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-64f, 40f));

            float y = -86f;
            (masterSlider, masterValTxt) = CreateSliderRow(card, "Master Volume", 0f, 1f, 0.90f, ref y, v =>
            {
                AudioManager.Instance?.SetVolumes(v, sfxSlider.value, ambientSlider.value);
                masterValTxt.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
            (sfxSlider, sfxValTxt) = CreateSliderRow(card, "SFX Volume", 0f, 1f, 0.85f, ref y, v =>
            {
                AudioManager.Instance?.SetVolumes(masterSlider.value, v, ambientSlider.value);
                sfxValTxt.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
            (ambientSlider, ambientValTxt) = CreateSliderRow(card, "Ambient & Tension Volume", 0f, 1f, 0.65f, ref y, v =>
            {
                AudioManager.Instance?.SetVolumes(masterSlider.value, sfxSlider.value, v);
                ambientValTxt.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
            (dodgeSlider, dodgeValTxt) = CreateSliderRow(card, "Tap-to-Dodge Window (Shift)", 0.15f, 0.25f, 0.20f, ref y, v =>
            {
                PlayerPrefs.SetFloat(DeadDepthsPlayerController.PrefTapDodgeThreshold, v);
                PlayerPrefs.Save();
                if (DeadDepthsPlayerController.Instance != null)
                {
                    DeadDepthsPlayerController.Instance.PlayerConfig.ShiftHoldThreshold = v;
                }
                dodgeValTxt.text = $"{v:F2}s";
            });

            y -= 8f;
            lookAheadToggle = CreateToggleRow(card, "Pan Camera While Aiming (RMB Look-Ahead)", true, ref y, on =>
            {
                PlayerPrefs.SetInt(IsometricCameraController.PrefAimLookAhead, on ? 1 : 0);
                PlayerPrefs.Save();
                IsometricCameraController.Instance?.SetAimLookAheadEnabled(on, true);
            });
            minimapToggle = CreateToggleRow(card, "Show Corner Tactical Minimap [M]", true, ref y, on =>
            {
                PlayerPrefs.SetInt(PrefShowMinimap, on ? 1 : 0);
                PlayerPrefs.Save();
                GameplayHUDCanvas.Instance?.SyncMinimapPreference(on);
            });
            reducedMotionToggle = CreateToggleRow(card, "Reduced Motion (Minimize UI & Popup Bounce)", false, ref y, on =>
            {
                PlayerPrefs.SetInt(PrefReducedMotion, on ? 1 : 0);
                PlayerPrefs.Save();
            });
            vsyncToggle = CreateToggleRow(card, "Vertical Sync (VSync)", true, ref y, on =>
            {
                if (Application.isPlaying) QualitySettings.vSyncCount = on ? 1 : 0;
                PlayerPrefs.SetInt(PrefVSync, on ? 1 : 0);
                PlayerPrefs.Save();
            });

            // Quality Preset button row
            y -= 10f;
            uiRoot.CreateText(card, "QualLabel", "Graphics Preset", 18f, FontStyles.Normal,
                new Color(0.84f, 0.88f, 0.94f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, y), new Vector2(260f, 32f));

            var qualBtn = uiRoot.CreateButton(card, "QualityButton", "PC (HIGH)", 17f,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-34f, y), new Vector2(220f, 38f),
                new Color(0.18f, 0.24f, 0.32f, 1f), new Color(0.28f, 0.38f, 0.50f, 1f), CycleQualityPreset);
            qualityBtnLabel = qualBtn.GetComponentInChildren<TMP_Text>();

            uiRoot.CreateButton(card, "CloseBtn", "SAVE & CLOSE", 19f,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(260f, 48f),
                new Color(0.18f, 0.45f, 0.30f, 1f), new Color(0.24f, 0.62f, 0.40f, 1f), () =>
                {
                    AudioManager.Instance?.PlayCue(SfxCue.UI);
                    Close();
                });
        }

        private (Slider, TMP_Text) CreateSliderRow(Transform parent, string title, float min, float max, float defaultVal, ref float y, UnityEngine.Events.UnityAction<float> onChanged)
        {
            uiRoot.CreateText(parent, title + "_Label", title, 18f, FontStyles.Normal,
                new Color(0.84f, 0.88f, 0.94f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, y), new Vector2(320f, 30f));

            var valTxt = uiRoot.CreateText(parent, title + "_Val", "", 18f, FontStyles.Bold,
                new Color(0.95f, 0.85f, 0.40f), TextAlignmentOptions.Right,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-34f, y), new Vector2(85f, 30f));

            GameObject sliderGo = new GameObject(title + "_Slider");
            sliderGo.transform.SetParent(parent, false);
            var rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-130f, y);
            rt.sizeDelta = new Vector2(230f, 24f);

            var bg = uiRoot.CreateImage(rt, "Track", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarTrackSprite, new Color(0.12f, 0.15f, 0.20f, 1f), true);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(rt, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(4f, 4f);
            faRt.offsetMax = new Vector2(-4f, -4f);

            var fillImg = uiRoot.CreateImage(faRt, "Fill", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.30f, 0.72f, 0.92f, 1f), false);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(rt, false);
            var haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(8f, 0f);
            haRt.offsetMax = new Vector2(-8f, 0f);

            var handleImg = uiRoot.CreateImage(haRt, "Handle", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 4f),
                DeadDepthsUIArtFactory.CircleSolidSprite, new Color(0.95f, 0.97f, 1f, 1f), true);

            var slider = sliderGo.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.targetGraphic = handleImg;
            slider.fillRect = fillImg.rectTransform;
            slider.handleRect = handleImg.rectTransform;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;
            slider.onValueChanged.AddListener(onChanged);

            y -= 52f;
            return (slider, valTxt);
        }

        private Toggle CreateToggleRow(Transform parent, string title, bool defaultVal, ref float y, UnityEngine.Events.UnityAction<bool> onChanged)
        {
            uiRoot.CreateText(parent, title + "_Label", title, 18f, FontStyles.Normal,
                new Color(0.84f, 0.88f, 0.94f), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, y), new Vector2(540f, 30f));

            GameObject toggleGo = new GameObject(title + "_Toggle");
            toggleGo.transform.SetParent(parent, false);
            var rt = toggleGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-34f, y);
            rt.sizeDelta = new Vector2(34f, 34f);

            var bg = uiRoot.CreateImage(rt, "Box", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.ChamferedCardSprite, new Color(0.20f, 0.25f, 0.32f, 1f), true);
            var check = uiRoot.CreateImage(rt, "Check", new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                DeadDepthsUIArtFactory.BarFillSprite, new Color(0.28f, 0.88f, 0.50f, 1f), false);

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = defaultVal;
            toggle.onValueChanged.AddListener(val =>
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                onChanged?.Invoke(val);
            });

            y -= 44f;
            return toggle;
        }

        private void CycleQualityPreset()
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            currentQualityIdx = (currentQualityIdx + 1) % count;
            ApplyQualityIndex(currentQualityIdx, true);
            AudioManager.Instance?.PlayCue(SfxCue.UI);
        }

        private void ApplyQualityIndex(int idx, bool save)
        {
            string[] names = QualitySettings.names;
            if (names != null && names.Length > 0)
            {
                currentQualityIdx = Mathf.Clamp(idx, 0, names.Length - 1);
                if (Application.isPlaying)
                {
                    QualitySettings.SetQualityLevel(currentQualityIdx, true);
                    QualitySettings.vSyncCount = PlayerPrefs.GetInt(PrefVSync, 1) == 1 ? 1 : 0;
                }
                if (qualityBtnLabel != null)
                    qualityBtnLabel.text = names[currentQualityIdx].ToUpperInvariant();
            }
            if (save)
            {
                PlayerPrefs.SetInt(PrefQualityLevel, currentQualityIdx);
                PlayerPrefs.Save();
            }
        }

        public void ApplySavedSettings()
        {
            float m = PlayerPrefs.GetFloat(AudioManager.PrefMasterVol, 0.90f);
            float s = PlayerPrefs.GetFloat(AudioManager.PrefSfxVol, 0.85f);
            float a = PlayerPrefs.GetFloat(AudioManager.PrefAmbientVol, 0.65f);
            float dodge = Mathf.Clamp(PlayerPrefs.GetFloat(DeadDepthsPlayerController.PrefTapDodgeThreshold, 0.20f), 0.15f, 0.25f);
            bool look = PlayerPrefs.GetInt(IsometricCameraController.PrefAimLookAhead, 1) == 1;
            bool map = PlayerPrefs.GetInt(PrefShowMinimap, 1) == 1;
            bool red = PlayerPrefs.GetInt(PrefReducedMotion, 0) == 1;
            bool vsync = PlayerPrefs.GetInt(PrefVSync, 1) == 1;
            int qual = PlayerPrefs.GetInt(PrefQualityLevel, Mathf.Min(1, Mathf.Max(0, QualitySettings.names.Length - 1)));

            if (masterSlider != null) masterSlider.SetValueWithoutNotify(m);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s);
            if (ambientSlider != null) ambientSlider.SetValueWithoutNotify(a);
            if (dodgeSlider != null) dodgeSlider.SetValueWithoutNotify(dodge);

            if (masterValTxt != null) masterValTxt.text = $"{Mathf.RoundToInt(m * 100)}%";
            if (sfxValTxt != null) sfxValTxt.text = $"{Mathf.RoundToInt(s * 100)}%";
            if (ambientValTxt != null) ambientValTxt.text = $"{Mathf.RoundToInt(a * 100)}%";
            if (dodgeValTxt != null) dodgeValTxt.text = $"{dodge:F2}s";

            if (lookAheadToggle != null) lookAheadToggle.SetIsOnWithoutNotify(look);
            if (minimapToggle != null) minimapToggle.SetIsOnWithoutNotify(map);
            if (reducedMotionToggle != null) reducedMotionToggle.SetIsOnWithoutNotify(red);
            if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(vsync);

            AudioManager.Instance?.SetVolumes(m, s, a, false);
            ApplyQualityIndex(qual, false);
            if (Application.isPlaying) QualitySettings.vSyncCount = vsync ? 1 : 0;
        }

        public void Open()
        {
            ApplySavedSettings();
            IsOpen = true;
            if (modalRoot != null) modalRoot.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (modalRoot != null) modalRoot.SetActive(false);
        }
    }
}
