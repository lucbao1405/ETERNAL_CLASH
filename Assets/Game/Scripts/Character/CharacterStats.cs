using UnityEngine;

namespace EternalClash.Character
{
    [System.Serializable]
    public class CharacterStats
    {
        public int level = 1;
        public int maxHealth = 100;
        public int attack = 10;
        public int defense = 5;
        public float criticalRate = 0.05f;

        public void IncreaseLevel()
        {
            level++;
            maxHealth += 20;
            attack += 5;
            defense += 2;
        }
    }
}
