using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Data;

namespace EternalClash.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemSlot : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text slotNumberText;

        [Header("Item")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private Button button;

        private ShopThoRenController controller;

        public ItemData ItemData => itemData;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            Transform existingIcon = transform.Find("ItemIcon");
            if (icon == null && existingIcon != null)
            {
                icon = existingIcon.GetComponent<Image>() ?? existingIcon.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            button.onClick.AddListener(SelectItem);
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(SelectItem);
        }

        public void Bind(ShopThoRenController owner)
        {
            controller = owner;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            EnsureIcon();
            if (icon != null)
            {
                icon.sprite = itemData != null ? itemData.icon : null;
                icon.enabled = icon.sprite != null;
            }

            if (button != null)
                button.interactable = itemData != null;
        }

        public void SetSlotNumber(int number)
        {
            if (slotNumberText != null)
                slotNumberText.text = number > 0 ? number.ToString() : string.Empty;
        }

        private void EnsureIcon()
        {
            if (icon != null)
                return;

            Transform existingIcon = transform.Find("ItemIcon");
            if (existingIcon == null)
                return;

            icon = existingIcon.GetComponent<Image>() ?? existingIcon.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void SelectItem()
        {
            if (itemData != null)
                controller?.SelectItem(itemData);
        }
    }
}
