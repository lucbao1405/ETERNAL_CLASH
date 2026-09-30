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

        /// <summary>Offer hoi sinh dang dong bang tran (Time.timeScale = 0).
        /// HitStopImpactSystem doc co nay de khong set/tra timeScale tranh chan
        /// freeze cua offer - neu khong thi mot hit-stop dang chay se "tra lai"
        /// timeScale = 1 giua luc offer mo va tran chay lai sau lung offer.</summary>
        public static bool OfferFreezeActive { get; private set; }

        public static void ResetForBattle()
        {
            usedThisRun = false;
            OfferFreezeActive = false;
        }

        /// <summary>Kiem tra offer con co the dua ra khong (chua dung, tran dang chay).
        /// Ben goi dung de quyet dinh co cho delay truoc khi mo offer hay khong.</summary>
        public static bool CanOffer(HealthSystem health)
        {
            if (usedThisRun || health == null)
                return false;

            StageManager stage = StageManager.Instance;
            return stage == null || stage.CurrentState == StageManager.StageState.Running;
        }

        /// <summary>Tra ve true neu offer da duoc dua ra (Ben goi DUNG chay tiep
        /// flow chet). Tra ve false neu da hoi sinh tran nay - de flow chet chay binh thuong.</summary>
        public static bool TryOffer(HealthSystem health)
        {
            if (!CanOffer(health))
                return false;

            OpenOffer(health);
            return true;
        }

        /// <summary>Mo offer thuc su (dung sau delay cho player chet ~2 giay).
        /// Chan flow chet bang MarkRevivePending va dong bang tran.</summary>
        public static void OpenOffer(HealthSystem health)
        {
            if (health == null || usedThisRun)
                return;

            usedThisRun = true;
            health.MarkRevivePending();

            timeScaleBeforeOffer = Time.timeScale;
            Time.timeScale = 0f;
            OfferFreezeActive = true;

            OfferOverlayUI.Show(
                "Revive?",
                "Watch an ad to revive with 50% HP and keep fighting.\nGold and EXP you collected are kept.",
                "Watch Ad",
                onWatchClicked: () => AdsService.ShowRewarded("revive", success => Resolve(health, success)),
                onDeclined: () => Resolve(health, false),
                autoDeclineSeconds: 5f);
        }

        private static void Resolve(HealthSystem health, bool success)
        {
            Time.timeScale = timeScaleBeforeOffer <= 0f ? 1f : timeScaleBeforeOffer;
            OfferFreezeActive = false;

            // health co the da bi huy khi doi scene trong luc offer mo.
            if (health == null || !health)
            {
                OfferOverlayUI.Close();
                return;
            }

            // Stage da ket thuc (thang/thua) thi khong con nen hoi sinh.
            // Popup co the con mo khi flow victory bat dau (race condition).
            StageManager stage = StageManager.Instance;
            if (stage != null && stage.CurrentState != StageManager.StageState.Running)
            {
                Debug.Log("[Revive] Da qua tran, bo qua hoi sinh.");
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
