using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Data;
using EternalClash.Upgrade;
using EternalClash.Village;

namespace EternalClash.UI
{
    public sealed class ShopThoRenController : MonoBehaviour
    {
        [Header("Item Slots")]
        public ShopItemSlot[] weaponSlots;
        public ShopItemSlot[] shieldSlots;
        public ShopItemSlot[] armorSlots;

        // Existing scenes still use ShopItemButton. These fields preserve their bindings
        // while the Inspector is migrated to ShopItemSlot.
        [Header("Legacy Item Slots")]
        [SerializeField] private ShopItemButton[] listKiem = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listKhien = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listSetAoGiap = new ShopItemButton[4];

        [Header("Item Details")]
        [SerializeField] private Transform thongTinVatPham;
        [SerializeField] private TMP_Text itemDetailsText;

        [Header("Upgrade Confirmation")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private BlacksmithUpgradeConfirmUI upgradeConfirmUI;
        [SerializeField] private UpgradeResultPopup upgradeResultPopup;

        private ItemData selectedItem;

        private void Awake()
        {
            AutoWireDetails();
            upgradeButton?.onClick.AddListener(OpenUpgradeConfirm);
            BindSlots(weaponSlots);
            BindSlots(shieldSlots);
            BindSlots(armorSlots);
            BindSlots(listKiem);
            BindSlots(listKhien);
            BindSlots(listSetAoGiap);
        }

        private void OnDestroy()
        {
            upgradeButton?.onClick.RemoveListener(OpenUpgradeConfirm);
        }

        private void OnEnable()
        {
            RefreshSlots(weaponSlots);
            RefreshSlots(shieldSlots);
            RefreshSlots(armorSlots);
            RefreshSlots(listKiem);
            RefreshSlots(listKhien);
            RefreshSlots(listSetAoGiap);
        }

        public void SelectItem(ItemData itemData)
        {
            if (itemData == null)
                return;

            selectedItem = itemData;
            UpdateItemDetails(itemData);
            thongTinVatPham?.gameObject.SetActive(true);
        }

        public void OpenUpgradeConfirm()
        {
            if (selectedItem == null || upgradeConfirmUI == null)
                return;

            UpgradeRecipeData recipe = FindRecipe(selectedItem.itemId);
            thongTinVatPham?.gameObject.SetActive(false);
            upgradeConfirmUI.Show(selectedItem, recipe, Mathf.Max(1, selectedItem.upgradeLevel + 1),
                recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1, ConfirmSelectedUpgrade, ReturnToItemPreview);
        }

        private bool ConfirmSelectedUpgrade()
        {
            if (selectedItem == null)
                return false;

            ItemSlot slot = ToItemSlot(selectedItem.equipmentSlot);
            int previousUpgradeLevel = selectedItem.upgradeLevel;
            int previousStat = slot == ItemSlot.Weapon ? selectedItem.strBonus : selectedItem.vitBonus;
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

        private void UpdateItemDetails(ItemData itemData)
        {
            if (itemData == null)
                return;

            if (itemDetailsText != null)
                itemDetailsText.text = FormatItemDetails(itemData, FindRecipe(itemData.itemId));
        }

        private void AutoWireDetails()
        {
            if (thongTinVatPham == null)
                return;

            itemDetailsText ??= thongTinVatPham.Find("ContentArea/Content/TTVP_Text")?.GetComponent<TMP_Text>();
            itemDetailsText ??= thongTinVatPham.GetComponentInChildren<TMP_Text>(true);
            upgradeButton ??= transform.Find("UPGRADE")?.GetComponent<Button>();
            upgradeConfirmUI ??= transform.Find("XacNhan")?.GetComponent<BlacksmithUpgradeConfirmUI>();
            upgradeResultPopup ??= GetComponent<UpgradeResultPopup>();
        }

        private void ReturnToItemPreview()
        {
            if (selectedItem == null)
                return;

            UpdateItemDetails(selectedItem);
            thongTinVatPham?.gameObject.SetActive(true);
        }

        private void BindSlots(ShopItemButton[] slots)
        {
            if (slots == null)
                return;

            foreach (ShopItemButton slot in slots)
                if (slot != null)
                    slot.Bind(this);
        }

        private void BindSlots(ShopItemSlot[] slots)
        {
            if (slots == null)
                return;

            for (int index = 0; index < slots.Length; index++)
            {
                ShopItemSlot slot = slots[index];
                if (slot == null)
                    continue;

                slot.Bind(this);
                slot.SetSlotNumber(index + 1);
            }
        }

        private static void RefreshSlots(ShopItemButton[] slots)
        {
            if (slots == null)
                return;

            foreach (ShopItemButton slot in slots)
                if (slot != null)
                    slot.RefreshDisplay();
        }

        private static void RefreshSlots(ShopItemSlot[] slots)
        {
            if (slots == null)
                return;

            foreach (ShopItemSlot slot in slots)
                if (slot != null)
                    slot.RefreshDisplay();
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

        private static ItemSlot ToItemSlot(EquipmentSlot equipmentSlot)
        {
            return equipmentSlot == EquipmentSlot.Weapon ? ItemSlot.Weapon
                : equipmentSlot == EquipmentSlot.Armor ? ItemSlot.Armor
                : ItemSlot.Accessory;
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

        private static string FormatItemDetails(ItemData itemData, UpgradeRecipeData recipe)
        {
            StringBuilder details = new StringBuilder();
            details.AppendLine(itemData.itemName);
            if (!string.IsNullOrWhiteSpace(itemData.description))
                details.AppendLine().AppendLine(itemData.description);

            details.AppendLine().AppendLine("Stats");
            details.Append(FormatStats(itemData));
            details.AppendLine().AppendLine().AppendLine("Requires:");

            if (recipe == null)
                return details.Append('-').ToString();

            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? System.Array.Empty<UpgradeMaterialRequirement>())
                if (requirement.amount > 0)
                    details.AppendLine(GetMaterialName(requirement.materialType) + " x" + requirement.amount);

            if (recipe.goldCost > 0)
                details.AppendLine("Gold x" + recipe.goldCost);
            return details.ToString();
        }

        private static string GetMaterialName(MaterialType materialType)
        {
            return materialType == MaterialType.Ore ? "Copper Ore"
                : materialType == MaterialType.Steel ? "Steel Ore"
                : materialType.ToString();
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
