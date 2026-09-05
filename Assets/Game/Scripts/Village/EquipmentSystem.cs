using UnityEngine;
using EternalClash.Data;
using EternalClash.Core.Save;

namespace EternalClash.Village
{
    public enum ItemSlot
    {
        Weapon,
        Armor,
        Accessory
    }

    public class EquipmentSystem : MonoBehaviour
    {
        public static EquipmentSystem Instance { get; private set; }

        [Header("Weapon Sprites by Tier")]
        [SerializeField] private Sprite[] weaponSprites;

        [Header("Armor Sprites by Tier")]
        [SerializeField] private Sprite[] armorSprites;

        [Header("Player Renderers")]
        [SerializeField] private SpriteRenderer playerWeaponRenderer;
        [SerializeField] private SpriteRenderer playerArmorRenderer;

        private ItemData equippedWeapon;
        private ItemData equippedArmor;
        private ItemData equippedAccessory;

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

        public ItemData GetEquippedItem(ItemSlot slot)
        {
            return slot switch
            {
                ItemSlot.Weapon => equippedWeapon,
                ItemSlot.Armor => equippedArmor,
                ItemSlot.Accessory => equippedAccessory,
                _ => null
            };
        }

        public void EquipItem(ItemData item)
        {
            if (item == null) return;

            if (item.weaponTier > 0)
            {
                equippedWeapon = item;
                ApplyWeaponTier(item.weaponTier);
                if (SaveManager.Instance != null)
                    SaveManager.Instance.Data.weaponTier = item.weaponTier;
            }

            if (item.armorTier > 0)
            {
                equippedArmor = item;
                ApplyArmorTier(item.armorTier);
                if (SaveManager.Instance != null)
                    SaveManager.Instance.Data.armorTier = item.armorTier;
            }

            ApplyEquipmentStats();
            SaveManager.Instance?.Save();
        }

        public void UnequipItem(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon:
                    equippedWeapon = null;
                    ApplyWeaponTier(0);
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.Data.weaponTier = 0;
                    break;
                case ItemSlot.Armor:
                    equippedArmor = null;
                    ApplyArmorTier(0);
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.Data.armorTier = 0;
                    break;
                case ItemSlot.Accessory:
                    equippedAccessory = null;
                    break;
            }

            ApplyEquipmentStats();
            SaveManager.Instance?.Save();
        }

        private void ApplyEquipmentStats()
        {
            var stats = PlayerStatSystem.Instance;
            if (stats == null) return;

            int totalStr = 0;
            int totalInt = 0;
            int totalVit = 0;
            int totalLuck = 0;

            if (equippedWeapon != null)
            {
                totalStr += equippedWeapon.strBonus;
                totalInt += equippedWeapon.intBonus;
                totalVit += equippedWeapon.vitBonus;
                totalLuck += equippedWeapon.luckBonus;
            }

            if (equippedArmor != null)
            {
                totalStr += equippedArmor.strBonus;
                totalInt += equippedArmor.intBonus;
                totalVit += equippedArmor.vitBonus;
                totalLuck += equippedArmor.luckBonus;
            }

            if (equippedAccessory != null)
            {
                totalStr += equippedAccessory.strBonus;
                totalInt += equippedAccessory.intBonus;
                totalVit += equippedAccessory.vitBonus;
                totalLuck += equippedAccessory.luckBonus;
            }

            stats.AddStrength(totalStr);
            stats.AddIntelligence(totalInt);
            stats.AddVitality(totalVit);
            stats.AddLuck(totalLuck);
        }

        public void ApplyWeaponTier(int tier)
        {
            if (weaponSprites == null || playerWeaponRenderer == null) return;
            if (tier >= 0 && tier < weaponSprites.Length)
            {
                playerWeaponRenderer.sprite = weaponSprites[tier];
                Debug.Log($"[EQUIPMENT] Weapon visual -> Tier {tier}");
            }
        }

        public void ApplyArmorTier(int tier)
        {
            if (armorSprites == null || playerArmorRenderer == null) return;
            if (tier >= 0 && tier < armorSprites.Length)
            {
                playerArmorRenderer.sprite = armorSprites[tier];
                Debug.Log($"[EQUIPMENT] Armor visual -> Tier {tier}");
            }
        }

        public void RefreshFromSave()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;
            ApplyWeaponTier(data.weaponTier);
            ApplyArmorTier(data.armorTier);
        }
    }
}
