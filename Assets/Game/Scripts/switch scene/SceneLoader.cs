using UnityEngine;
using EternalClash.Core;
using EternalClash.UI;

public class SceneLoader : MonoBehaviour
{
    public string gameplaySceneName = "Gameplay";

    /// <summary>
    /// Nut GO o Town. Mau phai dat PlayerConditionSystem.MIN_BATTLE_HP_PERCENT moi
    /// duoc vao tran; chua du thi bao so mau can hoi va o lai Town.
    /// </summary>
    public void LoadGameplay()
    {
        // Nut GO thuong (khong phai boss): huy thu thach boss con ton tai tu lan
        // choi boss truoc do, neu khong man thuong se bi thay bang man boss.
        EternalClash.Enemy.BossChallenge.Cancel();

        PlayerConditionSystem condition = PlayerConditionSystem.Instance;
        if (condition != null && !condition.CanStartBattle())
        {
            // ToastMessage đã bị khóa: không hiển thị popup cảnh báo khi HP thấp.
            HpLowBlink.BlinkAll();
            Debug.Log("[TOWN] Chan vao tran: " + condition.GetInjuredBlockReason());
            return;
        }

        // Qua Core.SceneLoader: ghi save dang cho va khoi phuc timeScale truoc khi doi scene.
        EternalClash.Core.SceneLoader.LoadScene(gameplaySceneName);
    }
}
