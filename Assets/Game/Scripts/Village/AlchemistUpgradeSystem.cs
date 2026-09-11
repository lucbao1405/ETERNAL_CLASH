using System;
using System.Collections.Generic;
using System.Linq;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.UI;
using UnityEngine;

namespace EternalClash.Village
{
    /// <summary>Persistent material-only upgrade state for Witch abilities.</summary>
    public sealed class AlchemistUpgradeSystem : MonoBehaviour
    {
        public static AlchemistUpgradeSystem Instance { get; private set; }
        public const int BaseHealingBonusPercent = 0;
        public const int HealingBonusPerLevelPercent = 2;
        public const int BaseCooldownSeconds = 15;
        public const int CooldownReductionPerLevelSeconds = 1;

        public int HealingLevel { get; private set; }
        public int CooldownLevel { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public MaterialType GetRequiredMaterial(WitchAbility ability)
        {
            return ability == WitchAbility.Healing ? MaterialType.Wood : MaterialType.Ore;
        }

        public int GetUpgradeCost(WitchAbility ability)
        {
            int level = ability == WitchAbility.Healing ? HealingLevel : CooldownLevel;
            int baseCost = ability == WitchAbility.Healing ? 3 : 2;
            return Mathf.CeilToInt(baseCost * Mathf.Pow(1.25f, Mathf.Max(0, level)));
        }

        public bool CanUpgradeHealing() => CanUpgrade(WitchAbility.Healing);
        public bool CanUpgradeCooldown() => CanUpgrade(WitchAbility.Cooldown);
        public bool TryUpgradeHealing() => TryUpgrade(WitchAbility.Healing);
        public bool TryUpgradeCooldown() => TryUpgrade(WitchAbility.Cooldown);

        public int GetHealingBonusPercent()
        {
            return GetHealingBonusPercent(HealingLevel);
        }

        public static int GetHealingBonusPercent(int level)
        {
            return BaseHealingBonusPercent + Mathf.Max(0, level) * HealingBonusPerLevelPercent;
        }

        public int GetCooldownValue()
        {
            return Mathf.Max(0, BaseCooldownSeconds - CooldownLevel * CooldownReductionPerLevelSeconds);
        }

        public bool CanUpgrade(WitchAbility ability)
        {
            SaveData data = SaveManager.Instance?.Data;
            return GetMaterialAmount(data?.inventory?.items, GetMaterialItemId(GetRequiredMaterial(ability))) >=
                GetUpgradeCost(ability);
        }

        public bool TryUpgrade(WitchAbility ability)
        {
            MaterialType material = GetRequiredMaterial(ability);
            int cost = GetUpgradeCost(ability);
            SaveData data = SaveManager.Instance?.Data;
            List<ItemStackSaveData> items = data?.inventory?.items;
            string itemId = GetMaterialItemId(material);
            if (items == null || GetMaterialAmount(items, itemId) < cost)
                return false;

            ConsumeMaterial(items, itemId, cost);

            if (ability == WitchAbility.Healing)
                HealingLevel++;
            else
                CooldownLevel++;

            data.abilities ??= new AbilitySaveData();
            data.abilities.healingLevel = HealingLevel;
            data.abilities.cooldownLevel = CooldownLevel;
            SaveCoordinator.RequestSave();
            RefreshBags();

            Debug.Log($"[WITCH] {ability} upgraded using {cost} {material}.");
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            HealingLevel = Mathf.Max(0, data?.abilities?.healingLevel ?? 0);
            CooldownLevel = Mathf.Max(0, data?.abilities?.cooldownLevel ?? 0);
        }

        public static string GetMaterialItemId(MaterialType materialType)
        {
            return materialType == MaterialType.Ore ? "copper_ore"
                : materialType == MaterialType.Leather ? "wolf_hide"
                : materialType == MaterialType.Wood ? "wood_small"
                : materialType == MaterialType.Steel ? "steel_ore"
                : string.Empty;
        }

        public static int GetMaterialAmount(IEnumerable<ItemStackSaveData> items, string itemId)
        {
            if (items == null || string.IsNullOrWhiteSpace(itemId))
                return 0;

            return items.Where(item => item != null &&
                    string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                .Sum(item => Mathf.Max(0, item.amount));
        }

        private static void ConsumeMaterial(List<ItemStackSaveData> items, string itemId, int amount)
        {
            for (int i = items.Count - 1; i >= 0 && amount > 0; i--)
            {
                ItemStackSaveData stack = items[i];
                if (stack == null || !string.Equals(stack.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                int consumed = Mathf.Min(Mathf.Max(0, stack.amount), amount);
                stack.amount -= consumed;
                amount -= consumed;
                if (stack.amount <= 0)
                    items.RemoveAt(i);
            }
        }

        private static void RefreshBags()
        {
            foreach (BagController bag in FindObjectsOfType<BagController>(true))
                bag.Refresh();
        }
    }

    public enum WitchAbility
    {
        Healing,
        Cooldown
    }
}
