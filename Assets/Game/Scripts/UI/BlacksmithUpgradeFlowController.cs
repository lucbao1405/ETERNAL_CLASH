using System;
using EternalClash.Data;
using EternalClash.Upgrade;
using EternalClash.Village;
using UnityEngine;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    public sealed class BlacksmithUpgradeFlowController : MonoBehaviour
    {
        public static event Action<ItemData> OnUpgradeSuccess;

        public bool TryPerformUpgrade(ItemSlot slot)
        {
            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null || !smith.CanUpgrade(slot))
                return false;

            if (!smith.TryUpgrade(slot))
                return false;

            ItemData item = EquipmentSystem.Instance?.GetEquippedItem(slot);
            FireUpgradeSuccess(item);
            return true;
        }

        public bool CanUpgrade(ItemSlot slot)
        {
            return BlacksmithCraftingSystem.Instance?.CanUpgrade(slot) == true;
        }

        private static void FireUpgradeSuccess(ItemData item)
        {
            try
            {
                OnUpgradeSuccess?.Invoke(item);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
