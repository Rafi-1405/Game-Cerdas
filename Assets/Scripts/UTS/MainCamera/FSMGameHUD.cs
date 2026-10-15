using System.Collections.Generic;
using UnityEngine;
using UTS.Zombies;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FSMGameHUD : MonoBehaviour
    {
        [Header("References (Auto-Found if Empty)")]
        [SerializeField] private SimplePlayerController playerController;
        [SerializeField] private EnemyFSM enemyFSM;

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

        private Texture2D texBadgeBandage;
        private Texture2D texBadgeFirstAid;
        private Texture2D texBadgeMedkit;
        private Texture2D texWeightFill;
        private Texture2D texExtractionFill;
        private Texture2D texExtractionGlow;
        private Texture2D texVictoryBackdrop;
        private UTS.Inventory.PlayerInventory playerInventory;

        private GUIStyle textStyleCenter;
        private GUIStyle textStyleShadow;
        private GUIStyle badgeTextStyle;
        private GUIStyle headerTextStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle victorySubStyle;
        private GUIStyle victoryStatStyle;

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
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<SimplePlayerController>();
            }

            if (enemyFSM == null)
            {
                enemyFSM = FindAnyObjectByType<EnemyFSM>();
            }

            if (playerInventory == null && playerController != null)
            {
                playerInventory = playerController.Inventory;
                if (playerInventory == null)
                {
                    playerInventory = playerController.GetComponent<UTS.Inventory.PlayerInventory>();
                }
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

            texBadgeBandage = CreateSolidTexture(new Color(0.55f, 0.55f, 0.55f, 0.95f)); // Grey
            texBadgeFirstAid = CreateSolidTexture(new Color(0.18f, 0.75f, 0.25f, 0.95f)); // Green
            texBadgeMedkit = CreateSolidTexture(new Color(0.68f, 0.18f, 0.88f, 0.95f)); // Purple
            texWeightFill = CreateSolidTexture(new Color(0.25f, 0.65f, 0.95f, 0.95f)); // Blue Weight Bar

            texExtractionFill = CreateSolidTexture(new Color(0.12f, 0.95f, 0.40f, 0.95f)); // Neon Rescue Green
            texExtractionGlow = CreateSolidTexture(new Color(0.12f, 1.0f, 0.40f, 0.20f));
            texVictoryBackdrop = CreateSolidTexture(new Color(0.06f, 0.09f, 0.07f, 0.94f));
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

            victoryTitleStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            victoryTitleStyle.normal.textColor = new Color(0.2f, 1.0f, 0.45f, 1f);

            victorySubStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            victorySubStyle.normal.textColor = new Color(0.9f, 0.95f, 0.9f, 0.95f);

            victoryStatStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            victoryStatStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            InitTextures();
            InitStyles();

            if (playerController == null || enemyFSM == null)
            {
                FindReferences();
            }

            DrawPlayerSoulKnightHUD();

            if (showEnemyOverhead)
            {
                DrawEnemyOverheadBar();
            }

            DrawInventoryHUD();
            DrawExtractionHUD();
            DrawAimReticle();
        }

        private void DrawPlayerSoulKnightHUD()
        {
            float totalH = (playerBarHeight * 2f) + playerBarSpacing + 44f;
            float startX = playerHudOffset.x;
            float startY = Screen.height - playerHudOffset.y - totalH;

            // Header Container Label
            GUI.Label(new Rect(startX, startY, playerBarWidth, 18f), "PLAYER STATUS", headerTextStyle);

            // 1. Bar HP Player (Soul Knight Style)
            float hpRatio = 1f;
            float curHp = 100f;
            float maxHp = 100f;
            if (playerController != null)
            {
                curHp = playerController.CurrentHealth;
                maxHp = playerController.MaxHealth;
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

            // 3. Status Senter
            bool isFlashlightOn = playerController != null && playerController.Flashlight != null && playerController.Flashlight.IsOn;
            bool isBelt = playerController != null && playerController.Flashlight != null && playerController.Flashlight.IsAttachedToBelt;
            string flMode = isBelt ? "BELT" : "HAND";
            string flStatus = isFlashlightOn ? $"[F] SENTER: MENYALA ({flMode})" : "[F] SENTER: PADAM";
            Rect flRect = new Rect(startX, staminaBarRect.yMax + 5f, playerBarWidth, 18f);
            GUI.Label(new Rect(flRect.x + 1f, flRect.y + 1f, flRect.width, flRect.height), flStatus, textStyleShadow);
            GUI.Label(flRect, flStatus, isFlashlightOn ? badgeTextStyle : headerTextStyle);
        }

        private void DrawEnemyOverheadBar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 playerPos = playerController != null ? playerController.transform.position : cam.transform.position;

            // Prioritaskan musuh aktif dari ZombieSwarmAgent
            var nearbyAgents = new List<(EnemyFSM fsm, float dist)>();
            const float maxDist = 22f;
            const int maxDrawn = 3;

            foreach (ZombieSwarmAgent agent in ZombieSwarmAgent.ActiveAgents)
            {
                if (agent == null) continue;
                EnemyFSM fsm = agent.GetComponent<EnemyFSM>();
                if (fsm == null) continue;

                float d = Vector3.Distance(playerPos, agent.transform.position);
                if (d <= maxDist)
                {
                    nearbyAgents.Add((fsm, d));
                }
            }

            if (nearbyAgents.Count > 0)
            {
                nearbyAgents.Sort((a, b) => a.dist.CompareTo(b.dist));
                int limit = Mathf.Min(maxDrawn, nearbyAgents.Count);
                for (int i = 0; i < limit; i++)
                {
                    DrawEnemyOverheadBarFor(nearbyAgents[i].fsm, cam);
                }
            }
            else if (enemyFSM != null && Vector3.Distance(playerPos, enemyFSM.transform.position) <= maxDist)
            {
                DrawEnemyOverheadBarFor(enemyFSM, cam);
            }
        }

        private void DrawEnemyOverheadBarFor(EnemyFSM fsm, Camera cam)
        {
            if (fsm == null || cam == null) return;

            Vector3 worldHead = fsm.transform.position + Vector3.up * enemyHeightOffset;
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
            EnemyState state = fsm.CurrentState;
            float curHp = fsm.CurrentHealth;
            float maxHp = fsm.MaxHealth;
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

            string badgeText = state == EnemyState.Patrol ? "[WANDER]" : $"[{state.ToString().ToUpper()}]";
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

        private void DrawInventoryHUD()
        {
            if (playerInventory == null)
            {
                if (playerController != null)
                {
                    playerInventory = playerController.Inventory;
                    if (playerInventory == null)
                        playerInventory = playerController.GetComponent<UTS.Inventory.PlayerInventory>();
                }
                if (playerInventory == null) return;
            }

            float panelW = 230f;
            float panelH = 104f;
            float startX = Screen.width - panelW - 20f;
            float startY = 20f;

            // Header Container Label
            GUI.Label(new Rect(startX, startY, panelW, 18f), "SCAVENGED RESOURCES", headerTextStyle);

            // Container Panel Background
            Rect panelRect = new Rect(startX, startY + 18f, panelW, panelH);
            GUI.DrawTexture(panelRect, texBorder);
            Rect panelInnerRect = new Rect(panelRect.x + 2f, panelRect.y + 2f, panelRect.width - 4f, panelRect.height - 4f);
            GUI.DrawTexture(panelInnerRect, texTrackDark);

            // Row 1: Total Value / Score
            int totalVal = playerInventory.TotalValue;
            Rect valRect = new Rect(panelInnerRect.x + 8f, panelInnerRect.y + 6f, panelInnerRect.width - 16f, 20f);
            string valText = $"VALUE / SCORE: {totalVal} PTS";
            GUI.Label(new Rect(valRect.x + 1f, valRect.y + 1f, valRect.width, valRect.height), valText, textStyleShadow);
            GUI.Label(valRect, valText, headerTextStyle);

            // Row 2: Weight Bar
            float curW = playerInventory.CurrentWeight;
            float maxW = playerInventory.MaxWeight;
            float wRatio = playerInventory.WeightRatio;
            Rect weightBarRect = new Rect(panelInnerRect.x + 8f, valRect.yMax + 4f, panelInnerRect.width - 16f, 20f);
            string weightText = $"WEIGHT  {curW:F1} / {maxW:F1} KG";
            DrawSoulKnightBar(weightBarRect, wRatio, texBorder, texWeightFill, weightText);

            // Row 3: Items Breakdown (3 Badges)
            float badgeW = (panelInnerRect.width - 24f) / 3f;
            float badgeH = 22f;
            float badgeY = weightBarRect.yMax + 8f;

            // Bandage (Grey)
            Rect bRect = new Rect(panelInnerRect.x + 8f, badgeY, badgeW, badgeH);
            GUI.DrawTexture(bRect, texBorder);
            GUI.DrawTexture(new Rect(bRect.x + 1.5f, bRect.y + 1.5f, bRect.width - 3f, bRect.height - 3f), texBadgeBandage);
            string bText = $"BND: {playerInventory.BandageCount}";
            GUI.Label(new Rect(bRect.x + 1f, bRect.y + 1f, bRect.width, bRect.height), bText, textStyleShadow);
            GUI.Label(bRect, bText, badgeTextStyle);

            // First Aid Kit (Green)
            Rect fRect = new Rect(bRect.xMax + 4f, badgeY, badgeW, badgeH);
            GUI.DrawTexture(fRect, texBorder);
            GUI.DrawTexture(new Rect(fRect.x + 1.5f, fRect.y + 1.5f, fRect.width - 3f, fRect.height - 3f), texBadgeFirstAid);
            string fText = $"FAK: {playerInventory.FirstAidCount}";
            GUI.Label(new Rect(fRect.x + 1f, fRect.y + 1f, fRect.width, fRect.height), fText, textStyleShadow);
            GUI.Label(fRect, fText, badgeTextStyle);

            // Medkit (Purple)
            Rect mRect = new Rect(fRect.xMax + 4f, badgeY, badgeW, badgeH);
            GUI.DrawTexture(mRect, texBorder);
            GUI.DrawTexture(new Rect(mRect.x + 1.5f, mRect.y + 1.5f, mRect.width - 3f, mRect.height - 3f), texBadgeMedkit);
            string mText = $"MED: {playerInventory.MedkitCount}";
            GUI.Label(new Rect(mRect.x + 1f, mRect.y + 1f, mRect.width, mRect.height), mText, textStyleShadow);
            GUI.Label(mRect, mText, badgeTextStyle);
        }

        private void DrawExtractionHUD()
        {
            var zone = UTS.Environment.GreenSmokeExtractionZone.Instance;
            if (zone == null) return;

            // 1. Evacuation Progress Bar (Saat player berada di dalam zona dan belum selesai evakuasi)
            if (zone.IsPlayerInside && !zone.IsExtracted)
            {
                float barW = 340f;
                float barH = 26f;
                float barX = (Screen.width - barW) * 0.5f;
                float barY = 28f;

                // Glowing background banner
                Rect glowRect = new Rect(barX - 10f, barY - 18f, barW + 20f, barH + 34f);
                GUI.DrawTexture(glowRect, texExtractionGlow);

                // Title label
                string titleText = "EVAKUASI ZONA ASAP HIJAU";
                Rect titleRect = new Rect(barX, barY - 18f, barW, 16f);
                GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), titleText, textStyleShadow);
                GUI.Label(titleRect, titleText, badgeTextStyle);

                // Progress Bar
                Rect progRect = new Rect(barX, barY, barW, barH);
                string progText = $"BERTAHAN: {zone.CurrentHoldTime:F1}s / {zone.RequiredHoldTime:F0}s ({(zone.ProgressRatio * 100f):F0}%)";
                DrawSoulKnightBar(progRect, zone.ProgressRatio, texTrackDark, texExtractionFill, progText);

                // Warning reminder
                string noteText = "Tetap berada di dalam lingkaran asap hijau!";
                Rect noteRect = new Rect(barX, progRect.yMax + 3f, barW, 14f);
                GUI.Label(new Rect(noteRect.x + 1f, noteRect.y + 1f, noteRect.width, noteRect.height), noteText, textStyleShadow);
                GUI.Label(noteRect, noteText, badgeTextStyle);
            }

            // 2. Victory Modal Dialog (Ketika berhasil bertahan 10 detik)
            if (zone.IsExtracted)
            {
                float modalW = 460f;
                float modalH = 260f;
                float modalX = (Screen.width - modalW) * 0.5f;
                float modalY = (Screen.height - modalH) * 0.5f;

                Rect outerRect = new Rect(modalX, modalY, modalW, modalH);
                Rect innerRect = new Rect(modalX + 4f, modalY + 4f, modalW - 8f, modalH - 8f);

                // Border & background
                GUI.DrawTexture(outerRect, texBadgeFirstAid); // Green border accent
                GUI.DrawTexture(innerRect, texVictoryBackdrop);

                // Title
                Rect titleRect = new Rect(innerRect.x, innerRect.y + 16f, innerRect.width, 32f);
                GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), "EXTRACTION SUCCESSFUL!", textStyleShadow);
                GUI.Label(titleRect, "EXTRACTION SUCCESSFUL!", victoryTitleStyle);

                // Subtitle
                Rect subRect = new Rect(innerRect.x, titleRect.yMax + 4f, innerRect.width, 20f);
                GUI.Label(new Rect(subRect.x + 1f, subRect.y + 1f, subRect.width, subRect.height), "Kamu berhasil bertahan dan dievakuasi dengan helikopter!", textStyleShadow);
                GUI.Label(subRect, "Kamu berhasil bertahan dan dievakuasi dengan helikopter!", victorySubStyle);

                // Divider line
                GUI.DrawTexture(new Rect(innerRect.x + 20f, subRect.yMax + 10f, innerRect.width - 40f, 2f), texExtractionFill);

                // Stats recap
                int totalVal = playerInventory != null ? playerInventory.TotalValue : 0;
                float curW = playerInventory != null ? playerInventory.CurrentWeight : 0f;
                float maxW = playerInventory != null ? playerInventory.MaxWeight : 100f;
                int bCount = playerInventory != null ? playerInventory.BandageCount : 0;
                int fCount = playerInventory != null ? playerInventory.FirstAidCount : 0;
                int mCount = playerInventory != null ? playerInventory.MedkitCount : 0;

                float statY = subRect.yMax + 20f;
                float statLineH = 22f;

                string valStr = $"TOTAL NILAI RESOURCING : {totalVal} PTS";
                Rect valR = new Rect(innerRect.x + 30f, statY, innerRect.width - 60f, statLineH);
                GUI.Label(new Rect(valR.x + 1f, valR.y + 1f, valR.width, valR.height), valStr, textStyleShadow);
                GUI.Label(valR, valStr, victoryStatStyle);

                string wStr = $"TOTAL BEBAN DIBAWA     : {curW:F1} / {maxW:F1} KG";
                Rect wR = new Rect(innerRect.x + 30f, valR.yMax + 4f, innerRect.width - 60f, statLineH);
                GUI.Label(new Rect(wR.x + 1f, wR.y + 1f, wR.width, wR.height), wStr, textStyleShadow);
                GUI.Label(wR, wStr, victoryStatStyle);

                string itemStr = $"RINCIAN LOOT           : {bCount} Bandage | {fCount} First Aid | {mCount} Medkit";
                Rect itemR = new Rect(innerRect.x + 30f, wR.yMax + 4f, innerRect.width - 60f, statLineH);
                GUI.Label(new Rect(itemR.x + 1f, itemR.y + 1f, itemR.width, itemR.height), itemStr, textStyleShadow);
                GUI.Label(itemR, itemStr, victoryStatStyle);

                // Restart instructions
                string restartStr = "Tekan [R] untuk Restart / Main Lagi";
                Rect restR = new Rect(innerRect.x, innerRect.yMax - 36f, innerRect.width, 24f);
                GUI.Label(new Rect(restR.x + 1f, restR.y + 1f, restR.width, restR.height), restartStr, textStyleShadow);
                GUI.Label(restR, restartStr, badgeTextStyle);
            }
        }

        private void DrawAimReticle()
        {
            if (playerController == null || !playerController.IsAiming) return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            // Reticle center dot
            float dotSize = 4f;
            Rect dotRect = new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize);
            GUI.DrawTexture(dotRect, texBorder);
            GUI.DrawTexture(new Rect(dotRect.x + 0.5f, dotRect.y + 0.5f, dotRect.width - 1f, dotRect.height - 1f), texBadgeChase);

            // Crosshair tick marks
            float lineLen = 10f;
            float lineGap = 6f;
            float lineThick = 2f;

            // Left
            GUI.DrawTexture(new Rect(centerX - lineGap - lineLen, centerY - lineThick * 0.5f, lineLen, lineThick), texBadgeChase);
            // Right
            GUI.DrawTexture(new Rect(centerX + lineGap, centerY - lineThick * 0.5f, lineLen, lineThick), texBadgeChase);
            // Top
            GUI.DrawTexture(new Rect(centerX - lineThick * 0.5f, centerY - lineGap - lineLen, lineThick, lineLen), texBadgeChase);
            // Bottom
            GUI.DrawTexture(new Rect(centerX - lineThick * 0.5f, centerY + lineGap, lineThick, lineLen), texBadgeChase);

            // [AIMING] text label removed per user request
        }
    }
}
