using System;
using System.Text;
using TMPro;
using UnityEngine;
using EternalClash.Data;
using EternalClash.Upgrade;
using EternalClash.Village;

namespace EternalClash.UI
{
    public sealed class ShopThoRenController : MonoBehaviour
    {
        [Header("Item Slots")]
        [SerializeField] private ShopItemButton[] listKiem = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listKhien = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listSetAoGiap = new ShopItemButton[4];

        [Header("Item Details")]
        [SerializeField] private Transform thongTinVatPham;
        [SerializeField] private TMP_Text itemDetailsText;
        [SerializeField] private UnityEngine.UI.Button previewUpgradeButton;

        [Header("Upgrade Confirmation")]
        [SerializeField] private BlacksmithUpgradeConfirmUI upgradeConfirmUI;
        [SerializeField] private UpgradeResultPopup upgradeResultPopup;

        private ItemData selectedItem;

        private void Awake()
        {
            AutoWireDetails();
            upgradeResultPopup ??= GetComponent<UpgradeResultPopup>();
            previewUpgradeButton?.onClick.AddListener(OpenUpgradeConfirm);
            BindSlots(listKiem);
            BindSlots(listKhien);
            BindSlots(listSetAoGiap);
            ValidateShieldItemData();
        }

        private void OnEnable()
        {
            RefreshSlots(listKiem);
            RefreshSlots(listKhien);
            RefreshSlots(listSetAoGiap);
            ValidateShieldItemData();
        }

        private void OnDestroy()
        {
            if (previewUpgradeButton != null)
                previewUpgradeButton.onClick.RemoveListener(OpenUpgradeConfirm);
        }

        public void SelectItem(ItemData itemData)
        {
            if (itemData == null)
                return;

            selectedItem = itemData;
            UpdateStats(itemData);
            thongTinVatPham?.gameObject.SetActive(true);
            previewUpgradeButton?.gameObject.SetActive(true);
        }

        public void OpenUpgradeConfirm()
        {
            if (selectedItem == null || upgradeConfirmUI == null)
                return;

            UpgradeRecipeData recipe = FindRecipe(selectedItem.itemId);
            thongTinVatPham?.gameObject.SetActive(false);
            previewUpgradeButton?.gameObject.SetActive(false);
            upgradeConfirmUI.Show(selectedItem, recipe, Mathf.Max(1, selectedItem.upgradeLevel + 1),
                recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1, ConfirmSelectedUpgrade, ReturnToItemPreview);
        }

        private bool ConfirmSelectedUpgrade()
        {
            if (selectedItem == null)
            {
                upgradeResultPopup?.ShowFailure("No item selected.");
                return true;
            }

            ItemSlot slot = ToItemSlot(selectedItem.equipmentSlot);
            int previousUpgradeLevel = selectedItem.upgradeLevel;
            int previousStat = GetUpgradeableStat(selectedItem, slot);
            UpgradeRecipeData recipe = FindRecipe(selectedItem.itemId);
            int upgradeValue = recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1;
            BlacksmithCraftingSystem blacksmith = BlacksmithCraftingSystem.Instance;
            if (blacksmith == null || !blacksmith.TryUpgrade(slot))
            {
                upgradeResultPopup?.ShowFailure("Not enough material.");
                return true;
            }

            string statName = slot == ItemSlot.Weapon ? "STR" : "VIT";
            upgradeResultPopup?.ShowSuccess(selectedItem.itemName, previousUpgradeLevel,
                previousUpgradeLevel + upgradeValue, statName, previousStat, previousStat + upgradeValue);
            return true;
        }

        private static int GetUpgradeableStat(ItemData itemData, ItemSlot slot)
        {
            if (itemData == null)
                return 0;

            return slot == ItemSlot.Weapon ? itemData.strBonus : itemData.vitBonus;
        }

        public void UpdateStats(ItemData itemData)
        {
            if (itemData == null)
                return;

            if (itemDetailsText != null)
                itemDetailsText.text = itemData.itemName + "\n\nCurrent:\n" + FormatStats(itemData) +
                    "\n\nAfter Upgrade:\n" + FormatUpgradePreview(itemData);
        }

        private void AutoWireDetails()
        {
            if (thongTinVatPham == null)
                return;

            itemDetailsText ??= thongTinVatPham.GetComponentInChildren<TMP_Text>(true);
            previewUpgradeButton ??= transform.Find("UPGRADE")?.GetComponent<UnityEngine.UI.Button>();
        }

        private void ReturnToItemPreview()
        {
            if (selectedItem == null)
                return;

            UpdateStats(selectedItem);
            thongTinVatPham?.gameObject.SetActive(true);
            previewUpgradeButton?.gameObject.SetActive(true);
        }

        private void BindSlots(ShopItemButton[] slots)
        {
            if (slots == null)
                return;

            foreach (ShopItemButton slot in slots)
                if (slot != null)
                    slot.Bind(this);
        }

        private static void RefreshSlots(ShopItemButton[] slots)
        {
            if (slots == null)
                return;

            foreach (ShopItemButton slot in slots)
                if (slot != null)
                    slot.RefreshDisplay();
        }

        private void ValidateShieldItemData()
        {
            if (listKhien == null)
                return;

            foreach (ShopItemButton slot in listKhien)
            {
                ItemData itemData = slot != null ? slot.ItemData : null;
                if (itemData != null &&
                    itemData.itemType == "Equipment" &&
                    itemData.equipmentSlot == EquipmentSlot.Shield &&
                    string.Equals(itemData.itemId, "iron_shield_t1", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            Debug.LogWarning("Missing Shield ItemData", this);
        }

        private static ItemSlot ToItemSlot(EquipmentSlot equipmentSlot)
        {
            return equipmentSlot == EquipmentSlot.Weapon ? ItemSlot.Weapon
                : equipmentSlot == EquipmentSlot.Armor ? ItemSlot.Armor
                : ItemSlot.Accessory;
        }

        private static UpgradeRecipeData FindRecipe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            string normalizedItemId = NormalizeItemId(itemId);
            foreach (UpgradeRecipeData recipe in Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes"))
                if (recipe != null && string.Equals(NormalizeItemId(recipe.itemId), normalizedItemId,
                    StringComparison.OrdinalIgnoreCase))
                    return recipe;
            return null;
        }

        private static string NormalizeItemId(string itemId)
        {
            int tierMarker = itemId.LastIndexOf("_t", StringComparison.OrdinalIgnoreCase);
            if (tierMarker >= 0 && int.TryParse(itemId.Substring(tierMarker + 2), out _))
                return itemId.Substring(0, tierMarker);
            return itemId;
        }

        private static string FormatStats(ItemData itemData)
        {
            StringBuilder result = new StringBuilder();
            AppendStat(result, "STR", itemData.strBonus);
            AppendStat(result, "INT", itemData.intBonus);
            AppendStat(result, "VIT", itemData.vitBonus);
            AppendStat(result, "LUCK", itemData.luckBonus);
            return result.Length > 0 ? result.ToString() : "-";
        }

        private static string FormatUpgradePreview(ItemData itemData)
        {
            UpgradeRecipeData recipe = FindRecipe(itemData.itemId);
            int upgradeValue = recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1;
            ItemSlot slot = ToItemSlot(itemData.equipmentSlot);
            int current = GetUpgradeableStat(itemData, slot);
            string statName = slot == ItemSlot.Weapon ? "STR" : "VIT";
            return statName + " +" + current + " -> " + statName + " +" + (current + upgradeValue);
        }

        private static void AppendStat(StringBuilder result, string label, int amount)
        {
            if (amount == 0)
                return;
            if (result.Length > 0)
                result.AppendLine();
            result.Append(label).Append(" +").Append(amount);
        }

    }
}
