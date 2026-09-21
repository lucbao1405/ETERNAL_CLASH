using System;
using UnityEngine;
using UnityEngine.Purchasing;
using EternalClash.Village;
using EternalClash.Core.Save;

namespace EternalClash.Monetization
{
    /// <summary>Mot san pham IAP trong danh muc. Gia hien tren card la chu
    /// priceLabel - gia that do cua hang app tra ve khi len store.</summary>
    [Serializable]
    public class IapProductDef
    {
        public string productId;
        public string displayName;
        public string priceLabel;
        public int gold;
        public int gem;
        public bool nonConsumable;

        /// <summary>So luong chinh de hien tren card shop (moi san pham chi
        /// ban mot loai tien te; Starter Pack la bundle nen lay gold).</summary>
        public int PrimaryAmount => gold > 0 ? gold : gem;
    }

    /// <summary>
    /// Duy nhat noi xu ly IAP (Unity Purchasing). Trong Editor su dung
    /// FakeStore nen mua thu duoc khong can cua hang that. Khi len store chi
    /// can giu nguyen danh muc productId trong Catalog.
    /// </summary>
    public sealed class IapService : MonoBehaviour, IStoreListener
    {
        public const string StarterPackId = "com.eternalclash.starterpack";
        public const string GemSmallId = "com.eternalclash.gem.small";
        public const string GemMediumId = "com.eternalclash.gem.medium";
        public const string GemLargeId = "com.eternalclash.gem.large";

        /// <summary>
        /// QUY UOC SHOP: kim cuong mua bang TIEN THAT (IAP ben duoi), con VANG
        /// mua bang KIM CUONG (card ShopOfferCard thong thuong trong scene,
        /// currencyId = "diamon" - khong nam trong catalog nay).
        /// 4 san pham map 1:1 voi 4 card gem trong SHOP panel o Town.
        /// productId dat theo tier (khong theo so luong) de thay doi so luong
        /// ve sau khong phai doi id tren store.
        /// </summary>
        public static readonly IapProductDef[] Catalog =
        {
            new IapProductDef { productId = StarterPackId, displayName = "Starter Pack", priceLabel = "$2.99",
                gold = 1000, gem = 100, nonConsumable = true },
            new IapProductDef { productId = GemSmallId, displayName = "Kim cương", priceLabel = "$0.99", gem = 20 },
            new IapProductDef { productId = GemMediumId, displayName = "Kim cương", priceLabel = "$4.99", gem = 120 },
            new IapProductDef { productId = GemLargeId, displayName = "Kim cương", priceLabel = "$9.99", gem = 300 },
        };

        public static IapService Instance { get; private set; }

        private IStoreController storeController;
        private IExtensionProvider extensionProvider;

        public static void InitializeIfNeeded()
        {
            if (Instance != null && Instance.storeController != null)
                return;

            EnsureInstance();

            // ponytail: Editor chay FakeStore de test mua khong can store that.
            // Khi len Google Play / App Store thi bo dong duoi di.
            StandardPurchasingModule.Instance().useFakeStoreAlways = Application.isEditor;

            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (IapProductDef product in Catalog)
            {
                builder.AddProduct(product.productId,
                    product.nonConsumable ? ProductType.NonConsumable : ProductType.Consumable);
            }

            UnityPurchasing.Initialize(Instance, builder);
        }

        public static void Buy(string productId)
        {
            InitializeIfNeeded();

            if (Instance == null || Instance.storeController == null)
            {
                Debug.LogWarning($"[IAP] Store chua san sang, khong the mua {productId}.");
                return;
            }

            Instance.storeController.InitiatePurchase(productId);
        }

        public static IapProductDef Find(string productId)
        {
            foreach (IapProductDef product in Catalog)
                if (string.Equals(product.productId, productId, StringComparison.OrdinalIgnoreCase))
                    return product;
            return null;
        }

        private static void EnsureInstance()
        {
            if (Instance != null && Instance)
                return;

            Instance = FindObjectOfType<IapService>();
            if (Instance != null)
                return;

            Instance = new GameObject("IapService (Runtime)").AddComponent<IapService>();
            DontDestroyOnLoad(Instance.gameObject);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Dialog FakeStore (Unity IAP) ve nut bang IMGUI voi kich thuoc mac dinh
        // rat nho tren man dpi cao. Cac dialog deu dung GUI.skin.button nen phong
        // to style nay mot lan la lo ca nut Buy/Cancel va cac option "Select Response".
        // ponytail: GUI skin la global - neu sau nay them IMGUI khac trong game thi
        // can gioi han viec phong to theo trang thai dialog thay vi set mot lan.
        const float FakeStoreButtonHeight = 72f;
        const int FakeStoreButtonFontSize = 28;
        bool m_FakeStoreSkinEnlarged;

        void OnGUI()
        {
            if (m_FakeStoreSkinEnlarged)
                return;

            m_FakeStoreSkinEnlarged = true;
            GUIStyle button = GUI.skin.button;
            button.fixedHeight = FakeStoreButtonHeight;
            button.fontSize = FakeStoreButtonFontSize;
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            extensionProvider = extensions;
            Debug.Log("[IAP] Store khoi tao thanh cong.");
        }

#pragma warning disable 0618
        public void OnInitializeFailed(InitializationFailureReason reason)
        {
            OnInitializeFailed(reason, null);
        }
#pragma warning restore 0618

        public void OnInitializeFailed(InitializationFailureReason reason, string message)
        {
            Debug.LogWarning($"[IAP] Store khoi tao that bai: {reason} {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            string productId = purchaseEvent.purchasedProduct.definition.id;
            Debug.Log($"[IAP] Mua thanh cong: {productId}");
            Grant(productId);
            return PurchaseProcessingResult.Complete;
        }

#pragma warning disable 0618
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogWarning($"[IAP] Mua that bai: {product.definition.id} - {reason}");
        }
#pragma warning restore 0618

        private static void Grant(string productId)
        {
            IapProductDef product = Find(productId);
            if (product == null)
            {
                Debug.LogWarning($"[IAP] Khong tim thay san pham {productId} trong Catalog.");
                return;
            }

            GoldSystem currency = GoldSystem.Instance;
            if (product.gold > 0)
                currency?.AddGold(product.gold);
            if (product.gem > 0)
                currency?.AddGem(product.gem);

            if (product.nonConsumable)
            {
                SaveData data = SaveManager.Instance?.Data;
                if (data != null)
                {
                    data.starterPackPurchased = true;
                    SaveCoordinator.RequestSave();
                }
            }
        }
    }
}
