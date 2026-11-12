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

        private float currentProgress = 0f;
        private bool isVisible = false;
        private Transform targetActor;
        private Camera activeCamera;

        private Texture2D texBg;
        private Texture2D texFill;
        private Texture2D texBorder;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeTextures();
        }

        private void Start()
        {
            activeCamera = Camera.main;
        }

        private void InitializeTextures()
        {
            texBg = CreateSolidTexture(barConfig.BackgroundColor);
            texFill = CreateSolidTexture(barConfig.FillColor);
            texBorder = CreateSolidTexture(barConfig.BorderColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[4] { color, color, color, color };
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
            if (!isVisible || targetActor == null) return;
            if (activeCamera == null) activeCamera = Camera.main;
            if (activeCamera == null) return;

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
            if (texBg != null) Destroy(texBg);
            if (texFill != null) Destroy(texFill);
            if (texBorder != null) Destroy(texBorder);
        }
    }
}
