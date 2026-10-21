using System;
using System.Collections.Generic;
using UnityEngine;
using UTS.Zombies;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Inventory Animation & State")]
        [SerializeField] private bool isInventoryOpen = false;
        private float inventoryAnimProgress = 0f;
        private float itemFeedbackTimer = 0f;
        private string itemFeedbackText = "";

        // Tekstur prosedural untuk styling Soul Knight & Minecraft Hotbar
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

        // Minecraft Hotbar Textures
        private Texture2D texMinecraftSlot;
        private Texture2D texMinecraftSlotGold;
        private Texture2D texMinecraftPanelBg;
        private Texture2D texIconBandage;
        private Texture2D texIconFirstAid;
        private Texture2D texIconMedkit;

        private UTS.Inventory.PlayerInventory playerInventory;

        private GUIStyle textStyleCenter;
        private GUIStyle textStyleShadow;
        private GUIStyle badgeTextStyle;
        private GUIStyle headerTextStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle victorySubStyle;
        private GUIStyle victoryStatStyle;

        private GUIStyle slotKeyStyle;
        private GUIStyle slotCountStyle;
        private GUIStyle slotNameStyle;
        private GUIStyle slotFeedbackStyle;
        private GUIStyle hintTextStyle;

        private void Awake()
        {
            FindReferences();
            InitTextures();
        }

        private void Start()
        {
            FindReferences();
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                // Toggle Inventory dengan tombol E (atau Tab sebagai alternatif)
                if (WasInventoryTogglePressed())
                {
                    isInventoryOpen = !isInventoryOpen;
                }

                // Hotkey 1, 2, 3 untuk konsumsi item
                if (WasKey1Pressed())
                {
                    TryUseItem(UTS.Inventory.ScavengeItemType.Bandage);
                }
                else if (WasKey2Pressed())
                {
                    TryUseItem(UTS.Inventory.ScavengeItemType.FirstAidKit);
                }
                else if (WasKey3Pressed())
                {
                    TryUseItem(UTS.Inventory.ScavengeItemType.Medkit);
                }
            }

            // Animasi slide-up / slide-down halus
            float targetProgress = isInventoryOpen ? 1f : 0f;
            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.02f;
            inventoryAnimProgress = Mathf.MoveTowards(inventoryAnimProgress, targetProgress, dt * 8f);

            // Timer notifikasi konsumsi item
            if (itemFeedbackTimer > 0f)
            {
                itemFeedbackTimer -= dt;
            }
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

            texBadgeBandage = CreateSolidTexture(new Color(0.55f, 0.55f, 0.55f, 0.95f));
            texBadgeFirstAid = CreateSolidTexture(new Color(0.18f, 0.75f, 0.25f, 0.95f));
            texBadgeMedkit = CreateSolidTexture(new Color(0.68f, 0.18f, 0.88f, 0.95f));
            texWeightFill = CreateSolidTexture(new Color(0.25f, 0.65f, 0.95f, 0.95f));

            texExtractionFill = CreateSolidTexture(new Color(0.12f, 0.95f, 0.40f, 0.95f));
            texExtractionGlow = CreateSolidTexture(new Color(0.12f, 1.0f, 0.40f, 0.20f));
            texVictoryBackdrop = CreateSolidTexture(new Color(0.06f, 0.09f, 0.07f, 0.94f));

            // Minecraft Hotbar textures
            texMinecraftSlot = CreateMinecraftSlotTexture(false);
            texMinecraftSlotGold = CreateMinecraftSlotTexture(true);
            texMinecraftPanelBg = CreateSolidTexture(new Color(0.12f, 0.12f, 0.15f, 0.95f));

            // Procedural 2D item icon textures
            texIconBandage = CreateBandageIcon(32);
            texIconFirstAid = CreateFirstAidIcon(32);
            texIconMedkit = CreateMedkitIcon(32);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateMinecraftSlotTexture(bool isGold)
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cOuter = new Color(0.10f, 0.10f, 0.12f, 0.98f);
            Color cShadow = isGold ? new Color(0.55f, 0.42f, 0.08f, 0.98f) : new Color(0.24f, 0.24f, 0.26f, 0.98f);
            Color cLight = isGold ? new Color(1.0f, 0.88f, 0.28f, 1.0f) : new Color(0.78f, 0.78f, 0.82f, 0.98f);
            Color cInner = new Color(0.28f, 0.28f, 0.32f, 0.92f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    if (x == 0 || x == s - 1 || y == 0 || y == s - 1)
                    {
                        tex.SetPixel(x, y, cOuter);
                    }
                    else if (x <= 2 || y >= s - 3)
                    {
                        tex.SetPixel(x, y, cShadow);
                    }
                    else if (x >= s - 3 || y <= 2)
                    {
                        tex.SetPixel(x, y, cLight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cInner);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateBandageIcon(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = Color.clear;
            Color gauzeWhite = new Color(0.92f, 0.91f, 0.88f, 1f);
            Color gauzeShade = new Color(0.74f, 0.72f, 0.68f, 1f);
            Color outline = new Color(0.20f, 0.18f, 0.16f, 1f);
            Color redCross = new Color(0.85f, 0.15f, 0.15f, 1f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Draw angled rolled bandage
            for (int y = 7; y <= 24; y++)
            {
                for (int x = 7; x <= 24; x++)
                {
                    bool isBorder = (x == 7 || x == 24 || y == 7 || y == 24);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else
                    {
                        bool isStripe = ((x + y) % 4 == 0);
                        tex.SetPixel(x, y, isStripe ? gauzeShade : gauzeWhite);
                    }
                }
            }

            // Center Red Cross
            for (int y = 12; y <= 19; y++)
            {
                for (int x = 14; x <= 17; x++)
                {
                    tex.SetPixel(x, y, redCross);
                }
            }
            for (int y = 14; y <= 17; y++)
            {
                for (int x = 11; x <= 20; x++)
                {
                    tex.SetPixel(x, y, redCross);
                }
            }

            tex.Apply();
            return tex;
        }

        private Texture2D CreateFirstAidIcon(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = Color.clear;
            Color bodyRed = new Color(0.88f, 0.16f, 0.16f, 1f);
            Color bodyDark = new Color(0.60f, 0.10f, 0.10f, 1f);
            Color outline = new Color(0.15f, 0.05f, 0.05f, 1f);
            Color metal = new Color(0.80f, 0.80f, 0.85f, 1f);
            Color crossWhite = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Top handle
            for (int y = 24; y <= 27; y++)
            {
                for (int x = 11; x <= 20; x++)
                {
                    if (y == 27 || x == 11 || x == 20)
                    {
                        tex.SetPixel(x, y, metal);
                    }
                }
            }

            // Main briefcase body
            for (int y = 6; y <= 23; y++)
            {
                for (int x = 5; x <= 26; x++)
                {
                    if (x == 5 || x == 26 || y == 6 || y == 23)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else if (y <= 9)
                    {
                        tex.SetPixel(x, y, bodyDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bodyRed);
                    }
                }
            }

            // Side silver latches
            for (int y = 13; y <= 16; y++)
            {
                tex.SetPixel(5, y, metal);
                tex.SetPixel(6, y, metal);
                tex.SetPixel(25, y, metal);
                tex.SetPixel(26, y, metal);
            }

            // Bold white medical cross in center
            for (int y = 10; y <= 19; y++)
            {
                for (int x = 14; x <= 17; x++)
                {
                    tex.SetPixel(x, y, crossWhite);
                }
            }
            for (int y = 13; y <= 16; y++)
            {
                for (int x = 10; x <= 21; x++)
                {
                    tex.SetPixel(x, y, crossWhite);
                }
            }

            tex.Apply();
            return tex;
        }

        private Texture2D CreateMedkitIcon(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = Color.clear;
            Color casePurple = new Color(0.55f, 0.18f, 0.78f, 1f);
            Color caseDark = new Color(0.35f, 0.10f, 0.52f, 1f);
            Color goldAccent = new Color(0.96f, 0.78f, 0.22f, 1f);
            Color outline = new Color(0.12f, 0.06f, 0.18f, 1f);
            Color crossWhite = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Top tactical double handle
            for (int y = 24; y <= 28; y++)
            {
                for (int x = 10; x <= 21; x++)
                {
                    if (y >= 27 || x <= 12 || x >= 19)
                    {
                        tex.SetPixel(x, y, goldAccent);
                    }
                }
            }

            // Main case body
            for (int y = 5; y <= 23; y++)
            {
                for (int x = 4; x <= 27; x++)
                {
                    if (x == 4 || x == 27 || y == 5 || y == 23)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else if (y <= 8)
                    {
                        tex.SetPixel(x, y, caseDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, casePurple);
                    }
                }
            }

            // Gold reinforced corner brackets
            int[] cx = { 4, 5, 6, 25, 26, 27 };
            int[] cy = { 5, 6, 7, 21, 22, 23 };
            foreach (int px in cx)
            {
                foreach (int py in cy)
                {
                    tex.SetPixel(px, py, goldAccent);
                }
            }

            // Center gold backing badge
            for (int y = 10; y <= 18; y++)
            {
                for (int x = 11; x <= 20; x++)
                {
                    tex.SetPixel(x, y, goldAccent);
                }
            }

            // White medical cross inside the gold badge
            for (int y = 11; y <= 17; y++)
            {
                for (int x = 14; x <= 17; x++)
                {
                    tex.SetPixel(x, y, crossWhite);
                }
            }
            for (int y = 13; y <= 15; y++)
            {
                for (int x = 12; x <= 19; x++)
                {
                    tex.SetPixel(x, y, crossWhite);
                }
            }

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
            textStyleShadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

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

            slotKeyStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            slotKeyStyle.normal.textColor = new Color(1.0f, 0.85f, 0.25f, 1f); // Bright Gold

            slotCountStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight
            };
            slotCountStyle.normal.textColor = Color.white;

            slotNameStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            slotNameStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f, 0.95f);

            slotFeedbackStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            slotFeedbackStyle.normal.textColor = new Color(0.2f, 1.0f, 0.45f, 1f); // Neon Green

            hintTextStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            hintTextStyle.normal.textColor = new Color(0.75f, 0.75f, 0.82f, 0.85f);

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
            InitTextures();
            InitStyles();

            if (playerController == null || enemyFSM == null)
            {
                FindReferences();
            }

            if (Event.current.type != EventType.Repaint) return;

            DrawPlayerSoulKnightHUD();

            if (showEnemyOverhead)
            {
                DrawEnemyOverheadBar();
            }

            // Minecraft Bottom Hotbar Inventory (menggantikan UI pojok kanan atas)
            DrawMinecraftBottomInventory();

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

            // Line of Sight check: Bar overhead tidak boleh tembus tembok / partisi
            Vector3 camPos = cam.transform.position;
            Vector3 toHead = worldHead - camPos;
            float dist = toHead.magnitude;
            if (dist > 0.05f)
            {
                Vector3 dir = toHead / dist;
                int wallMask = LayerMask.GetMask("Wall", "Obstacle", "Default");
                RaycastHit[] hits = Physics.RaycastAll(camPos, dir, dist - 0.25f, wallMask, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform == fsm.transform || hit.transform.IsChildOf(fsm.transform)) continue;
                    if (playerController != null && (hit.transform == playerController.transform || hit.transform.IsChildOf(playerController.transform))) continue;

                    // Terhalang tembok atau rintangan lain
                    return;
                }
            }

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
            if (!string.IsNullOrEmpty(label))
            {
                Rect textShadowRect = new Rect(rect.x + 1.2f, rect.y + 1.2f, rect.width, rect.height);
                GUI.Label(textShadowRect, label, textStyleShadow);
                GUI.Label(rect, label, textStyleCenter);
            }
        }

        /// <summary>
        /// Menggambar Hotbar Inventaris 3 Slot gaya Minecraft yang meluncur dari bawah saat tombol E ditekan.
        /// Skor / Value sengaja dihilangkan dari HUD gameplay dan hanya ditampilkan saat game selesai (evakuasi).
        /// </summary>
        private void DrawMinecraftBottomInventory()
        {
            if (playerInventory == null)
            {
                if (playerController != null)
                {
                    playerInventory = playerController.Inventory;
                    if (playerInventory == null)
                        playerInventory = playerController.GetComponent<UTS.Inventory.PlayerInventory>();
                }
            }

            // Jika tertutup sepenuhnya dan tidak sedang membuka, tampilkan pill petunjuk kecil di bagian bawah layar
            if (!isInventoryOpen && inventoryAnimProgress <= 0.01f)
            {
                float hintW = 130f;
                float hintH = 24f;
                float hintX = (Screen.width - hintW) * 0.5f;
                float hintY = Screen.height - hintH - 12f;
                Rect hintRect = new Rect(hintX, hintY, hintW, hintH);

                GUI.DrawTexture(hintRect, texBorder);
                GUI.DrawTexture(new Rect(hintRect.x + 1.5f, hintRect.y + 1.5f, hintRect.width - 3f, hintRect.height - 3f), texTrackDark);

                string hText = "[E] INVENTORY";
                GUI.Label(new Rect(hintRect.x + 1f, hintRect.y + 1f, hintRect.width, hintRect.height), hText, textStyleShadow);
                GUI.Label(hintRect, hText, badgeTextStyle);
                return;
            }

            // Dimensi Panel Minecraft Hotbar
            float panelW = 310f;
            float panelH = 150f;
            float panelX = (Screen.width - panelW) * 0.5f;

            // Animasi slide-up dari bawah layar (SmoothStep)
            float t = Mathf.SmoothStep(0f, 1f, inventoryAnimProgress);
            float hiddenY = Screen.height + 15f;
            float openY = Screen.height - panelH - 18f;
            float panelY = Mathf.Lerp(hiddenY, openY, t);

            Rect panelRect = new Rect(panelX, panelY, panelW, panelH);

            // Floating feedback banner saat item digunakan
            if (itemFeedbackTimer > 0f && !string.IsNullOrEmpty(itemFeedbackText))
            {
                float fbW = 240f;
                float fbH = 22f;
                float fbX = panelX + (panelW - fbW) * 0.5f;
                float fbY = panelY - fbH - 6f;
                Rect fbRect = new Rect(fbX, fbY, fbW, fbH);

                GUI.DrawTexture(fbRect, texBorder);
                GUI.DrawTexture(new Rect(fbRect.x + 1.5f, fbRect.y + 1.5f, fbRect.width - 3f, fbRect.height - 3f), texTrackDark);
                GUI.Label(new Rect(fbRect.x + 1f, fbRect.y + 1f, fbRect.width, fbRect.height), itemFeedbackText, textStyleShadow);
                GUI.Label(fbRect, itemFeedbackText, slotFeedbackStyle);
            }

            // Container Panel Background (Minecraft Stone / Charcoal Frame)
            GUI.DrawTexture(panelRect, texBorder);
            Rect panelInnerRect = new Rect(panelRect.x + 2f, panelRect.y + 2f, panelRect.width - 4f, panelRect.height - 4f);
            GUI.DrawTexture(panelInnerRect, texMinecraftPanelBg);

            // Baris 1: Header Title & Berat Ransel (Skor sengaja tidak ditampilkan)
            float curW = playerInventory != null ? playerInventory.CurrentWeight : 0f;
            float maxW = playerInventory != null ? playerInventory.MaxWeight : 40f;
            float wRatio = playerInventory != null ? playerInventory.WeightRatio : 0f;

            Rect titleRect = new Rect(panelInnerRect.x + 10f, panelInnerRect.y + 6f, 130f, 16f);
            GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), "INVENTORY  [E]", textStyleShadow);
            GUI.Label(titleRect, "INVENTORY  [E]", headerTextStyle);

            string weightText = $"BEBAN: {curW:F1} / {maxW:F1} KG";
            GUIStyle weightStyle = new GUIStyle(headerTextStyle) { alignment = TextAnchor.MiddleRight };
            Rect weightLabelRect = new Rect(panelInnerRect.xMax - 140f, panelInnerRect.y + 6f, 130f, 16f);
            GUI.Label(new Rect(weightLabelRect.x + 1f, weightLabelRect.y + 1f, weightLabelRect.width, weightLabelRect.height), weightText, textStyleShadow);
            GUI.Label(weightLabelRect, weightText, weightStyle);

            // Mini Weight Progress Bar
            Rect weightBarRect = new Rect(panelInnerRect.x + 10f, titleRect.yMax + 4f, panelInnerRect.width - 20f, 6f);
            DrawSoulKnightBar(weightBarRect, wRatio, texBorder, texWeightFill, "");

            // 3 Slot Kotak Gaya Minecraft
            float slotSize = 74f;
            float slotGap = 12f;
            float totalSlotsW = (3f * slotSize) + (2f * slotGap);
            float startSlotX = panelRect.x + (panelW - totalSlotsW) * 0.5f;
            float slotY = weightBarRect.yMax + 8f;

            // Slot 1: Bandage (+15 HP, Key [1])
            int bCount = playerInventory != null ? playerInventory.BandageCount : 0;
            Rect slot1Rect = new Rect(startSlotX, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot1Rect, 0, "[1]", texIconBandage, bCount, "BANDAGE", "+15 HP");

            // Slot 2: First Aid Kit (+35 HP, Key [2])
            int fCount = playerInventory != null ? playerInventory.FirstAidCount : 0;
            Rect slot2Rect = new Rect(startSlotX + slotSize + slotGap, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot2Rect, 1, "[2]", texIconFirstAid, fCount, "FIRST AID", "+35 HP");

            // Slot 3: Medkit (+70 HP, Key [3])
            int mCount = playerInventory != null ? playerInventory.MedkitCount : 0;
            Rect slot3Rect = new Rect(startSlotX + (slotSize + slotGap) * 2f, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot3Rect, 2, "[3]", texIconMedkit, mCount, "MEDKIT", "+70 HP");

            // Footer Subtitle Hint
            string hint = "Tekan [1] Bandage  |  [2] First Aid  |  [3] Medkit";
            Rect footerRect = new Rect(panelInnerRect.x, panelInnerRect.yMax - 18f, panelInnerRect.width, 14f);
            GUI.Label(new Rect(footerRect.x + 1f, footerRect.y + 1f, footerRect.width, footerRect.height), hint, textStyleShadow);
            GUI.Label(footerRect, hint, hintTextStyle);
        }

        private void DrawMinecraftSlot(Rect rect, int slotIndex, string keyBadge, Texture2D iconTex, int count, string itemName, string healLabel)
        {
            // 1. Gambar latar belakang beveled Minecraft slot (aksen emas jika memiliki item)
            Texture2D slotTex = count > 0 ? texMinecraftSlotGold : texMinecraftSlot;
            GUI.DrawTexture(rect, slotTex);

            // 2. Gambar Ikon Item di tengah slot
            float iconSize = 44f;
            float iconX = rect.x + (rect.width - iconSize) * 0.5f;
            float iconY = rect.y + 8f;
            Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

            if (iconTex != null)
            {
                // Jika jumlah 0, beri efek transparansi sedikit redup
                Color oldColor = GUI.color;
                if (count <= 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.35f);
                }
                GUI.DrawTexture(iconRect, iconTex);
                GUI.color = oldColor;
            }

            // 3. Hotkey Badge di pojok kiri atas slot
            Rect badgeRect = new Rect(rect.x + 4f, rect.y + 4f, 20f, 14f);
            GUI.DrawTexture(badgeRect, texBorder);
            GUI.Label(new Rect(badgeRect.x + 1f, badgeRect.y + 1f, badgeRect.width, badgeRect.height), keyBadge, textStyleShadow);
            GUI.Label(badgeRect, keyBadge, slotKeyStyle);

            // 4. Stack Count di pojok kanan bawah slot (Minecraft Style: x0, x1, ...)
            string countText = $"x{count}";
            Rect countRect = new Rect(rect.x, rect.y, rect.width - 6f, rect.height - 4f);
            GUI.Label(new Rect(countRect.x + 1.2f, countRect.y + 1.2f, countRect.width, countRect.height), countText, textStyleShadow);

            Color oldTextColor = slotCountStyle.normal.textColor;
            slotCountStyle.normal.textColor = count > 0 ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.7f);
            GUI.Label(countRect, countText, slotCountStyle);
            slotCountStyle.normal.textColor = oldTextColor;

            // 5. Nama Item & Status Pemulihan di bawah slot
            string subText = $"{itemName} ({healLabel})";
            Rect nameRect = new Rect(rect.x - 12f, rect.yMax + 2f, rect.width + 24f, 14f);
            GUI.Label(new Rect(nameRect.x + 1f, nameRect.y + 1f, nameRect.width, nameRect.height), subText, textStyleShadow);
            GUI.Label(nameRect, subText, slotNameStyle);
        }

        private void TryUseItem(UTS.Inventory.ScavengeItemType type)
        {
            if (playerController == null)
            {
                FindReferences();
                if (playerController == null) return;
            }

            if (playerController.CurrentHealth >= playerController.MaxHealth)
            {
                SetItemFeedback("HP SUDAH PENUH (100%)");
                return;
            }

            int count = playerInventory != null ? playerInventory.GetCount(type) : 0;
            string itemName = UTS.Inventory.ScavengeItemInfo.GetName(type);

            if (count <= 0)
            {
                SetItemFeedback($"{itemName.ToUpper()} KOSONG / HABIS!");
                return;
            }

            if (playerController.UseItem(type))
            {
                float heal = type == UTS.Inventory.ScavengeItemType.Bandage ? 15f : (type == UTS.Inventory.ScavengeItemType.FirstAidKit ? 35f : 70f);
                SetItemFeedback($"+{heal:F0} HP ({itemName.ToUpper()})");
            }
        }

        private void SetItemFeedback(string message)
        {
            itemFeedbackText = message;
            itemFeedbackTimer = 2.2f;
        }

        private bool WasInventoryTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.eKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        private bool WasKey1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#endif
        }

        private bool WasKey2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#endif
        }

        private bool WasKey3Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
#endif
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

            // 2. Victory Modal Dialog (Ketika berhasil bertahan 10 detik - Menampilkan Total Nilai / Score)
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

                // Stats recap (Menampilkan Skor Akhir Hasil Petualangan)
                int totalVal = playerInventory != null ? playerInventory.TotalValue : 0;
                float totalCurW = playerInventory != null ? playerInventory.CurrentWeight : 0f;
                float totalMaxW = playerInventory != null ? playerInventory.MaxWeight : 100f;
                int bCount = playerInventory != null ? playerInventory.BandageCount : 0;
                int fCount = playerInventory != null ? playerInventory.FirstAidCount : 0;
                int mCount = playerInventory != null ? playerInventory.MedkitCount : 0;

                float statY = subRect.yMax + 20f;
                float statLineH = 22f;

                string valStr = $"TOTAL NILAI RESOURCING : {totalVal} PTS";
                Rect valR = new Rect(innerRect.x + 30f, statY, innerRect.width - 60f, statLineH);
                GUI.Label(new Rect(valR.x + 1f, valR.y + 1f, valR.width, valR.height), valStr, textStyleShadow);
                GUI.Label(valR, valStr, victoryStatStyle);

                string wStr = $"TOTAL BEBAN DIBAWA     : {totalCurW:F1} / {totalMaxW:F1} KG";
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
        }
    }
}
