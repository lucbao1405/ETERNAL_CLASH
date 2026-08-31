namespace EternalClash.Village
{
    public class BlacksmithCraftingSystem
    {
        public static BlacksmithCraftingSystem Instance { get; private set; }

        public int WeaponTier { get; set; }
        public int ArmorTier { get; set; }

        public const int TIER1_GOLD_COST = 100;
        public const int TIER1_ORE_COST = 5;
        public const int TIER1_LEATHER_COST = 3;

        public bool CanUpgradeWeapon() => false;
        public bool CanUpgradeArmor() => false;
        public void UpgradeWeapon() {}
        public void UpgradeArmor() {}
    }
}
