using System;
using System.Collections.Generic;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using DeadDepths.Player;
using DeadDepths.UI;
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
    /// Represents an interactable supplies crate placed inside structures or clusters.
    /// Grants Supply Intel (for 3/3 extraction), heals injured players or stocks the Quick-Belt,
    /// animates a hinged Lid child, and spawns a transient 3D loot preview mesh.
    /// ASD-STE100: This container gives items to the player after opening progress finishes.
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
            public float InteractionDistance = 2.4f;

            [Tooltip("Color of the crate indicator glow.")]
            public Color OutlineColor = new Color(1f, 0.85f, 0.2f, 1f);

            [Tooltip("Heal amount granted per Medkit.")]
            public float MedkitHealAmount = 40f;

            [Tooltip("Heal amount granted per Bandage.")]
            public float BandageHealAmount = 15f;
        }
        #endregion

        [SerializeField] private Config crateConfig = new Config();
        public Config CrateConfig => crateConfig;

        [SerializeField] private GameObject medkitPreviewPrefab;
        [SerializeField] private GameObject bandagePreviewPrefab;
        [SerializeField] private GameObject firstAidKitPreviewPrefab;

        public bool IsOpened { get; private set; } = false;
        public float CurrentProgress { get; private set; } = 0f;
        public bool IsInteracting { get; private set; } = false;

        private List<CrateLootItem> lootContents = new List<CrateLootItem>();
        public IReadOnlyList<CrateLootItem> LootContents => lootContents;
        private Transform interactingPlayer;

        private Transform lidTransform;
        private bool animateLidOpen = false;
        private float lidAnimT = 0f;

        private Transform lootPopupTransform;
        private float lootPopupTimer = 0f;

        public event Action<float> OnProgressUpdated;
        public event Action OnInteractionStarted;
        public event Action OnInteractionCancelled;
        public event Action OnCrateOpened;

        private void Awake()
        {
            ApplyFallbackDefaults();
            lidTransform = transform.Find("Lid");
        }

        private void OnValidate()
        {
            ApplyFallbackDefaults();
        }

        private void ApplyFallbackDefaults()
        {
            if (crateConfig == null) crateConfig = new Config();
            if (crateConfig.OpenDuration <= 0f) crateConfig.OpenDuration = 2.0f;
            if (crateConfig.InteractionDistance <= 0f) crateConfig.InteractionDistance = 2.4f;
            if (crateConfig.MedkitHealAmount <= 0f) crateConfig.MedkitHealAmount = 40f;
            if (crateConfig.BandageHealAmount <= 0f) crateConfig.BandageHealAmount = 15f;
        }

        public void SetLootPreviewPrefabs(GameObject medkit, GameObject bandage, GameObject firstAidKit)
        {
            medkitPreviewPrefab = medkit;
            bandagePreviewPrefab = bandage;
            firstAidKitPreviewPrefab = firstAidKit;
        }

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

        private void Update()
        {
            if (animateLidOpen && lidTransform != null && lidAnimT < 1f)
            {
                lidAnimT = Mathf.Clamp01(lidAnimT + Time.deltaTime * 4.5f);
                float angle = Mathf.SmoothStep(0f, -65f, lidAnimT);
                lidTransform.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }

            if (lootPopupTransform != null)
            {
                lootPopupTimer += Time.deltaTime;
                lootPopupTransform.position += Vector3.up * (Time.deltaTime * 0.65f);
                lootPopupTransform.Rotate(Vector3.up, 150f * Time.deltaTime, Space.World);
                float scaleFactor = lootPopupTimer < 1.2f ? 1f : Mathf.Clamp01(1f - (lootPopupTimer - 1.2f) / 0.5f);
                lootPopupTransform.localScale = Vector3.one * (0.85f * scaleFactor);
                if (lootPopupTimer >= 1.7f)
                {
                    Destroy(lootPopupTransform.gameObject);
                    lootPopupTransform = null;
                }
            }
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

            if (lidTransform == null) lidTransform = transform.Find("Lid");
            if (lidTransform != null)
            {
                if (Application.isPlaying)
                {
                    animateLidOpen = true;
                    lidAnimT = 0f;
                }
                else
                {
                    lidTransform.localRotation = Quaternion.Euler(-65f, 0f, 0f);
                }
            }

            Spawn3DLootPopup();

            // Visually dim opened crate (body + hinged lid)
            var rends = GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                var pb = new MaterialPropertyBlock();
                foreach (var rend in rends)
                {
                    if (rend == null) continue;
                    rend.GetPropertyBlock(pb);
                    pb.SetColor("_BaseColor", new Color(0.25f, 0.22f, 0.18f));
                    pb.SetColor("_Color", new Color(0.25f, 0.22f, 0.18f));
                    rend.SetPropertyBlock(pb);
                }
            }

            OnCrateOpened?.Invoke();

            DeadDepthsPlayerController playerCtrl = interactingPlayer != null
                ? interactingPlayer.GetComponentInParent<DeadDepthsPlayerController>()
                : DeadDepthsPlayerController.Instance;

            var gm = GameManager.Instance;
            var notificationMgr = ItemNotificationManager.Instance;

            // Every crate yields 1 Supply Intel toward the 3/3 evacuation requirement
            if (gm != null)
            {
                gm.AddSupplyIntel(1);
            }
            if (notificationMgr != null)
            {
                notificationMgr.PostNotification("Supply Intel", 1);
            }

            // Grant items to player: immediate heal when injured (preserves Test #6), plus Quick-Belt items
            if (lootContents != null && lootContents.Count > 0)
            {
                float totalHeal = 0f;
                bool hasMedkit = false;

                foreach (var item in lootContents)
                {
                    if (notificationMgr != null)
                    {
                        notificationMgr.PostNotification(item.ItemName, item.Quantity);
                    }

                    if (item.IsHealing)
                    {
                        bool isMedkit = item.ItemName.IndexOf("Medkit", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isMedkit) hasMedkit = true;

                        float perUnit = isMedkit
                            ? crateConfig.MedkitHealAmount
                            : crateConfig.BandageHealAmount;
                        totalHeal += perUnit * Mathf.Max(1, item.Quantity);
                    }
                    else if (gm != null && item.ItemName.IndexOf("Adrenaline", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gm.AddBeltItem(BeltItemType.Adrenaline, Mathf.Max(1, item.Quantity));
                    }
                }

                if (totalHeal > 0f)
                {
                    bool playerInjured = playerCtrl != null && playerCtrl.CurrentHealth < playerCtrl.MaxHealth - 0.1f;
                    if (playerInjured)
                    {
                        playerCtrl.Heal(totalHeal);
                        CombatVfxManager.Instance?.PlayHealBurst(playerCtrl.transform.position);
                        DamagePopupManager.Instance?.ShowHeal(playerCtrl.transform.position + Vector3.up * 1.9f, Mathf.RoundToInt(totalHeal));
                        if (PlayerPerformanceTracker.Instance != null)
                        {
                            PlayerPerformanceTracker.Instance.RecordResourceConsumed(1f);
                        }
                        // Also grant a spare belt bandage/adrenaline so players feel rewarded
                        if (gm != null)
                        {
                            gm.AddBeltItem(BeltItemType.Bandage, 1);
                        }
                    }
                    else if (gm != null)
                    {
                        gm.AddBeltItem(hasMedkit ? BeltItemType.Medkit : BeltItemType.Bandage, 1);
                    }
                }

                // Small chance or alternating crates drop Adrenaline for Quick-Belt slot [3]
                if (gm != null && (gm.TotalCratesOpened % 2 == 0 || gm.AdrenalineCount == 0))
                {
                    gm.AddBeltItem(BeltItemType.Adrenaline, 1);
                    if (notificationMgr != null)
                    {
                        notificationMgr.PostNotification("Adrenaline", 1);
                    }
                }
            }

            if (gm != null)
            {
                gm.RecordCrateOpened();
            }
        }

        private void Spawn3DLootPopup()
        {
            if (!Application.isPlaying) return;

            GameObject prefab = null;
            if (lootContents != null)
            {
                foreach (var item in lootContents)
                {
                    if (item.ItemName.IndexOf("Medkit", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        prefab = medkitPreviewPrefab != null ? medkitPreviewPrefab : firstAidKitPreviewPrefab;
                        break;
                    }
                    if (item.ItemName.IndexOf("Bandage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        prefab = bandagePreviewPrefab;
                        break;
                    }
                }
            }

            if (prefab == null)
            {
                if (firstAidKitPreviewPrefab != null) prefab = firstAidKitPreviewPrefab;
                else if (medkitPreviewPrefab != null) prefab = medkitPreviewPrefab;
                else prefab = bandagePreviewPrefab;
            }
            if (prefab == null) return;

            GameObject popup = Instantiate(prefab, transform.position + Vector3.up * 0.85f, Quaternion.identity);
            popup.name = "CrateLootPopup";
            foreach (var c in popup.GetComponentsInChildren<Collider>())
            {
                Destroy(c);
            }
            lootPopupTransform = popup.transform;
            lootPopupTimer = 0f;
        }
    }
}
