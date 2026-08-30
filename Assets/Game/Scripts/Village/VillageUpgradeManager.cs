using UnityEngine;

namespace EternalClash.Village
{
    public class VillageUpgradeManager : MonoBehaviour
    {
        public int strengthCost = 100;
        public int intelligenceCost = 100;
        public int vitalityCost = 100;
        public int potionCost = 150;

        public PlayerStatSystem stats;
        public HealthPotionUpgradeSystem potion;

        public bool UpgradeStrength()
        {
            if (!GoldSystem.Instance.SpendGold(strengthCost)) return false;
            stats.strength++;
            return true;
        }

        public bool UpgradeIntelligence()
        {
            if (!GoldSystem.Instance.SpendGold(intelligenceCost)) return false;
            stats.intelligence++;
            return true;
        }

        public bool UpgradeVitality()
        {
            if (!GoldSystem.Instance.SpendGold(vitalityCost)) return false;
            stats.vitality++;
            return true;
        }

        public bool UpgradePotion()
        {
            if (!GoldSystem.Instance.SpendGold(potionCost)) return false;
            potion.Upgrade();
            return true;
        }
    }
}
