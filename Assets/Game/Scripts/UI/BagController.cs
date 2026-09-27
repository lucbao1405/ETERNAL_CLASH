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
        private System.Action<ItemData> giftClickHandler;

        /// <summary>
        /// Gift mode: only giftable items (itemType "Gift") are shown and clicking
        /// one routes to the callback instead of the tooltip. Pass null to restore
        /// the normal bag.
        /// </summary>
        public void SetGiftMode(System.Action<ItemData> onGiftClicked)
        {
            giftClickHandler = onGiftClicked;
            RearmGiftMode();
            Refresh();
        }

        // SetGiftMode can run while the panel is still inactive (Ela menu arms it
        // before opening the bag); slots are only discovered in Awake/OnEnable, so
        // re-apply the override whenever the panel wakes up.
        private void RearmGiftMode()
        {
            System.Action<ItemData> slotOverride = giftClickHandler != null ? OnGiftSlotClicked : (System.Action<ItemData>)null;
            foreach (BagSlot slot in slots)
                slot.ClickOverride = slotOverride;
        }

        private void OnGiftSlotClicked(ItemData item) => giftClickHandler?.Invoke(item);

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
            RearmGiftMode();
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

            // Leaving the bag (closed via X, scene change) must never keep gift
            // mode armed, otherwise the next normal open would swallow clicks.
            if (giftClickHandler != null)
                SetGiftMode(null);
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
            int remaining = amount;
            for (int i = inventory.items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStackSaveData stack = inventory.items[i];
                if (stack == null || !string.Equals(stack.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                int taken = Mathf.Min(stack.amount, remaining);
                stack.amount -= taken;
                remaining -= taken;
                if (stack.amount <= 0)
                    inventory.items.RemoveAt(i);
            }

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

            int total = 0;
            foreach (ItemStackSaveData stack in inventory.items)
                if (stack != null && string.Equals(stack.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    total += stack.amount;
            return total >= amount;
        }

        private static void ConsolidateStacks(List<ItemStackSaveData> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].amount <= 0)
                    continue;
                for (int j = items.Count - 1; j > i; j--)
                {
                    if (items[j] == null || items[j].amount <= 0)
                    {
                        items.RemoveAt(j);
                        continue;
                    }
                    if (string.Equals(items[j].itemId, items[i].itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        items[i].amount += items[j].amount;
                        items.RemoveAt(j);
                    }
                }
            }
            items.RemoveAll(stack => stack == null || stack.amount <= 0);
        }

        public void Refresh()
        {
            foreach (BagSlot slot in slots)
                slot.Clear();

            InventorySaveData inventory = GetInventory();
            if (inventory?.items == null)
                return;

            // Duplicates of the same itemId (legacy saves, older writers) would
            // otherwise render as separate slots with split counts.
            ConsolidateStacks(inventory.items);

            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (ItemStackSaveData stack in inventory.items)
            {
                if (stack == null || stack.amount <= 0)
                    continue;
                if (totals.TryGetValue(stack.itemId, out int total))
                    totals[stack.itemId] = total + stack.amount;
                else
                    totals[stack.itemId] = stack.amount;
            }

            int slotIndex = 0;
            foreach (ItemStackSaveData stack in inventory.items)
            {
                if (slotIndex >= slots.Count || stack == null || stack.amount <= 0 ||
                    !TryGetBagItem(stack.itemId, out ItemData item) ||
                    !totals.TryGetValue(stack.itemId, out int total))
                    continue;

                totals.Remove(stack.itemId);
                if (giftClickHandler != null && !IsGiftable(item))
                    continue;

                slots[slotIndex++].Show(item, total);
            }
        }

        private static bool IsGiftable(ItemData item)
        {
            // Chi hoa la vat pham tang Ela (yellow_wildflower, blue_flower) —
            // la ca hai asset co itemType "Gift"; con lai la nguyen lieu craft.
            return item != null &&
                   string.Equals(item.itemType, "Gift", StringComparison.OrdinalIgnoreCase);
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
