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
        public const int MinCooldownSeconds = 5;
        public const int MaxCooldownLevel = BaseCooldownSeconds - MinCooldownSeconds;
        public const int BaseShieldValue = 5;
        public const int ShieldValuePerLevel = 2;
        public const int ExpPerInfuse = 10;
        public const int BaseHealingExp = 30;
        public const int BaseAbilityExp = 20;

        public int HealingLevel { get; private set; }
        public int CooldownLevel { get; private set; }
        public int DefenseLevel { get; private set; }
        public int HealingExp { get; private set; }
        public int CooldownExp { get; private set; }
        public int DefenseExp { get; private set; }

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
            // Materials still needed to fill the current EXP bar (legacy UI text).
            int missing = GetExpToNextLevel(ability, GetLevel(ability)) - GetCurrentExp(ability);
            return Mathf.Max(1, Mathf.CeilToInt(missing / (float)ExpPerInfuse));
        }

        public static int GetExpToNextLevel(WitchAbility ability, int level)
        {
            int baseExp = ability == WitchAbility.Healing ? BaseHealingExp : BaseAbilityExp;
            return Mathf.CeilToInt(baseExp * Mathf.Pow(1.25f, Mathf.Max(0, level)));
        }

        public int GetCurrentExp(WitchAbility ability)
        {
            return ability == WitchAbility.Healing ? HealingExp
                : ability == WitchAbility.Cooldown ? CooldownExp : DefenseExp;
        }

        public int GetLevel(WitchAbility ability)
        {
            return ability == WitchAbility.Healing ? HealingLevel
                : ability == WitchAbility.Cooldown ? CooldownLevel : DefenseLevel;
        }

        private void SetLevel(WitchAbility ability, int value)
        {
            if (ability == WitchAbility.Healing) HealingLevel = value;
            else if (ability == WitchAbility.Cooldown) CooldownLevel = value;
            else DefenseLevel = value;
        }

        private void SetExp(WitchAbility ability, int value)
        {
            if (ability == WitchAbility.Healing) HealingExp = value;
            else if (ability == WitchAbility.Cooldown) CooldownExp = value;
            else DefenseExp = value;
        }

        public bool CanUpgradeHealing() => CanUpgrade(WitchAbility.Healing);
        public bool TryUpgradeHealing() => TryUpgrade(WitchAbility.Healing);

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
            // Stat only moves on a level-up (full EXP bar): -1s per level,
            // floored at MinCooldownSeconds so it never reaches 0.
            return Mathf.Max(MinCooldownSeconds, BaseCooldownSeconds - Mathf.Max(0, level));
        }

        public int GetShieldValue()
        {
            return GetShieldValue(DefenseLevel);
        }

        public static int GetShieldValue(int level)
        {
            return BaseShieldValue + Mathf.Max(0, level) * ShieldValuePerLevel;
        }

        public bool IsMaxed(WitchAbility ability)
        {
            return ability == WitchAbility.Cooldown && CooldownLevel >= MaxCooldownLevel;
        }

        public bool CanUpgrade(WitchAbility ability)
        {
            if (IsMaxed(ability))
                return false;
            SaveData data = SaveManager.Instance?.Data;
            return GetMaterialAmount(data?.inventory?.items, GetMaterialItemId(GetRequiredMaterial(ability))) >= 1;
        }

        /// <summary>Legacy full-level API: infuses until the level rises.</summary>
        public bool TryUpgrade(WitchAbility ability)
        {
            int levelBefore = GetLevel(ability);
            while (GetLevel(ability) == levelBefore && TryInfuse(ability)) { }
            return GetLevel(ability) > levelBefore;
        }

        /// <summary>
        /// Consumes one material and fills the ability's EXP bar. The stat only
        /// changes when the bar fills and the level rises; a maxed ability
        /// accepts nothing.
        /// </summary>
        public bool TryInfuse(WitchAbility ability)
        {
            if (IsMaxed(ability))
                return false;

            string itemId = GetMaterialItemId(GetRequiredMaterial(ability));
            List<ItemStackSaveData> items = SaveManager.Instance?.Data?.inventory?.items;
            if (items == null || GetMaterialAmount(items, itemId) < 1)
                return false;

            ConsumeMaterial(items, itemId, 1);

            int level = GetLevel(ability);
            int exp = GetCurrentExp(ability) + ExpPerInfuse;
            while (exp >= GetExpToNextLevel(ability, level))
            {
                exp -= GetExpToNextLevel(ability, level);
                level++;
            }

            SetLevel(ability, level);
            SetExp(ability, exp);
            PersistAbilities();
            RefreshBags();

            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.ItemUpgrade);
            Debug.Log($"[WITCH] {ability} infused +{ExpPerInfuse} EXP (level {level}, exp {exp}).");
            return true;
        }

        private void PersistAbilities()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return;
            data.abilities ??= new AbilitySaveData();
            data.abilities.healingLevel = HealingLevel;
            data.abilities.cooldownLevel = CooldownLevel;
            data.abilities.defenseLevel = DefenseLevel;
            data.abilities.healingExp = HealingExp;
            data.abilities.cooldownExp = CooldownExp;
            data.abilities.defenseExp = DefenseExp;
            SaveCoordinator.RequestSave();
        }

        public void LoadFromSave(SaveData data)
        {
            HealingLevel = Mathf.Max(0, data?.abilities?.healingLevel ?? 0);
            CooldownLevel = Mathf.Max(0, data?.abilities?.cooldownLevel ?? 0);
            DefenseLevel = Mathf.Max(0, data?.abilities?.defenseLevel ?? 0);
            HealingExp = Mathf.Max(0, data?.abilities?.healingExp ?? 0);
            CooldownExp = Mathf.Max(0, data?.abilities?.cooldownExp ?? 0);
            DefenseExp = Mathf.Max(0, data?.abilities?.defenseExp ?? 0);
        }

        [ContextMenu("Self Check")]
        public void SelfCheck()
        {
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Healing)) == "blue_flower", "healing brews blue flower");
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Cooldown)) == "blue_flower", "cooldown brews blue flower");
            Debug.Assert(GetMaterialItemId(GetRequiredMaterial(WitchAbility.Defense)) == "blue_flower", "defense brews blue flower");
            Debug.Assert(GetExpToNextLevel(WitchAbility.Defense, 0) == BaseAbilityExp &&
                GetExpToNextLevel(WitchAbility.Healing, 0) == BaseHealingExp, "base exp");
            Debug.Assert(GetExpToNextLevel(WitchAbility.Healing, 4) > GetExpToNextLevel(WitchAbility.Healing, 0),
                "exp grows with level");
            Debug.Assert(GetShieldValue(0) == BaseShieldValue && GetShieldValue(3) == BaseShieldValue + 3 * ShieldValuePerLevel, "shield value");
            Debug.Assert(GetCooldownValue(0) == BaseCooldownSeconds && GetCooldownValue(999) == MinCooldownSeconds,
                "cooldown floors at MinCooldownSeconds");
            Debug.Assert(GetCooldownValue(MaxCooldownLevel) == MinCooldownSeconds, "max level reaches the floor");
            for (int level = 1; level < 60; level++)
                Debug.Assert(GetCooldownValue(level) <= GetCooldownValue(level - 1), "cooldown never rises");
            for (int level = 0; level < 60; level++)
            {
                int drop = GetCooldownValue(level) - GetCooldownValue(level + 1);
                Debug.Assert(drop >= 0 && drop <= 1, "cooldown drops 0-1s per level");
            }

            int previousHealing = HealingLevel;
            int previousCooldown = CooldownLevel;
            int previousDefense = DefenseLevel;
            int previousHealingExp = HealingExp;
            int previousCooldownExp = CooldownExp;
            int previousDefenseExp = DefenseExp;
            SaveData probe = new SaveData();
            probe.abilities = new AbilitySaveData
            {
                healingLevel = 2, cooldownLevel = 1, defenseLevel = 4,
                healingExp = 7, cooldownExp = 13, defenseExp = 19
            };
            LoadFromSave(probe);
            Debug.Assert(HealingLevel == 2 && CooldownLevel == 1 && DefenseLevel == 4, "save roundtrip levels");
            Debug.Assert(HealingExp == 7 && CooldownExp == 13 && DefenseExp == 19, "save roundtrip exp");
            LoadFromSave(new SaveData
            {
                abilities = new AbilitySaveData
                {
                    healingLevel = previousHealing,
                    cooldownLevel = previousCooldown,
                    defenseLevel = previousDefense,
                    healingExp = previousHealingExp,
                    cooldownExp = previousCooldownExp,
                    defenseExp = previousDefenseExp
                }
            });

            Debug.Log("[WITCH] SelfCheck passed.");
        }

        public static string GetMaterialItemId(MaterialType materialType)
        {
            return materialType == MaterialType.Ore || materialType == MaterialType.Leather ||
                materialType == MaterialType.Wood ? "blue_flower"
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
