using UnityEngine;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.Stage
{
    public static class RewardGenerator
    {
        private static readonly string[] WeaponNames = {
            "Rusty Blade", "Iron Sword", "Steel Claymore", "Knight's Greatsword", "Royal Saber"
        };

        private static readonly string[] ArmorNames = {
            "Linen Vest", "Leather Jerkin", "Iron Plate", "Knight's Armor", "Royal Cuirass"
        };

        private static readonly string[] Descriptions = {
            "A simple weapon for beginners.",
            "Reliable steel, well-balanced.",
            "Forged for the front lines.",
            "Worn by seasoned knights.",
            "Fit for royalty."
        };

        public static RewardData GenerateStageReward(int stageLevel)
        {
            float roll = Random.value;
            RewardData reward = new RewardData();

            if (roll < 0.35f)
            {
                reward.type = RewardType.Gold;
                reward.amount = Random.Range(20, 60) + stageLevel * 10;
            }
            else if (roll < 0.55f)
            {
                reward.type = RewardType.Material;
                reward.amount = Random.Range(1, 3);
            }
            else if (roll < 0.75f)
            {
                reward.type = RewardType.Gem;
                reward.amount = Random.Range(1, 3);
            }
            else
            {
                reward.type = RewardType.Equipment;
                reward.item = GenerateEquipment(stageLevel);
                reward.amount = 1;
            }

            return reward;
        }

        private static ItemData GenerateEquipment(int stageLevel)
        {
            ItemRarity rarity = RollRarity();
            int tier = Mathf.Clamp((int)rarity, 0, 4);
            bool isWeapon = Random.value > 0.5f;

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = isWeapon ? "weapon_" + tier : "armor_" + tier;
            item.itemName = isWeapon ? WeaponNames[tier] : ArmorNames[tier];
            item.itemType = isWeapon ? "Weapon" : "Armor";
            item.equipmentSlot = isWeapon ? EquipmentSlot.Weapon : EquipmentSlot.Armor;
            item.rarity = rarity;
            item.stars = Mathf.Clamp(tier + 1, 1, 5);
            item.weaponTier = isWeapon ? tier : 0;
            item.armorTier = isWeapon ? 0 : tier;
            item.level = Mathf.Max(1, tier);
            item.description = Descriptions[tier];

            int statBudget = 1 + tier * 2 + stageLevel;
            int str = 0, intel = 0, vit = 0, luck = 0;

            if (isWeapon)
            {
                str = statBudget;
                item.strBonus = str;
            }
            else
            {
                vit = statBudget;
                item.vitBonus = vit;
            }

            item.intBonus = intel;
            item.luckBonus = luck;

            return item;
        }

        private static ItemRarity RollRarity()
        {
            float roll = Random.value;
            if (roll < 0.50f) return ItemRarity.Common;
            if (roll < 0.80f) return ItemRarity.Uncommon;
            if (roll < 0.95f) return ItemRarity.Rare;
            if (roll < 0.99f) return ItemRarity.Epic;
            return ItemRarity.Legendary;
        }
    }
}
