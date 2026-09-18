using System;
using UnityEngine;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Diem trung gian duy nhất cho quang cao rewarded. Toan bo touchpoint
    /// (X2, hoi sinh, daily gift) goi qua day.
    /// Quang cao la content TU SAN XUAT cua team (video/hinh trong
    /// Resources/Ads) do SelfMadeAdPlayer phat - khong gan mang quang cao
    /// ben ngoai. Khi can doi cach phat thi chi sua trong SelfMadeAdPlayer.
    /// </summary>
    public static class AdsService
    {
        private static AdsRunner runner;
        private static bool showing;

        public static bool IsRewardedReady => true;

        public static void ShowRewarded(string placement, Action<bool> onResult)
        {
            if (showing)
            {
                Debug.LogWarning($"[Ads] Placement '{placement}' bi bo qua: co quang cao khac dang chay.");
                onResult?.Invoke(false);
                return;
            }

            EnsureRunner();
            showing = true;
            SelfMadeAdPlayer.Play(placement, success =>
            {
                showing = false;
                onResult?.Invoke(success);
            });
        }

        private static void EnsureRunner()
        {
            if (runner != null)
                return;

            runner = new GameObject("AdsService (Runtime)").AddComponent<AdsRunner>();
            UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
        }

        private sealed class AdsRunner : MonoBehaviour { }
    }
}
