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
    /// <summary>
    /// Binds the blacksmith panel to the persistent equipment and upgrade systems.
    /// </summary>
    public class BlacksmithShopUI : MonoBehaviour
    {
        [Serializable]
        private class MaterialSlotReferences
        {
            private GameObject slotObject;
            [SerializeField] private Image itemIcon;
            [SerializeField] private TMP_Text requiredAmountText;

            public void Clear()
            {
                if (itemIcon != null)
                {
                    itemIcon.sprite = null;
                    itemIcon.gameObject.SetActive(false);
                }

                if (requiredAmountText != null)
                {
                    requiredAmountText.text = string.Empty;
                    requiredAmountText.gameObject.SetActive(false);
                }

                SetActive(false);
            }

            public void SetIcon(Sprite icon)
            {
                if (itemIcon == null)
                    return;
                itemIcon.sprite = icon;
                itemIcon.gameObject.SetActive(icon != null);
            }

            public void SetAmount(int amount)
            {
                if (requiredAmountText != null)
                {
                    requiredAmountText.text = "x" + amount;
                    requiredAmountText.gameObject.SetActive(true);
                }
            }

            public void SetActive(bool active)
            {
                if (slotObject != null)
                    slotObject.SetActive(active);
            }

            public void Bind(Transform root)
            {
                if (root == null)
                    return;
                slotObject = root.gameObject;
                itemIcon = root.Find("ItemIcon")?.GetComponent<Image>()
                    ?? root.Find("ItemPic")?.GetComponent<Image>()
                    ?? root.GetComponentInChildren<Image>(true);
                requiredAmountText = root.Find("RequiredAmountText")?.GetComponent<TMP_Text>()
                    ?? root.Find("Soluong")?.GetComponent<TMP_Text>()
                    ?? root.GetComponentInChildren<TMP_Text>(true);
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
        [SerializeField] private ScrollRect itemDetailsScrollRect;

        [Header("Controls")]
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        [Header("Upgrade Button Colors")]
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

        private ItemSlot selectedSlot = ItemSlot.Weapon;
        private string selectedItemId;
        private ItemData selectedItem;
        private UpgradeRecipeData selectedRecipe;
        private bool detailShown;
        private Transform weaponListRoot;
        private Transform shieldListRoot;
        private Transform armorListRoot;
        private SaveManager subscribedSaveManager;

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
            itemDetailsScrollRect ??= itemDetailsRoot?.Find("ContentArea")?.GetComponent<ScrollRect>();
            EnsureItemDetailsScrollRect();
            EnsureItemDetailsIcon();
            current ??= legacyPreview?.Find("Current") ?? legacyPreview?.Find("ThongTinVatPham");
            next ??= legacyPreview?.Find("Next");
            currentEquipmentIcon ??= current?.Find("ItemIcon")?.GetComponent<Image>();
            currentEquipmentNameText ??= current?.Find("ItemName")?.GetComponent<TMP_Text>();
            currentEquipmentStatsText ??= current?.Find("StatsText")?.GetComponent<TMP_Text>();
            nextEquipmentIcon ??= next?.Find("ItemIcon")?.GetComponent<Image>();
            nextEquipmentNameText ??= next?.Find("ItemName")?.GetComponent<TMP_Text>();
            nextEquipmentStatsText ??= next?.Find("StatsText")?.GetComponent<TMP_Text>();

            Transform requirements = transform.Find("RequirementPanel")
                ?? FindChildRecursive(transform, "VatPham_Can")
                ?? FindChildRecursive(transform, "Vat_Pham_Can");
            Transform gold = requirements?.Find("GoldRequirement");
            goldAmountText ??= gold?.Find("GoldAmountText")?.GetComponent<TMP_Text>();
            Transform materials = requirements?.Find("MaterialRequirement") ?? requirements?.Find("Hientv")
                ?? requirements?.Find("Hienthivp") ?? FindChildRecursive(requirements, "Hientv")
                ?? FindChildRecursive(requirements, "Hienthivp");
            if (materialSlots == null || materialSlots.Length != 3)
                materialSlots = new MaterialSlotReferences[3];
            for (int i = 0; i < materialSlots.Length; i++)
            {
                materialSlots[i] ??= new MaterialSlotReferences();
                materialSlots[i].Bind(materials?.Find("MaterialSlot_" + (i + 1))
                    ?? materials?.Find((i + 1).ToString()));
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
            DisableRequirementLabels();

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
            SubscribeToSaveChanges();
            Open();
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
            RefreshCurrentView();
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
            ClearRequirementSlots();
            RefreshInventory();
        }

        public void Close()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }

        private void OnWeaponPressed()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ClearSelection();
            detailShown = true;
            ShowDetail(ItemSlot.Weapon);
        }

        private void OnArmorPressed()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ClearSelection();
            detailShown = true;
            ShowDetail(ItemSlot.Armor);
        }

        private void OnShieldPressed()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
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
            if (selectedItem == null || !CanUpgrade())
            {
                Debug.Log("Can Upgrade: false");
                UpdateUpgradeButtonState();
                return;
            }

            Debug.Log("Can Upgrade: true - Attempting upgrade for " + selectedItem.itemName);

            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null)
            {
                Debug.LogError("[BLACKSMITH] CraftingSystem instance is null!");
                return;
            }

            bool success = false;
            if (selectedRecipe != null)
            {
                Debug.Log("[UpgradeSelected] Using selectedRecipe: " + selectedRecipe.itemId);
                success = smith.TryUpgradeWithRecipe(selectedRecipe, selectedSlot);
            }
            else
            {
                Debug.Log("[UpgradeSelected] No selectedRecipe, using slot-based upgrade");
                BlacksmithUpgradeFlowController flow = GetComponent<BlacksmithUpgradeFlowController>()
                    ?? FindObjectOfType<BlacksmithUpgradeFlowController>();
                if (flow != null)
                    success = flow.TryPerformUpgrade(selectedSlot);
                else
                    success = smith.TryUpgrade(selectedSlot);
            }

            if (success)
            {
                SaveCoordinator.RequestSave();
                RefreshAfterUpgrade();
            }
            else
            {
                Debug.LogWarning("[UpgradeSelected] Upgrade failed!");
                UpdateUpgradeButtonState();
            }
        }

        private void ShowDetail(ItemSlot slot)
        {
            selectedSlot = slot;
            selectedItem ??= ResolveItem(slot);
            selectedItemId ??= selectedItem?.itemId;

            if (selectedRecipe == null && selectedItem != null)
            {
                selectedRecipe = FindRecipeForItem(selectedItem.itemId) ?? BlacksmithCraftingSystem.Instance?.GetRecipe(slot);
            }
            else if (selectedRecipe == null)
            {
                selectedRecipe = BlacksmithCraftingSystem.Instance?.GetRecipe(slot);
            }

            Debug.Log("[ShowDetail] slot=" + slot + " item=" + selectedItem?.itemName + " recipe=" + selectedRecipe?.itemId);

            RefreshEquipment();
            RefreshRequirementSlots(selectedItem ?? ResolveItem(slot));
            UpdateUpgradeButtonState();
        }

        private UpgradeRecipeData FindRecipeForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            UpgradeRecipeData[] recipes = Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes");
            foreach (UpgradeRecipeData recipe in recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.itemId))
                    continue;
                if (string.Equals(recipe.itemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeRecipeItemId(recipe.itemId), NormalizeRecipeItemId(itemId), StringComparison.OrdinalIgnoreCase))
                    return recipe;
            }
            return null;
        }

        private static string NormalizeRecipeItemId(string itemId)
        {
            int tierMarker = itemId.LastIndexOf("_t", StringComparison.OrdinalIgnoreCase);
            if (tierMarker >= 0 && int.TryParse(itemId.Substring(tierMarker + 2), out _))
                return itemId.Substring(0, tierMarker);
            return itemId;
        }

        private void RefreshAfterUpgrade()
        {
            // Keep the recipe selection so the details panel stays on the upgraded item.
            selectedItem = ResolveItem(selectedSlot);
            selectedItemId = selectedItem?.itemId;
            RefreshEquipment();
            RefreshRequirementSlots(selectedItem);
            RefreshInventory();
            UpdateUpgradeButtonState();
        }

        public void RefreshCurrentView()
        {
            if (detailShown)
            {
                RefreshEquipment();
                RefreshRequirementSlots(selectedItem ?? ResolveItem(selectedSlot));
            }
            else
            {
                ClearDetail();
            }
            RefreshInventory();
            UpdateUpgradeButtonState();
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
            if (itemDetailsScrollRect != null)
                itemDetailsScrollRect.verticalNormalizedPosition = 1f;
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

        public void RefreshRequirementSlots(ItemData item)
        {
            ClearRequirementSlots();

            if (item == null)
            {
                Debug.Log("[RefreshRequirementSlots] item is null");
                UpdateUpgradeButtonState();
                return;
            }

            UpgradeRecipeData recipe = selectedRecipe ?? FindRecipeForItem(item.itemId) ?? BlacksmithCraftingSystem.Instance?.GetRecipe(selectedSlot);
            if (recipe == null)
            {
                Debug.LogWarning("[RefreshRequirementSlots] No recipe found for item: " + item.itemId);
                UpdateUpgradeButtonState();
                return;
            }

            Debug.Log("Requirement count: " + (recipe.requiredMaterials?.Length ?? 0) + " for " + recipe.itemId);

            SaveData saveData = SaveManager.Instance?.Data;
            int upgradeLevel = GetSavedUpgradeLevel(selectedItemId ?? item.itemId, item);
            int gold = BlacksmithCraftingSystem.GetGoldCost(recipe, upgradeLevel);

            if (goldAmountText != null)
            {
                goldAmountText.text = "x" + gold;
            }

            int slotIndex = 0;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
            {
                int requiredAmount = BlacksmithCraftingSystem.GetMaterialCost(requirement.amount, upgradeLevel);
                if (requiredAmount <= 0 || materialSlots == null || slotIndex >= materialSlots.Length)
                    continue;

                string materialItemId = BlacksmithCraftingSystem.GetMaterialItemId(requirement.materialType);
                ItemData material = ItemCatalog.Find(materialItemId);
                Debug.Log("[RefreshRequirementSlots] Material " + slotIndex + ": " + materialItemId + " x" + requiredAmount +
                    (material != null ? " (has icon)" : " (NO ICON!)"));
                materialSlots[slotIndex].SetIcon(material?.icon);
                materialSlots[slotIndex].SetAmount(requiredAmount);
                materialSlots[slotIndex].SetActive(true);
                slotIndex++;
            }
            UpdateUpgradeButtonState();
        }

        private void ClearRequirementSlots()
        {
            if (materialSlots == null)
                return;
            foreach (MaterialSlotReferences slot in materialSlots)
                slot?.Clear();
        }

        private void DisableRequirementLabels()
        {
            Transform[] labels = GetComponentsInChildren<Transform>(true);
            foreach (Transform label in labels)
                if (string.Equals(label.name, "REQUIRES", StringComparison.OrdinalIgnoreCase))
                    label.gameObject.SetActive(false);
        }

        private void UpdateUpgradeButtonState()
        {
            if (upgradeButton == null)
                return;

            bool canUpgrade = CanUpgrade();
            upgradeButton.interactable = canUpgrade;
            upgradeButton.colors = canUpgrade ? activeButtonColors : disabledButtonColors;
        }

        private bool CanUpgrade()
        {
            if (selectedItem == null)
                return false;

            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null)
                return false;

            if (selectedRecipe != null)
            {
                int upgradeLevel = GetSavedUpgradeLevel(selectedItemId ?? selectedItem.itemId, selectedItem);
                bool canUpgrade = smith.CanUpgradeWithRecipe(selectedRecipe, upgradeLevel);
                Debug.Log("[CanUpgrade] With recipe " + selectedRecipe.itemId + " level " + upgradeLevel + ": " + canUpgrade);
                return canUpgrade;
            }

            return smith.CanUpgrade(selectedSlot);
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
                EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
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

        private void EnsureItemDetailsScrollRect()
        {
            if (itemDetailsScrollRect != null || itemDetailsRoot == null)
                return;

            RectTransform textRect = itemDetailsText?.rectTransform;
            if (textRect == null)
                return;

            ScrollRect scroll = itemDetailsRoot.GetComponent<ScrollRect>();
            if (scroll == null)
                scroll = itemDetailsRoot.gameObject.AddComponent<ScrollRect>();

            scroll.viewport = null;
            scroll.content = textRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 30f;
            scroll.elasticity = 0.1f;

            if (itemDetailsRoot.GetComponent<Mask>() == null)
                itemDetailsRoot.gameObject.AddComponent<Mask>();

            Image maskImage = itemDetailsRoot.GetComponent<Image>();
            if (maskImage == null)
                maskImage = itemDetailsRoot.gameObject.AddComponent<Image>();
            maskImage.color = Color.clear;

            itemDetailsScrollRect = scroll;

            if (itemDetailsScrollRect.GetComponent<UIScrollSound>() == null)
                itemDetailsScrollRect.gameObject.AddComponent<UIScrollSound>();
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
                ResetItemDetailsScroll();
                return;
            }

            if (itemDetailsIcon != null)
            {
                itemDetailsIcon.sprite = item.icon;
                itemDetailsIcon.gameObject.SetActive(item.icon != null);
            }

            string description = string.IsNullOrEmpty(item.description) ? "-" : item.description;
            int upgradeLevel = GetSavedUpgradeLevel(selectedItemId ?? item.itemId, item);
            int upgradeValue = selectedRecipe != null ? Mathf.Max(1, selectedRecipe.upgradeValue) : 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(item.itemName);
            sb.AppendLine("Upgrade Level: +" + upgradeLevel);
            if (!string.IsNullOrEmpty(description) && description != "-")
                sb.AppendLine().AppendLine(description);

            sb.AppendLine().AppendLine("Stats");
            AppendStat(sb, "STR", item.strBonus);
            AppendStat(sb, "INT", item.intBonus);
            AppendStat(sb, "VIT", item.vitBonus);
            AppendStat(sb, "LUCK", item.luckBonus);

            if (upgradeValue > 0)
            {
                sb.AppendLine().AppendLine("After Upgrade");
                if (selectedSlot == ItemSlot.Weapon)
                    sb.Append("STR: +" + (item.strBonus + upgradeValue));
                else
                    sb.Append("VIT: +" + (item.vitBonus + upgradeValue));
            }

            itemDetailsText.text = sb.ToString();

            ResetItemDetailsScroll();
        }

        private void ResetItemDetailsScroll()
        {
            if (itemDetailsText == null)
                return;

            if (itemDetailsScrollRect != null)
                itemDetailsScrollRect.verticalNormalizedPosition = 1f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemDetailsText.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        private static void AppendStat(StringBuilder result, string label, int amount)
        {
            if (amount == 0)
                return;
            if (result.Length > 0)
                result.AppendLine();
            result.Append(label).Append(" +").Append(amount);
        }

        private static void LogSelectedItem(string itemId, ItemData item)
        {
            if (item == null)
            {
                Debug.Log("[Blacksmith Detail]\nMissing ItemData:\n" + itemId);
                return;
            }

            Debug.Log("[Blacksmith Detail]\nItemId:\n" + itemId + "\n\nItemData:\n" + item.itemName);
            Debug.Log("Selected upgrade item: " + item.itemName);
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
