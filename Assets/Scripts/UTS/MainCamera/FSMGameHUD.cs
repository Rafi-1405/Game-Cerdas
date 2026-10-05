using UnityEngine;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FSMGameHUD : MonoBehaviour
    {
        [Header("References (Auto-Found if Empty)")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private SimplePlayerController playerController;
        [SerializeField] private EnemyFSM enemyFSM;
        [SerializeField] private EnemyHealth enemyHealth;

        [Header("Player HUD Settings (Bottom-Left)")]
        [SerializeField] private Vector2 playerHudOffset = new Vector2(25f, 25f);
        [SerializeField] private float playerBarWidth = 240f;
        [SerializeField] private float playerBarHeight = 24f;
        [SerializeField] private float playerBarSpacing = 8f;

        [Header("Enemy Overhead Bar Settings")]
        [SerializeField] private bool showEnemyOverhead = true;
        [SerializeField] private float enemyBarWidth = 200f;
        [SerializeField] private float enemyBarHeight = 24f;
        [SerializeField] private float enemyHeightOffset = 2.4f;

        // Tekstur prosedural untuk styling Soul Knight
        private Texture2D texBorder;
        private Texture2D texTrackDark;
        private Texture2D texHpFill;
        private Texture2D texStaminaFill;
        private Texture2D texEnemyHpFill;
        private Texture2D texEnemyDeadFill;

        private Texture2D texBadgePatrol;
        private Texture2D texBadgeInvestigate;
        private Texture2D texBadgeChase;
        private Texture2D texBadgeAttack;
        private Texture2D texBadgeFlee;
        private Texture2D texBadgeDead;

        private GUIStyle textStyleCenter;
        private GUIStyle textStyleShadow;
        private GUIStyle badgeTextStyle;
        private GUIStyle headerTextStyle;

        private void Awake()
        {
            FindReferences();
            InitTextures();
        }

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<SimplePlayerController>();
            }

            if (enemyFSM == null)
            {
                enemyFSM = FindAnyObjectByType<EnemyFSM>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = FindAnyObjectByType<EnemyHealth>();
            }
        }

        private void InitTextures()
        {
            if (texBorder != null) return;

            texBorder = CreateSolidTexture(new Color(0.08f, 0.08f, 0.12f, 0.95f));
            texTrackDark = CreateSolidTexture(new Color(0.18f, 0.18f, 0.22f, 0.85f));
            texHpFill = CreateSolidTexture(new Color(0.92f, 0.18f, 0.22f, 1f)); // Red Soul Knight
            texStaminaFill = CreateSolidTexture(new Color(0.96f, 0.65f, 0.12f, 1f)); // Gold/Amber Soul Knight
            texEnemyHpFill = CreateSolidTexture(new Color(0.90f, 0.15f, 0.15f, 1f));
            texEnemyDeadFill = CreateSolidTexture(new Color(0.35f, 0.35f, 0.35f, 0.85f));

            texBadgePatrol = CreateSolidTexture(new Color(0.14f, 0.65f, 0.22f, 0.95f));
            texBadgeInvestigate = CreateSolidTexture(new Color(0.05f, 0.68f, 0.72f, 0.95f));
            texBadgeChase = CreateSolidTexture(new Color(0.95f, 0.48f, 0.05f, 0.95f));
            texBadgeAttack = CreateSolidTexture(new Color(0.90f, 0.12f, 0.12f, 0.95f));
            texBadgeFlee = CreateSolidTexture(new Color(0.92f, 0.78f, 0.05f, 0.95f));
            texBadgeDead = CreateSolidTexture(new Color(0.30f, 0.30f, 0.30f, 0.95f));
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void InitStyles()
        {
            if (textStyleCenter != null) return;

            textStyleCenter = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            textStyleCenter.normal.textColor = Color.white;

            textStyleShadow = new GUIStyle(textStyleCenter);
            textStyleShadow.normal.textColor = new Color(0f, 0f, 0f, 0.8f);

            badgeTextStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            badgeTextStyle.normal.textColor = Color.white;

            headerTextStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 0.9f);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            InitTextures();
            InitStyles();

            if (playerHealth == null || playerController == null || enemyFSM == null)
            {
                FindReferences();
            }

            DrawPlayerSoulKnightHUD();

            if (showEnemyOverhead)
            {
                DrawEnemyOverheadBar();
            }
        }

        private void DrawPlayerSoulKnightHUD()
        {
            float totalH = (playerBarHeight * 2f) + playerBarSpacing + 22f;
            float startX = playerHudOffset.x;
            float startY = Screen.height - playerHudOffset.y - totalH;

            // Header Container Label
            GUI.Label(new Rect(startX, startY, playerBarWidth, 18f), "PLAYER STATUS", headerTextStyle);

            // 1. Bar HP Player (Soul Knight Style)
            float hpRatio = 1f;
            float curHp = 100f;
            float maxHp = 100f;
            if (playerHealth != null)
            {
                curHp = playerHealth.CurrentHealth;
                maxHp = playerHealth.MaxHealth;
                hpRatio = maxHp > 0f ? curHp / maxHp : 0f;
            }

            Rect hpBarRect = new Rect(startX, startY + 20f, playerBarWidth, playerBarHeight);
            string hpText = $"HP  {curHp:F0} / {maxHp:F0}";
            DrawSoulKnightBar(hpBarRect, hpRatio, texTrackDark, texHpFill, hpText);

            // 2. Bar Stamina Player (Soul Knight Style)
            float staminaRatio = 1f;
            float curStamina = 100f;
            float maxStamina = 100f;
            if (playerController != null)
            {
                curStamina = playerController.CurrentStamina;
                maxStamina = playerController.MaxStamina;
                staminaRatio = playerController.StaminaRatio;
            }

            Rect staminaBarRect = new Rect(startX, hpBarRect.yMax + playerBarSpacing, playerBarWidth, playerBarHeight);
            string staminaText = $"STAMINA  {curStamina:F0} / {maxStamina:F0}";
            DrawSoulKnightBar(staminaBarRect, staminaRatio, texTrackDark, texStaminaFill, staminaText);
        }

        private void DrawEnemyOverheadBar()
        {
            if (enemyFSM == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldHead = enemyFSM.transform.position + Vector3.up * enemyHeightOffset;
            Vector3 screenPos = cam.WorldToScreenPoint(worldHead);

            // Hanya gambar jika objek berada di depan kamera
            if (screenPos.z <= 0.1f) return;

            float screenX = screenPos.x;
            float screenY = Screen.height - screenPos.y;

            // Ukuran bar overhead besar (200px x 24px)
            float barW = enemyBarWidth;
            float barH = enemyBarHeight;
            float badgeW = 120f;
            float badgeH = 18f;

            float barStartX = screenX - (barW * 0.5f);
            float barStartY = screenY - (barH * 0.5f);
            float badgeStartX = screenX - (badgeW * 0.5f);
            float badgeStartY = barStartY - badgeH - 3f;

            // Ambil data status dan HP Enemy
            EnemyState state = enemyFSM.CurrentState;
            float curHp = 100f;
            float maxHp = 100f;
            if (enemyHealth != null)
            {
                curHp = enemyHealth.CurrentHealth;
                maxHp = enemyHealth.MaxHealth;
            }
            float hpRatio = maxHp > 0f ? curHp / maxHp : 0f;

            // Tentukan tekstur badge dan warna fill
            Texture2D badgeTex = texBadgePatrol;
            Texture2D fillTex = texEnemyHpFill;

            switch (state)
            {
                case EnemyState.Patrol:
                    badgeTex = texBadgePatrol;
                    break;
                case EnemyState.Investigate:
                    badgeTex = texBadgeInvestigate;
                    break;
                case EnemyState.Chase:
                    badgeTex = texBadgeChase;
                    break;
                case EnemyState.Attack:
                    badgeTex = texBadgeAttack;
                    break;
                case EnemyState.Flee:
                    badgeTex = texBadgeFlee;
                    break;
                case EnemyState.Dead:
                    badgeTex = texBadgeDead;
                    fillTex = texEnemyDeadFill;
                    break;
            }

            // 1. Gambar Badge State di atas bar
            Rect badgeBorderRect = new Rect(badgeStartX, badgeStartY, badgeW, badgeH);
            GUI.DrawTexture(badgeBorderRect, texBorder);
            Rect badgeInnerRect = new Rect(badgeStartX + 2, badgeStartY + 2, badgeW - 4, badgeH - 4);
            GUI.DrawTexture(badgeInnerRect, badgeTex);

            string badgeText = $"[{state.ToString().ToUpper()}]";
            GUI.Label(new Rect(badgeBorderRect.x + 1, badgeBorderRect.y + 1, badgeBorderRect.width, badgeBorderRect.height), badgeText, textStyleShadow);
            GUI.Label(badgeBorderRect, badgeText, badgeTextStyle);

            // 2. Gambar Health Bar Enemy (Soul Knight Style)
            Rect enemyBarRect = new Rect(barStartX, barStartY, barW, barH);
            string enemyHpText = state == EnemyState.Dead ? "DEAD (0 / 100)" : $"HP: {curHp:F0} / {maxHp:F0}";
            DrawSoulKnightBar(enemyBarRect, hpRatio, texTrackDark, fillTex, enemyHpText);
        }

        private void DrawSoulKnightBar(Rect rect, float fillRatio, Texture2D trackTex, Texture2D fillTex, string label)
        {
            // Outer Border (Gelap Tebal khas Soul Knight)
            GUI.DrawTexture(rect, texBorder);

            // Inner Recessed Track
            Rect trackRect = new Rect(rect.x + 2.5f, rect.y + 2.5f, rect.width - 5f, rect.height - 5f);
            GUI.DrawTexture(trackRect, trackTex);

            // Colored Fill Bar
            float fillWidth = trackRect.width * Mathf.Clamp01(fillRatio);
            if (fillWidth > 0.5f)
            {
                Rect fillRect = new Rect(trackRect.x, trackRect.y, fillWidth, trackRect.height);
                GUI.DrawTexture(fillRect, fillTex);
            }

            // Text Label di dalam bar (dengan drop shadow halus)
            Rect textShadowRect = new Rect(rect.x + 1.2f, rect.y + 1.2f, rect.width, rect.height);
            GUI.Label(textShadowRect, label, textStyleShadow);
            GUI.Label(rect, label, textStyleCenter);
        }
    }
}
