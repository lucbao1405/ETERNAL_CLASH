using UnityEngine;

namespace EternalClash.Village
{
    public class HealthPotionUpgradeSystem : MonoBehaviour
    {
        public int potionLevel = 1;
        public int potionCount = 3;
        public int healAmount = 50;

        public void Upgrade()
        {
            potionLevel++;
            potionCount++;
            healAmount += 25;
        }
    }
}
