using UnityEngine;

namespace EternalClash.Village
{
    public class GoldSystem : MonoBehaviour
    {
        public static GoldSystem Instance;

        public int gold = 0;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void AddGold(int amount)
        {
            gold += amount;
            Debug.Log("[GOLD] +" + amount + " Total=" + gold);
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount)
                return false;

            gold -= amount;
            return true;
        }
    }
}
