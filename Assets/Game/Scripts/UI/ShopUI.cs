using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private ScrollRect shopScroll;
        [SerializeField] private Transform content;
        [SerializeField] private ShopItemSlotUI itemPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button bottomCloseButton;

        private static readonly string[] TestItems =
        {
            "Amethyst Shard", "Moon Pearl", "Ruby Cluster", "Crystal Chest",
            "Gold Coins", "Ancient Coin", "Mystic Scroll", "Healing Flask",
            "Iron Ore", "Leather Bundle", "Lucky Charm", "Treasure Map"
        };

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
            closeButton ??= transform.Find("Header/Close_Button")?.GetComponent<Button>();
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
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null) animator.Close();
            else gameObject.SetActive(false);
        }

        public void RefreshShop()
        {
            if (content == null || itemPrefab == null)
                return;

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            for (int i = 0; i < TestItems.Length; i++)
            {
                ShopItemSlotUI item = Instantiate(itemPrefab, content);
                item.SetItem(TestItems[i], (i + 1) * 100, (i + 1) * 250);
            }
        }
    }
}
