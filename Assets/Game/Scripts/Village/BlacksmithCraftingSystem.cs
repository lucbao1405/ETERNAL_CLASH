using System;
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
        public const int TIER1_GOLD_COST = 100;
        public const int TIER1_ORE_COST = 5;
        public const int TIER1_LEATHER_COST = 3;

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
            GoldSystem gold = GoldSystem.Instance;
            return recipe != null && gold != null && HasResources(gold, recipe);
        }

        public bool TryUpgrade(ItemSlot slot)
        {
            SaveData data = SaveManager.Instance?.Data;
            GoldSystem gold = GoldSystem.Instance;
            UpgradeRecipeData recipe = GetRecipe(slot);
            EquipmentItemSaveData savedItem = GetSavedItem(data, slot);
            if (recipe == null || gold == null || savedItem == null || !HasResources(gold, recipe))
                return false;

            gold.SpendGold(recipe.goldCost);
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                if (requirement.amount > 0)
                    gold.SpendMaterial(requirement.materialType, requirement.amount);
            }

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

        private bool HasResources(GoldSystem gold, UpgradeRecipeData recipe)
        {
            if (gold.Gold < recipe.goldCost)
                return false;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
                if (gold.GetMaterial(requirement.materialType) < requirement.amount)
                    return false;
            return true;
        }

        private UpgradeRecipeData FindRecipe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            foreach (UpgradeRecipeData recipe in recipes ?? Array.Empty<UpgradeRecipeData>())
                if (recipe != null && recipe.itemId == itemId)
                    return recipe;
            return null;
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
