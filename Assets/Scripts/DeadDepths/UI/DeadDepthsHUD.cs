using System;
using UnityEngine;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.Player;

namespace DeadDepths.UI
{
    /// <summary>
    /// Minimalist in-game HUD showing health, stamina, interaction prompts, and combat feedback.
    /// ASD-STE100: This HUD shows player health, stamina, and interaction prompts.
    /// Follows Section 10 of the specification.
    /// </summary>
    public class DeadDepthsHUD : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Header("Bar Dimensions (Bottom-Left)")]
            public Vector2 HudOffset = new Vector2(25f, 25f);
            public float BarWidth = 220f;
            public float BarHeight = 18f;
            public float BarSpacing = 6f;

            [Header("Colors")]
            public Color HealthFillColor = new Color(0.85f, 0.22f, 0.22f);
            public Color StaminaFillColor = new Color(0.24f, 0.65f, 0.95f);
            public Color TrackColor = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            public Color BorderColor = new Color(0.22f, 0.25f, 0.32f, 0.95f);
            public Color TextColor = new Color(0.92f, 0.95f, 1.0f);
            public Color PromptColor = new Color(1.0f, 0.88f, 0.35f);
        }
        #endregion

        [SerializeField] private Config hudConfig = new Config();
        public Config HudConfig => hudConfig;

        public static DeadDepthsHUD Instance { get; private set; }

        private DeadDepthsPlayerController player;
        private Texture2D texTrack;
        private Texture2D texHpFill;
        private Texture2D texStamFill;
        private Texture2D texBorder;
        private Texture2D texHurtVignette;

        private GUIStyle labelStyle;
        private GUIStyle promptStyle;
        private GUIStyle deathStyle;
        private float hurtFlashAlpha = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeTextures();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void InitializeTextures()
        {
            texTrack = CreateSolidTexture(hudConfig.TrackColor);
            texHpFill = CreateSolidTexture(hudConfig.HealthFillColor);
            texStamFill = CreateSolidTexture(hudConfig.StaminaFillColor);
            texBorder = CreateSolidTexture(hudConfig.BorderColor);
            texHurtVignette = CreateSolidTexture(new Color(0.8f, 0.1f, 0.1f, 0.35f));
        }

        private Texture2D CreateSolidTexture(Color c)
        {
            Texture2D t = new Texture2D(2, 2);
            t.SetPixels(new Color[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private void FindPlayer()
        {
            player = DeadDepthsPlayerController.Instance;
            if (player == null)
            {
                var pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) player = pObj.GetComponent<DeadDepthsPlayerController>();
            }

            if (player != null)
            {
                player.OnHealthChanged += OnPlayerHealthChanged;
            }
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            hurtFlashAlpha = 0.6f;
        }

        private void Update()
        {
            if (player == null) FindPlayer();

            if (hurtFlashAlpha > 0f)
            {
                hurtFlashAlpha = Mathf.MoveTowards(hurtFlashAlpha, 0f, Time.deltaTime * 2.5f);
            }
        }

        private void OnGUI()
        {
            if (player == null) return;

            InitStyles();

            // 1. Hurt Flash Overlay
            if (hurtFlashAlpha > 0.01f)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, hurtFlashAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texHurtVignette);
                GUI.color = oldColor;
            }

            // 2. Bottom-Left Status Bars
            float baseX = hudConfig.HudOffset.x;
            float baseY = Screen.height - hudConfig.HudOffset.y - (hudConfig.BarHeight * 2f + hudConfig.BarSpacing);
            float w = hudConfig.BarWidth;
            float h = hudConfig.BarHeight;

            // Health Bar
            DrawBar(baseX, baseY, w, h, player.CurrentHealth / Mathf.Max(1f, player.MaxHealth), texHpFill, $"HP: {Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.MaxHealth)}");

            // Stamina Bar
            float stamY = baseY + h + hudConfig.BarSpacing;
            DrawBar(baseX, stamY, w, h, player.CurrentStamina / Mathf.Max(1f, player.MaxStamina), texStamFill, $"STAMINA: {Mathf.CeilToInt(player.CurrentStamina)}%");

            // 3. Top-Right Level Indicator
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            string levelText = $"LEVEL {level}  |  DEAD DEPTHS";
            GUI.Label(new Rect(Screen.width - 260f, 20f, 240f, 30f), levelText, labelStyle);

            // 4. Center Interaction Prompts
            if (!player.IsDead)
            {
                // Check nearby crates
                Collider[] nearby = Physics.OverlapSphere(player.transform.position, player.PlayerConfig.InteractionScanRadius);
                foreach (var col in nearby)
                {
                    var crate = col.GetComponentInParent<SuppliesCrate>();
                    if (crate != null && !crate.IsOpened)
                    {
                        GUI.Label(new Rect((Screen.width * 0.5f) - 150f, Screen.height * 0.65f, 300f, 30f), "[HOLD E] OPEN SUPPLIES CRATE", promptStyle);
                        break;
                    }

                    var heli = col.GetComponentInParent<ExtractionHelicopter>();
                    if (heli != null && !heli.HasExtracted)
                    {
                        GUI.Label(new Rect((Screen.width * 0.5f) - 150f, Screen.height * 0.65f, 300f, 30f), "ENTER HELICOPTER TO EXTRACT", promptStyle);
                        break;
                    }
                }
            }
            else
            {
                // Death message
                GUI.Label(new Rect((Screen.width * 0.5f) - 200f, (Screen.height * 0.5f) - 40f, 400f, 50f), "YOU DIED", deathStyle);
                GUI.Label(new Rect((Screen.width * 0.5f) - 200f, (Screen.height * 0.5f) + 15f, 400f, 30f), "PRESS [R] TO RESTART LEVEL", promptStyle);
            }
        }

        private void DrawBar(float x, float y, float width, float height, float ratio, Texture2D fillTex, string label)
        {
            // Border
            GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), texBorder);
            // Track
            GUI.DrawTexture(new Rect(x, y, width, height), texTrack);
            // Fill
            float fillW = width * Mathf.Clamp01(ratio);
            if (fillW > 0.5f)
            {
                GUI.DrawTexture(new Rect(x, y, fillW, height), fillTex);
            }
            // Text Label
            GUI.Label(new Rect(x + 8, y - 1, width - 16, height), label, labelStyle);
        }

        private void InitStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = hudConfig.TextColor;
            }

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                promptStyle.normal.textColor = hudConfig.PromptColor;
            }

            if (deathStyle == null)
            {
                deathStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 32,
                    fontStyle = FontStyle.Bold
                };
                deathStyle.normal.textColor = new Color(0.9f, 0.15f, 0.15f);
            }
        }

        private void OnDestroy()
        {
            if (texTrack != null) Destroy(texTrack);
            if (texHpFill != null) Destroy(texHpFill);
            if (texStamFill != null) Destroy(texStamFill);
            if (texBorder != null) Destroy(texBorder);
            if (texHurtVignette != null) Destroy(texHurtVignette);
        }
    }
}
