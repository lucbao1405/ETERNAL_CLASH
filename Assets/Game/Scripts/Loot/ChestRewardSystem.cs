using UnityEngine;
using EternalClash.Village;

public class ChestRewardSystem : MonoBehaviour
{
    public bool opened;

    [SerializeField] private int clearGold = 100;
    [SerializeField] private int clearOre = 2;
    [SerializeField] private int clearLeather = 1;
    [SerializeField] private int clearExp = 50;
    [SerializeField] private int clearAffinity = 20;

    public void OpenChest()
    {
        if (opened) return;
        opened = true;

        GiveReward();
    }

    private void GiveReward()
    {
        GoldSystem.Instance?.AddGold(clearGold);
        GoldSystem.Instance?.AddMaterials(clearOre, clearLeather);
        PlayerStatSystem.Instance?.AddExp(clearExp);
        AffinityManager.Instance?.AddAffinityPoints(clearAffinity);

        Debug.Log($"[STAGE CLEAR] Received {clearGold} Gold, {clearOre} Ore, {clearLeather} Leather, {clearExp} EXP, {clearAffinity} Affinity Points!");
    }
}
