using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Hoi sinh 1 lan moi tran: khi Player chet, dung lai tran de dua offer
    /// xem quang cao - xem thi song lai voi 50% HP va danh tiep (giu nguyen
    /// da nhat), khong xem thi chay dung flow thua cu.
    /// 
    /// Cach chan flow: trong luc offer mo, HealthSystem.RevivePending = true
    /// giup IsDead tra ve false nen PlayerDeathHandler (event + Update poll)
    /// chua kip kich hoat flow thua. Chi khi tu choi hoac ad that bai thi
    /// ConfirmDeath() moi thuc hien dung buoc chet nhu ban cu.
    /// </summary>
    public static class ReviveOffer
    {
        private static bool usedThisRun;
        private static float timeScaleBeforeOffer = 1f;

        public static void ResetForBattle()
        {
            usedThisRun = false;
        }

        /// <summary>Tra ve true neu offer da duoc dua ra (Ben goi DUNG chay tiep
        /// flow chet). Tra ve false neu da hoi sinh tran nay - de flow chet chay binh thuong.</summary>
        public static bool TryOffer(HealthSystem health)
        {
            if (usedThisRun || health == null)
                return false;

            usedThisRun = true;
            health.MarkRevivePending();

            timeScaleBeforeOffer = Time.timeScale;
            Time.timeScale = 0f;

            OfferOverlayUI.Show(
                "Revive?",
                "Watch an ad to revive with 50% HP and keep fighting.\nGold and EXP you collected are kept.",
                "Watch Ad",
                onWatchClicked: () => AdsService.ShowRewarded("revive", success => Resolve(health, success)),
                onDeclined: () => Resolve(health, false),
                autoDeclineSeconds: 5f);
            return true;
        }

        private static void Resolve(HealthSystem health, bool success)
        {
            Time.timeScale = timeScaleBeforeOffer <= 0f ? 1f : timeScaleBeforeOffer;

            // health co the da bi huy khi doi scene trong luc offer mo.
            if (health == null || !health)
            {
                OfferOverlayUI.Close();
                return;
            }

            if (success)
            {
                Debug.Log("[Revive] Nguoi choi hoi sinh voi 50% HP.");
                health.ReviveAtHalfHealth();
            }
            else
            {
                Debug.Log("[Revive] Tu choi hoi sinh - chay flow thua.");
                health.ConfirmDeath();
            }
        }
    }
}
