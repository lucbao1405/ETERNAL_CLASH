using EternalClash.Item;
using UnityEngine;

namespace EternalClash.Loot
{
    [System.Serializable]
    public class LootData
    {
        public string itemId;
        public Sprite itemSprite;
        public GameObject prefab;
        [Range(0f, 1f)]
        public float dropRate = 1f;
        [Min(1)]
        public int minAmount = 1;
        [Min(1)]
        public int maxAmount = 1;
        public ItemType itemType;
    }
}
