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
        PlayerConditionSystem condition = PlayerConditionSystem.Instance;
        if (condition != null && !condition.CanStartBattle())
        {
            ToastMessage.Show(condition.GetInjuredBlockReason());
            HpLowBlink.BlinkAll();
            Debug.Log("[TOWN] Chan vao tran: " + condition.GetInjuredBlockReason());
            return;
        }

        // Qua Core.SceneLoader: ghi save dang cho va khoi phuc timeScale truoc khi doi scene.
        EternalClash.Core.SceneLoader.LoadScene(gameplaySceneName);
    }
}
