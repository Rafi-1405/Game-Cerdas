using System;
using System.Collections.Generic;
using UnityEngine;

namespace UTS.Inventory
{
    [DisallowMultipleComponent]
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Backpack Capacity")]
        [SerializeField] private float maxWeight = 40f;

        [Header("Collected Items (Read-Only)")]
        [SerializeField] private int totalValue = 0;
        [SerializeField] private float currentWeight = 0f;
        [SerializeField] private int bandageCount = 0;
        [SerializeField] private int firstAidCount = 0;
        [SerializeField] private int medkitCount = 0;

        public int TotalValue => totalValue;
        public float CurrentWeight => currentWeight;
        public float MaxWeight => maxWeight;
        public float WeightRatio => maxWeight > 0f ? Mathf.Clamp01(currentWeight / maxWeight) : 0f;
        public bool IsFull => currentWeight >= maxWeight;

        public int BandageCount => bandageCount;
        public int FirstAidCount => firstAidCount;
        public int MedkitCount => medkitCount;

        public event Action OnInventoryChanged;

        public bool CanAddItem(ScavengeItemType type)
        {
            float itemWeight = ScavengeItemInfo.GetWeight(type);
            return (currentWeight + itemWeight) <= maxWeight;
        }

        public bool TryAddItem(ScavengeItemType type)
        {
            float itemWeight = ScavengeItemInfo.GetWeight(type);
            int itemValue = ScavengeItemInfo.GetValue(type);
            string itemName = ScavengeItemInfo.GetName(type);

            if ((currentWeight + itemWeight) > maxWeight)
            {
                Debug.LogWarning($"[PlayerInventory] Ransel penuh! Tidak bisa mengambil {itemName}. (Beban: {currentWeight:F1}/{maxWeight:F1} kg)", this);
                return false;
            }

            currentWeight += itemWeight;
            totalValue += itemValue;

            switch (type)
            {
                case ScavengeItemType.Bandage:
                    bandageCount++;
                    break;
                case ScavengeItemType.FirstAidKit:
                    firstAidCount++;
                    break;
                case ScavengeItemType.Medkit:
                    medkitCount++;
                    break;
            }

            Debug.Log($"[PlayerInventory] Mengambil {itemName} (+{itemValue} Value, +{itemWeight:F1}kg). Total Value: {totalValue}, Total Beban: {currentWeight:F1}/{maxWeight:F1}kg", this);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public int GetCount(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage: return bandageCount;
                case ScavengeItemType.FirstAidKit: return firstAidCount;
                case ScavengeItemType.Medkit: return medkitCount;
                default: return 0;
            }
        }

        public bool TryConsumeItem(ScavengeItemType type)
        {
            if (GetCount(type) <= 0) return false;

            float itemWeight = ScavengeItemInfo.GetWeight(type);
            int itemValue = ScavengeItemInfo.GetValue(type);
            string itemName = ScavengeItemInfo.GetName(type);

            switch (type)
            {
                case ScavengeItemType.Bandage:
                    bandageCount--;
                    break;
                case ScavengeItemType.FirstAidKit:
                    firstAidCount--;
                    break;
                case ScavengeItemType.Medkit:
                    medkitCount--;
                    break;
            }

            currentWeight = Mathf.Max(0f, currentWeight - itemWeight);
            totalValue = Mathf.Max(0, totalValue - itemValue);

            Debug.Log($"[PlayerInventory] Menggunakan {itemName}. Sisa: {GetCount(type)} pcs (Beban: {currentWeight:F1}/{maxWeight:F1}kg)", this);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void ClearInventory()
        {
            totalValue = 0;
            currentWeight = 0f;
            bandageCount = 0;
            firstAidCount = 0;
            medkitCount = 0;
            OnInventoryChanged?.Invoke();
        }
    }
}
