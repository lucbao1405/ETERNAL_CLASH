using EternalClash.UI;
using NUnit.Framework;
using UnityEngine;

namespace EternalClash.Tests.EditMode
{
    public class BattleProgressUITests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void StageFraction_TracksWavesAndKills()
        {
            // Truoc wave dau tien chua ha quai nao -> 0.
            Assert.AreEqual(0f, BattleProgressUI.StageFraction(0, 3, 0, 0), Eps);

            // Giua wave 1: ha 2/4 quai -> 0.5/3.
            Assert.AreEqual(0.5f / 3f, BattleProgressUI.StageFraction(1, 3, 2, 4), Eps);

            // Xong wave 1 (killed == total) -> dung 1/3.
            Assert.AreEqual(1f / 3f, BattleProgressUI.StageFraction(1, 3, 4, 4), Eps);

            // Giua wave cuoi: (2 + 0.25)/3.
            Assert.AreEqual(2.25f / 3f, BattleProgressUI.StageFraction(3, 3, 1, 4), Eps);

            // Xong stage -> dung 1f, player cham Dichden.
            Assert.AreEqual(1f, BattleProgressUI.StageFraction(3, 3, 4, 4), Eps);

            // Gia tri ngoai pham vi khong lam tran.
            Assert.AreEqual(1f, BattleProgressUI.StageFraction(5, 3, 99, 4), Eps);
            Assert.AreEqual(0f, BattleProgressUI.StageFraction(0, 0, 0, 0), Eps);
        }
    }
}
