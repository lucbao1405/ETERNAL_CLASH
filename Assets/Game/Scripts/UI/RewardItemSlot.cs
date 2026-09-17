using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Data;

namespace EternalClash.UI
{
    /// <summary>
    /// One reward item slot inside the Win/Lose popup grid.
    /// Holds an ItemPic image and a Soluong text label.
    /// </summary>
    public class RewardItemSlot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image itemPic;
        [SerializeField] private TMP_Text soluongText;
        [SerializeField] private ItemTooltipController tooltipController;

        public Image ItemPic => itemPic;
        public TMP_Text SoluongText => soluongText;

        private void Awake()
        {
            if (itemPic == null)
            {
                Image childPic = FindChildImage(transform, "Icon");
                if (childPic == null)
                    childPic = FindChildImage(transform, "ItemPic");
                itemPic = childPic != null ? childPic : GetComponent<Image>();
            }

            if (soluongText == null)
            {
                soluongText = FindChildText(transform, "Quantity");
                if (soluongText == null)
                    soluongText = FindChildText(transform, "Soluong");
            }
            if (soluongText != null)
            {
                soluongText.raycastTarget = false;
                FitQuantityLabel(soluongText);
            }

            if (tooltipController == null)
                tooltipController = GetComponent<ItemTooltipController>();

            DisableItemNameLabels();
        }

        private const float QuantityMinWidth = 100f;
        private const float QuantityMinFontSize = 20f;

        /// <summary>
        /// Khung Soluong trong scene chi rong ~39px (vua chu mau "11") va dang bat tu
        /// xuong dong, nen "x45" bi ngat thanh 2 dong va tran xuong duoi o.
        /// Giu mep phai cua khung (goc duoi phai icon), noi rong sang trai, can phai,
        /// khong xuong dong va tu thu nho chu khi so qua dai.
        /// </summary>
        private static void FitQuantityLabel(TMP_Text label)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopRight;

            float maxSize = label.fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = Mathf.Min(QuantityMinFontSize, maxSize);

            RectTransform rect = label.rectTransform;
            if (rect.sizeDelta.x < QuantityMinWidth)
            {
                float rightEdge = rect.anchoredPosition.x + rect.sizeDelta.x * (1f - rect.pivot.x);
                rect.sizeDelta = new Vector2(QuantityMinWidth, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(
                    rightEdge - QuantityMinWidth * (1f - rect.pivot.x),
                    rect.anchoredPosition.y);
            }
        }

        private static TMP_Text FindChildText(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (Normalize(child.name) == Normalize(name))
                {
                    TMP_Text text = child.GetComponent<TMP_Text>();
                    if (text != null)
                        return text;
                }

                TMP_Text nested = FindChildText(child, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private void DisableItemNameLabels()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && Normalize(text.gameObject.name) == "itemname")
                    text.gameObject.SetActive(false);
            }
        }

        // Slot "1..8": root Image là khung slot, icon thật nằm ở Image con "ItemPic".
        private static Image FindChildImage(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (Normalize(child.name) == Normalize(name))
                    return child.GetComponent<Image>();
                Image nested = FindChildImage(child, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var buffer = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    buffer.Append(char.ToLowerInvariant(c));
            }
            return buffer.ToString();
        }

        public void SetSprite(Sprite sprite)
        {
            if (itemPic != null)
            {
                itemPic.enabled = sprite != null;
                itemPic.sprite = sprite;
                // Icon trong Data/Items co ti le khac nhau, giu nguyen ti le de khong bi meo.
                itemPic.preserveAspect = true;
            }
        }

        public void SetItem(ItemData item)
        {
            if (tooltipController == null)
                tooltipController = GetComponent<ItemTooltipController>();
            tooltipController?.SetItem(item);
        }

        public void SetQuantity(int quantity)
        {
            if (soluongText != null)
            {
                soluongText.enabled = quantity > 0;
                soluongText.text = quantity > 0 ? $"x{quantity}" : string.Empty;
            }
        }

        public void Clear()
        {
            if (itemPic != null)
            {
                itemPic.enabled = false;
                itemPic.sprite = null;
            }

            if (soluongText != null)
            {
                soluongText.text = string.Empty;
                soluongText.enabled = false;
            }

            tooltipController?.ClearItem();

            gameObject.SetActive(false);
        }
    }
}
