using System;
using System.Collections.Generic;
using UnityEngine;
using DeadDepths.Audio;
using UTS.Audio;

namespace DeadDepths.Interactive
{
    [Serializable]
    public struct CrateLootItem
    {
        public string ItemName;
        public int Quantity;
        public bool IsHealing;

        public CrateLootItem(string name, int qty, bool isHealing = false)
        {
            ItemName = name;
            Quantity = qty;
            IsHealing = isHealing;
        }
    }

    /// <summary>
    /// Represents an interactable supplies crate placed inside structures.
    /// ASD-STE100: This container gives items to the player after opening progress finishes.
    /// Follows Section 3.2 of the specification.
    /// </summary>
    public class SuppliesCrate : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            [Tooltip("Time in seconds to open the crate.")]
            public float OpenDuration = 2.0f;

            [Tooltip("Maximum interaction distance from the player.")]
            public float InteractionDistance = 2.2f;

            [Tooltip("Color of the crate indicator glow.")]
            public Color OutlineColor = new Color(1f, 0.85f, 0.2f, 1f);
        }
        #endregion

        [SerializeField] private Config crateConfig = new Config();
        public Config CrateConfig => crateConfig;

        public bool IsOpened { get; private set; } = false;
        public float CurrentProgress { get; private set; } = 0f;
        public bool IsInteracting { get; private set; } = false;

        private List<CrateLootItem> lootContents = new List<CrateLootItem>();
        private Transform interactingPlayer;

        public event Action<float> OnProgressUpdated;
        public event Action OnInteractionStarted;
        public event Action OnInteractionCancelled;
        public event Action OnCrateOpened;

        public void ConfigureLoot(List<CrateLootItem> items)
        {
            lootContents = items ?? new List<CrateLootItem>();
        }

        public void StartInteraction(Transform player)
        {
            if (IsOpened) return;

            interactingPlayer = player;
            IsInteracting = true;
            CurrentProgress = 0f;

            OnInteractionStarted?.Invoke();
        }

        public void UpdateInteraction(float deltaTime)
        {
            if (!IsInteracting || IsOpened) return;

            // Distance check
            if (interactingPlayer == null ||
                Vector3.Distance(transform.position, interactingPlayer.position) > crateConfig.InteractionDistance)
            {
                CancelInteraction();
                return;
            }

            CurrentProgress += deltaTime;
            float ratio = Mathf.Clamp01(CurrentProgress / Mathf.Max(0.1f, crateConfig.OpenDuration));
            OnProgressUpdated?.Invoke(ratio);

            if (CurrentProgress >= crateConfig.OpenDuration)
            {
                CompleteOpen();
            }
        }

        public void CancelInteraction()
        {
            if (!IsInteracting || IsOpened) return;

            IsInteracting = false;
            CurrentProgress = 0f;
            interactingPlayer = null;
            OnInteractionCancelled?.Invoke();
        }

        private void CompleteOpen()
        {
            IsInteracting = false;
            IsOpened = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(SfxCue.Creak, transform.position);
                AudioManager.Instance.PlayCue(SfxCue.Pickup, transform.position);
            }

            // Animate or tilt lid
            Transform lid = transform.Find("Lid");
            if (lid != null)
            {
                lid.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            }

            OnCrateOpened?.Invoke();

            // Grant items to player via UI notification manager and inventory
            if (lootContents != null && lootContents.Count > 0)
            {
                var notificationMgr = UI.ItemNotificationManager.Instance;
                foreach (var item in lootContents)
                {
                    if (notificationMgr != null)
                    {
                        notificationMgr.PostNotification(item.ItemName, item.Quantity);
                    }
                }
            }
        }
    }
}
