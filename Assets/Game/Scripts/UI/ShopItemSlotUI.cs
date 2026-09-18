using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    public class ShopItemSlotUI : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text quantity;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text priceText;

        public void SetItem(string displayName, int amount, int price, Sprite icon = null)
        {
            itemName ??= transform.Find("ItemName")?.GetComponent<TMP_Text>();
            quantity ??= transform.Find("QuantityPill/Quantity")?.GetComponent<TMP_Text>();
            quantity ??= transform.Find("Quantity")?.GetComponent<TMP_Text>();
            itemIcon ??= transform.Find("ItemIcon")?.GetComponent<Image>();
            priceText ??= transform.Find("BuyButton/PriceText")?.GetComponent<TMP_Text>();
            if (itemName != null) itemName.text = displayName;
            if (quantity != null) quantity.text = amount.ToString("N0");
            // Gia hien so va icon tien te ben canh (dung icon rieng, khong them chu "G").
            if (priceText != null) priceText.text = price.ToString("#,##0").Replace(',', '.');
            if (itemIcon != null) itemIcon.sprite = icon;
        }

        /// <summary>
        /// Dong bo card khi ban bang IAP (tien that): chi doi ten, so luong va
        /// gia USD - khong dong icon de giu nguyen anh card da dat trong scene.
        /// </summary>
        public void SetIapOffer(string displayName, int amount, string priceLabel)
        {
            itemName ??= transform.Find("ItemName")?.GetComponent<TMP_Text>();
            quantity ??= transform.Find("QuantityPill/Quantity")?.GetComponent<TMP_Text>();
            quantity ??= transform.Find("Quantity")?.GetComponent<TMP_Text>();
            priceText ??= transform.Find("BuyButton/PriceText")?.GetComponent<TMP_Text>();
            if (itemName != null) itemName.text = displayName;
            if (quantity != null) quantity.text = amount.ToString("N0");
            if (priceText != null) priceText.text = priceLabel;
        }
    }
}
