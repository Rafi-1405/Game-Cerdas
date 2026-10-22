using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private GTACameraController cameraController;

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

        [Header("Playtime & Game Flow State")]
        private float elapsedPlaytime = 0f;
        private float finalPlaytime = 0f;
        private bool extractionRecorded = false;
        private int finalScore = 0;
        private int finalSpeedBonus = 0;
        private string finalRank = "RANK B  ★ SURVIVOR ★";
        private Color finalRankColor = new Color(0.25f, 0.75f, 1.0f);
        private float mainMenuFeedbackTimer = 0f;
        private string mainMenuFeedbackText = "";

        // Tekstur prosedural untuk styling Soul Knight & Minecraft Hotbar (NonSerialized agar tidak rusak saat hot-reload)
        [System.NonSerialized] private Texture2D texBorder;
        [System.NonSerialized] private Texture2D texTrackDark;
        [System.NonSerialized] private Texture2D texHpFill;
        [System.NonSerialized] private Texture2D texStaminaFill;
        [System.NonSerialized] private Texture2D texEnemyHpFill;
        [System.NonSerialized] private Texture2D texEnemyDeadFill;

        [System.NonSerialized] private Texture2D texBadgePatrol;
        [System.NonSerialized] private Texture2D texBadgeInvestigate;
        [System.NonSerialized] private Texture2D texBadgeChase;
        [System.NonSerialized] private Texture2D texBadgeAttack;
        [System.NonSerialized] private Texture2D texBadgeFlee;
        [System.NonSerialized] private Texture2D texBadgeDead;

        [System.NonSerialized] private Texture2D texBadgeBandage;
        [System.NonSerialized] private Texture2D texBadgeFirstAid;
        [System.NonSerialized] private Texture2D texBadgeMedkit;
        [System.NonSerialized] private Texture2D texWeightFill;
        [System.NonSerialized] private Texture2D texExtractionFill;
        [System.NonSerialized] private Texture2D texExtractionGlow;
        [System.NonSerialized] private Texture2D texVictoryBackdrop;

        // Minecraft Hotbar Textures
        [System.NonSerialized] private Texture2D texMinecraftSlot;
        [System.NonSerialized] private Texture2D texMinecraftSlotGold;
        [System.NonSerialized] private Texture2D texMinecraftPanelBg;
        [System.NonSerialized] private Texture2D texIconBandage;
        [System.NonSerialized] private Texture2D texIconFirstAid;
        [System.NonSerialized] private Texture2D texIconMedkit;

        // Modal & Button Textures
        [System.NonSerialized] private Texture2D texModalBackdropDark;
        [System.NonSerialized] private Texture2D texModalBg;
        [System.NonSerialized] private Texture2D texModalBorderGold;
        [System.NonSerialized] private Texture2D texModalBorderCyan;
        [System.NonSerialized] private Texture2D texBtnNormal;
        [System.NonSerialized] private Texture2D texBtnHover;
        [System.NonSerialized] private Texture2D texBtnGreenNormal;
        [System.NonSerialized] private Texture2D texBtnGreenHover;

        [System.NonSerialized] private UTS.Inventory.PlayerInventory playerInventory;

        [System.NonSerialized] private GUIStyle textStyleCenter;
        [System.NonSerialized] private GUIStyle badgeTextStyle;
        [System.NonSerialized] private GUIStyle headerTextStyle;
        [System.NonSerialized] private GUIStyle victoryTitleStyle;
        [System.NonSerialized] private GUIStyle victorySubStyle;
        [System.NonSerialized] private GUIStyle victoryStatStyle;

        [System.NonSerialized] private GUIStyle slotKeyStyle;
        [System.NonSerialized] private GUIStyle slotCountStyle;
        [System.NonSerialized] private GUIStyle slotNameStyle;
        [System.NonSerialized] private GUIStyle slotFeedbackStyle;
        [System.NonSerialized] private GUIStyle hintTextStyle;

        [System.NonSerialized] private GUIStyle btnPrimaryStyle;
        [System.NonSerialized] private GUIStyle btnSecondaryStyle;
        [System.NonSerialized] private GUIStyle modalTitleStyle;
        [System.NonSerialized] private GUIStyle modalSubtitleStyle;
        [System.NonSerialized] private GUIStyle rankBadgeStyle;
        [System.NonSerialized] private GUIStyle statRowStyle;
        [System.NonSerialized] private GUIStyle statValStyle;
        [System.NonSerialized] private GUIStyle toastTextStyle;

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
            var zone = UTS.Environment.GreenSmokeExtractionZone.Instance;
            bool isExtracted = zone != null && zone.IsExtracted;
            bool isPaused = cameraController != null && cameraController.IsPaused;

            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.02f;

            if (Application.isPlaying)
            {
                // Hitung waktu bermain hanya jika game aktif (tidak pause & belum selesai ekstraksi)
                if (!isPaused && !isExtracted)
                {
                    elapsedPlaytime += dt;
                }

                // Kunci statistik akhir begitu ekstraksi berhasil dicapai
                if (isExtracted && !extractionRecorded)
                {
                    RecordExtractionCompletion();
                }

                // Kontrol gameplay (Inventory Hotbar & Item Use) hanya saat aktif bermain
                if (!isPaused && !isExtracted)
                {
                    if (WasInventoryTogglePressed())
                    {
                        isInventoryOpen = !isInventoryOpen;
                    }

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
            }

            // Animasi slide-up / slide-down inventaris
            float targetProgress = (isInventoryOpen && !isPaused && !isExtracted) ? 1f : 0f;
            inventoryAnimProgress = Mathf.MoveTowards(inventoryAnimProgress, targetProgress, dt * 8f);

            // Timer feedback
            if (itemFeedbackTimer > 0f) itemFeedbackTimer -= dt;
            if (mainMenuFeedbackTimer > 0f) mainMenuFeedbackTimer -= dt;
        }

        private void RecordExtractionCompletion()
        {
            extractionRecorded = true;
            finalPlaytime = elapsedPlaytime;

            // Hentikan waktu & buka kursor mouse untuk modal dialog kemenangan
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Kalkulasi Skor Akhir Dinamis: Loot + Speed Bonus + Survival Clear
            int lootVal = playerInventory != null ? playerInventory.TotalValue : 0;
            const float parTime = 300f; // Target 5 Menit
            finalSpeedBonus = Mathf.Max(0, Mathf.RoundToInt((parTime - finalPlaytime) * 10f));
            int survivalClearBonus = 1000;
            finalScore = lootVal + finalSpeedBonus + survivalClearBonus;

            // Penentuan Pangkat / Rank Survivor
            if (finalScore >= 3500)
            {
                finalRank = "RANK S  ★ ELITE SURVIVOR ★";
                finalRankColor = new Color(1f, 0.84f, 0.0f); // Gold
            }
            else if (finalScore >= 2500)
            {
                finalRank = "RANK A  ★ VETERAN SURVIVOR ★";
                finalRankColor = new Color(0.2f, 1.0f, 0.45f); // Emerald Green
            }
            else if (finalScore >= 1500)
            {
                finalRank = "RANK B  ★ SURVIVOR ★";
                finalRankColor = new Color(0.25f, 0.75f, 1.0f); // Cyan
            }
            else
            {
                finalRank = "RANK C  ★ ROOKIE SCAVENGER ★";
                finalRankColor = new Color(0.75f, 0.75f, 0.82f); // Silver
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

            if (cameraController == null)
            {
                cameraController = FindAnyObjectByType<GTACameraController>();
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
            texBorder = CreateSolidTexture(new Color(0.08f, 0.08f, 0.12f, 0.95f));
            texTrackDark = CreateSolidTexture(new Color(0.18f, 0.18f, 0.22f, 0.85f));
            texHpFill = CreateSolidTexture(new Color(0.92f, 0.18f, 0.22f, 1f));
            texStaminaFill = CreateSolidTexture(new Color(0.96f, 0.65f, 0.12f, 1f));
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

            // Modal & Button Textures
            texModalBackdropDark = CreateSolidTexture(new Color(0f, 0f, 0f, 0.78f));
            texModalBg = CreateSolidTexture(new Color(0.10f, 0.11f, 0.14f, 0.98f));
            texModalBorderGold = CreateSolidTexture(new Color(1f, 0.82f, 0.22f, 1f));
            texModalBorderCyan = CreateSolidTexture(new Color(0.25f, 0.72f, 0.95f, 1f));
            texBtnNormal = CreateSolidTexture(new Color(0.20f, 0.22f, 0.26f, 0.95f));
            texBtnHover = CreateSolidTexture(new Color(0.30f, 0.35f, 0.42f, 1f));
            texBtnGreenNormal = CreateSolidTexture(new Color(0.16f, 0.58f, 0.28f, 0.95f));
            texBtnGreenHover = CreateSolidTexture(new Color(0.22f, 0.78f, 0.38f, 1f));
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.hideFlags = HideFlags.DontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateMinecraftSlotTexture(bool isGold)
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
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
            tex.hideFlags = HideFlags.DontSave;
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
            tex.hideFlags = HideFlags.DontSave;
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

            for (int y = 13; y <= 16; y++)
            {
                tex.SetPixel(5, y, metal);
                tex.SetPixel(6, y, metal);
                tex.SetPixel(25, y, metal);
                tex.SetPixel(26, y, metal);
            }

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
            tex.hideFlags = HideFlags.DontSave;
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

            int[] cx = { 4, 5, 6, 25, 26, 27 };
            int[] cy = { 5, 6, 7, 21, 22, 23 };
            foreach (int px in cx)
            {
                foreach (int py in cy)
                {
                    tex.SetPixel(px, py, goldAccent);
                }
            }

            for (int y = 10; y <= 18; y++)
            {
                for (int x = 11; x <= 20; x++)
                {
                    tex.SetPixel(x, y, goldAccent);
                }
            }

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

        private void EnsureGUIResources()
        {
            if (texBorder == null || texModalBorderGold == null || texModalBg == null || texBtnNormal == null || texBorder.width == 0)
            {
                InitTextures();
            }

            if (textStyleCenter == null || victoryTitleStyle == null || statRowStyle == null || btnPrimaryStyle == null || victoryTitleStyle.normal.textColor.a < 0.5f)
            {
                InitStyles();
            }
        }

        private void InitStyles()
        {
            textStyleCenter = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            textStyleCenter.normal.textColor = Color.white;

            badgeTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            badgeTextStyle.normal.textColor = Color.white;

            headerTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 0.9f);

            slotKeyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            slotKeyStyle.normal.textColor = new Color(1.0f, 0.85f, 0.25f, 1f);

            slotCountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight
            };
            slotCountStyle.normal.textColor = Color.white;

            slotNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            slotNameStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f, 0.95f);

            slotFeedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            slotFeedbackStyle.normal.textColor = new Color(0.2f, 1.0f, 0.45f, 1f);

            hintTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            hintTextStyle.normal.textColor = new Color(0.75f, 0.75f, 0.82f, 0.85f);

            victoryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            victoryTitleStyle.normal.textColor = new Color(0.2f, 1.0f, 0.45f, 1f);

            victorySubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            victorySubStyle.normal.textColor = new Color(0.9f, 0.95f, 0.9f, 0.95f);

            victoryStatStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            victoryStatStyle.normal.textColor = Color.white;

            // Modal & Button Styles
            btnPrimaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnPrimaryStyle.normal.textColor = Color.white;

            btnSecondaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnSecondaryStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f, 1f);

            modalTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            modalTitleStyle.normal.textColor = new Color(0.25f, 0.75f, 1.0f);

            modalSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            modalSubtitleStyle.normal.textColor = new Color(0.78f, 0.82f, 0.88f, 0.9f);

            rankBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            rankBadgeStyle.normal.textColor = Color.white;

            statRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            statRowStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 1f);

            statValStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            statValStyle.normal.textColor = Color.white;

            toastTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            toastTextStyle.normal.textColor = new Color(1.0f, 0.65f, 0.2f, 1f);
        }

        private void OnGUI()
        {
            EnsureGUIResources();

            if (playerController == null || enemyFSM == null || cameraController == null)
            {
                FindReferences();
            }

            var zone = UTS.Environment.GreenSmokeExtractionZone.Instance;
            bool isExtracted = zone != null && zone.IsExtracted;
            bool isPaused = cameraController != null && cameraController.IsPaused;

            // 1. Jika game selesai (Ekstraksi Berhasil), gambar Modal Dialog Kemenangan Meriah
            if (isExtracted)
            {
                DrawFestiveExtractionModal();
                return;
            }

            // 2. Jika game dijeda (Pause), gambar Modal Dialog Pause Menu
            if (isPaused)
            {
                DrawPauseMenuModal();
                return;
            }

            // 3. Gameplay HUD normal (Hanya Repaint)
            if (Event.current.type != EventType.Repaint) return;

            DrawPlayerSoulKnightHUD();

            if (showEnemyOverhead)
            {
                DrawEnemyOverheadBar();
            }

            DrawMinecraftBottomInventory();
            DrawAimReticle();
            DrawEvacuationProgressBar();
        }

        private void DrawPlayerSoulKnightHUD()
        {
            float totalH = (playerBarHeight * 2f) + playerBarSpacing + 44f;
            float startX = playerHudOffset.x;
            float startY = Screen.height - playerHudOffset.y - totalH;

            GUI.Label(new Rect(startX, startY, playerBarWidth, 18f), "PLAYER STATUS", headerTextStyle);

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

            bool isFlashlightOn = playerController != null && playerController.Flashlight != null && playerController.Flashlight.IsOn;
            bool isBelt = playerController != null && playerController.Flashlight != null && playerController.Flashlight.IsAttachedToBelt;
            string flMode = isBelt ? "BELT" : "HAND";
            string flStatus = isFlashlightOn ? $"[F] SENTER: MENYALA ({flMode})" : "[F] SENTER: PADAM";
            Rect flRect = new Rect(startX, staminaBarRect.yMax + 5f, playerBarWidth, 18f);
            GUI.Label(flRect, flStatus, isFlashlightOn ? badgeTextStyle : headerTextStyle);
        }

        private void DrawEnemyOverheadBar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 playerPos = playerController != null ? playerController.transform.position : cam.transform.position;

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

                    return;
                }
            }

            Vector3 screenPos = cam.WorldToScreenPoint(worldHead);
            if (screenPos.z <= 0.1f) return;

            float screenX = screenPos.x;
            float screenY = Screen.height - screenPos.y;

            float barW = enemyBarWidth;
            float barH = enemyBarHeight;
            float badgeW = 120f;
            float badgeH = 18f;

            float barStartX = screenX - (barW * 0.5f);
            float barStartY = screenY - (barH * 0.5f);
            float badgeStartX = screenX - (badgeW * 0.5f);
            float badgeStartY = barStartY - badgeH - 3f;

            EnemyState state = fsm.CurrentState;
            float curHp = fsm.CurrentHealth;
            float maxHp = fsm.MaxHealth;
            float hpRatio = maxHp > 0f ? curHp / maxHp : 0f;

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

            Rect badgeBorderRect = new Rect(badgeStartX, badgeStartY, badgeW, badgeH);
            GUI.DrawTexture(badgeBorderRect, texBorder);
            Rect badgeInnerRect = new Rect(badgeStartX + 2, badgeStartY + 2, badgeW - 4, badgeH - 4);
            GUI.DrawTexture(badgeInnerRect, badgeTex);

            string badgeText = state == EnemyState.Patrol ? "[WANDER]" : $"[{state.ToString().ToUpper()}]";
            GUI.Label(badgeBorderRect, badgeText, badgeTextStyle);

            Rect enemyBarRect = new Rect(barStartX, barStartY, barW, barH);
            string enemyHpText = state == EnemyState.Dead ? "DEAD (0 / 100)" : $"HP: {curHp:F0} / {maxHp:F0}";
            DrawSoulKnightBar(enemyBarRect, hpRatio, texTrackDark, fillTex, enemyHpText);
        }

        private void DrawSoulKnightBar(Rect rect, float fillRatio, Texture2D trackTex, Texture2D fillTex, string label)
        {
            GUI.DrawTexture(rect, texBorder);

            Rect trackRect = new Rect(rect.x + 2.5f, rect.y + 2.5f, rect.width - 5f, rect.height - 5f);
            GUI.DrawTexture(trackRect, trackTex);

            float fillWidth = trackRect.width * Mathf.Clamp01(fillRatio);
            if (fillWidth > 0.5f)
            {
                Rect fillRect = new Rect(trackRect.x, trackRect.y, fillWidth, trackRect.height);
                GUI.DrawTexture(fillRect, fillTex);
            }

            if (!string.IsNullOrEmpty(label))
            {
                GUI.Label(rect, label, textStyleCenter);
            }
        }

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
                GUI.Label(hintRect, hText, badgeTextStyle);
                return;
            }

            float panelW = 310f;
            float panelH = 150f;
            float panelX = (Screen.width - panelW) * 0.5f;

            float t = Mathf.SmoothStep(0f, 1f, inventoryAnimProgress);
            float hiddenY = Screen.height + 15f;
            float openY = Screen.height - panelH - 18f;
            float panelY = Mathf.Lerp(hiddenY, openY, t);

            Rect panelRect = new Rect(panelX, panelY, panelW, panelH);

            if (itemFeedbackTimer > 0f && !string.IsNullOrEmpty(itemFeedbackText))
            {
                float fbW = 240f;
                float fbH = 22f;
                float fbX = panelX + (panelW - fbW) * 0.5f;
                float fbY = panelY - fbH - 6f;
                Rect fbRect = new Rect(fbX, fbY, fbW, fbH);

                GUI.DrawTexture(fbRect, texBorder);
                GUI.DrawTexture(new Rect(fbRect.x + 1.5f, fbRect.y + 1.5f, fbRect.width - 3f, fbRect.height - 3f), texTrackDark);
                GUI.Label(fbRect, itemFeedbackText, slotFeedbackStyle);
            }

            GUI.DrawTexture(panelRect, texBorder);
            Rect panelInnerRect = new Rect(panelRect.x + 2f, panelRect.y + 2f, panelRect.width - 4f, panelRect.height - 4f);
            GUI.DrawTexture(panelInnerRect, texMinecraftPanelBg);

            float curW = playerInventory != null ? playerInventory.CurrentWeight : 0f;
            float maxW = playerInventory != null ? playerInventory.MaxWeight : 40f;
            float wRatio = playerInventory != null ? playerInventory.WeightRatio : 0f;

            Rect titleRect = new Rect(panelInnerRect.x + 10f, panelInnerRect.y + 6f, 130f, 16f);
            GUI.Label(titleRect, "INVENTORY  [E]", headerTextStyle);

            string weightText = $"BEBAN: {curW:F1} / {maxW:F1} KG";
            GUIStyle weightStyle = new GUIStyle(headerTextStyle) { alignment = TextAnchor.MiddleRight };
            Rect weightLabelRect = new Rect(panelInnerRect.xMax - 140f, panelInnerRect.y + 6f, 130f, 16f);
            GUI.Label(weightLabelRect, weightText, weightStyle);

            Rect weightBarRect = new Rect(panelInnerRect.x + 10f, titleRect.yMax + 4f, panelInnerRect.width - 20f, 6f);
            DrawSoulKnightBar(weightBarRect, wRatio, texBorder, texWeightFill, "");

            float slotSize = 74f;
            float slotGap = 12f;
            float totalSlotsW = (3f * slotSize) + (2f * slotGap);
            float startSlotX = panelRect.x + (panelW - totalSlotsW) * 0.5f;
            float slotY = weightBarRect.yMax + 8f;

            int bCount = playerInventory != null ? playerInventory.BandageCount : 0;
            Rect slot1Rect = new Rect(startSlotX, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot1Rect, 0, "[1]", texIconBandage, bCount, "BANDAGE", "+15 HP");

            int fCount = playerInventory != null ? playerInventory.FirstAidCount : 0;
            Rect slot2Rect = new Rect(startSlotX + slotSize + slotGap, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot2Rect, 1, "[2]", texIconFirstAid, fCount, "FIRST AID", "+35 HP");

            int mCount = playerInventory != null ? playerInventory.MedkitCount : 0;
            Rect slot3Rect = new Rect(startSlotX + (slotSize + slotGap) * 2f, slotY, slotSize, slotSize);
            DrawMinecraftSlot(slot3Rect, 2, "[3]", texIconMedkit, mCount, "MEDKIT", "+70 HP");

            string hint = "Tekan [1] Bandage  |  [2] First Aid  |  [3] Medkit";
            Rect footerRect = new Rect(panelInnerRect.x, panelInnerRect.yMax - 18f, panelInnerRect.width, 14f);
            GUI.Label(footerRect, hint, hintTextStyle);
        }

        private void DrawMinecraftSlot(Rect rect, int slotIndex, string keyBadge, Texture2D iconTex, int count, string itemName, string healLabel)
        {
            Texture2D slotTex = count > 0 ? texMinecraftSlotGold : texMinecraftSlot;
            GUI.DrawTexture(rect, slotTex);

            float iconSize = 44f;
            float iconX = rect.x + (rect.width - iconSize) * 0.5f;
            float iconY = rect.y + 8f;
            Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

            if (iconTex != null)
            {
                Color oldColor = GUI.color;
                if (count <= 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.35f);
                }
                GUI.DrawTexture(iconRect, iconTex);
                GUI.color = oldColor;
            }

            Rect badgeRect = new Rect(rect.x + 4f, rect.y + 4f, 20f, 14f);
            GUI.DrawTexture(badgeRect, texBorder);
            GUI.Label(badgeRect, keyBadge, slotKeyStyle);

            string countText = $"x{count}";
            Rect countRect = new Rect(rect.x, rect.y, rect.width - 6f, rect.height - 4f);

            Color oldTextColor = slotCountStyle.normal.textColor;
            slotCountStyle.normal.textColor = count > 0 ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.7f);
            GUI.Label(countRect, countText, slotCountStyle);
            slotCountStyle.normal.textColor = oldTextColor;

            string subText = $"{itemName} ({healLabel})";
            Rect nameRect = new Rect(rect.x - 12f, rect.yMax + 2f, rect.width + 24f, 14f);
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
            if (kb != null)
            {
                // Jika inventaris terbuka dan pengguna menekan Escape, tutup inventaris
                if (isInventoryOpen && kb.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
                return kb.eKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
            }
            return false;
#else
            if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape)) return true;
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

        /// <summary>
        /// Menggambar progress bar saat bertahan di dalam lingkaran evakuasi asap hijau.
        /// </summary>
        private void DrawEvacuationProgressBar()
        {
            var zone = UTS.Environment.GreenSmokeExtractionZone.Instance;
            if (zone == null) return;

            if (zone.IsPlayerInside && !zone.IsExtracted)
            {
                float barW = 340f;
                float barH = 26f;
                float barX = (Screen.width - barW) * 0.5f;
                float barY = 28f;

                Rect glowRect = new Rect(barX - 10f, barY - 18f, barW + 20f, barH + 34f);
                GUI.DrawTexture(glowRect, texExtractionGlow);

                string titleText = "EVAKUASI ZONA ASAP HIJAU";
                Rect titleRect = new Rect(barX, barY - 18f, barW, 16f);
                GUI.Label(titleRect, titleText, badgeTextStyle);

                Rect progRect = new Rect(barX, barY, barW, barH);
                string progText = $"BERTAHAN: {zone.CurrentHoldTime:F1}s / {zone.RequiredHoldTime:F0}s ({(zone.ProgressRatio * 100f):F0}%)";
                DrawSoulKnightBar(progRect, zone.ProgressRatio, texTrackDark, texExtractionFill, progText);

                string noteText = "Tetap berada di dalam lingkaran asap hijau!";
                Rect noteRect = new Rect(barX, progRect.yMax + 3f, barW, 14f);
                GUI.Label(noteRect, noteText, badgeTextStyle);
            }
        }

        /// <summary>
        /// Pop-up Dialog Pause Menu Interaktif (Tombol RESUME, RESTART, MAIN MENU).
        /// </summary>
        private void DrawPauseMenuModal()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texModalBackdropDark);

            float modalW = 390f;
            float modalH = 310f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            Rect outerRect = new Rect(modalX, modalY, modalW, modalH);
            Rect innerRect = new Rect(modalX + 3f, modalY + 3f, modalW - 6f, modalH - 6f);

            GUI.DrawTexture(outerRect, texModalBorderCyan);
            GUI.DrawTexture(innerRect, texModalBg);

            float curY = innerRect.y + 18f;
            Rect titleRect = new Rect(innerRect.x, curY, innerRect.width, 28f);
            GUI.Label(titleRect, "GAME PAUSED", modalTitleStyle);

            curY += 28f;
            Rect subRect = new Rect(innerRect.x, curY, innerRect.width, 18f);
            GUI.Label(subRect, "Tekan [Esc] atau [P] untuk Lanjut Bermain", modalSubtitleStyle);

            curY += 22f;
            GUI.DrawTexture(new Rect(innerRect.x + 25f, curY, innerRect.width - 50f, 2f), texModalBorderCyan);

            curY += 18f;
            float btnW = innerRect.width - 60f;
            float btnH = 40f;
            float btnX = innerRect.x + 30f;
            float spacing = 10f;

            // 1. Tombol RESUME
            Rect resumeRect = new Rect(btnX, curY, btnW, btnH);
            if (DrawStyledButton(resumeRect, "RESUME GAME", true))
            {
                if (cameraController != null)
                {
                    cameraController.SetPause(false);
                }
                else
                {
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // 2. Tombol RESTART
            curY += btnH + spacing;
            Rect restartRect = new Rect(btnX, curY, btnW, btnH);
            if (DrawStyledButton(restartRect, "RESTART LEVEL", false))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            // 3. Tombol MAIN MENU
            curY += btnH + spacing;
            Rect menuRect = new Rect(btnX, curY, btnW, btnH);
            if (DrawStyledButton(menuRect, "MAIN MENU", false))
            {
                TriggerMainMenu();
            }

            // Feedback Toast jika Main Menu belum dibuat
            if (mainMenuFeedbackTimer > 0f)
            {
                Rect toastRect = new Rect(innerRect.x + 10f, innerRect.yMax - 22f, innerRect.width - 20f, 18f);
                GUI.Label(toastRect, mainMenuFeedbackText, toastTextStyle);
            }
        }

        /// <summary>
        /// Pop-up Dialog Kemenangan Meriah (Playtime, Skor Dinamis, Rank Badge, Play Again, Main Menu).
        /// </summary>
        private void DrawFestiveExtractionModal()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texModalBackdropDark);

            float modalW = 540f;
            float modalH = 440f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            Rect outerRect = new Rect(modalX, modalY, modalW, modalH);
            Rect innerRect = new Rect(modalX + 3.5f, modalY + 3.5f, modalW - 7f, modalH - 7f);

            // Glowing Gold Border
            GUI.DrawTexture(outerRect, texModalBorderGold);
            GUI.DrawTexture(innerRect, texModalBg);

            float curY = innerRect.y + 16f;
            Rect titleRect = new Rect(innerRect.x, curY, innerRect.width, 30f);
            GUI.Label(titleRect, "MISSION ACCOMPLISHED!", victoryTitleStyle);

            curY += 30f;
            Rect subRect = new Rect(innerRect.x, curY, innerRect.width, 18f);
            GUI.Label(subRect, "Helikopter penyelamat telah berhasil mengevakuasimu!", victorySubStyle);

            // Rank Badge Box
            curY += 22f;
            float rankW = innerRect.width - 60f;
            float rankH = 34f;
            float rankX = innerRect.x + 30f;
            Rect rankRect = new Rect(rankX, curY, rankW, rankH);
            GUI.DrawTexture(rankRect, texBorder);
            GUI.DrawTexture(new Rect(rankRect.x + 1.5f, rankRect.y + 1.5f, rankRect.width - 3f, rankRect.height - 3f), texTrackDark);

            GUIStyle rankStyle = new GUIStyle(rankBadgeStyle) { normal = { textColor = finalRankColor } };
            GUI.Label(rankRect, finalRank, rankStyle);

            curY += rankH + 10f;
            GUI.DrawTexture(new Rect(innerRect.x + 25f, curY, innerRect.width - 50f, 2f), texExtractionFill);

            // Stat Breakdown
            curY += 10f;
            float statX = innerRect.x + 35f;
            float statW = innerRect.width - 70f;
            float lineH = 22f;

            // 1. Playtime
            int minutes = Mathf.FloorToInt(finalPlaytime / 60f);
            int seconds = Mathf.FloorToInt(finalPlaytime % 60f);
            string timeStr = $"{minutes:00}:{seconds:00}";
            string timeBonusStr = finalSpeedBonus > 0 ? $"+{finalSpeedBonus} PTS Speed" : "0 PTS";
            DrawStatRow(new Rect(statX, curY, statW, lineH), "WAKTU BERMAIN (PLAYTIME)", timeStr, timeBonusStr);

            // 2. Looting
            curY += lineH + 3f;
            int lootVal = playerInventory != null ? playerInventory.TotalValue : 0;
            int bCount = playerInventory != null ? playerInventory.BandageCount : 0;
            int fCount = playerInventory != null ? playerInventory.FirstAidCount : 0;
            int mCount = playerInventory != null ? playerInventory.MedkitCount : 0;
            string lootDetail = $"{bCount} Bandage | {fCount} First Aid | {mCount} Medkit";
            DrawStatRow(new Rect(statX, curY, statW, lineH), "TOTAL HASIL LOOTING", $"{lootVal} PTS", lootDetail);

            // 3. Survival Clear Bonus
            curY += lineH + 3f;
            DrawStatRow(new Rect(statX, curY, statW, lineH), "SURVIVAL EXTRACTION BONUS", "+1,000 PTS", "Evakuasi Sukses");

            curY += lineH + 6f;
            GUI.DrawTexture(new Rect(innerRect.x + 25f, curY, innerRect.width - 50f, 1.5f), texBorder);

            // 4. TOTAL SKOR
            curY += 8f;
            Rect totalLabelRect = new Rect(statX, curY, 200f, 24f);
            GUI.Label(totalLabelRect, "TOTAL SKOR AKHIR", victoryStatStyle);

            string finalScoreStr = $"{finalScore:N0} PTS";
            Rect totalValRect = new Rect(statX + statW - 200f, curY, 200f, 24f);
            GUIStyle scoreValStyle = new GUIStyle(victoryTitleStyle) { fontSize = 19, alignment = TextAnchor.MiddleRight, normal = { textColor = finalRankColor } };
            GUI.Label(totalValRect, finalScoreStr, scoreValStyle);

            // Buttons: PLAY AGAIN & MAIN MENU side-by-side
            curY += 34f;
            float btnW = (innerRect.width - 70f) * 0.5f;
            float btnH = 42f;
            float btn1X = innerRect.x + 30f;
            float btn2X = btn1X + btnW + 10f;

            Rect playAgainRect = new Rect(btn1X, curY, btnW, btnH);
            if (DrawStyledButton(playAgainRect, "PLAY AGAIN", true))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            Rect mainMenuRect = new Rect(btn2X, curY, btnW, btnH);
            if (DrawStyledButton(mainMenuRect, "MAIN MENU", false))
            {
                TriggerMainMenu();
            }

            if (mainMenuFeedbackTimer > 0f)
            {
                Rect toastRect = new Rect(innerRect.x + 10f, innerRect.yMax - 20f, innerRect.width - 20f, 18f);
                GUI.Label(toastRect, mainMenuFeedbackText, toastTextStyle);
            }
        }

        private void DrawStatRow(Rect rect, string label, string mainVal, string subVal)
        {
            float leftW = rect.width * 0.46f;
            float rightW = rect.width * 0.54f;

            Rect lblRect = new Rect(rect.x, rect.y, leftW, rect.height);
            GUI.Label(lblRect, label, statRowStyle);

            string combinedRight = $"{mainVal}  ({subVal})";
            Rect valRect = new Rect(rect.x + leftW, rect.y, rightW, rect.height);
            GUI.Label(valRect, combinedRight, statValStyle);
        }

        private bool DrawStyledButton(Rect rect, string label, bool isPrimary)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHovered = rect.Contains(mousePos);

            Texture2D bgTex = isPrimary
                ? (isHovered ? texBtnGreenHover : texBtnGreenNormal)
                : (isHovered ? texBtnHover : texBtnNormal);

            GUI.DrawTexture(rect, texBorder);
            GUI.DrawTexture(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width - 3f, rect.height - 3f), bgTex);

            GUIStyle style = isPrimary ? btnPrimaryStyle : btnSecondaryStyle;
            GUI.Label(rect, label, style);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && isHovered)
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private void TriggerMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                mainMenuFeedbackTimer = 3.5f;
                mainMenuFeedbackText = "Scene [MainMenu] belum dibuat oleh rekan tim.";
                Debug.LogWarning("[FSMGameHUD] Tombol Main Menu: Scene 'MainMenu' belum terdaftar di Build Settings. (Placeholder)");
            }
        }

        private void DrawAimReticle()
        {
            if (playerController == null || !playerController.IsAiming) return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            float dotSize = 4f;
            Rect dotRect = new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize);
            GUI.DrawTexture(dotRect, texBorder);
            GUI.DrawTexture(new Rect(dotRect.x + 0.5f, dotRect.y + 0.5f, dotRect.width - 1f, dotRect.height - 1f), texBadgeChase);

            float lineLen = 10f;
            float lineGap = 6f;
            float lineThick = 2f;

            GUI.DrawTexture(new Rect(centerX - lineGap - lineLen, centerY - lineThick * 0.5f, lineLen, lineThick), texBadgeChase);
            GUI.DrawTexture(new Rect(centerX + lineGap, centerY - lineThick * 0.5f, lineLen, lineThick), texBadgeChase);
            GUI.DrawTexture(new Rect(centerX - lineThick * 0.5f, centerY - lineGap - lineLen, lineThick, lineLen), texBadgeChase);
            GUI.DrawTexture(new Rect(centerX - lineThick * 0.5f, centerY + lineGap, lineThick, lineLen), texBadgeChase);
        }
    }
}
