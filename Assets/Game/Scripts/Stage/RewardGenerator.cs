using UnityEngine;
using EternalClash.Data;

namespace EternalClash.Stage
{
    // Phan thuong ruong uu tien vat pham Gift. Bang cu: 85% la la cay nguyen lieu
    // nang cap phu thuy (xanh = heal, do = hoi chieu, vang = khien), phan con lai
    // la hoa bond (hoa cuc vang, hoa xanh) va thuong cu (vang). Kim cuong khong
    // nam trong bang nay nua - rot rieng theo GemChance o moi ruong.
    public static class RewardGenerator
    {
        private static readonly string[] WitchLeafItemIds = { "leaf_green", "leaf_red", "leaf_yellow" };

        // Ti le uu tien Gift khi stage khai bao ca tag tien te ("Gold"/"EXP"):
        // 90% lan mo rong chi rot trong so vat pham gift, phan con lai moi dung pool goc.
        private const float GiftBiasChance = 0.9f;

        // Kim cuong: ti le vua (12%) rot o moi ruong, du stage co drop story hay khong.
        internal const float GemChance = 0.12f;

        public static RewardData GenerateStageReward(int stageLevel)
        {
            if (Random.value < GemChance)
                return new RewardData { type = RewardType.Gem, amount = Random.Range(3, 6) };

            // Story-first: each stage declares its own drops (Wood, Wolf Hide,
            // Copper Ore, Rare Material...). Falls back to the legacy leaf table
            // when the stage has no configured drops.
            string[] drops = EternalClash.Story.StoryManager.GetStageDrops(stageLevel);
            if (drops != null && drops.Length > 0)
            {
                string[] giftDrops = System.Array.FindAll(drops, d => d != "Gold" && d != "EXP");
                string[] pool = giftDrops.Length > 0 && Random.value < GiftBiasChance ? giftDrops : drops;
                RewardData storyReward = GenerateStoryDrop(pool[Random.Range(0, pool.Length)], stageLevel);
                if (storyReward != null)
                    return storyReward;
            }

            float roll = Random.value;
            RewardData reward = new RewardData();

            if (roll < 0.85f)
            {
                reward.type = RewardType.Gift;
                reward.item = ItemCatalog.Find(WitchLeafItemIds[Random.Range(0, WitchLeafItemIds.Length)]);
                reward.amount = Random.Range(2, 4);
            }
            else if (roll < 0.90f)
            {
                reward.type = RewardType.Gift;
                reward.item = ItemCatalog.Find("yellow_wildflower");
                reward.amount = 1;
            }
            else if (roll < 0.96f)
            {
                reward.type = RewardType.Gift;
                reward.item = ItemCatalog.Find("blue_flower");
                reward.amount = 1;
            }
            else
            {
                reward.type = RewardType.Gold;
                reward.amount = Random.Range(20, 60) + stageLevel * 10;
            }

            return reward;
        }

        private static RewardData GenerateStoryDrop(string drop, int stageLevel)
        {
            switch (drop)
            {
                case "Gold":
                case "EXP": // EXP itself is granted by combat; the chest tops up gold
                    return new RewardData
                    {
                        type = RewardType.Gold,
                        amount = Random.Range(20, 60) + stageLevel * 10
                    };

                case "Wood":
                    return MakeGift("wood_small", Random.Range(2, 4));

                case "Wolf Hide":
                    return MakeGift("wolf_hide", Random.Range(1, 3));

                case "Copper Ore":
                    return MakeGift("copper_ore", Random.Range(1, 3));

                case "Rare Material":
                    return MakeGift("blue_flower", 1);

                default:
                    return null; // unknown tag -> legacy table
            }
        }

        private static RewardData MakeGift(string itemId, int amount)
        {
            ItemData item = ItemCatalog.Find(itemId);
            if (item == null)
                return null;

            return new RewardData { type = RewardType.Gift, item = item, amount = amount };
        }
    }
}
