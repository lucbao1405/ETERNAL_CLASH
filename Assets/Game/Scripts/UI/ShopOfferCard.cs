using UnityEngine;
using EternalClash.Audio;
using EternalClash.Monetization;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Du lieu goi hang gan truc tiep len card trong scene ("ban cung").
    /// Nút MUA goi Buy(); sua gia/so luong ngay tren Inspector cua card.
    /// Dien iapProductId (mot productId trong IapService.Catalog) de card
    /// chuyen sang MUA BANG TIEN THAT qua IAP - luc do so luong va gia USD
    /// tu dong dong bo tu catalog, khong dung den amount/price ben duoi.
    /// </summary>
    public class ShopOfferCard : MonoBehaviour
    {
        [SerializeField] private string itemId = "diamon";
        [SerializeField] private int amount = 100;
        [SerializeField] private string currencyId = "coin";
        [SerializeField] private int price = 29000;
        [SerializeField, Tooltip("productId trong IapService.Catalog. De trong neu mua bang vang/gem trong game.")]
        private string iapProductId = "";

        public string IapProductId => iapProductId;

        private void OnEnable()
        {
            SyncIapLabels();
        }

        public void Buy()
        {
            GameAudio.Play(SoundId.UIClick);

            // Card IAP: mua bang tien that (FakeStore trong Editor), khong tieu vang/gem.
            if (!string.IsNullOrWhiteSpace(iapProductId))
            {
                IapService.Buy(iapProductId);
                return;
            }

            // Instance co the la fake-null sau khi scene chua object goc bi unload.
            GoldSystem gold = GoldSystem.Instance != null ? GoldSystem.Instance : FindObjectOfType<GoldSystem>();
            if (gold == null)
            {
                Debug.LogWarning("[ShopOfferCard] GoldSystem chua khoi tao - khong the mua.");
                return;
            }

            bool paid = currencyId == "coin" ? gold.SpendGold(price) : gold.SpendGem(price);
            if (!paid)
            {
                Debug.Log($"[ShopOfferCard] Khong du {currencyId} de mua {amount} {itemId} (can {price}).");
                return;
            }

            if (itemId == "coin") gold.AddGold(amount);
            else if (itemId == "diamon") gold.AddGem(amount);
            else Debug.Log($"[ShopOfferCard] Da mua {amount} {itemId}.");
        }

        private void SyncIapLabels()
        {
            if (string.IsNullOrWhiteSpace(iapProductId))
                return;

            IapProductDef product = IapService.Find(iapProductId);
            if (product == null)
            {
                Debug.LogWarning($"[ShopOfferCard] Khong tim thay IAP product '{iapProductId}' cho {name}.");
                return;
            }

            ShopItemSlotUI slot = GetComponent<ShopItemSlotUI>();
            if (slot == null)
            {
                Debug.LogWarning($"[ShopOfferCard] {name} thieu ShopItemSlotUI - khong sync duoc label IAP.");
                return;
            }

            slot.SetIapOffer(product.displayName, product.PrimaryAmount, product.priceLabel);
        }
    }
}
