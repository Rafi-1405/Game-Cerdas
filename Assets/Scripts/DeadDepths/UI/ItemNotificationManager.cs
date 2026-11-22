using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.UI
{
    /// <summary>
    /// Displays and stacks item-gained notifications above the player's head.
    /// ASD-STE100: This module shows item notifications above the player.
    /// Follows Section 3.3 of the specification.
    /// </summary>
    public class ItemNotificationManager : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public Vector3 BaseOverheadOffset = new Vector3(0f, 2.5f, 0f);
            public float RowHeight = 24f;
            public float BoxWidth = 160f;
            public int MaxVisibleNotifications = 3;
            public float DisplayDuration = 3.5f;
            public float FadeDuration = 0.5f;
            public Color BackgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.82f);
            public Color BorderColor = new Color(0.2f, 0.75f, 0.4f, 0.9f);
            public Color TextColor = new Color(0.95f, 0.98f, 1.0f);
        }
        #endregion

        [SerializeField] private Config notifyConfig = new Config();
        public Config NotifyConfig => notifyConfig;

        public static ItemNotificationManager Instance { get; private set; }
        public static event Action<string, int> OnNotificationPosted;

        private class ActiveNotification
        {
            public string ItemName;
            public int Quantity;
            public float TimeRemaining;
            public float CurrentYOffset;
            public float TargetYOffset;
            public float Alpha;
        }

        [NonSerialized] private readonly List<ActiveNotification> activeList = new List<ActiveNotification>();
        [NonSerialized] private Transform playerTransform;
        [NonSerialized] private Camera activeCamera;

        [NonSerialized] private Texture2D texBg;
        [NonSerialized] private Texture2D texBorder;
        [NonSerialized] private GUIStyle notifyStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            activeList.Clear();
            playerTransform = null;
            activeCamera = null;
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
            ResetStyles();
        }

        private void Start()
        {
            activeCamera = Camera.main;
            FindPlayer();
        }

        private void InitializeTextures()
        {
            DestroyTextures();
            texBg = CreateSolidTexture(notifyConfig.BackgroundColor);
            texBorder = CreateSolidTexture(notifyConfig.BorderColor);
        }

        private void EnsureTextures()
        {
            if (texBg == null) texBg = CreateSolidTexture(notifyConfig.BackgroundColor);
            if (texBorder == null) texBorder = CreateSolidTexture(notifyConfig.BorderColor);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels(new Color[] { color, color, color, color });
            tex.Apply();
            return tex;
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref texBg);
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

        private void ResetStyles()
        {
            notifyStyle = null;
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        /// <summary>
        /// Posts or merges an item-gained notification.
        /// </summary>
        public void PostNotification(string itemName, int quantity)
        {
            OnNotificationPosted?.Invoke(itemName, quantity);
            // Check if already active in list -> merge quantity and refresh timer
            for (int i = 0; i < activeList.Count; i++)
            {
                if (string.Equals(activeList[i].ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    activeList[i].Quantity += quantity;
                    activeList[i].TimeRemaining = notifyConfig.DisplayDuration;
                    activeList[i].Alpha = 1f;
                    return;
                }
            }

            // If at capacity, remove oldest
            if (activeList.Count >= notifyConfig.MaxVisibleNotifications)
            {
                activeList.RemoveAt(0);
            }

            // Create new notification entering at the bottom
            ActiveNotification notif = new ActiveNotification
            {
                ItemName = itemName,
                Quantity = quantity,
                TimeRemaining = notifyConfig.DisplayDuration,
                CurrentYOffset = 0f,
                TargetYOffset = 0f,
                Alpha = 1f
            };

            activeList.Add(notif);
            RecalculateTargetOffsets();
        }

        private void RecalculateTargetOffsets()
        {
            // Bottom of stack is newest (index = activeList.Count - 1)
            for (int i = 0; i < activeList.Count; i++)
            {
                int stackPos = activeList.Count - 1 - i;
                activeList[i].TargetYOffset = stackPos * (notifyConfig.RowHeight + 4f);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                var n = activeList[i];
                n.TimeRemaining -= dt;

                // Smoothly shift upward
                n.CurrentYOffset = Mathf.Lerp(n.CurrentYOffset, n.TargetYOffset, dt * 10f);

                // Fade out at end
                if (n.TimeRemaining <= notifyConfig.FadeDuration)
                {
                    n.Alpha = Mathf.Clamp01(n.TimeRemaining / notifyConfig.FadeDuration);
                }

                if (n.TimeRemaining <= 0f)
                {
                    activeList.RemoveAt(i);
                    RecalculateTargetOffsets();
                }
            }
        }

        private void OnGUI()
        {
            if (DeadDepthsUIRoot.IsCanvasActive) return;
            if (activeList.Count == 0) return;
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }
            if (activeCamera == null) activeCamera = Camera.main;
            if (activeCamera == null) return;

            EnsureTextures();

            if (notifyStyle == null)
            {
                notifyStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                notifyStyle.normal.textColor = notifyConfig.TextColor;
            }

            Vector3 worldPos = playerTransform.position + notifyConfig.BaseOverheadOffset;
            Vector3 screenPos = activeCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) return;

            float w = notifyConfig.BoxWidth;
            float h = notifyConfig.RowHeight;
            float centerX = screenPos.x;
            float baseY = Screen.height - screenPos.y;

            Color oldColor = GUI.color;

            foreach (var notif in activeList)
            {
                GUI.color = new Color(1f, 1f, 1f, notif.Alpha);

                float x = centerX - (w * 0.5f);
                float y = baseY - notif.CurrentYOffset - h;

                // Draw background and border
                GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), texBorder);
                GUI.DrawTexture(new Rect(x, y, w, h), texBg);

                string label = $"{notif.ItemName} +{notif.Quantity}";
                GUI.Label(new Rect(x, y, w, h), label, notifyStyle);
            }

            GUI.color = oldColor;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            activeList.Clear();
            ResetStyles();
            DestroyTextures();
        }
    }
}
