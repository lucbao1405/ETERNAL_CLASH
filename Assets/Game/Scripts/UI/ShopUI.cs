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
                IapService.GemLargeId
            };

            int productIndex = 0;
            foreach (ShopItemSlotUI slot in slots)
            {
                if (productIndex >= productIds.Length)
                    break;

                Button buyButton = slot.GetComponentInChildren<Button>(true);
                if (buyButton == null)
                    continue;

                ShopOfferCard card = slot.GetComponent<ShopOfferCard>();
                if (card == null)
                    card = slot.gameObject.AddComponent<ShopOfferCard>();
                card.ConfigureIap(productIds[productIndex++]);

                buyButton.onClick.RemoveAllListeners();
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
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }
    }
}
