using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Audio;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    public class ShopUI : MonoBehaviour
    {
        /// <summary>Goi hang kieu PostKnight: mua "amount" don vi "itemId" bang dong tien "currency".</summary>
        private readonly struct Offer
        {
            public readonly string itemId;
            public readonly int amount;
            public readonly int price;
            public readonly string currency;

            public Offer(string itemId, int amount, string currency, int price)
            {
                this.itemId = itemId;
                this.amount = amount;
                this.currency = currency;
                this.price = price;
            }
        }

        // Bang gia duoc sao chep tu panel PostKnight: trang 1 ban gem bang vang,
        // trang 2 ban vang bang gem.
        private static readonly Offer[] Offers =
        {
            new Offer("diamon", 100, "coin", 29000),
            new Offer("diamon", 800, "coin", 129000),
            new Offer("diamon", 2600, "coin", 249000),
            new Offer("diamon", 7200, "coin", 498000),
            new Offer("coin", 10000, "diamon", 100),
            new Offer("coin", 54000, "diamon", 480),
            new Offer("coin", 118000, "diamon", 960),
            new Offer("coin", 480000, "diamon", 3200)
        };

        [SerializeField] private ScrollRect shopScroll;
        [SerializeField] private Transform content;
        [SerializeField] private ShopItemSlotUI itemPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button bottomCloseButton;

        private void Awake()
        {
            AutoWireReferences();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bottomCloseButton != null) bottomCloseButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            Open();
        }

        private void AutoWireReferences()
        {
            shopScroll ??= transform.Find("ShopScrollView")?.GetComponent<ScrollRect>();
            content ??= transform.Find("ShopScrollView/Viewport/Content");
            // Nut X la con cua panel goc (Shop/X), khong nam trong ShopPanel.
            closeButton ??= transform.parent?.Find("X")?.GetComponent<Button>();
            bottomCloseButton ??= transform.Find("Bottom_Close_Button")?.GetComponent<Button>();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshShop();
            if (shopScroll != null)
            {
                shopScroll.verticalNormalizedPosition = 1f;
                if (shopScroll.GetComponent<UIScrollSound>() == null)
                    shopScroll.gameObject.AddComponent<UIScrollSound>();
            }
        }

        public void Close()
        {
            GameAudio.Play(SoundId.UIClick);
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }

        public void RefreshShop()
        {
            if (content == null || itemPrefab == null)
                return;

            // DestroyImmediate de viec xay dung list xac dinh ke ca khi Open() bi
            // goi nhieu lan trong cung mot frame.
            for (int i = content.childCount - 1; i >= 0; i--)
                DestroyImmediate(content.GetChild(i).gameObject);

            foreach (Offer offer in Offers)
                CreateOfferCard(offer);
        }

        private void CreateOfferCard(Offer offer)
        {
            ItemData item = ItemCatalog.Find(offer.itemId);
            ItemData currencyItem = ItemCatalog.Find(offer.currency);

            ShopItemSlotUI slot = Instantiate(itemPrefab, content);
            slot.SetItem(item != null ? item.itemName : offer.itemId, offer.amount, offer.price,
                item != null ? item.icon : null);

            Image currencyIcon = slot.transform.Find("BuyButton/CurrencyIcon")?.GetComponent<Image>();
            if (currencyIcon != null)
                currencyIcon.sprite = currencyItem != null ? currencyItem.icon : null;

            Button buyButton = slot.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
                buyButton.onClick.AddListener(() => Buy(offer));
        }

        private void Buy(Offer offer)
        {
            GameAudio.Play(SoundId.UIClick);
            // Instance co the la fake-null sau khi scene chua object goc bi unload.
            GoldSystem gold = GoldSystem.Instance != null ? GoldSystem.Instance : FindObjectOfType<GoldSystem>();
            if (gold == null)
            {
                Debug.LogWarning("[ShopUI] GoldSystem chua khoi tao - khong the mua.");
                return;
            }

            bool paid = offer.currency == "coin" ? gold.SpendGold(offer.price) : gold.SpendGem(offer.price);
            if (!paid)
            {
                Debug.Log($"[ShopUI] Khong du {offer.currency} de mua {offer.amount} {offer.itemId} (can {offer.price}).");
                return;
            }

            if (offer.itemId == "coin") gold.AddGold(offer.amount);
            else if (offer.itemId == "diamon") gold.AddGem(offer.amount);
            else Debug.Log($"[ShopUI] Da mua {offer.amount} {offer.itemId}.");
        }
    }
}
