using UnityEngine;
using UnityEngine.UI;
using EternalClash.Audio;

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
