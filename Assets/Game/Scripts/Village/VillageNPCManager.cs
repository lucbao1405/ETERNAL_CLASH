using UnityEngine;

public class VillageNPCManager : MonoBehaviour
{
    public void TalkToVillageChief()
    {
        Debug.Log("Village Chief: Welcome back hero!");
        // Quest, story, stage unlock
    }

    public void TalkToGirlfriend()
    {
        Debug.Log("Girlfriend: Take care on your next journey!");
        // Dialogue, affection system later
    }

    public void OpenBlacksmith()
    {
        Debug.Log("Open Blacksmith Upgrade Menu");
        // Equipment upgrade UI
    }

    public void OpenPotionHouse()
    {
        Debug.Log("Open Potion Upgrade Menu");
        // Health potion upgrade UI
    }
}