using System;
using EternalClash.Data;

namespace EternalClash.UI
{
    [System.Serializable]
    public class ItemReward
    {
        public ItemData item;
        public int quantity;

        public ItemReward(ItemData item, int quantity = 1)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }
}