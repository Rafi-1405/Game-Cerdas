using System;
using UnityEngine;
using DeadDepths.Core;
using DeadDepths.Interactive;
using DeadDepths.PCG;
using DeadDepths.Player;

namespace DeadDepths.UI
{
    /// <summary>
    /// Minimalist in-game HUD showing health, stamina, score, God Mode status, interaction prompts, and Death Retry button.
    /// ASD-STE100: This HUD shows player health, stamina, score, and interaction prompts.
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

        [NonSerialized] private DeadDepthsPlayerController player;
        [NonSerialized] private Texture2D texTrack;
        [NonSerialized] private Texture2D texHpFill;
        [NonSerialized] private Texture2D texStamFill;
        [NonSerialized] private Texture2D texBorder;
        [NonSerialized] private Texture2D texHurtVignette;
        [NonSerialized] private Texture2D texRetryBtn;
        [NonSerialized] private Texture2D texRetryBtnHover;
        [NonSerialized] private Texture2D texGodBadge;

        [NonSerialized] private GUIStyle labelStyle;
        [NonSerialized] private GUIStyle centeredLabelStyle;
        [NonSerialized] private GUIStyle rightLabelStyle;
        [NonSerialized] private GUIStyle promptStyle;
        [NonSerialized] private GUIStyle deathStyle;
        [NonSerialized] private GUIStyle retryButtonStyle;
        [NonSerialized] private GUIStyle godBadgeStyle;
        [NonSerialized] private float hurtFlashAlpha = 0f;
        [NonSerialized] private float lastRecordedHealth = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            player = null;
            hurtFlashAlpha = 0f;
            lastRecordedHealth = -1f;
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
            if (player != null)
            {
                player.OnHealthChanged -= OnPlayerHealthChanged;
                player = null;
            }
            ResetStyles();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void InitializeTextures()
        {
            DestroyTextures();
            texTrack = CreateSolidTexture(hudConfig.TrackColor);
            texHpFill = CreateSolidTexture(hudConfig.HealthFillColor);
            texStamFill = CreateSolidTexture(hudConfig.StaminaFillColor);
            texBorder = CreateSolidTexture(hudConfig.BorderColor);
            texHurtVignette = CreateSolidTexture(new Color(0.8f, 0.1f, 0.1f, 0.35f));
            texRetryBtn = CreateSolidTexture(new Color(0.75f, 0.18f, 0.18f, 0.95f));
            texRetryBtnHover = CreateSolidTexture(new Color(0.92f, 0.26f, 0.26f, 1f));
            texGodBadge = CreateSolidTexture(new Color(0.95f, 0.72f, 0.10f, 0.90f));
        }

        private void EnsureTextures()
        {
            if (texTrack == null) texTrack = CreateSolidTexture(hudConfig.TrackColor);
            if (texHpFill == null) texHpFill = CreateSolidTexture(hudConfig.HealthFillColor);
            if (texStamFill == null) texStamFill = CreateSolidTexture(hudConfig.StaminaFillColor);
            if (texBorder == null) texBorder = CreateSolidTexture(hudConfig.BorderColor);
            if (texHurtVignette == null) texHurtVignette = CreateSolidTexture(new Color(0.8f, 0.1f, 0.1f, 0.35f));
            if (texRetryBtn == null) texRetryBtn = CreateSolidTexture(new Color(0.75f, 0.18f, 0.18f, 0.95f));
            if (texRetryBtnHover == null) texRetryBtnHover = CreateSolidTexture(new Color(0.92f, 0.26f, 0.26f, 1f));
            if (texGodBadge == null) texGodBadge = CreateSolidTexture(new Color(0.95f, 0.72f, 0.10f, 0.90f));
        }

        private Texture2D CreateSolidTexture(Color c)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixels(new Color[] { c, c, c, c });
            t.Apply();
            return t;
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref texTrack);
            DestroyTexture(ref texHpFill);
            DestroyTexture(ref texStamFill);
            DestroyTexture(ref texBorder);
            DestroyTexture(ref texHurtVignette);
            DestroyTexture(ref texRetryBtn);
            DestroyTexture(ref texRetryBtnHover);
            DestroyTexture(ref texGodBadge);
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
            labelStyle = null;
            centeredLabelStyle = null;
            rightLabelStyle = null;
            promptStyle = null;
            deathStyle = null;
            retryButtonStyle = null;
            godBadgeStyle = null;
        }

        private void FindPlayer()
        {
            var found = DeadDepthsPlayerController.Instance;
            if (found == null)
            {
                var pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) found = pObj.GetComponent<DeadDepthsPlayerController>();
            }

            if (found != player)
            {
                if (player != null)
                {
                    player.OnHealthChanged -= OnPlayerHealthChanged;
                }
                player = found;
                if (player != null)
                {
                    lastRecordedHealth = player.CurrentHealth;
                    player.OnHealthChanged += OnPlayerHealthChanged;
                }
            }
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            if (lastRecordedHealth >= 0f && current < lastRecordedHealth)
            {
                hurtFlashAlpha = 0.6f;
            }
            lastRecordedHealth = current;
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
            if (DeadDepthsUIRoot.IsCanvasActive) return;
            if (player == null) return;
            if (CompletionScreenUI.Instance != null && CompletionScreenUI.Instance.IsVisible) return;

            GUI.depth = player.IsDead ? -5 : 0;
            EnsureTextures();
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

            string hpLabel = player.IsGodMode
                ? $"HP: {Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.MaxHealth)} [INVULNERABLE]"
                : $"HP: {Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.MaxHealth)}";
            DrawBar(baseX, baseY, w, h, player.CurrentHealth / Mathf.Max(1f, player.MaxHealth), texHpFill, hpLabel);

            float stamY = baseY + h + hudConfig.BarSpacing;
            DrawBar(baseX, stamY, w, h, player.CurrentStamina / Mathf.Max(1f, player.MaxStamina), texStamFill, $"STAMINA: {Mathf.CeilToInt(player.CurrentStamina)}%");

            // 3. Top-Right Level, Score & Evacuation Distance Indicator
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            int score = GameManager.Instance != null ? GameManager.Instance.TotalScore : 0;
            string evacHint = GetEvacCompassHint(player.transform.position);
            string levelText = $"SCORE: {score}  |  SECTOR {level}{evacHint}  |  [F1] DEBUG  [G] GOD";
            float topRightW = Mathf.Min(500f, Mathf.Max(100f, Screen.width - 20f));
            GUI.Label(new Rect(Mathf.Max(10f, Screen.width - topRightW - 20f), 16f, topRightW, 26f), levelText, rightLabelStyle);

            // 4. God Mode Active Banner (Top-Center, fits cleanly between Debug Panels at 1024px)
            if (player.IsGodMode)
            {
                Rect godRect = new Rect((Screen.width - 240f) * 0.5f, 14f, 240f, 26f);
                GUI.DrawTexture(godRect, texGodBadge);
                GUI.Label(godRect, "GOD MODE ON (NOCLIP)", godBadgeStyle);
            }

            // 5. Center Interaction Prompts or Death Retry Overlay
            if (!player.IsDead)
            {
                Collider[] nearby = Physics.OverlapSphere(player.transform.position, player.PlayerConfig.InteractionScanRadius);
                foreach (var col in nearby)
                {
                    var crate = col.GetComponentInParent<SuppliesCrate>();
                    if (crate != null && !crate.IsOpened)
                    {
                        float crateDist = Vector3.Distance(player.transform.position, crate.transform.position);
                        if (crateDist <= Mathf.Min(player.PlayerConfig.InteractionScanRadius, crate.CrateConfig.InteractionDistance))
                        {
                            GUI.Label(new Rect((Screen.width * 0.5f) - 160f, Screen.height * 0.65f, 320f, 30f), "[HOLD E] OPEN SUPPLIES CRATE", promptStyle);
                            break;
                        }
                    }

                    var heli = col.GetComponentInParent<ExtractionHelicopter>();
                    if (heli != null && !heli.HasExtracted)
                    {
                        GUI.Label(new Rect((Screen.width * 0.5f) - 160f, Screen.height * 0.65f, 320f, 30f), "ENTER HELICOPTER TO EXTRACT", promptStyle);
                        break;
                    }
                }
            }
            else
            {
                // Death Overlay with clickable "Retry" button
                float panelW = 360f;
                float panelH = 210f;
                float px = (Screen.width - panelW) * 0.5f;
                float py = (Screen.height - panelH) * 0.5f;

                GUI.DrawTexture(new Rect(px - 2, py - 2, panelW + 4, panelH + 4), texBorder);
                GUI.DrawTexture(new Rect(px, py, panelW, panelH), texTrack);

                GUI.Label(new Rect(px, py + 20f, panelW, 44f), "YOU DIED", deathStyle);
                GUI.Label(new Rect(px, py + 70f, panelW, 24f), $"FINAL SCORE: {score}   |   SECTOR: {level}", promptStyle);

                Rect retryRect = new Rect(px + (panelW - 180f) * 0.5f, py + 112f, 180f, 46f);
                if (GUI.Button(retryRect, "Retry", retryButtonStyle))
                {
                    var gm = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
                    if (gm != null)
                    {
                        gm.RetryRun();
                    }
                }

                GUI.Label(new Rect(px, py + 168f, panelW, 24f), "or press [R] to Retry", centeredLabelStyle);
            }
        }

        private string GetEvacCompassHint(Vector3 playerPos)
        {
            var wg = WorldGenerator.Instance;
            if (wg == null) return string.Empty;
            Vector3 delta = wg.ExtractionPosition - playerPos;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 1f) return "  |  EVAC: HERE";

            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int idx = Mathf.RoundToInt(angle / 45f) % 8;
            return $"  |  EVAC: {dist:0}m {dirs[idx]}";
        }

        private void DrawBar(float x, float y, float width, float height, float ratio, Texture2D fillTex, string label)
        {
            GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), texBorder);
            GUI.DrawTexture(new Rect(x, y, width, height), texTrack);
            float fillW = width * Mathf.Clamp01(ratio);
            if (fillW > 0.5f)
            {
                GUI.DrawTexture(new Rect(x, y, fillW, height), fillTex);
            }
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

            if (centeredLabelStyle == null)
            {
                centeredLabelStyle = new GUIStyle(labelStyle)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (rightLabelStyle == null)
            {
                rightLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                rightLabelStyle.normal.textColor = hudConfig.TextColor;
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
                deathStyle.normal.textColor = new Color(0.95f, 0.18f, 0.18f);
            }

            if (retryButtonStyle == null ||
                retryButtonStyle.normal.background == null ||
                retryButtonStyle.hover.background == null ||
                retryButtonStyle.active.background == null ||
                retryButtonStyle.focused.background == null)
            {
                retryButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
                retryButtonStyle.normal.textColor = Color.white;
                retryButtonStyle.hover.textColor = Color.white;
                retryButtonStyle.active.textColor = Color.white;
                retryButtonStyle.focused.textColor = Color.white;
                retryButtonStyle.normal.background = texRetryBtn;
                retryButtonStyle.hover.background = texRetryBtnHover;
                retryButtonStyle.active.background = texRetryBtnHover;
                retryButtonStyle.focused.background = texRetryBtn;
            }

            if (godBadgeStyle == null)
            {
                godBadgeStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                godBadgeStyle.normal.textColor = Color.black;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (player != null)
            {
                player.OnHealthChanged -= OnPlayerHealthChanged;
                player = null;
            }
            ResetStyles();
            DestroyTextures();
        }
    }
}
