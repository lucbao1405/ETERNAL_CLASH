using UnityEngine;

namespace EternalClash.Village
{
    public class PlayerStatSystem : MonoBehaviour
    {
        public int strength = 1;
        public int intelligence = 1;
        public int vitality = 1;
        public int luck = 0;

        public int maxHp = 100;
        public int damage = 10;

        public void UpgradeStrength()
        {
            strength++;
            damage += 5;
        }

        public void UpgradeIntelligence()
        {
            intelligence++;
            // reserved for skill damage / cooldown effects
        }

        public void UpgradeVitality()
        {
            vitality++;
            maxHp += 20;
        }

        public void UpgradeLuck()
        {
            luck++;
        }
    }
}
