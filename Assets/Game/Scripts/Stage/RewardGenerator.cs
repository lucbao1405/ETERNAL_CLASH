using UnityEngine;
using EternalClash.Data;

namespace EternalClash.Stage
{
    // Phan thuong ruong: 85% la la cay nguyen lieu nang cap phu thuy
    // (xanh = heal, do = hoi chieu, vang = khien). Phan con lai: hoa bond
    // (hoa cuc vang, hoa xanh) voi ti le thap va thuong cu (vang, gem).
    public static class RewardGenerator
    {
        private static readonly string[] WitchLeafItemIds = { "leaf_green", "leaf_red", "leaf_yellow" };

        public static RewardData GenerateStageReward(int stageLevel)
        {
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
            else if (roll < 0.95f)
            {
                reward.type = RewardType.Gift;
                reward.item = ItemCatalog.Find("blue_flower");
                reward.amount = 1;
            }
            else if (roll < 0.975f)
            {
                reward.type = RewardType.Gold;
                reward.amount = Random.Range(20, 60) + stageLevel * 10;
            }
            else
            {
                reward.type = RewardType.Gem;
                reward.amount = Random.Range(1, 3);
            }

            return reward;
        }
    }
}
