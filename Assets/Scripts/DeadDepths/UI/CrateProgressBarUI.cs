using System;
using UnityEngine;

namespace DeadDepths.UI
{
    /// <summary>
    /// Displays a small progress bar above the player's head while opening supplies crates.
    /// ASD-STE100: This UI element shows crate opening progress with a white bar.
    /// Follows Section 3.2 of the specification.
    /// </summary>
    public class CrateProgressBarUI : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public Vector3 OverheadOffset = new Vector3(0f, 2.2f, 0f);
            public float Width = 140f;
            public float Height = 12f;
            public Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.65f);
            public Color FillColor = Color.white;
            public Color BorderColor = new Color(0.25f, 0.25f, 0.35f, 0.85f);
        }
        #endregion

        [SerializeField] private Config barConfig = new Config();
        public Config BarConfig => barConfig;

        public static CrateProgressBarUI Instance { get; private set; }

        [NonSerialized] private float currentProgress = 0f;
        [NonSerialized] private bool isVisible = false;
        [NonSerialized] private Transform targetActor;
        [NonSerialized] private Camera activeCamera;

        [NonSerialized] private Texture2D texBg;
        [NonSerialized] private Texture2D texFill;
        [NonSerialized] private Texture2D texBorder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            currentProgress = 0f;
            isVisible = false;
            targetActor = null;
            activeCamera = null;
            InitializeTextures();
        }

        private void OnEnable()
        {
            EnsureTextures();
        }

        private void Start()
        {
            activeCamera = Camera.main;
        }

        private void InitializeTextures()
        {
            DestroyTextures();
            texBg = CreateSolidTexture(barConfig.BackgroundColor);
            texFill = CreateSolidTexture(barConfig.FillColor);
            texBorder = CreateSolidTexture(barConfig.BorderColor);
        }

        private void EnsureTextures()
        {
            if (texBg == null) texBg = CreateSolidTexture(barConfig.BackgroundColor);
            if (texFill == null) texFill = CreateSolidTexture(barConfig.FillColor);
            if (texBorder == null) texBorder = CreateSolidTexture(barConfig.BorderColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[4] { color, color, color, color };
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref texBg);
            DestroyTexture(ref texFill);
            DestroyTexture(ref texBorder);
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

        public void Show(Transform actor)
        {
            targetActor = actor;
            currentProgress = 0f;
            isVisible = true;
        }

        public void UpdateProgress(float progressRatio)
        {
            currentProgress = Mathf.Clamp01(progressRatio);
        }

        public void Hide()
        {
            isVisible = false;
            currentProgress = 0f;
            targetActor = null;
        }

        private void OnGUI()
        {
            if (DeadDepthsUIRoot.IsCanvasActive) return;
            if (!isVisible || targetActor == null) return;
            if (activeCamera == null) activeCamera = Camera.main;
            if (activeCamera == null) return;

            EnsureTextures();

            Vector3 worldPos = targetActor.position + barConfig.OverheadOffset;
            Vector3 screenPos = activeCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) return;

            float w = barConfig.Width;
            float h = barConfig.Height;
            float x = screenPos.x - (w * 0.5f);
            float y = Screen.height - screenPos.y - (h * 0.5f);

            // Draw border
            GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), texBorder);
            // Draw background
            GUI.DrawTexture(new Rect(x, y, w, h), texBg);
            // Draw white fill
            float fillW = w * currentProgress;
            if (fillW > 0.5f)
            {
                GUI.DrawTexture(new Rect(x, y, fillW, h), texFill);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            isVisible = false;
            currentProgress = 0f;
            targetActor = null;
            DestroyTextures();
        }
    }
}
