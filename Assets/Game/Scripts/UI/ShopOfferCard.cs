using UnityEngine;
using EternalClash.Audio;
using EternalClash.Monetization;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Du lieu goi hang "ban cung": component duoc dat san tren tung card trong
    /// scene Town, sua gia/so luong/productId ngay tren Inspector cua card.
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

        public void Buy()
        {
            GameAudio.Play(SoundId.UIClick);

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

            // TODO: tam thoi tren mobile chua mua duoc bang tien - cho phep nhan
            // vat pham mien phi khi bam mua. Khoi phuc lai viec tru tien sau.
            if (itemId == "coin") gold.AddGold(amount);
            else if (itemId == "diamon") gold.AddGem(amount);
            else Debug.Log($"[ShopOfferCard] Da mua {amount} {itemId}.");
        }
    }
}
