#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.UI;
using EternalClash.Village;
using UnityEditor;
using UnityEngine;

namespace EternalClash.EditorTools
{
    /// <summary>Play Mode editor utility for changing the existing normal-item inventory.</summary>
    public sealed class ItemCheatWindow : EditorWindow
    {
        private readonly List<ItemData> eligibleItems = new List<ItemData>();
        private string[] itemNames = Array.Empty<string>();
        private int selectedItemIndex;
        private int quantity = 1;

        [MenuItem("Tools/Item Cheat")]
        private static void Open()
        {
            GetWindow<ItemCheatWindow>("Item Cheat");
        }

        private void OnEnable()
        {
            RefreshItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Item Cheat", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Gold and Diamond change SaveData.currency. Materials and consumables change SaveData.inventory.items. Enter Play Mode before modifying data.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh Items", GUILayout.Width(110f)))
                    RefreshItems();
            }

            if (eligibleItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No eligible ItemData assets were found.", MessageType.Warning);
                return;
            }

            selectedItemIndex = EditorGUILayout.Popup("Item", selectedItemIndex, itemNames);
            quantity = EditorGUILayout.IntField("Quantity", quantity);

            bool canModify = Application.isPlaying && SaveManager.Instance?.Data != null && quantity > 0;
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to modify the active SaveManager data.", MessageType.Warning);
            else if (SaveManager.Instance?.Data == null)
                EditorGUILayout.HelpBox("SaveManager is not initialized yet.", MessageType.Warning);
            else if (quantity <= 0)
                EditorGUILayout.HelpBox("Quantity must be greater than zero.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!canModify))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ADD ITEM", GUILayout.Height(32f)))
                    AddSelectedItem();
                if (GUILayout.Button("REMOVE ITEM", GUILayout.Height(32f)))
                    RemoveSelectedItem();
            }
        }

        private void RefreshItems()
        {
            string selectedItemId = selectedItemIndex >= 0 && selectedItemIndex < eligibleItems.Count
                ? eligibleItems[selectedItemIndex].itemId
                : null;

            eligibleItems.Clear();
            eligibleItems.AddRange(Resources.FindObjectsOfTypeAll<ItemData>()
                .Where(IsEligible)
                .OrderBy(item => item.itemName, StringComparer.OrdinalIgnoreCase));
            itemNames = eligibleItems.Select(item => string.IsNullOrWhiteSpace(item.itemName) ? item.itemId : item.itemName)
                .ToArray();

            selectedItemIndex = Mathf.Max(0, eligibleItems.FindIndex(item => item.itemId == selectedItemId));
            Repaint();
        }

        private void AddSelectedItem()
        {
            if (!TryGetSelection(out ItemData item, out InventorySaveData inventory))
                return;

            if (TryModifyCurrency(item, quantity))
                return;

            ItemStackSaveData stack = inventory.items.FirstOrDefault(value =>
                value != null && string.Equals(value.itemId, item.itemId, StringComparison.OrdinalIgnoreCase));
            if (stack == null)
            {
                stack = new ItemStackSaveData { itemId = item.itemId };
                inventory.items.Add(stack);
            }

            stack.amount += quantity;
            SaveCoordinator.RequestSave();
            RefreshBags();
        }

        private void RemoveSelectedItem()
        {
            if (!TryGetSelection(out ItemData item, out InventorySaveData inventory))
                return;

            if (TryModifyCurrency(item, -quantity))
                return;

            ItemStackSaveData stack = inventory.items.FirstOrDefault(value =>
                value != null && string.Equals(value.itemId, item.itemId, StringComparison.OrdinalIgnoreCase));
            if (stack == null)
                return;

            stack.amount -= quantity;
            if (stack.amount <= 0)
                inventory.items.Remove(stack);

            SaveCoordinator.RequestSave();
            RefreshBags();
        }

        private bool TryGetSelection(out ItemData item, out InventorySaveData inventory)
        {
            item = null;
            inventory = null;
            if (selectedItemIndex < 0 || selectedItemIndex >= eligibleItems.Count || quantity <= 0)
                return false;

            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return false;

            data.inventory ??= new InventorySaveData();
            data.inventory.items ??= new List<ItemStackSaveData>();
            item = eligibleItems[selectedItemIndex];
            inventory = data.inventory;
            return true;
        }

        private static bool IsEligible(ItemData item)
        {
            return item != null &&
                !string.IsNullOrWhiteSpace(item.itemId) &&
                item.equipmentSlot == EquipmentSlot.None &&
                !string.Equals(item.itemType, "Weapon", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Armor", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Shield", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.itemType, "Equipment", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryModifyCurrency(ItemData item, int delta)
        {
            if (!IsCurrency(item, out bool isGem))
                return false;

            SaveData data = SaveManager.Instance.Data;
            data.currency ??= new CurrencySaveData();
            if (isGem)
            {
                data.currency.gem = Mathf.Max(0, data.currency.gem + delta);
                data.gem = data.currency.gem;
            }
            else
            {
                data.currency.gold = Mathf.Max(0, data.currency.gold + delta);
                data.gold = data.currency.gold;
            }

            GoldSystem.Instance?.LoadFromSave(data);
            SaveCoordinator.RequestSave();
            RefreshBags();
            return true;
        }

        private static bool IsCurrency(ItemData item, out bool isGem)
        {
            isGem = false;
            if (item == null || !string.Equals(item.itemType, "Currency", StringComparison.OrdinalIgnoreCase))
                return false;

            string id = item.itemId ?? string.Empty;
            string name = item.itemName ?? string.Empty;
            isGem = id.IndexOf("gem", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("diamon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("gem", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("diamond", StringComparison.OrdinalIgnoreCase) >= 0;
            return true;
        }

        private static void RefreshBags()
        {
            foreach (BagController bag in FindObjectsOfType<BagController>(true))
                bag.Refresh();
        }
    }
}
#endif
