using System;
using System.Collections.Generic;
using System.Linq;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.Upgrade;
using UnityEngine;

namespace EternalClash.Village
{
    public class BlacksmithCraftingSystem : MonoBehaviour
    {
        public static BlacksmithCraftingSystem Instance { get; private set; }

        // Kept as compatibility constants for existing town UI callers.
        public const int TIER1_GOLD_COST = 10;
        public const int TIER1_ORE_COST = 2;
        public const int TIER1_LEATHER_COST = 0;

        public int WeaponTier { get; private set; }
        public int ArmorTier { get; private set; }

        private UpgradeRecipeData[] recipes;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            recipes = Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes");
        }

        public UpgradeRecipeData GetRecipe(ItemSlot slot)
        {
            ItemData item = EquipmentSystem.Instance?.GetEquippedItem(slot);
            return FindRecipe(item != null ? item.itemId : GetSavedItemId(slot));
        }

        public bool CanUpgrade(ItemSlot slot)
        {
            UpgradeRecipeData recipe = GetRecipe(slot);
            EquipmentItemSaveData savedItem = GetSavedItem(SaveManager.Instance?.Data, slot);
            return recipe != null && HasResources(SaveManager.Instance?.Data, recipe,
                savedItem != null ? savedItem.upgradeLevel : 0, false);
        }

        public bool TryUpgrade(ItemSlot slot)
        {
            SaveData data = SaveManager.Instance?.Data;
            UpgradeRecipeData recipe = GetRecipe(slot);
            EquipmentItemSaveData savedItem = GetSavedItem(data, slot);
            if (recipe == null || savedItem == null || !HasResources(data, recipe, savedItem.upgradeLevel, true))
                return false;

            data.currency.gold -= GetGoldCost(recipe, savedItem.upgradeLevel);
            // Save migration still mirrors legacy currency fields during persistence.
            data.gold = data.currency.gold;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                int cost = GetMaterialCost(requirement.amount, savedItem.upgradeLevel);
                if (cost > 0)
                    ConsumeMaterial(data.inventory.items, GetMaterialItemId(requirement.materialType), cost);
            }

            GoldSystem.Instance?.LoadFromSave(data);

            savedItem.upgradeLevel += Mathf.Max(1, recipe.upgradeValue);
            ApplyUpgradeBonus(savedItem, slot, Mathf.Max(1, recipe.upgradeValue));
            UpgradeInventorySnapshot(data, savedItem);
            if (slot == ItemSlot.Weapon)
                WeaponTier = Mathf.Max(WeaponTier, savedItem.level);
            else if (slot == ItemSlot.Armor)
                ArmorTier = Mathf.Max(ArmorTier, savedItem.level);

            // Upgrade only reloads persistent item data and stats. It must not refresh player visuals.
            EquipmentSystem.Instance?.RefreshFromSaveWithoutVisuals();
            SaveCoordinator.RequestSave();
            Debug.Log($"[BLACKSMITH] {savedItem.itemId} upgraded to +{savedItem.upgradeLevel}");
            return true;
        }

        public bool CanUpgradeWeapon() => CanUpgrade(ItemSlot.Weapon);
        public bool CanUpgradeArmor() => CanUpgrade(ItemSlot.Armor);
        public bool CanUpgradeShield() => CanUpgrade(ItemSlot.Accessory);
        public void UpgradeWeapon() => TryUpgradeWeapon();
        public void UpgradeArmor() => TryUpgradeArmor();
        public bool TryUpgradeWeapon() => TryUpgrade(ItemSlot.Weapon);
        public bool TryUpgradeArmor() => TryUpgrade(ItemSlot.Armor);
        public bool TryUpgradeShield() => TryUpgrade(ItemSlot.Accessory);

        public void LoadFromSave(SaveData data)
        {
            WeaponTier = data != null ? data.weaponTier : 0;
            ArmorTier = data != null ? data.armorTier : 0;
        }

        public static int GetGoldCost(UpgradeRecipeData recipe, int upgradeLevel)
        {
            return recipe != null ? recipe.goldCost * (Mathf.Max(0, upgradeLevel) + 1) : 0;
        }

        public static int GetMaterialCost(int baseAmount, int upgradeLevel)
        {
            if (baseAmount <= 0)
                return 0;
            return Mathf.CeilToInt(baseAmount * Mathf.Pow(1.25f, Mathf.Max(0, upgradeLevel)));
        }

        private static bool HasResources(SaveData data, UpgradeRecipeData recipe, int upgradeLevel, bool logFailures)
        {
            if (data?.currency == null || data.inventory?.items == null)
                return false;

            int goldCost = GetGoldCost(recipe, upgradeLevel);
            if (data.currency.gold < goldCost)
            {
                if (logFailures)
                    Debug.LogWarning($"[BLACKSMITH] Gold check failed: Required: gold {goldCost}; Actual: gold {data.currency.gold}");
                return false;
            }

            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                int materialCost = GetMaterialCost(requirement.amount, upgradeLevel);
                if (materialCost <= 0)
                    continue;

                string itemId = GetMaterialItemId(requirement.materialType);
                int actual = GetMaterialAmount(data.inventory.items, itemId);
                if (actual < materialCost)
                {
                    if (logFailures)
                    {
                        Debug.LogWarning($"[BLACKSMITH] Material check failed:\nRequired:\n{itemId} {materialCost}\nActual:\n{itemId} {actual}");
                    }
                    return false;
                }
            }

            return true;
        }

        public static string GetMaterialItemId(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Ore: return "copper_ore";
                case MaterialType.Leather: return "wolf_hide";
                case MaterialType.Wood: return "wood_small";
                case MaterialType.Steel: return "steel_ore";
                default: return string.Empty;
            }
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

                int consumed = Mathf.Min(stack.amount, amount);
                stack.amount -= consumed;
                amount -= consumed;
                if (stack.amount <= 0)
                    items.RemoveAt(i);
            }
        }

        private UpgradeRecipeData FindRecipe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            foreach (UpgradeRecipeData recipe in recipes ?? Array.Empty<UpgradeRecipeData>())
                if (recipe != null && string.Equals(NormalizeItemId(recipe.itemId), NormalizeItemId(itemId),
                    StringComparison.OrdinalIgnoreCase))
                    return recipe;
            return null;
        }

        private static string NormalizeItemId(string itemId)
        {
            int tierMarker = itemId.LastIndexOf("_t", StringComparison.OrdinalIgnoreCase);
            if (tierMarker >= 0 && int.TryParse(itemId.Substring(tierMarker + 2), out _))
                return itemId.Substring(0, tierMarker);
            return itemId;
        }

        private static EquipmentItemSaveData GetSavedItem(SaveData data, ItemSlot slot)
        {
            if (data?.equipment == null)
                return null;
            return slot == ItemSlot.Weapon ? data.equipment.weapon
                : slot == ItemSlot.Armor ? data.equipment.armor : data.equipment.shield;
        }

        private static string GetSavedItemId(ItemSlot slot)
        {
            return GetSavedItem(SaveManager.Instance?.Data, slot)?.itemId;
        }

        private static void ApplyUpgradeBonus(EquipmentItemSaveData item, ItemSlot slot, int value)
        {
            if (slot == ItemSlot.Weapon)
                item.strength += value;
            else if (slot == ItemSlot.Armor)
                item.vitality += value;
            else
                item.vitality += value;
        }

        private static void UpgradeInventorySnapshot(SaveData data, EquipmentItemSaveData upgraded)
        {
            if (data?.equipmentInventory == null || upgraded == null)
                return;
            foreach (EquipmentItemSaveData item in data.equipmentInventory)
            {
                if (item == null || item.itemId != upgraded.itemId || item.slot != upgraded.slot)
                    continue;
                item.upgradeLevel = upgraded.upgradeLevel;
                item.strength = upgraded.strength;
                item.intelligence = upgraded.intelligence;
                item.vitality = upgraded.vitality;
                item.luck = upgraded.luck;
            }
        }
    }
}
