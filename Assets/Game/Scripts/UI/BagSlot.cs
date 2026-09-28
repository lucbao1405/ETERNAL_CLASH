using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EternalClash.Data;

namespace EternalClash.UI
{
    /// <summary>Displays one material-bag item using only its icon and stack quantity.</summary>
    public sealed class BagSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private ItemTooltipController itemDetail;

        /// <summary>Item currently displayed; null when the slot is empty.</summary>
        public ItemData CurrentItem { get; private set; }

        /// <summary>When set, clicking the slot routes to this callback instead of the tooltip (gift mode).</summary>
        public System.Action<ItemData> ClickOverride;

        private void Awake()
        {
            ResolveReferences();
            Clear();
        }

        public void Show(ItemData item, int amount)
        {
            ResolveReferences();
            CurrentItem = item != null && amount > 0 ? item : null;
            bool hasItem = CurrentItem != null;

            if (itemIcon != null)
            {
                itemIcon.sprite = hasItem ? item.icon : null;
                bool hasIcon = hasItem && item.icon != null;
                itemIcon.gameObject.SetActive(hasIcon);
                itemIcon.enabled = hasIcon;
            }

            if (quantityText != null)
            {
                quantityText.text = hasItem ? $"x{amount}" : string.Empty;
                quantityText.enabled = hasItem;
                quantityText.raycastTarget = false;
            }

            if (hasItem)
                itemDetail?.SetItem(item);
            else
                itemDetail?.ClearItem();
        }

        public void Clear() => Show(null, 0);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ClickOverride != null && CurrentItem != null)
            {
                ClickOverride(CurrentItem);
                return;
            }

            itemDetail?.ShowItemDetail();
        }

        private void ResolveReferences()
        {
            itemIcon ??= FindChildImage(transform, "ItemPic") ?? FindChildImage(transform, "Icon") ?? GetComponent<Image>();
            quantityText ??= FindChildText(transform, "Soluong") ?? FindChildText(transform, "Quantity");
            HideDuplicateQuantityLabels();
            itemDetail ??= GetComponent<ItemTooltipController>();
            itemDetail ??= gameObject.AddComponent<ItemTooltipController>();
        }

        /// <summary>
        /// Scene co the con sot ban clone "Soluong (1)" chu cung text mac dinh ("11"),
        /// che khuat so luong that. Chu viet vao label dau tien, cac ban clone trung
        /// ten bi an di de khong hien so luong cu.
        /// </summary>
        private void HideDuplicateQuantityLabels()
        {
            if (quantityText == null)
                return;
            string primary = Normalize(quantityText.gameObject.name);
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text != quantityText && Normalize(text.gameObject.name) == primary)
                    text.gameObject.SetActive(false);
            }
        }

        private static Image FindChildImage(Transform root, string name)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.transform != root && Normalize(image.gameObject.name) == Normalize(name))
                    return image;
            return null;
        }

        private static TMP_Text FindChildText(Transform root, string name)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // "Soluong (1)", "Soluong (2)"... la ban duplicate trong scene van
                // phai Khop, khong thi Show() ghi so luong vao text khong ton tai.
                string normalized = Normalize(text.gameObject.name);
                if (normalized == Normalize(name) || normalized.StartsWith(Normalize(name)))
                    return text;
            }
            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length);
            foreach (char character in value)
                if (char.IsLetterOrDigit(character))
                    result.Append(char.ToLowerInvariant(character));
            return result.ToString();
        }
    }
}
