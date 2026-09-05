using UnityEngine;
using EternalClash.Core.Save;

namespace EternalClash.Village
{
    public class BlacksmithCraftingSystem : MonoBehaviour
    {
        public static BlacksmithCraftingSystem Instance { get; private set; }

        public int WeaponTier { get; private set; }
        public int ArmorTier { get; private set; }

        public const int TIER1_GOLD_COST = 100;
        public const int TIER1_ORE_COST = 5;
        public const int TIER1_LEATHER_COST = 3;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool CanUpgradeWeapon()
        {
            if (GoldSystem.Instance == null) return false;
            int tier = WeaponTier + 1;
            return GoldSystem.Instance.Gold >= TIER1_GOLD_COST * tier
                && GoldSystem.Instance.OreMaterial >= TIER1_ORE_COST * tier
                && GoldSystem.Instance.LeatherMaterial >= TIER1_LEATHER_COST * tier;
        }

        public bool CanUpgradeArmor()
        {
            if (GoldSystem.Instance == null) return false;
            int tier = ArmorTier + 1;
            return GoldSystem.Instance.Gold >= TIER1_GOLD_COST * tier
                && GoldSystem.Instance.OreMaterial >= TIER1_ORE_COST * tier
                && GoldSystem.Instance.LeatherMaterial >= TIER1_LEATHER_COST * tier;
        }

        public void UpgradeWeapon()
        {
            if (!CanUpgradeWeapon()) return;
            int tier = WeaponTier + 1;
            GoldSystem.Instance.SpendGold(TIER1_GOLD_COST * tier);
            GoldSystem.Instance.SpendMaterials(TIER1_ORE_COST * tier, TIER1_LEATHER_COST * tier);
            WeaponTier = tier;

            PlayerStatSystem.Instance?.ApplyWeaponTierBonus(WeaponTier);
            EquipmentSystem.Instance?.ApplyWeaponTier(WeaponTier);

            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.weaponTier = WeaponTier;
                SaveManager.Instance.Save();
            }
            Debug.Log($"[BLACKSMITH] Weapon upgraded to Tier {WeaponTier}");
        }

        public void UpgradeArmor()
        {
            if (!CanUpgradeArmor()) return;
            int tier = ArmorTier + 1;
            GoldSystem.Instance.SpendGold(TIER1_GOLD_COST * tier);
            GoldSystem.Instance.SpendMaterials(TIER1_ORE_COST * tier, TIER1_LEATHER_COST * tier);
            ArmorTier = tier;

            PlayerStatSystem.Instance?.ApplyArmorTierBonus(ArmorTier);
            EquipmentSystem.Instance?.ApplyArmorTier(ArmorTier);

            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.armorTier = ArmorTier;
                SaveManager.Instance.Save();
            }
            Debug.Log($"[BLACKSMITH] Armor upgraded to Tier {ArmorTier}");
        }

        public void LoadFromSave(SaveData data)
        {
            WeaponTier = data.weaponTier;
            ArmorTier = data.armorTier;
        }
    }
}
