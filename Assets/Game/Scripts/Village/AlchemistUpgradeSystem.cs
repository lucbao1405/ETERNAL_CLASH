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
        public const int BaseShieldValue = 5;
        public const int ShieldValuePerLevel = 2;

        public int HealingLevel { get; private set; }
        public int CooldownLevel { get; private set; }
        public int DefenseLevel { get; private set; }

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
            if (ability == WitchAbility.Healing)
                return MaterialType.Wood;
            if (ability == WitchAbility.Cooldown)
                return MaterialType.Ore;
            return MaterialType.Leather;
        }

        public int GetUpgradeCost(WitchAbility ability)
        {
            int level = ability == WitchAbility.Healing ? HealingLevel
                : ability == WitchAbility.Cooldown ? CooldownLevel
                : DefenseLevel;
            return GetUpgradeCost(ability, level);
        }

        public static int GetUpgradeCost(WitchAbility ability, int level)
        {
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
            return GetCooldownValue(CooldownLevel);
        }

        public static int GetCooldownValue(int level)
        {
            return Mathf.Max(0, BaseCooldownSeconds - Mathf.Max(0, level) * CooldownReductionPerLevelSeconds);
        }

        public int GetShieldValue()
        {
            return GetShieldValue(DefenseLevel);
        }

        public static int GetShieldValue(int level)
        {
            return BaseShieldValue + Mathf.Max(0, level) * ShieldValuePerLevel;
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
            else if (ability == WitchAbility.Cooldown)
                CooldownLevel++;
            else
                DefenseLevel++;

            data.abilities ??= new AbilitySaveData();
            data.abilities.healingLevel = HealingLevel;
            data.abilities.cooldownLevel = CooldownLevel;
            data.abilities.defenseLevel = DefenseLevel;
            SaveCoordinator.RequestSave();
            RefreshBags();

            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.ItemUpgrade);
            Debug.Log($"[WITCH] {ability} upgraded using {cost} {material}.");
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            HealingLevel = Mathf.Max(0, data?.abilities?.healingLevel ?? 0);
            CooldownLevel = Mathf.Max(0, data?.abilities?.cooldownLevel ?? 0);
            DefenseLevel = Mathf.Max(0, data?.abilities?.defenseLevel ?? 0);
        }

        [ContextMenu("Self Check")]
        public void SelfCheck()
        {
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Healing)) == "wood_small", "healing brews wood");
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Cooldown)) == "copper_ore", "cooldown brews ore");
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Defense)) == "wolf_hide", "defense brews hide");
            Debug.Assert(GetUpgradeCost(WitchAbility.Defense, 0) == 2 && GetUpgradeCost(WitchAbility.Healing, 0) == 3, "base costs");
            Debug.Assert(GetUpgradeCost(WitchAbility.Healing, 4) > GetUpgradeCost(WitchAbility.Healing, 0), "cost grows with level");
            Debug.Assert(GetShieldValue(0) == BaseShieldValue && GetShieldValue(3) == BaseShieldValue + 3 * ShieldValuePerLevel, "shield value");
            Debug.Assert(GetCooldownValue(0) == BaseCooldownSeconds && GetCooldownValue(99) == 0, "cooldown clamps");

            int previousHealing = HealingLevel;
            int previousCooldown = CooldownLevel;
            int previousDefense = DefenseLevel;
            SaveData probe = new SaveData();
            probe.abilities = new AbilitySaveData { healingLevel = 2, cooldownLevel = 1, defenseLevel = 4 };
            LoadFromSave(probe);
            Debug.Assert(HealingLevel == 2 && CooldownLevel == 1 && DefenseLevel == 4, "save roundtrip");
            LoadFromSave(new SaveData
            {
                abilities = new AbilitySaveData
                {
                    healingLevel = previousHealing,
                    cooldownLevel = previousCooldown,
                    defenseLevel = previousDefense
                }
            });

            Debug.Log("[WITCH] SelfCheck passed.");
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
        Cooldown,
        Defense
    }
}
