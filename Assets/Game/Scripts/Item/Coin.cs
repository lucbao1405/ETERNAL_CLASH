using UnityEngine;

namespace EternalClash.Item
{
    public class Coin : ItemPickup
    {
        [SerializeField] private int value = 1;

        private void Awake()
        {
            itemType = ItemType.Gold;
            amount = Mathf.Max(1, value);
            magnetRange = 4f;
            moveSpeed = 8f;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                amount = Mathf.Max(1, value);
        }
    }
}
