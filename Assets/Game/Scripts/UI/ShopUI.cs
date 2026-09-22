using UnityEngine;
using UnityEngine.UI;
using EternalClash.Audio;
using EternalClash.Monetization;

namespace EternalClash.UI
{
    /// <summary>
    /// Panel shop kieu PostKnight. Card la doi tuong cung dat san trong scene
    /// (Content), moi card mang ShopOfferCard de nut MUA tu lo. Script nay chi
    /// quan ly mo/dong va cam xac khi keo danh sach.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private ScrollRect shopScroll;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button bottomCloseButton;

        private void Awake()
        {
            AutoWireReferences();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bottomCloseButton != null) bottomCloseButton.onClick.AddListener(Close);
            WireRuntimeIapCards();
        }

        private void WireRuntimeIapCards()
        {
            ShopItemSlotUI[] slots = GetComponentsInChildren<ShopItemSlotUI>(true);
            string[] productIds = {
                IapService.GemSmallId,
                IapService.GemMediumId,
                IapService.GemLargeId,
                IapService.GemHugeId
            };

            int productIndex = 0;
            foreach (ShopItemSlotUI slot in slots)
            {
                Button buyButton = slot.GetComponentInChildren<Button>(true);
                if (buyButton == null)
                    continue;

                ShopOfferCard card = slot.GetComponent<ShopOfferCard>();
                if (card == null)
                    card = slot.gameObject.AddComponent<ShopOfferCard>();

                buyButton.onClick.RemoveAllListeners();

                // 4 card kim cuong dau tien mua bang tien that (fake khi test);
                // cac card con lai (vang) mua bang kim cuong trong game.
                if (productIndex < productIds.Length)
                    card.ConfigureIap(productIds[productIndex++]);
                else
                    card.ConfigureGemPurchase();

                buyButton.onClick.AddListener(card.Buy);
            }
        }

        private void OnEnable()
        {
            Open();
        }

        private void AutoWireReferences()
        {
            shopScroll ??= transform.Find("ShopScrollView")?.GetComponent<ScrollRect>();
            // Nut X la con cua panel goc (Shop/X), khong nam trong ShopPanel.
            closeButton ??= transform.parent?.Find("X")?.GetComponent<Button>();
            bottomCloseButton ??= transform.Find("Bottom_Close_Button")?.GetComponent<Button>();
        }

        public void Open()
        {
            gameObject.SetActive(true);
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
            if (animator != null)
            {
                animator.Close();
                return;
            }

            // Panel root co the duoc SmoothSlide quan ly (vd Canvas/Man_Hinh_Khac/Shop,
            // con ShopUI nam tren VatPhamMua). Khong duoc tu SetActive(false) o day
            // vi khong ai bat lai object nay khi mo panel lan sau - chi forward close
            // cho manager go ben duoc va de no tat root.
            SmoothSlide slide = GetComponent<SmoothSlide>()
                ?? GetComponentInParent<SmoothSlide>(true);
            if (slide != null)
            {
                if (slide.IsOpen)
                    slide.ClosePanel();
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
