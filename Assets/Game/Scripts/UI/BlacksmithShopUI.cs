using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Reference-only UI for the blacksmith shop. It previews existing equipment
    /// and resource values without performing an upgrade or changing save data.
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

        [Header("Controls")]
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        private ItemSlot selectedSlot = ItemSlot.Weapon;

        private void Awake()
        {
            AutoWireReferences();
            if (weaponButton != null) weaponButton.onClick.AddListener(SelectWeapon);
            if (armorButton != null) armorButton.onClick.AddListener(SelectArmor);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        private void AutoWireReferences()
        {
            Transform equipment = transform.Find("EquipmentPreview");
            Transform current = equipment?.Find("CurrentEquipment");
            Transform next = equipment?.Find("NextEquipment");
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
            beforeStatsText ??= upgradePreview?.Find("BeforeStats")?.GetComponent<TMP_Text>();
            afterStatsText ??= upgradePreview?.Find("AfterStats")?.GetComponent<TMP_Text>();
            weaponButton ??= transform.Find("CategoryTab/Weapon_Button")?.GetComponent<Button>();
            armorButton ??= transform.Find("CategoryTab/Armor_Button")?.GetComponent<Button>();
            upgradeButton ??= transform.Find("Upgrade_Button")?.GetComponent<Button>();
            closeButton ??= transform.Find("Header/Close_Button")?.GetComponent<Button>();
        }

        private void OnEnable()
        {
            Open();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            SelectWeapon();
        }

        public void Close()
        {
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }

        public void SelectWeapon()
        {
            selectedSlot = ItemSlot.Weapon;
            RefreshEquipment();
            RefreshRequirement();
        }

        public void SelectArmor()
        {
            selectedSlot = ItemSlot.Armor;
            RefreshEquipment();
            RefreshRequirement();
        }

        public void RefreshEquipment()
        {
            int tier = GetSelectedTier();
            ItemData current = EquipmentSystem.Instance?.GetEquippedItem(selectedSlot);

            if (currentEquipmentIcon != null)
                currentEquipmentIcon.sprite = current != null ? current.icon : null;
            if (currentEquipmentNameText != null)
                currentEquipmentNameText.text = current != null ? current.itemName : GetEquipmentLabel() + " - Bậc " + tier;
            if (currentEquipmentStatsText != null)
                currentEquipmentStatsText.text = FormatStats(current);

            if (nextEquipmentNameText != null)
                nextEquipmentNameText.text = GetEquipmentLabel() + " - Bậc " + (tier + 1);
            if (nextEquipmentStatsText != null)
                nextEquipmentStatsText.text = "Xem trước nâng cấp";
            if (nextEquipmentIcon != null)
                nextEquipmentIcon.sprite = null;

            if (beforeStatsText != null)
                beforeStatsText.text = "Bậc " + tier + "\n" + FormatStats(current);
            if (afterStatsText != null)
                afterStatsText.text = "Bậc " + (tier + 1) + "\nChỉ số sau nâng cấp";
        }

        public void RefreshRequirement()
        {
            int tier = GetSelectedTier() + 1;
            int gold = BlacksmithCraftingSystem.TIER1_GOLD_COST * tier;
            int ore = BlacksmithCraftingSystem.TIER1_ORE_COST * tier;
            int leather = BlacksmithCraftingSystem.TIER1_LEATHER_COST * tier;
            GoldSystem resources = GoldSystem.Instance;

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
            materialSlots[2]?.Refresh("Wood", resources != null ? resources.WoodMaterial : 0, 0);
        }

        private int GetSelectedTier()
        {
            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            if (smith == null) return 0;
            return selectedSlot == ItemSlot.Weapon ? smith.WeaponTier : smith.ArmorTier;
        }

        private string GetEquipmentLabel()
        {
            return selectedSlot == ItemSlot.Weapon ? "Vũ khí" : "Áo giáp";
        }

        private static string FormatStats(ItemData item)
        {
            if (item == null) return "Chưa trang bị";
            return "STR +" + item.strBonus + " | INT +" + item.intBonus +
                   " | VIT +" + item.vitBonus + " | LUCK +" + item.luckBonus;
        }
    }
}
