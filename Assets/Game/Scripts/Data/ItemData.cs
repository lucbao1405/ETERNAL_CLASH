using System;
using UnityEngine;

namespace EternalClash.Data
{
    public enum EquipmentSlot
    {
        None,
        Weapon,
        Shield,
        Armor
    }

    [Serializable]
    [CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemId;
        public string itemName;
        public string itemType;
        public EquipmentSlot equipmentSlot;
        public ItemRarity rarity;
        public int stars;
        public int weaponTier;
        public int armorTier;
        public int strBonus;
        public int intBonus;
        public int vitBonus;
        public int luckBonus;
        [Min(1)] public int level = 1;
        [Min(0)] public int upgradeLevel;
        public string description;
        public Sprite icon;
        public int quantity;
        public string iconSpriteName;
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
