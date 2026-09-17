using EternalClash.Upgrade;
using EternalClash.Village;
using UnityEngine;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    public sealed class BlacksmithUpgradeFlowController : MonoBehaviour
    {
        public bool TryPerformUpgrade(ItemSlot slot)
        {
            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null || !smith.CanUpgrade(slot))
                return false;

            return smith.TryUpgrade(slot);
        }
    }
}
