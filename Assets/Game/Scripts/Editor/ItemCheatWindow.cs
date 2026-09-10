#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.UI;
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
                "Changes the active game's existing SaveData.inventory.items. Enter Play Mode before adding or removing items.",
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

        private static void RefreshBags()
        {
            foreach (BagController bag in FindObjectsOfType<BagController>(true))
                bag.Refresh();
        }
    }
}
#endif
