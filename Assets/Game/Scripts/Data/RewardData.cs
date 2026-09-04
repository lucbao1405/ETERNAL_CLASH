using System;

namespace EternalClash.Data
{
    [Serializable]
    public class RewardData
    {
        public RewardType type;
        public int amount;
        public ItemData item;

        public bool HasItem => item != null;
    }

    public enum RewardType
    {
        Gold,
        Gem,
        Equipment,
        Material
    }
}
