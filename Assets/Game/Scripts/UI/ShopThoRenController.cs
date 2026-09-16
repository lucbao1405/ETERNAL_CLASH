using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Core.Save;
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

        [Header("Legacy Item Slots")]
        [SerializeField] private ShopItemButton[] listKiem = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listKhien = new ShopItemButton[4];
        [SerializeField] private ShopItemButton[] listSetAoGiap = new ShopItemButton[4];

        [Header("Item Details")]
        [SerializeField] private Transform thongTinVatPham;
        [SerializeField] private TMP_Text itemDetailsText;

        [Header("Upgrade")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private UpgradeResultPopup upgradeResultPopup;

        [Header("Upgrade Requirements")]
        private Transform requirementSlotsRoot;
        private Image[] requirementIcons = new Image[3];
        private TMP_Text[] requirementAmountTexts = new TMP_Text[3];

        [Header("Upgrade Success")]
        [SerializeField] private GameObject upgradeSuccessPanel;

        [Header("Button Colors")]
        [SerializeField] private ColorBlock activeButtonColors = ColorBlock.defaultColorBlock;
        [SerializeField] private ColorBlock disabledButtonColors = new ColorBlock
        {
            normalColor = new Color(0.3f, 0.3f, 0.3f, 1f),
            highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
            pressedColor = new Color(0.25f, 0.25f, 0.25f, 1f),
            selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        private ItemData selectedItem;
        private SaveManager subscribedSaveManager;

        private void Awake()
        {
            AutoWireDetails();
            AutoWireRequirementSlots();
            DisableRequirementLabels();
            AutoWireLegacySlots();
            if (weaponSlots != null) BindSlots(weaponSlots);
            if (shieldSlots != null) BindSlots(shieldSlots);
            if (armorSlots != null) BindSlots(armorSlots);
            BindSlots(listKiem);
            BindSlots(listKhien);
            BindSlots(listSetAoGiap);
        }

        private void Start()
        {
            EnsureCraftingSystem();
            AutoWireDetails();
            AutoWireLegacySlots();
            RefreshSlots(listKiem);
            RefreshSlots(listKhien);
            RefreshSlots(listSetAoGiap);
        }

        private void OnDestroy()
        {
            upgradeButton?.onClick.RemoveListener(UpgradeSelected);
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
        }

        private void OnEnable()
        {
            Debug.Log("[ShopThoRen] OnEnable called");
            AutoWireDetails();
            SubscribeToSaveChanges();
            EnsureCraftingSystem();
            AutoWireLegacySlots();
            selectedItem = null;
            // Equipment services may initialize after this component's Awake.
            // Rebind the legacy slots here so their icons and click targets are
            // valid every time the panel opens.
            AutoAssignSlotItemData(listKiem, ItemSlot.Weapon);
            AutoAssignSlotItemData(listKhien, ItemSlot.Accessory);
            AutoAssignSlotItemData(listSetAoGiap, ItemSlot.Armor);
            RefreshSlots(weaponSlots);
            RefreshSlots(shieldSlots);
            RefreshSlots(armorSlots);
            RefreshSlots(listKiem);
            RefreshSlots(listKhien);
            RefreshSlots(listSetAoGiap);
            // Do not auto-select an item while opening the panel. The user
            // must select the equipment slot before Upgrade is enabled.
            UpdateUpgradeButtonState();
            EnsurePanelInteractable();
        }

        private void EnsurePanelInteractable()
        {
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null && animator.State == ShopPanelAnimator.PanelState.Closed)
                animator.Open();
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

        private void OnSaveDataChanged(SaveData _)
        {
            if (selectedItem == null)
            {
                UpdateUpgradeButtonState();
                return;
            }

            selectedItem = EquipmentSystem.Instance?.GetEquippedItem(ToItemSlot(selectedItem.equipmentSlot)) ?? selectedItem;
            RefreshItemDetail();
            RefreshRequirementSlots(selectedItem);
            UpdateUpgradeButton();
        }

        public void SelectItem(ItemData itemData)
        {
            if (itemData == null)
                return;

            // Slot buttons can hold stale instances (e.g. a copy captured before
            // the last upgrade). Always select the live equipped instance when
            // the clicked item matches it, so the displayed level is current.
            ItemSlot slot = ToItemSlot(itemData.equipmentSlot);
            ItemData live = EquipmentSystem.Instance?.GetEquippedItem(slot);
            if (live != null && live.equipmentSlot == itemData.equipmentSlot &&
                string.Equals(live.itemId, itemData.itemId, StringComparison.OrdinalIgnoreCase))
                itemData = live;

            selectedItem = itemData;
            Debug.Log("Selected upgrade item: " + itemData.itemName);
            RefreshItemDetail();
            RefreshRequirementSlots(itemData);
            thongTinVatPham?.gameObject.SetActive(true);
            UpdateUpgradeButton();
            if (upgradeButton != null)
            {
                bool canUpgrade = CanUpgradeSelected();
                upgradeButton.interactable = canUpgrade;
                upgradeButton.colors = canUpgrade ? activeButtonColors : disabledButtonColors;
                Debug.Log("[SelectItem] upgradeButton.interactable set to: " + canUpgrade);
            }
        }

        public void UpgradeSelected()
        {
            if (selectedItem == null)
            {
                Debug.Log("Can Upgrade: false - No item selected");
                UpdateUpgradeButtonState();
                return;
            }

            ItemSlot slot = ToItemSlot(selectedItem.equipmentSlot);
            ItemData equipped = EquipmentSystem.Instance?.GetEquippedItem(slot);
            if (equipped == null || equipped.equipmentSlot != selectedItem.equipmentSlot)
                equipped = selectedItem;
            BlacksmithCraftingSystem smith = EnsureCraftingSystem();
            if (smith == null)
            {
                smith = FindObjectOfType<BlacksmithCraftingSystem>(true);
                if (smith == null)
                {
                    Debug.LogError("[ShopThoRen] CraftingSystem instance is null!");
                    return;
                }
            }

            if (equipped == null)
            {
                Debug.Log("[ShopThoRen] Nothing equipped in slot " + slot + " - cannot upgrade");
                UpdateUpgradeButtonState();
                return;
            }

            if (!CanUpgradeSelected())
            {
                Debug.Log("Can Upgrade: false - Not enough resources");
                UpdateUpgradeButtonState();
                return;
            }

            Debug.Log("Can Upgrade: true - Attempting upgrade for " + equipped.itemName);

            UpgradeRecipeData recipe = FindRecipe(equipped.itemId);
            int previousUpgradeLevel = equipped.upgradeLevel;
            int previousStat = slot == ItemSlot.Weapon ? equipped.strBonus : equipped.vitBonus;
            bool success = false;

            if (recipe != null)
            {
                success = smith.TryUpgradeWithRecipe(recipe, slot);
            }
            else
            {
                BlacksmithUpgradeFlowController flow = GetComponent<BlacksmithUpgradeFlowController>()
                    ?? FindObjectOfType<BlacksmithUpgradeFlowController>();
                if (flow != null)
                    success = flow.TryPerformUpgrade(slot);
                else
                    success = smith.TryUpgrade(slot);
            }

            if (success)
            {
                SaveCoordinator.RequestSave();
                selectedItem = EquipmentSystem.Instance?.GetEquippedItem(slot) ?? selectedItem;
                ShowUpgradeSuccessPopup(selectedItem, previousUpgradeLevel, previousStat, slot, recipe);
                RefreshRequirementSlots(selectedItem);
                RefreshItemDetail();
                ActivateUpgradeSuccessPanel();
                UpdateUpgradeButton();
            }
            else
            {
                Debug.LogWarning("[ShopThoRen] Upgrade failed!");
                UpdateUpgradeButtonState();
            }
        }

        private static BlacksmithCraftingSystem EnsureCraftingSystem()
        {
            if (BlacksmithCraftingSystem.Instance != null)
                return BlacksmithCraftingSystem.Instance;

            BlacksmithCraftingSystem existing = FindObjectOfType<BlacksmithCraftingSystem>(true);
            if (existing != null)
                return existing;

            GameObject serviceObject = new GameObject("BlacksmithCraftingSystem");
            return serviceObject.AddComponent<BlacksmithCraftingSystem>();
        }

        private void ShowUpgradeSuccessPopup(ItemData item, int previousUpgradeLevel, int previousStat,
            ItemSlot slot, UpgradeRecipeData recipe)
        {
            if (item == null || upgradeResultPopup == null)
                return;

            int upgradeValue = recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1;
            string statName = slot == ItemSlot.Weapon ? "STR" : "VIT";
            upgradeResultPopup.ShowSuccess(item, previousUpgradeLevel,
                previousUpgradeLevel + upgradeValue, statName, previousStat, previousStat + upgradeValue);
        }

        public void RefreshItemDetail()
        {
            UpdateItemDetails(selectedItem);
        }

        public void UpdateUpgradeButton()
        {
            UpdateUpgradeButtonState();
        }

        private void UpdateUpgradeButtonState()
        {
            if (upgradeButton == null)
                return;

            bool canUpgrade = CanUpgradeSelected();
            upgradeButton.interactable = canUpgrade;
            upgradeButton.colors = canUpgrade ? activeButtonColors : disabledButtonColors;
        }

        private bool CanUpgradeSelected()
        {
            if (selectedItem == null)
            {
                Debug.LogWarning("[CanUpgradeSelected] selectedItem is NULL");
                return false;
            }

            BlacksmithCraftingSystem smith = EnsureCraftingSystem();
            if (smith == null)
            {
                Debug.LogWarning("[CanUpgradeSelected] smith is NULL");
                return false;
            }

            ItemSlot slot = ToItemSlot(selectedItem.equipmentSlot);
            Debug.Log("[CanUpgradeSelected] slot=" + slot + " selectedItem.itemId=" + selectedItem.itemId);

            ItemData equipped = EquipmentSystem.Instance?.GetEquippedItem(slot);
            Debug.Log("[CanUpgradeSelected] equipped from EquipmentSystem: " + (equipped != null ? equipped.itemId + " icon=" + (equipped.icon != null ? equipped.icon.name : "NULL") : "NULL"));

            if (equipped == null || equipped.equipmentSlot != selectedItem.equipmentSlot)
            {
                Debug.Log("[CanUpgradeSelected] falling back to selectedItem");
                equipped = selectedItem;
            }

            string lookupId = equipped.itemId;
            Debug.Log("[CanUpgradeSelected] lookupId=" + lookupId + " normalized=" + NormalizeItemId(lookupId));

            UpgradeRecipeData recipe = FindRecipe(lookupId);
            if (recipe == null)
            {
                Debug.LogWarning("[CanUpgradeSelected] recipe is NULL for itemId=" + lookupId + " - Normalized=" + NormalizeItemId(lookupId));
                Debug.Log("[CanUpgradeSelected] Checking available recipes...");
                foreach (UpgradeRecipeData r in Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes"))
                    if (r != null)
                        Debug.Log("  Available: " + r.itemId + " -> Normalized: " + NormalizeItemId(r.itemId));
                return false;
            }

            int upgradeLevel = GetSavedUpgradeLevel(lookupId, equipped);
            bool canUpgrade = smith.CanUpgradeWithRecipe(recipe, upgradeLevel);
            Debug.Log("[CanUpgradeSelected] " + equipped.itemName + " level " + upgradeLevel + ": " + canUpgrade);
            return canUpgrade;
        }

        private void UpdateItemDetails(ItemData itemData)
        {
            if (itemData == null)
            {
                if (itemDetailsText != null)
                    itemDetailsText.text = string.Empty;
                if (upgradeButton != null)
                    upgradeButton.interactable = false;
                return;
            }

            if (itemDetailsText != null)
                itemDetailsText.text = FormatItemDetails(itemData, FindRecipe(itemData.itemId));
        }

        private void AutoWireDetails()
        {
            // A reference dragged from another panel (e.g. Shop_Phu_Thuy's UPGRADE
            // button) silently steals the click; only accept a button inside this panel.
            if (upgradeButton != null && !upgradeButton.transform.IsChildOf(transform))
                upgradeButton = null;
            if (thongTinVatPham != null)
            {
                itemDetailsText ??= thongTinVatPham.Find("ContentArea/Content/TTVP_Text")?.GetComponent<TMP_Text>();
                itemDetailsText ??= thongTinVatPham.GetComponentInChildren<TMP_Text>(true);
            }
            upgradeButton ??= transform.Find("UPGRADE")?.GetComponent<Button>();
            upgradeButton ??= FindChildComponent<Button>(transform, "UPGRADE");
            upgradeResultPopup ??= GetComponent<UpgradeResultPopup>();
            upgradeResultPopup ??= GetComponentInChildren<UpgradeResultPopup>(true);
            upgradeSuccessPanel ??= transform.Find("UpGradeSuccess")?.gameObject;
            upgradeSuccessPanel ??= FindChildRecursive(transform, "UpGradeSuccess")?.gameObject;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(UpgradeSelected);
                upgradeButton.onClick.AddListener(UpgradeSelected);
                Debug.Log("[ShopThoRen] UPGRADE button bound: " + upgradeButton.name);
            }
            else
                Debug.LogWarning("[ShopThoRen] Could not find UPGRADE button in panel hierarchy.");
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = FindChildRecursive(root, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void AutoWireLegacySlots()
        {
            if (listKiem != null) AutoAssignSlotItemData(listKiem, ItemSlot.Weapon);
            if (listKhien != null) AutoAssignSlotItemData(listKhien, ItemSlot.Accessory);
            if (listSetAoGiap != null) AutoAssignSlotItemData(listSetAoGiap, ItemSlot.Armor);
        }

        private static void AutoAssignSlotItemData(ShopItemButton[] slots, ItemSlot slot)
        {
            if (slots == null || slots.Length == 0)
                return;

            // Blacksmith shows only the item currently equipped in this slot.
            // Never manufacture tier variants or fall back to catalog defaults:
            // an empty equipment slot must remain visually empty and unusable.
            ItemData equipped = EquipmentSystem.Instance?.GetEquippedItem(slot);
            EquipmentSlot expectedSlot = slot == ItemSlot.Weapon ? EquipmentSlot.Weapon
                : slot == ItemSlot.Armor ? EquipmentSlot.Armor : EquipmentSlot.Shield;
            if (equipped != null && equipped.equipmentSlot != expectedSlot)
                equipped = null;
            slots[0].SetItemData(equipped);
            for (int index = 1; index < slots.Length; index++)
                slots[index].SetItemData(null);
        }

        private void AutoWireRequirementSlots()
        {
            Transform requirementPanel = transform.Find("Vat_Pham_Can");
            // The live town hierarchy uses Hientv. Hienthivp is retained only as
            // a fallback for older scenes and is never preferred over Hientv.
            Transform slotsRoot = requirementPanel != null ? requirementPanel.Find("Hientv") : null;
            slotsRoot = slotsRoot != null ? slotsRoot : requirementPanel?.Find("Hienthivp");
            slotsRoot = slotsRoot != null ? slotsRoot : FindChildRecursive(transform, "Hientv");
            slotsRoot = slotsRoot != null ? slotsRoot : FindChildRecursive(transform, "Hienthivp");
            requirementSlotsRoot = slotsRoot;
            if (slotsRoot == null)
                return;

            requirementIcons ??= new Image[3];
            requirementAmountTexts ??= new TMP_Text[3];
            for (int index = 0; index < requirementIcons.Length; index++)
            {
                Transform slot = slotsRoot.Find("Slot_" + (index + 1));
                slot = slot != null ? slot : slotsRoot.Find((index + 1).ToString());
                slot = slot != null ? slot : slotsRoot.Find("MaterialSlot_" + (index + 1));
                if (slot == null)
                    continue;

                requirementIcons[index] ??= slot.Find("ItemIcon")?.GetComponent<Image>();
                requirementIcons[index] ??= slot.Find("ItemPic")?.GetComponent<Image>();
                requirementIcons[index] ??= slot.GetComponentInChildren<Image>(true);
                requirementAmountTexts[index] ??= slot.Find("Soluong")?.GetComponent<TMP_Text>();
                requirementAmountTexts[index] ??= slot.Find("RequiredAmountText")?.GetComponent<TMP_Text>();
                requirementAmountTexts[index] ??= slot.GetComponentInChildren<TMP_Text>(true);
                slot.gameObject.SetActive(false);
            }
        }

        public void RefreshRequirementSlots(ItemData item)
        {
            AutoWireRequirementSlots();
            ClearRequirementSlots();
            if (item == null)
            {
                Debug.Log("[RefreshRequirementSlots] item is null");
                return;
            }

            ItemSlot slot = ToItemSlot(item.equipmentSlot);
            ItemData equipped = EquipmentSystem.Instance?.GetEquippedItem(slot);
            string lookupId = equipped != null ? equipped.itemId : item.itemId;

            UpgradeRecipeData recipe = FindRecipe(lookupId);
            if (recipe == null)
            {
                Debug.LogWarning("[RefreshRequirementSlots] No recipe for: " + lookupId);
                return;
            }

            int upgradeLevel = GetSavedUpgradeLevel(lookupId, equipped ?? item);
            Debug.Log("[RefreshRequirementSlots] " + lookupId + " level=" + upgradeLevel + " materials=" + (recipe.requiredMaterials?.Length ?? 0));

            int slotIndex = 0;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                int amount = BlacksmithCraftingSystem.GetMaterialCost(requirement.amount, upgradeLevel);
                if (amount <= 0 || slotIndex >= requirementIcons.Length)
                    continue;

                string materialItemId = BlacksmithCraftingSystem.GetMaterialItemId(requirement.materialType);
                SetRequirementSlot(slotIndex++, materialItemId, amount);
            }

            if (recipe.goldCost > 0 && slotIndex < requirementIcons.Length)
            {
                int goldCost = BlacksmithCraftingSystem.GetGoldCost(recipe, upgradeLevel);
                SetRequirementSlot(slotIndex, "coin", goldCost);
            }
        }

        private void ClearRequirementSlots()
        {
            Transform slotsRoot = ResolveRequirementSlotsRoot();
            if (slotsRoot != null)
                for (int index = 0; index < slotsRoot.childCount; index++)
                    slotsRoot.GetChild(index).gameObject.SetActive(false);

            requirementIcons ??= Array.Empty<Image>();
            requirementAmountTexts ??= Array.Empty<TMP_Text>();
            for (int index = 0; index < requirementIcons.Length; index++)
            {
                if (requirementIcons[index] != null)
                    requirementIcons[index].sprite = null;
                if (index < requirementAmountTexts.Length && requirementAmountTexts[index] != null)
                    requirementAmountTexts[index].text = string.Empty;
            }
        }

        private void SetRequirementSlot(int index, string itemId, int amount)
        {
            if (index < 0 || index >= requirementIcons.Length)
                return;

            Transform slotsRoot = ResolveRequirementSlotsRoot();
            Transform slot = slotsRoot?.Find("Slot_" + (index + 1))
                ?? slotsRoot?.Find((index + 1).ToString())
                ?? slotsRoot?.Find("MaterialSlot_" + (index + 1));
            slot?.gameObject.SetActive(true);
            if (requirementIcons[index] != null)
            {
                Sprite icon = ItemCatalog.Find(itemId)?.icon;
                requirementIcons[index].sprite = icon;
                requirementIcons[index].gameObject.SetActive(icon != null);
            }
            if (index < requirementAmountTexts.Length && requirementAmountTexts[index] != null)
            {
                requirementAmountTexts[index].text = "x" + amount;
                requirementAmountTexts[index].gameObject.SetActive(true);
            }
            Debug.Log("[SetRequirementSlot] Slot " + (index+1) + ": " + itemId + " x" + amount);
        }

        private Transform ResolveRequirementSlotsRoot()
        {
            if (requirementSlotsRoot != null)
                return requirementSlotsRoot;

            Transform requirementPanel = transform.Find("Vat_Pham_Can");
            requirementSlotsRoot = requirementPanel != null ? requirementPanel.Find("Hientv") : null;
            requirementSlotsRoot = requirementSlotsRoot != null
                ? requirementSlotsRoot : requirementPanel?.Find("Hienthivp");
            requirementSlotsRoot = requirementSlotsRoot != null
                ? requirementSlotsRoot
                : FindChildRecursive(transform, "Hientv");
            requirementSlotsRoot = requirementSlotsRoot != null
                ? requirementSlotsRoot
                : FindChildRecursive(transform, "Hienthivp");
            if (requirementSlotsRoot != null && (requirementIcons == null || requirementAmountTexts == null))
                AutoWireRequirementSlots();
            return requirementSlotsRoot;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform result = FindChildRecursive(root.GetChild(index), childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static int GetSavedUpgradeLevel(string itemId, ItemData fallback)
        {
            SaveData data = SaveManager.Instance?.Data;
            EquipmentItemSaveData[] equipped =
            {
                data?.equipment?.weapon,
                data?.equipment?.armor,
                data?.equipment?.shield
            };

            foreach (EquipmentItemSaveData item in equipped)
                if (item != null && string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    return item.upgradeLevel;

            return fallback != null ? fallback.upgradeLevel : 0;
        }

        private void DisableRequirementLabels()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, "REQUIRES", StringComparison.OrdinalIgnoreCase))
                    child.gameObject.SetActive(false);
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
            details.AppendLine("Upgrade Level: +" + itemData.upgradeLevel);
            if (!string.IsNullOrWhiteSpace(itemData.description))
                details.AppendLine().AppendLine(itemData.description);

            details.AppendLine().AppendLine("Stats");
            details.Append(FormatStats(itemData));

            if (recipe != null)
            {
                int upgradeValue = Mathf.Max(1, recipe.upgradeValue);
                details.AppendLine().AppendLine("After Upgrade");
                if (itemData.equipmentSlot == EquipmentSlot.Weapon)
                    details.Append("STR: +" + (itemData.strBonus + upgradeValue));
                else
                    details.Append("VIT: +" + (itemData.vitBonus + upgradeValue));
            }
            return details.ToString();
        }

        private static void AppendStat(StringBuilder result, string label, int amount)
        {
            if (amount == 0)
                return;
            if (result.Length > 0)
                result.AppendLine();
            result.Append(label).Append(" +").Append(amount);
        }

        private void ActivateUpgradeSuccessPanel()
        {
            upgradeResultPopup?.gameObject.SetActive(true);
            if (upgradeSuccessPanel != null)
            {
                upgradeSuccessPanel.SetActive(true);
                Debug.Log("[ShopThoRen] UpGradeSuccess panel activated");
            }
        }
    }
}
