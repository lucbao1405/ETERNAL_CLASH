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
        private readonly System.Collections.Generic.List<EquipmentItemSaveData> inventory =
            new System.Collections.Generic.List<EquipmentItemSaveData>();
        private SaveManager subscribedSaveManager;

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

        private void Start()
        {
            SubscribeToSaveChanges();
        }

        private void OnDestroy()
        {
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
        }

        private void SubscribeToSaveChanges()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == subscribedSaveManager)
                return;

            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;

            subscribedSaveManager = saveManager;
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged += OnSaveDataChanged;
        }

        private void OnSaveDataChanged(SaveData _) => RefreshFromSave();

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
            EquipItem(item, true);
        }

        private void EquipItem(ItemData item, bool addToInventory)
        {
            if (item == null) return;

            if (addToInventory)
                AddToInventory(item);

            if (GetEquipmentSlot(item) == ItemSlot.Weapon)
            {
                equippedWeapon = item;
                ApplyWeaponTier(item.weaponTier > 0 ? item.weaponTier : item.level);
                if (SaveManager.Instance != null)
                    SaveManager.Instance.Data.weaponTier = item.weaponTier > 0 ? item.weaponTier : item.level;
            }

            if (GetEquipmentSlot(item) == ItemSlot.Armor)
            {
                equippedArmor = item;
                ApplyArmorTier(item.armorTier > 0 ? item.armorTier : item.level);
                if (SaveManager.Instance != null)
                    SaveManager.Instance.Data.armorTier = item.armorTier > 0 ? item.armorTier : item.level;
            }

            if (GetEquipmentSlot(item) == ItemSlot.Accessory)
            {
                equippedAccessory = item;
            }

            ApplyEquipmentStats();
            SyncEquipmentSave();
            SaveCoordinator.RequestSave();
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
            SyncEquipmentSave();
            SaveCoordinator.RequestSave();
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

            stats.SetEquipmentBonuses(totalStr, totalInt, totalVit, totalLuck);
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
            RefreshFromSave(true);
        }

        /// <summary>
        /// Reloads persistent equipment data and recalculates player stats.
        /// Visual tier sprites are only refreshed for equip/load flows, never upgrades.
        /// </summary>
        public void RefreshFromSaveWithoutVisuals()
        {
            RefreshFromSave(false);
        }

        private void RefreshFromSave(bool refreshVisuals)
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;
            inventory.Clear();
            if (data.equipmentInventory != null)
                inventory.AddRange(data.equipmentInventory);

            data.equipment ??= new EquipmentSaveData();
            equippedWeapon = CreateItemFromSave(data.equipment.weapon, EquipmentSlot.Weapon, data.weaponTier);
            equippedArmor = CreateItemFromSave(data.equipment.armor, EquipmentSlot.Armor, data.armorTier);
            equippedAccessory = CreateItemFromSave(data.equipment.shield, EquipmentSlot.Shield, 0);
            if (refreshVisuals)
            {
                ApplyWeaponTier(data.weaponTier);
                ApplyArmorTier(data.armorTier);
            }
            ApplyEquipmentStats();
        }

        public void SyncEquipmentSave()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return;

            data.equipment ??= new EquipmentSaveData();
            WriteItemSave(data.equipment.weapon, equippedWeapon, data.weaponTier, equippedWeapon != null ? equippedWeapon.upgradeLevel : 0);
            WriteItemSave(data.equipment.armor, equippedArmor, data.armorTier, equippedArmor != null ? equippedArmor.upgradeLevel : 0);
            WriteItemSave(data.equipment.shield, equippedAccessory, 0, 0);
            data.equipmentInventory = new System.Collections.Generic.List<EquipmentItemSaveData>(inventory);
        }

        /// <summary>Stores every obtained equipment drop, including items not equipped.</summary>
        public void AddToInventory(ItemData item)
        {
            if (item == null)
                return;

            EquipmentItemSaveData snapshot = new EquipmentItemSaveData();
            WriteItemSave(snapshot, item, item.level, item.upgradeLevel);
            snapshot.slot = (int)item.equipmentSlot;
            snapshot.isNew = true;
            inventory.Add(snapshot);
        }

        public System.Collections.Generic.IReadOnlyList<EquipmentItemSaveData> Inventory => inventory;

        public bool HasNewEquipment
        {
            get
            {
                foreach (EquipmentItemSaveData item in inventory)
                    if (item.isNew)
                        return true;
                return false;
            }
        }

        public bool TryEquipInventoryItem(int index)
        {
            if (index < 0 || index >= inventory.Count)
                return false;

            EquipmentItemSaveData saved = inventory[index];
            ItemData item = CreateItemFromSave(saved, (EquipmentSlot)saved.slot, 0);
            if (item == null)
                return false;

            saved.isNew = false;
            EquipItem(item, false);
            return true;
        }

        public void MarkAllEquipmentReviewed()
        {
            foreach (EquipmentItemSaveData item in inventory)
                item.isNew = false;
            SyncEquipmentSave();
            SaveCoordinator.RequestSave();
        }

        private static void WriteItemSave(EquipmentItemSaveData target, ItemData item, int level, int upgradeLevel)
        {
            if (target == null)
                return;

            target.itemId = item != null ? item.itemId : string.Empty;
            target.slot = item != null ? (int)item.equipmentSlot : 0;
            target.level = level;
            target.upgradeLevel = upgradeLevel;
            target.strength = item != null ? item.strBonus : 0;
            target.intelligence = item != null ? item.intBonus : 0;
            target.vitality = item != null ? item.vitBonus : 0;
            target.luck = item != null ? item.luckBonus : 0;
        }

        private static ItemData CreateItemFromSave(EquipmentItemSaveData saved, EquipmentSlot slot, int legacyTier)
        {
            if (saved == null || (string.IsNullOrEmpty(saved.itemId) && saved.level <= 0))
                return null;

            ItemData template = ItemCatalog.Find(saved.itemId);
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = saved.itemId;
            item.itemName = template != null && !string.IsNullOrEmpty(template.itemName)
                ? template.itemName
                : (string.IsNullOrEmpty(saved.itemId) ? "Equipment" : saved.itemId);
            item.description = template != null ? template.description : null;
            item.icon = template != null ? template.icon : null;
            item.equipmentSlot = slot;
            item.level = Mathf.Max(1, saved.level > 0 ? saved.level : legacyTier);
            item.upgradeLevel = Mathf.Max(0, saved.upgradeLevel);
            item.strBonus = saved.strength;
            item.intBonus = saved.intelligence;
            item.vitBonus = saved.vitality;
            item.luckBonus = saved.luck;

            if (slot == EquipmentSlot.Weapon)
                item.weaponTier = item.level;
            else if (slot == EquipmentSlot.Armor)
                item.armorTier = item.level;

            return item;
        }

        private static ItemSlot GetEquipmentSlot(ItemData item)
        {
            if (item.equipmentSlot == EquipmentSlot.Weapon || item.weaponTier > 0)
                return ItemSlot.Weapon;
            if (item.equipmentSlot == EquipmentSlot.Armor || item.armorTier > 0)
                return ItemSlot.Armor;
            return item.equipmentSlot == EquipmentSlot.Shield ? ItemSlot.Accessory : ItemSlot.Accessory;
        }
    }
}
