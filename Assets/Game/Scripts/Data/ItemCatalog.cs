using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.Data
{
    /// <summary>
    /// Danh muc tat ca ItemData (Data/Items) de code luc chay tra cuu duoc.
    /// Data/Items khong nam trong Resources nen khong Load truc tiep duoc; file
    /// Resources/ItemCatalog.asset chi giu tham chieu toi chung.
    /// Them item moi: keo file ItemData vao danh sach "Items" cua ItemCatalog.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Game/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        public const string ResourcePath = "ItemCatalog";

        /// <summary>
        /// Ma phan thuong dung trong tran (BattleRewardData.CreateEntry) -> itemId
        /// trong Data/Items, cho nhung ma khong trung ten voi file ItemData.
        /// </summary>
        private static readonly Dictionary<string, string> RewardKeyToItemId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Gold", "coin" },
                { "Gem", "diamon" },
                { "Ore", "copper_ore" },
                { "Leather", "wolf_hide" },
                { "Wood", "wood_small" }
            };

        [SerializeField] private List<ItemData> items = new List<ItemData>();

        private static ItemCatalog instance;
        private static bool loadAttempted;

        public static ItemCatalog Instance
        {
            get
            {
                if (instance == null && !loadAttempted)
                {
                    loadAttempted = true;
                    instance = Resources.Load<ItemCatalog>(ResourcePath);
                    if (instance == null)
                        Debug.LogWarning("[ItemCatalog] Khong tim thay Resources/" + ResourcePath +
                                         " - vat pham se hien thi khong co hinh.");
                }
                return instance;
            }
        }

        /// <summary>
        /// Tim ItemData theo ma phan thuong ("Gold", "Ore"...), itemId, ten file hoac itemName.
        /// </summary>
        public static ItemData Find(string key)
        {
            ItemCatalog catalog = Instance;
            if (catalog == null || string.IsNullOrEmpty(key))
                return null;

            string itemId = RewardKeyToItemId.TryGetValue(key, out string mapped) ? mapped : key;

            foreach (ItemData item in catalog.items)
            {
                if (item == null)
                    continue;

                if (string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.name, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.itemName, key, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        /// <summary>Item dau tien trong danh muc thuoc o trang bi nay (Weapon/Shield/Armor).</summary>
        public static ItemData FindBySlot(EquipmentSlot slot)
        {
            ItemCatalog catalog = Instance;
            if (catalog == null || slot == EquipmentSlot.None)
                return null;

            foreach (ItemData item in catalog.items)
            {
                if (item != null && item.equipmentSlot == slot)
                    return item;
            }

            return null;
        }
    }
}
