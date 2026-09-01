using UnityEngine;
using System;
using EternalClash.Character;

namespace EternalClash.Core
{
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        public event Action<bool> OnRewardedAdCompleted;
        public event Action<string> OnPurchaseCompleted;

        private SaveData Data => SaveManager.Instance != null ? SaveManager.Instance.Data : null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Hook for Rewarded Ad: Double end of match rewards.
        /// </summary>
        public void ShowRewardedAd_DoubleRewards(Action onSuccess)
        {
            Debug.Log("[ADS] Showing Rewarded Video for 2x Rewards...");
            // Simulate / Hook ad network completion
            onSuccess?.Invoke();
            OnRewardedAdCompleted?.Invoke(true);
        }

        /// <summary>
        /// Hook for Rewarded Ad: Emergency Revive at 50% HP.
        /// </summary>
        public void ShowRewardedAd_Revive(Action onSuccess)
        {
            Debug.Log("[ADS] Showing Rewarded Video for Revive...");
            var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                Time.timeScale = 1f;
                int reviveHp = playerHealth.MaxHealth / 2;
                playerHealth.Heal(reviveHp);
                Debug.Log($"[ADS] Player revived with {reviveHp} HP!");
            }
            onSuccess?.Invoke();
            OnRewardedAdCompleted?.Invoke(true);
        }

        /// <summary>
        /// Hook for IAP: Remove Ads ($2.99)
        /// </summary>
        public void PurchaseRemoveAds()
        {
            Debug.Log("[IAP] Purchasing Remove Ads ($2.99)...");
            if (Data != null)
            {
                Data.hasRemovedAds = true;
                SaveManager.Instance?.Save();
            }
            OnPurchaseCompleted?.Invoke("no_ads");
        }

        /// <summary>
        /// Hook for IAP: Monthly VIP Pass ($4.99)
        /// </summary>
        public void PurchaseVipPass()
        {
            Debug.Log("[IAP] Purchasing Monthly VIP Pass ($4.99)...");
            if (Data != null)
            {
                Data.isVipActive = true;
                // Daily materials bonus
                Data.oreMaterial += 10;
                Data.leatherMaterial += 10;
                SaveManager.Instance?.Save();
            }
            OnPurchaseCompleted?.Invoke("vip_monthly");
        }

        /// <summary>
        /// Hook for IAP: Skin packages (Visual only)
        /// </summary>
        public void PurchaseSkin(string skinId)
        {
            Debug.Log($"[IAP] Purchased Skin: {skinId}");
            OnPurchaseCompleted?.Invoke(skinId);
        }
    }
}
