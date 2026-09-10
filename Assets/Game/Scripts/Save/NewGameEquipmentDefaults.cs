using System;
using EternalClash.Data;
using UnityEngine;

namespace EternalClash.Core.Save
{
    /// <summary>
    /// Defines the equipment snapshot granted to a genuinely new save.
    /// Existing saves are never rewritten by this class.
    /// </summary>
    public static class NewGameEquipmentDefaults
    {
        public const string IronSwordId = "iron_sword_t1";
        public const string IronShieldId = "iron_shield_t1";
        public const string IronArmorId = "Iron_armor";

        public static void Apply(SaveData data)
        {
            if (data == null)
                return;

            data.equipment ??= new EquipmentSaveData();
            data.equipment.weapon = CreateItem(IronSwordId, 1, 5, 0, 0, 0, 1);
            data.equipment.shield = CreateItem(IronShieldId, 2, 0, 0, 10, 0, 1);
            data.equipment.armor = CreateItem(IronArmorId, 3, 0, 5, 10, 0, 1);
            data.weaponTier = 1;
            data.armorTier = 1;
        }

        public static string GetDisplayName(string itemId)
        {
            if (string.Equals(itemId, IronSwordId, StringComparison.OrdinalIgnoreCase))
                return "Iron Sword";
            if (string.Equals(itemId, IronShieldId, StringComparison.OrdinalIgnoreCase))
                return "Iron Shield";
            if (string.Equals(itemId, IronArmorId, StringComparison.OrdinalIgnoreCase))
                return "Iron Armor";
            return itemId;
        }

        public static ItemData CreateItemData(string itemId)
        {
            if (string.Equals(itemId, IronSwordId, StringComparison.OrdinalIgnoreCase))
                return CreateItemData(IronSwordId, "Iron Sword", EquipmentSlot.Weapon, 5, 0, 0, 1, 0);
            if (string.Equals(itemId, IronShieldId, StringComparison.OrdinalIgnoreCase))
                return CreateItemData(IronShieldId, "Iron Shield", EquipmentSlot.Shield, 0, 0, 10, 1, 0);
            if (string.Equals(itemId, IronArmorId, StringComparison.OrdinalIgnoreCase))
                return CreateItemData(IronArmorId, "Iron Armor", EquipmentSlot.Armor, 0, 5, 10, 0, 1);
            return null;
        }

        private static EquipmentItemSaveData CreateItem(
            string itemId,
            int slot,
            int strength,
            int intelligence,
            int vitality,
            int luck,
            int level)
        {
            return new EquipmentItemSaveData
            {
                itemId = itemId,
                slot = slot,
                level = level,
                upgradeLevel = 0,
                strength = strength,
                intelligence = intelligence,
                vitality = vitality,
                luck = luck,
                isNew = false
            };
        }

        private static ItemData CreateItemData(
            string itemId,
            string itemName,
            EquipmentSlot slot,
            int strength,
            int intelligence,
            int vitality,
            int weaponTier,
            int armorTier)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = itemId;
            item.itemName = itemName;
            item.itemType = "Equipment";
            item.equipmentSlot = slot;
            item.rarity = ItemRarity.Common;
            item.stars = 1;
            item.weaponTier = weaponTier;
            item.armorTier = armorTier;
            item.strBonus = strength;
            item.intBonus = intelligence;
            item.vitBonus = vitality;
            item.level = 1;
            return item;
        }
    }
}
