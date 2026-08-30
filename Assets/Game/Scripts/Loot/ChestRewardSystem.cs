using UnityEngine;

public class ChestRewardSystem : MonoBehaviour
{
    public bool opened;

    public void OpenChest()
    {
        if (opened) return;
        opened = true;

        GiveReward();
    }

    private void GiveReward()
    {
        // Reward pipeline:
        // Gold
        // Equipment material
        // Upgrade item
        Debug.Log("Chest reward received");
    }
}
