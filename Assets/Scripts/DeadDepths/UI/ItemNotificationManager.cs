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

        private class ActiveNotification
        {
            public string ItemName;
            public int Quantity;
            public float TimeRemaining;
            public float CurrentYOffset;
            public float TargetYOffset;
            public float Alpha;
        }

        private readonly List<ActiveNotification> activeList = new List<ActiveNotification>();
        private Transform playerTransform;
        private Camera activeCamera;

        private Texture2D texBg;
        private Texture2D texBorder;
        private GUIStyle notifyStyle;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeTextures();
        }

        private void Start()
        {
            activeCamera = Camera.main;
            FindPlayer();
        }

        private void InitializeTextures()
        {
            texBg = new Texture2D(2, 2);
            texBg.SetPixels(new Color[] { notifyConfig.BackgroundColor, notifyConfig.BackgroundColor, notifyConfig.BackgroundColor, notifyConfig.BackgroundColor });
            texBg.Apply();

            texBorder = new Texture2D(2, 2);
            texBorder.SetPixels(new Color[] { notifyConfig.BorderColor, notifyConfig.BorderColor, notifyConfig.BorderColor, notifyConfig.BorderColor });
            texBorder.Apply();
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
            if (activeList.Count == 0) return;
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }
            if (activeCamera == null) activeCamera = Camera.main;
            if (activeCamera == null) return;

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
            if (texBg != null) Destroy(texBg);
            if (texBorder != null) Destroy(texBorder);
        }
    }
}
