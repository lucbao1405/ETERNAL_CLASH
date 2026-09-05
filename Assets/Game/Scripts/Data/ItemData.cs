using System;

namespace EternalClash.Data
{
    [Serializable]
    public class ItemData
    {
        public string itemId;
        public string itemName;
        public ItemRarity rarity;
        public int stars;
        public int weaponTier;
        public int armorTier;
        public int strBonus;
        public int intBonus;
        public int vitBonus;
        public int luckBonus;
        public string description;
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
