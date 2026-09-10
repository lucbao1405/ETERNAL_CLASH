using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.Upgrade;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Binds the blacksmith panel to the persistent equipment and upgrade systems.
    /// </summary>
    public class BlacksmithShopUI : MonoBehaviour
    {
        [Serializable]
        private class MaterialSlotReferences
        {
            [SerializeField] private Image itemIcon;
            [SerializeField] private TMP_Text currentAmountText;
            [SerializeField] private TMP_Text requiredAmountText;

            public void Refresh(string label, int currentAmount, int requiredAmount)
            {
                if (itemIcon != null)
                    itemIcon.gameObject.name = label + "_Icon";

                if (currentAmountText != null)
                {
                    currentAmountText.text = currentAmount.ToString();
                    currentAmountText.color = currentAmount >= requiredAmount ? Color.green : Color.red;
                }

                if (requiredAmountText != null)
                    requiredAmountText.text = requiredAmount.ToString();
            }

            public void Bind(Transform root)
            {
                if (root == null)
                    return;
                itemIcon = root.Find("ItemIcon")?.GetComponent<Image>();
                currentAmountText = root.Find("CurrentAmountText")?.GetComponent<TMP_Text>();
                requiredAmountText = root.Find("RequiredAmountText")?.GetComponent<TMP_Text>();
            }
        }

        [Header("Equipment Preview")]
        [SerializeField] private Image currentEquipmentIcon;
        [SerializeField] private TMP_Text currentEquipmentNameText;
        [SerializeField] private TMP_Text currentEquipmentStatsText;
        [SerializeField] private Image nextEquipmentIcon;
        [SerializeField] private TMP_Text nextEquipmentNameText;
        [SerializeField] private TMP_Text nextEquipmentStatsText;

        [Header("Requirements")]
        [SerializeField] private MaterialSlotReferences[] materialSlots = new MaterialSlotReferences[3];
        [SerializeField] private TMP_Text goldAmountText;

        [Header("Upgrade Preview")]
        [SerializeField] private TMP_Text beforeStatsText;
        [SerializeField] private TMP_Text afterStatsText;

        [Header("Item Details")]
        [SerializeField] private Transform itemDetailsRoot;
        [SerializeField] private TMP_Text itemDetailsText;
        [SerializeField] private Image itemDetailsIcon;

        [Header("Controls")]
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        private ItemSlot selectedSlot = ItemSlot.Weapon;
        private string selectedItemId;
        private ItemData selectedItem;
        private UpgradeRecipeData selectedRecipe;
        private bool detailShown;
        private Transform weaponListRoot;
        private Transform shieldListRoot;
        private Transform armorListRoot;

        private void Awake()
        {
            AutoWireReferences();
            if (weaponButton != null) weaponButton.onClick.AddListener(OnWeaponPressed);
            if (armorButton != null) armorButton.onClick.AddListener(OnArmorPressed);
            if (shieldButton != null) shieldButton.onClick.AddListener(OnShieldPressed);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(UpgradeSelected);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (upgradeButton != null) upgradeButton.interactable = false;
            BindInventoryLists();
        }

        private void AutoWireReferences()
        {
            Transform equipment = transform.Find("EquipmentPreview");
            Transform current = equipment?.Find("CurrentEquipment");
            Transform next = equipment?.Find("NextEquipment");
            Transform legacyPreview = FindChildRecursive(transform, "ThongTinNangCap");
            itemDetailsRoot ??= FindChildRecursive(transform, "ThongTinVatPham");
            itemDetailsText ??= itemDetailsRoot?.Find("TTVP_Text")?.GetComponent<TMP_Text>();
            EnsureItemDetailsIcon();
            current ??= legacyPreview?.Find("Current") ?? legacyPreview?.Find("ThongTinVatPham");
            next ??= legacyPreview?.Find("Next");
            currentEquipmentIcon ??= current?.Find("ItemIcon")?.GetComponent<Image>();
            currentEquipmentNameText ??= current?.Find("ItemName")?.GetComponent<TMP_Text>();
            currentEquipmentStatsText ??= current?.Find("StatsText")?.GetComponent<TMP_Text>();
            nextEquipmentIcon ??= next?.Find("ItemIcon")?.GetComponent<Image>();
            nextEquipmentNameText ??= next?.Find("ItemName")?.GetComponent<TMP_Text>();
            nextEquipmentStatsText ??= next?.Find("StatsText")?.GetComponent<TMP_Text>();

            Transform requirements = transform.Find("RequirementPanel");
            Transform gold = requirements?.Find("GoldRequirement");
            goldAmountText ??= gold?.Find("GoldAmountText")?.GetComponent<TMP_Text>();
            Transform materials = requirements?.Find("MaterialRequirement");
            if (materialSlots == null || materialSlots.Length != 3)
                materialSlots = new MaterialSlotReferences[3];
            for (int i = 0; i < materialSlots.Length; i++)
            {
                materialSlots[i] ??= new MaterialSlotReferences();
                materialSlots[i].Bind(materials?.Find("MaterialSlot_" + (i + 1)));
            }

            Transform upgradePreview = transform.Find("UpgradePreview");
            upgradePreview ??= legacyPreview;
            beforeStatsText ??= upgradePreview?.Find("BeforeStats")?.GetComponent<TMP_Text>();
            afterStatsText ??= upgradePreview?.Find("AfterStats")?.GetComponent<TMP_Text>();
            weaponButton ??= transform.Find("CategoryTab/Weapon_Button")?.GetComponent<Button>();
            armorButton ??= transform.Find("CategoryTab/Armor_Button")?.GetComponent<Button>();
            shieldButton ??= transform.Find("CategoryTab/Shield_Button")?.GetComponent<Button>();
            upgradeButton ??= transform.Find("Upgrade_Button")?.GetComponent<Button>();
            closeButton ??= transform.Find("Header/Close_Button")?.GetComponent<Button>();

            BindLegacyButton("KhungKiem", ref weaponButton);
            BindLegacyButton("KhungSetAoGiap", ref armorButton);
            BindLegacyButton("KhungKhien", ref shieldButton);
            closeButton ??= FindChildRecursive(transform, "X")?.GetComponent<Button>();
        }

        private void BindLegacyButton(string objectName, ref Button field)
        {
            Transform target = FindChildRecursive(transform, objectName);
            if (target == null)
                return;
            field ??= target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        }

        private void OnEnable()
        {
            Open();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            detailShown = false;
            selectedSlot = ItemSlot.Weapon;
            selectedItemId = null;
            selectedItem = null;
            selectedRecipe = null;
            ClearDetail();
            RefreshInventory();
        }

        public void Close()
        {
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }

        public void SelectWeapon()
        {
            OnWeaponPressed();
        }

        public void SelectArmor()
        {
            OnArmorPressed();
        }

        private void OnWeaponPressed()
        {
            ClearSelection();
            detailShown = true;
            ShowDetail(ItemSlot.Weapon);
        }

        private void OnArmorPressed()
        {
            ClearSelection();
            detailShown = true;
            ShowDetail(ItemSlot.Armor);
        }

        private void OnShieldPressed()
        {
            ClearSelection();
            detailShown = true;
            ShowDetail(ItemSlot.Accessory);
        }

        private void ClearSelection()
        {
            selectedItemId = null;
            selectedItem = null;
            selectedRecipe = null;
        }

        private void UpgradeSelected()
        {
            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null || !smith.CanUpgrade(selectedSlot))
                return;
            if (smith.TryUpgrade(selectedSlot))
            {
                SaveCoordinator.RequestSave();
                RefreshAfterUpgrade();
            }
        }

        private void ShowDetail(ItemSlot slot)
        {
            selectedSlot = slot;
            RefreshEquipment();
            RefreshRequirement();
            if (upgradeButton != null)
                upgradeButton.interactable = BlacksmithCraftingSystem.Instance?.CanUpgrade(selectedSlot) == true;
        }

        private void RefreshAfterUpgrade()
        {
            // Keep the recipe selection so the details panel stays on the upgraded item.
            selectedItem = FindItemData(selectedItemId);
            RefreshEquipment();
            RefreshRequirement();
            RefreshInventory();
        }

        public void RefreshCurrentView()
        {
            if (detailShown)
            {
                RefreshEquipment();
                RefreshRequirement();
            }
            else
            {
                ClearDetail();
            }
            RefreshInventory();
        }

        private void ClearDetail()
        {
            if (currentEquipmentIcon != null)
                currentEquipmentIcon.sprite = null;
            if (nextEquipmentIcon != null)
                nextEquipmentIcon.sprite = null;
            if (currentEquipmentNameText != null)
                currentEquipmentNameText.text = "Select a weapon or armor";
            if (nextEquipmentNameText != null)
                nextEquipmentNameText.text = string.Empty;
            if (currentEquipmentStatsText != null)
                currentEquipmentStatsText.text = string.Empty;
            if (nextEquipmentStatsText != null)
                nextEquipmentStatsText.text = string.Empty;
            if (beforeStatsText != null)
                beforeStatsText.text = string.Empty;
            if (afterStatsText != null)
                afterStatsText.text = string.Empty;
            if (goldAmountText != null)
                goldAmountText.text = string.Empty;
            if (itemDetailsText != null)
                itemDetailsText.text = "Select an item";
            if (itemDetailsIcon != null)
            {
                itemDetailsIcon.sprite = null;
                itemDetailsIcon.gameObject.SetActive(false);
            }
            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }

        public void RefreshEquipment()
        {
            int tier = GetSelectedTier();
            ItemData current = selectedItem ?? ResolveItem(selectedSlot);

            RefreshItemDetails(current, tier);

            if (currentEquipmentIcon != null)
                currentEquipmentIcon.sprite = current != null ? current.icon : null;
            if (currentEquipmentNameText != null)
                currentEquipmentNameText.text = current != null ? current.itemName : GetEquipmentLabel() + " - Bậc " + tier;
            if (currentEquipmentStatsText != null)
                currentEquipmentStatsText.text = FormatStats(current);

            if (nextEquipmentNameText != null)
                nextEquipmentNameText.text = GetEquipmentLabel() + " - Bậc " + (tier + 1);
            if (nextEquipmentStatsText != null)
                nextEquipmentStatsText.text = FormatUpgradeStat(current);
            if (nextEquipmentIcon != null)
                nextEquipmentIcon.sprite = current != null ? current.icon : null;

            if (beforeStatsText != null)
                beforeStatsText.text = "Bậc " + tier + "\n" + FormatStats(current);
            if (afterStatsText != null)
                afterStatsText.text = "Bậc " + (tier + 1) + "\n" + FormatUpgradeStat(current);
        }

        public void RefreshRequirement()
        {
            UpgradeRecipeData recipe = selectedRecipe ?? BlacksmithCraftingSystem.Instance?.GetRecipe(selectedSlot);
            GoldSystem resources = GoldSystem.Instance;
            int gold = recipe != null ? recipe.goldCost : 0;
            int ore = GetRequirement(recipe, MaterialType.Ore);
            int leather = GetRequirement(recipe, MaterialType.Leather);
            int wood = GetRequirement(recipe, MaterialType.Wood);

            if (goldAmountText != null)
            {
                int currentGold = resources != null ? resources.Gold : 0;
                goldAmountText.text = currentGold + " / " + gold;
                goldAmountText.color = currentGold >= gold ? Color.green : Color.red;
            }

            if (materialSlots == null || materialSlots.Length < 3)
                return;

            materialSlots[0]?.Refresh("Ore", resources != null ? resources.OreMaterial : 0, ore);
            materialSlots[1]?.Refresh("Leather", resources != null ? resources.LeatherMaterial : 0, leather);
            materialSlots[2]?.Refresh("Wood", resources != null ? resources.WoodMaterial : 0, wood);
            if (upgradeButton != null)
                upgradeButton.interactable = BlacksmithCraftingSystem.Instance?.CanUpgrade(selectedSlot) == true;
        }

        private static int GetRequirement(UpgradeRecipeData recipe, MaterialType type)
        {
            if (recipe == null || recipe.requiredMaterials == null)
                return 0;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials)
                if (requirement.materialType == type)
                    return requirement.amount;
            return 0;
        }

        private int GetSelectedTier()
        {
            if (!string.IsNullOrEmpty(selectedItemId))
                return GetSavedItemLevel(selectedItemId, selectedItem);
            ItemData item = ResolveItem(selectedSlot);
            if (item != null)
                return GetSavedItemLevel(item.itemId, item);
            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null) return 0;
            return selectedSlot == ItemSlot.Weapon ? smith.WeaponTier : smith.ArmorTier;
        }

        private string GetEquipmentLabel()
        {
            return selectedSlot == ItemSlot.Weapon ? "Vũ khí" : selectedSlot == ItemSlot.Armor ? "Áo giáp" : "Khiên";
        }

        private static string FormatStats(ItemData item)
        {
            if (item == null) return "Chưa trang bị";
            return "STR +" + item.strBonus + " | INT +" + item.intBonus +
                   " | VIT +" + item.vitBonus + " | LUCK +" + item.luckBonus;
        }

        private string FormatUpgradeStat(ItemData item)
        {
            UpgradeRecipeData recipe = selectedRecipe ?? BlacksmithCraftingSystem.Instance?.GetRecipe(selectedSlot);
            int value = recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 0;
            if (selectedSlot == ItemSlot.Weapon)
                return "STR +" + value;
            if (selectedSlot == ItemSlot.Armor || selectedSlot == ItemSlot.Accessory)
                return "VIT +" + value;
            return "No upgrade data";
        }

        private void BindInventoryLists()
        {
            weaponListRoot = FindChildRecursive(transform, "List_Kiem");
            shieldListRoot = FindChildRecursive(transform, "List_Khien");
            armorListRoot = FindChildRecursive(transform, "List_Armor") ?? FindChildRecursive(transform, "List_SetAoGiap");
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void RefreshInventory()
        {
            ClearGeneratedSlots(weaponListRoot);
            ClearGeneratedSlots(shieldListRoot);
            ClearGeneratedSlots(armorListRoot);

            UpgradeRecipeData[] recipes = Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes");
            int weaponIndex = 0;
            int shieldIndex = 0;
            int armorIndex = 0;
            foreach (UpgradeRecipeData recipe in recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.itemId))
                    continue;
                ItemData item = FindItemData(recipe.itemId);
                EquipmentSlot equipmentSlot = item != null ? item.equipmentSlot : InferEquipmentSlot(recipe.itemId);
                if (equipmentSlot == EquipmentSlot.None)
                    continue;
                Transform list = GetListRoot(equipmentSlot);
                if (list == null)
                    continue;
                int index = equipmentSlot == EquipmentSlot.Weapon ? weaponIndex++
                    : equipmentSlot == EquipmentSlot.Shield ? shieldIndex++ : armorIndex++;
                CreateRecipeSlot(list, recipe, item, index);
            }
        }

        private static void ClearGeneratedSlots(Transform root)
        {
            if (root == null)
                return;
            for (int i = root.childCount - 1; i >= 0; i--)
                if (root.GetChild(i).name.StartsWith("BlacksmithRecipe_", StringComparison.Ordinal))
                    Destroy(root.GetChild(i).gameObject);
        }

        private Transform GetListRoot(EquipmentSlot slot) => slot == EquipmentSlot.Weapon ? weaponListRoot
            : slot == EquipmentSlot.Shield ? shieldListRoot : slot == EquipmentSlot.Armor ? armorListRoot : null;

        private void CreateRecipeSlot(Transform parent, UpgradeRecipeData recipe, ItemData item, int index)
        {
            GameObject slotObject = new GameObject("BlacksmithRecipe_" + recipe.itemId,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            slotObject.transform.SetParent(parent, false);
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(index * 150f, -8f);
            rect.sizeDelta = new Vector2(135f, 115f);
            slotObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            EquipmentSlot equipmentSlot = item != null ? item.equipmentSlot : InferEquipmentSlot(recipe.itemId);
            ItemSlot slot = ToItemSlot(equipmentSlot);
            slotObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedItemId = recipe.itemId;
                selectedItem = FindItemData(recipe.itemId);
                selectedRecipe = recipe;
                detailShown = true;
                LogSelectedItem(recipe.itemId, selectedItem);
                ShowDetail(slot);
            });
            CreateInventoryIcon(slotObject.transform, item != null ? item.icon : null);
            CreateInventoryLabel(slotObject.transform,
                item != null ? item.itemName : NewGameEquipmentDefaults.GetDisplayName(recipe.itemId));
        }

        private static ItemSlot ToItemSlot(EquipmentSlot slot) => slot == EquipmentSlot.Weapon ? ItemSlot.Weapon
            : slot == EquipmentSlot.Armor ? ItemSlot.Armor : ItemSlot.Accessory;

        private static EquipmentSlot InferEquipmentSlot(string itemId)
        {
            if (itemId.IndexOf("shield", StringComparison.OrdinalIgnoreCase) >= 0)
                return EquipmentSlot.Shield;
            if (itemId.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0)
                return EquipmentSlot.Armor;
            if (string.Equals(itemId, NewGameEquipmentDefaults.IronSwordId, StringComparison.OrdinalIgnoreCase))
                return EquipmentSlot.Weapon;
            return EquipmentSlot.None;
        }

        private static ItemData FindItemData(string itemId)
        {
            ItemData bestMatch = null;
            int bestScore = int.MinValue;
            foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
                if (item != null && string.Equals(item.itemId, itemId, StringComparison.Ordinal))
                {
                    int score = 0;
                    if (!string.IsNullOrEmpty(item.itemName) && item.itemName != item.itemId) score += 4;
                    if (item.icon != null) score += 2;
                    if (!string.IsNullOrEmpty(item.description)) score++;
                    if (item.equipmentSlot != EquipmentSlot.None) score++;
                    if (score > bestScore)
                    {
                        bestMatch = item;
                        bestScore = score;
                    }
                }
            return bestMatch ?? NewGameEquipmentDefaults.CreateItemData(itemId);
        }

        private void EnsureItemDetailsIcon()
        {
            if (itemDetailsRoot == null || itemDetailsIcon != null)
                return;

            Transform existing = itemDetailsRoot.Find("ItemIcon");
            if (existing != null)
            {
                itemDetailsIcon = existing.GetComponent<Image>();
                return;
            }

            GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(itemDetailsRoot, false);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(72f, 72f);
            itemDetailsIcon = iconObject.GetComponent<Image>();
            itemDetailsIcon.preserveAspect = true;
            itemDetailsIcon.raycastTarget = false;
            iconObject.SetActive(false);
        }

        private void RefreshItemDetails(ItemData item, int tier)
        {
            if (itemDetailsText == null)
                return;

            if (item == null)
            {
                itemDetailsText.text = "Missing ItemData";
                if (itemDetailsIcon != null)
                    itemDetailsIcon.gameObject.SetActive(false);
                return;
            }

            if (itemDetailsIcon != null)
            {
                itemDetailsIcon.sprite = item.icon;
                itemDetailsIcon.gameObject.SetActive(item.icon != null);
            }

            string description = string.IsNullOrEmpty(item.description) ? "-" : item.description;
            itemDetailsText.text = string.Format(
                "{0}\n{1} | Tier {2} | +{3}\n\nDescription\n{4}\n\nStats\nSTR bonus: +{5}\nINT bonus: +{6}\nVIT bonus: +{7}\nLUCK bonus: +{8}\n\nREQUIRES\n{9}",
                item.itemName,
                GetEquipmentLabelFor(selectedSlot),
                tier,
                GetSavedUpgradeLevel(selectedItemId ?? item.itemId, item),
                description,
                item.strBonus,
                item.intBonus,
                item.vitBonus,
                item.luckBonus,
                FormatRequirements(selectedRecipe));
        }

        private static string FormatRequirements(UpgradeRecipeData recipe)
        {
            if (recipe == null)
                return "-";

            string requirements = string.Empty;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                if (requirement.amount <= 0)
                    continue;
                if (requirements.Length > 0)
                    requirements += "\n";
                requirements += GetMaterialLabel(requirement.materialType) + " x" + requirement.amount;
            }

            if (recipe.goldCost > 0)
            {
                if (requirements.Length > 0)
                    requirements += "\n";
                requirements += "Gold " + recipe.goldCost;
            }
            return requirements.Length > 0 ? requirements : "-";
        }

        private static string GetMaterialLabel(MaterialType materialType)
        {
            return materialType == MaterialType.Ore ? "Copper Ore"
                : materialType == MaterialType.Leather ? "Leather"
                : materialType == MaterialType.Wood ? "Wood"
                : materialType == MaterialType.Steel ? "Steel Ore"
                : materialType.ToString();
        }

        private static void LogSelectedItem(string itemId, ItemData item)
        {
            if (item == null)
            {
                Debug.Log("[Blacksmith Detail]\nMissing ItemData:\n" + itemId);
                return;
            }

            Debug.Log("[Blacksmith Detail]\nItemId:\n" + itemId + "\n\nItemData:\n" + item.itemName);
        }

        private static int GetSavedUpgradeLevel(string itemId, ItemData fallback)
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data?.equipment != null)
            {
                EquipmentItemSaveData[] equipped = { data.equipment.weapon, data.equipment.armor, data.equipment.shield };
                foreach (EquipmentItemSaveData item in equipped)
                    if (item != null && item.itemId == itemId)
                        return item.upgradeLevel;
            }
            if (data?.equipmentInventory != null)
                foreach (EquipmentItemSaveData item in data.equipmentInventory)
                    if (item != null && item.itemId == itemId)
                        return item.upgradeLevel;
            return fallback != null ? fallback.upgradeLevel : 0;
        }

        private static int GetSavedItemLevel(string itemId, ItemData fallback)
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data?.equipment != null)
            {
                EquipmentItemSaveData[] equipped = { data.equipment.weapon, data.equipment.armor, data.equipment.shield };
                foreach (EquipmentItemSaveData item in equipped)
                    if (item != null && item.itemId == itemId)
                        return Mathf.Max(1, item.level);
            }
            return fallback != null ? Mathf.Max(1, fallback.level) : 1;
        }

        private static string GetEquipmentLabelFor(ItemSlot slot) =>
            slot == ItemSlot.Weapon ? "Weapon" : slot == ItemSlot.Armor ? "Armor" : "Shield";

        private static void CreateInventoryIcon(Transform parent, Sprite sprite)
        {
            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(parent, false);
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(24f, 0f);
            rect.sizeDelta = new Vector2(32f, 32f);
            icon.GetComponent<Image>().sprite = sprite;
            icon.GetComponent<Image>().preserveAspect = true;
        }

        private static ItemData ResolveItem(ItemSlot slot)
        {
            ItemData item = EquipmentSystem.Instance?.GetEquippedItem(slot);
            string itemId = SaveItemId(slot);
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                itemId = item.itemId;
            if (string.IsNullOrEmpty(itemId))
                return item;
            foreach (ItemData candidate in Resources.FindObjectsOfTypeAll<ItemData>())
                if (candidate != null && candidate.itemId == itemId)
                {
                    if (item == null)
                        return candidate;
                    item.icon = candidate.icon;
                    if (string.IsNullOrEmpty(item.itemName) || item.itemName == item.itemId)
                        item.itemName = candidate.itemName;
                    return item;
                }
            if (item != null && (string.IsNullOrEmpty(item.itemName) || item.itemName == item.itemId))
                item.itemName = NewGameEquipmentDefaults.GetDisplayName(item.itemId);
            return item;
        }

        private static string SaveItemId(ItemSlot slot)
        {
            SaveData data = SaveManager.Instance?.Data;
            EquipmentItemSaveData saved = slot == ItemSlot.Weapon ? data?.equipment?.weapon
                : slot == ItemSlot.Armor ? data?.equipment?.armor : data?.equipment?.shield;
            return saved?.itemId;
        }

        private static void CreateInventoryLabel(Transform parent, string value)
        {
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = value;
        }
    }
}
