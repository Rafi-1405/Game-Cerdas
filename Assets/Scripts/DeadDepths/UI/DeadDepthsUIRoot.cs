using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DeadDepths.UI
{
    /// <summary>
    /// Master uGUI + TextMesh Pro Canvas root (1920x1080 ScaleWithScreenSize) and InputSystemUIInputModule EventSystem host.
    /// Ensures passive HUD elements use raycastTarget = false so UI clicks never block combat input.
    /// </summary>
    public class DeadDepthsUIRoot : MonoBehaviour
    {
        public static DeadDepthsUIRoot Instance { get; private set; }
        public static bool IsCanvasActive => Instance != null && Instance.IsInitialized;

        public bool IsInitialized { get; private set; }
        public Canvas MasterCanvas { get; private set; }
        public TMP_FontAsset DefaultFont { get; private set; }

        public RectTransform GameplayLayer { get; private set; }
        public RectTransform WorldOverlayLayer { get; private set; }
        public RectTransform MainMenuLayer { get; private set; }
        public RectTransform CompletionLayer { get; private set; }
        public RectTransform PauseLayer { get; private set; }
        public RectTransform SettingsLayer { get; private set; }

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
            InitializeCanvasSystem();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                DeadDepthsUIArtFactory.DisposeAll();
            }
            IsInitialized = false;
        }

        public void InitializeCanvasSystem()
        {
            if (IsInitialized) return;

            DeadDepthsUIArtFactory.EnsureInitialized();
            DefaultFont = LoadDefaultTmpFont();
            EnsureInputSystemEventSystem();

            GameObject canvasGo = new GameObject("DeadDepths_UICanvas");
            canvasGo.transform.SetParent(transform, false);

            MasterCanvas = canvasGo.AddComponent<Canvas>();
            MasterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MasterCanvas.sortingOrder = 10;
            MasterCanvas.pixelPerfect = false;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            WorldOverlayLayer = CreateFullLayer(canvasGo.transform, "Layer_WorldOverlays");
            GameplayLayer = CreateFullLayer(canvasGo.transform, "Layer_GameplayHUD");
            MainMenuLayer = CreateFullLayer(canvasGo.transform, "Layer_MainMenu");
            CompletionLayer = CreateFullLayer(canvasGo.transform, "Layer_Completion");
            PauseLayer = CreateFullLayer(canvasGo.transform, "Layer_Pause");
            SettingsLayer = CreateFullLayer(canvasGo.transform, "Layer_Settings");

            // Attach modular UI controllers
            EnsureChildComponent<DamagePopupManager>(WorldOverlayLayer.gameObject);
            EnsureChildComponent<ZombieHealthBarUI>(WorldOverlayLayer.gameObject);
            EnsureChildComponent<GameplayHUDCanvas>(GameplayLayer.gameObject);
            EnsureChildComponent<MainMenuUI>(MainMenuLayer.gameObject);
            EnsureChildComponent<PauseMenuUI>(PauseLayer.gameObject);
            EnsureChildComponent<SettingsModalUI>(SettingsLayer.gameObject);

            IsInitialized = true;
        }

        private static T EnsureChildComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static TMP_FontAsset LoadDefaultTmpFont()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null && TMP_Settings.instance != null)
            {
                font = TMP_Settings.defaultFontAsset;
            }
            return font;
        }

        public static void EnsureInputSystemEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            GameObject esGo;
            if (existing == null)
            {
                esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
            }
            else
            {
                esGo = existing.gameObject;
            }

            // Remove legacy StandaloneInputModule when activeInputHandler = 1 (New Input System)
            var legacy = esGo.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                if (Application.isPlaying) Destroy(legacy);
                else DestroyImmediate(legacy);
            }

#if ENABLE_INPUT_SYSTEM
            if (esGo.GetComponent<InputSystemUIInputModule>() == null)
            {
                esGo.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }

        private static RectTransform CreateFullLayer(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        #region Shared UI Builder Helpers (Passive elements default raycastTarget = false)
        public RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Sprite sprite, Color color, bool raycast = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = raycast;
            return rt;
        }

        public Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPos, size, sprite, color, raycast);
            return rt.GetComponent<Image>();
        }

        public TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (DefaultFont != null) tmp.font = DefaultFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        public Button CreateButton(Transform parent, string name, string label, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color normalColor, Color highlightColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = DeadDepthsUIArtFactory.ChamferedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = normalColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = highlightColor;
            cb.pressedColor = highlightColor * 0.85f;
            cb.selectedColor = highlightColor;
            cb.colorMultiplier = 1f;
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                onClick?.Invoke();
            });

            var tmp = CreateText(rt, "Label", label, fontSize, FontStyles.Bold, new Color(0.95f, 0.97f, 1f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tmp.raycastTarget = false;
            return btn;
        }
        #endregion
    }
}
