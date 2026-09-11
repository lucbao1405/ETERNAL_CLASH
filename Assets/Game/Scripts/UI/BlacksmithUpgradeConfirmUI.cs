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
    public sealed class BlacksmithUpgradeConfirmUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text tenTrangbi;
        [SerializeField] private TMP_Text chiTiet;
        [SerializeField] private TMP_Text chisoTang;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject vatPhamCan;
        [SerializeField] private Image equipmentPreviewIcon;
        [SerializeField] private MaterialSlotUI[] materialSlots = new MaterialSlotUI[3];
        [SerializeField] private ItemData copperOreItem;
        [SerializeField] private ItemData woodItem;
        [SerializeField] private ItemData leatherItem;
        [SerializeField] private ItemData goldItem;

        private Func<bool> upgradeCallback;
        private Action closeCallback;

        private void Awake()
        {
            AutoWireMaterialSlots();
            confirmButton?.onClick.AddListener(Confirm);
            closeButton?.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            confirmButton?.onClick.RemoveListener(Confirm);
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Show(ItemData itemData, UpgradeRecipeData recipe, int currentLevel, int upgradeValue, Func<bool> callback,
            Action onClose)
        {
            if (itemData == null)
                return;

            AutoWireMaterialSlots();
            upgradeCallback = callback;
            closeCallback = onClose;
            if (equipmentPreviewIcon != null)
            {
                equipmentPreviewIcon.sprite = itemData.icon;
                equipmentPreviewIcon.enabled = itemData.icon != null;
            }
            if (tenTrangbi != null)
                tenTrangbi.text = itemData.itemName;
            string upgradePreview = "Upgrade:\nLevel " + currentLevel + " -> Level " + (currentLevel + 1);
            string statIncrease = FormatStatIncrease(itemData, Mathf.Max(1, upgradeValue));
            if (chiTiet == chisoTang)
            {
                if (chiTiet != null)
                    chiTiet.text = upgradePreview + "\n" + statIncrease;
            }
            else
            {
                if (chiTiet != null)
                    chiTiet.text = upgradePreview;
                if (chisoTang != null)
                    chisoTang.text = statIncrease;
            }

            RefreshMaterials(recipe, Mathf.Max(0, currentLevel - 1));
            gameObject.SetActive(true);
        }

        public void Close()
        {
            Close(true);
        }

        private void Close(bool returnToPreview)
        {
            upgradeCallback = null;
            gameObject.SetActive(false);
            if (returnToPreview)
                closeCallback?.Invoke();
            closeCallback = null;
        }

        private void Confirm()
        {
            // Tieng rieng cua nut xac nhan (thay tieng click thuong). Nang cap thanh
            // cong thi BlacksmithCraftingSystem phat them "ItemUpgrade".
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UpgradeAccept);
            if (upgradeCallback != null && upgradeCallback())
                Close(false);
        }

        private void RefreshMaterials(UpgradeRecipeData recipe, int upgradeLevel)
        {
            int slotIndex = 0;
            if (recipe != null)
            {
                foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
                {
                    int materialCost = BlacksmithCraftingSystem.GetMaterialCost(requirement.amount, upgradeLevel);
                    if (materialCost <= 0 || slotIndex >= materialSlots.Length)
                        continue;
                    string itemId = BlacksmithCraftingSystem.GetMaterialItemId(requirement.materialType);
                    int available = BlacksmithCraftingSystem.GetMaterialAmount(SaveManager.Instance?.Data?.inventory?.items, itemId);
                    materialSlots[slotIndex++]?.Show(GetMaterialItem(requirement.materialType), materialCost, available >= materialCost);
                }

                int goldCost = BlacksmithCraftingSystem.GetGoldCost(recipe, upgradeLevel);
                if (goldCost > 0 && slotIndex < materialSlots.Length)
                {
                    int availableGold = SaveManager.Instance?.Data?.currency?.gold ?? 0;
                    materialSlots[slotIndex++]?.Show(goldItem, goldCost, availableGold >= goldCost);
                }
            }

            for (int i = slotIndex; i < materialSlots.Length; i++)
                materialSlots[i]?.Hide();

            bool hasMaterials = slotIndex > 0;
            if (vatPhamCan != null)
                vatPhamCan.SetActive(hasMaterials);
        }

        private void AutoWireMaterialSlots()
        {
            Transform materialRoot = vatPhamCan != null ? vatPhamCan.transform.Find("Hienthivp") : null;
            if (materialRoot == null)
                return;

            if (materialSlots == null || materialSlots.Length != 3)
                materialSlots = new MaterialSlotUI[3];

            for (int i = 0; i < materialSlots.Length; i++)
            {
                materialSlots[i] ??= new MaterialSlotUI();
                Transform root = materialRoot.Find((i + 1).ToString());
                materialSlots[i].Bind(root);
            }
        }

        private ItemData GetMaterialItem(MaterialType materialType)
        {
            return materialType == MaterialType.Ore ? copperOreItem
                : materialType == MaterialType.Wood ? woodItem
                : materialType == MaterialType.Leather ? leatherItem
                : null;
        }

        private static string FormatStatIncrease(ItemData itemData, int upgradeValue)
        {
            StringBuilder result = new StringBuilder();
            if (itemData.equipmentSlot == EquipmentSlot.Weapon)
                AppendIncrease(result, "STR", itemData.strBonus, upgradeValue);
            else
                AppendIncrease(result, "VIT", itemData.vitBonus, upgradeValue);
            return result.Length > 0 ? result.ToString() : "-";
        }

        private static void AppendIncrease(StringBuilder result, string label, int current, int increase)
        {
            if (current == 0)
                return;
            result.Append(label).Append(" +").Append(current)
                .Append(" -> ").Append(label).Append(" +").Append(current + increase);
        }

        [Serializable]
        private sealed class MaterialSlotUI
        {
            [SerializeField] private GameObject root;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text quantityText;

            public void Bind(Transform slotRoot)
            {
                if (slotRoot == null)
                    return;

                root ??= slotRoot.gameObject;
                icon ??= slotRoot.Find("ItemPic")?.GetComponent<Image>();
                quantityText ??= slotRoot.Find("Soluong")?.GetComponent<TMP_Text>();
            }

            public void Show(ItemData itemData, int quantity, bool enough)
            {
                if (root != null)
                    root.SetActive(true);
                ShopMaterialDisplay.Refresh(icon, quantityText, itemData, quantity, enough);
            }

            public void Hide()
            {
                if (root != null)
                    root.SetActive(false);
            }
        }
    }
}
