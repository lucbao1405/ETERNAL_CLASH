using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Data;

namespace EternalClash.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private ItemData itemData;

        private ShopThoRenController controller;

        public ItemData ItemData => itemData;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            EnsureIcon();

            // Chua co item thi an o icon. Truoc day o icon vua tao (chua co anh) van
            // hien nen ve ra mot o TRANG - thay ro o bang Skill trong Town, noi cac o
            // cung gan script nay nhung khong bao gio duoc Bind() item.
            HideIconIfEmpty();

            button.onClick.AddListener(SelectItem);
        }

        private void HideIconIfEmpty()
        {
            if (icon == null)
                return;

            bool hasSprite = itemData != null && icon.sprite != null;
            icon.enabled = hasSprite;
            icon.gameObject.SetActive(hasSprite);
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

        public void SetItemData(ItemData value)
        {
            itemData = value;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            EnsureIcon();
            if (icon != null)
            {
                Sprite sprite = itemData != null ? itemData.icon : null;
                if (sprite == null && itemData != null)
                    sprite = ItemCatalog.Find(itemData.itemId)?.icon;
                icon.sprite = sprite;
                icon.enabled = itemData != null && sprite != null;
                icon.gameObject.SetActive(itemData != null && sprite != null);
            }

            if (tierText != null)
                tierText.text = itemData != null ? "T" + GetTier(itemData) : string.Empty;

            if (button != null)
                button.interactable = itemData != null;
        }

        private void EnsureIcon()
        {
            if (icon != null)
                return;

            Transform existingIcon = transform.Find("ItemIcon");
            if (existingIcon != null)
            {
                // The town shop already contains an ItemIcon child. Add the missing
                // Image component there instead of creating an icon elsewhere.
                icon = existingIcon.GetComponent<Image>() ?? existingIcon.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                return;
            }

            // Legacy town slots do not contain an icon child in the scene.
            // Create one so serialized items are visible without scene rework.
            GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -12f);
            icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void SelectItem()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            if (itemData != null)
                controller?.SelectItem(itemData);
        }

        private static int GetTier(ItemData item)
        {
            if (item.equipmentSlot == EquipmentSlot.Weapon)
                return Mathf.Max(1, item.weaponTier);
            if (item.equipmentSlot == EquipmentSlot.Armor)
                return Mathf.Max(1, item.armorTier);
            return Mathf.Max(1, item.stars);
        }
    }
}
