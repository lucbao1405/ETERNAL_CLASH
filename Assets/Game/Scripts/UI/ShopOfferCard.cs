using UnityEngine;
using EternalClash.Audio;
using EternalClash.Monetization;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Du lieu goi hang "ban cung": component duoc dat san tren tung card trong
    /// scene Town, sua gia/so luong/productId ngay tren Inspector cua card.
    /// offerByAd = true: card tra kim cuong bang quang cao rewarded (GemAdService,
    /// +20 kim cuong / lan xem, toi da 3 lan/ngay) - khong dung IAP hay vang.
    /// Card IAP: dien iapProductId (mot productId trong IapService.Catalog) de
    /// mua bang tien that (FakeStore grant truc tiep khi test). Card thong
    /// thuong: itemId = vat nhan duoc, currencyId = tien phai tra ("coin" tra
    /// bang vang, "diamon" tra bang kim cuong).
    /// </summary>
    public class ShopOfferCard : MonoBehaviour
    {
        [SerializeField] private string itemId = "diamon";
        [SerializeField] private int amount = 100;
        [SerializeField] private string currencyId = "coin";
        [SerializeField] private int price = 29000;
        [SerializeField, Tooltip("productId trong IapService.Catalog. De trong neu mua bang vang/gem trong game.")]
        private string iapProductId = "";
        [SerializeField, Tooltip("Bat neu card nay tra kim cuong bang quang cao rewarded thay vi mua.")]
        private bool offerByAd = false;

        public bool IsAdOffer => offerByAd;

        public void Buy()
        {
            GameAudio.Play(SoundId.UIClick);

            // Card quang cao: xem video rewarded de nhan kim cuong (gioi han ngay).
            if (offerByAd)
            {
                WatchAdForGems();
                return;
            }

            // Card IAP: mua bang tien that (FakeStore trong Editor/test), khong tieu vang/gem.
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

            if (price > 0)
            {
                bool paid = string.Equals(currencyId, "diamon", System.StringComparison.OrdinalIgnoreCase)
                    ? gold.SpendGem(price)
                    : gold.SpendGold(price);
                if (!paid)
                {
                    ToastMessage.Show(string.Equals(currencyId, "diamon", System.StringComparison.OrdinalIgnoreCase)
                        ? "Not enough diamonds!"
                        : "Not enough gold!");
                    return;
                }
            }

            if (itemId == "coin") gold.AddGold(amount);
            else if (itemId == "diamon") gold.AddGem(amount);
            else Debug.Log($"[ShopOfferCard] Da mua {amount} {itemId}.");
        }

        /// <summary>Dong bo nhan tren card theo so luot xem quang cao con lai.</summary>
        public void RefreshLabel()
        {
            if (!offerByAd)
                return;

            ShopItemSlotUI slot = GetComponent<ShopItemSlotUI>();
            if (slot != null)
                slot.SetAdOffer(GemAdService.WatchedToday, GemAdService.RemainingToday);
        }

        private void WatchAdForGems()
        {
            if (!GemAdService.CanWatch())
            {
                ToastMessage.Show("No ad rewards left for today!");
                RefreshLabel();
                return;
            }

            // Giong offer ben Battle (X2 cuoi tran / hoi sinh): hien panel
            // Ad_Offer (da copy sang Town) truoc khi phat video. Panel tu tim
            // theo ten "Ad_Offer" trong scene, khong co thi dung overlay runtime.
            OfferOverlayUI.Show(
                "Claim Diamonds!",
                $"Watch an ad to receive +{GemAdService.GemsPerAd} diamonds ({GemAdService.RemainingToday}/{GemAdService.MaxAdsPerDay} rewards left today).",
                "Watch Ad",
                onWatchClicked: () => GemAdService.Watch(ok =>
                {
                    RefreshLabel();
                    if (ok)
                        ToastMessage.Show($"+{GemAdService.GemsPerAd} diamonds!");
                }),
                onDeclined: RefreshLabel);
        }
    }
}
