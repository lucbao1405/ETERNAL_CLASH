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

        private void Awake()
        {
            ResolveReferences();
            Clear();
        }

        public void Show(ItemData item, int amount)
        {
            ResolveReferences();
            bool hasItem = item != null && amount > 0;

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
            itemDetail?.ShowItemDetail();
        }

        private void ResolveReferences()
        {
            itemIcon ??= FindChildImage(transform, "ItemPic") ?? FindChildImage(transform, "Icon") ?? GetComponent<Image>();
            quantityText ??= FindChildText(transform, "Soluong") ?? FindChildText(transform, "Quantity");
            itemDetail ??= GetComponent<ItemTooltipController>();
            itemDetail ??= gameObject.AddComponent<ItemTooltipController>();
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
                if (Normalize(text.gameObject.name) == Normalize(name))
                    return text;
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
