using UnityEngine;
using EternalClash.Data;

namespace EternalClash.Stage
{
    // Phan thuong chi rot ra tu 5 vat pham hien co: Gold, Gem, Ore, Leather, Wood.
    // Khong sinh trang bi (giap/kiem) lam phan thuong nhat duoc.
    public static class RewardGenerator
    {
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
            else
            {
                reward.type = RewardType.Gem;
                reward.amount = Random.Range(1, 3);
            }

            return reward;
        }
    }
}
