using UnityEngine;
using EternalClash.Data;
using EternalClash.Story;

namespace EternalClash.Stage
{
    public static class StageRewardConfig
    {
        public static RewardData GenerateStageReward(int stageLevel)
        {
            var drops = StoryManager.Instance?.GetStageDrops(stageLevel);
            
            if (drops == null || drops.Length == 0)
                return FallbackReward(stageLevel);

            string primaryDrop = drops[Random.Range(0, drops.Length)];
            
            return primaryDrop switch
            {
                "Gold" => MakeGold(stageLevel),
                "Wood" => MakeGift("wood", 2, 4),
                "Wolf Hide" => MakeGift("wolf_hide", 1, 2),
                "Copper Ore" => MakeGift("copper_ore", 1, 3),
                "Rare Material" => MakeRareMaterial(),
                "EXP" => MakeGold(stageLevel), // EXP is handled separately
                _ => FallbackReward(stageLevel)
            };
        }

        static RewardData MakeGold(int stageLevel) => new()
        {
            type = RewardType.Gold,
            amount = Random.Range(20, 60) + stageLevel * 10
        };

        static RewardData MakeGift(string itemId, int minAmount, int maxAmount) => new()
        {
            type = RewardType.Gift,
            item = ItemCatalog.Find(itemId),
            amount = Random.Range(minAmount, maxAmount + 1)
        };

        static RewardData MakeRareMaterial() => new()
        {
            type = RewardType.Gift,
            item = ItemCatalog.Find("blue_flower"),
            amount = 1
        };

        static RewardData FallbackReward(int stageLevel) => new()
        {
            type = RewardType.Gold,
            amount = Random.Range(15, 40) + stageLevel * 5
        };
    }
}
