using System;
using UnityEngine;
using EternalClash.Village;
using EternalClash.Core.Save;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Kim cuong nhan bang quang cao rewarded thay cho mua IAP: moi lan xem
    /// duoc GemsPerAd kim cuong, toi da MaxAdsPerAd lan trong 1 ngay. So lan
    /// xem duoc luu trong SaveData (gemAdsWatchedToday) va tu reset sang ngay moi.
    /// </summary>
    public static class GemAdService
    {
        public const int GemsPerAd = 20;
        public const int MaxAdsPerDay = 3;
        private const string Placement = "gem_daily";

        private static SaveData Data => SaveManager.Instance != null ? SaveManager.Instance.Data : null;

        private static string TodayKey => DateTime.Now.ToString("yyyy-MM-dd");

        public static int WatchedToday
        {
            get
            {
                SaveData data = Data;
                if (data == null)
                    return 0;
                EnsureDay(data);
                return data.gemAdsWatchedToday;
            }
        }

        public static int RemainingToday => Mathf.Max(0, MaxAdsPerDay - WatchedToday);

        public static bool CanWatch()
        {
            SaveData data = Data;
            if (data == null)
                return false;
            EnsureDay(data);
            return data.gemAdsWatchedToday < MaxAdsPerDay;
        }

        public static void Watch(Action<bool> onResult)
        {
            if (!CanWatch())
            {
                onResult?.Invoke(false);
                return;
            }

            AdsService.ShowRewarded(Placement, success =>
            {
                if (!success)
                {
                    onResult?.Invoke(false);
                    return;
                }

                SaveData data = Data;
                if (data == null)
                {
                    onResult?.Invoke(false);
                    return;
                }

                EnsureDay(data);
                if (data.gemAdsWatchedToday >= MaxAdsPerDay)
                {
                    onResult?.Invoke(false);
                    return;
                }

                data.gemAdsWatchedToday++;
                GoldSystem.Instance?.AddGem(GemsPerAd);
                SaveCoordinator.RequestSave();
                Debug.Log($"[ADS] Diamonds via ad: {data.gemAdsWatchedToday}/{MaxAdsPerDay} (+{GemsPerAd}).");
                onResult?.Invoke(true);
            });
        }

        private static void EnsureDay(SaveData data)
        {
            string today = TodayKey;
            if (!string.Equals(data.gemAdDate, today, StringComparison.Ordinal))
            {
                data.gemAdDate = today;
                data.gemAdsWatchedToday = 0;
            }
        }
    }
}
