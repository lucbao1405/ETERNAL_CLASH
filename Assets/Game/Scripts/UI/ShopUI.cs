using UnityEngine;
using UnityEngine.UI;
using EternalClash.Audio;

namespace EternalClash.UI
{
    /// <summary>
    /// Panel shop kieu PostKnight. Card la doi tuong cung dat san trong scene
    /// (Content), moi card mang ShopOfferCard da cau hinh san (productId, gia,
    /// so luong) ngay tren Inspector - khong sinh hay ghi de gi luc chay de
    /// khong phu thuoc wiring runtime khi len dien thoai. Script nay chi mo/dong,
    /// cam xac khi keo danh sach va noi nut MUA voi card.Buy().
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
            WireBuyButtons();
        }

        /// <summary>Chi noi nut MUA cua tung card (da cau hinh san trong scene)
        /// voi ShopOfferCard.Buy. Card thieu component se bi bo qua.</summary>
        private void WireBuyButtons()
        {
            ShopItemSlotUI[] slots = GetComponentsInChildren<ShopItemSlotUI>(true);
            foreach (ShopItemSlotUI slot in slots)
            {
                Button buyButton = slot.GetComponentInChildren<Button>(true);
                ShopOfferCard card = slot.GetComponent<ShopOfferCard>();
                if (buyButton == null || card == null)
                {
                    if (buyButton != null)
                        Debug.LogWarning($"[ShopUI] {slot.name} thieu ShopOfferCard - nut MUA khong hoat dong.");
                    continue;
                }

                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(card.Buy);
                // Card quang cao (kim cuong xem QC): cap nhat nhan so luot con lai.
                card.RefreshLabel();
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
