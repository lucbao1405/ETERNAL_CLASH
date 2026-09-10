using System;
using System.Collections.Generic;
using System.Linq;
using EternalClash.Core.Save;
using EternalClash.Data;
using UnityEngine;

namespace EternalClash.UI
{
    /// <summary>Persistent material and normal-item bag. Equipment remains in EquipmentSystem.</summary>
    public sealed class BagController : MonoBehaviour
    {
        private readonly List<BagSlot> slots = new List<BagSlot>(12);
        private readonly Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
        private SaveManager subscribedSaveManager;

        private void Awake()
        {
            DiscoverSlots();
            BuildItemLookup();
        }

        private void OnEnable()
        {
            if (slots.Count == 0)
                DiscoverSlots();
            if (itemLookup.Count == 0)
                BuildItemLookup();
            Refresh();
            SubscribeToSaveChanges();
        }

        private void Start()
        {
            SubscribeToSaveChanges();
            Refresh();
        }

        private void OnDisable()
        {
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
            subscribedSaveManager = null;
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

        private void OnSaveDataChanged(SaveData _) => Refresh();

        public bool AddItem(string itemId, int amount)
        {
            if (amount <= 0 || !TryGetBagItem(itemId, out ItemData item))
                return false;

            InventorySaveData inventory = GetInventory();
            if (inventory == null)
                return false;

            ItemStackSaveData stack = inventory.items.FirstOrDefault(value =>
                value != null && string.Equals(value.itemId, item.itemId, StringComparison.OrdinalIgnoreCase));
            if (stack == null)
            {
                stack = new ItemStackSaveData { itemId = item.itemId };
                inventory.items.Add(stack);
            }

            stack.amount += amount;
            SaveCoordinator.RequestSave();
            Refresh();
            return true;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (amount <= 0 || !HasItem(itemId, amount))
                return false;

            InventorySaveData inventory = GetInventory();
            ItemStackSaveData stack = inventory.items.First(value =>
                value != null && string.Equals(value.itemId, itemId, StringComparison.OrdinalIgnoreCase));
            stack.amount -= amount;
            if (stack.amount <= 0)
                inventory.items.Remove(stack);

            SaveCoordinator.RequestSave();
            Refresh();
            return true;
        }

        public bool HasItem(string itemId, int amount)
        {
            if (amount <= 0)
                return true;

            InventorySaveData inventory = GetInventory();
            if (inventory?.items == null)
                return false;

            ItemStackSaveData stack = inventory.items.FirstOrDefault(value =>
                value != null && string.Equals(value.itemId, itemId, StringComparison.OrdinalIgnoreCase));
            return stack != null && stack.amount >= amount;
        }

        public void Refresh()
        {
            foreach (BagSlot slot in slots)
                slot.Clear();

            InventorySaveData inventory = GetInventory();
            if (inventory?.items == null)
                return;

            int slotIndex = 0;
            foreach (ItemStackSaveData stack in inventory.items)
            {
                if (slotIndex >= slots.Count || stack == null || stack.amount <= 0 ||
                    !TryGetBagItem(stack.itemId, out ItemData item))
                    continue;

                slots[slotIndex++].Show(item, stack.amount);
            }
        }

        private InventorySaveData GetInventory()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return null;

            data.inventory ??= new InventorySaveData();
            data.inventory.items ??= new List<ItemStackSaveData>();
            return data.inventory;
        }

        private bool TryGetBagItem(string itemId, out ItemData item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (itemLookup.Count == 0)
                BuildItemLookup();
            if (!itemLookup.TryGetValue(itemId, out item))
                return false;

            return item.equipmentSlot == EquipmentSlot.None &&
                !string.Equals(item.itemType, "Currency", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Gold", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Gem", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Weapon", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Armor", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Shield", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Equipment", StringComparison.OrdinalIgnoreCase);
        }

        private void BuildItemLookup()
        {
            itemLookup.Clear();
            foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
                if (item != null && !string.IsNullOrWhiteSpace(item.itemId) && !itemLookup.ContainsKey(item.itemId))
                    itemLookup.Add(item.itemId, item);
        }

        private void DiscoverSlots()
        {
            slots.Clear();
            Transform grid = transform.Find("Item");
            if (grid == null)
                return;

            foreach (Transform child in grid)
            {
                BagSlot slot = child.GetComponent<BagSlot>();
                if (slot == null)
                    slot = child.gameObject.AddComponent<BagSlot>();
                slots.Add(slot);
            }
        }
    }
}
