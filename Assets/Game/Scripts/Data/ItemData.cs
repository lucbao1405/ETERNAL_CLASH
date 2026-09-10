using System;
using UnityEngine;

namespace EternalClash.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
    public class ItemData : ScriptableObject
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
